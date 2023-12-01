using System.Collections.Generic;

List<string> structEnumerator = [];
IEnumerable<string> refEnumerator = structEnumerator;

string[] xs = [..structEnumerator, ..refEnumerator, ..Iterator(), ..args];
return;

static IEnumerable<string> Iterator() { yield break; }