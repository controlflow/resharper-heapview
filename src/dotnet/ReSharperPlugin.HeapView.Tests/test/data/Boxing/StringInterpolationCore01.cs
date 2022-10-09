class StringInterpolation
{
  public string Empty() => $"id";
  public string Simple(int id) => $"id: {id}";
  public string TwoArgs(int id) => $"id: {id} id2: {id}";
  public string ThreeArgs(int id) => $"id: {id} id2: {id} id3: {id}";
  public string FourArgs(int id) => $"id: {id} id2: {id} id3: {id} id4: {id}";
  public string Broken(int id) => $"id: {id} id2: {id} id3: {}"; // todo: fix in R# PSI
}