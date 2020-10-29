using Hangfire.Dashboard;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Hangfire.HttpJob.Dashboard
{
    public class DynamicCssDispatcher : IDashboardDispatcher
    {
        private readonly HangfireHttpJobOptions _options;
        public DynamicCssDispatcher(HangfireHttpJobOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            _options = options;
        }

        public Task Dispatch(DashboardContext context)
        {
            var builder = new StringBuilder();

            builder.AppendLine(".table tbody .tooltip{word-break: break-all;}");

            builder.AppendLine(".console, .console .line-buffer {")
                .Append("    color: ").Append("red").AppendLine(";")
                .AppendLine("}");

            if (context.Request != null)
            {
                if (context is AspNetCoreDashboardContext abc)
                {
                    if (abc.HttpContext.Request.Headers.TryGetValue("Referer", out var refer) && (refer.ToString().EndsWith("/processing") ||refer.ToString().EndsWith("/recurring")|| refer.ToString().EndsWith("/succeeded") || refer.ToString().EndsWith("/deleted")))
                    {
                        builder.AppendLine(".table tbody { display:none; }");
                    }
                }
            }

            return context.Response.WriteAsync(builder.ToString());
        }
    }
}
