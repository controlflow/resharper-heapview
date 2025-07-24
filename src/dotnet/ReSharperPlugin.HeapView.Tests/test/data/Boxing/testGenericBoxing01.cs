void F<TValue, TClass, TAny>(TValue val, TClass cl, TAny any, TValue? nul)
  where TValue : struct
  where TClass : class
{
  object o1 = val;
  object o2 = cl;
  object o3 = any;
  object o4 = nul;
}