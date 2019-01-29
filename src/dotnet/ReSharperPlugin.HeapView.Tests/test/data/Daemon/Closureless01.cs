using System;

class Foo {
  public void LambdaLifting(ITreeNode node, CachedValue<string> cachedValue) {
    var str0 = cachedValue.GetOrCreate(node, 4222, (_, __) => _.ToString());
    var str1 = cachedValue.GetOrCreate(node, _ => _.ToString());
    var str2 = cachedValue.GetOrCreate(node, _ => node.ToString());
    var str3 = cachedValue.GetOrCreate(() => node.ToString());
  }
}

class CachedValue<T> {
  public T GetOrCreate(Func<T> func) => default(T);
  public T GetOrCreate<TState>(TState state, Func<TState, T> func) => default(T);
  public T GetOrCreate<TState1, TState2>(TState1 state1, TState2 state2, Func<TState1, TState2, T> func) => default(T);
}

interface ITreeNode { }