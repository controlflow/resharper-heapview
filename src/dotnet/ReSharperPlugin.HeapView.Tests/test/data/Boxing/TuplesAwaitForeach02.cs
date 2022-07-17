using System.Collections.Generic;

IAsyncEnumerable<int> xs = null;

await foreach (object x in xs) { }
