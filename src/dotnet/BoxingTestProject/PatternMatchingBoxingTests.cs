using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
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

  [Test]
  public void Generics()
  {
#if NETFRAMEWORK
    // unfortunately, generic type checks do introduce boxing in .NETFW
    Allocations.AssertAllocates(() => TestForInterface(42));
    Allocations.AssertAllocates(() => TestForInterface(new SomeStruct()));
    Allocations.AssertAllocates(() => TestForValueType(42));
    Allocations.AssertAllocates(() => TestForValueType(new SomeStruct()));
    Allocations.AssertAllocates(() => TestForRandomReferenceType(new SomeStruct()));
#else
    Allocations.AssertNoAllocations(() => TestForInterface(42));
    Allocations.AssertNoAllocations(() => TestForInterface(new SomeStruct()));
    Allocations.AssertNoAllocations(() => TestForValueType(42));
    Allocations.AssertNoAllocations(() => TestForValueType(new SomeStruct()));
    Allocations.AssertNoAllocations(() => TestForRandomReferenceType(new SomeStruct()));
#endif

    bool TestForInterface<TUnconstrained>(TUnconstrained t) => t is IConvertible;
    bool TestForValueType<TUnconstrained>(TUnconstrained t) => t is int;
    bool TestForRandomReferenceType<TUnconstrained>(TUnconstrained t) => t is StringBuilder;
  }

  private struct SomeStruct { }
  private enum SomeEnum { A }
}