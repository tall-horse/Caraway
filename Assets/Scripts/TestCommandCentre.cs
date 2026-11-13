using OddJelly.CarvedKingdoms;
using UnityEngine;

public class TestCommandCentre : MonoBehaviour
{
    private HexBoard _hexBoard;
    [SerializeField] private CarvedTeam[] _tiles;
    private void Awake()
    {
        _hexBoard = FindAnyObjectByType<HexBoard>();
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyUp(KeyCode.Alpha1))
        {
            _hexBoard.PlaceTile(_tiles[0].gameObject, new Vector2Int(0, 0), 0);
        }
    }
}
