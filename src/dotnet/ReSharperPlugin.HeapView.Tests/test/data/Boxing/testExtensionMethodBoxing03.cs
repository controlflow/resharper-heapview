using System.Collections.Generic;

var foo = new Foo();

foreach (var x in foo) { }

struct Foo : IFoo { }

interface IFoo { }

static class Extensions
{
  public static IEnumerator<int> GetEnumerator(this IFoo foo)
  {
    yield break;
  }
}