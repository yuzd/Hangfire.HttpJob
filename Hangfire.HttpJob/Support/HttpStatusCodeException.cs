using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Hangfire.HttpJob.Content.resx;

namespace Hangfire.HttpJob.Support
{
    public class HttpStatusCodeException:Exception
    {
        public string Msg  { get; set; }
        public HttpStatusCodeException(HttpStatusCode code,string data):base($"{Strings.ResponseCode}:{code} ===> CheckResult: Fail ")
        {
            Msg = data;
        }
    }
    
    public class CallbackJobException:Exception
    {
        public CallbackJobException(string code):base($"{Strings.CallbackFail} ===> {code} ")
        {
            
        }
    }
}
