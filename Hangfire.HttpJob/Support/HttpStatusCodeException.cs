using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Hangfire.HttpJob.Content.resx;

namespace Hangfire.HttpJob.Support
{
    public class HttpStatusCodeException:Exception
    {
        public HttpStatusCodeException(HttpStatusCode code):base($"{Strings.ResponseCode}:{code} ===> CheckResult: Fail ")
        {
            
        }
    }
    
    public class ChildJobException:Exception
    {
        public ChildJobException(string code):base($"ChildJob Fail ===> {code} ")
        {
            
        }
    }
}
