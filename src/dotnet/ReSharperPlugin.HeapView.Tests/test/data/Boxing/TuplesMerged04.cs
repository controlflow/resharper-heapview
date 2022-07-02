void AllPossible<T>(T u)
{
  var s = (u, u);
  (object, object) t = s;
  t = s;
}

void Partial<T>(T u)
{
  var s = (u, 1);
  (object, object) t = s;
  t = s;
}