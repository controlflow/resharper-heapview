// ReSharper disable ValueParameterNotUsed

public interface IWithDefaultMembers
{
  public int Property
  {
    get => 1;
    set { }
  }
}

public struct SomeStruct1 : IWithDefaultMembers { } // warn

public struct SomeStruct2 : IWithDefaultMembers // warn
{
  public int Property => 1;
}

public struct SomeStruct3 : IWithDefaultMembers // warn
{
  public int Property
  {
    set { }
  }
}

public struct WillNotBox : IWithDefaultMembers
{
  public int Property { get; set; }
}