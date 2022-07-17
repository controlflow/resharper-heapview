using NUnit.Framework;

namespace BoxingTestProject;

[TestFixture]
public class StructGetTypeMethodInvocationTests
{
  [Test]
  public void StructVirtualMethodWithoutOverride()
  {
    var st = new SomeStruct();
#if DEBUG
    Allocations.AssertAllocates(() => st.GetType());
#else
    Allocations.AssertNoAllocations(() => st.GetType());
#endif
  }

  private struct SomeStruct { }
}