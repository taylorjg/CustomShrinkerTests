﻿using System.Collections.Generic;
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
                .For(PalindromeGen(Any.OfType<int>()), xs => PalindromeChecker.IsPalindrome(xs, introduceDeliberateBug: false))
                .Shrink(_ => Enumerable.Empty<IList<int>>())
                .Check(Configuration);
        }

        [Test]
        public void FailingTestWithShrinking()
        {
            Spec
                .For(PalindromeGen(Any.OfType<int>()), xs => PalindromeChecker.IsPalindrome(xs, introduceDeliberateBug: true))
                .Check(Configuration);
        }

        [Test]
        public void FailingTestWithoutShrinking()
        {
            Spec
                .For(PalindromeGen(Any.OfType<int>()), xs => PalindromeChecker.IsPalindrome(xs, introduceDeliberateBug: true))
                .Shrink(_ => Enumerable.Empty<IList<int>>())
                .Check(Configuration);
        }

        [Test]
        public void FailingTestWithCustomShrinking()
        {
            Spec
                .For(PalindromeGen(Any.OfType<int>()), xs => PalindromeChecker.IsPalindrome(xs, introduceDeliberateBug: true))
                .Shrink(PalindromeShrinker)
                .Check(Configuration);
        }

        private static IEnumerable<IList<int>> PalindromeShrinker(IList<int> value)
        {
            var copyOfValue = new List<int>(value);

            for (;;)
            {
                if (copyOfValue.Count == 0) yield break;
            
                if (copyOfValue.Count%2 == 1)
                {
                    var index = (copyOfValue.Count - 1) / 2;
                    copyOfValue.RemoveAt(index);
                    yield return copyOfValue;
                }
                else
                {
                    var index = (copyOfValue.Count - 2) / 2;

                    copyOfValue.RemoveAt(index);
                    yield return copyOfValue;

                    copyOfValue.RemoveAt(index);
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