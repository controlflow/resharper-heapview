class C
{
  public C(string parameter)
  {
    var localVariable = "local";
    var func = () => parameter + localVariable;
  }

  public object Method(int parameter) => (int x) => parameter + x;
}