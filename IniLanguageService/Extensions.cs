using Microsoft.VisualStudio.Text;
using System.Collections.Generic;
using System.Linq;

namespace IniLanguageService
{
    internal static class Extensions
    {
        public static IEnumerable<T> SwitchToIfEmpty<T>(this IEnumerable<T> source, IEnumerable<T> second)
        {
            return source.Any()
                ? source
                : second
            ;
        }

        public static bool ContainsOrEndsWith(this SnapshotSpan span, SnapshotPoint point)
        {
            return span.Contains(point) || span.End == point;
        }
    }
}
