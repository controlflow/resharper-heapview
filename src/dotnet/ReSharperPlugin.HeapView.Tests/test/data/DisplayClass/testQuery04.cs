using System.Linq;

_ = from x in args group x by x + 1;