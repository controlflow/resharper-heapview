// conditional scopes from expression statement
bool F(object o) => true;

var a = 0;
if (a > 0)
  _ = a is var t1 && F(() => t1);

if (a > 0)
  _ = a is var t2 && F(() => t2 + a);

_ = a is var t3 && F(() => t3 + a);