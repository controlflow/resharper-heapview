// ReSharper disable ReturnValueOfPureMethodIsNotUsed
// ReSharper disable CheckNamespace
// ReSharper disable UnusedVariable
// ReSharper disable UnusedParameter.Global
// ReSharper disable ConvertToConstant.Local
// ReSharper disable UnusedMember.Local
// ReSharper disable RedundantBaseQualifier
// ReSharper disable RedundantThisQualifier

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ConsoleApplication2
{
  internal class Program
  {
    private static void Main(string[] args)
    {
      var o = new {Foo = new {Bar = 123}};

      object a = 123;
      object b = o.Foo.Bar;
      string s = "sdsdsdsds" + 1;

      var foo = new Foo();
      var bar = new Bar();

      foreach (var x in foo) Console.WriteLine(x);
      foreach (var x in bar) Console.WriteLine(x);
      foreach (var x in (dynamic) foo) Console.WriteLine(x);

      var s1 = new S1();
      var s2 = new S2();

      foreach (var x in s1) Console.WriteLine(x);
      foreach (var x in s2) Console.WriteLine(x);

      s1.Select(x => x + 1)
        .Where(x => x > 0)
        .Select(x => new[] { x })
        .ToList()
        .ForEach(_ => { });

    }
  }

  public class Foo : IEnumerable<int>
  {
    public IEnumerator<int> GetEnumerator()
    {
      yield return 1;
      //return new Enumerator();
    }

    public struct Enumerator
    {
      private bool flag;
      public int Current { get { return 1; } }
      public bool MoveNext() { flag = false; return !flag; }
    }

    IEnumerator<int> IEnumerable<int>.GetEnumerator()
    {
      yield return 2;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      yield return 3;
    }
  }

  public class Bar : Foo { }

  public struct S1 : IEnumerable<int>
  {
    IEnumerator<int> IEnumerable<int>.GetEnumerator()
    {
      yield return 1;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      yield return 2;
    }
  }

  public struct S2
  {
    public IEnumerator<int> GetEnumerator()
    {
      yield return 1;
    }
  }
}
