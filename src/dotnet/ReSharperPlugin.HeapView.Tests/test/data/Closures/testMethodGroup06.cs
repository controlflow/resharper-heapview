using System;

public class Optimization
{
  static void Static() { }
  void Instance() { }

  public Action CombineThemAll()
  {
    Action action = new ((Static)); // alloc

    return
      new Action(Static) // alloc
      + Static
      + new Action(Instance) // alloc
      + Instance; // alloc
  }
}