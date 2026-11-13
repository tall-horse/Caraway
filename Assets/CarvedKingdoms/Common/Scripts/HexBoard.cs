using System.Collections.Generic;
using UnityEngine;

namespace OddJelly.CarvedKingdoms
{
    /// <summary>
    /// Helps with the hex grid of Carved Kingdoms tiles. It handles axial coordinates (q, r), neighbours, distances and
    /// placing pieces.
    ///
    /// Tiles are pointy-top hexagons, 2 m from corner to corner, with their tops at y = 0. Edge d faces neighbour
    /// Dirs[d], and edge 0 faces +x.
    /// Rivers, roads and coasts start from edge 0. Place(prefab, cell, k) turns them so they start from edge k.
    /// river_straight joins edges 0 and 3, river_bend joins 0 and 2, and river_sharp joins 0 and 1.
    /// road_fork joins edges 0, 2 and 4, and road_cross joins 0, 1, 3 and 4.
    /// coast_side has the sea on edge 0, and coast_two_sides has it on 0 and 1.
    /// </summary>
    public class HexBoard : MonoBehaviour
    {
        public const float Size = 1f;
        /// <summary>The way edge 0 faces on a tile that has not been turned.</summary>
        public static readonly Vector3 EdgeZero = new Vector3(1f, 0f, 0f);
        /// <summary>Points between edges 1 and 2.</summary>
        public static readonly Vector3 North = new Vector3(0f, 0f, 1f);
        public static readonly Vector2Int[] Dirs =
        {
            new Vector2Int(1, 0), new Vector2Int(1, -1), new Vector2Int(0, -1),
            new Vector2Int(-1, 0), new Vector2Int(-1, 1), new Vector2Int(0, 1),
        };

        /// <summary>Every tile placed with PlaceTile, looked up by its cell.</summary>
        public readonly Dictionary<Vector2Int, GameObject> Tiles = new Dictionary<Vector2Int, GameObject>();

        /// <summary>Gives the middle of a cell's top, in the board's own space.</summary>
        public static Vector3 CellToLocal(Vector2Int cell)
        {
            return EdgeZero * (Mathf.Sqrt(3f) * Size * (cell.x + cell.y * 0.5f)) - North * (1.5f * Size * cell.y);
        }

        /// <summary>Gives the cell under a point in the board's own space. The point's y is ignored.</summary>
        public static Vector2Int LocalToCell(Vector3 p)
        {
            float r = -Vector3.Dot(p, North) / (1.5f * Size);
            float q = Vector3.Dot(p, EdgeZero) / (Mathf.Sqrt(3f) * Size) - r * 0.5f;
            // Round in cube coordinates.
            float x = q, z = r, y = -x - z;
            float rx = Mathf.Round(x), ry = Mathf.Round(y), rz = Mathf.Round(z);
            float dx = Mathf.Abs(rx - x), dy = Mathf.Abs(ry - y), dz = Mathf.Abs(rz - z);
            if (dx > dy && dx > dz) rx = -ry - rz;
            else if (dy <= dz) rz = -rx - ry;
            return new Vector2Int((int)rx, (int)rz);
        }

        /// <summary>Gives the cell across edge `edge`, which runs from 0 to 5.</summary>
        public static Vector2Int Neighbour(Vector2Int cell, int edge)
        {
            return cell + Dirs[((edge % 6) + 6) % 6];
        }

        /// <summary>Counts how many steps apart two cells are.</summary>
        public static int Distance(Vector2Int a, Vector2Int b)
        {
            var d = a - b;
            return (Mathf.Abs(d.x) + Mathf.Abs(d.y) + Mathf.Abs(d.x + d.y)) / 2;
        }

        /// <summary>Lists the cells within `reach` steps of `cell`. The list starts with `cell` itself.</summary>
        public static List<Vector2Int> CellsWithin(Vector2Int cell, int reach)
        {
            var list = new List<Vector2Int> { cell };
            for (int q = -reach; q <= reach; q++)
                for (int r = Mathf.Max(-reach, -q - reach); r <= Mathf.Min(reach, -q + reach); r++)
                    if (q != 0 || r != 0) list.Add(cell + new Vector2Int(q, r));
            return list;
        }

        /// <summary>Gives the rotation for k steps of 60 degrees. It moves edge 0 to edge k.</summary>
        public static Quaternion Turn(int k)
        {

            float way = Vector3.Dot(Quaternion.AngleAxis(90f, Vector3.up) * EdgeZero, North) > 0f ? 1f : -1f;
            return Quaternion.AngleAxis(way * 60f * k, Vector3.up);
        }

        /// <summary>Puts a tile or piece on a cell, turned by k * 60 degrees and painted in a side's colours. Returns
        /// the new object.</summary>
        public GameObject Place(GameObject prefab, Vector2Int cell, int turn = 0, CarvedTeam.Side side = CarvedTeam.Side.Neutral)
        {
            var go = Instantiate(prefab, transform);
            go.transform.localPosition = CellToLocal(cell);
            go.transform.localRotation = Turn(turn);
            if (side != CarvedTeam.Side.Neutral)
            {
                var team = go.GetComponent<CarvedTeam>();
                if (team == null) team = go.AddComponent<CarvedTeam>();
                team.SetSide(side);
            }
            return go;
        }

        /// <summary>Puts a tile on a cell and replaces the tile that was there.</summary>
        public GameObject PlaceTile(GameObject prefab, Vector2Int cell, int turn = 0, CarvedTeam.Side side = CarvedTeam.Side.Neutral)
        {
            if (Tiles.TryGetValue(cell, out var old) && old != null) Destroy(old);
            var go = Place(prefab, cell, turn, side);
            Tiles[cell] = go;
            return go;
        }
    }
}
