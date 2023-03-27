using System;
using System.Collections.Generic;

var holder = new DataHolder();

var list = new List<bool>();

string[] Local() => args;

_ = holder.VeryGeneric(42, list, (string t2, int x) => args, 'a'); // warn
_ = holder.VeryGeneric(42, list, (string t2, int x) => Local(), 'a');

class DataHolder
{
  public TResult VeryGeneric<T1, T2, TExtra, TResult>(int key, List<T1> list, Func<T2, int, TResult> factory, TExtra extra) => default!;
  public TResult VeryGeneric<T1, T2, TExtra, TState, TResult>(int key, List<T1> list, TState state, Func<T2, int, TState, TResult> factory, TExtra extra) => default!;
}