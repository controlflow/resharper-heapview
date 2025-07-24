using System.Linq;

var ys1 =
  from xxx in args
  from yyy in args // yes
  from zzz in args // yes
  let lll = zzz // yes
  select xxx + yyy + zzz + lll;

var ys2 = args
  .SelectMany(_ => args, (xxx, yyy) => new { xxx, yyy })
  .SelectMany(_ => args, (t, zzz) => new { t, zzz })
  .Select(t => new { t, lll = t.zzz })
  .Select(t => t.t.t.xxx + t.t.t.yyy + t.t.zzz + t.lll);