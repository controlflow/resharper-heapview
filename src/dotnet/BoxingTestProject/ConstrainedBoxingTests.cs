using System.Diagnostics.CodeAnalysis;
using NUnit.Framework;

namespace BoxingTestProject;

[TestFixture]
[SuppressMessage("ReSharper", "RedundantCast")]
public class ConstrainedBoxingTests
{
  [Test]
  public void GenericTypeTestAndCast()
  {
    Allocations.AssertNoAllocations(() => Generic(new SomeStruct()));
    Allocations.AssertNoAllocations(() => Generic(42));
    Allocations.AssertNoAllocations(() => Generic("aaa"));

    static int Generic<T>(T t) => typeof(T) == typeof(int) ? (int)(object?)t! : -1;
  }

  [Test]
  public void ConstrainedMethodUsage()
  {
    var st = new SomeStruct();
#if RELEASE && NETCOREAPP
    Allocations.AssertNoAllocations(() => NonGeneric(st));
    Allocations.AssertNoAllocations(() => Generic(st));
#else
    Allocations.AssertAllocates(() => NonGeneric(st));
    Allocations.AssertAllocates(() => Generic(st));
#endif

    static int NonGeneric(SomeStruct s) => ((IFoo) s).Method();
    static int Generic<T>(T t) => ((IFoo) t!).Method();
  }

  [Test]
  public void ConstrainedPropertyUsage()
  {
    var st = new SomeStruct();
#if RELEASE && NETCOREAPP
    Allocations.AssertNoAllocations(() => NonGeneric(st));
    Allocations.AssertNoAllocations(() => Generic(st));
#else
    Allocations.AssertAllocates(() => NonGeneric(st));
    Allocations.AssertAllocates(() => Generic(st));
#endif

    static int NonGeneric(SomeStruct s) => ((IFoo) s).Property;
    static int Generic<T>(T t) => ((IFoo) t!).Property;
  }

  [Test]
  public void ConstrainedIndexerUsage()
  {
    var st = new SomeStruct();
#if RELEASE && NETCOREAPP
    Allocations.AssertNoAllocations(() => NonGeneric(st));
    Allocations.AssertNoAllocations(() => Generic(st));
#else
    Allocations.AssertAllocates(() => NonGeneric(st));
    Allocations.AssertAllocates(() => Generic(st));
#endif

    static int NonGeneric(SomeStruct s) => ((IFoo) s)[42];
    static int Generic<T>(T t) => ((IFoo) t!)[42];
  }

  private struct SomeStruct : IFoo
  {
    public int Method() => 42;
    public int Property => 42;
    public int this[int index] => index;
  }

  private interface IFoo
  {
    int Method();
    int Property { get; }
    int this[int index] { get; }
  }
}