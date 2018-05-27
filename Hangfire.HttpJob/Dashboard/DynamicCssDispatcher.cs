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

            builder.AppendLine(".console, .console .line-buffer {")
                .Append("    color: ").Append("red").AppendLine(";")
                .AppendLine("}");

            return context.Response.WriteAsync(builder.ToString());
        }
    }
}
