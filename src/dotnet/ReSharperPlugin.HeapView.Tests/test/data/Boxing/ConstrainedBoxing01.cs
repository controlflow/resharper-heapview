class Constrained<T>
{
  public int Test01(T t) => typeof(T) == typeof(int) ? (int) (object?) t! : -1;
  public int Test02(T t) => typeof(T) == typeof(string) ? (string) (object?) t! : "-1";
}