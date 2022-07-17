using System.Collections.Generic;

IAsyncEnumerable<(int, int, bool)> xs = null;

await foreach ((object a, object _, object b) in xs) { }
