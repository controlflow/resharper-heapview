class SomeClass
{
  public static implicit operator (int, bool)(SomeClass someClass) => (1, true);
  public static explicit operator SomeClass((object, int) t) => default!;

  public void Bar(SomeClass someClass, (short, byte) t)
  {
    (object, object) oo = someClass;
    var s = (SomeClass)t;
  }
}

struct SomeStruct
{
  public static implicit operator (int, bool)(SomeStruct someStruct) => (1, true);
  public static explicit operator SomeStruct((object, int) t) => default;

  public void M((short, byte) t)
  {
    (object, object) oo = this;
    var s = (SomeStruct)t;
  }
}