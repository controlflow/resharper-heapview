record Base(params int[] Items);
record Derived1(int X) : Base; // alloc
record Derived2(int Y) : Base(); // alloc
record Derived3(int Z) : Base(Z); // alloc

namespace System.Runtime.CompilerServices
{
  internal static class IsExternalInit { }
}