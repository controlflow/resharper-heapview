class Foo
{
  void Usage()
  {
    int x = 0;

    void Local()
    {
      x++;
      System.Console.WriteLine(x);
    }

    var f = () => Local();
  }
}