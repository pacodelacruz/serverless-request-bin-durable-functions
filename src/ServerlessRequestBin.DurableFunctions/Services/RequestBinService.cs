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

        public async Task<HttpRequestDescription> GetRequestDescriptionAsync(HttpRequest request)
        {
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
                requestDescription.Headers.Add(new KeyValuePair<string, string>(header.Key, header.Value));
            }

            return requestDescription;
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
               
        public string RenderToString(string binId, string binUrl, List<HttpRequestDescription> storedRequests = null, string errorMessage = null)
        {
            var requestBinHistory = PrepareBinHistoryForHtml(GetRequestBinHistory(binId, binUrl, storedRequests, errorMessage));
            return LiquidTemplate.Render(Hash.FromDictionary(requestBinHistory.ToDictionary()));
        }

        public HttpRequestBinHistory GetRequestBinHistory(string binId, string binUrl, List<HttpRequestDescription> storedRequests, string errorMessage = null)
        {
            if (string.IsNullOrWhiteSpace(errorMessage))
            {
                var encodedBinId = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(binId));

                if (storedRequests != null && storedRequests.Count > 0)
                    return new HttpRequestBinHistory(Options.Value)
                    {
                        BinId = binId,
                        BinUrl = binUrl,
                        Timestamp = DateTime.UtcNow,
                        RequestHistoryItems = storedRequests
                    };
                else
                    return new HttpRequestBinHistory(Options.Value)
                    {
                        BinId = binId,
                        BinUrl = binUrl,
                        Timestamp = DateTime.UtcNow,
                        ErrorMessage = $"Request Bin with Id '{binId}' is empty. Send your requests to {binUrl}.",
                        RequestHistoryItems = null
                    };
            }
            else
                return new HttpRequestBinHistory(Options.Value)
                {
                    BinId = binId,
                    BinUrl = binUrl,
                    Timestamp = DateTime.UtcNow,
                    ErrorMessage = errorMessage,
                    RequestHistoryItems = null
                };
        }

        /// <summary>
        /// Encodes string values on the HttpRequestBinHistory to Html so it can be rendered properly
        /// Sorts requests by Timestamp descending
        /// </summary>
        /// <param name="requestBinHistory"></param>
        /// <returns></returns>
        private HttpRequestBinHistory PrepareBinHistoryForHtml(HttpRequestBinHistory requestBinHistory)
        {
            if (requestBinHistory == null)
                return null;
            if (requestBinHistory.RequestHistoryItems == null)
                return requestBinHistory;

            var htmlEncodedRequestBinHistory = new HttpRequestBinHistory();
            htmlEncodedRequestBinHistory.BinId = HttpUtility.HtmlEncode(requestBinHistory.BinId);
            htmlEncodedRequestBinHistory.BinUrl = HttpUtility.HtmlEncode(requestBinHistory.BinUrl);
            htmlEncodedRequestBinHistory.Timestamp = requestBinHistory.Timestamp;
            htmlEncodedRequestBinHistory.ErrorMessage = HttpUtility.HtmlEncode(requestBinHistory.ErrorMessage);
            htmlEncodedRequestBinHistory.RequestHistoryItems = new List<HttpRequestDescription>();
            foreach (var request in requestBinHistory.RequestHistoryItems.OrderByDescending(i => i.Timestamp).ToList())
            {
                var htmlEncodedRequest = new HttpRequestDescription();
                htmlEncodedRequest.Body = HttpUtility.HtmlEncode(request.Body);
                htmlEncodedRequest.Method = HttpUtility.HtmlEncode(request.Method);
                htmlEncodedRequest.Path = HttpUtility.HtmlEncode(request.Path);
                htmlEncodedRequest.SourceIp = HttpUtility.HtmlEncode(request.SourceIp);
                htmlEncodedRequest.Timestamp = request.Timestamp;
                htmlEncodedRequest.QueryParams = new List<KeyValuePair<string, string>>();
                htmlEncodedRequest.Headers = new List<KeyValuePair<string, string>>();
                foreach (var queryParam in request.QueryParams)
                    htmlEncodedRequest.QueryParams.Add(new KeyValuePair<string, string>(HttpUtility.HtmlEncode(queryParam.Key), HttpUtility.HtmlEncode(queryParam.Value)));
                foreach (var header in request.Headers)
                    htmlEncodedRequest.Headers.Add(new KeyValuePair<string, string>(HttpUtility.HtmlEncode(header.Key), HttpUtility.HtmlEncode(header.Value)));
                htmlEncodedRequestBinHistory.RequestHistoryItems.Add(htmlEncodedRequest);
            }
            htmlEncodedRequestBinHistory.Settings = new HttpRequestBinHistory.HttpRequestBinSettings();
            htmlEncodedRequestBinHistory.Settings.RequestBinRenderer = requestBinHistory.Settings?.RequestBinRenderer;
            htmlEncodedRequestBinHistory.Settings.RequestBinMaxSize = requestBinHistory.Settings?.RequestBinMaxSize ?? 0;
            htmlEncodedRequestBinHistory.Settings.RequestBodyMaxLength = requestBinHistory.Settings?.RequestBodyMaxLength ?? 0;

            return htmlEncodedRequestBinHistory;
        }

        public string EncodeBinId(string binId)
        {
            return Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(binId));
        }
    }
}
