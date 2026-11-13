Carved Kingdoms: Base Set
=========================
Odd Jelly Games - oddjellygames.com - assets@oddjellygames.com
Version 1.1, 2026-11-08. 15 pieces of its own, plus the shared ones in Common.

START HERE (UNITY 6, URP)
  1. Use a project on the Universal Render Pipeline, such as the Universal 3D template.
  2. Assets > Import Package > Custom Package, pick CarvedKingdoms_Base.unitypackage and import everything.
  3. The welcome window opens once (later: Tools > Carved Kingdoms > Welcome). Press its button to add the edge lines
     to your renderer.
  4. Open CarvedKingdoms/Kingdom/Scenes/Kingdom_Demo and press Play. Drag or WASD to move, the wheel to zoom, Q and
     E to turn, R for another island. Kingdom_Overview shows every piece by name; press Play there to see the
     animations.

PLACING PIECES
  Drag a prefab into the scene. Tools > Carved Kingdoms > Snap selection to the hex grid (Alt+Shift+G) puts it on a
  cell, Turn selection 60 degrees (Alt+Shift+R) turns it so rivers, roads and coasts meet their neighbours.
  Kingdom_Board is an open board of plain tiles to start from. From code, use the HexBoard component.
  Team colours: the CarvedTeam component on every prefab.
  Animated pieces: GetComponent<CarvedUnit>().Play("Attack"). The Overview names every animation.

THREE LOOKS
  Wooden (the default), Outlined and Plain: tick them on the Carved Look component of the Look object in each scene,
  or press O and P in the demo.

MORE THAN ONE PACK
  Every pack carries the same Common folder (ground, rivers, roads, coasts, props), so packs sit side by side in one
  project.
  The Kingdom pack contains everything in the Base Set: with Kingdom in your project you do not need this one.

WHAT IS IN HERE
  Tiles/Places/        3  premade tiles of the pack's places
  Kit/Animals/        12  animals, animated

LICENCE
  Bought on the Unity Asset Store: the Asset Store's standard EULA applies.
  Questions: assets@oddjellygames.com
