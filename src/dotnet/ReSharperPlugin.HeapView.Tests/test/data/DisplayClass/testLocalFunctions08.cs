using System.Collections.Generic;

{
  // display class #1, optimized
  int i = 0;
  void Local() => i++;
  Local();
}

{
  // display class #2
  int i = 0;
  async void Local() => i++;
  Local();
}

{
  // display class #3
  int i = 0;
  IEnumerable<int> Local() { yield return i; }
  Local();
}