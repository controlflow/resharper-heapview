using System;
using System.Collections.Generic;
using System.Linq;

var xs = new List<ConsoleKey>();

var ys = from ValueType o in xs
         from Enum v in xs
         join object u in xs on o equals u
         select o;