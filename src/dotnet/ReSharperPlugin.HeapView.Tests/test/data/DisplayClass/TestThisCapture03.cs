using System;

class ThisCapture
{
  Func<int> Method1() => () => Property;
  Func<int> Method2() { return () => Property; }

  Func<int> Property1 => () => Property;
  Func<int> Property2 => () => this[1];
  Func<int> Property3
  {
    get => () => Property;
  }

  int Property => 1;
  int this[int index] => 1;
}