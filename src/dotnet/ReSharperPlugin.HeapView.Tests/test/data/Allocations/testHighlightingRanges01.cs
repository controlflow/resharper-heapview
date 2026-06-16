// ReSharper disable RedundantStringInterpolation
// ReSharper disable RawStringCanBeSimplified

Params(
  """
  aaa
  bbbbbb
  cccc
  """);
Params("very long string literal");
Params(
  $"""
  aaa
  bbbbbb{args}
  cccc
  """);
Params(
  $$$$$"""""
  aaa
  bbbbbb{args}
  cccc
  """"");

return;

void Params(params string[] xs) { }