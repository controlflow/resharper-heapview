using System;
using System.Linq;
using JetBrains.Annotations;
using NUnit.Framework;

namespace BoxingTestProject;

public static class Allocations
{
  private const int IterationsCount = 10000000;

  public static void AssertNoAllocations<T>(Func<T> func)
  {
    AssertNoAllocations(() => { _ = func(); });
  }

  public static void AssertAllocates<T>(Func<T> func)
  {
    AssertAllocates(() => { _ = func(); });
  }

  public static void AssertNoAllocations(Action action)
  {
    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();

    var before = DumpAllocationsCount();

    for (var count = 0; count < IterationsCount; count++)
    {
      action();
    }

    var after = DumpAllocationsCount();

    if (!before.SequenceEqual(after))
    {
      var bef = before.Aggregate("", (s, i) => s + "/" + i);
      var aft = after.Aggregate("", (s, i) => s + "/" + i);
      Assert.Fail($"Found allocations: {bef} -> {aft}");
    }
  }

  public static void AssertAllocates(Action action)
  {
    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();

    var before = DumpAllocationsCount();

    for (var count = 0; count < IterationsCount; count++)
    {
      action();
    }

    var after = DumpAllocationsCount();

    if (before.SequenceEqual(after))
    {
      Assert.Fail("Found no allocations");
    }
  }

  [Pure]
  private static int[] DumpAllocationsCount()
  {
    var collections = new int[GC.MaxGeneration + 1];

    for (var gen = 0; gen <= GC.MaxGeneration; gen++)
    {
      collections[gen] = GC.CollectionCount(gen);
    }

    return collections;
  }
}