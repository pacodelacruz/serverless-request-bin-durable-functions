using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerlessRequestBin.DurableFunctions.Models
{
    [JsonObject(MemberSerialization.OptOut)]
    public class HttpRequestBinHistory
    {

        public string BinId;
        public string BinUrl;
        public string ErrorMessage;
        public DateTime Timestamp;
        public IList<HttpRequestDescription> RequestHistoryItems;
        public HttpRequestBinSettings Settings;

        public HttpRequestBinHistory(RequestBinOptions options)
        {
            Settings = new HttpRequestBinSettings();
            Settings.RequestBinMaxSize = options.RequestBinMaxSize;
            Settings.RequestBodyMaxLength = options.RequestBodyMaxLength;
        }

        public HttpRequestBinHistory()
        {
            
        }

        public class HttpRequestBinSettings
        {
            public string RequestBinRenderer;
            public int RequestBinMaxSize;
            public int RequestBodyMaxLength;
        }
    }
}
