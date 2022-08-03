using System;

// display class #1
int x = 0;
Func<int> func = () => x;

if (args.Length > 0)
{
  // display class #2, optimized into struct
  int y = x;
  void Local() { x++; y++; }
  Local();
}