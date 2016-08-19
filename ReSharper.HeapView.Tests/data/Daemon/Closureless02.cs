using System;

class Foo {
  public void LambdaLifting(ITreeNode node, CachedValue cachedValue) {
    var str0 = cachedValue.GetOrCreate(node, 1, (_, __) => _.ToString());
    var str1 = cachedValue.GetOrCreate(node, _ => _.ToString());
    var str2 = cachedValue.GetOrCreate(node, _ => node.ToString());
    var str3 = cachedValue.GetOrCreate(() => node.ToString());
  }
}

class CachedValue {
  public TResult GetOrCreate<TResult>(Func<TResult> func) => default(TResult);
  public TResult GetOrCreate<TState, TResult>(TState state, Func<TState, TResult> func) => default(TResult);
  public TResult GetOrCreate<TState1, TState2, TResult>(
    TState1 state1, TState2 state2, Func<TState1, TState2, TResult> func) => default(TResult);
}

interface ITreeNode { }