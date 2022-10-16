using static System.Activator;

void Generic<TUnconstrained>()
{
  CreateInstance<int>();
  CreateInstance<string>(); // yes
  CreateInstance<TUnconstrained>(); // possible

  CreateInstance(typeof(int)); // yes
  CreateInstance(typeof(string)); // yes
  CreateInstance(typeof(TUnconstrained)); // yes
}