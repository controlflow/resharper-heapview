using System;
// ReSharper disable UseArrayEmptyMethod
// ReSharper disable RedundantExplicitArraySize
// ReSharper disable RedundantOverflowCheckingContext

public class C
{
  public ReadOnlySpan<byte> Test01() => new byte[0] { };
  public ReadOnlySpan<byte> Test02() => new byte[0]; // yes, no initializer
  public ReadOnlySpan<bool> Test03() => checked(new[] { true, false });
  public Span<bool> Test04() => new[] { true, false }; // yes, mutable
  public ReadOnlySpan<int> Test05() => new[] { 1, 2, 3 }; // yes, not bytes
  public ReadOnlySpan<byte> Test06(byte b) => new byte[] { 1, b, 3 }; // yes, not constant
  public ReadOnlySpan<sbyte> Test07() => new sbyte[] { 1, 0, 3 };
  public ReadOnlySpan<sbyte> Test08() => new sbyte[13]; // yes, no initializer
  public ReadOnlySpan<sbyte> Test09() => new sbyte[13]; // yes, no initializer
  public ReadOnlySpan<IntE> Test10() => new[] { IntE.A, IntE.A }; // yes, not bytes
  public ReadOnlySpan<ByteE> Test11() => new[] { ByteE.A, ByteE.A };
}

public enum ByteE : byte { A }
public enum IntE { A }