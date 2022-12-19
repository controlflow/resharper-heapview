// ReSharper disable UseArrayEmptyMethod
// ReSharper disable RedundantExplicitParamsArrayCreation
// ReSharper disable ExpressionIsAlwaysNull

unsafe
{
  // no args
  Safe();
  Unsafe1(); // alloc
  Unsafe2();
  Unsafe3(); // alloc

  // expanded args
  Safe(1); // alloc
  Safe(1, 2); // alloc
  int* ptr = null;
  Unsafe1(ptr); // alloc
  Unsafe1(null, ptr); // alloc
  var ptrArray = new int*[0];
  Unsafe2(ptrArray); // alloc

  // explicit array passed
  var array = new int[0];
  Safe(array);
  var pointers = new int*[0];
  Unsafe1(pointers);
  Unsafe2(new int*[0][]);
  Unsafe3(new delegate*<void>[0]);

  Safe(null);
  Unsafe1(null);
  Unsafe2(null);
  Unsafe3(null);

  void Safe(params int[] xs) { }
  void Unsafe1(params int*[] xs) { }
  void Unsafe2(params int*[][] xs) { }
  void Unsafe3(params delegate*<void>[] xs) { }
}