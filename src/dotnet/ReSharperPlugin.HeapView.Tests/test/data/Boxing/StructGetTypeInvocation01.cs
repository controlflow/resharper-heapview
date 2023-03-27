var s = new S();
_ = s.GetType();
_ = s.GetType(42);

S? ns = s;
_ = ns.GetType();
_ = ns?.GetType();

var rs = new RS();
_ = rs.GetType();

var re = new REF();
_ = re.GetType();

struct S { }
record struct RS { }
ref struct REF { }