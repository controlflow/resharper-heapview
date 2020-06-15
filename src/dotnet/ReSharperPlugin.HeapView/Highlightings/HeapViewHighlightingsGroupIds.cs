using JetBrains.ReSharper.Feature.Services.Daemon;

namespace ReSharperPlugin.HeapView.Highlightings
{
  [RegisterConfigurableHighlightingsGroup(
    ID, "[Heap Allocations Plugin] Allocation hints")]
  public static class HeapViewHighlightingsGroupIds
  {
    public const string ID = "AllocationHints";
  }
}
