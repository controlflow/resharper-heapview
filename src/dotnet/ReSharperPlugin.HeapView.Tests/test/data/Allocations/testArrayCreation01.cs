// ReSharper disable UnusedVariable
// ReSharper disable UseArrayEmptyMethod
// ReSharper disable RedundantExplicitArrayCreation
// ReSharper disable RedundantExplicitArraySize
// ReSharper disable SuggestVarOrType_BuiltInTypes

var array01 = new byte[0];
var array02 = new byte[4];
var array03 = new byte[4] { 1, 2, 3, 4 };
var array04 = new sbyte[] { 1, 2, 3, 4 };
var array05 = new[] { 1, 2, 3, 4 };
var array06 = new string[] { "aa", "bb", "cc" };
var array07 = new Unresolved[42];
Unresolved array08 = new Unresolved[42];
var array09 = new int[10, 10];

_ = Generic<int>();
T[] Generic<T>() => new T[42];

unsafe
{
  int*[] pointers = new int*[44];
}