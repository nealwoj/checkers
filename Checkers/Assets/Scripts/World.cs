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
//public struct GridPiece
//{
//    public Color col;
//    public int x, y;

//    public GridPiece(Color col, int x, int y)
//    {
//        this.col = col;
//        this.x = x;
//        this.y = y;
//    }
//}

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
}

public class World : MonoBehaviour
{
    public static World Instance { get; private set; }
    public const int ROWS = 8, COLS = 8;
    public const int MAX_PIECES = 12;
    public const int RED_FORWARD = 1, WHITE_FORWARD = -1, LEFT = -1, RIGHT = 1;
    public const float GRID_LAYER = 0f;
    public const float PIECE_LAYER = -1f;
    public const float GRID_OFFSET = 4.5f;

    //game data
    public Color[,] board;
    public List<byte> pieces = new List<byte>();

    //holds information for draw functions like current grid and checkers pieces
    public List<GameObject> objects = new List<GameObject>(), grid = new List<GameObject>();
    
    public bool init, selected;
    public float score;

    public int pieceIndex, gridIndex;

    //prefabs
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
        // C# uses implicit conversion without telling us, so we cannot use the shift operator all the time

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
        //Vector3 temp = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        //Debug.Log("Regular: (" + (int)(temp.x) + ", " + (int)(temp.y) + ")");
        //Debug.Log("Offset: (" + (int)(temp.x + GRID_OFFSET) + ", " + (int)(temp.y + GRID_OFFSET) + ")");

        //input
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            int x = (int)(pos.x + GRID_OFFSET);
            int y = (int)(pos.y + GRID_OFFSET);
            //Debug.Log("Click at: (" + x + ", " + y + ")");

            //mouse click is on the board
            if (x > -1 && x < 8 && y > -1 && y < 8)
                Click(x, y);
        }
    }

    public void Click(int x, int y)
    {
        for (int i = 0; i < grid.Count; i++)
        {
            if ((int)(grid[i].transform.position.x + GRID_OFFSET) == x && (int)(grid[i].transform.position.y + GRID_OFFSET) == y)
            {
                //if there is a piece selected, else select piece
                if (selected)
                {
                    //if the mouse clicked on another piece then change it, else if the mouse clicked on the selected piece then deselect, else move current selected piece
                    if (Get(x, y) && GetIndex(x, y) != pieceIndex)
                        Select(x, y, GetIndex(x, y), i);
                    else if (GetIndex(x, y) == pieceIndex)
                        DeselectPiece();
                    else
                        Move(x, y);
                }
                else
                    Select(x, y, GetIndex(x, y), i);
            }
        }
    }
    private void Select(int x, int y, int pieceIndex, int gridIndex)
    {
        if (Get(x, y) && board[x, y] == Color.BLACK)
        {
            ResetGrid();
            this.pieceIndex = pieceIndex;

            grid[gridIndex].gameObject.GetComponent<SpriteRenderer>().color = UnityEngine.Color.yellow;
            FindMoves();
            selected = true;
        }
    }
    public void Move(int x, int y)
    {
        if (FindMoves() != 0 && GetGrid(x, y).GetComponent<SpriteRenderer>().color == UnityEngine.Color.yellow)
        {
            CheckersPiece piece = UnPack(pieces[pieceIndex]);
            piece.x = x;
            piece.y = y;

            pieces[pieceIndex] = Pack(piece);
            selected = false;

            Draw();
            ResetGrid();
        }
    }
    //private void Deselect()
    //{
    //    //return color
    //    int x = (int)(selectedGrid.transform.position.x + GRID_OFFSET);
    //    int y = (int)(selectedGrid.transform.position.y + GRID_OFFSET);

    //    if (board[x, y] == Color.BLACK)
    //        selectedGrid.gameObject.GetComponent<SpriteRenderer>().color = UnityEngine.Color.black;
    //    else if (board[x, y] == Color.RED)
    //        selectedGrid.gameObject.GetComponent<SpriteRenderer>().color = UnityEngine.Color.red;
    //}
    private void DeselectPiece()
    {
        ResetGrid();
        pieceIndex = -1;
    }
    public bool Get(int x, int y)
    {
        for (int i = 0; i < pieces.Count; i++)
        {
            CheckersPiece piece = UnPack(pieces[i]);
            if (piece.x == x && piece.y == y)
            {
                //Debug.Log("Get() at (" + x + ", " + y + ")");
                return true;
            }
        }

        return false;
    }
    public int GetIndex(int x, int y)
    {
        for (int i = 0; i < pieces.Count; i++)
        {
            CheckersPiece piece = UnPack(pieces[i]);
            if (piece.x == x && piece.y == y)
            {
                //Debug.Log("GetPiece() at (" + x + ", " + y + ")");
                return i;
            }
        }

        return -1;
    }
    public GameObject GetGrid(int x, int y)
    {
        for (int i = 0; i < grid.Count; i++)
            if ((int)(grid[i].transform.position.x + GRID_OFFSET) == x && (int)(grid[i].transform.position.y + GRID_OFFSET) == y)
                return grid[i];

        return null;
    }
    public int FindMoves()
    {
        CheckersPiece piece = UnPack(pieces[pieceIndex]);

        int count = 0;
        int xRIGHT = piece.x + RIGHT, xLEFT = piece.x + LEFT;
        int yRED = piece.y + RED_FORWARD, yWHITE = piece.y + WHITE_FORWARD;

        Debug.Log("xRIGHT = " + xRIGHT);

        if (piece.col == Color.RED)
        {
            if (xRIGHT > -1 && xRIGHT < 8 && yRED > -1 && yRED < 8)
            {
                if (board[xRIGHT, yRED] == Color.BLACK && Get(xRIGHT, yRED) == false)
                {
                    GetGrid(xRIGHT, yRED).GetComponent<SpriteRenderer>().color = UnityEngine.Color.yellow;
                    count++;
                }
            }
            if (xLEFT > -1 && xLEFT < 8 && yWHITE > -1 && yWHITE < 8)
            {
                if (board[xLEFT, yRED] == Color.BLACK && Get(xLEFT, yRED) == false)
                {
                    GetGrid(xLEFT, yRED).GetComponent<SpriteRenderer>().color = UnityEngine.Color.yellow;
                    count++;
                }
            }

            //Debug.Log("Source is (" + piece.x + ", " + piece.y + ")");
            //Debug.Log("Finding at (" + xRIGHT + ", " + yRED + ")");
            //Debug.Log("Finding at (" + xLEFT + ", " + yRED + ")");
        }
        else if (piece.col == Color.WHITE)
        {
            if (xRIGHT > -1 && xRIGHT < 8 && yRED > -1 && yRED < 8)
            {
                if (board[xRIGHT, yWHITE] == Color.BLACK && Get(xRIGHT, yWHITE) == false)
                {
                    GetGrid(xRIGHT, yWHITE).GetComponent<SpriteRenderer>().color = UnityEngine.Color.yellow;
                    count++;
                }
            }
            if (xLEFT > -1 && xLEFT < 8 && yWHITE > -1 && yWHITE < 8)
            {
                if (board[xLEFT, yWHITE] == Color.BLACK && Get(xLEFT, yWHITE) == false)
                {
                    GetGrid(xLEFT, yWHITE).GetComponent<SpriteRenderer>().color = UnityEngine.Color.yellow;
                    count++;
                }
            }
        }

        return count;
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
        int count = 0;
        for (int x = 0; x < ROWS; x++)
        {
            for (int y = 5; y < COLS; y++)
            {
                if (board[x, y] == Color.BLACK && count < MAX_PIECES)
                {
                    byte b = Pack(new CheckersPiece(Color.WHITE, x, y, false));
                    pieces.Add(b);
                    DrawWhitePiece(x, y);
                    count++;
                }
            }
        }
    }
    private void GenerateRedPieces()
    {
        int count = 0;
        for (int x = 0; x < ROWS; x++)
        {
            for (int y = 0; y < COLS; y++)
            {
                if (board[x, y] == Color.BLACK && count < MAX_PIECES)
                {
                    byte b = Pack(new CheckersPiece(Color.RED, y, x, false));
                    pieces.Add(b);
                    DrawRedPiece(y, x);
                    count++;
                }
            }
        }
    }
    #endregion

    #region Draw Functions
    public void Draw()
    {
        ClearPieces();

        for (int i = 0; i < pieces.Count; i++)
            DrawPiece(pieces[i]);
    }
    private void ClearPieces()
    {
        for (int i = 0; i < objects.Count; i++)
            Destroy(objects[i]);

        objects.Clear();
    }
    private void ClearGrid()
    {
        for (int i = 0; i < grid.Count; i++)
            Destroy(grid[i]);

        grid.Clear();
    }
    private void ResetGrid()
    {
        ClearGrid();

        for (int x = 0; x < ROWS; x++)
        {
            for (int y = 0; y < COLS; y++)
            {
                if ((x + y) % 2 == 0)
                    DrawGrid(Color.BLACK, x, y);
                else
                    DrawGrid(Color.RED, x, y);
            }
        }
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

        objects.Add(go);
    }
    private void DrawRedPiece(int x, int y)
    {
        GameObject go = Instantiate(piece_red);

        go.transform.SetParent(GameObject.Find("Pieces").transform);
        go.transform.position = new Vector3(x - horizontalOffset, y - verticalOffset, PIECE_LAYER);

        objects.Add(go);
    }
    #endregion
}