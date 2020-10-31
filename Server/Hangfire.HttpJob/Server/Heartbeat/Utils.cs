using System.IO;
using System.Resources;

namespace Hangfire.Heartbeat
{
    internal static class Utils
    {
        public static string ReadStringResource(string resourceName)
        {
            var assembly = typeof(Utils).Assembly;
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null) throw new MissingManifestResourceException($"Cannot find resource {resourceName}");

                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        public static string FormatKey(string serverId) => "utilization:" + serverId;
    }
}
