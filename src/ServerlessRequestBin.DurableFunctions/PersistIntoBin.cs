// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ServerlessRequestBin.DurableFunctions.Services;
using System.Text;

namespace ServerlessRequestBin.DurableFunctions
{
    public class PersistIntoBin
    {
        private readonly IRequestBinService RequestBinService;

        public PersistIntoBin(IRequestBinService requestBinService)
        {
            RequestBinService = requestBinService;
        }

        [FunctionName("PersistIntoBin")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous,
            "get", "post", "put", "patch", "delete", "head", "options", "trace",
            Route = "{binId}")] HttpRequest request,
            [DurableClient] IDurableClient client,
            string binId,
            ILogger log)
        {
            try
            {
                log.LogInformation(new EventId(100), "{BinId}, {Message}", binId, $"Request received for bin '{binId}'.");
                if (!RequestBinService.IsBinIdValid(binId, out var validationMessage))
                {
                    log.LogError(new EventId(191), "{BinId}, {Message}", binId, $"Invalid Bin Id '{binId}'.");
                    return new BadRequestObjectResult(validationMessage);
                }
                var requestDescription = await RequestBinService.GetRequestDescriptionAsync(request);
                var encodedBinId = RequestBinService.EncodeBinId(binId);

                //Send a one-way message to an entity (via a queue) using a proxy object for type-safe calls. 
                await client.SignalEntityAsync<IRequestBin>(encodedBinId, x => x.Add(requestDescription));

                log.LogInformation(new EventId(110), "{BinId}, {Message}", binId, $"Request for bin '{binId}' stored.");
                return new OkResult();
            }
            catch (Exception ex)
            {
                log.LogError(new EventId(190), ex, "{BinId}, {Message}", binId, $"Error occurred while trying to persist request into bin: '{binId}'.");
                throw;
            }
        }
    }
}
