using System;
using System.Linq;
using JetBrains.Annotations;
using NUnit.Framework;

namespace BoxingTestProject;

[PublicAPI]
public static class Allocations
{
  private const int IterationsCount = 500000000;

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
    for (var count = 0; count < IterationsCount; count++)
    {
      action();
    }

    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();

    var before = DumpAllocationsCount();
    var memoryBefore = GC.GetTotalMemory(false);

    for (var count = 0; count < IterationsCount; count++)
    {
      action();
    }

    var memoryAfter = GC.GetTotalMemory(false);
    var after = DumpAllocationsCount();

    //if (memoryBefore != memoryAfter)
    if (!before.SequenceEqual(after))
    {
      var bef = before.Aggregate("", (s, i) => s + "/" + i);
      var aft = after.Aggregate("", (s, i) => s + "/" + i);
      Assert.Fail($"Found allocations: {bef} -> {aft}, {memoryBefore} -> {memoryAfter}");
    }
  }

  public static void AssertAllocates(Action action)
  {
    for (var count = 0; count < IterationsCount; count++)
    {
      action();
    }

    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();

    var allocationsBefore = DumpAllocationsCount();
    var memoryBefore = GC.GetTotalMemory(false);

    for (var count = 0; count < IterationsCount; count++)
    {
      action();
    }

    var memoryAfter = GC.GetTotalMemory(false);
    var allocationsAfter = DumpAllocationsCount();

    //if (memoryAfter == memoryBefore)
    if (allocationsBefore.SequenceEqual(allocationsAfter))
    {
      var bef = allocationsBefore.Aggregate("", (s, i) => s + "/" + i);
      var aft = allocationsAfter.Aggregate("", (s, i) => s + "/" + i);
      Assert.Fail($"Found no allocations, {bef} -> {aft}, {memoryBefore} -> {memoryAfter}");
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