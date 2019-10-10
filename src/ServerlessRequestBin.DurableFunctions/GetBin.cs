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
using ServerlessRequestBin.DurableFunctions.Models;

namespace ServerlessRequestBin.DurableFunctions
{
    public class GetBin
    {
        private readonly IRequestBinService RequestBinService;
        private readonly IRequestBinRenderer RequestBinRenderer;

        public GetBin(IRequestBinService requestBinService, IRequestBinRenderer requestBinRenderer)
        {
            RequestBinService = requestBinService;
            RequestBinRenderer = requestBinRenderer;
        }

        [FunctionName("GetBin")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous,
            "get",
            Route = "history/{binId}")] HttpRequest request,
            [DurableClient] IDurableClient client,
            string binId,
            ILogger log,
            ExecutionContext context)
        {
            try
            {
                log.LogInformation(new EventId(200), "{BinId}, {Message}", binId, $"A request to return request history for bin '{binId}' has been received.");
                if (!RequestBinService.IsBinIdValid(binId, out var validationMessage))
                {
                    log.LogError(new EventId(291), "{BinId}, {Message}", binId, $"Invalid Bin Id '{binId}'.");
                    return NewHtmlContentResult(HttpStatusCode.BadRequest,
                                            RequestBinRenderer.RenderToString(binId, "Invalid", null, validationMessage));
                }
                var binUrl = $"http{(request.IsHttps ? "s" : "")}://{request.Host}{request.Path.ToString().Replace("/history", "")}";
                var encodedBinId = RequestBinService.EncodeBinId(binId);
                var durableRequestBinId = new EntityId(nameof(RequestBin), encodedBinId);
                
                //Read the state of a Durable Entity
                var durableRequestBin = await client.ReadEntityStateAsync<RequestBin>(durableRequestBinId);
                var requestBinHistory = RequestBinRenderer.RenderToString(binId, binUrl, durableRequestBin.EntityState?.RequestHistoryItems);

                log.LogInformation(new EventId(210), "{BinId}, {Message}", binId, $"Request history for bin '{binId}' returned successfully.");
                return NewHtmlContentResult(HttpStatusCode.OK, requestBinHistory);
            }
            catch (Exception ex)
            {
                log.LogError(new EventId(290), ex, "{BinId}", binId, $"Error occurred trying to return the request history for bin: '{binId}'");
                try
                {
                    return NewHtmlContentResult(HttpStatusCode.InternalServerError,
                                            RequestBinRenderer.RenderToString(binId, "", null, $"500 Internal Server Error. Execution Id: '{context.InvocationId.ToString()}'"));
                }
                catch (Exception)
                {
                    //In case the custom Html render didn't work, return a message without format.
                    return NewHtmlContentResult(HttpStatusCode.InternalServerError,
                                            $"500 Internal Server Error. Execution Id: '{context.InvocationId.ToString()}'");
                }
            }
        }

        private static ContentResult NewHtmlContentResult(HttpStatusCode statusCode, string content)
        {
            return new ContentResult
            {
                ContentType = "text/html",
                StatusCode = (int)statusCode,
                Content = content
            };
        }

    }
}
