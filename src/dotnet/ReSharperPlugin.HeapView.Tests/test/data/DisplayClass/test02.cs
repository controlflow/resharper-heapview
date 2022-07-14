Local();

void Local()
{
  Local();

  void Local2()
  {
    Local2();
  }

  Local2();
  var name = nameof(Local2);
}

void Local3()
{
  var f = Local3; // delegate
}