using static System.Activator;

void Generic<TUnconstrained>()
{
  CreateInstance<int>(); // yes in .NET Framework
  CreateInstance<string>(); // yes
  CreateInstance<TUnconstrained>(); // yes in .NET Framework

  CreateInstance(typeof(int)); // yes
  CreateInstance(typeof(string)); // yes
  CreateInstance(typeof(TUnconstrained)); // yes
}