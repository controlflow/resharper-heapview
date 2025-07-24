using System;
using System.Collections.Generic;

class Foreach
{
  public void Spans(Span<char> xs)
  {
    foreach (var _ in xs) { }
  }

  public async void Method(IAsyncEnumerable<string> asyncEnumerable)
  {
    await foreach (var _ in asyncEnumerable) { } // alloc
    await foreach (var _ in IteratorMethod()) { }
  }

  private async IAsyncEnumerable<string> IteratorMethod() { yield break; }
}