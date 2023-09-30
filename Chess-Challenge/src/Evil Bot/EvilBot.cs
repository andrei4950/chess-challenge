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
    int nodes = 0;
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
        isEndgame = CountMaterialOfColour(board, true) + CountMaterialOfColour(board, false) < 2800;
        int depth = 0;
        int initTime, endTime;
        initTime = timer.MillisecondsRemaining;
        do
        {
            transpositionTable.Clear();
            depth++;
            MiniMax(board, depth, -inf, inf, false);
            endTime = timer.MillisecondsRemaining;
        }
        while((initTime - endTime) * 200 < endTime && depth < 20);
        Move bestMove = GetMoveLine(board)[0];
        moveScoreTable.Clear();
        return bestMove;
    }

    /// <summary>
    /// Returns evaluation of the position calculated at specified depth (high value if position is good for the player to move)
    /// Makes use of transpositionTable for improved efficiency and moveScoreTable for sorting the moves (also better efficiency)
    /// </summary>
    public int MiniMax(Board board, int depth, int a, int b, bool isLastMoveCapture)
    {
        nodes++; // DEBUG
        // Check if node was visited before
        ulong key = board.ZobristKey ^ ((ulong)board.PlyCount << 1) ^ (ulong)(isLastMoveCapture ? 1 : 0) ^ ((ulong)depth << 8)^ ((ulong)a << 16)^ ((ulong)b << 24);
        if(transpositionTable.TryGetValue(key, out var value)) 
            return value;

        // Check if node is final node
        if (board.IsDraw()) 
            return 0;

        if (board.IsInCheckmate())
            return -inf - depth;
  
        int shallowEval = Eval(board);

        // or if we reached depth limit
        if((depth <= 0 && !isLastMoveCapture) || depth <= -8)
            return shallowEval;

        
        //get available moves
        System.Span<Move> allMoves = stackalloc Move[128];
        board.GetLegalMovesNonAlloc(ref allMoves, depth <= 0);

        if (allMoves.Length == 0) 
            return shallowEval;

        if (depth >= -6) SortMoves(board, ref allMoves);

        int bestEval = -inf;
        if (depth <= 0)
        {
            bestEval = shallowEval; // do not go deeper if a player prefers to not capture anything
            if (bestEval > b) // beta pruning
                return bestEval;

            if (bestEval > a) // update alpha (the improvement is probably only minor)
                a = bestEval;
        }
        foreach (Move move in allMoves)
        {
            board.MakeMove(move);
            int eval =  -MiniMax(board, depth - 1, -b, -a, move.IsCapture);
            board.UndoMove(move);
            
            moveScoreTable[move.RawValue ^ board.ZobristKey] = -eval - 900;
            if (eval > b) // beta pruning
            {
                transpositionTable[key] = eval;
                return eval;
            }
            if(eval > bestEval)
            {
                bestEval = eval;
                if (eval > a) // update alpha
                    a = eval;
            }
        }
        transpositionTable[key] = bestEval;
        return bestEval;
    }

    /// <summary>
    /// Returns evaluation of the position (high value if position is good for the player to move)
    /// </summary>
    public int Eval(Board board)
    {
        return CountMaterialOfColour(board, board.IsWhiteToMove) - CountMaterialOfColour(board, !board.IsWhiteToMove) - (board.IsInCheck() ? 1 : 0);
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
    /// Sorts the moves based on move score
    /// </summary>
    public void SortMoves(Board board, ref Span<Move> moves)
    {
        System.Span<int> moveOrderKeys = stackalloc int[moves.Length];
        for (int i = 0; i < moves.Length; i++)
            moveOrderKeys[i] = GetMoveScore(board, moves[i]);
        MemoryExtensions.Sort(moveOrderKeys, moves);
    }

    /// <summary>
    /// Returns evaluation of the move 
    /// Good moves recieve lower values
    /// Best efficiency obtained with iterative deepening
    /// Uses moveScoreTable
    /// </summary>
    public int GetMoveScore(Board board, Move move)
    {
        if(moveScoreTable.TryGetValue(move.RawValue ^ board.ZobristKey, out var value))
        {
            return value;
        }
        else
        {
            board.MakeMove(move);
            int score = Eval(board);
            board.UndoMove(move);
            moveScoreTable[move.RawValue ^ board.ZobristKey] = score;
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
    /// </summary>
    public Move[] GetMoveLine(Board startingBoard)
    {
        Board board = Board.CreateBoardFromFEN(startingBoard.GetFenString()); // make a copy of the board (we do not undo moves)
        Move[] moveLine = new Move[50];
        for(int j = 0; j < moveLine.Length; j++)
        {
            System.Span<Move> moves = stackalloc Move[128];
            board.GetLegalMovesNonAlloc(ref moves);
            if (moves.Length == 0)
            {
                Array.Resize(ref moveLine, j);
                break;
            }
            SortMoves(board, ref moves);
            moveLine[j] = moves[0];
            board.MakeMove(moves[0]);
        }
        return moveLine;
    }
}
}