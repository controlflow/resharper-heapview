using System.Diagnostics.CodeAnalysis;
using NUnit.Framework;

namespace BoxingTestProject;

[TestFixture]
[SuppressMessage("Reliability", "CA2013:Do not use ReferenceEquals with value types")]
public class NullChecksBoxingTests
{
  [Test]
  public void ObjectEquals()
  {
    var st = new SomeStruct();
    Allocations.AssertAllocates(() => Equals(st, null));
  }

  [Test]
  [SuppressMessage("ReSharper", "ReferenceEqualsWithValueType")]
  public void ReferenceEquals()
  {
    var st = new SomeStruct();
#if DEBUG
    Allocations.AssertAllocates(() => ReferenceEquals(st, null));
#else
    Allocations.AssertNoAllocations(() => ReferenceEquals(st, null));
#endif
  }

  [Test]
  public void GenericReferenceEquals()
  {
    var st = new SomeStruct();
#if DEBUG
    Allocations.AssertAllocates(() => Generic(st));
#else
    Allocations.AssertNoAllocations(() => Generic(st));
#endif
    static bool Generic<T>(T unconstrained) => ReferenceEquals(unconstrained, null);
  }

  [Test]
  public void GenericEqualsNull()
  {
    var st = new SomeStruct();
#if DEBUG
    Allocations.AssertAllocates(() => Generic(st));
#else
    Allocations.AssertNoAllocations(() => Generic(st));
#endif
    static bool Generic<T>(T unconstrained) => unconstrained == null;
  }

  [Test]
  public void GenericNotEqualsNull()
  {
    var st = new SomeStruct();
#if DEBUG
    Allocations.AssertAllocates(() => Generic(st));
#else
    Allocations.AssertNoAllocations(() => Generic(st));
#endif
    static bool Generic<T>(T unconstrained) => unconstrained != null;
  }

  [Test]
  public void GenericIsNullPattern()
  {
    var st = new SomeStruct();
#if DEBUG
    Allocations.AssertAllocates(() => Generic(st));
#else
    Allocations.AssertNoAllocations(() => Generic(st));
#endif
    static bool Generic<T>(T unconstrained) => unconstrained is null;
  }

  [Test]
  public void GenericIsNotNullPattern()
  {
    var st = new SomeStruct();
#if DEBUG
    Allocations.AssertAllocates(() => Generic(st));
#else
    Allocations.AssertNoAllocations(() => Generic(st));
#endif
    static bool Generic<T>(T unconstrained) => unconstrained is not null;
  }

  [Test]
  public void CastAndNullCheck()
  {
    var st = new SomeStruct();
#if DEBUG
    Allocations.AssertAllocates(() => Generic(st));
#else
    Allocations.AssertNoAllocations(() => Generic(st));
#endif
    // ReSharper disable once RedundantCast
    static bool Generic<T>(T unconstrained) => (object?) unconstrained == null;
  }

  private struct SomeStruct { }
}