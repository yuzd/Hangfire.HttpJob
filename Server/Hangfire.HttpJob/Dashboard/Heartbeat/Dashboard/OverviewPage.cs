using System;
using Hangfire.Dashboard;
using Hangfire.Dashboard.Pages;

namespace Hangfire.Heartbeat.Dashboard
{
    internal sealed class OverviewPage : RazorPage
    {
        private readonly HeartbeatDashboardOptions _dashboardOptions;
        public const string Title = "Heartbeat";
        public const string PageRoute = "/heartbeat";
        public const string StatsRoute = "/heartbeat/stats";

        private static readonly string PageHtml;

        static OverviewPage()
        {
            PageHtml = Utils.ReadStringResource("Hangfire.HttpJob.Dashboard.Heartbeat.Dashboard.html.OverviewPage.html");
        }

        public OverviewPage(HeartbeatDashboardOptions dashboardOptions)
        {
            _dashboardOptions = dashboardOptions ?? throw new ArgumentNullException(nameof(dashboardOptions));
        }

        public override void Execute()
        {
            WriteEmptyLine();
            Layout = new LayoutPage(Title);
            WriteLiteralLine(PageHtml);
            WriteEmptyLine();
        }

        private void WriteLiteralLine(string textToAppend)
        {
            WriteLiteral(textToAppend);
            WriteConfig();
            WriteLiteral("\r\n");
        }

        private void WriteConfig()
        {
            WriteLiteral($@"<div id='heartbeatConfig' 
data-pollinterval='{(int)_dashboardOptions.CheckInterval.TotalMilliseconds}'
data-pollurl='{Url.To(StatsRoute)}'");
        }

        private void WriteEmptyLine()
        {
            WriteLiteral("\r\n");
        }
    }
}
