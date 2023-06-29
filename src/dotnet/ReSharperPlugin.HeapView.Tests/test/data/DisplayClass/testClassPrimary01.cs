using System;

// class-level display class
class C1(int parameter) : B(() => parameter);

// class-level display class
class C2(int parameter)
{
  public readonly Func<int> Field = () => parameter;
}

class B(Func<int> func);