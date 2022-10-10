using NUnit.Framework;

namespace BoxingTestProject;

#if NETCOREAPP

[TestFixture]
public class DefaultMemberBoxingTest
{
  [Test]
  public void Method()
  {
    var wi = new WithImpl();
    Allocations.AssertNoAllocations(() => Generic(wi));
    Allocations.AssertNoAllocations(() => wi.BaseCall());

    var woi = new WithoutImpl();
    Allocations.AssertAllocates(() => Generic(woi));
    Allocations.AssertNoAllocations(() => woi.BaseCall());

    int Generic<T>(T t) where T : IWithDefault => t.Method();
  }

  private interface IWithDefault
  {
    int Method() => 0xFFFF + Other();
    int Other() => 0xAAAA;
  }

  private struct WithImpl : IWithDefault
  {
    public int Method() => 0xEEEE + ((IWithDefault)this).Other();
    public int BaseCall() => ((IWithDefault)this).Other();
  }

  private struct WithoutImpl : IWithDefault
  {
    public int BaseCall() => ((IWithDefault)this).Other();
  }
}

#endif