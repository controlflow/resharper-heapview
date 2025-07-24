class Tuples
{
  void M((int, int) tuple)
  {
    (object o, int x) = tuple;
    var casted = ((object, int)) tuple;
  }
}