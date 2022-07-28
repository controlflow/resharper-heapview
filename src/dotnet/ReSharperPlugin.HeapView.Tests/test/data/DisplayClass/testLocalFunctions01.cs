static bool F(Func<int> f) => true;

// display class #1
int x = 0;

{
  // display class #2
  int a = 0;
  void Local() => a++;
  Local();

  {
    // display class #3
    int y = x;
    F(() => x + y);
  }
}