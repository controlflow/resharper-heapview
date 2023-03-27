using System.Linq.Expressions;

var f = Local;
ref int Local() => throw null!;

Expression expr = () => new C().Method;

class C {
  public void Method() { }
}