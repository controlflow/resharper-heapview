using System.Linq;

var ys1 =
  from xxx in args
  select (from yyy in args
          select xxx + yyy);

var ys2 = args
  .Select(xxx =>
    args.Select(yyy => xxx + yyy));