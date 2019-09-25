using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using ServerlessRequestBin.DurableFunctions.Models;

namespace ServerlessRequestBin.DurableFunctions
{
    public interface IRequestBin
    {
        void Add(HttpRequestDescription requestDescription);
        
        void Empty();
    }
}
