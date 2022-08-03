// display class #1
int y = 0;

if (args.Length > 0)
{
  void Outer() => y++;

  // display class #2, references #1
  int x = 0;

  void Local()
  {
    x++;
    Outer();
  }
    
  System.Action action = () =>
  {
    Local();
  };
}