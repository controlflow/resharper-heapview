using NUnit.Framework;

namespace BoxingTestProject;

[TestFixture]
public class BoxingTests
{
  [Test]
  public void StructVirtualMethodWithoutOverride()
  {
    var st = new NoGetHashCodeOverride();
    Allocations.AssertAllocates(() => st.GetHashCode());
  }

  [Test]
  public void StructVirtualMethodWithOverride()
  {
    var st = new WithGetHashCodeOverride();
    Allocations.AssertNoAllocations(() => st.GetHashCode());
  }

  [Test]
  public void BaseGetHashCodeInvocation()
  {
    var st = new WithBaseGetHashCodeInvocation();
    Allocations.AssertAllocates(() => st.GetHashCode());
  }

  private struct NoGetHashCodeOverride { }

  private struct WithGetHashCodeOverride
  {
    public override int GetHashCode() => 1;
  }

  private struct WithBaseGetHashCodeInvocation
  {
    // ReSharper disable once RedundantOverriddenMember
    // ReSharper disable once BaseObjectGetHashCodeCallInGetHashCode
    public override int GetHashCode() => base.GetHashCode();
  }
}