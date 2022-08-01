using System;

record ThisCapture
{
  public ThisCapture(int parameter)
  {
    int local = 0; // separate display class
    if (parameter > 0)
    {
      Func<int> func = () => parameter + local;
    }
    else
    {
      Func<int> func = () => OtherMethod() + local;
    }
  }

  // shared display class
  public ThisCapture(int parameter, bool overload)
  {
    if (parameter > 0)
    {
      Func<int> func = () => parameter;
    }
    else
    {
      Func<int> func = () => OtherMethod();
    }
  }
    
  void SharedDisplayClass(int parameter)
  {
    int local = 0;
    if (parameter > 0)
    {
      Func<int> func = () => parameter + local;
    }
    else
    {
      Func<int> func = () => OtherMethod() + local;
    }
  }

  int OtherMethod() => 1;
}
