using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using ServerlessRequestBin.DurableFunctions.Models;

namespace ServerlessRequestBin.DurableFunctions.Services
{
    public interface IRequestBinRenderer
    {
        string RenderToString(string binId, string binUrl, List<HttpRequestDescription> storedRequests = null, string errorMessage = null);
    }
}