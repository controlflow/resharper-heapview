using System;

class TypeTests
{
  public bool Test01<T>(T t) => t is int; // possible
  public bool Test02<T>(T t) => t is IConvertible; // possible
  public bool Test03<T>(T t) where T : struct => t is int; // yes
  public bool Test04<T>(T t) where T : struct => t is IConvertible; // yes
  public bool Test05<T>(T t) where T : class => t is int;
  public bool Test06<T>(T t) where T : class => t is IConvertible;
}