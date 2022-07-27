using System;

class C
{
  private void F(object o) { }
    
  public object Property
  {
    get => null;
    init => F(() => value);
  }

  public event EventHandler Event
  {
    add => F(() => value);
    remove => F(() => value);
  }
}