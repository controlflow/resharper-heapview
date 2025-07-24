class Generics
{
  public T Test01<T>() where T : new() => new T(); // possible
  public T Test02<T>() where T : new() => new(); // possible
  public T Test04<T>() where T : class, new() => new(); // yes
  public T Test05<T>() where T : struct => new();
  public T Test06<T>() where T : unmanaged => new();
}