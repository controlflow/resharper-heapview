using System;

// empty
{
  Span<int> empty1 = [];
  ReadOnlySpan<int> empty2 = [];
}

// bytes
{
  Span<byte> byte1 = [1]; // inline array, mutable
  ReadOnlySpan<byte> byte2 = [2]; // static data
}

// non bytes
{
  Span<int> int1 = [1]; // inline array, mutable
  ReadOnlySpan<int> int2 = [2]; // createspan helper
}