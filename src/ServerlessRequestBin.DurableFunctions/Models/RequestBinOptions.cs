using System;
using System.Collections.Generic;
using System.Text;

namespace ServerlessRequestBin.DurableFunctions.Models
{
    /// <summary>
    /// Class to implement the Options Pattern described here
    /// https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options?view=aspnetcore-2.2#reload-configuration-data-with-ioptionssnapshot
    /// And particularly on Azure Functions here
    /// https://docs.microsoft.com/en-us/azure/architecture/serverless/code
    /// </summary>
    public class RequestBinOptions
    {
        public string RequestBinRendererTemplate { get; set; } = "DarkHtmlRender.liquid";
        public int RequestBinMaxSize { get; set; } = 20;
        public int RequestBodyMaxLength { get; set; } = 128000;
    }
}
