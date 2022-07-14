TopLevelCode();

class Class
{
  public Class(int constructorParameter) { }
  ~Class() { }

  private void Method(string methodParameter) { }

  public int ExpressionBodiedProperty => 42;

  public int Property
  {
    get => 42;
    set { _ = value; }
  } = 45;

  public string this[int indexerParameter] => "1";

  public string this[int parameter1, bool parameter2]
  {
    get { return "2"; }
    init => _ = value;
  }

  public static implicit operator bool(Class c) => true;

  public int Field = 45;
  public event EventHandler E = null;
}

record Base(bool Flag);

record Derived() : Base(true | false);