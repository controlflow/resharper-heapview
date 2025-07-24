// all optimized in C# 8.0 compiler
class StringConcat {
  string Char(string s) => s + 'a'; // + "a"
  string Char(string s, char c) => s +  c; // char.ToString()
  
  string Int32(string s) => s + 1; // int.ToString()
  string Int32(string s, int i) => s + (object) i; // int.ToString()

  string Boolean(string s, bool b) => b + s; // bool.ToString()
    
  string NoOverride(string s, NoToString n) => s + n; // constrained. callvirt
  string HasOverride(string s, HasToString h) => s + h; // constrained. callvirt
    
  string Generic<T>(string s, T t) where T : struct => s + t; // constrained. callvirt
  string Nullable<T>(string s, T? t) where T : struct => s + t; // constrained. callvirt

  string Compound<T>(string s, char c, int i, bool b, T? n, T t) where T : struct {
    s += 'a';
    s += c;
    s += i;
    s += b;
    s += n;
    s += t;
    return $"id: {i} {s}"; // box
  }
}

struct NoToString { }
struct HasToString { public override string ToString() => ""; }