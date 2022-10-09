// ReSharper disable ReplaceConditionalExpressionWithNullCoalescing

class Unboxing1<T> where T : struct
{
  public I Cast(T t) => (I) t; // yes
  public I TryCast(T t) => t as I; // yes
  public bool TypeTest(T t) => t is I; // yes
  public I Pattern(T t) => t is I i ? i : null; // yes
}

class Unboxing2<T>
{
  public I Cast(T t) => (I) t; // possible
  public I TryCast(T t) => t as I; // possible
  public bool TypeTest(T t) => t is I; // possible
  public I Pattern(T t) => t is I i ? i : null; // possible
}

interface I { }
struct S : I { }