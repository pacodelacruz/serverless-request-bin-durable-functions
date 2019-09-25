using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using ServerlessRequestBin.DurableFunctions.Models;

namespace ServerlessRequestBin.DurableFunctions
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class RequestBin : IRequestBin
    {
        private readonly IOptions<RequestBinOptions> Options;

        public RequestBin(IOptions<RequestBinOptions> options)
        {
            Options = options;
        }

        [JsonProperty]
        public List<HttpRequestDescription> RequestHistoryItems { get; set; } = new List<HttpRequestDescription>();

        public void Add(HttpRequestDescription requestDescription)
        {
            RequestHistoryItems.Add(requestDescription);
            if (RequestHistoryItems.Count > Options.Value.RequestBinMaxSize)
                RequestHistoryItems = RequestHistoryItems.Skip(1).ToList();
        }

        public void Empty()
        {
            RequestHistoryItems = new List<HttpRequestDescription>();
        }

        /// <summary>
        /// Entry point for the Durable Entity
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        [FunctionName(nameof(RequestBin))]
        public static Task HandleEntityOperation([EntityTrigger] IDurableEntityContext context)
        {
            return context.DispatchAsync<RequestBin>();
        }
    }
}