using System;

void Complex<T>(T u)
{
  var s = (1, true, (5, u, ConsoleKey.B), "abc");
  (int, object, (int, object, Enum), object) t = s;
  t = s;
  (int, bool, object, string) t2 = s;
  (int, object, Enum) tt;
  (int _, _, tt, string _) = s;
}