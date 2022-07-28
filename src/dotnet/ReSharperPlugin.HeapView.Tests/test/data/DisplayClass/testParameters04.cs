class C
{
  public object this[string parameter]
  {
    get => () => parameter;
    set { var f = () => value + parameter; }
  }

  public object this[int parameter] => () => parameter;
}