using System;
using NUnit.Framework;

namespace BoxingTestProject;

[TestFixture]
public class StructVirtualMethodInvocationBoxingTests
{
  [Test]
  public void StructVirtualMethodWithoutOverride()
  {
    var st = new NoGetHashCodeOverride();
    Allocations.AssertAllocates(() => st.GetHashCode());
  }

  private struct NoGetHashCodeOverride { }

  [Test]
  public void NullableStructVirtualMethodWithoutOverride()
  {
    NoGetHashCodeOverride? st = new NoGetHashCodeOverride();
    Allocations.AssertAllocates(() => st.GetHashCode());
  }

  [Test]
  public void StructVirtualMethodWithOverride()
  {
    var st = new WithGetHashCodeOverride();
    Allocations.AssertNoAllocations(() => st.GetHashCode());
  }

  private struct WithGetHashCodeOverride
  {
    public override int GetHashCode() => 1;
  }

  [Test]
  public void NullableStructVirtualMethodWithOverride()
  {
    WithGetHashCodeOverride? st = new WithGetHashCodeOverride();
    Allocations.AssertNoAllocations(() => st.GetHashCode());
  }

  [Test]
  public void StructBaseGetHashCodeInvocation()
  {
    var st = new WithBaseGetHashCodeInvocation();
    Allocations.AssertAllocates(() => st.GetHashCode());
  }

  private struct WithBaseGetHashCodeInvocation
  {
    // ReSharper disable once RedundantOverriddenMember
    // ReSharper disable once BaseObjectGetHashCodeCallInGetHashCode
    public override int GetHashCode() => base.GetHashCode();
  }

  [Test]
  public void RecordStructGetHashCodeInvocation()
  {
    var st = new RecordStruct();
    Allocations.AssertNoAllocations(() => st.GetHashCode());
  }

  private record struct RecordStruct;
}