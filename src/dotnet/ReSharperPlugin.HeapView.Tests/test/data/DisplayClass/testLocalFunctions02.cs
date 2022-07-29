int x = 0;

void Local() { }

if (args.Length > 0)
{
  System.Action action = () =>
  {
    Local();
    x++;
  };
}