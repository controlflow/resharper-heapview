using System;

static bool F(Func<int> f) => true;

bool Local1(int parameter)
  => parameter is var local && F(() => parameter + local + 1);

bool Local2(int parameter)
{ 
  var local = parameter;
  return F(() => parameter + local + 2);
}