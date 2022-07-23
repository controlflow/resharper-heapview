using System.Linq;

static ref int LocalFunction(ref int x) => ref x;

var f = static int (bool x) =>
{
  var l = (ref bool b) => ref x;
  return x.GetHashCode();
};

var d = delegate(int i) { return i; }

var xs = from x in new int[0]
         select x + 1;