using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

//piece enums
public enum Level
{
    INVALID = -1,
    DEAD,
    ALIVE
}
public enum Team
{
    INVALID = -1,
    BLACK,
    RED
}

public struct CheckersPiece
{
    public Team team;
    public int x, y;

    public CheckersPiece(Team team, int x, int y)
    {
        this.team = team;
        this.x = x;
        this.y = y;
    }
}

public class World : MonoBehaviour
{
    public static World Instance { get; private set; }
    public const int ROWS = 8, COLS = 8;
    public const int MAX_PIECES = 12;

    public Team[,] board;
    public List<CheckersPiece> team_red, team_black;
    public bool init;
    public float score;
    public GameObject grid_black, grid_red, piece_red, piece_black;

    [HideInInspector]
    public int verticalOffset, horizontalOffset;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(this);
    }

    // Start is called before the first frame update
    void Start()
    {
        Init();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    #region INIT
    private void Init()
    {
        //grid
        verticalOffset = (int)Camera.main.orthographicSize;
        horizontalOffset = verticalOffset * (Screen.width / Screen.height);

        board = new Team[ROWS, COLS];
        team_black = new List<CheckersPiece>();
        team_red = new List<CheckersPiece>();

        GenerateGrid();
        GeneratePieces();

        init = true;
    }
    public void GenerateGrid()
    {
        for (int x = 0; x < ROWS; x++)
        {
            for (int y = 0; y < COLS; y++)
            {
                if ((x + y) % 2 == 0)
                {
                    DrawGrid(Team.BLACK, x, y);
                    board[x, y] = Team.BLACK;
                }
                else
                {
                    DrawGrid(Team.RED, x, y);
                    board[x, y] = Team.RED;
                }
            }
        }
    }
    public void GeneratePieces()
    {
        for (int x = 0; x < ROWS; x++)
        {
            for (int y = 0; y < COLS; y++)
            {
                if (board[x, y] == Team.RED && team_red.Count < MAX_PIECES)
                {
                    team_red.Add(new CheckersPiece(Team.RED, x, y));
                    DrawPiece(board[x, y], y, x);
                }
                else if (board[x, y] == Team.BLACK && team_black.Count < MAX_PIECES)
                {
                    team_black.Add(new CheckersPiece(Team.BLACK, x, y));
                    DrawPiece(board[x, y], y, x);
                }
            }
        }
    }
    #endregion

    #region Draw Functions
    public void DrawGrid(Team team, int x, int y)
    {
        switch(team)
        {
            case Team.BLACK:
                DrawBlackSquare(x,y);
                break;
            case Team.RED:
                DrawRedSquare(x,y);
                break;
            default:
                Debug.Log("Draw Sprite was defaulted");
                break;
        }
    }
    public void DrawPiece(Team tm, int x, int y)
    {
        switch (tm)
        {
            case Team.BLACK:
                DrawBlackPiece(x, y);
                break;
            case Team.RED:
                DrawRedPiece(x, y);
                break;
            default:
                Debug.Log("Drawing piece was defaulted");
                break;
        }
    }
    private void DrawBlackSquare(int x, int y)
    {
        GameObject go = Instantiate(grid_black);
        go.transform.SetParent(GameObject.Find("Grid").transform);
        go.transform.position = new Vector3(x - horizontalOffset, y - verticalOffset, 0f);
    }
    private void DrawRedSquare(int x, int y)
    {
        GameObject go = Instantiate(grid_red);
        go.transform.SetParent(GameObject.Find("Grid").transform);
        go.transform.position = new Vector3(x - horizontalOffset, y - verticalOffset, 0f);
    }
    private void DrawBlackPiece(int x, int y)
    {
        GameObject go = Instantiate(piece_black);
        go.transform.SetParent(GameObject.Find("Pieces").transform);
        go.transform.position = new Vector3(x - horizontalOffset, y - verticalOffset, 0f);
    }
    private void DrawRedPiece(int x, int y)
    {
        GameObject go = Instantiate(piece_red);
        go.transform.SetParent(GameObject.Find("Pieces").transform);
        go.transform.position = new Vector3(x - horizontalOffset, y - verticalOffset, 0f);
    }
    #endregion
}
