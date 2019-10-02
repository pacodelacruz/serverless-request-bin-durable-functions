using DotLiquid;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using ServerlessRequestBin.DurableFunctions.Extensions;
using ServerlessRequestBin.DurableFunctions.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace ServerlessRequestBin.DurableFunctions.Services
{
    //TODO: Split Rendering Functionality
    public class RequestBinService : IRequestBinService
    {
        private readonly IOptions<RequestBinOptions> Options;
        private static Template LiquidTemplate;

        public RequestBinService(IOptions<RequestBinOptions> options)
        {
            Options = options;
            // Read and load the Liquid Template from an embedded resource
            using (var reader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream(
                $"{Assembly.GetExecutingAssembly().GetName().Name}.Resources.{options.Value.RequestBinRendererTemplate}")))
            {
                LiquidTemplate = Template.Parse(reader.ReadToEnd());
            }
        }
        public bool IsBinIdValid(string binId, out string validationMessage)
        {
            validationMessage = "";
            if (string.IsNullOrWhiteSpace(binId))
            {
                validationMessage = "Bin Id cannot be empty.";
                return false;
            }
            else if (binId.Length > 36)
            {
                validationMessage = "Bin Id cannot be longer than 36 chars.";
                return false;
            }
            else if (!binId.All(c => Char.IsLetterOrDigit(c) && (c < 128) || c == '-' || c == '_' || c == '.'))
            {
                validationMessage = "Bin Id can only contain Numbers, Letters, '-', '_' and '.'";
                return false;
            }
            else if (binId.Equals("bin", StringComparison.OrdinalIgnoreCase))
            {
                validationMessage = "Bin Id cannot be 'bin'.";
                return false;
            }
            return true;
        }

        public string EncodeBinId(string binId)
        {
            return Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(binId));
        }

        public async Task<HttpRequestDescription> GetRequestDescriptionAsync(HttpRequest request)
        {
            var HeadersToIgnore = Options.Value.HeadersToIgnore.ToLower().Split('|').ToList();

            HttpRequestDescription requestDescription = new HttpRequestDescription();
            requestDescription.Body = await ReadBodyFirstCharsAsync(request, Options.Value.RequestBodyMaxLength);
            requestDescription.Method = request.Method;
            requestDescription.SourceIp = request.HttpContext.Connection.RemoteIpAddress.ToString();
            requestDescription.Path = $"{request.Path}{(string.IsNullOrEmpty(request.QueryString.ToString()) ? "" : request.QueryString.ToString())}";
            requestDescription.Timestamp = DateTime.UtcNow;
            requestDescription.QueryParams = new List<KeyValuePair<string, string>>();
            requestDescription.Headers = new List<KeyValuePair<string, string>>();

            foreach (var param in request.Query)
            {
                requestDescription.QueryParams.Add(new KeyValuePair<string, string>(param.Key, param.Value));
            }

            //TODO: Remove headers from Azure Functions
            foreach (var header in request.Headers)
            {
                if (!HeadersToIgnore.Contains(header.Key.ToLower()))
                    requestDescription.Headers.Add(new KeyValuePair<string, string>(header.Key, header.Value));
            }
            return requestDescription;
        }

        private async Task<string> ReadBodyFirstCharsAsync(HttpRequest request, int size)
        {
            using (var reader = new StreamReader(request.Body, Encoding.UTF8))
            {
                char[] buffer = new char[size];
                int n = await reader.ReadBlockAsync(buffer, 0, size);
                char[] result = new char[n];
                Array.Copy(buffer, result, n);
                return new string(result);
            }
        }
    }
}
