using System;
// ReSharper disable ValueParameterNotUsed
// ReSharper disable ArrangeAccessorOwnerBody

class SomeClass {
  public SomeClass() { F(() => "ctor"); }
  public SomeClass(string s) => F(() => "ctor");

  public void ExpressionBodyMethod() => F(() => "expr method");

  public void BlockBodyMethod() {
    var localVariable = F(() => "block method");
    void BlockLocalFunction() { F(() => "block local function"); }
    void ExpressionLocalFunction() => F(() => "expr local function");
  }

  public static implicit operator string(SomeClass s) => F(() => "conv operator");
  public static string operator-(SomeClass s) { return F(() => "sign operator"); }

  ~SomeClass() { F(() => "destructor"); }

  public string PropertyWithAccessors {
    get { return F(() => "block getter"); }
    set => F(() => "expr setter");
  }

  public string ExpressionProperty => F(() => "expr property");

  public string this[int index] {
    get => F(() => "expr indexer getter");
  }

  public string this[string key] => F(() => "expr indexer");

  public event EventHandler CustomEvent {
    add { F(() => "block adder"); }
    remove => F(() => "expr remover");
  }

  public const string Const = "aaa";
  public string Field = F(() => "field initializer");
  public event EventHandler FieldLikeEvent = delegate { _ = "field like event"; };
  public string[] AutoProperty { get; } = { F(() => "auto-property") };

  public static string F(Func<string> func) => func();
}