using System;

public class Unfinished
{
  void GenericMethod<TFoo>(TFoo foo) where TFoo : class
  {
    if (foo is IDisposable disposable) { }
  }
}