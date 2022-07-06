using System.Collections;

_ = new Foo { 1, 2, "aaa", { 1, true } };

struct Foo : IFoo, IEnumerable
{
  public IEnumerator GetEnumerator() { yield break; }
}

interface IFoo { }

static class Extensions
{
  public static void Add(this IFoo foo, int item) { }
  public static void Add(this IFoo foo, string item) { }
  public static void Add(this IFoo foo, int item, bool flag) { }
}