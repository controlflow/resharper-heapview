using JetBrains.ReSharper.HeapView.Highlightings;
#if RESHARPER8
using JetBrains.ReSharper.Daemon;
#elif RESHARPER9
using JetBrains.ReSharper.Feature.Services.Daemon;
#endif

[assembly: RegisterConfigurableHighlightingsGroup(
  AllocationHighlightingGroupIds.ID, "[Heap Allocations Plugin] Allocation hints")]

namespace JetBrains.ReSharper.HeapView.Highlightings
{
  public static class AllocationHighlightingGroupIds
  {
    public const string ID = "AllocationHints";
  }
}