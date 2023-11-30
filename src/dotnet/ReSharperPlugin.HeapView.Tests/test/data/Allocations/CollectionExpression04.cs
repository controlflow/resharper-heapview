﻿using System.Collections.Generic;

{
  // new List<T> { x }
  IList<int> list = [1];
  ICollection<int> collection = [2];

  // new Wrapper(new[] { x })
  IReadOnlyList<int> readOnlyList = [3];
  IReadOnlyCollection<int> readOnlyCollection = [4];
  IEnumerable<int> enumerable = [5];
}

{
  // new List<T>
  IList<int> list = [];
  ICollection<int> collection = [];

  // Array.Empty<T>
  IReadOnlyList<int> readOnlyList = [];
  IReadOnlyCollection<int> readOnlyCollection = [];
  IEnumerable<int> enumerable = [];
}