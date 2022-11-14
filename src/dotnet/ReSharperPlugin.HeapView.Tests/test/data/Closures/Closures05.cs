var f1 = (int x) => args.Length;
var f2 = long (ref int x) => args.Length;
object f3 = () => args.Length;
System.Delegate f4 = (bool f) => f ? args.Length : throw null!;