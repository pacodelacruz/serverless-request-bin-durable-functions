using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using ServerlessRequestBin.DurableFunctions.Models;

namespace ServerlessRequestBin.DurableFunctions.Services
{
    public interface IRequestBinService
    {
        HttpRequestBinHistory GetRequestBinHistory(string binId, string binUrl, List<HttpRequestDescription> storedRequests, string errorMessage = null);
        Task<HttpRequestDescription> GetRequestDescriptionAsync(HttpRequest request);
        bool IsBinIdValid(string binId, out string validationMessage);
        string RenderToString(string binId, string binUrl, List<HttpRequestDescription> storedRequests = null, string errorMessage = null);
        string EncodeBinId(string binId);
    }
}