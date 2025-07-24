using System.Linq;

var ys1 =
  from xxx in args
  from yyy in args // alloc
  from zzz in args
  select zzz;

var ys2 = args
  .SelectMany(_ => args, (xxx, yyy) => new { xxx, yyy })
  .SelectMany(_ => args);