using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using ServerlessRequestBin.DurableFunctions.Models;

namespace ServerlessRequestBin.DurableFunctions
{
    /// <summary>
    /// Durable Entity Function Class
    /// </summary>
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class RequestBin : IRequestBin
    {
        private readonly IOptions<RequestBinOptions> Options;

        [JsonProperty]
        public List<HttpRequestDescription> RequestHistoryItems { get; set; } = new List<HttpRequestDescription>();

        public RequestBin(IOptions<RequestBinOptions> options)
        {
            Options = options;
        }

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
        [FunctionName(nameof(RequestBin))]
        public static Task HandleEntityOperation([EntityTrigger] IDurableEntityContext context)
        {
            return context.DispatchAsync<RequestBin>();
        }
    }
}