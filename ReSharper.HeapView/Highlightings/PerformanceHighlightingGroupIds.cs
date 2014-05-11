using JetBrains.ReSharper.HeapView.Highlightings;
#if RESHARPER8
using JetBrains.ReSharper.Daemon;
#elif RESHARPER9
using JetBrains.ReSharper.Feature.Services.Daemon;
#endif

[assembly: RegisterStaticHighlightingsGroup(
  PerformanceHighlightingGroupIds.PERFORMANCE_HINTS, "C# performance hints", isVisible: true)]

namespace JetBrains.ReSharper.HeapView.Highlightings
{
  public static class PerformanceHighlightingGroupIds
  {
    public const string PERFORMANCE_HINTS = "PerformanceHints";
  }
}