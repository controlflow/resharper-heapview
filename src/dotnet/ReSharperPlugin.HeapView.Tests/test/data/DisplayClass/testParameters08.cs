static bool F(Func<int> f) => true;

Func<int, bool> lambda1 = (int parameter) =>
  parameter is var local && F(() => parameter + local + 1);

Func<int, bool> lambda2 = (int parameter) =>
{
  var local = parameter;
  return F(() => parameter + local + 2);
};
  
Func<int, bool> anonymous = delegate (int parameter)
{
  var local = parameter;
  return F(() => parameter + local + 3);
};