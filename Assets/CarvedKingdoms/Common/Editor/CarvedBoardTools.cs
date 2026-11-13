using UnityEditor;
using UnityEngine;

namespace OddJelly.CarvedKingdoms.EditorTools
{
    /// <summary>
    /// Tools for laying a board by hand. They snap the selected tiles onto the hex grid, or turn them one step.
    /// The grid belongs to the HexBoard the tile sits under. If there is none, the grid starts at the world's origin.
    /// </summary>
    public static class CarvedBoardTools
    {
        const string Snap = "Tools/Carved Kingdoms/Snap selection to the hex grid &#g";
        const string Turn = "Tools/Carved Kingdoms/Turn selection 60 degrees &#r";

        [MenuItem(Snap)]
        public static void SnapSelection()
        {
            foreach (var t in Selection.transforms)
            {
                Undo.RecordObject(t, "Snap to the hex grid");
                var board = t.GetComponentInParent<HexBoard>();
                var at = board != null ? board.transform.InverseTransformPoint(t.position) : t.position;
                var cell = HexBoard.CellToLocal(HexBoard.LocalToCell(at));
                cell.y = at.y;
                t.position = board != null ? board.transform.TransformPoint(cell) : cell;
                var angles = t.eulerAngles;
                t.rotation = Quaternion.Euler(0f, Mathf.Round(angles.y / 60f) * 60f, 0f);
            }
        }

        [MenuItem(Turn)]
        public static void TurnSelection()
        {
            foreach (var t in Selection.transforms)
            {
                Undo.RecordObject(t, "Turn 60 degrees");
                t.rotation = Quaternion.AngleAxis(60f, Vector3.up) * t.rotation;
            }
        }

        [MenuItem(Snap, true)]
        [MenuItem(Turn, true)]
        static bool HasSelection()
        {
            return Selection.transforms.Length > 0;
        }
    }
}
