using JetBrains.ReSharper.HeapView.Highlightings;
using JetBrains.ReSharper.Feature.Services.Daemon;

[assembly: RegisterConfigurableHighlightingsGroup(
  HeapViewHighlightingsGroupIds.ID, "[Heap Allocations Plugin] Allocation hints")]

namespace JetBrains.ReSharper.HeapView.Highlightings
{
  public static class HeapViewHighlightingsGroupIds
  {
    public const string ID = "AllocationHints";
  }
}