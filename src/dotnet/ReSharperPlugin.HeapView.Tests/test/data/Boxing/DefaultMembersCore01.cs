public interface IWithDefaultMembers
{
  void DefaultMethod(int x) { }
  void DefaultMethod(string x) { }
  public static virtual void StaticDefaultMethod() { }
}

public interface IUsualInterface
{
  void AbstractMethod(bool f);
}

public struct SomeStruct : IWithDefaultMembers { } // warn
public record struct RecordStruct : IWithDefaultMembers;

public struct WillNotBox : IWithDefaultMembers, IUsualInterface
{
  public void DefaultMethod(int x) { }
  public void DefaultMethod(string x) { }
  public void AbstractMethod(bool f) { }
}

public struct WillNotBox2 : IUsualInterface, IWithDefaultMembers
{
  void IWithDefaultMembers.DefaultMethod(int x) { }
  void IWithDefaultMembers.DefaultMethod(string x) { }
  void IUsualInterface.AbstractMethod(bool f) { }
}

public struct WillBoxSometimes : IUsualInterface, IWithDefaultMembers
{
  void IWithDefaultMembers.DefaultMethod(string x) { }
  void IUsualInterface.AbstractMethod(bool f) { }
}