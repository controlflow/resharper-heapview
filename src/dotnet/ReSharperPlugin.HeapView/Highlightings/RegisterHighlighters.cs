using JetBrains.TextControl.DocumentMarkup;

namespace ReSharperPlugin.HeapView.Highlightings;

public class HeapViewSettingsNamesProvider : PrefixBasedSettingsNamesProvider
{
  public HeapViewSettingsNamesProvider()
    : base("ReSharper HeapView", "HEAP_VIEW") { }
}