using System;
using System.Diagnostics.CodeAnalysis;
using NUnit.Framework;

namespace BoxingTestProject;

[TestFixture]
[SuppressMessage("ReSharper", "ConvertToConstant.Local")]
[SuppressMessage("Usage", "CA2248:Provide correct \'enum\' argument to \'Enum.HasFlag\'")]
public class RuntimeBoxingOptimizationsTests
{
  private static readonly MyEnum EnumValue = MyEnum.Flag1 | MyEnum.Flag2;
  private static readonly MyEnum EnumFlag = MyEnum.Flag1;
  private static readonly MyEnum? NullableEnumFlag = MyEnum.Flag1;
  private static readonly Enum EnumValueBoxed = EnumValue;

  [Test]
  public void EnumHasFlags()
  {
#if RELEASE && NETCOREAPP
    Allocations.AssertNoAllocations(() => EnumValue.HasFlag(EnumFlag));
#else
    Allocations.AssertAllocates(() => EnumValue.HasFlag(EnumFlag));
#endif

    Allocations.AssertAllocates(() => EnumValueBoxed.HasFlag(NullableEnumFlag!)); // not optimized

    Allocations.AssertAllocates(() => EnumValueBoxed.HasFlag(EnumFlag)); // arg boxing
    Allocations.AssertAllocates(() => EnumValue.HasFlag(EnumValueBoxed)); // this boxing

    Allocations.AssertNoAllocations(() => EnumValueBoxed.HasFlag(EnumValueBoxed)); // nothing to optimize
  }

  [Test]
  public void EnumHasFlagsGeneric()
  {
#if RELEASE && NETCOREAPP
    Allocations.AssertNoAllocations(() => Generic(EnumValue, EnumFlag));
#else
    Allocations.AssertAllocates(() => Generic(EnumValue, EnumFlag));
#endif

    bool Generic<TEnum>(TEnum? value, TEnum flag) where TEnum : Enum
      => value?.HasFlag(flag) == true && value.HasFlag(flag);
  }

  [Flags]
  private enum MyEnum
  {
    Flag1 = 1 << 0,
    Flag2 = 1 << 1
  }

  [Test]
  public void EnumGetHashCodeInvocation()
  {
    var e = GetHashCode() % 2 == 0 ? ConsoleKey.Clear : ConsoleKey.Add;
#if NETCOREAPP
    Allocations.AssertNoAllocations(() => e.GetHashCode()); // optimized by runtime
    Allocations.AssertNoAllocations(() => Generic(e)); // optimized by runtime
    Allocations.AssertAllocates(() => e.ToString());
    Allocations.AssertAllocates(() => e.Equals(null));
#else
    Allocations.AssertAllocates(() => e.GetHashCode());
    Allocations.AssertAllocates(() => Generic(e));
    Allocations.AssertAllocates(() => e.ToString());
    Allocations.AssertAllocates(() => e.Equals(null));
#endif

    int Generic<TEnum>(TEnum value) => value!.GetHashCode();
  }

  [Test]
  public void NullableEnumGetHashCodeInvocation()
  {
    ConsoleKey? e = GetHashCode() % 2 == 0 ? ConsoleKey.Clear : ConsoleKey.Add;
    object boxedComparand = ConsoleKey.Clear;
#if NETCOREAPP
    Allocations.AssertNoAllocations(() => e.GetHashCode()); // optimized by runtime
    Allocations.AssertAllocates(() => e.ToString());
    Allocations.AssertNoAllocations(() => e.Equals(null)); // shortcut
    Allocations.AssertAllocates(() => e.Equals(boxedComparand));
#else
    Allocations.AssertAllocates(() => e.GetHashCode());
    Allocations.AssertAllocates(() => e.ToString());
    Allocations.AssertNoAllocations(() => e.Equals(null)); // shortcut
    Allocations.AssertAllocates(() => e.Equals(boxedComparand));
#endif
  }
}