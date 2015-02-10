using System.Collections.Generic;
using System.Linq;

namespace CustomShrinkerTests
{
    public static class PalindromeChecker
    {
        public static bool IsPalindrome<T>(IList<T> xs, bool introduceDeliberateBug)
        {
            if (xs.Count <= 1) return true;

            var defaultEqualityComparer = EqualityComparer<T>.Default;

            if (introduceDeliberateBug && xs.Count % 2 == 1)
            {
                if (defaultEqualityComparer.Equals(xs.Skip(1).First(), xs.Last())) return true;
            }
            else
            {
                if (defaultEqualityComparer.Equals(xs.First(), xs.Last())) return true;
            }

            return false;
        }
    }
}
