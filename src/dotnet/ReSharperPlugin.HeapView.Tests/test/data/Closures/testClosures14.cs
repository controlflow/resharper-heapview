record R
{
  private int myField;

  public void Method()
  {
    System.Func<int> LocalCapturingThis() => () => myField;
    var f = LocalCapturingThis;
  }
}