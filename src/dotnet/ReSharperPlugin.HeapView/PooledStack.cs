using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.Util.DataStructures.Specialized;

namespace ReSharperPlugin.HeapView;

public sealed class PooledStack<T> : Stack<T>, IDisposable
{
  private readonly ObjectPool<PooledStack<T>> myPool;
  private static readonly ObjectPool<PooledStack<T>> GlobalPool = CreatePool();

  [Pure]
  public static PooledStack<T> GetInstance()
  {
    var instance = GlobalPool.Allocate();
    Assertion.Assert(instance.Count == 0, "Pooled collection is not clean");
    return instance;
  }

  private PooledStack(ObjectPool<PooledStack<T>> pool)
  {
    myPool = pool;
  }

  public void Dispose()
  {
    if (Count > 1024)
    {
      myPool.Forget(this);
    }
    else
    {
      Clear();
      myPool.Return(this);
    }
  }

  [Pure, PublicAPI]
  public static ObjectPool<PooledStack<T>> CreatePool() => new(static p => new PooledStack<T>(p));
}