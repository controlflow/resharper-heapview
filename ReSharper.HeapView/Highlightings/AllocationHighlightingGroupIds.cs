using JetBrains.ReSharper.HeapView.Highlightings;
#if RESHARPER8
using JetBrains.ReSharper.Daemon;
#elif RESHARPER9
using JetBrains.ReSharper.Feature.Services.Daemon;
#endif

[assembly: RegisterStaticHighlightingsGroup(
  AllocationHighlightingGroupIds.PERFORMANCE_HINTS, "C# allocation hints", isVisible: true)]

namespace JetBrains.ReSharper.HeapView.Highlightings
{
  public static class AllocationHighlightingGroupIds
  {
    public const string PERFORMANCE_HINTS = "AllocationHints";
  }
}