var f1 = long (int x) => x;
var f2 = long (ref int x) => x;
object f3 = () => { };
System.Delegate f4 = string () => throw null!;