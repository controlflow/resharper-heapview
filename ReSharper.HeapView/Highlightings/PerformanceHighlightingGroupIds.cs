using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.HeapView.Highlightings;

[assembly: RegisterStaticHighlightingsGroup(
  PerformanceHighlightingGroupIds.PERFORMANCE_HINTS, "C# performance hints", isVisible: true)]

namespace JetBrains.ReSharper.HeapView.Highlightings
{
  public static class PerformanceHighlightingGroupIds
  {
    public const string PERFORMANCE_HINTS = "PerformanceHints";
  }
}