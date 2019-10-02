using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ServerlessRequestBin.DurableFunctions.Services;

namespace ServerlessRequestBin.DurableFunctions
{
    public class EmptyBin
    {
        private readonly IRequestBinService RequestBinService;

        public EmptyBin(IRequestBinService requestBinService)
        {
            RequestBinService = requestBinService;
        }

        [FunctionName("EmptyBin")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous,
            "delete",
            Route = "history/{binId}")] HttpRequest request,
            [DurableClient] IDurableClient client,
            string binId,
            ILogger log)
        {
            try
            {
                log.LogInformation(new EventId(300), "{BinId}, {Message}", binId, $"A request to delete request history for bin '{binId}' has been received.");

                if (!RequestBinService.IsBinIdValid(binId, out var validationMessage))
                {
                    log.LogError(new EventId(391), "{BinId}, {Message}", binId, $"Invalid Bin Id '{binId}'.");
                    return new BadRequestObjectResult(validationMessage);
                }

                var encodedBinId = RequestBinService.EncodeBinId(binId);
                
                //Send a one-way message to an entity (via a queue) using a proxy object for type-safe calls. 
                await client.SignalEntityAsync<IRequestBin>(encodedBinId, x => x.Empty());

                log.LogInformation(new EventId(310), "{BinId}, {Message}", binId, $"Request history for bin '{binId}' has been deleted.");
                return new OkResult();
            }
            catch (Exception ex)
            {
                log.LogError(new EventId(390), ex, "{BinId}", binId, $"Error occurred while trying to delete request history for bin: '{binId}'");
                throw;
            }
        }
    }
}
