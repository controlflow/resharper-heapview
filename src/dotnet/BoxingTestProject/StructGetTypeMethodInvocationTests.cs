using NUnit.Framework;

namespace BoxingTestProject;

[TestFixture]
public class StructGetTypeMethodInvocationTests
{
  [Test]
  public void StructVirtualMethodWithoutOverride()
  {
    var st = new SomeStruct();
    Allocations.AssertAllocates(() => st.GetType());
  }

  private struct SomeStruct { }
}