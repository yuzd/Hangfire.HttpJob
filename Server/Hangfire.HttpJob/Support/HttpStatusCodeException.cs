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
        public bool IsEl  { get; set; }
        public string El  { get; set; }
        public HttpStatusCodeException(HttpStatusCode code,string data):base($"{Strings.ResponseCode}:{code} ===> CheckResult: Fail ")
        {
            Msg = data;
        }

        public HttpStatusCodeException(string el, string data) : base($"{Strings.CallbackELExcuteError}:{el} ===> CheckResult: Fail ")
        {
            IsEl = true;
            El = el;
            Msg = data;
        }
    }
    
    public class CallbackJobException:Exception
    {
        public CallbackJobException(string code):base($"{Strings.CallbackFail} ===> {code} ")
        {
            
        }
    }

    public class AgentJobException : Exception
    {
        public AgentJobException(string agentClass,string err) : base($"AgentClass:"+ agentClass + "=>" + err)
        {

        }
    }

    public class HangfireServerShutDownError : Exception
    {
        public HangfireServerShutDownError():this("hangfire server was shut down!")
        {
            
        }
        public HangfireServerShutDownError(string msg):base(msg)
        {
            
        }
    }

    public class HangfireAgentShutDownError : Exception
    {
        public HangfireAgentShutDownError() : this("hangfire agent was shut down!")
        {

        }
        public HangfireAgentShutDownError(string msg) : base(msg)
        {

        }
    }
}
