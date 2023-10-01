using ChessChallenge.API;
using System;
using System.Collections.Generic;

public class MyBot : IChessBot
{
    Dictionary <ulong, int> transpositionTable = new();
    Dictionary <ulong, int> moveScoreTable = new();
    const int inf = 30000;
    int nodes = 0; //DEBUG

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
        int initTime, endTime, bestEval;
        initTime = timer.MillisecondsRemaining;
        do
        {
            transpositionTable.Clear();
            depth++;
            bestEval = MiniMax(board, depth, -inf, inf, false, false, true);
            endTime = timer.MillisecondsRemaining;
            Console.Write("Eval: "); //DEBUG
            Console.Write(bestEval * (board.IsWhiteToMove ? 1 : -1)); //DEBUG
            Console.Write(" nodes visited:  "); //
            Console.Write(nodes); //DEBUG
            Console.Write(" time elapsed: "); //DEBUG
            Console.Write(initTime - endTime); //DEBUG
            Console.Write(" nodes/s :  "); //DEBUG
            Console.Write(nodes * 1000 / (initTime - endTime + 1)); //DEBUG
            Console.Write(" at depth "); //DEBUG
            Console.WriteLine(depth); //DEBUG
        }
        //while(depth < 20); //DEBUG
        while((initTime - endTime) * 200 < endTime && depth < 20);
        Move bestMove = GetMoveLine(board)[0];
        moveScoreTable.Clear();
        return bestMove;
    }

    /// <summary>
    /// Returns evaluation of the position calculated at specified depth (high value if position is good for the player to move)
    /// Makes use of transpositionTable for improved efficiency and moveScoreTable for sorting the moves (also better efficiency)
    /// </summary>
    public int MiniMax(Board board, int depth, int a, int b, bool isLastMoveCapture, bool isQuiescenceSearch, bool wasInCheck)
    {
        nodes++; // DEBUG
        // Check if node was visited before
        ulong key = board.ZobristKey ^ ((ulong)board.PlyCount << 3) ^ ((ulong)depth << 10)^ ((ulong)a << 18)^ ((ulong)b << 26) ^ (ulong)(isLastMoveCapture ? 1 : 0) ^ (ulong)(isQuiescenceSearch ? 2 : 0) ^ (ulong)(wasInCheck ? 4 : 0);
        if(transpositionTable.TryGetValue(key, out var value)) 
            return value;

        // Check if node is final node
        if (board.IsDraw()) 
            return 0;

        if (board.IsInCheckmate())
            return -inf - depth;

        bool isInCheck = board.IsInCheck();

        if (isQuiescenceSearch && !isLastMoveCapture && !isInCheck && !wasInCheck) // only want captures and checks in quiescence search (or getting out of check)
            return inf;
  
        int shallowEval = CountMaterialOfColour(board, board.IsWhiteToMove) - CountMaterialOfColour(board, !board.IsWhiteToMove);

        // or if we reached depth limit
        if(depth <= 0)
            if (isQuiescenceSearch) return shallowEval;
            else return StartQuiescenceSearch(board, a, b, wasInCheck);
        
        //get available moves
        System.Span<Move> allMoves = stackalloc Move[128];
        board.GetLegalMovesNonAlloc(ref allMoves);

        SortMoves(board, ref allMoves);

        int bestEval = -inf;
        if (isQuiescenceSearch && !isInCheck)
        {
            bestEval = shallowEval; // do not go deeper if a player prefers to not capture anything
            if (bestEval > b) // beta pruning
            {
                return bestEval;
            }
        }
        foreach (Move move in allMoves)
        {
            board.MakeMove(move);
            int eval = -MiniMax(board, depth - 1, -b, -a, move.IsCapture, isQuiescenceSearch, isInCheck);
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
                {
                    a = eval;
                }
            }
        }
        transpositionTable[key] = bestEval;
        return bestEval;
    }

    public int StartQuiescenceSearch(Board board, int a, int b, bool wasInCheck)
    {
        /*int depth = 0;
        int bestEval;
        do
        {
            depth++;
            bestEval = MiniMax(board, depth, a, b, true, true, wasInCheck);
            //Move bestMove = GetMoveLine(board)[0]; // DEBUG
        }
        while(depth < 8); 
        return bestEval;*/
        return MiniMax(board, 8, a, b, true, true, wasInCheck);
    }

    /// <summary>
    /// Returns evaluation of the position (high value if position is good for the player to move)
    /// </summary>
    public int Eval(Board board)
    {
        int eval = CountMaterialOfColour(board, board.IsWhiteToMove) - CountMaterialOfColour(board, !board.IsWhiteToMove);
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