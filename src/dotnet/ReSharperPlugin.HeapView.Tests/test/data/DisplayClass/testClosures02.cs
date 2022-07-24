var local = 0;
var other = 0;
var f = (int parameter) => parameter + local + nameof(other);
var g = () =>
{
  var inner = other;
  return nameof(local) + inner;
};