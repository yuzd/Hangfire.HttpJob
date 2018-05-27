using Hangfire.Dashboard;
using System;
using System.Reflection;

namespace Hangfire.HttpJob.Support
{
    /// <summary>
    /// Provides extension methods for <see cref="HtmlHelper"/>.
    /// </summary>
    internal static class HtmlHelperExtensions
    {
        private static readonly FieldInfo _page = typeof(HtmlHelper).GetTypeInfo().GetDeclaredField(nameof(_page));

        /// <summary>
        /// Returs a <see cref="RazorPage"/> associated with <see cref="HtmlHelper"/>.
        /// </summary>
        /// <param name="helper">Helper</param>
        public static RazorPage GetPage(this HtmlHelper helper)
        {
            if (helper == null)
                throw new ArgumentNullException(nameof(helper));

            return (RazorPage)_page.GetValue(helper);
        }
    }
}
