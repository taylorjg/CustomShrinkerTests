
## Introduction

This project demonstrates FsCheck property-based testing using a custom generator and
a custom shrinker in C#.   

## The Method Under Test

In order to demonstrate a custom generator and a custom shrinker, I have chosen to test a function that determines whether a given list is palindromic i.e. the list reads the same forwards and backwards. The
method under test has the following signature:

```C#
public static bool IsPalindromic<T>(IList<T> xs, bool introduceDeliberateBug)
```

The _introduceDeliberateBug_ parameter can be used to force the method under test to return the wrong result.
Specifically, when this parameter is true it returns false when the list has an odd length that is greater than one.        

## The Custom Generator

In order to test _IsPalindromic_, we need to be able to generate palindromic lists. I am using a custom generator 
for this purpose: 

```C#
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
```

_PalindromeGen_ generates randomly sized palindromic lists (50% with an odd length, 50% with an even length).
The elements in the list are generated by the parameter, _genT_.

## Failing Test With Shrinking

Our first attempt at a property test looks like this: 

```C#
[Test]
public void FailingTestWithShrinking()
{
    Spec
        .For(PalindromeGen(Any.OfType<int>()), xs => PalindromeChecker.IsPalindromic(xs, introduceDeliberateBug: true))
        .Check(Configuration);
}
```

When we run this test, we get output like this:

```
0:
seq [-5]
1:
seq [8; -2; 8]
shrink:
seq [-2; 8]
shrink:
seq [-2; 0]
shrink:
seq [2; 0]
shrink:
seq [1; 0]
System.Exception : Falsifiable, after 2 tests (4 shrinks) (StdGen (744174082,295970871)):

seq [1; 0]


   at <StartupCode$FsCheck>.$Runner.get_throwingRunner@349-1.Invoke(String message) in C:\Users\Kurt\Projects\FsCheck\fsharp\src\FsCheck\Runner.fs: line 349
   at FsCheck.Runner.check(Config config, a p) in C:\Users\Kurt\Projects\FsCheck\fsharp\src\FsCheck\Runner.fs: line 264
   at CustomShrinkerTests.PalindromeCheckerTests.FailingTestWithShrinking() in PalindromeCheckerTests.cs: line 29System.Exception : Falsifiable, after 2 tests (4 shrinks) (StdGen (744174082,295970871)):
```

The property has been falsified using the list <code>[8; -2; 8]</code> which has then been shrunk to the list <code>[1; 0]</code>.
However, the shrunk list is not palindromic! So the default shrinking behaviour for our list is no use to us. 

## Failing Test Without Shrinking

For our second attempt, we disable shrinking altogether: 

```C#
[Test]
public void FailingTestWithoutShrinking()
{
    Spec
        .For(PalindromeGen(Any.OfType<int>()), xs => PalindromeChecker.IsPalindromic(xs, introduceDeliberateBug: true))
        .Shrink(_ => Enumerable.Empty<IList<int>>())
        .Check(Configuration);
}
```

When we run this test, we get output like this:

```
0:
seq [2; 1; 1; 2]
1:
seq [0; 1; 1; 0]
2:
seq [0]
3:
seq [-4; -1; -5; -2; ...]
System.Exception : Falsifiable, after 4 tests (0 shrinks) (StdGen (786290520,295970872)):

seq [-4; -1; -5; -2; ...]


   at <StartupCode$FsCheck>.$Runner.get_throwingRunner@349-1.Invoke(String message) in C:\Users\Kurt\Projects\FsCheck\fsharp\src\FsCheck\Runner.fs: line 349
   at FsCheck.Runner.check(Config config, a p) in C:\Users\Kurt\Projects\FsCheck\fsharp\src\FsCheck\Runner.fs: line 264
   at CustomShrinkerTests.PalindromeCheckerTests.FailingTestWithoutShrinking() in PalindromeCheckerTests.cs: line 37
```

Not surprisingly, the list that falsified the property has not been shrunk at all. This makes it harder to
debug the problem. Ideally, we want the smallest valid input that demonstrates the problem.

## Failing Test With Custom Shrinking

For our third attempt, we add custom shrinking:

```C#
[Test]
public void FailingTestWithCustomShrinking()
{
    Spec
        .For(PalindromeGen(Any.OfType<int>()), xs => PalindromeChecker.IsPalindromic(xs, introduceDeliberateBug: true))
        .Shrink(PalindromeShrinker)
        .Check(Configuration);
}
```

Custom shrinking is implemented by _PalindromeShrinker_ which looks like this:

```C#
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
```

Our custom shrinker keeps yielding smaller and smaller versions of the original palindromic list.
Each time round the <code>for</code> loop, we yield either one or two smaller lists depending on whether the
list currently has an odd or even length respectively. For example, if the current list is <code>[1; 2; 3; 2; 1]</code>
then we will yield <code>[1; 2; 2; 1]</code>. If the current list is <code>[1; 2; 3; 3; 2; 1]</code> then
we will yield <code>[1; 2; 3; 2; 1]</code> followed by <code>[1; 2; 2; 1]</code>. Note that we make a copy of _value_
and repeatedly shrink the copy to prevent interfering with FsCheck's original value.  

When we run the test, we get the following ouput: 

```
0:
seq [0]
1:
seq []
2:
seq [0; 1; 1; 0]
3:
seq [0]
4:
seq [2; 1; -3; 2; ...]
shrink:
seq [2; 1; -3; 1; ...]
shrink:
seq [2; 1; 2]
System.Exception : Falsifiable, after 5 tests (2 shrinks) (StdGen (1972204082,295970871)):

seq [2; 1; 2]


   at <StartupCode$FsCheck>.$Runner.get_throwingRunner@349-1.Invoke(String message) in C:\Users\Kurt\Projects\FsCheck\fsharp\src\FsCheck\Runner.fs: line 349
   at FsCheck.Runner.check(Config config, a p) in C:\Users\Kurt\Projects\FsCheck\fsharp\src\FsCheck\Runner.fs: line 264
   at CustomShrinkerTests.PalindromeCheckerTests.FailingTestWithCustomShrinking() in PalindromeCheckerTests.cs: line 46
```

This time, the list that falsified the property has been shrunk to <code>[2; 1; 2]</code>. This is exactly what we want.
