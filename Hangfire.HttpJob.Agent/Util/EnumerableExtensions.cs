using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Hangfire.HttpJob.Agent.Util
{
    /// <summary>
    /// Provides a set of extension methods to enumerate collections with progress.
    /// </summary>
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Returns an <see cref="IEnumerable{T}"/> reporting enumeration progress.
        /// </summary>
        /// <typeparam name="T">Item type</typeparam>
        /// <param name="enumerable">Source enumerable</param>
        /// <param name="progressBar">Progress bar</param>
        /// <param name="count">Item count</param>
        public static IEnumerable<T> WithProgress<T>(this IEnumerable<T> enumerable, IProgressBar progressBar, int count = -1)
        {
            if (enumerable is ICollection<T> collection)
            {
                count = collection.Count;
            }
            else if (enumerable is IReadOnlyCollection<T> readOnlyCollection)
            {
                count = readOnlyCollection.Count;
            }
            else if (count < 0)
            {
                throw new ArgumentException("Count is required when enumerable is not a collection", nameof(count));
            }

            return new ProgressEnumerable<T>(enumerable, progressBar, count);
        }

        /// <summary>
        /// Returns an <see cref="IEnumerable"/> reporting enumeration progress.
        /// </summary>
        /// <param name="enumerable">Source enumerable</param>
        /// <param name="progressBar">Progress bar</param>
        /// <param name="count">Item count</param>
        public static IEnumerable WithProgress(this IEnumerable enumerable, IProgressBar progressBar, int count = -1)
        {
            if (enumerable is ICollection collection)
            {
                count = collection.Count;
            }
            else if (count < 0)
            {
                throw new ArgumentException("Count is required when enumerable is not a collection", nameof(count));
            }

            return new ProgressEnumerable(enumerable, progressBar, count);
        }

        /// <summary>
        /// Returns an <see cref="IEnumerable{T}"/> reporting enumeration progress.
        /// </summary>
        /// <typeparam name="T">Item type</typeparam>
        /// <param name="enumerable">Source enumerable</param>
        /// <param name="context">Perform context</param>
        /// <param name="color">Progress bar color</param>
        /// <param name="count">Item count</param>
        public static IEnumerable<T> WithProgress<T>(this IEnumerable<T> enumerable, IHangfireConsole context, ConsoleFontColor color = null, int count = -1)
        {
            return WithProgress(enumerable, context.WriteProgressBar("ProgressBar", 0, color), count);
        }

        /// <summary>
        /// Returns ab <see cref="IEnumerable"/> reporting enumeration progress.
        /// </summary>
        /// <param name="enumerable">Source enumerable</param>
        /// <param name="context">Perform context</param>
        /// <param name="color">Progress bar color</param>
        /// <param name="count">Item count</param>
        public static IEnumerable WithProgress(this IEnumerable enumerable, IHangfireConsole context, ConsoleFontColor color = null, int count = -1)
        {
            return WithProgress(enumerable, context.WriteProgressBar("ProgressBar", 0, color), count);
        }

        /// <summary>
        /// Returns an <see cref="IEnumerable{T}"/> reporting enumeration progress.
        /// </summary>
        /// <typeparam name="T">Item type</typeparam>
        /// <param name="enumerable">Source enumerable</param>
        /// <param name="context">Perform context</param>
        /// <param name="name">Progress bar name</param>
        /// <param name="color">Progress bar color</param>
        /// <param name="count">Item count</param>
        public static IEnumerable<T> WithProgress<T>(this IEnumerable<T> enumerable, IHangfireConsole context, string name, ConsoleFontColor color = null, int count = -1)
        {
            return WithProgress(enumerable, context.WriteProgressBar(name, 0, color), count);
        }

        /// <summary>
        /// Returns ab <see cref="IEnumerable"/> reporting enumeration progress.
        /// </summary>
        /// <param name="enumerable">Source enumerable</param>
        /// <param name="context">Perform context</param>
        /// <param name="name">Progress bar name</param>
        /// <param name="color">Progress bar color</param>
        /// <param name="count">Item count</param>
        public static IEnumerable WithProgress(this IEnumerable enumerable, IHangfireConsole context, string name, ConsoleFontColor color = null, int count = -1)
        {
            return WithProgress(enumerable, context.WriteProgressBar(name, 0, color), count);
        }
    }
}
