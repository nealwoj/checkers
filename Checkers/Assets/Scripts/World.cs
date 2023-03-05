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
public enum Color
{
    INVALID = -1,
    BLACK,
    RED
}

public struct CheckersPiece
{
    public Color col;
    public int x, y;

    public CheckersPiece(Color col, int x, int y)
    {
        this.col = col;
        this.x = x;
        this.y = y;
    }
    public void SetPos(int x, int y)
    {
        this.x = x;
        this.y = y;
    }
}

public class World : MonoBehaviour
{
    public static World Instance { get; private set; }
    public const int ROWS = 8, COLS = 8;
    public const int MAX_PIECES = 12;

    public Color[,] board;
    public List<CheckersPiece> team_red, team_black;
    public List<GameObject> pieces;
    public bool init, timer;
    public float score, timeCount = 0f, delay = 1f;
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
        //timed game loop that updates moves every delay
        if (timer)
        {
            if (timeCount > delay)
            {
                //update
                Draw();

                timeCount = 0f;
            }
            else
                timeCount += Time.deltaTime;
        }
    }

    #region INIT
    private void Init()
    {
        //world data
        board = new Color[ROWS, COLS];
        team_black = new List<CheckersPiece>();
        team_red = new List<CheckersPiece>();
        pieces = new List<GameObject>();

        //grid
        verticalOffset = (int)Camera.main.orthographicSize;
        horizontalOffset = verticalOffset * (Screen.width / Screen.height);
        GenerateGrid();

        init = true;
    }
    public void GenerateGrid()
    {
        //generate checkers grid
        for (int x = 0; x < ROWS; x++)
        {
            for (int y = 0; y < COLS; y++)
            {
                if ((x + y) % 2 == 0)
                {
                    DrawGrid(Color.BLACK, x, y);
                    board[x, y] = Color.BLACK;
                }
                else
                {
                    DrawGrid(Color.RED, x, y);
                    board[x, y] = Color.RED;
                }
            }
        }

        //add starting pieces
        GenerateRedPieces();
        GenerateBlackPieces();
    }
    private void GenerateBlackPieces()
    {
        for (int x = 0; x < ROWS; x++)
        {
            for (int y = 5; y < COLS; y++)
            {
                if (board[x, y] == Color.RED && team_black.Count < MAX_PIECES)
                {
                    team_black.Add(new CheckersPiece(Color.BLACK, x, y));
                    DrawPiece(Color.BLACK, x, y);
                }
            }
        }
    }
    private void GenerateRedPieces()
    {
        for (int x = 0; x < ROWS; x++)
        {
            for (int y = 0; y < COLS; y++)
            {
                if (board[x, y] == Color.BLACK && team_red.Count < MAX_PIECES)
                {
                    team_red.Add(new CheckersPiece(Color.RED, x, y));
                    DrawPiece(Color.RED, y, x);
                }
            }
        }
    }
    #endregion

    public void Jump(CheckersPiece piece, int x, int y)
    {
        //check jump spots

        //if we can jump over a piece, set newer x and y, remove jumped piece

        //else jump to new pos

        piece.SetPos(x, y);
    }

    #region Draw Functions
    public void Draw()
    {
        ClearPieces();

        int i;
        for (i = 0; i < team_black.Count; i++)
            DrawBlackPiece(team_black[i].x, team_black[i].y);
        for (i = 0; i < team_red.Count; i++)
            DrawRedPiece(team_red[i].x, team_red[i].y);
    }
    private void ClearPieces()
    {
        for (int i = 0; i < pieces.Count; i++)
            Destroy(pieces[i]);

        pieces.Clear();
    }
    public void DrawGrid(Color col, int x, int y)
    {
        switch(col)
        {
            case Color.BLACK:
                DrawBlackSquare(x,y);
                break;
            case Color.RED:
                DrawRedSquare(x,y);
                break;
            default:
                Debug.Log("Drawing Sprite was defaulted");
                break;
        }
    }
    public void DrawPiece(Color col, int x, int y)
    {
        switch (col)
        {
            case Color.BLACK:
                DrawBlackPiece(x, y);
                break;
            case Color.RED:
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
        pieces.Add(go);
    }
    private void DrawRedPiece(int x, int y)
    {
        GameObject go = Instantiate(piece_red);
        go.transform.SetParent(GameObject.Find("Pieces").transform);
        go.transform.position = new Vector3(x - horizontalOffset, y - verticalOffset, 0f);
        pieces.Add(go);
    }
    #endregion
}