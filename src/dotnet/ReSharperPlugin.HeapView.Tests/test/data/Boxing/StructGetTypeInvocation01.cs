var s = new S();
_ = s.GetType();

S? ns = s;
_ = ns.GetType();
_ = ns?.GetType();

var rs = new RS();
_ = rs.GetType();

struct S { }
record struct RS { }