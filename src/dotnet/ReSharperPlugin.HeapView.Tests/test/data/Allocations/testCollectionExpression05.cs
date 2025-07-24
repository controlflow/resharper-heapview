using System.Collections.Generic;

// bound
{
  IList<string> list = [..args];
  ICollection<string> collection = [..args];

  IReadOnlyList<string> readOnlyList = [..args];
  IReadOnlyCollection<string> readOnlyCollection = [..args];
  IEnumerable<string> enumerable = [..args];
}

// unbound
{
  IEnumerable<string> xs = args;
    
  IList<string> list = [..xs];
  ICollection<string> collection = [..xs];

  IReadOnlyList<string> readOnlyList = [..xs];
  IReadOnlyCollection<string> readOnlyCollection = [..xs];
  IEnumerable<string> enumerable = [..xs];
}