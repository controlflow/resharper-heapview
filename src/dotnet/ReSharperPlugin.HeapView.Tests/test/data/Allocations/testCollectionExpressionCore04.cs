using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

{
  MyCollection<int> xs1 = [];
  MyCollection<int> xs2 = [1111];
  MyCollection<int> xs3 = [1111, 2222];
  MyCollection<string> xs4 = ["aaa"];
}

{
  MyCollection<int> xs1 = [args.Length];
  MyCollection<int> xs2 = [..xs1];
  MyCollection<int> xs3 = [..xs1, 1111];
}

[CollectionBuilder(typeof(MyCollection), nameof(MyCollection.Factory))]
public class MyCollection<T> : List<T>;

public static class MyCollection {
  public static MyCollection<T> Factory<T>(ReadOnlySpan<T> xs) => new();
}

namespace System.Runtime.CompilerServices
{
  public sealed class CollectionBuilderAttribute(Type builderType, string methodName) : Attribute;
}