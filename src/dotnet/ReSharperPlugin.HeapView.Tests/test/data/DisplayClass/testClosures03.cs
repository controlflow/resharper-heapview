try
{
  System.Console.ReadLine();
}
catch (System.Exception catchVariable)
{
  string localVariable = "aaa";
  const string constant = "bbb";
  var f = delegate () { return catchVariable + localVariable + constant; };
}