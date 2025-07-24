using System.Linq;

var ys1 =
  from xxx in args
  join yyy in args on xxx equals yyy
  select xxx + yyy;

var ys2 = args
  .Join(args, xxx => xxx, yyy => yyy, (xxx, yyy) => xxx + yyy);