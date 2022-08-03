using System;

class C
{
  public int Field;

  void Method(int x)
  {
    // only references 'this', lowered into instance member
    Func<int> func = () => Field;

    if (x > 0)
    {
      // display class is created here
      int y = x;
      Func<int> inner = () => Field + y;

      Func<int> func2 = () => Field + 2; // instance method
    }
    else
    {
      int z = x;
      void Local() => z += Field;
      Local();
    }
  }
    
  void OtherMethod()
  {
    int Local() => Field;
    Local();
  }
}