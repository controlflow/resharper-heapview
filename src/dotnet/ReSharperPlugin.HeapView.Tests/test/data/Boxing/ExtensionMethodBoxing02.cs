var foo = new Foo();

var (a, b) = foo;
(a, b) = foo;

var xs = new[] { foo };
foreach (var (x, y) in xs) { }
foreach ((var xx, var yy) in xs) { }

if (foo is (_, _)) { }
if (foo is var (_, _)) { }
if (foo is { Inner: (_, _) }) { }
if (foo is { Inner: var (_, _) }) { }

object obj = foo;
if (obj is Foo (_, _)) { }
if (obj is Foo { Inner: (_, _) }) { }
if (obj is Foo { Inner: var (_, _) }) { }

IFoo ifoo = foo;
if (ifoo is (_, _)) { }
if (ifoo is var (_, _)) { }

var tuple = (foo, (IFoo) foo);
if (tuple is ((_, _), (_, _))) { }
var ((_, _), (_, _)) = tuple;
((_, _), (_, _)) = tuple;

struct Foo : IFoo
{
  public Foo Inner => default;
}

interface IFoo { }

static class Extensions
{
  public static void Deconstruct(this IFoo foo, out int a, out int b)
  {
    a = b = 0;
  }
}