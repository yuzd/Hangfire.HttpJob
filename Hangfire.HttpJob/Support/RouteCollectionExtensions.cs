using Hangfire.Dashboard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Hangfire.HttpJob.Support
{
    /// <summary>
    /// Provides extension methods for <see cref="RouteCollection"/>.
    /// </summary>
    internal static class RouteCollectionExtensions
    {
        private static readonly FieldInfo _dispatchers = typeof(RouteCollection).GetTypeInfo().GetDeclaredField(nameof(_dispatchers));

        /// <summary>
        /// Returns a private list of registered routes.
        /// </summary>
        /// <param name="routes">Route collection</param>
        private static List<Tuple<string, IDashboardDispatcher>> GetDispatchers(this RouteCollection routes)
        {
            if (routes == null)
                throw new ArgumentNullException(nameof(routes));

            return (List<Tuple<string, IDashboardDispatcher>>)_dispatchers.GetValue(routes);
        }

        /// <summary>
        /// Checks if there's a dispatcher registered for given <paramref name="pathTemplate"/>.
        /// </summary>
        /// <param name="routes">Route collection</param>
        /// <param name="pathTemplate">Path template</param>
        public static bool Contains(this RouteCollection routes, string pathTemplate)
        {
            if (routes == null)
                throw new ArgumentNullException(nameof(routes));
            if (pathTemplate == null)
                throw new ArgumentNullException(nameof(pathTemplate));

            return routes.GetDispatchers().Any(x => x.Item1 == pathTemplate);
        }

        /// <summary>
        /// Combines exising dispatcher for <paramref name="pathTemplate"/> with <paramref name="dispatcher"/>.
        /// If there's no dispatcher for the specified path, adds a new one.
        /// </summary>
        /// <param name="routes">Route collection</param>
        /// <param name="pathTemplate">Path template</param>
        /// <param name="dispatcher">Dispatcher to add or append for specified path</param>
        public static void Append(this RouteCollection routes, string pathTemplate, IDashboardDispatcher dispatcher)
        {
            if (routes == null)
                throw new ArgumentNullException(nameof(routes));
            if (pathTemplate == null)
                throw new ArgumentNullException(nameof(pathTemplate));
            if (dispatcher == null)
                throw new ArgumentNullException(nameof(dispatcher));

            var list = routes.GetDispatchers();

            for (int i = 0; i < list.Count; i++)
            {
                var pair = list[i];
                if (pair.Item1 == pathTemplate)
                {
                    var composite = pair.Item2 as CompositeDispatcher;
                    if (composite == null)
                    {
                        // replace original dispatcher with a composite one
                        composite = new CompositeDispatcher(pair.Item2);
                        list[i] = new Tuple<string, IDashboardDispatcher>(pathTemplate, composite);
                    }

                    composite.AddDispatcher(dispatcher);
                    return;
                }
            }

            routes.Add(pathTemplate, dispatcher);
        }

        /// <summary>
        /// Replaces exising dispatcher for <paramref name="pathTemplate"/> with <paramref name="dispatcher"/>.
        /// If there's no dispatcher for the specified path, adds a new one.
        /// </summary>
        /// <param name="routes">Route collection</param>
        /// <param name="pathTemplate">Path template</param>
        /// <param name="dispatcher">Dispatcher to set for specified path</param>
        public static void Replace(this RouteCollection routes, string pathTemplate, IDashboardDispatcher dispatcher)
        {
            if (routes == null)
                throw new ArgumentNullException(nameof(routes));
            if (pathTemplate == null)
                throw new ArgumentNullException(nameof(pathTemplate));
            if (dispatcher == null)
                throw new ArgumentNullException(nameof(dispatcher));

            var list = routes.GetDispatchers();

            for (int i = 0; i < list.Count; i++)
            {
                var pair = list[i];
                if (pair.Item1 == pathTemplate)
                {
                    list[i] = new Tuple<string, IDashboardDispatcher>(pathTemplate, dispatcher);
                    return;
                }
            }

            routes.Add(pathTemplate, dispatcher);
        }

        /// <summary>
        /// Removes dispatcher for <paramref name="pathTemplate"/>.
        /// </summary>
        /// <param name="routes">Route collection</param>
        /// <param name="pathTemplate">Path template</param>
        public static void Remove(this RouteCollection routes, string pathTemplate)
        {
            if (routes == null)
                throw new ArgumentNullException(nameof(routes));
            if (pathTemplate == null)
                throw new ArgumentNullException(nameof(pathTemplate));

            var list = routes.GetDispatchers();

            for (int i = 0; i < list.Count; i++)
            {
                var pair = list[i];
                if (pair.Item1 == pathTemplate)
                {
                    list.RemoveAt(i);
                    return;
                }
            }
        }
    }
}
