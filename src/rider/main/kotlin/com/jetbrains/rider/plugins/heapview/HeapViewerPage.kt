package com.jetbrains.rider.plugins.heapview

import com.jetbrains.rider.settings.simple.SimpleOptionsPage

class HeapViewerPage : SimpleOptionsPage(
  name = "Heap Allocations Viewer",
  pageId = "HeapViewer"
) {
  override fun getId(): String {
    return "HeapViewer"
  }
}