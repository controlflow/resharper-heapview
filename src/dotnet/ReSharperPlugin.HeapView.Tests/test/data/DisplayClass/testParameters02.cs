object LocalFunction(int parameter)
{
  int localVariable = 0;
  return () => parameter + localVariable; 
}

class C
{
  public static object operator+(C parameter) => () => parameter;
}