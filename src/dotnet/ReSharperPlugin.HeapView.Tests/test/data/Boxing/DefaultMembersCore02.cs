public interface IWithDefaultMembers<T>
{
  void DefaultMethod(T x) { }
}

public interface IInherits : IWithDefaultMembers<int> {  }
public interface IInherits2 : IWithDefaultMembers<int>, IWithDefaultMembers<string> {  }

public struct SomeStruct1 : IInherits { } // warn
public struct SomeStruct2 : IInherits2 { } // warn

public struct SomeStruct3 : IInherits2 // warn
{
  public void DefaultMethod(string x) { }
}

public struct WillNotBox : IInherits2
{
  public void DefaultMethod(int x) { }
  public void DefaultMethod(string x) { }
}

public struct WillNotBox2 : IWithDefaultMembers<int>, IWithDefaultMembers<string>
{
  void IWithDefaultMembers<int>.DefaultMethod(int x) { }
  void IWithDefaultMembers<string>.DefaultMethod(string x) { }
}