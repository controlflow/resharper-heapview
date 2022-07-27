using System.Linq;

var xs = args;
var t = from x in xs
        join z in xs on x equals z
        select x + z;