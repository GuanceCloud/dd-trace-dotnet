using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.RabbitMQ
{
    internal static class ContextPropagation
    {
#pragma warning disable SA1401 // Fields must be private
        public static Action<IDictionary<string, object>, string, string> HeadersSetter = (carrier, key, value) =>
        {
            carrier[key] = Encoding.UTF8.GetBytes(value);
        };

        public static Func<IDictionary<string, object>, string, IEnumerable<string>> HeadersGetter = ((carrier, key) =>
        {
            if (carrier.TryGetValue(key, out object value) && value is byte[] bytes)
            {
                return new[] { Encoding.UTF8.GetString(bytes) };
            }
            else
            {
                return Enumerable.Empty<string>();
            }
        });
    }
}
