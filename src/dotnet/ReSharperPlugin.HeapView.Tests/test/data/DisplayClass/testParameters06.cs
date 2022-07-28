class C
{
  public C(int parameter)
    : this(parameter is var local1
           && F(() => parameter + local1 + 0))
  {
    var local2 = parameter;
    F(() => parameter + local2 + 1);
  }
    
  public C(bool b) { }

  public object Method1(int parameter)
    => parameter is var local
       && F(() => parameter + local + 2);

  public void Method2(int parameter)
  {
    var local = parameter;
    F(() => parameter + local + 3);
  }

  public static bool operator-(C parameter)
  {
    var local = parameter.GetHashCode();
    return F(() => parameter + "4" + local);
  }

  private static bool F(object o) => true;
}