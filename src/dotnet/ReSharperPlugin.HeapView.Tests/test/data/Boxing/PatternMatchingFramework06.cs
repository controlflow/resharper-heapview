class TypeTests
{
  public bool Test01<TSource, TTarget>(TSource t) => t is TTarget; // possible
  public bool Test02<TSource, TTarget>(TSource t) where TSource : struct => t is TTarget; // yes
  public bool Test03<TSource, TTarget>(TSource t) where TSource : class => t is TTarget;

  // where TTarget : struct
  public bool Test04<TSource, TTarget>(TSource t) where TTarget : struct => t is TTarget; // possible
  public bool Test05<TSource, TTarget>(TSource t) where TSource : struct where TTarget : struct => t is TTarget; // yes
  public bool Test06<TSource, TTarget>(TSource t) where TSource : class where TTarget : struct => t is TTarget;

  // where TTarget : class
  public bool Test07<TSource, TTarget>(TSource t) where TTarget : class => t is TTarget; // possible
  public bool Test08<TSource, TTarget>(TSource t) where TSource : struct where TTarget : class => t is TTarget; // yes
  public bool Test09<TSource, TTarget>(TSource t) where TSource : class where TTarget : class => t is TTarget;
}