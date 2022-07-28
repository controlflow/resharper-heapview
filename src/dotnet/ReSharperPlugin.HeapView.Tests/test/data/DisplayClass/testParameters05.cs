class C
{
  private static bool M(object o) => true;

  public bool this[int parameter]
    => parameter is var localVariable1
       && M(() => parameter + localVariable1);

  public bool Property1
    => 42 is var localVariable2
       && M(() => localVariable2);

  public bool Property2
  {
    get => 42 is var localVariable3
           && M(() => localVariable3);
  }
}