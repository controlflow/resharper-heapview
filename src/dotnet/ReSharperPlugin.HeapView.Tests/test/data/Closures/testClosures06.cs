string s = "sss";
string u = "uuu";
string x = "xxx";
int i = 42;
var tup = (42, true, "ref");

var f1 = () => s + u + i + tup;
f1();

{
  var t = "ttt";
  var f2 = () => x + t;
  f2();
}