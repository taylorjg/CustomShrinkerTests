using System.Collections.Generic;
using System.Linq;

namespace CustomShrinkerTests
{
    public static class PalindromeChecker
    {
        public static bool IsPalindromic<T>(IList<T> xs, bool introduceDeliberateBug)
        {
            if (xs.Count <= 1) return true;

            if (introduceDeliberateBug && xs.Count % 2 == 1) return false;

            var defaultEqualityComparer = EqualityComparer<T>.Default;

            return
                defaultEqualityComparer.Equals(xs.First(), xs.Last()) &&
                IsPalindromic(xs.Skip(1).Take(xs.Count - 2).ToList(), introduceDeliberateBug);
        }
    }
}
