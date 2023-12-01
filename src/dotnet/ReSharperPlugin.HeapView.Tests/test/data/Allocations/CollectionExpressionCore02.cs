using System;

// non-constant
{
  Span<int> nonConst1 = [args.Length]; // inline array
  ReadOnlySpan<int> nonConst2 = [args.Length]; // inline array
}

// non-blittable
{
  Span<string> str1 = ["a"]; // inline array
  ReadOnlySpan<string> str2 = ["b"]; // inline array
}

// spread w/ count
{
  Span<string> str1 = [..args]; // heap array
  ReadOnlySpan<string> str2 = [..args]; // heap array
}

// spread unbound
{
  System.Collections.Generic.IEnumerable<string> xs = args;
  Span<string> str1 = [..xs]; // temp list + heap array
  ReadOnlySpan<string> str2 = [..xs]; // temp list + heap array
  ReadOnlySpan<string> str3 = ["", ..xs]; // temp list + heap array
}