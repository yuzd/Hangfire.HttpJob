using System;
using Hangfire.Annotations;
using Hangfire.Dashboard;
using Hangfire.Heartbeat.Dashboard;

namespace Hangfire.Heartbeat
{
    public static class ConfigurationExtensions
    {
        [PublicAPI]
        public static IGlobalConfiguration UseHeartbeatPage(this IGlobalConfiguration config, TimeSpan? checkInterval = null)
        {
            return UseHeartbeatPage(config, new HeartbeatDashboardOptions(checkInterval??TimeSpan.FromSeconds(1)));
        }
          

        [PublicAPI]
        public static IGlobalConfiguration UseHeartbeatPage(this IGlobalConfiguration config, HeartbeatDashboardOptions heartbeatDashboardOptions)
        {
            DashboardRoutes.Routes.AddRazorPage(OverviewPage.PageRoute, x => new OverviewPage(heartbeatDashboardOptions));
            NavigationMenu.Items.Add(page => new MenuItem(OverviewPage.Title, page.Url.To(OverviewPage.PageRoute))
            {
                Active = page.RequestPath.StartsWith(OverviewPage.PageRoute)
            });
            DashboardRoutes.Routes.Add(OverviewPage.StatsRoute, new UtilizationJsonDispatcher());

            DashboardRoutes.Routes.Add(
                "/heartbeat/jsknockout",
                new ContentDispatcher("application/javascript", "Hangfire.HttpJob.Dashboard.Heartbeat.Dashboard.js.knockout-3.4.2.js",
                    TimeSpan.FromDays(30)));

            DashboardRoutes.Routes.Add(
                "/heartbeat/jsknockoutorderable",
                new ContentDispatcher("application/javascript", "Hangfire.HttpJob.Dashboard.Heartbeat.Dashboard.js.knockout.bindings.orderable.js",
                    TimeSpan.FromDays(30)));

            DashboardRoutes.Routes.Add(
                "/heartbeat/jsnumeral",
                new ContentDispatcher("application/javascript", "Hangfire.HttpJob.Dashboard.Heartbeat.Dashboard.js.numeral.min.js", TimeSpan.FromDays(30)));

            DashboardRoutes.Routes.Add(
                "/heartbeat/jspage",
                new ContentDispatcher("application/javascript", "Hangfire.HttpJob.Dashboard.Heartbeat.Dashboard.js.OverviewPage.js", TimeSpan.FromSeconds(1)));

            DashboardRoutes.Routes.Add(
                "/heartbeat/cssstyles",
                new ContentDispatcher("text/css", "Hangfire.HttpJob.Dashboard.Heartbeat.Dashboard.css.styles.css", TimeSpan.FromSeconds(1)));

            return config;
        }
    }
}
