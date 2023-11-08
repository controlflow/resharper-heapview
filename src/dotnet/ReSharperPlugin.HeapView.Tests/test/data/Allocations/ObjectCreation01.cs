// ReSharper disable ArrangeObjectCreationWhenTypeNotEvident

var c = new C();
c = new();

var s = new S { };
s = new();

S? ns = new();
ns = new();

var r = new R();
r = new();

var rs = new RS();
rs = new();

class C { }
struct S { }
record R;
record struct RS;