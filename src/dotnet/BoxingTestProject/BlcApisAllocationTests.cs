using System;
using System.Diagnostics.CodeAnalysis;
using NUnit.Framework;

namespace BoxingTestProject;

[TestFixture]
[SuppressMessage("ReSharper", "ConvertClosureToMethodGroup")]
public class BlcApisAllocationTests
{
  [Test]
  public void ActivatorCreateInstance()
  {
    Allocations.AssertAllocates(() => Activator.CreateInstance(typeof(SomeStruct)));
  }

  [Test]
  public void GenericActivatorCreateInstance()
  {
#if NETCOREAPP
    Allocations.AssertNoAllocations(() => Activator.CreateInstance<SomeStruct>());
#else
    Allocations.AssertAllocates(() => Activator.CreateInstance<SomeStruct>());
#endif
  }

  private struct SomeStruct { }
}