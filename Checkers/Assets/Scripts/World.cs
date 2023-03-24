using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

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

public struct MoveData
{
    public int x, y;
    public int heuristic;
}
public struct Move
{
    public int x, y;
}

public class World : MonoBehaviour
{
    public static World Instance { get; private set; }
    public const int ROWS = 8, COLS = 8;
    public const int LOWER_BOUND = -1, UPPER_BOUND = 8;
    public const int MAX_PIECES = 12;
    public const int RED_FORWARD = 1, WHITE_FORWARD = -1, LEFT = -1, RIGHT = 1, UP = 1, DOWN = -1;
    public const float GRID_LAYER = 0f, PIECE_LAYER = -1f, DEBUG_LAYER = -5f;
    public const float GRID_OFFSET = 4.5f;

    //game data
    public Color[,] board;
    public List<byte> pieces = new List<byte>(), jumpPieces = new List<byte>();
    public List<Move> moveList = new List<Move>();
    public Color turnColor, AIcolor;

    //holds information for draw functions like current grid and checkers pieces
    public List<GameObject> objects = new List<GameObject>(), grid = new List<GameObject>();
    
    public bool init, selected, AIenabled, playerTurn;
    public float redScore, whiteScore;
    public int pieceIndex, gridIndex;

    //prefabs
    public GameObject grid_black, grid_red, piece_red, piece_white, piece_debug, piece_kingwhite, piece_kingred;

    [HideInInspector]
    public int verticalOffset, horizontalOffset;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(this);
    }

    static public CheckersPiece UnPackPiece(byte b)
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
    static public MoveData UnPackMove(byte b)
    {
        MoveData move = new MoveData();

        // x 0b11100000
        // y 0b00011100
        // c 0b00000010
        // l 0b00000001
        // C# uses implicit conversion without telling us, so we cannot use the shift operator all the time

        move.x = b >> 5;
        move.y = (b & 0b00011100) >> 2;
        //piece.col = (Color)((b & 0b00000010) >> 1);
        //piece.level = (b % 2) == 1;

        return move;
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
    static public byte Pack(MoveData move)
    {
        byte b = 0;

        b = (byte)(move.x << 5);
        b |= (byte)(move.y << 2);
        b |= (byte)((byte)(move.heuristic) << 1);

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

        //win/loss
        CheckWin();

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
        //debug
        if (Input.GetKeyDown(KeyCode.Space))
            DisplayPieces();
        if (Input.GetKeyDown(KeyCode.Escape))
            ClearPieces();

        //if AI is active and not the player's turn
        if (AIenabled && turnColor == AIcolor)
        {
            //fill move list
            moveList.Clear();
            
            for (int i = 0; i < pieces.Count; i++)
            {
                if (UnPackPiece(pieces[i]).col == AIcolor)
                {
                    //add possible piece moves to moveList
                }
            }
        }
    }

    private int CalculateHeuristic(int index)
    {
        int score = 0;
        CheckersPiece piece = UnPackPiece(pieces[index]);

        if (piece.level)
            score++;

        int moveCount = FindMoves(index);
        if (moveCount > 0)
        {
            score++;

            if (moveCount > 1)
                score++;

            //if we can jump once, add score
            if (jumpPieces.Count > 0)
            {
                score++;

                if (jumpPieces.Count > 1)
                    score++;
            }
        }

        return score;
    }

    #region Debug
    private void DisplayPieces()
    {
        ClearPieces();
        for (int i = 0; i < pieces.Count; i++)
        {
            CheckersPiece piece = UnPackPiece(pieces[i]);
            GameObject go = Instantiate(piece_debug);

            go.transform.SetParent(GameObject.Find("Pieces").transform);
            go.transform.position = new Vector3(piece.x - horizontalOffset, piece.y - verticalOffset, DEBUG_LAYER);

            objects.Add(go);
        }
    }
    #endregion

    private void CheckWin()
    {
        if (whiteScore >= 12)
        {
            ClearGrid();
            ClearPieces();
            UIController.Instance.whiteWinUI.SetActive(true);
        }
        else if (redScore >= 12)
        {
            ClearGrid();
            ClearPieces();
            UIController.Instance.redWinUI.SetActive(true);
        }
    }

    #region Selecting Pieces
    public void Click(int x, int y)
    {
        jumpPieces.Clear();
        for (int i = 0; i < grid.Count; i++)
        {
            if ((int)(grid[i].transform.position.x + GRID_OFFSET) == x && (int)(grid[i].transform.position.y + GRID_OFFSET) == y)
            {
                int index = GetIndex(x, y);
                //if there is a piece selected, else select piece
                if (selected)
                {
                    //if the mouse clicked on another piece, else if the mouse clicked on the selected piece, else move current selected piece
                    if (Get(x, y) && index != pieceIndex)
                        Select(x, y, index, i);
                    else if (index == pieceIndex)
                        DeselectPiece();
                    else
                        Move(x, y);
                }
                else
                    Select(x, y, index, i);
            }
        }
    }
    private void Select(int x, int y, int pieceIndex, int gridIndex)
    {
        if (Get(x, y) && board[x, y] == Color.BLACK && UnPackPiece(pieces[pieceIndex]).col == turnColor)
        {
            ResetGrid();
            this.pieceIndex = pieceIndex;

            grid[gridIndex].gameObject.GetComponent<SpriteRenderer>().color = UnityEngine.Color.yellow;
            FindMoves(this.pieceIndex);
            selected = true;
        }
    }
    private void DeselectPiece()
    {
        ResetGrid();
        pieceIndex = -1;
    }
    #endregion

    #region Moving Pieces
    public void Move(int x, int y)
    {
        if (FindMoves(pieceIndex) != 0 && GetGrid(x, y).GetComponent<SpriteRenderer>().color == UnityEngine.Color.yellow)
        {
            //change piece x and y
            CheckersPiece piece = UnPackPiece(pieces[pieceIndex]);
            piece.x = x;
            piece.y = y;

            //did the piece reach the other side, if so promote it
            if ((piece.col == Color.RED && piece.y == 7) ||
            (piece.col == Color.WHITE && piece.y == 0))
                piece.level = true;

            //return piece as packed byte to list
            pieces[pieceIndex] = Pack(piece);
            selected = false;

            //jump case - todo: optimize to avoid if there is no jump
            CheckJump(piece);

            //change turn
            if (turnColor == Color.WHITE)
                turnColor = Color.RED;
            else if (turnColor == Color.RED)
                turnColor = Color.WHITE;

            //draw
            ResetGrid();
            Draw();
        }
    }
    private void CheckJump(CheckersPiece piece)
    {
        //checks the jump pieces to see if we are adjacent, which means we jumped it
        for (int i = 0; i < jumpPieces.Count; i++)
        {
            CheckersPiece jump = UnPackPiece(jumpPieces[i]);
            if (IsDiagonal(GetIndex(piece.x, piece.y), GetIndex(jump.x, jump.y)))
            {
                //scoring
                if (jump.col == Color.RED)
                {
                    whiteScore++;
                    if (jump.level)
                        whiteScore++;
                }
                else if (jump.col == Color.WHITE)
                {
                    redScore++;
                    if (jump.level)
                        redScore++;
                }

                pieces.RemoveAt(GetIndex(jump.x, jump.y));
            }
        }
    }
    private bool IsDiagonal(int index, int adjIndex)
    {
        CheckersPiece piece = UnPackPiece(pieces[index]);

        //checks adj spots for matching index
        if (GetIndex(piece.x + LEFT, piece.y + UP) == adjIndex ||
            GetIndex(piece.x + LEFT, piece.y + DOWN) == adjIndex ||
            GetIndex(piece.x + RIGHT, piece.y + UP) == adjIndex ||
            GetIndex(piece.x + RIGHT, piece.y + DOWN) == adjIndex)
            return true;
        else
            return false;
    }
    private bool IsAdjacent(int index, int adjIndex)
    {
        CheckersPiece piece = UnPackPiece(pieces[index]);

        //checks adj spots for matching index
        if (GetIndex(piece.x, piece.y + UP) == adjIndex ||
            GetIndex(piece.x, piece.y + DOWN) == adjIndex ||
            GetIndex(piece.x + LEFT, piece.y) == adjIndex ||
            GetIndex(piece.x + LEFT, piece.y + UP) == adjIndex ||
            GetIndex(piece.x + LEFT, piece.y + DOWN) == adjIndex ||
            GetIndex(piece.x + RIGHT, piece.y) == adjIndex ||
            GetIndex(piece.x + RIGHT, piece.y + UP) == adjIndex ||
            GetIndex(piece.x + RIGHT, piece.y + DOWN) == adjIndex)
            return true;
        else
            return false;
    }
    #endregion

    #region Getting Pieces
    public bool Get(int x, int y)
    {
        for (int i = 0; i < pieces.Count; i++)
        {
            CheckersPiece piece = UnPackPiece(pieces[i]);
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
            CheckersPiece piece = UnPackPiece(pieces[i]);
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
    #endregion

    #region Finding Moves
    public int FindMoves(int index)
    {
        if (index < 0)
            return -1;

        CheckersPiece piece = UnPackPiece(pieces[index]);
        int count = 0;

        //forwards/backwards
        int xRIGHT = piece.x + RIGHT;
        int xLEFT = piece.x + LEFT;
        int yRED = piece.y + RED_FORWARD;
        int yWHITE = piece.y + WHITE_FORWARD;

        //jump case
        int xxRIGHT = xRIGHT + RIGHT;
        int xxLEFT = xLEFT + LEFT;
        int yyRED = yRED + RED_FORWARD;
        int yyWHITE = yWHITE + WHITE_FORWARD;

        //determines which grid pieces should be marked as movable
        if (piece.col == Color.RED)
        {
            //right check
            if (xRIGHT > LOWER_BOUND && xRIGHT < UPPER_BOUND && yRED > LOWER_BOUND && yRED < UPPER_BOUND)
            {
                //forward right check
                if (board[xRIGHT, yRED] == Color.BLACK && Get(xRIGHT, yRED) == false)
                {
                    GetGrid(xRIGHT, yRED).GetComponent<SpriteRenderer>().color = UnityEngine.Color.yellow;
                    count++;
                }

                //jump case
                if (xxRIGHT > LOWER_BOUND && xxRIGHT < UPPER_BOUND && yyRED > LOWER_BOUND && yyRED < UPPER_BOUND)
                {
                    //forwards right
                    if (Get(xRIGHT, yRED) && Get(xxRIGHT, yyRED) == false && UnPackPiece(pieces[GetIndex(xRIGHT, yRED)]).col == Color.WHITE)
                    {
                        GetGrid(xRIGHT, yRED).GetComponent<SpriteRenderer>().color = UnityEngine.Color.green;
                        GetGrid(xxRIGHT, yyRED).GetComponent<SpriteRenderer>().color = UnityEngine.Color.yellow;
                        jumpPieces.Add(pieces[GetIndex(xRIGHT, yRED)]);
                        count++;
                    }
                }
            }

            //backward right check
            if (piece.level && yWHITE > LOWER_BOUND && yWHITE < UPPER_BOUND && xRIGHT > LOWER_BOUND && xRIGHT < UPPER_BOUND)
            {
                if (board[xRIGHT, yWHITE] == Color.BLACK && Get(xRIGHT, yWHITE) == false)
                {
                    GetGrid(xRIGHT, yWHITE).GetComponent<SpriteRenderer>().color = UnityEngine.Color.yellow;
                    count++;
                }

                if (yyWHITE > LOWER_BOUND && yyWHITE < UPPER_BOUND && xxRIGHT > LOWER_BOUND && xxRIGHT < UPPER_BOUND)
                {
                    if (Get(xRIGHT, yWHITE) && Get(xxRIGHT, yyWHITE) == false && UnPackPiece(pieces[GetIndex(xRIGHT, yWHITE)]).col == Color.WHITE)
                    {
                        GetGrid(xRIGHT, yWHITE).GetComponent<SpriteRenderer>().color = UnityEngine.Color.green;
                        GetGrid(xxRIGHT, yyWHITE).GetComponent<SpriteRenderer>().color = UnityEngine.Color.yellow;
                        jumpPieces.Add(pieces[GetIndex(xRIGHT, yWHITE)]);
                        count++;
                    }
                }
            }

            //left check
            if (xLEFT > LOWER_BOUND && xLEFT < UPPER_BOUND && yRED > LOWER_BOUND && yRED < UPPER_BOUND)
            {
                //forward left check
                if (board[xLEFT, yRED] == Color.BLACK && Get(xLEFT, yRED) == false)
                {
                    GetGrid(xLEFT, yRED).GetComponent<SpriteRenderer>().color = UnityEngine.Color.yellow;
                    count++;
                }

                //jump case
                if (xxLEFT > LOWER_BOUND && xxLEFT < UPPER_BOUND && yyRED > LOWER_BOUND && yyRED < UPPER_BOUND)
                {
                    //forwards left
                    if (Get(xLEFT, yRED) && Get(xxLEFT, yyRED) == false && UnPackPiece(pieces[GetIndex(xLEFT, yRED)]).col == Color.WHITE)
                    {
                        GetGrid(xLEFT, yRED).GetComponent<SpriteRenderer>().color = UnityEngine.Color.green;
                        GetGrid(xxLEFT, yyRED).GetComponent<SpriteRenderer>().color = UnityEngine.Color.yellow;
                        jumpPieces.Add(pieces[GetIndex(xLEFT, yRED)]);
                        count++;
                    }
                }
            }

            //backward left check
            if (piece.level && yWHITE > LOWER_BOUND && yWHITE < UPPER_BOUND && xLEFT > LOWER_BOUND && xLEFT < UPPER_BOUND)
            {
                if (board[xLEFT, yWHITE] == Color.BLACK && Get(xLEFT, yWHITE) == false)
                {
                    GetGrid(xLEFT, yWHITE).GetComponent<SpriteRenderer>().color = UnityEngine.Color.yellow;
                    count++;
                }

                if (yyWHITE > LOWER_BOUND && yyWHITE < UPPER_BOUND && xxLEFT > LOWER_BOUND && xxLEFT < UPPER_BOUND)
                {
                    if (Get(xLEFT, yWHITE) && Get(xxLEFT, yyWHITE) == false && UnPackPiece(pieces[GetIndex(xLEFT, yWHITE)]).col == Color.WHITE)
                    {
                        GetGrid(xLEFT, yWHITE).GetComponent<SpriteRenderer>().color = UnityEngine.Color.green;
                        GetGrid(xxLEFT, yyWHITE).GetComponent<SpriteRenderer>().color = UnityEngine.Color.yellow;
                        jumpPieces.Add(pieces[GetIndex(xLEFT, yWHITE)]);
                        count++;
                    }
                }
            }
        }
        else if (piece.col == Color.WHITE)
        {
            //forward right check
            if (xRIGHT > LOWER_BOUND && xRIGHT < UPPER_BOUND && yWHITE > LOWER_BOUND && yWHITE < UPPER_BOUND)
            {
                //forward right check
                if (board[xRIGHT, yWHITE] == Color.BLACK && Get(xRIGHT, yWHITE) == false)
                {
                    GetGrid(xRIGHT, yWHITE).GetComponent<SpriteRenderer>().color = UnityEngine.Color.yellow;
                    count++;
                }

                //jump case
                if (xxRIGHT > LOWER_BOUND && xxRIGHT < UPPER_BOUND && yyWHITE > LOWER_BOUND && yyWHITE < UPPER_BOUND)
                {
                    //forwards right
                    if (Get(xRIGHT, yWHITE) && Get(xxRIGHT, yyWHITE) == false && UnPackPiece(pieces[GetIndex(xRIGHT, yWHITE)]).col == Color.RED)
                    {
                        GetGrid(xRIGHT, yWHITE).GetComponent<SpriteRenderer>().color = UnityEngine.Color.green;
                        GetGrid(xxRIGHT, yyWHITE).GetComponent<SpriteRenderer>().color = UnityEngine.Color.yellow;
                        jumpPieces.Add(pieces[GetIndex(xRIGHT, yWHITE)]);
                        count++;
                    }
                }
            }

            //backward right check
            if (piece.level && yRED > LOWER_BOUND && yRED < UPPER_BOUND && xRIGHT > LOWER_BOUND && xRIGHT < UPPER_BOUND)
            {
                if (board[xRIGHT, yRED] == Color.BLACK && Get(xRIGHT, yRED) == false)
                {
                    GetGrid(xRIGHT, yRED).GetComponent<SpriteRenderer>().color = UnityEngine.Color.yellow;
                    count++;
                }

                if (yyWHITE > LOWER_BOUND && yyWHITE < UPPER_BOUND && xxRIGHT > LOWER_BOUND && xxRIGHT < UPPER_BOUND)
                {
                    if (Get(xRIGHT, yRED) && Get(xxRIGHT, yyRED) == false && UnPackPiece(pieces[GetIndex(xRIGHT, yRED)]).col == Color.RED)
                    {
                        GetGrid(xRIGHT, yRED).GetComponent<SpriteRenderer>().color = UnityEngine.Color.green;
                        GetGrid(xxRIGHT, yyRED).GetComponent<SpriteRenderer>().color = UnityEngine.Color.yellow;
                        jumpPieces.Add(pieces[GetIndex(xRIGHT, yRED)]);
                        count++;
                    }
                }
            }

            //forward left check
            if (xLEFT > LOWER_BOUND && xLEFT < UPPER_BOUND && yWHITE > LOWER_BOUND && yWHITE < UPPER_BOUND)
            {
                //forward left check
                if (board[xLEFT, yWHITE] == Color.BLACK && Get(xLEFT, yWHITE) == false)
                {
                    GetGrid(xLEFT, yWHITE).GetComponent<SpriteRenderer>().color = UnityEngine.Color.yellow;
                    count++;
                }

                //jump case
                if (xxLEFT > LOWER_BOUND && xxLEFT < UPPER_BOUND && yyWHITE > LOWER_BOUND && yyWHITE < UPPER_BOUND)
                {
                    if (Get(xLEFT, yWHITE) && Get(xxLEFT, yyWHITE) == false && UnPackPiece(pieces[GetIndex(xLEFT, yWHITE)]).col == Color.RED)
                    {
                        GetGrid(xLEFT, yWHITE).GetComponent<SpriteRenderer>().color = UnityEngine.Color.green;
                        GetGrid(xxLEFT, yyWHITE).GetComponent<SpriteRenderer>().color = UnityEngine.Color.yellow;
                        jumpPieces.Add(pieces[GetIndex(xLEFT, yWHITE)]);
                        count++;
                    }
                }
            }

            //backward left check
            if (piece.level && yRED > LOWER_BOUND && yRED < UPPER_BOUND && xLEFT > LOWER_BOUND && xLEFT < UPPER_BOUND)
            {
                if (board[xLEFT, yRED] == Color.BLACK && Get(xLEFT, yRED) == false)
                {
                    GetGrid(xLEFT, yRED).GetComponent<SpriteRenderer>().color = UnityEngine.Color.yellow;
                    count++;
                }

                //jump case
                if (yyRED > LOWER_BOUND && yyRED < UPPER_BOUND && xxLEFT > LOWER_BOUND && xxLEFT < UPPER_BOUND)
                {
                    if (Get(xLEFT, yRED) && Get(xxLEFT, yyRED) == false && UnPackPiece(pieces[GetIndex(xLEFT, yRED)]).col == Color.RED)
                    {
                        GetGrid(xLEFT, yRED).GetComponent<SpriteRenderer>().color = UnityEngine.Color.green;
                        GetGrid(xxLEFT, yyRED).GetComponent<SpriteRenderer>().color = UnityEngine.Color.yellow;
                        jumpPieces.Add(pieces[GetIndex(xLEFT, yRED)]);
                        count++;
                    }
                }
            }
        }

        return count;
    }
    public Move FindAIMoves(int index)
    {
        Move mv = new Move();



        return mv;
    }
    #endregion

    #region INIT
    private void Init()
    {
        //world data
        board = new Color[ROWS, COLS];

        turnColor = Color.WHITE;

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
                    DrawWhitePiece(x, y, false);
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
                    DrawRedPiece(y, x, false);
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
        CheckersPiece checkersPiece = UnPackPiece(piece);
        switch (checkersPiece.col)
        {
            case Color.WHITE:
                DrawWhitePiece(checkersPiece.x, checkersPiece.y, checkersPiece.level);
                break;
            case Color.RED:
                DrawRedPiece(checkersPiece.x, checkersPiece.y, checkersPiece.level);
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
    private void DrawWhitePiece(int x, int y, bool king)
    {
        GameObject go;
        if (king)
            go = Instantiate(piece_kingwhite);
        else
            go = Instantiate(piece_white);

        go.transform.SetParent(GameObject.Find("Pieces").transform);
        go.transform.position = new Vector3(x - horizontalOffset, y - verticalOffset, PIECE_LAYER);

        objects.Add(go);
    }
    private void DrawRedPiece(int x, int y, bool king)
    {
        GameObject go;
        if (king)
            go = Instantiate(piece_kingred);
        else
            go = Instantiate(piece_red);

        go.transform.SetParent(GameObject.Find("Pieces").transform);
        go.transform.position = new Vector3(x - horizontalOffset, y - verticalOffset, PIECE_LAYER);

        objects.Add(go);
    }
    #endregion
}