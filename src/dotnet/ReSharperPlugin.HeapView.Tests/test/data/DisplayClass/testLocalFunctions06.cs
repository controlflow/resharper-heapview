// display class #1, optimized
int x = 0;
{
  // display class #2, optimized
  int y = 0;
  int Local() => y++;

  {
    int Other() => Local() + x;
    Other();
  }
}