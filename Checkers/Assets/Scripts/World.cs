using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

//piece enums
public enum CheckersColor : byte
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
    public CheckersColor col;
    public int x, y;
    public bool level;

    public CheckersPiece(CheckersColor col, int x, int y, bool lvl)
    {
        this.col = col;
        this.x = x;
        this.y = y;
        level = lvl;
    }
}

public struct Move
{
    public int x, y, index, jumpIndex;

    //multiple constructors for a jump case
    public Move(int x, int y, int index, int jump)
    {
        this.x = x;
        this.y = y;
        this.index = index;
        jumpIndex = jump;
    }
    public Move(int x, int y, int index)
    {
        this.x = x;
        this.y = y;
        this.index = index;
        jumpIndex = -1;
    }

    //operator overload lambdas
    public static bool operator==(Move first, Move second) => first.x == second.x && first.y == second.y;
    public static bool operator!=(Move first, Move second) => first.x != second.x && first.y != second.y;
}
public struct RankedMove
{
    public Move move;
    public int score;

    public RankedMove(Move move, int score)
    {
        this.move = move;
        this.score = score;
    }
}

public class World : MonoBehaviour
{
    public static World Instance { get; private set; }

    //constants
    private const int TARGET_SCALE = 50, TARGET_DEPTH = 4;
    private const int ROWS = 8, COLS = 8;
    private const int LOWER_BOUND = -1, UPPER_BOUND = 8;
    private const int MAX_PIECES = 12;
    private const int RED_FORWARD = 1, WHITE_FORWARD = -1, LEFT = -1, RIGHT = 1, UP = 1, DOWN = -1;
    private const float GRID_LAYER = 0f, PIECE_LAYER = -1f, DEBUG_LAYER = -5f;
    private const float GRID_OFFSET = 4.5f;

    //game data
    public CheckersColor[,] board;
    public List<byte> pieces = new List<byte>(), jumpPieces = new List<byte>();
    public Difficulty AIdiff = Difficulty.EASY;
    public Move lastMove;

    //coloring
    public CheckersColor turnColor, AIcolor; //by default AI is set to the WHITE team (top of screen)
    public Color mainGridColor, otherGridColor, teamOneColor, teamTwoColor;

    //holds information for draw functions like current grid and checkers pieces
    public List<GameObject> objects = new List<GameObject>(), grid = new List<GameObject>();

    public bool init, selected, AIenabled, playerTurn;
    public float redScore, whiteScore;
    public int pieceIndex, redCount, whiteCount, pieceMoves;

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
        piece.col = (CheckersColor)((b & 0b00000010) >> 1);
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
        if (init)
        {
            //input
            if (Input.GetMouseButtonDown(0))
            {
                Vector3 pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                int x = (int)(pos.x + GRID_OFFSET);
                int y = (int)(pos.y + GRID_OFFSET);

                //mouse click is in board bounds
                if (x > LOWER_BOUND && x < UPPER_BOUND && y > LOWER_BOUND && y < UPPER_BOUND)
                    Click(x, y);
            }

            //debug
            if (Input.GetKeyDown(KeyCode.Space))
                DisplayPieces();
            if (Input.GetKeyDown(KeyCode.Escape))
                ClearPieces();

            //AI
            if (AIenabled && turnColor == AIcolor)
                GenerateAI();
        }
    }

    public void Restart()
    {
        turnColor = CheckersColor.RED;

        //grid
        verticalOffset = (int)Camera.main.orthographicSize;
        horizontalOffset = verticalOffset * (Screen.width / Screen.height);
        GenerateGrid();

        whiteCount = 12;
        whiteScore = 0;
        redCount = 12;
        redScore = 0;

        selected = false;
        pieceIndex = -1;
        pieceMoves = -1;

        init = true;
    }

    #region AI
    public void ActivateAI()
    {
        //Restart();

        AIenabled = true;
        AIcolor = CheckersColor.WHITE;
    }
    private void GenerateAI()
    {
        CheckWin();

        //go through each AI piece and get total number of moves stored in a list of move types
        List<Move> moves = FindAIMoves();
        if (moves.Count <= 0)
            return;

        //then check each move data (x and y) for a score based on whats in/around the tile, storing that in a list of scores (int)
        List<RankedMove> moveList = CalculateScores(moves);

        //EASY: moveAI using minimax search to get random moves
        //HARD: sort moves by heuristic score from smallest to largest and then select the last element in moveList which has the highest score
        if (AIdiff == Difficulty.MEDIUM)
        {
            //lambda that sorts the vectors based on score (smallest to larget)
            moveList.Sort((move1, move2) => move1.score.CompareTo(move2.score));

            //execute best move for AI by grabbing the largest scored move at the end of the list
            MoveAI(moveList[moveList.Count - 1].move);
        }
        else if (AIdiff == Difficulty.EASY)
            MoveAI(SearchTree(moveList).move);
    }
    private RankedMove SearchTree(List<RankedMove> tree)
    {
        List<RankedMove> newTree = tree;
        Debug.Log("NEW TREE COUNT: " + newTree.Count);

        bool run = true;
        while (run)
        {
            newTree = Search(newTree);
            Debug.Log("NEW TREE COUNT: " + newTree.Count);

            if (newTree.Count < 2)
                run = false;
        }

        Debug.Log("NEW TREE FINAL COUNT: " + newTree.Count);
        return newTree[0];
    }
    private List<RankedMove> Search(List<RankedMove> tree)
    {
        List<RankedMove> newTree = new List<RankedMove>();
        bool max = false;

        //if we have an odd number, truncate to even and add move at end later
        RankedMove mv = new RankedMove();
        bool odd = false;
        if (tree.Count % 2 != 0)
        {
            Debug.Log("ODD!");
            odd = true;
            mv = tree[0];
            tree.RemoveAt(0);
        }

        for (int i = 0; i < tree.Count / 2; i++)
        {
            max = !max;
            newTree.Add(minimax(tree, i, max));
        }

        if (odd)
            newTree.Add(mv);

        return newTree;
    }
    private RankedMove minimax(List<RankedMove> tree, int depth, bool isMax)
    {
        //RankedMove mv = new RankedMove();
        //if (depth == 0)
        //    return mv;

        int adjacent = depth + 1;

        //if we need to find max, return move with highest score, if they are equal then choose a random move between the two
        if (isMax)
        {
            if (tree[depth].score > tree[adjacent].score)
                return tree[depth];
            else if (tree[depth].score < tree[adjacent].score)
                return tree[adjacent];
            else
            {
                int rand = UnityEngine.Random.Range(1, 2);
                if (rand == 1)
                    return tree[depth];
                else
                    return tree[adjacent];
            }
        }
        else
        {
            if (tree[depth].score < tree[adjacent].score)
                return tree[depth];
            else if (tree[depth].score > tree[adjacent].score)
                return tree[adjacent];
            else
            {
                int rand = UnityEngine.Random.Range(1, 2);
                if (rand == 1)
                    return tree[depth];
                else
                    return tree[adjacent];
            }
        }
    }
    private void MoveAI(Move move)
    {
        CheckersPiece piece = UnPackPiece(pieces[move.index]);

        //lastMove
        lastMove = new Move(piece.x, piece.y, GetIndex(piece.x, piece.y), -1);
        Debug.Log("lastMove - (" + lastMove.x + ", " + lastMove.y + ")");

        //change piece x and y
        piece.x = move.x;
        piece.y = move.y;

        //did the piece reach the other side, if so promote it
        if (piece.y == 0)
            piece.level = true;

        //return piece as packed byte to list
        pieces[move.index] = Pack(piece);

        //jump case - MUST HAPPEN AFTER PACKING PIECE
        if (move.jumpIndex > -1)
        {
            redCount--;
            whiteScore++;
            pieces.RemoveAt(move.jumpIndex);
        }

        //change turn
        if (turnColor == CheckersColor.WHITE)
            turnColor = CheckersColor.RED;
        else if (turnColor == CheckersColor.RED)
            turnColor = CheckersColor.WHITE;
        
        Draw();
        CheckWin();
    }
    private List<RankedMove> CalculateScores(List<Move> moves)
    {
        List<RankedMove> moveList = new List<RankedMove>();
        for (int i = 0; i < moves.Count; i++)
        {
            CheckersPiece piece = UnPackPiece(pieces[moves[i].index]);
            int score = 0;

            //adjacent case
            score += CalculateAdjacent(moves[i], piece.level);

            //jump case
            if (moves[i].jumpIndex > -1)
            {
                //try to limit aggression
                //if (whiteCount > (whiteCount / 2))
                //    score += 500;
                //else
                //    score += 200;

                score += 500;

                //edge case - give bonus points since the piece cant be jumped if it moves
                if (moves[i].x == 0 || moves[i].x == 7)
                    score += 500;

                //if its jumping a promoted piece
                if (UnPackPiece(pieces[moves[i].jumpIndex]).level)
                    score += 500;
            }
            else
            {
                //if there are no pieces to jump, search for targets close and add score depending on how many it detects (mainly a deterrent to promoted pieces doing the same moves over and over)
                //if (score == 0 && piece.level)
                //{
                //    //if no units were adjacent/close and not in any special position, add points for targets that are close (depth)
                //    score += SearchClosestTarget(moves[i], TARGET_DEPTH) * TARGET_SCALE;
                //}
            }

            //promoted
            //if (piece.level)
            //    score += 100;

            //near promotion line
            if (moves[i].y < 1 && piece.level == false)
                score += 200;

            //edge case - if your on the edge and this spot can be jumped, don't because you can't trade with another piece
            if (piece.x == LOWER_BOUND + 1 || piece.x == UPPER_BOUND - 1)
            {
                if (Get(moves[i].x + RIGHT, moves[i].y + WHITE_FORWARD))
                {
                    if (UnPackPiece(pieces[GetIndex(moves[i].x + RIGHT, moves[i].y + WHITE_FORWARD)]).col != AIcolor)
                        score -= 300;
                }
                if (Get(moves[i].x + LEFT, moves[i].y + WHITE_FORWARD))
                {
                    if (UnPackPiece(pieces[GetIndex(moves[i].x + LEFT, moves[i].y + WHITE_FORWARD)]).col != AIcolor)
                        score -= 300;
                }

                //promotion check
                if (Get(moves[i].x + RIGHT, moves[i].y + RED_FORWARD))
                {
                    if (UnPackPiece(pieces[GetIndex(moves[i].x + RIGHT, moves[i].y + RED_FORWARD)]).col != AIcolor && UnPackPiece(pieces[GetIndex(moves[i].x + RIGHT, moves[i].y + RED_FORWARD)]).level)
                        score -= 300;
                }
                //promotion check
                if (Get(moves[i].x + LEFT, moves[i].y + RED_FORWARD))
                {
                    if (UnPackPiece(pieces[GetIndex(moves[i].x + LEFT, moves[i].y + RED_FORWARD)]).col != AIcolor && UnPackPiece(pieces[GetIndex(moves[i].x + LEFT, moves[i].y + RED_FORWARD)]).level)
                        score -= 300;
                }
            }

            //about to be jumped
            //if (GetAdjacent(moves[i]) > 0)
            //{
            //    Debug.Log("ABOUT TO BE JUMPED");
            //    //score += 1000;
            //}

            //add score to move data
            moveList.Add(new RankedMove(moves[i], score));
        }
        return moveList;
    }
    private int GetAdjacent(Move move)
    {
        int count = 0;
        //check front right and back left
        if (Get(move.x + RIGHT, move.y + WHITE_FORWARD) && Get(move.x + LEFT, move.y + RED_FORWARD))
        {
            if (UnPackPiece(pieces[GetIndex(move.x + RIGHT, move.y + WHITE_FORWARD)]).col != AIcolor)
                count++;
        }
        //check front left and back right
        if (Get(move.x + LEFT, move.y + WHITE_FORWARD) && Get(move.x + RIGHT, move.y + RED_FORWARD))
        {
            if (UnPackPiece(pieces[GetIndex(move.x + LEFT, move.y + WHITE_FORWARD)]).col != AIcolor)
                count++;
        }
        //check behind right
        //if (Get(move.x + RIGHT, move.y + RED_FORWARD))
        //{
        //    if (UnPackPiece(pieces[GetIndex(move.x + RIGHT, move.y + RED_FORWARD)]).col != AIcolor)
        //        count++;
        //}
        ////check behind left
        //if (Get(move.x + LEFT, move.y + RED_FORWARD))
        //{
        //    if (UnPackPiece(pieces[GetIndex(move.x + LEFT, move.y + RED_FORWARD)]).col != AIcolor)
        //        count++;
        //}
        return count;
    }
    private int CalculateAdjacent(Move move, bool level)
    {
        //calculate adjacent pieces and what they are
        //subtracts score if an opponent piece is adjacent to avoid being jumped
        //adds score if there are friendly pieces behind that can trade

        //subtracts 100 if there is a regular piece, then another 200 if its a promoted piece
        //if there is a friendly piece behind this move and its not itself, then add 200 because the friendly piece can trade if this move gets jumped

        int score = 0;

        //check front right
        if (Get(move.x + RIGHT, move.y + WHITE_FORWARD))
        {
            if (UnPackPiece(pieces[GetIndex(move.x + RIGHT, move.y + WHITE_FORWARD)]).col != AIcolor)
            {
                score -= 100;

                if (UnPackPiece(pieces[GetIndex(move.x + RIGHT, move.y + WHITE_FORWARD)]).level)
                    score -= 200;
            }
        }
        //check front left
        if (Get(move.x + LEFT, move.y + WHITE_FORWARD))
        {
            if (UnPackPiece(pieces[GetIndex(move.x + LEFT, move.y + WHITE_FORWARD)]).col != AIcolor)
            {
                score -= 100;

                if (UnPackPiece(pieces[GetIndex(move.x + LEFT, move.y + WHITE_FORWARD)]).level)
                    score -= 200;
            }
        }
        //check behind right
        if (Get(move.x + RIGHT, move.y + RED_FORWARD))
        {
            if (UnPackPiece(pieces[GetIndex(move.x + RIGHT, move.y + RED_FORWARD)]).col != AIcolor)
            {
                score -= 100;

                if (UnPackPiece(pieces[GetIndex(move.x + RIGHT, move.y + RED_FORWARD)]).level)
                    score -= 200;
            }
            else
                if (GetIndex(move.x + RIGHT, move.y + RED_FORWARD) != move.index)
                    score += 300;
        }
        //check behind left
        if (Get(move.x + LEFT, move.y + RED_FORWARD))
        {
            if (UnPackPiece(pieces[GetIndex(move.x + LEFT, move.y + RED_FORWARD)]).col != AIcolor)
            {
                score -= 100;

                if (UnPackPiece(pieces[GetIndex(move.x + LEFT, move.y + RED_FORWARD)]).level)
                    score -= 200;
            }
            else
                if (GetIndex(move.x + LEFT, move.y + RED_FORWARD) != move.index)
                    score += 300;
        }

        //extend the search to more tiles to detect targets
        //if (Get(move.x + RIGHT + RIGHT, move.y + WHITE_FORWARD + WHITE_FORWARD))
        //{
        //    if (UnPackPiece(pieces[GetIndex(move.x + RIGHT + RIGHT, move.y + WHITE_FORWARD + WHITE_FORWARD)]).col != AIcolor)
        //    {
        //        score += 50;
        //    }
        //}
        //if (Get(move.x + LEFT + LEFT, move.y + WHITE_FORWARD + WHITE_FORWARD))
        //{
        //    if (UnPackPiece(pieces[GetIndex(move.x + LEFT + LEFT, move.y + WHITE_FORWARD + WHITE_FORWARD)]).col != AIcolor)
        //    {
        //        score += 50;
        //    }
        //}
        //if (Get(move.x + RIGHT + RIGHT, move.y + RED_FORWARD + RED_FORWARD))
        //{
        //    if (UnPackPiece(pieces[GetIndex(move.x + RIGHT + RIGHT, move.y + RED_FORWARD + RED_FORWARD)]).col != AIcolor)
        //    {
        //        score += 50;
        //    }
        //}
        //if (Get(move.x + LEFT + LEFT, move.y + RED_FORWARD + RED_FORWARD))
        //{
        //    if (UnPackPiece(pieces[GetIndex(move.x + LEFT + LEFT, move.y + RED_FORWARD + RED_FORWARD)]).col != AIcolor)
        //    {
        //        score += 50;
        //    }
        //}

        return score;
    }
    private int SearchClosestTarget(Move move, int depth)
    {
        //search diagonal tiles within depth range, start at 2 because we have already checked adjacent tiles
        int count = 0;
        for (int i = 2; i < depth; i++)
        {
            //check diagonal down right position
            if (Get(move.x + i, move.y - i))
                if (UnPackPiece(pieces[GetIndex(move.x + i, move.y - i)]).col != AIcolor)
                    count++;

            //check diagonal down left position
            if (Get(move.x - i, move.y - i))
                if (UnPackPiece(pieces[GetIndex(move.x - i, move.y - i)]).col != AIcolor)
                    count++;

            //check diagonal up right position
            if (Get(move.x + i, move.y + i))
                if (UnPackPiece(pieces[GetIndex(move.x + i, move.y + i)]).col != AIcolor && UnPackPiece(pieces[GetIndex(move.x + i, move.y + i)]).level)
                    count++;

            //check diagonal up left position
            if (Get(move.x - i, move.y + i))
                if (UnPackPiece(pieces[GetIndex(move.x - i, move.y + i)]).col != AIcolor && UnPackPiece(pieces[GetIndex(move.x - i, move.y + i)]).level)
                    count++;
        }
        return count;
    }
    #endregion

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

    #region Win State
    private void Win(CheckersColor winColor)
    {
        string txt = "Game Over";
        switch (winColor)
        {
            case CheckersColor.RED:
                if (AIenabled)
                    txt = "Player Wins!";
                else
                    txt = "Player 2 Wins!";
                break;
            case CheckersColor.WHITE:
                if (AIenabled)
                    txt = "Computer WINS!";
                else
                    txt = "Player 1 Wins!";
                break;
            case CheckersColor.BLACK:
                txt = "Draw!";
                break;
            default:
                Debug.Log("Win was defaulted!");
                break;
        }

        //UI
        UIController.Instance.winUI.SetActive(true);
        UIController.Instance.winUI.GetComponent<TextMeshProUGUI>().text = txt;
        UIController.Instance.playAgainButton.SetActive(true);

        init = false;
    }
    //shell for Win()
    private void CheckWin()
    {
        //check if team white has no more pieces, else if team red has no more pieces, else if the current selected piece has zero moves and it is the last piece remaining
        if (whiteCount <= 0 || CheckMoves(CheckersColor.RED) <= 0)
            Win(CheckersColor.RED);
        if (redCount <= 0)
            Win(CheckersColor.WHITE);

        if (CheckMoves(CheckersColor.WHITE) <= 0 && CheckMoves(CheckersColor.RED) > 0)
            Win(CheckersColor.RED);
        if (CheckMoves(CheckersColor.RED) <= 0 && CheckMoves(CheckersColor.WHITE) > 0)
            Win(CheckersColor.WHITE);
        if (CheckMoves(CheckersColor.RED) <= 0 && CheckMoves(CheckersColor.WHITE) <= 0)
            Win(CheckersColor.BLACK);
    }
    #endregion

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
        if (Get(x, y) && board[x, y] == CheckersColor.BLACK && UnPackPiece(pieces[pieceIndex]).col == turnColor)
        {
            ResetGrid();
            this.pieceIndex = pieceIndex;

            grid[gridIndex].gameObject.GetComponent<SpriteRenderer>().color = Color.yellow;
            pieceMoves = FindMoves(this.pieceIndex);

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
            if ((piece.col == CheckersColor.RED && piece.y == 7) ||
            (piece.col == CheckersColor.WHITE && piece.y == 0))
                piece.level = true;

            //return piece as packed byte to list
            pieces[pieceIndex] = Pack(piece);
            selected = false;

            //jump case - todo: optimize to avoid if there is no jump
            CheckJump(piece);

            //change turn
            if (turnColor == CheckersColor.WHITE)
                turnColor = CheckersColor.RED;
            else if (turnColor == CheckersColor.RED)
                turnColor = CheckersColor.WHITE;

            Draw();

            //win state
            CheckWin();
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
                if (jump.col == CheckersColor.RED)
                {
                    redCount--;
                    whiteScore++;

                    if (jump.level)
                        whiteScore++;
                }
                else if (jump.col == CheckersColor.WHITE)
                {
                    whiteCount--;
                    redScore++;

                    if (jump.level)
                        redScore++;
                }

                CheckWin();
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
    private int CheckMoves(CheckersColor color)
    {
        int count = 0;
        for (int i = 0; i < pieces.Count; i++)
        {
            if (UnPackPiece(pieces[i]).col == color)
                count += FindPossibleMoves(i);
        }
        return count;
    }
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
        if (piece.col == CheckersColor.RED)
        {
            //right check
            if (xRIGHT > LOWER_BOUND && xRIGHT < UPPER_BOUND && yRED > LOWER_BOUND && yRED < UPPER_BOUND)
            {
                //forward right check
                if (board[xRIGHT, yRED] == CheckersColor.BLACK && Get(xRIGHT, yRED) == false)
                {
                    GetGrid(xRIGHT, yRED).GetComponent<SpriteRenderer>().color = Color.yellow;
                    count++;
                }

                //jump case
                if (xxRIGHT > LOWER_BOUND && xxRIGHT < UPPER_BOUND && yyRED > LOWER_BOUND && yyRED < UPPER_BOUND)
                {
                    //forwards right
                    if (Get(xRIGHT, yRED) && Get(xxRIGHT, yyRED) == false && UnPackPiece(pieces[GetIndex(xRIGHT, yRED)]).col == CheckersColor.WHITE)
                    {
                        GetGrid(xRIGHT, yRED).GetComponent<SpriteRenderer>().color = Color.green;
                        GetGrid(xxRIGHT, yyRED).GetComponent<SpriteRenderer>().color = Color.yellow;
                        jumpPieces.Add(pieces[GetIndex(xRIGHT, yRED)]);
                        count++;
                    }
                }
            }

            //backward right check
            if (piece.level && yWHITE > LOWER_BOUND && yWHITE < UPPER_BOUND && xRIGHT > LOWER_BOUND && xRIGHT < UPPER_BOUND)
            {
                if (board[xRIGHT, yWHITE] == CheckersColor.BLACK && Get(xRIGHT, yWHITE) == false)
                {
                    GetGrid(xRIGHT, yWHITE).GetComponent<SpriteRenderer>().color = Color.yellow;
                    count++;
                }

                if (yyWHITE > LOWER_BOUND && yyWHITE < UPPER_BOUND && xxRIGHT > LOWER_BOUND && xxRIGHT < UPPER_BOUND)
                {
                    if (Get(xRIGHT, yWHITE) && Get(xxRIGHT, yyWHITE) == false && UnPackPiece(pieces[GetIndex(xRIGHT, yWHITE)]).col == CheckersColor.WHITE)
                    {
                        GetGrid(xRIGHT, yWHITE).GetComponent<SpriteRenderer>().color = Color.green;
                        GetGrid(xxRIGHT, yyWHITE).GetComponent<SpriteRenderer>().color = Color.yellow;
                        jumpPieces.Add(pieces[GetIndex(xRIGHT, yWHITE)]);
                        count++;
                    }
                }
            }

            //left check
            if (xLEFT > LOWER_BOUND && xLEFT < UPPER_BOUND && yRED > LOWER_BOUND && yRED < UPPER_BOUND)
            {
                //forward left check
                if (board[xLEFT, yRED] == CheckersColor.BLACK && Get(xLEFT, yRED) == false)
                {
                    GetGrid(xLEFT, yRED).GetComponent<SpriteRenderer>().color = Color.yellow;
                    count++;
                }

                //jump case
                if (xxLEFT > LOWER_BOUND && xxLEFT < UPPER_BOUND && yyRED > LOWER_BOUND && yyRED < UPPER_BOUND)
                {
                    //forwards left
                    if (Get(xLEFT, yRED) && Get(xxLEFT, yyRED) == false && UnPackPiece(pieces[GetIndex(xLEFT, yRED)]).col == CheckersColor.WHITE)
                    {
                        GetGrid(xLEFT, yRED).GetComponent<SpriteRenderer>().color = Color.green;
                        GetGrid(xxLEFT, yyRED).GetComponent<SpriteRenderer>().color = Color.yellow;
                        jumpPieces.Add(pieces[GetIndex(xLEFT, yRED)]);
                        count++;
                    }
                }
            }

            //backward left check
            if (piece.level && yWHITE > LOWER_BOUND && yWHITE < UPPER_BOUND && xLEFT > LOWER_BOUND && xLEFT < UPPER_BOUND)
            {
                if (board[xLEFT, yWHITE] == CheckersColor.BLACK && Get(xLEFT, yWHITE) == false)
                {
                    GetGrid(xLEFT, yWHITE).GetComponent<SpriteRenderer>().color = Color.yellow;
                    count++;
                }

                if (yyWHITE > LOWER_BOUND && yyWHITE < UPPER_BOUND && xxLEFT > LOWER_BOUND && xxLEFT < UPPER_BOUND)
                {
                    if (Get(xLEFT, yWHITE) && Get(xxLEFT, yyWHITE) == false && UnPackPiece(pieces[GetIndex(xLEFT, yWHITE)]).col == CheckersColor.WHITE)
                    {
                        GetGrid(xLEFT, yWHITE).GetComponent<SpriteRenderer>().color = Color.green;
                        GetGrid(xxLEFT, yyWHITE).GetComponent<SpriteRenderer>().color = Color.yellow;
                        jumpPieces.Add(pieces[GetIndex(xLEFT, yWHITE)]);
                        count++;
                    }
                }
            }
        }
        else if (piece.col == CheckersColor.WHITE)
        {
            //forward right check
            if (xRIGHT > LOWER_BOUND && xRIGHT < UPPER_BOUND && yWHITE > LOWER_BOUND && yWHITE < UPPER_BOUND)
            {
                //forward right check
                if (board[xRIGHT, yWHITE] == CheckersColor.BLACK && Get(xRIGHT, yWHITE) == false)
                {
                    GetGrid(xRIGHT, yWHITE).GetComponent<SpriteRenderer>().color = Color.yellow;
                    count++;
                }

                //jump case
                if (xxRIGHT > LOWER_BOUND && xxRIGHT < UPPER_BOUND && yyWHITE > LOWER_BOUND && yyWHITE < UPPER_BOUND)
                {
                    //forwards right
                    if (Get(xRIGHT, yWHITE) && Get(xxRIGHT, yyWHITE) == false && UnPackPiece(pieces[GetIndex(xRIGHT, yWHITE)]).col == CheckersColor.RED)
                    {
                        GetGrid(xRIGHT, yWHITE).GetComponent<SpriteRenderer>().color = Color.green;
                        GetGrid(xxRIGHT, yyWHITE).GetComponent<SpriteRenderer>().color = Color.yellow;
                        jumpPieces.Add(pieces[GetIndex(xRIGHT, yWHITE)]);
                        count++;
                    }
                }
            }

            //backward right check
            if (piece.level && yRED > LOWER_BOUND && yRED < UPPER_BOUND && xRIGHT > LOWER_BOUND && xRIGHT < UPPER_BOUND)
            {
                if (board[xRIGHT, yRED] == CheckersColor.BLACK && Get(xRIGHT, yRED) == false)
                {
                    GetGrid(xRIGHT, yRED).GetComponent<SpriteRenderer>().color = Color.yellow;
                    count++;
                }

                if (yyRED > LOWER_BOUND && yyRED < UPPER_BOUND && xxRIGHT > LOWER_BOUND && xxRIGHT < UPPER_BOUND)
                {
                    if (Get(xRIGHT, yRED) && Get(xxRIGHT, yyRED) == false && UnPackPiece(pieces[GetIndex(xRIGHT, yRED)]).col == CheckersColor.RED)
                    {
                        GetGrid(xRIGHT, yRED).GetComponent<SpriteRenderer>().color = Color.green;
                        GetGrid(xxRIGHT, yyRED).GetComponent<SpriteRenderer>().color = Color.yellow;
                        jumpPieces.Add(pieces[GetIndex(xRIGHT, yRED)]);
                        count++;
                    }
                }
            }

            //forward left check
            if (xLEFT > LOWER_BOUND && xLEFT < UPPER_BOUND && yWHITE > LOWER_BOUND && yWHITE < UPPER_BOUND)
            {
                //forward left check
                if (board[xLEFT, yWHITE] == CheckersColor.BLACK && Get(xLEFT, yWHITE) == false)
                {
                    GetGrid(xLEFT, yWHITE).GetComponent<SpriteRenderer>().color = Color.yellow;
                    count++;
                }

                //jump case
                if (xxLEFT > LOWER_BOUND && xxLEFT < UPPER_BOUND && yyWHITE > LOWER_BOUND && yyWHITE < UPPER_BOUND)
                {
                    if (Get(xLEFT, yWHITE) && Get(xxLEFT, yyWHITE) == false && UnPackPiece(pieces[GetIndex(xLEFT, yWHITE)]).col == CheckersColor.RED)
                    {
                        GetGrid(xLEFT, yWHITE).GetComponent<SpriteRenderer>().color = Color.green;
                        GetGrid(xxLEFT, yyWHITE).GetComponent<SpriteRenderer>().color = Color.yellow;
                        jumpPieces.Add(pieces[GetIndex(xLEFT, yWHITE)]);
                        count++;
                    }
                }
            }

            //backward left check
            if (piece.level && yRED > LOWER_BOUND && yRED < UPPER_BOUND && xLEFT > LOWER_BOUND && xLEFT < UPPER_BOUND)
            {
                if (board[xLEFT, yRED] == CheckersColor.BLACK && Get(xLEFT, yRED) == false)
                {
                    GetGrid(xLEFT, yRED).GetComponent<SpriteRenderer>().color = Color.yellow;
                    count++;
                }

                //jump case
                if (yyRED > LOWER_BOUND && yyRED < UPPER_BOUND && xxLEFT > LOWER_BOUND && xxLEFT < UPPER_BOUND)
                {
                    if (Get(xLEFT, yRED) && Get(xxLEFT, yyRED) == false && UnPackPiece(pieces[GetIndex(xLEFT, yRED)]).col == CheckersColor.RED)
                    {
                        GetGrid(xLEFT, yRED).GetComponent<SpriteRenderer>().color = Color.green;
                        GetGrid(xxLEFT, yyRED).GetComponent<SpriteRenderer>().color = Color.yellow;
                        jumpPieces.Add(pieces[GetIndex(xLEFT, yRED)]);
                        count++;
                    }
                }
            }
        }

        return count;
    }
    public int FindPossibleMoves(int index)
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
        if (piece.col == CheckersColor.RED)
        {
            //right check
            if (xRIGHT > LOWER_BOUND && xRIGHT < UPPER_BOUND && yRED > LOWER_BOUND && yRED < UPPER_BOUND)
            {
                //forward right check
                if (board[xRIGHT, yRED] == CheckersColor.BLACK && Get(xRIGHT, yRED) == false)
                {
                    count++;
                }

                //jump case
                if (xxRIGHT > LOWER_BOUND && xxRIGHT < UPPER_BOUND && yyRED > LOWER_BOUND && yyRED < UPPER_BOUND)
                {
                    //forwards right
                    if (Get(xRIGHT, yRED) && Get(xxRIGHT, yyRED) == false && UnPackPiece(pieces[GetIndex(xRIGHT, yRED)]).col == CheckersColor.WHITE)
                    {
                        count++;
                    }
                }
            }

            //backward right check
            if (piece.level && yWHITE > LOWER_BOUND && yWHITE < UPPER_BOUND && xRIGHT > LOWER_BOUND && xRIGHT < UPPER_BOUND)
            {
                if (board[xRIGHT, yWHITE] == CheckersColor.BLACK && Get(xRIGHT, yWHITE) == false)
                {
                    count++;
                }

                if (yyWHITE > LOWER_BOUND && yyWHITE < UPPER_BOUND && xxRIGHT > LOWER_BOUND && xxRIGHT < UPPER_BOUND)
                {
                    if (Get(xRIGHT, yWHITE) && Get(xxRIGHT, yyWHITE) == false && UnPackPiece(pieces[GetIndex(xRIGHT, yWHITE)]).col == CheckersColor.WHITE)
                    {
                        count++;
                    }
                }
            }

            //left check
            if (xLEFT > LOWER_BOUND && xLEFT < UPPER_BOUND && yRED > LOWER_BOUND && yRED < UPPER_BOUND)
            {
                //forward left check
                if (board[xLEFT, yRED] == CheckersColor.BLACK && Get(xLEFT, yRED) == false)
                {
                    count++;
                }

                //jump case
                if (xxLEFT > LOWER_BOUND && xxLEFT < UPPER_BOUND && yyRED > LOWER_BOUND && yyRED < UPPER_BOUND)
                {
                    //forwards left
                    if (Get(xLEFT, yRED) && Get(xxLEFT, yyRED) == false && UnPackPiece(pieces[GetIndex(xLEFT, yRED)]).col == CheckersColor.WHITE)
                    {
                        count++;
                    }
                }
            }

            //backward left check
            if (piece.level && yWHITE > LOWER_BOUND && yWHITE < UPPER_BOUND && xLEFT > LOWER_BOUND && xLEFT < UPPER_BOUND)
            {
                if (board[xLEFT, yWHITE] == CheckersColor.BLACK && Get(xLEFT, yWHITE) == false)
                {
                    count++;
                }

                if (yyWHITE > LOWER_BOUND && yyWHITE < UPPER_BOUND && xxLEFT > LOWER_BOUND && xxLEFT < UPPER_BOUND)
                {
                    if (Get(xLEFT, yWHITE) && Get(xxLEFT, yyWHITE) == false && UnPackPiece(pieces[GetIndex(xLEFT, yWHITE)]).col == CheckersColor.WHITE)
                    {
                        count++;
                    }
                }
            }
        }
        else if (piece.col == CheckersColor.WHITE)
        {
            //forward right check
            if (xRIGHT > LOWER_BOUND && xRIGHT < UPPER_BOUND && yWHITE > LOWER_BOUND && yWHITE < UPPER_BOUND)
            {
                //forward right check
                if (board[xRIGHT, yWHITE] == CheckersColor.BLACK && Get(xRIGHT, yWHITE) == false)
                {
                    count++;
                }

                //jump case
                if (xxRIGHT > LOWER_BOUND && xxRIGHT < UPPER_BOUND && yyWHITE > LOWER_BOUND && yyWHITE < UPPER_BOUND)
                {
                    //forwards right
                    if (Get(xRIGHT, yWHITE) && Get(xxRIGHT, yyWHITE) == false && UnPackPiece(pieces[GetIndex(xRIGHT, yWHITE)]).col == CheckersColor.RED)
                    {
                        count++;
                    }
                }
            }

            //backward right check
            if (piece.level && yRED > LOWER_BOUND && yRED < UPPER_BOUND && xRIGHT > LOWER_BOUND && xRIGHT < UPPER_BOUND)
            {
                if (board[xRIGHT, yRED] == CheckersColor.BLACK && Get(xRIGHT, yRED) == false)
                {
                    count++;
                }

                if (yyRED > LOWER_BOUND && yyRED < UPPER_BOUND && xxRIGHT > LOWER_BOUND && xxRIGHT < UPPER_BOUND)
                {
                    if (Get(xRIGHT, yRED) && Get(xxRIGHT, yyRED) == false && UnPackPiece(pieces[GetIndex(xRIGHT, yRED)]).col == CheckersColor.RED)
                    {
                        count++;
                    }
                }
            }

            //forward left check
            if (xLEFT > LOWER_BOUND && xLEFT < UPPER_BOUND && yWHITE > LOWER_BOUND && yWHITE < UPPER_BOUND)
            {
                //forward left check
                if (board[xLEFT, yWHITE] == CheckersColor.BLACK && Get(xLEFT, yWHITE) == false)
                {
                    count++;
                }

                //jump case
                if (xxLEFT > LOWER_BOUND && xxLEFT < UPPER_BOUND && yyWHITE > LOWER_BOUND && yyWHITE < UPPER_BOUND)
                {
                    if (Get(xLEFT, yWHITE) && Get(xxLEFT, yyWHITE) == false && UnPackPiece(pieces[GetIndex(xLEFT, yWHITE)]).col == CheckersColor.RED)
                    {
                        count++;
                    }
                }
            }

            //backward left check
            if (piece.level && yRED > LOWER_BOUND && yRED < UPPER_BOUND && xLEFT > LOWER_BOUND && xLEFT < UPPER_BOUND)
            {
                if (board[xLEFT, yRED] == CheckersColor.BLACK && Get(xLEFT, yRED) == false)
                {
                    count++;
                }

                //jump case
                if (yyRED > LOWER_BOUND && yyRED < UPPER_BOUND && xxLEFT > LOWER_BOUND && xxLEFT < UPPER_BOUND)
                {
                    if (Get(xLEFT, yRED) && Get(xxLEFT, yyRED) == false && UnPackPiece(pieces[GetIndex(xLEFT, yRED)]).col == CheckersColor.RED)
                    {
                        count++;
                    }
                }
            }
        }

        return count;
    }
    //finds possible moves for AI
    public List<Move> FindListOfMoves(int index)
    {
        if (index < 0)
            return null;

        CheckersPiece piece = UnPackPiece(pieces[index]);
        List<Move> moves = new List<Move>();

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

        //forward right check
        if (xRIGHT > LOWER_BOUND && xRIGHT < UPPER_BOUND && yWHITE > LOWER_BOUND && yWHITE < UPPER_BOUND)
        {
            //forward right check
            if (board[xRIGHT, yWHITE] == CheckersColor.BLACK && Get(xRIGHT, yWHITE) == false)
            {
                GetGrid(xRIGHT, yWHITE).GetComponent<SpriteRenderer>().color = Color.yellow;
                moves.Add(new Move(xRIGHT, yWHITE, index));
            }

            //jump case
            if (xxRIGHT > LOWER_BOUND && xxRIGHT < UPPER_BOUND && yyWHITE > LOWER_BOUND && yyWHITE < UPPER_BOUND)
            {
                //forwards right
                if (Get(xRIGHT, yWHITE) && Get(xxRIGHT, yyWHITE) == false && UnPackPiece(pieces[GetIndex(xRIGHT, yWHITE)]).col == CheckersColor.RED)
                {
                    GetGrid(xRIGHT, yWHITE).GetComponent<SpriteRenderer>().color = Color.green;
                    GetGrid(xxRIGHT, yyWHITE).GetComponent<SpriteRenderer>().color = Color.yellow;
                    jumpPieces.Add(pieces[GetIndex(xRIGHT, yWHITE)]);
                    moves.Add(new Move(xxRIGHT, yyWHITE, index, GetIndex(xRIGHT, yWHITE)));
                }
            }
        }

        //backward right check (promotion only)
        if (piece.level && yRED > LOWER_BOUND && yRED < UPPER_BOUND && xRIGHT > LOWER_BOUND && xRIGHT < UPPER_BOUND)
        {
            if (board[xRIGHT, yRED] == CheckersColor.BLACK && Get(xRIGHT, yRED) == false)
            {
                GetGrid(xRIGHT, yRED).GetComponent<SpriteRenderer>().color = Color.yellow;
                moves.Add(new Move(xRIGHT, yRED, index));
            }

            //jump case
            if (yyRED > LOWER_BOUND && yyRED < UPPER_BOUND && xxRIGHT > LOWER_BOUND && xxRIGHT < UPPER_BOUND)
            {
                if (Get(xRIGHT, yRED) && Get(xxRIGHT, yyRED) == false && UnPackPiece(pieces[GetIndex(xRIGHT, yRED)]).col == CheckersColor.RED)
                {
                    GetGrid(xRIGHT, yRED).GetComponent<SpriteRenderer>().color = Color.green;
                    GetGrid(xxRIGHT, yyRED).GetComponent<SpriteRenderer>().color = Color.yellow;
                    jumpPieces.Add(pieces[GetIndex(xRIGHT, yRED)]);
                    moves.Add(new Move(xxRIGHT, yyRED, index, GetIndex(xRIGHT, yRED)));
                }
            }
        }

        //forward left check
        if (xLEFT > LOWER_BOUND && xLEFT < UPPER_BOUND && yWHITE > LOWER_BOUND && yWHITE < UPPER_BOUND)
        {
            //forward left check
            if (board[xLEFT, yWHITE] == CheckersColor.BLACK && Get(xLEFT, yWHITE) == false)
            {
                GetGrid(xLEFT, yWHITE).GetComponent<SpriteRenderer>().color = Color.yellow;
                moves.Add(new Move(xLEFT, yWHITE, index));
            }

            //jump case
            if (xxLEFT > LOWER_BOUND && xxLEFT < UPPER_BOUND && yyWHITE > LOWER_BOUND && yyWHITE < UPPER_BOUND)
            {
                if (Get(xLEFT, yWHITE) && Get(xxLEFT, yyWHITE) == false && UnPackPiece(pieces[GetIndex(xLEFT, yWHITE)]).col == CheckersColor.RED)
                {
                    GetGrid(xLEFT, yWHITE).GetComponent<SpriteRenderer>().color = Color.green;
                    GetGrid(xxLEFT, yyWHITE).GetComponent<SpriteRenderer>().color = Color.yellow;
                    jumpPieces.Add(pieces[GetIndex(xLEFT, yWHITE)]);
                    moves.Add(new Move(xxLEFT, yyWHITE, index, GetIndex(xLEFT, yWHITE)));
                }
            }
        }

        //backward left check (promotion only)
        if (piece.level && yRED > LOWER_BOUND && yRED < UPPER_BOUND && xLEFT > LOWER_BOUND && xLEFT < UPPER_BOUND)
        {
            if (board[xLEFT, yRED] == CheckersColor.BLACK && Get(xLEFT, yRED) == false)
            {
                GetGrid(xLEFT, yRED).GetComponent<SpriteRenderer>().color = Color.yellow;
                moves.Add(new Move(xLEFT, yRED, index));
            }

            //jump case
            if (yyRED > LOWER_BOUND && yyRED < UPPER_BOUND && xxLEFT > LOWER_BOUND && xxLEFT < UPPER_BOUND)
            {
                if (Get(xLEFT, yRED) && Get(xxLEFT, yyRED) == false && UnPackPiece(pieces[GetIndex(xLEFT, yRED)]).col == CheckersColor.RED)
                {
                    GetGrid(xLEFT, yRED).GetComponent<SpriteRenderer>().color = Color.green;
                    GetGrid(xxLEFT, yyRED).GetComponent<SpriteRenderer>().color = Color.yellow;
                    jumpPieces.Add(pieces[GetIndex(xLEFT, yRED)]);
                    moves.Add(new Move(xxLEFT, yyRED, index, GetIndex(xLEFT, yRED)));
                }
            }
        }

        return moves;
    }
    //shell for FindListOfMoves
    public List<Move> FindAIMoves()
    {
        List<Move> moves = new List<Move>();
        for (int i = 0; i < pieces.Count; i++)
        {
            if (UnPackPiece(pieces[i]).col == AIcolor)
            {
                //add each pieces move to list
                List<Move> pieceMoves = FindListOfMoves(i);
                for (int k = 0; k < pieceMoves.Count; k++)
                {
                    //moves.Add(pieceMoves[k]);
                    //if (pieceMoves[k] != lastMove)
                    //    if (whiteCount == 1 || pieceMoves.Count == 1)
                    //        moves.Add(pieceMoves[k]);

                    if (pieceMoves[k] != lastMove || pieceMoves.Count == 1)
                        moves.Add(pieceMoves[k]);
                }
            }
        }
        return moves;
    }
    #endregion

    #region INIT
    private void Init()
    {
        //world data
        board = new CheckersColor[ROWS, COLS];
        turnColor = CheckersColor.RED;

        AIdiff = GameController.Instance.difficulty;

        //grid
        verticalOffset = (int)Camera.main.orthographicSize;
        horizontalOffset = verticalOffset * (Screen.width / Screen.height);
        GenerateGrid();

        whiteCount = 12;
        redCount = 12;

        //activate AI if necessary
        if (GameController.Instance)
            if (GameController.Instance.AIenabled)
                ActivateAI();

        init = true;
    }
    public void GenerateGrid()
    {
        ClearGrid();
        ClearPieces();
        pieces.Clear();

        //generate checkers grid
        for (int x = 0; x < ROWS; x++)
        {
            for (int y = 0; y < COLS; y++)
            {
                if ((x + y) % 2 == 0)
                {
                    //set custom color and set the board position to be a valid move tile (pieces can only move on black spots)
                    DrawGrid(mainGridColor, x, y);
                    board[x, y] = CheckersColor.BLACK;
                }
                else
                {
                    //set custom color and set the board position to be a invalid move tile
                    DrawGrid(otherGridColor, x, y);
                    board[x, y] = CheckersColor.RED;
                }
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
                if (board[x, y] == CheckersColor.BLACK && count < MAX_PIECES)
                {
                    byte b = Pack(new CheckersPiece(CheckersColor.WHITE, x, y, false));
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
                if (board[x, y] == CheckersColor.BLACK && count < MAX_PIECES)
                {
                    byte b = Pack(new CheckersPiece(CheckersColor.RED, y, x, false));
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
        ResetGrid();
        ResetPieces();
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
    public void ResetGrid()
    {
        ClearGrid();

        for (int x = 0; x < ROWS; x++)
        {
            for (int y = 0; y < COLS; y++)
            {
                if ((x + y) % 2 == 0)
                    DrawGrid(mainGridColor, x, y);
                else
                    DrawGrid(otherGridColor, x, y);
            }
        }
    }
    public void ResetPieces()
    {
        ClearPieces();

        for (int i = 0; i < pieces.Count; i++)
            DrawPiece(pieces[i]);
    }
    public void DrawGrid(Color col, int x, int y)
    {
        GameObject go = Instantiate(grid_black);

        go.transform.SetParent(GameObject.Find("Grid").transform);
        go.GetComponent<SpriteRenderer>().color = col;
        go.transform.position = new Vector3(x - horizontalOffset, y - verticalOffset, GRID_LAYER);

        grid.Add(go);
    }
    public void DrawPiece(byte piece)
    {
        CheckersPiece checkersPiece = UnPackPiece(piece);
        switch (checkersPiece.col)
        {
            case CheckersColor.WHITE:
                DrawWhitePiece(checkersPiece.x, checkersPiece.y, checkersPiece.level);
                break;
            case CheckersColor.RED:
                DrawRedPiece(checkersPiece.x, checkersPiece.y, checkersPiece.level);
                break;
            default:
                Debug.Log("Drawing piece was defaulted");
                break;
        }
    }
    //private void DrawBlackSquare(int x, int y)
    //{
    //    GameObject go = Instantiate(grid_black);

    //    go.transform.SetParent(GameObject.Find("Grid").transform);
    //    go.transform.position = new Vector3(x - horizontalOffset, y - verticalOffset, GRID_LAYER);

    //    grid.Add(go);
    //}
    //private void DrawRedSquare(int x, int y)
    //{
    //    GameObject go = Instantiate(grid_red);

    //    go.transform.SetParent(GameObject.Find("Grid").transform);
    //    go.transform.position = new Vector3(x - horizontalOffset, y - verticalOffset, GRID_LAYER);

    //    grid.Add(go);
    //}
    private void DrawWhitePiece(int x, int y, bool king)
    {
        GameObject go;
        if (king)
        {
            go = Instantiate(piece_kingwhite);
            go.transform.GetChild(0).GetComponent<SpriteRenderer>().color = teamOneColor;
        }
        else
        {
            go = Instantiate(piece_white);
            go.GetComponent<SpriteRenderer>().color = teamOneColor;
        }

        go.transform.SetParent(GameObject.Find("Pieces").transform);
        go.transform.position = new Vector3(x - horizontalOffset, y - verticalOffset, PIECE_LAYER);

        objects.Add(go);
    }
    private void DrawRedPiece(int x, int y, bool king)
    {
        GameObject go;
        if (king)
        {
            go = Instantiate(piece_kingred);
            go.transform.GetChild(0).GetComponent<SpriteRenderer>().color = teamTwoColor;
        }
        else
        {
            go = Instantiate(piece_red);
            go.GetComponent<SpriteRenderer>().color = teamTwoColor;
        }

        go.transform.SetParent(GameObject.Find("Pieces").transform);
        go.transform.position = new Vector3(x - horizontalOffset, y - verticalOffset, PIECE_LAYER);

        objects.Add(go);
    }
    #endregion
}