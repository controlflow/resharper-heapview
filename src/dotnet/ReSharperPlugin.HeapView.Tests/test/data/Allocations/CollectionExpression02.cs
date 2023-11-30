using System;
using System.Collections;
using System.Collections.Generic;

List<int> xs1 = [];
List<int> xs2 = [1];
List<int> xs3 = [12];
List<int> xs4 = [123];
List<int> xs5 = [1234];
List<int> xs6 = [12345,67890];

C1 cs1 = [];
C1 cs2 = ["123"];

C2 ds1 = [];
C2 ds2 = [123, 456];

if (args.Length > 0)
{
  List<bool> xssss = [true, true, false];
  throw new Exception();
}

class C1 : IEnumerable
{
  public void Add(object x) { }
  public IEnumerator GetEnumerator() { yield break; }
}

class C2 : IEnumerable<int>
{
  public void Add(int x) { }
  IEnumerator IEnumerable.GetEnumerator() { yield break; }
  IEnumerator<int> IEnumerable<int>.GetEnumerator() { yield break; }
}