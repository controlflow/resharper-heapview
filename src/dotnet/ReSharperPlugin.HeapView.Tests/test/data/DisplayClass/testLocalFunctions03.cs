int x = 0;

void Local()
{
  x++;
}

if (args.Length > 0)
{
  System.Action action = () =>
  {
    Local();
  };
}