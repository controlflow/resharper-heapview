using System;

foreach (var x in args)
{
  // note: Action instance is cached inside display class field
  Action action = () => { _ = args; };
}