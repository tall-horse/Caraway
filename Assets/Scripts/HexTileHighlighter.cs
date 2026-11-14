using UnityEngine;
using System.Collections.Generic;
using OddJelly.CarvedKingdoms;

public class HexTileHighlighter : MonoBehaviour
{
    [Header("References")]
    public HexBoard board;   // Drag your HexBoard here

    private GameObject _current = null;

    // Store the original light/dark colours for each cell so we can restore them.
    private readonly Dictionary<Vector2Int, (Color light, Color dark)> _originalWoodColors =
        new Dictionary<Vector2Int, (Color, Color)>();

    void Update()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(Vector3.up, 0f);

        if (!plane.Raycast(ray, out float dist))
        {
            Unhighlight();
            return;
        }

        Vector3 hitWorldPos = ray.GetPoint(dist);
        Vector2Int cell = HexBoard.LocalToCell(hitWorldPos);

        if (!board.Tiles.TryGetValue(cell, out GameObject tile) || tile == null)
        {
            Unhighlight();
            return;
        }

        if (_current != tile)
        {
            Unhighlight();
            Highlight(tile, cell);
        }
    }

    void Highlight(GameObject tile, Vector2Int cell)
    {
        foreach (var r in tile.GetComponentsInChildren<Renderer>(true))
        {
            if (r.sharedMaterial == null || !r.sharedMaterial.shader.name.Contains("Painted Wood"))
                continue;

            var mat = r.material;

            // Store original colours once per cell
            if (!_originalWoodColors.TryGetValue(cell, out var orig))
            {
                Color light = mat.GetColor("_WoodLight");
                Color dark = mat.GetColor("_WoodDark");
                _originalWoodColors[cell] = (light, dark);
            }

            mat.SetColor("_WoodLight", Color.red);
            mat.SetColor("_WoodDark", new Color(0.5f, 0f, 0f));   // dark red
        }

        _current = tile;
    }

    void Unhighlight()
    {
        if (_current == null) return;

        foreach (var r in _current.GetComponentsInChildren<Renderer>(true))
        {
            if (r.sharedMaterial == null || !r.sharedMaterial.shader.name.Contains("Painted Wood"))
                continue;

            var mat = r.material;
            Vector2Int cell = HexBoard.LocalToCell(_current.transform.localPosition);

            if (_originalWoodColors.TryGetValue(cell, out var orig))
            {
                mat.SetColor("_WoodLight", orig.light);
                mat.SetColor("_WoodDark", orig.dark);
            }
        }

        _current = null;
    }
}
