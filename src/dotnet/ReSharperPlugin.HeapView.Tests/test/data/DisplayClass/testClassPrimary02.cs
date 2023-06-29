using System;

class C3(int parameter)
{
  // optimized this capture
  public Func<int> Property => () => parameter;
}

class C4(int parameter)
{
  // optimized this capture
  public Func<int> Method() => () => parameter;
}

class C5(int parameter)
{
  // optimized this capture!
  public Func<int> Field = () => parameter;
  public Func<int> Method() => () => parameter;
}

class C6(int parameter1, int parameter2)
{
  // display class + this reference
  public Func<int> Field = () => parameter1 + parameter2;
  public Func<int> Method() => () => parameter2;
}

// display class
partial class C7(int parameter);

partial class C7
{
  public Func<int> Field = () => parameter;
}