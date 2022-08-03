// display class #1, optimized
int y = 0;

if (args.Length > 0)
{
  void Outer() => y++;

  // display class #2, optimized
  int x = 0;

  void Local()
  {
    x++;
    Outer();
  }
    
  Local();
}