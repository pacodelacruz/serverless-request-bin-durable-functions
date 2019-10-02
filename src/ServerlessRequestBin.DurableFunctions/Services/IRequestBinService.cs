using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using ServerlessRequestBin.DurableFunctions.Models;

namespace ServerlessRequestBin.DurableFunctions.Services
{
    public interface IRequestBinService
    {
        bool IsBinIdValid(string binId, out string validationMessage);
        string EncodeBinId(string binId);
        Task<HttpRequestDescription> GetRequestDescriptionAsync(HttpRequest request);
    }
}