using System.Collections.Generic;

namespace Continuation_Calculus.Utils
{
    public static class HashHelper
    {
        public const int Base = 17;

        public static int HashObject(this int hash, object obj)
        {
            unchecked { return hash * 23 + (obj == null ? 0 : obj.GetHashCode()); }
        }

        public static int HashValue<T>(this int hash, T value)
            where T : struct
        {
            unchecked { return hash * 23 + value.GetHashCode(); }
        }
        public static int HashEnumerable<T>(this int hash, IEnumerable<T> enumerable)
        {
            var r = hash * 23;
            foreach (var e in enumerable)
            {
                unchecked { r += hash * 23 + e.GetHashCode(); }
            }
            return r;
        }
    }
}
