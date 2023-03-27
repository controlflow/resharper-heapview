using JetBrains.ReSharper.Feature.Services.Daemon;

namespace ReSharperPlugin.HeapView.Highlightings;

[RegisterConfigurableHighlightingsGroup(
  Key: ID, Title: "[Heap Allocations Plugin] Allocation hints")]
public static class HeapViewHighlightingsGroupIds
{
  public const string ID = "AllocationHints";
}