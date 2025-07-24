var foo1 = new { }; // yes
var foo2 = new { A = 42 }; // yes
var foo3 = new { A = 42, B = true }; // yes

if (args.Length == 1)
{
  var box = new { Message = "aaa" };
  throw new System.InvalidOperationException();
}