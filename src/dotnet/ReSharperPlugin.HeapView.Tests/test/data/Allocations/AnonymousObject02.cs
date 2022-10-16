using System.Linq;

var ys1 =
  from xxx in args
  from yyy in args // yes
  from zzz in args
  select xxx + yyy + zzz;

var ys2 = args
  .SelectMany(_ => args, (xxx, yyy) => new { xxx, yyy })
  .SelectMany(_ => args, (t, zzz) => t.xxx + t.yyy + zzz);