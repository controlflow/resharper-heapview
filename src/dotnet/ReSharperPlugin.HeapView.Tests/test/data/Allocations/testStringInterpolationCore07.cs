using System.Runtime.CompilerServices;
// ReSharper disable RedundantStringInterpolation

class Interpolation
{
  public void Overloaded(string text) { }
  public void Overloaded(CustomInterpolationHandler handler) { }

  public void Usage(int id, string s)
  {
    Overloaded($"aaa");
    Overloaded("aaa" + $"bbb");
    Overloaded($"aaa" + "bbb");
    Overloaded($"aaa" + $"bbb");
    Overloaded($"aaa" + $"bbb{id}"); // alloc handler, boxing
    Overloaded($"aaa{id}" + $"bbb"); // alloc handler, boxing
    Overloaded($"aaa" + $"bbb" + $"ccc{s}"); // alloc handler
    Overloaded($"aaa{id}" + $"bbb" + "ccc"); // alloc string
  }
}

[InterpolatedStringHandler]
class CustomInterpolationHandler
{
  public CustomInterpolationHandler(int a, int b) { }
  public void AppendLiteral(string part) { }
  public void AppendFormatted(object box) { }
}