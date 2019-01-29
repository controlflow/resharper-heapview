using System;

class Foo {
  public void LambdaLifting(ITreeNode node, CachedValue cachedValue) {
    // no
    var str1 = cachedValue.GetOrCreate(() => node.ToString());
    var str2 = cachedValue.GetOrCreate2(() => node.ToString());
    // yes
    var str3 = cachedValue.GetOrCreate3(() => node.ToString());
    var str4 = cachedValue.GetOrCreate4(() => node.ToString());
  }
}

class CachedValue {
  public TResult GetOrCreate<TResult>(Func<TResult> func) => default(TResult);
  public TResult GetOrCreate<TState, TResult>(ref TState state, Func<TState, TResult> func) => default(TResult);
  public TResult GetOrCreate<TState, TResult>(TState state, Func<TResult, TState> func) => default(TResult);
  public static TResult GetOrCreate<TState, TResult>(Func<TState, TResult> func, TState state) => default(TResult);

  public TResult GetOrCreate2<TResult>(Func<TResult> func) => default(TResult);
  public TResult GetOrCreate2<TState, TResult>(ref TState state, Func<TState, TResult> func) => default(TResult);
  public TResult GetOrCreate2<TState, TResult>(TState state, Func<TResult, TState> func) => default(TResult);
  public TResult GetOrCreate2<TState, TResult>(ref Func<TState, TResult> func, TState state) => default(TResult);
  public TResult GetOrCreate2<TState, TResult>(Func<TState, TResult> func, out TState state) => default(TResult);
  public TResult GetOrCreate2<TState, TResult>(Func<TState, TResult, int> func, TState state) => default(TResult);

  public TResult GetOrCreate3<TResult>(Func<TResult> func) => default(TResult);
  public TResult GetOrCreate3<TState, TResult>(Func<TState, TResult> func, TState state) => default(TResult);

  public TResult GetOrCreate4<TResult>(Func<TResult> func) => default(TResult);
  public TResult GetOrCreate4<TState, TState2, TResult>(
    TState state, TState2 state2, Func<TState, TState2, TResult> func) => default(TResult);
}

interface ITreeNode { }