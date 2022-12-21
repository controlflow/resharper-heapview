class StringConcat
{
  public string Case01(string s, char c)
  {
    s += c + "" + c;
    s += c;
    return s + c;
  }
}