using Newtonsoft.Json;
using ServerlessRequestBin.DurableFunctions.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerlessRequestBin.DurableFunctions.Extensions
{
    public static class ObjectExtensions
    {
        /// <summary>
        /// Object extension to deserialise an object into a Dictionary of string and objects throughout the object hierarchy
        /// It could be optimised using reflection.
        /// </summary>
        public static IDictionary<string, object> ToDictionary(this object source)
        {
            return JsonConvert.DeserializeObject<IDictionary<string, object>>(JsonConvert.SerializeObject(source), new JsonDictionaryConverter());
        }
    }
}
