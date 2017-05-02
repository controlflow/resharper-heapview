using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using NUnit.Framework;
#pragma warning disable 659

namespace JetBrains.ReSharper.HeapView
{
  [TestFixture]
  public class AllocationsTest
  {
    [Test]
    [SuppressMessage("ReSharper", "ReturnValueOfPureMethodIsNotUsed")]
    public void StructInstanceMember()
    {
      // warm up
      var s = new S();

      s.InstanceMethod();
      s.GetType();
      //s.GetHashCode();
      s.Equals(null);

      var memoryBefore = GC.GetTotalMemory(true);

      s.InstanceMethod();

      var memoryAfterInstanceMethod = GC.GetTotalMemory(false);
      AssertNoAlloc(memoryAfterInstanceMethod == memoryBefore);

      s.GetType();

      var memoryAfterGetType = GC.GetTotalMemory(false);
      AssertNoAlloc(memoryAfterGetType > memoryAfterInstanceMethod);

      s.GetHashCode();

      var memoryAfterGetHashCode = GC.GetTotalMemory(false);
      AssertNoAlloc(memoryAfterGetHashCode == memoryAfterGetType);

      s.Equals(null);

      var memoryAfterOverridenEquals = GC.GetTotalMemory(false);
      AssertNoAlloc(memoryAfterOverridenEquals == memoryAfterGetHashCode);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [SuppressMessage("ReSharper", "UnusedParameter.Local")]
    private static void Use(int x) { }

    [Test]
    public void GenericStructInstanceMember()
    {
      //GenericStructInstanceMember(new S());
    }

    [SuppressMessage("ReSharper", "ReturnValueOfPureMethodIsNotUsed")]
    public void GenericStructInstanceMember<T>(T t)
      where T : struct, I
    {
      // warm up
      t.InstanceMethod();
      t.GetType();
      t.GetHashCode();
      t.Equals(null);

      var memoryBefore = GC.GetTotalMemory(true);

      t.InstanceMethod();

      var memoryAfterInstanceMethod = GC.GetTotalMemory(false);
      AssertNoAlloc(memoryAfterInstanceMethod == memoryBefore);

      t.GetType();

      var memoryAfterGetType = GC.GetTotalMemory(false);
      AssertNoAlloc(memoryAfterGetType > memoryAfterInstanceMethod);

      t.GetHashCode();

      var memoryAfterGetHashCode = GC.GetTotalMemory(false);
      AssertNoAlloc(memoryAfterGetHashCode == memoryAfterGetType);

      t.Equals(null);

      var memoryAfterOverridenEquals = GC.GetTotalMemory(false);
      AssertNoAlloc(memoryAfterOverridenEquals == memoryAfterGetHashCode);
    }

    [ContractAnnotation("false => halt")]
    [SuppressMessage("ReSharper", "UnusedParameter.Local")]
    private static void AssertNoAlloc(bool condition)
    {
      if (!condition)
        throw new AssertionException("Condition is false");
    }

    public struct S //: I
    {
      public void InstanceMethod() { }

      public override bool Equals(object obj)
      {
        return false;
      }
    }

    public interface I
    {
      void InstanceMethod();
    }
  }
}