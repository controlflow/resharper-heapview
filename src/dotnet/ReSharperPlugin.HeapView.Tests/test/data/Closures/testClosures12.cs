class Foo
{
  void Usage()
  {
    void Local() { }
    var f = () => Local();
  }
}