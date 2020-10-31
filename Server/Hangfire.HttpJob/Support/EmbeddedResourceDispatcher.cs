using Hangfire.Dashboard;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Hangfire.HttpJob.Support
{
    /// <summary>
    /// Alternative to built-in EmbeddedResourceDispatcher, which (for some reasons) is not public.
    /// </summary>
    internal class EmbeddedResourceDispatcher : IDashboardDispatcher
    {
        private readonly Assembly _assembly;
        private readonly string _resourceName;
        private readonly string _contentType;

        public EmbeddedResourceDispatcher(Assembly assembly, string resourceName, string contentType = null)
        {
            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly));
            if (string.IsNullOrEmpty(resourceName))
                throw new ArgumentNullException(nameof(resourceName));

            _assembly = assembly;
            _resourceName = resourceName;
            _contentType = contentType;
        }

        public async Task Dispatch(DashboardContext context)
        {
            if (!string.IsNullOrEmpty(_contentType))
            {
                var contentType = context.Response.ContentType;

                if (string.IsNullOrEmpty(contentType))
                {
                    // content type not yet set
                    context.Response.ContentType = _contentType;
                }
                else if (contentType != _contentType)
                {
                    // content type already set, but doesn't match ours
                    throw new InvalidOperationException($"ContentType '{_contentType}' conflicts with '{context.Response.ContentType}'");
                }
            }

            await WriteResourceAsync(context.Response, _assembly, _resourceName);
        }

        private static async Task WriteResourceAsync(DashboardResponse response, Assembly assembly, string resourceName)
        {
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                    throw new ArgumentException($@"Resource '{resourceName}' not found in assembly {assembly}.");

                await stream.CopyToAsync(response.Body);
            }
        }
    }
}
