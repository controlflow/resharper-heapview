using System;

var holder = new DataHolder();

_ = holder.NonGeneric01(() => args.Length); // warn
_ = holder.NonGeneric01(args, a => a.Length);

_ = holder.NonGeneric02(() => args.Length); // warn
_ = holder.NonGeneric02(a => a.Length, args);

_ = holder.NonGeneric03("aa", () => args.Length); // warn
_ = holder.NonGeneric03("aa", args, a => args.Length); // warn

_ = holder.WasGeneric(() => args); // warn


class DataHolder
{
  public int NonGeneric01(Func<int> factory) => 0;
  public int NonGeneric01<TState>(TState t, Func<TState, int> factory) => 0;

  public int NonGeneric02(Func<int> factory) => 0;
  public int NonGeneric02<TState>(Func<TState, int> factory, TState t) => 0;

  public int NonGeneric03(string key, Func<int> factory) => 0;
  public int NonGeneric03<TState>(string key, TState t, Func<TState, int> factory) => 0;
  public int NonGeneric03<TState1, TState2>(string key, TState1 t1, TState2 t2, Func<TState1, TState2, int> factory) => 0;

  public T WasGeneric<T>(Func<T> factory) => default!;
  public T WasGeneric<T, TState>(TState t, Func<TState, T> factory) => default!;
}