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
    public class LiquidRequestBinRenderer : IRequestBinRenderer
    {
        private readonly IOptions<RequestBinOptions> Options;
        private static Template LiquidTemplate;

        public LiquidRequestBinRenderer(IOptions<RequestBinOptions> options)
        {
            Options = options;
            // Read and load the Liquid Template from an embedded resource
            using (var reader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream(
                $"{Assembly.GetExecutingAssembly().GetName().Name}.Resources.{options.Value.RequestBinRendererTemplate}")))
            {
                LiquidTemplate = Template.Parse(reader.ReadToEnd());
            }
        }

               
        public string RenderToString(string binId, string binUrl, List<HttpRequestDescription> storedRequests = null, string errorMessage = null)
        {
            var requestBinHistory = PrepareBinHistoryForHtml(EnrichRequestBinHistory(binId, binUrl, storedRequests, errorMessage));
            return LiquidTemplate.Render(Hash.FromDictionary(requestBinHistory.ToDictionary()));
        }

        private HttpRequestBinHistory EnrichRequestBinHistory(string binId, string binUrl, List<HttpRequestDescription> storedRequests, string errorMessage = null)
        {
            if (string.IsNullOrWhiteSpace(errorMessage))
            {
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
    }
}
