// display class {foo}
int foo = 0;

if (args.Length > 0)
{
  // display class {bar}
  int bar = 1;

  // display class {boo}
  {
    int boo = 2;
    var g = () => boo;
  }

  // display class {zzz+{bar}}
  {
    int zzz = 4;
    var gg = () => zzz + bar;
    var f = () => foo + 111;
  }
}
else
{
  // display class {blah+{foo}}
  {
    int blah = 3;
    var ff = () => foo + blah;
  }
}