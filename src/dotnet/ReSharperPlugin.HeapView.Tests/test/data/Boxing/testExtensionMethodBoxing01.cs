using System.Collections;

_ = new Foo { 1, 2, "aaa", { 1, true } };
_ 

struct Foo : IFoo, IEnumerable
{
  public IEnumerator GetEnumerator() { yield break; }
}

interface IFoo { }

static class Extensions
{
  public static void Add(this IFoo foo, int item) { }
  public static void Add(this Foo foo, string item) { }
  public static void Add(this IFoo foo, int item, bool flag) { }
}