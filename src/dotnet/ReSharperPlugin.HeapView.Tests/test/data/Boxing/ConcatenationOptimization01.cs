string s = null;

s = s + 42;
s = true + s;
s += checked(42);
s += (object) 42L;

int? n = 42;
s = s + n;
s += n;

s = s + new S1();
s = new S1() + s;
s += new S1();
s += (object) new S1();

s = s + new S2();
s = new S2() + s;
s += new S2();

s += System.DateTime.Now;

string F<TUnconstrained>(TUnconstrained t, string s) => s + t;

struct S1 { }
struct S2 { public override string ToString() => ""; }