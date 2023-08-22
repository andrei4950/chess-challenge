using ChessChallenge.API;
using System;
using System.Collections.Generic;

namespace ChessChallenge.Example
{
    using ChessChallenge.API;
using System;

    public class EvilBot : IChessBot
{
    Dictionary <ulong, int> transpositionTable = new();
    Dictionary <ulong, int> moveScoreTable = new();
    const int inf = 30000;
    private const int clearlyWinningDifference = 1100; 
    int nodes = 0; //DEBUG
    private int currentEval = 0;
    private bool isEndgame;

    private readonly int[] whitePawnDesiredPositions = { 0, 0, 0, 0, 0, 0, 0, 0, 
                                                10, 10, 10, 0, 0, 10, 10, 10,
                                                0, 5, 0, 11, 11, 0, 5, 0,
                                                0, 0, 0, 21, 21, 0, 0, 0,
                                                5, 5, 5, 25, 25, 5, 5, 5,
                                                20, 20, 20, 30, 30, 20, 20, 20,
                                                40, 40, 40, 40, 40, 40, 40, 40,
                                                40, 40, 40, 40, 40, 40, 40, 40};

    public Move Think(Board board, Timer timer)
    {
        currentEval = Eval(board) * (board.IsWhiteToMove ? 1 : -1);

        isEndgame = CountMaterialOfColour(board, true) + CountMaterialOfColour(board, false) < 2800;
        int depth = 0;
        Move bestMove;
        int initTime, endTime, bestEval;

        do
        {
            depth++;
            nodes = 0;
            initTime = timer.MillisecondsRemaining;
            (bestEval, bestMove) = MiniMax(board, depth, -inf, inf, false, true);
            endTime = timer.MillisecondsRemaining;
        }
        while((initTime - endTime) * 200 < endTime && depth < 20);
        //while(depth < 2); //DEBUG
        Console.Write("Eval: "); //DEBUG
        Console.Write(bestEval * (board.IsWhiteToMove ? 1 : -1)); //DEBUG
        Console.Write(" nodes visited:  "); //DEBUG
        Console.Write(nodes); //DEBUG
        Console.Write(" time elapsed: "); //DEBUG
        Console.Write(initTime - endTime); //DEBUG
        Console.Write(" at depth "); //DEBUG
        Console.WriteLine(depth); //DEBUG
        Console.WriteLine(MoveLineString(board)); //DEBUG
        Console.WriteLine(board.GetFenString()); //DEBUG
        
        return bestMove;
    }

    /// <summary>
    /// Returns evaluation of the position calculated at specified depth (high value if position is good for the player to move)
    /// Makes use of transpositionTable for improved efficiency and moveScoreTable for sorting the moves (also better efficiency)
    /// </summary>
    public (int, Move) MiniMax(Board board, int depth, int a, int b, bool isLastMoveCapture, bool firstCall = false)
    {
        nodes++; //DEBUG
        // Check if node is final node
        if (board.IsDraw()) 
            return (0, Move.NullMove);

        if (board.IsInCheckmate())
            return (-inf - depth, Move.NullMove);
  
        int shallowEval = Eval(board);

        // or if we reached depth limit
        if((depth <= 0 && !isLastMoveCapture) || depth <= -8)
        {
            return (shallowEval, Move.NullMove);
        }

        // do not go deeper if we know we are winning/losing
        int absEval = shallowEval * (board.IsWhiteToMove ? 1 : -1);
        if (!firstCall && (absEval - currentEval >= clearlyWinningDifference || absEval - currentEval <= -clearlyWinningDifference))
        {
            return (shallowEval, Move.NullMove);
        }
        
        //get available moves
        System.Span<Move> allMoves = stackalloc Move[128];
        board.GetLegalMovesNonAlloc(ref allMoves, depth <= 0);
        if (allMoves.Length == 0)
        {
            return (shallowEval, Move.NullMove);
        }

        Move bestMove = allMoves[0];

        //sort start
        System.Span<int> moveOrderKeys = stackalloc int[allMoves.Length];
        for (int i = 0; i < allMoves.Length; i++)
            moveOrderKeys[i] = GetMoveScore(board, allMoves[i]);
        MemoryExtensions.Sort(moveOrderKeys, allMoves);
        //sort end

        string fen = board.GetFenString();
        ulong key = board.ZobristKey ^ ((ulong)board.PlyCount << 1) ^ (ulong)(board.IsWhiteToMove ? 1 : 0);
        int bestEval = -inf;
        if (depth <= 0)
        {
            bestEval = shallowEval; // do not go deeper if a player prefers to not capture anything
            if (bestEval > b) // beta pruning
            {
                transpositionTable[key] = bestEval;
                return (bestEval, Move.NullMove);
            }
            if (bestEval > a) // update alpha (the improvement is probably only minor)
            {
                a = bestEval;
            }
        }
        foreach (Move move in allMoves)
        {
            board.MakeMove(move);
            (int eval, Move temp) = MiniMax(board, depth - 1, -b, -a, move.IsCapture);
            board.UndoMove(move);
            
            moveScoreTable[move.RawValue + board.ZobristKey] = eval;
            eval = - eval;
            if (eval > b) // beta pruning
            {
                transpositionTable[key] = eval;
                return (eval, move);
            }
            if(eval > bestEval)
            {
                bestEval = eval;
                bestMove = move;
                if (eval > a) // update alpha
                {
                    a = eval;
                }
            }
        }
        transpositionTable[key] = bestEval;
        return (bestEval, bestMove);
    }

    /// <summary>
    /// Returns evaluation of the position (high value if position is good for the player to move)
    /// </summary>
    public int Eval(Board board)
    {
        ulong key = board.ZobristKey ^ ((ulong)board.PlyCount << 1) ^ (ulong)(board.IsWhiteToMove ? 1 : 0);
        if(transpositionTable.TryGetValue(key, out var value))
        {
            return value;
        }
        int eval = CountMaterialOfColour(board, board.IsWhiteToMove) - CountMaterialOfColour(board, !board.IsWhiteToMove);
        eval -= board.IsInCheck() ? 1 : 0;
        transpositionTable.Add(key, eval);
        return eval;
    }

    public int CountMaterialOfColour(Board board, bool colour)
    {
        PieceList pawns = board.GetPieceList(PieceType.Pawn, colour);
        int eval = 100 * pawns.Count;
        PieceList knights = board.GetPieceList(PieceType.Knight, colour);
        eval += 300 * knights.Count;
        PieceList bishops = board.GetPieceList(PieceType.Bishop, colour);
        eval += 300 * bishops.Count;
        PieceList rooks = board.GetPieceList(PieceType.Rook, colour);
        eval += 500 * rooks.Count;
        PieceList queens = board.GetPieceList(PieceType.Queen, colour);
        eval += 900 * queens.Count;

        // bonusess:
        //pawns
        for (int i = 0; i < pawns.Count; i++)
        {
            int index = pawns.GetPiece(i).Square.Index;
            if (!colour)
                index = 63 - index;
            eval += whitePawnDesiredPositions[index];
        }
        //other pieces
        eval += GetDistEvalBonus(board, knights);
        eval += GetDistEvalBonus(board, bishops);
        eval += GetDistEvalBonus(board, rooks);
        eval += GetDistEvalBonus(board, queens);
        return eval;
    }

    /// <summary>
    /// Returns evaluation of the move 
    /// Good moves recieve lower values
    /// Best efficiency obtained with iterative deepening
    /// Uses moveScoreTable
    /// </summary>
    public int GetMoveScore(Board board, Move move)
    {
        if(moveScoreTable.TryGetValue(move.RawValue + board.ZobristKey, out var value))
        {
            return value;
        }
        else
        {
            board.MakeMove(move);
            int score = Eval(board);
            board.UndoMove(move);
            moveScoreTable[move.RawValue + board.ZobristKey] = score;
            return score;
        }
    }
    
    public static int GetDistEvalBonus(Board board, PieceList pieceList)
    {
        int bonus = 0;
        for (int i = 0; i < pieceList.Count; i++)
        {
            bonus -= DistanceFromKing(board, pieceList.GetPiece(i), !pieceList.IsWhitePieceList) * 2;
            if (board.PlyCount > 30)
            {
                bonus -= DistanceFromKing(board, pieceList.GetPiece(i), pieceList.IsWhitePieceList);
            }
        }
        return bonus;
    }
    public static int DistanceFromKing(Board board, Piece piece, bool kingColour)
    {
        return Math.Abs(board.GetKingSquare(kingColour).File - piece.Square.File) + Math.Abs(board.GetKingSquare(kingColour).Rank - piece.Square.Rank);
    }


    // 176 tokens for GetMoveLine and MoveLineString
    /// <summary>
    /// Debug function used for generating the best-play predicted move line
    /// Exclusively uses moveScoreTable as source of information
    /// Does not evaluate positions or change the state of the board/bot object
    /// </summary>
    Move[] GetMoveLine(Board startingBoard)
    {
        Board board = Board.CreateBoardFromFEN(startingBoard.GetFenString()); // make a copy of the board (we do not undo moves)
        Move[] moveLine = new Move[50];
        for(int j = 0; j < moveLine.Length; j++)
        {
            int bestScore = inf;
            int index = -1;
            Move[] moves = board.GetLegalMoves();
            for(int i = 0; i < moves.Length; i++)
            {
                if(moveScoreTable.TryGetValue(board.ZobristKey + moves[i].RawValue, out int score))
                {
                    if (score <= bestScore)
                    {
                        bestScore = score;
                        index = i;
                    }
                }
            }
            if (index == -1)
            {
                Array.Resize(ref moveLine, j);
                break;
            }
            Move bestMove = moves[index];
            moveLine[j] = bestMove;
            board.MakeMove(bestMove);
        }
        return moveLine;
    }

    /// <summary>
    /// Returns a move line string
    /// Used for debigging
    /// Makes use of GetMoveLine
    /// </summary>
    string MoveLineString(Board startingBoard)
    {
        string myString = "";
        foreach(Move move in GetMoveLine(startingBoard))
        {
            myString += move.ToString().Split(' ')[1] + ' ';
        }
        return myString;
    }
}
}