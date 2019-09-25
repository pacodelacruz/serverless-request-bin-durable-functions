using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerlessRequestBin.DurableFunctions.Models
{
    [JsonObject(MemberSerialization.OptOut)]
    public class HttpRequestDescription
    {
        public string Method;
        public string Path;
        public string SourceIp;
        public DateTime Timestamp;
        public List<KeyValuePair<string, string>> Headers;
        public List<KeyValuePair<string, string>> QueryParams;
        public string Body;
    }
}
