package com.jetbrains.rider.plugins.heapview

import com.jetbrains.rider.settings.simple.SimpleOptionsPage

class HeapViewerPage : SimpleOptionsPage(
  name = HeapViewerBundle.message("configurable.name.heapviewer.options.title"),
  pageId = "HeapViewer"
) {
  override fun getId(): String {
    return "HeapViewer"
  }
}