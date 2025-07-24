using System;

void EnumConstraint<T>(T e) where T : Enum
{
  object o = e;
  Enum x = e;
}

void EnumConstraint2<T>(T e) where T : struct, Enum
{
  object o = e;
  Enum x = e;
}

void DelegateConstraint<T>(T e) where T : Delegate
{
  object o = e;
  Delegate d = e;
}