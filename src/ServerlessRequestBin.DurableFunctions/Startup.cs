using System;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using ServerlessRequestBin.DurableFunctions.Services;
using ServerlessRequestBin.DurableFunctions.Models;
using Microsoft.Extensions.Configuration;

[assembly: FunctionsStartup(typeof(ServerlessRequestBin.DurableFunctions.Startup))]
namespace ServerlessRequestBin.DurableFunctions
{
    /// <summary>
    /// Implements the Options Pattern on Azure Functions here
    /// https://docs.microsoft.com/en-us/azure/architecture/serverless/code
    /// </summary>
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddOptions<RequestBinOptions>()
                .Configure<IConfiguration>((configSection, configuration) =>
                { configuration.Bind(configSection); });

            builder.Services.AddSingleton<IRequestBinService, RequestBinService>();
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("RequestBinRenderer")) ||
                Environment.GetEnvironmentVariable("RequestBinRenderer").ToLower() == "liquid")
            {
                //HtmlRequestBinRenderer is the default implementation
                builder.Services.AddSingleton<IRequestBinRenderer, LiquidRequestBinRenderer>();
            }
            else
            {
                throw new NotImplementedException($"RequestBinRenderer '{string.IsNullOrEmpty(Environment.GetEnvironmentVariable("RequestBinRenderer"))}' not implemented.");
            }

        }
    }
}
