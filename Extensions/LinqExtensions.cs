using System;
using System.Collections.Generic;
using System.Linq;

namespace LiveSplit.Extensions
{
    public static class LinqExtensions
    {
        public static IEnumerable<T> TakeLast<T>(this IEnumerable<T> source, int N)
        {
            return source.Skip(Math.Max(0, source.Count() - N));
        }
    }
}
