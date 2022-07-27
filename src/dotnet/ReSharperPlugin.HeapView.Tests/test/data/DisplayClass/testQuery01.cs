using System.Linq;

var xs1 = from x in args
          select x;

var xs2 = from x in args
          from y in args
          select x + y;

var xs3 = from x in args
          from y in args
          from z in args
          select x + y + z;