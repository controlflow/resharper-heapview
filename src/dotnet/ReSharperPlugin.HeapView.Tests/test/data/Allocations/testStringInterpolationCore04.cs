using System.Threading.Tasks;
// ReSharper disable UnusedVariable

public class StringInterpolationExpression
{
  private const string ConstPart = "const";

  public async void Method(int value, Task<string> task)
  {
    var string01 = $"value={value}"; // alloc, handler
    var string02 = $"text1={await task} boxin={value}"; // alloc, format, boxing
  }
}