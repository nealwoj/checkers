using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

//piece enums
public enum Color : byte
{
    BLACK = 0b0100,
    WHITE = 0b0001,
    RED = 0b0000
}
public struct GridPiece
{
    public Color col;
    public int x, y;

    public GridPiece(Color col, int x, int y)
    {
        this.col = col;
        this.x = x;
        this.y = y;
    }
}

public struct CheckersPiece
{
    public Color col;
    public int x, y;
    public bool level;

    public CheckersPiece(Color col, int x, int y, bool lvl)
    {
        this.col = col;
        this.x = x;
        this.y = y;
        level = lvl;
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
    public const float GRID_LAYER = 0f;
    public const float PIECE_LAYER = -1f;
    public const float GRID_OFFSET = 4.5f;

    public Color[,] board;
    public List<byte> team_red = new List<byte>(), team_white = new List<byte>();
    public List<GameObject> pieces = new List<GameObject>(), grid = new List<GameObject>();
    public bool init;
    public float score;
    public GameObject grid_black, grid_red, piece_red, piece_white;

    [HideInInspector]
    public int verticalOffset, horizontalOffset;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(this);
    }

    static public CheckersPiece UnPack(byte b)
    {
        CheckersPiece piece = new CheckersPiece();

        // x 0b11100000
        // y 0b00011100
        // c 0b00000010
        // l 0b00000001
        // C# uses implicit conversion without telling us!! Unlike the alpha C++

        piece.x = b >> 5;
        piece.y = (b & 0b00011100) >> 2;
        piece.col = (Color)((b & 0b00000010) >> 1);
        piece.level = (b % 2) == 1;

        return piece;
    }
    static public byte Pack(CheckersPiece piece)
    {
        byte b = 0;

        b = (byte)(piece.x << 5);
        b |= (byte)(piece.y << 2);
        b |= (byte)((byte)(piece.col) << 1);
        b |= (byte)(piece.level ? 1 : 0);

        return b;
    }

    // Start is called before the first frame update
    void Start()
    {
        Init();
    }

    // Update is called once per frame
    void Update()
    {
        //Draw();
        //Vector3 pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        //Debug.Log("Mouse: (" + (pos.x + GRID_OFFSET) + ", " + (pos.y + GRID_OFFSET) + ")");

        //input
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            int x = (int)pos.x;
            int y = (int)pos.y;

            SelectGrid(x, y);
        }
    }

    public void SelectGrid(int x, int y)
    {
        for (int i = 0; i < grid.Count; i++)
        {
            if (grid[i].transform.position.x == x && grid[i].transform.position.y == y)
            {
                grid[i].gameObject.GetComponent<SpriteRenderer>().color = UnityEngine.Color.yellow;
            }
        }
    }

    #region INIT
    private void Init()
    {
        //world data
        board = new Color[ROWS, COLS];

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

                //Debug.Log("(" + x + ", " + y + ")");
            }
        }

        //add starting pieces
        GenerateRedPieces();
        GenerateWhitePieces();
    }
    private void GenerateWhitePieces()
    {
        for (int x = 0; x < ROWS; x++)
        {
            for (int y = 5; y < COLS; y++)
            {
                if (board[x, y] == Color.BLACK && team_white.Count < MAX_PIECES)
                {
                    byte b = Pack(new CheckersPiece(Color.WHITE, x, y, false));
                    team_white.Add(b);
                    DrawWhitePiece(x, y);
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
                    byte b = Pack(new CheckersPiece(Color.RED, x, y, false));
                    team_red.Add(b);
                    DrawRedPiece(y, x);
                }
            }
        }
    }
    #endregion

    #region Draw Functions
    public void Draw()
    {
        ClearPieces();

        int i;
        for (i = 0; i < team_white.Count; i++)
            DrawPiece(team_white[i]);
        for (i = 0; i < team_red.Count; i++)
            DrawPiece(team_red[i]);
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
    public void DrawPiece(byte piece)
    {
        CheckersPiece checkersPiece = UnPack(piece);
        switch (checkersPiece.col)
        {
            case Color.WHITE:
                DrawWhitePiece(checkersPiece.x, checkersPiece.y);
                break;
            case Color.RED:
                DrawRedPiece(checkersPiece.x, checkersPiece.y);
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
        go.transform.position = new Vector3(x - horizontalOffset, y - verticalOffset, GRID_LAYER);

        grid.Add(go);
    }
    private void DrawRedSquare(int x, int y)
    {
        GameObject go = Instantiate(grid_red);

        go.transform.SetParent(GameObject.Find("Grid").transform);
        go.transform.position = new Vector3(x - horizontalOffset, y - verticalOffset, GRID_LAYER);

        grid.Add(go);
    }
    private void DrawWhitePiece(int x, int y)
    {
        GameObject go = Instantiate(piece_white);

        go.transform.SetParent(GameObject.Find("Pieces").transform);
        go.transform.position = new Vector3(x - horizontalOffset, y - verticalOffset, PIECE_LAYER);

        pieces.Add(go);
    }
    private void DrawRedPiece(int x, int y)
    {
        GameObject go = Instantiate(piece_red);

        go.transform.SetParent(GameObject.Find("Pieces").transform);
        go.transform.position = new Vector3(x - horizontalOffset, y - verticalOffset, PIECE_LAYER);

        pieces.Add(go);
    }
    #endregion
}