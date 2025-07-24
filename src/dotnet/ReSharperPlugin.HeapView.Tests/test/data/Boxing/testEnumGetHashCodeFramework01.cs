using System;

class EnumGetHashCode
{
  private int Test01(Flags value) => value.GetHashCode(); // yes
  private int Test02(Flags? value) => value.GetHashCode(); // yes
  private int Test03<TEnum>(TEnum value) where TEnum : Enum => value.GetHashCode(); // possible
  private int Test04<TEnum>(TEnum value) where TEnum : struct, Enum => value.GetHashCode(); // possible
  private int Test05<TEnum>(TEnum? value) where TEnum : Enum => value!.GetHashCode(); // possible
  private int Test06<TEnum>(TEnum? value) where TEnum : struct, Enum => value.GetHashCode(); // possible

  [Flags] private enum Flags { }
}