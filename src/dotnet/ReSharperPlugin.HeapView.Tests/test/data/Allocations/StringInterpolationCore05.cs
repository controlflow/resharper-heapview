using System.Runtime.CompilerServices;
// ReSharper disable UnusedVariable

public class StringInterpolationExpression
{
  public void Method(int value)
  {
    CustomInterpolationHandler01 hander01 = $"aaa"; // alloc, custom handler creation
    CustomInterpolationHandler01 hander02 = $"bbb{value}"; // alloc, boxing
    CustomInterpolationHandler02 hander03 = $"aaa";
    CustomInterpolationHandler02 hander04 = $"bbb{value}"; // boxing
  }
}

[InterpolatedStringHandler]
class CustomInterpolationHandler01 {
  public CustomInterpolationHandler01(int a, int b) { }
  public void AppendLiteral(string part) { }
  public void AppendFormatted(object box) { }
}

[InterpolatedStringHandler]
struct CustomInterpolationHandler02 {
  public CustomInterpolationHandler02(int a, int b) { }
  public void AppendLiteral(string part) { }
  public void AppendFormatted(object box) { }
}