using Hangfire.Dashboard;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hangfire.HttpJob.Support
{
    /// <summary>
    /// Dispatcher that combines output from several other dispatchers.
    /// Used internally by <see cref="RouteCollectionExtensions.Append"/>.
    /// </summary>
    internal class CompositeDispatcher : IDashboardDispatcher
    {
        private readonly List<IDashboardDispatcher> _dispatchers;

        public CompositeDispatcher(params IDashboardDispatcher[] dispatchers)
        {
            _dispatchers = new List<IDashboardDispatcher>(dispatchers);
        }

        public void AddDispatcher(IDashboardDispatcher dispatcher)
        {
            if (dispatcher == null)
                throw new ArgumentNullException(nameof(dispatcher));

            _dispatchers.Add(dispatcher);
        }

        public async Task Dispatch(DashboardContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (_dispatchers.Count == 0)
                throw new InvalidOperationException("CompositeDispatcher should contain at least one dispatcher");

            foreach (var dispatcher in _dispatchers)
            {
                await dispatcher.Dispatch(context);
            }
        }
    }
}
