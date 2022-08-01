using System;

class ThisCapture
{
  void HasDisplayClass(int parameter)
  {
    Func<int> func = () => OtherMethod() + parameter;
  }
    
  void NoDisplayClass()
  {
    Func<int> func = () => OtherMethod();
    Action action = () => HasDisplayClass(42);
    Func<object> explicitThis = () => this;
  }
    
  int OtherMethod() => 1;
}
