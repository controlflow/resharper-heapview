// ReSharper disable ReturnValueOfPureMethodIsNotUsed
// ReSharper disable CheckNamespace
// ReSharper disable UnusedVariable
// ReSharper disable UnusedParameter.Global
// ReSharper disable ConvertToConstant.Local
// ReSharper disable UnusedMember.Local
// ReSharper disable RedundantBaseQualifier
// ReSharper disable RedundantThisQualifier
// ReSharper disable EqualExpressionComparison
using System;
#pragma warning disable 659

public enum E { C }
public interface I { void InstanceMethod(); }

public struct S : I {
  public void InstanceMethod() { }
  private static void StaticMethod() { }

  public override int GetHashCode() {
    var box = base.GetHashCode(); // boxing

    Action f = InstanceMethod, g = StaticMethod;
    Func<int>
      g1 = base.GetHashCode,
      g2 = GetHashCode;

    return new[] {
      base.ToString(),
      ToString()
    }.Length;
  }
}

public static class Extensions {
  public static void ExtensionMethod(this I i) { }
}

public static class A {
  private static void ConcreteTypesWithoutOverrides(S s, E e, S? n) {
    var str = "aa" + s + 'a';
    object o1 = s, o2 = n;
    ValueType v1 = s, v2 = n;
    Enum y1 = e, y2 = (E?) e;
    I i1 = s, i2 = (S?) s;

    Action f = s.InstanceMethod;
    Func<int> g = e.GetHashCode;

    s.ExtensionMethod();
    n.ExtensionMethod();
    s.Equals(null);
    n.Equals(null);
    s.GetType();
    n.GetValueOrDefault();

    e.ToString();
    e.GetHashCode(); // fixed in CoreCLR
    e.Equals(e);

    // no boxing
    s.InstanceMethod();
    n?.InstanceMethod();
    s.GetHashCode();
  }

  private static void Struct<TStruct>(TStruct s) where TStruct : struct, I {
    object o1 = s, o2 = (TStruct?) s;
    ValueType v1 = s, v2 = (TStruct?) s;
    I i1 = s, i2 = (TStruct?) s;

    // always
    Action f = s.InstanceMethod;
    s.ExtensionMethod();
    s.GetType();

    // possible
    s.GetHashCode();
    s.ToString();
    s.Equals(null);

    // no boxing
    s.InstanceMethod();
  }

  private static void Unmanaged<TUnmanaged>(TUnmanaged u) where TUnmanaged : unmanaged, I {
    object o1 = u, o2 = (TUnmanaged?) u;
    ValueType v1 = u, v2 = (TUnmanaged?) u;
    I i1 = u, i2 = (TUnmanaged?) u;

    // always
    Action f = u.InstanceMethod;
    u.ExtensionMethod();
    u.GetType();

    // possible
    u.GetHashCode();
    u.ToString();
    u.Equals(null);

    // no boxing
    u.InstanceMethod();
  }

  private static void Nullable<TNullable>(TNullable? n) where TNullable : struct, I {
    object o1 = n;
    ValueType v1 = n;
    I i1 = n;

    // always
    Func<int> f = n.GetHashCode;
    n.ExtensionMethod(); // boxing!
    n.GetType();

    // possible boxing INSIDE Nullable<T>
    n.GetHashCode();
    n.ToString();
    n.Equals(null);

    // no boxing
    n?.InstanceMethod();
  }

  private static void Reference<TReferenceType>(TReferenceType r) where TReferenceType : class, I, new() {
    object o1 = r;
    I i1 = r;

    Action f = r.InstanceMethod;
    r.ExtensionMethod();
    r.GetType();

    // no boxing
    r.GetHashCode();
    r.ToString();
    r.Equals(null);

    // no boxing
    r.InstanceMethod();
  }

  private static void Unconstrained<TUnconstrained>(TUnconstrained u) where TUnconstrained : I, new() {
    object o1 = u;
    I i1 = u;

    Action f = u.InstanceMethod;
    u.ExtensionMethod();
    u.GetType();

    // possible
    u.GetHashCode();
    u.ToString();
    u.Equals(null);

    // no boxing
    u.InstanceMethod();
  }
}