// ReSharper disable ConvertToLocalFunction

ParamsDelegate func = _ => { };
func(1 + 2, 2, 3); // alloc
func(int.MaxValue); // alloc
func();

public delegate void ParamsDelegate(params int[] xs);