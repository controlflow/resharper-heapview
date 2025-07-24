using System;

Foo.Print(__arglist(1, true));

class Foo
{
  public static void Print(__arglist)
  {
    var iterator = new ArgIterator(__arglist);
    while (iterator.GetRemainingCount() > 0)
    {
      var typedReference = iterator.GetNextArg();
      Console.WriteLine("{0} / {1}",
        TypedReference.ToObject(typedReference), 
        TypedReference.GetTargetType(typedReference));
    }
  }
}