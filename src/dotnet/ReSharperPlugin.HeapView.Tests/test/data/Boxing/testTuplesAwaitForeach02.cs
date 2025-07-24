#nullable enable
using System.Collections.Generic;
// ReSharper disable MoreSpecificForeachVariableTypeAvailable

IAsyncEnumerable<int> xs = null!;

await foreach (object x in xs) { }