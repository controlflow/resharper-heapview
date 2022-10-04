using System;
using System.Diagnostics.CodeAnalysis;
using NUnit.Framework;
#pragma warning disable CS8794
#pragma warning disable CS0183

namespace BoxingTestProject;

[TestFixture]
[SuppressMessage("ReSharper", "RedundantDiscardDesignation")]
[SuppressMessage("ReSharper", "PatternAlwaysMatches")]
[SuppressMessage("ReSharper", "ReplaceConditionalExpressionWithNullCoalescing")]
[SuppressMessage("ReSharper", "InlineTemporaryVariable")]
[SuppressMessage("ReSharper", "ConvertToConstant.Local")]
[SuppressMessage("ReSharper", "MergeAndPattern")]
public class PatternMatchingBoxingTests
{
  [Test]
  public void SimpleTypeTest()
  {
    var st = new SomeStruct();
    Allocations.AssertNoAllocations(() => st is ValueType);
    Allocations.AssertNoAllocations(() => st is ValueType _);
    Allocations.AssertNoAllocations(() => st is object);
    Allocations.AssertNoAllocations(() => st is object _);
    Allocations.AssertNoAllocations(() => st is var t ? t : default);

    Allocations.AssertAllocates(() => st is ValueType o ? o : null);
    Allocations.AssertAllocates(() => st is object o ? o : null);
    Allocations.AssertAllocates(() => st is object and var o ? o : null);

    var e = SomeEnum.A;
    Allocations.AssertNoAllocations(() => e is ValueType);
    Allocations.AssertNoAllocations(() => e is ValueType _);
    Allocations.AssertNoAllocations(() => e is Enum);
    Allocations.AssertNoAllocations(() => e is Enum _);
    Allocations.AssertNoAllocations(() => e is object);
    Allocations.AssertNoAllocations(() => e is object _);

    Allocations.AssertAllocates(() => e is ValueType o ? o : null);
    Allocations.AssertAllocates(() => e is Enum o ? o : null);
    Allocations.AssertAllocates(() => e is IComparable o ? o : null);
    Allocations.AssertAllocates(() => e is object o ? o : null);
  }

  [Test]
  public void BinaryPatterns()
  {
    var e = SomeEnum.A;
    Allocations.AssertAllocates(() => e is object and var o ? o : null);
    Allocations.AssertAllocates(() => e is _ and object and var o ? o : null);
  }

  private struct SomeStruct { }
  private enum SomeEnum { A }
}