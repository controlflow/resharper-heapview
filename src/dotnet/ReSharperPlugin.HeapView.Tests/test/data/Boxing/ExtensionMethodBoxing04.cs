using System;
using System.Runtime.CompilerServices;

await new CustomAwaitable();

public interface IFoo : INotifyCompletion
{
  bool IsCompleted { get; }
  void GetResult();
}

public struct CustomAwaitable : IFoo
{
  public bool IsCompleted => true;
  public void GetResult() { }
  public void OnCompleted(Action continuation) { }
}

public static class Extensions
{
  public static IFoo GetAwaiter(this IFoo foo) => foo;
}