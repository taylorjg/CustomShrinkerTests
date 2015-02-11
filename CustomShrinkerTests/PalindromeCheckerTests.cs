using System.Collections.Generic;
using System.Linq;
using FsCheck;
using FsCheck.Fluent;
using FsCheckUtils;
using NUnit.Framework;

// ReSharper disable RedundantArgumentNameForLiteralExpression

namespace CustomShrinkerTests
{
    [TestFixture]
    public class PalindromeCheckerTests
    {
        private static readonly Config Config = Config.VerboseThrowOnFailure.WithStartSize(20);
        private static readonly Configuration Configuration = Config.ToConfiguration();

        [Test]
        public void PassingTest()
        {
            Spec
                .For(PalindromeGen(Any.OfType<int>()), xs => PalindromeChecker.IsPalindromic(xs, introduceDeliberateBug: false))
                .Check(Configuration);
        }

        [Test]
        public void FailingTestWithShrinking()
        {
            Spec
                .For(PalindromeGen(Any.OfType<int>()), xs => PalindromeChecker.IsPalindromic(xs, introduceDeliberateBug: true))
                .Check(Configuration);
        }

        [Test]
        public void FailingTestWithoutShrinking()
        {
            Spec
                .For(PalindromeGen(Any.OfType<int>()), xs => PalindromeChecker.IsPalindromic(xs, introduceDeliberateBug: true))
                .Shrink(_ => Enumerable.Empty<IList<int>>())
                .Check(Configuration);
        }

        [Test]
        public void FailingTestWithCustomShrinking()
        {
            Spec
                .For(PalindromeGen(Any.OfType<int>()), xs => PalindromeChecker.IsPalindromic(xs, introduceDeliberateBug: true))
                .Shrink(PalindromeShrinker)
                .Check(Configuration);
        }

        private static IEnumerable<IList<T>> PalindromeShrinker<T>(IList<T> value)
        {
            var copyOfValue = new List<T>(value);

            for (;;)
            {
                var hasOddLength = copyOfValue.Count%2 == 1;
                for (var i = 0; i < (hasOddLength ? 1 : 2); i++)
                {
                    if (copyOfValue.Count == 0) yield break;
                    copyOfValue.RemoveAt((copyOfValue.Count - 1)/2);
                    yield return copyOfValue;
                }
            }
        }

        private static Gen<IList<T>> PalindromeGen<T>(Gen<T> genT)
        {
            return Any.WeighedGeneratorIn(
                new WeightAndValue<Gen<IList<T>>>(50, EvenLengthPalindromeGen(genT)),
                new WeightAndValue<Gen<IList<T>>>(50, OddLengthPalindromeGen(genT)));
        }

        private static Gen<IList<T>> EvenLengthPalindromeGen<T>(Gen<T> genT)
        {
            return
                from xs in genT.MakeList()
                select xs.Concat(xs.AsEnumerable().Reverse()).ToList() as IList<T>;
        }

        private static Gen<IList<T>> OddLengthPalindromeGen<T>(Gen<T> genT)
        {
            return
                from xs in genT.MakeList()
                from x in genT
                select xs.Concat(new[] {x}).Concat(xs.AsEnumerable().Reverse()).ToList() as IList<T>;
        }
    }
}
