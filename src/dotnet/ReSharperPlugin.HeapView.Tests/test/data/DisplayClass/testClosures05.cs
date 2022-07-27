bool F(object o) => true;

var a = 0;
if (a > 0)
  if (a is var t1)
    _ = a is var t2
        && F(() => t2 + a)
        && F(() => t1 + t2 + a);