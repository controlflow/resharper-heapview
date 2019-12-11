using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

// ReSharper disable UnusedTypeParameter
// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable InconsistentNaming

internal interface IBar
{
  object BooVirt();
  object BooVirt<U>();
}

class Bar : IBar
{
  public static object Foo() { return null; }
  public static object Foo<U>() { return null; }
  public object Boo() { return null; }
  public object Boo<U>() { return null; }
  public virtual object BooVirt() { return null; }
  public virtual object BooVirt<U>() { return null; }
  public static void Baz<U>(U a) { }
  public void Baz2<U>(U a) { }
}

internal interface IBar<T>
{
  object BooVirt();
}

class Bar<T> : IBar<T>
{
  public static object Foo() { return null; }
  public object Boo() { return null; }
  public virtual object BooVirt() { return null; }
}

class Program
{
  static void Measure(Action action)
  {
    Thread.CurrentThread.Priority = ThreadPriority.Highest;

    action();

    var sw = Stopwatch.StartNew();
    for (var i = 0; i < COUNT; i++) action();

    Console.WriteLine("{0} => {1}", ourIndex++, sw.Elapsed);
    Thread.CurrentThread.Priority = ThreadPriority.Normal;
  }

  const int COUNT = 1000000;
  static int ourIndex = 1;

  static readonly Bar simpleBar = new Bar();
  static readonly Bar<int> genericBarOfInt = new Bar<int>();
  static readonly Bar<string> genericBarOfString = new Bar<string>();
  static readonly IBar simpleIface = simpleBar;
  static readonly IBar<int> genericIfaceOfInt = genericBarOfInt;
  static readonly IBar<string> genericIfaceOfString = genericBarOfString;

  public static void Main()
  {
    // delegates from static methods
    Measure(() => PassMeDelegate(Bar.Foo));          // 1
    Measure(() => PassMeDelegate(Bar.Foo<int>));     // 2
    Measure(() => PassMeDelegate(Bar.Foo<string>));  // 3
    Measure(() => PassMeDelegate(Bar.Foo<string>));  // 4
    Measure(() => PassMeDelegate(Bar<int>.Foo));     // 5
    Measure(() => PassMeDelegate(Bar<string>.Foo));  // 6

    // delegates from instance methods
    Measure(() => PassMeDelegate(simpleBar.Boo));               // 7
    Measure(() => PassMeDelegate(simpleBar.Boo<int>));          // 8
    Measure(() => PassMeDelegate(simpleBar.Boo<string>));       // 9
    Measure(() => PassMeDelegate(simpleBar.BooVirt));           // 10
    Measure(() => PassMeDelegate(simpleBar.BooVirt<int>));      // 11
    Measure(() => PassMeDelegate(simpleBar.BooVirt<string>));   // 12
    Measure(() => PassMeDelegate(genericBarOfInt.Boo));         // 13
    Measure(() => PassMeDelegate(genericBarOfString.Boo));      // 14
    Measure(() => PassMeDelegate(genericBarOfInt.BooVirt));     // 15
    Measure(() => PassMeDelegate(genericBarOfString.BooVirt));  // 16

    // delegate from interface members
    Measure(() => PassMeDelegate(simpleIface.BooVirt));           // 17  <--
    Measure(() => PassMeDelegate(simpleIface.BooVirt<int>));      // 18  <--
    Measure(() => PassMeDelegate(simpleIface.BooVirt<string>));   // 19  <--
    Measure(() => PassMeDelegate(genericIfaceOfInt.BooVirt));     // 20  <--
    Measure(() => PassMeDelegate(genericIfaceOfString.BooVirt));  // 21  <--

    // delegates, created from lambdas in a generic method
    Measure(GenericTest<int>);     // 22
    Measure(GenericTest<string>);  // 23  <--

    // delegates, created from non-generic static method group in a generic type,
    // parametrized with the containing generic method's type parameter
    Measure(GenericTest2<int>);     // 24
    Measure(GenericTest2<string>);  // 25 <--

    // delegates, created from generic static method group in a non-generic type,
    // parametrized with the containing generic method's type parameter
    Measure(GenericTest3<int>);     // 26
    Measure(GenericTest3<string>);  // 27 <--

    // delegates, created from generic instance method group in a non-generic type,
    // parametrized with the containing generic method's type parameter
    Measure(GenericTest4<int>);     // 28
    Measure(GenericTest4<string>);  // 29

    // delegates, created from non-generic static method group in a generic type,
    // parametrized with the containing type type parameter
    Measure(Some<int>.StaticTest);     // 30
    Measure(Some<string>.StaticTest);  // 31 <--

    // delegates, created from generic static method group in a non-generic type,
    // parametrized with the containing type type parameter
    Measure(Some<int>.StaticTest2);     // 32
    Measure(Some<string>.StaticTest2);  // 33 <--

    // delegates, created from generic instance method group in a non-generic type,
    // parametrized with the containing type type parameter
    Measure(Some<int>.StaticTest3);     // 34
    Measure(Some<string>.StaticTest3);  // 35

    var someOfInt = new Some<int>();
    var someOfString = new Some<int>();

    // delegates, from non-generic static method group in a generic type,
    // parametrized with the containing type type parameter, created in a instance method
    Measure(someOfInt.InstanceTest);     // 36
    Measure(someOfString.InstanceTest);  // 37

    // delegates, created from generic static method group in a non-generic type,
    // parametrized with the containing type type parameter, created in a instance method
    Measure(someOfInt.InstanceTest2);     // 38
    Measure(someOfString.InstanceTest2);  // 39

    // delegates, created from generic instance method group in a non-generic type,
    // parametrized with the containing type type parameter, created in a instance method
    Measure(someOfInt.InstanceTest3);     // 40
    Measure(someOfString.InstanceTest3);  // 41

    // delegates, created from generic static method group in a non-generic type,
    // parametrized with the containing generic method's type parameter, infered from usage
    Measure(GenericTest5<int>);     // 42
    Measure(GenericTest5<string>);  // 43 <--

    // delegates, created from generic instance method group in a non-generic type,
    // parametrized with the containing generic method's type parameter, infered from usage
    Measure(GenericTest6<int>);     // 44
    Measure(GenericTest6<string>);  // 45

    // delegates, created from generic static method group in a non-generic type,
    // parametrized with the containing type type parameter, infered from usage
    Measure(Some<int>.StaticTest4);     // 46
    Measure(Some<string>.StaticTest4);  // 47 <--

    // delegates, created from generic static method group in a non-generic type,
    // parametrized with the containing type type parameter, infered from usage
    Measure(Some<int>.StaticTest5);     // 48
    Measure(Some<string>.StaticTest5);  // 49

    // delegates, created from generic static method group in a non-generic type,
    // parametrized with the containing type type parameter,
    // infered from usage and created in a instance method
    Measure(someOfInt.InstanceTest4);     // 50
    Measure(someOfString.InstanceTest4);  // 51

    // delegates, created from generic instance method group in a non-generic type,
    // parametrized with the containing type type parameter,
    // infered from usage and created in a instance method
    Measure(someOfInt.InstanceTest5);     // 52
    Measure(someOfString.InstanceTest5);  // 53

    // delegates, created from non-generic static method group in a generic type,
    // parametrized with the containing type type parameter (hidden by implicit qualifier)
    Measure(Some<int>.StaticTest6);           // 54
    Measure(Some<string>.StaticTest6);        // 55 <--
    Measure(Some<int>.Other.StaticTest8);     // 56
    Measure(Some<string>.Other.StaticTest8);  // 57 <--
    Measure(someOfInt.InstanceTest6);         // 58
    Measure(someOfString.InstanceTest6);      // 59

    var otherOfInt = new Some<int>.Other();
    var otherOfString = new Some<string>.Other();
    Measure(otherOfInt.InstanceTest8);        // 60
    Measure(otherOfString.InstanceTest8);     // 61 <-- ????

    // delegates, created from non-generic static method group in a generic type,
    // parameterized with some other type, parametrized with the containing type type parameter
    Measure(Some<int>.StaticTest7);     // 62
    Measure(Some<string>.StaticTest7);  // 63
  }

  // lags only if T is a reference type (but not at x64)
  static void GenericTest<T>() { PassMeDelegate(() => typeof(T)); }  // <--
  static void GenericTest2<T>() { PassMeDelegate(Bar<T>.Foo); }       // <--
  static void GenericTest3<T>() { PassMeDelegate(Bar.Foo<T>); }       // <--

  // do not lags (instance vtbl helps to resolve method)
  static void GenericTest4<T>() { PassMeDelegate(simpleBar.Boo<T>); }

  // infered type parameters
  static void GenericTest5<T>() { PassMeGenericDelegate<T>(Bar.Baz); } // <--
  static void GenericTest6<T>() { PassMeGenericDelegate<T>(simpleBar.Baz2); }

  sealed class Some<T>
  {
    public static void StaticTest() { PassMeDelegate(Bar<T>.Foo); } // <--
    public static void StaticTest2() { PassMeDelegate(Bar.Foo<T>); } // <--
    public static void StaticTest3() { PassMeDelegate(simpleBar.Boo<T>); }
    public static void StaticTest4() { PassMeGenericDelegate<T>(Bar.Baz); } // <--
    public static void StaticTest5() { PassMeGenericDelegate<T>(simpleBar.Baz2); }
    public static void StaticTest6() { PassMeDelegate(Booo); } // <-- (hidden type parameter)
    public static void StaticTest7() { PassMeDelegate(Bar<Action<T>>.Foo); } // <--

    // do not lags at all (the same vtbl help)
    public void InstanceTest() { PassMeDelegate(Bar<T>.Foo); }
    public void InstanceTest2() { PassMeDelegate(Bar.Foo<T>); }
    public void InstanceTest3() { PassMeDelegate(simpleBar.Boo<T>); }
    public void InstanceTest4() { PassMeGenericDelegate<T>(Bar.Baz); }
    public void InstanceTest5() { PassMeGenericDelegate<T>(simpleBar.Baz2); }
    public void InstanceTest6() { PassMeDelegate(Booo); }

    // even more hidden type parameter
    public class Other
    {
      public static void StaticTest8() { PassMeDelegate(Booo2); } // <--
      public void InstanceTest8() { PassMeDelegate(Booo2); } // <-- !!!!!!! because this method is static_by_T_type_parameter
      private static object Booo2() { return null; }
    }

    private static object Booo() { return null; }
  }

  delegate object SomeFunc();
  [MethodImpl(MethodImplOptions.NoInlining)]
  static void PassMeDelegate(SomeFunc action) { GC.KeepAlive(action); }

  [MethodImpl(MethodImplOptions.NoInlining)]
  static void PassMeGenericDelegate<U>(Action<U> action) { GC.KeepAlive(action); }
}