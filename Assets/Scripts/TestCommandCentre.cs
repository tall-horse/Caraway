using OddJelly.CarvedKingdoms;
using UnityEngine;

public class TestCommandCentre : MonoBehaviour
{
    private HexBoard _hexBoard;
    [SerializeField] private CarvedTeam[] _tiles;
    private CarvedTeam _tileToPlace = null;
    private GameObject _firstTile = null;
    private void Awake()
    {
        _hexBoard = FindAnyObjectByType<HexBoard>();
    }

    void Update()
    {
        if (Input.GetKeyUp(KeyCode.Alpha1))
        {
            _firstTile = _hexBoard.PlaceTile(_tiles[0].gameObject, new Vector2Int(0, 0), 0);
        }
        if (Input.GetMouseButtonUp(0))
        {
            Vector2Int cell = GetCellUnderMouse();
            _firstTile = _hexBoard.PlaceTile(_tiles[0].gameObject, cell, 0);
        }
    }
    /// <summary>
    /// Returns the axial cell that is underneath the current mouse position.
    /// </summary>
    private Vector2Int GetCellUnderMouse()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        Plane boardPlane = new Plane(Vector3.up, 0f);

        if (!boardPlane.Raycast(ray, out float distance))
            return default;

        Vector3 hitWorldPos = ray.GetPoint(distance);

        return HexBoard.LocalToCell(hitWorldPos);
    }
}
