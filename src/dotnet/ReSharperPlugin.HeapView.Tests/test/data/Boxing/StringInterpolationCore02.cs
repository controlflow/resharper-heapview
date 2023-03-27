[System.Runtime.CompilerServices.InterpolatedStringHandler]
public class CustomHandler
{
  public CustomHandler Usage => $"aaa {42,-1} bbb {true}";

  public CustomHandler(int a, int b) { }

  public void AppendFormatted(object o) { }
  public void AppendFormatted(object o, object alignment) { }
  public void AppendLiteral(string s) { }
}