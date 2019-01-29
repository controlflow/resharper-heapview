using JetBrains.ReSharper.Feature.Services.Daemon;
using ReSharperPlugin.HeapView.Highlightings;

[assembly: RegisterConfigurableHighlightingsGroup(
  HeapViewHighlightingsGroupIds.ID, "[Heap Allocations Plugin] Allocation hints")]

namespace ReSharperPlugin.HeapView.Highlightings
{
  public static class HeapViewHighlightingsGroupIds
  {
    public const string ID = "AllocationHints";
  }
}