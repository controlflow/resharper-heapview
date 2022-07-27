using System.Linq;

var xs = new int[0];

var ys = from object o in xs
         from object v in xs
         join object u in xs on o equals u
         select o;