using System;

class EnumHasFlags
{
  private bool Test01(Flags value, Flags flag) => value.HasFlag(flag); // yes, 2 boxes, debug
  private bool Test02(Flags value, Flags? flag) => value.HasFlag(flag!); // yes, 2 boxes
  private bool Test03(Flags value, Enum flag) => value.HasFlag(flag); // yes, this invocation
  private bool Test04(Enum value, Flags flag) => value.HasFlag(flag); // yes, arg boxing
  private bool Test05(Enum value, Enum flag) => value.HasFlag(flag);

  private bool Generic01<TEnum>(TEnum value, TEnum flag) where TEnum : Enum => value.HasFlag(flag); // yes, 2 boxes, debug
  private bool Generic02<TEnum>(TEnum value, TEnum? flag) where TEnum : Enum => value.HasFlag(flag!); // yes, 2 boxes, debug
  private bool Generic03<TEnum>(TEnum value, Enum flag) where TEnum : Enum => value.HasFlag(flag); // possible, this invocation
  private bool Generic03<TEnum>(Enum value, TEnum flag) where TEnum : Enum => value.HasFlag(flag); // possible, arg boxing

  private bool Generic04<TEnum>(TEnum value, TEnum flag) where TEnum : struct, Enum => value.HasFlag(flag); // yes, 2 boxes, debug
  private bool Generic05<TEnum>(TEnum value, TEnum? flag) where TEnum : struct, Enum => value.HasFlag(flag!); // yes, 2 boxes
  private bool Generic06<TEnum>(TEnum value, Enum flag) where TEnum : struct, Enum => value.HasFlag(flag); // yes, this invocation
  private bool Generic07<TEnum>(Enum value, TEnum flag) where TEnum : struct, Enum => value.HasFlag(flag); // yes, arg boxing

  private bool? Generic08<TEnum>(TEnum? value, TEnum flag) where TEnum : Enum => value?.HasFlag(flag); // yes, 2 boxes, debug

  [Flags]
  private enum Flags { }
}