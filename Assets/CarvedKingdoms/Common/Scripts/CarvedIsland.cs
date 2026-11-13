using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM && CARVED_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace OddJelly.CarvedKingdoms
{
    /// <summary>
    /// Lays a small island of one pack's tiles as children of this object. The sea goes round it, and the coasts are
    /// turned to meet the sea. The pack's places sit in the middle, and two walled places never stand side by side. The
    /// rest is open ground and woods. In Play, two companies on each side take turns to attack.
    /// Seed 0 gives the pack's own island. Press Randomize in the Inspector, or R while playing, to lay another one.
    /// The pieces come from the lists below, and the demo scene fills them in.
    /// </summary>
    [AddComponentMenu("Carved Kingdoms/Carved Island")]
    public class CarvedIsland : MonoBehaviour
    {
        [Tooltip("0 is the pack's own island. Any other number gives another island of the same tiles.")]
        public int seed;
        [Tooltip("Lay another island when R is pressed while playing")]
        public bool randomizeKey = true;
        [Tooltip("The pack's places, like the castle, the town and the farm")]
        public GameObject[] places = new GameObject[0];
        [Tooltip("The land between the places")]
        public GameObject[] ground = new GameObject[0];
        [Tooltip("Open ground, where the companies stand")]
        public GameObject[] open = new GameObject[0];
        public GameObject sea, deepSea;
        [Tooltip("Coasts sorted by how many of their edges face the sea, at index 1 to 5")]
        public GameObject[] coasts = new GameObject[6];
        [Tooltip("Another coast of each kind, for variety on islands other than the first")]
        public GameObject[] coastsOther = new GameObject[6];
        public GameObject[] units = new GameObject[0];
        public GameObject ship;

        const int Land = 3, Sea = 2;
        static readonly string[] Walled = { "_town", "_fort", "_castle", "_outpost" };

        void Update()
        {
            if (!randomizeKey) return;
            bool pressed = false;
#if ENABLE_INPUT_SYSTEM && CARVED_INPUT_SYSTEM
            pressed = Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            pressed = Input.GetKeyDown(KeyCode.R);
#endif
            if (pressed) Randomize();
        }

        /// <summary>Picks a new seed and lays the board again, so you get another island.</summary>
        public void Randomize()
        {
            seed = Random.Range(1, 100000);
            Lay();
        }

        /// <summary>Takes away what this island laid before, then lays it again from the seed.</summary>
        public void Lay()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i).gameObject;
                if (Application.isPlaying) { child.SetActive(false); Destroy(child); }
                else DestroyImmediate(child);
            }
            var dice = new System.Random(seed);
            int land = seed == 0 ? Land : 3 + dice.Next(2);
            // A harbour needs the sea next to it, so it never goes inland.
            var placeList = places.Where(p => p != null && !p.name.EndsWith("_harbour")).ToList();
            var landCells = new List<Vector2Int>();
            foreach (var cell in HexBoard.CellsWithin(Vector2Int.zero, land + Sea))
            {
                int d = HexBoard.Distance(cell, Vector2Int.zero);
                if (d > land) { Put(d > land + 1 ? deepSea : sea, cell, 0, CarvedTeam.Side.Neutral, transform); continue; }
                if (d == land)
                {
                    // Find the edges that face out to sea. How many there are picks the coast piece, and the first
                    // one sets its turn.
                    var outward = Enumerable.Range(0, 6).Select(e => HexBoard.Distance(HexBoard.Neighbour(cell, e), Vector2Int.zero) > land).ToArray();
                    int n = outward.Count(o => o);
                    int start = Enumerable.Range(0, 6).First(e => outward[e] && !outward[(e + 5) % 6]);
                    var piece = seed != 0 && dice.Next(2) == 1 && coastsOther[n] != null ? coastsOther[n] : coasts[n];
                    Put(piece, cell, start, CarvedTeam.Side.Neutral, transform);
                    continue;
                }
                landCells.Add(cell);
            }
            if (seed != 0)
            {
                // Any other island gets the places in another order, on other cells. The middle cell stays first.
                Mix(placeList, dice);
                var middle = landCells[0];
                landCells.RemoveAt(0);
                Mix(landCells, dice);
                landCells.Insert(0, middle);
            }
            var spots = new List<Vector2Int>();
            var rest = new List<Vector2Int>();
            for (int i = 0; i < landCells.Count; i++)
            {
                if (spots.Count < Mathf.Min(placeList.Count, 9) && (i % 2 == 0 || landCells[i] == Vector2Int.zero)) spots.Add(landCells[i]);
                else rest.Add(landCells[i]);
            }
            // The castle and the town always get a spot. No walled place goes next to another one, so you never get
            // two sets of walls side by side.
            var left = placeList.Where(e => e.name.EndsWith("_castle") || e.name.EndsWith("_town")).ToList();
            left.AddRange(placeList.Where(e => !left.Contains(e)).ToList());
            var walled = new List<Vector2Int>();
            for (int i = 0; i < spots.Count; i++)
                foreach (var p in left)
                {
                    bool walls = Walled.Any(w => p.name.EndsWith(w));
                    if (walls && walled.Any(at => HexBoard.Distance(at, spots[i]) == 1)) continue;
                    if (walls) walled.Add(spots[i]);
                    left.Remove(p);
                    Put(p, spots[i], 0, i % 2 == 0 ? CarvedTeam.Side.Blue : CarvedTeam.Side.Red, transform);
                    break;
                }
            var openList = open.Where(g => g != null).ToList();
            var groundList = ground.Where(g => g != null).ToList();
            if (openList.Count == 0) openList = groundList;
            var camps = new List<Vector2Int>();
            for (int k = 0; k < Mathf.Min(4, rest.Count); k++) camps.Add(rest[k * Mathf.Max(1, rest.Count / 4)]);
            for (int i = 0; i < rest.Count; i++)
            {
                var tiles = camps.Contains(rest[i]) ? openList : groundList;
                if (tiles.Count == 0) continue;
                int pick = seed == 0 ? (i * 7) % tiles.Count : dice.Next(tiles.Count);
                Put(tiles[pick], rest[i], seed == 0 ? 0 : dice.Next(6), CarvedTeam.Side.Neutral, transform);
            }
            var unitList = units.Where(u => u != null).ToList();
            if (unitList.Count > 0)
            {
                var armies = new GameObject("Armies").AddComponent<CarvedDemo>();
                armies.transform.SetParent(transform, false);
                for (int k = 0; k < camps.Count; k++)
                {
                    var side = k % 2 == 0 ? CarvedTeam.Side.Blue : CarvedTeam.Side.Red;
                    var company = new GameObject($"Company {k + 1} ({side})").transform;
                    company.SetParent(armies.transform, false);
                    var middle = HexBoard.CellToLocal(camps[k]);
                    for (int j = 0; j < 5; j++)
                    {
                        float a = Mathf.PI * 2f * j / 5f;
                        var go = Put(unitList[k % unitList.Count], camps[k], 0, side, company);
                        if (go == null) continue;
                        go.transform.localPosition = middle + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 0.42f;
                        go.transform.localRotation = Quaternion.LookRotation(side == CarvedTeam.Side.Blue ? HexBoard.EdgeZero : -HexBoard.EdgeZero);
                    }
                }
            }
            if (ship != null) Put(ship, new Vector2Int(land + 1, -1), 0, CarvedTeam.Side.Red, transform);
        }

        static void Mix<T>(List<T> list, System.Random dice)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = dice.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        static GameObject Put(GameObject prefab, Vector2Int cell, int turn, CarvedTeam.Side side, Transform parent)
        {
            if (prefab == null) return null;
            GameObject go = null;
#if UNITY_EDITOR
            // In the editor, lay prefab instances. That way a change to a prefab shows on the island too.
            if (!Application.isPlaying) go = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, parent);
#endif
            if (go == null) go = Instantiate(prefab, parent);
            go.transform.localPosition = HexBoard.CellToLocal(cell);
            go.transform.localRotation = HexBoard.Turn(turn);
            if (side != CarvedTeam.Side.Neutral)
            {
                var team = go.GetComponent<CarvedTeam>();
                if (team != null) team.SetSide(side);
            }
            return go;
        }
    }
}
