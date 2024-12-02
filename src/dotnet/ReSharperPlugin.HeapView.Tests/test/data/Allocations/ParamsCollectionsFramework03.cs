using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
// ReSharper disable RedundantExplicitParamsArrayCreation

var collection = new MyCollection<int>();

ParamsBuilder<int>();
ParamsBuilder<int>([]);
ParamsBuilder(1);
ParamsBuilder([1]);
ParamsBuilder(1, 2, 3);
ParamsBuilder([1, 2, 3]);
ParamsBuilder("abc");
ParamsBuilder(["abc"]);
ParamsBuilder(collection);
ParamsBuilder("abc", args[0]);
ParamsBuilder(["abc", args[0]]);
return;

void ParamsBuilder<T>(params MyCollection<T> xs)
{
  _ = xs;
}

[CollectionBuilder(typeof(MyCollection), nameof(MyCollection.Create))]
public class MyCollection<T> : List<T>;

public static class MyCollection
{
  public static MyCollection<T> Create<T>(ReadOnlySpan<T> xs) => new();
}

namespace System.Runtime.CompilerServices
{
  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, Inherited = false)]
  public sealed class CollectionBuilderAttribute(Type builderType, string methodName) : Attribute
  {
    public Type BuilderType { get; } = builderType;
    public string MethodName { get; } = methodName;
  }
}