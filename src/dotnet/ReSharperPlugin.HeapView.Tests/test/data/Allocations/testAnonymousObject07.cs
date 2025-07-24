using System.Linq;

var ys1 =
  from xxx in args
  where xxx.Length > 0
  group xxx.Length + 1 by xxx.Length + 2
  into x
  join y in args on x.Key equals y.Length // alloc
  let t = 1 // alloc
  select y.Length + t;

var ys2 = args
  .Where(xxx => xxx.Length > 0)
  .GroupBy(xxx => xxx.Length + 2, xxx => xxx.Length + 1)
  .Join(args, x => x.Key, y => y.Length, (x, y) => new { x, y })
  .Select(t1 => new { t1, t = 1 })
  .Select(t1 => t1.t1.y.Length + t1.t);