using ChessChallenge.API;
using System;

public class MyBot : IChessBot
{
    //int positionsEvaluated = 0;
    //int branchesPruned = 0;
    const int clearlyWinningDifference = 1100; 

    const double captureBonusDepth = 0.4;
    int currentEval = 0;

    int[] whitePawnDesiredPositions = { 0, 0, 0, 0, 0, 0, 0, 0, 
                                        10, 10, 10, 0, 0, 10, 10, 10,
                                        0, 5, 0, 11, 11, 0, 5, 0,
                                        0, 0, 0, 21, 21, 0, 0, 0,
                                        5, 5, 5, 25, 25, 5, 5, 5,
                                        20, 20, 20, 30, 30, 20, 20, 20,
                                        40, 40, 40, 40, 40, 40, 40, 40,
                                        40, 40, 40, 40, 40, 40, 40, 40};

    int[] whiteKnightDesiredPositions = {   0, 0, 0, 0, 0, 0, 0, 0, 
                                            0, 0, 0, 5, 5, 0, 0, 0, 
                                            0, 5, 20, 20, 20, 20, 5, 0, 
                                            0, 10, 20, 20, 20, 20, 10, 0, 
                                            0, 10, 20, 20, 20, 20, 10, 0, 
                                            0, 5, 20, 20, 20, 20, 5, 0, 
                                            0, 0, 0, 5, 5, 0, 0, 0, 
                                            0, 0, 0, 0, 0, 0, 0, 0};

    public Move Think(Board board, Timer timer)
    {
        //positionsEvaluated = 0; //DEBUG
        //branchesPruned = 0; //DEBUG

        double depth = 3;
        if (timer.MillisecondsRemaining < 20000)
        {
            depth = 2;
        }
        currentEval = Eval(board) * (board.IsWhiteToMove ? 1 : -1);

        (int bestEval, Move bestMove) = MinMax(board, depth, Int16.MinValue, Int16.MaxValue);
        
        Console.Write("Eval: "); //DEBUG
        Console.WriteLine(bestEval * (board.IsWhiteToMove ? 1 : -1)); //DEBUG
        Console.Write("Current eval: "); //DEBUG
        Console.WriteLine(currentEval); //DEBUG
        /*Console.Write("Positions evaluated: "); //DEBUG
        Console.WriteLine(positionsEvaluated); //DEBUG
        Console.Write("Branches pruned: "); //DEBUG
        Console.WriteLine(branchesPruned); //DEBUG
        */
        return bestMove;
    }

    (int, Move) MinMax(Board board, double depth, int a, int b)
    {
        if (board.IsDraw()) 
            return (0, Move.NullMove);

        if (board.IsInCheckmate())
            return (Int16.MinValue, Move.NullMove);
            
        int eval = Eval(board);

        if(depth <= 0)
        {
            return (eval, Move.NullMove);
        }

        int colour = board.IsWhiteToMove ? 1 : -1;
        /*
        if (eval - currentEval >= clearlyWinningDifference)
        {
            //branchesPruned++; //DEBUG
            return Int32.MaxValue;
        }
        if (eval - currentEval <= -clearlyWinningDifference)
        {
            //branchesPruned++; //DEBUG
            return Int32.MinValue;
        }
        */
        Move[] allMoves = board.GetLegalMoves();
        Move bestMove = allMoves[0];

        //sort start
        int[] moveOrderKeys = new int[allMoves.Length];
        for (int i = 0; i < allMoves.Length; i++)
            moveOrderKeys[i] = GetMoveScore(board, allMoves[i]);
        Array.Sort(moveOrderKeys, allMoves);
        //sort end

        foreach (Move move in allMoves)
        {
            board.MakeMove(move);
            (int bestEval, Move temp) = MinMax(board, depth - 1 + (move.IsCapture ? captureBonusDepth : 0), -b, -a);
            bestEval = - bestEval;
            board.UndoMove(move);
            if (bestEval > b)
            {
                //branchesPruned++; //DEBUG
                return (b, move);
            }
            if (bestEval > a)
            {
                a = bestEval;
                bestMove = move;
            }
        }
        return (a, bestMove);
    }

    int Eval(Board board)
    {
        //positionsEvaluated++; //DEBUG

        return CountMaterialOfColour(board, board.IsWhiteToMove) - CountMaterialOfColour(board, !board.IsWhiteToMove);
    }

    int CountMaterialOfColour(Board board, bool colour)
    {
        PieceList pawns = board.GetPieceList(PieceType.Pawn, colour);
        int eval = 100 * pawns.Count;
        PieceList knights = board.GetPieceList(PieceType.Knight, colour);
        eval += 300 * knights.Count;
        eval += 300 * board.GetPieceList(PieceType.Bishop, colour).Count;
        eval += 500 * board.GetPieceList(PieceType.Rook, colour).Count;
        eval += 900 * board.GetPieceList(PieceType.Queen, colour).Count;

        // bonusess:
        //pawns
        for (int i = 0; i < pawns.Count; i++)
        {
            int index = pawns.GetPiece(i).Square.Index;
            if (!colour)
                index = 63 - index;
            eval += whitePawnDesiredPositions[index];
        }

        // knights
        for (int i = 0; i < knights.Count; i++)
        {
            eval += whiteKnightDesiredPositions[pawns.GetPiece(i).Square.Index];
        }
        return eval;
    }

    bool IsCheck(Board board, Move move) // 27 tokens
    {
        board.MakeMove(move);
        bool isCheck = board.IsInCheck();
        board.UndoMove(move);
        return isCheck;
    }

    int GetMoveScore(Board board, Move move)
    {
        int score = - 2 * GetPieceValue(move.CapturePieceType) - 3 * GetPieceValue(move.PromotionPieceType);
        if (IsCheck(board, move))
        {
            score -= 80;
        }
        if (board.SquareIsAttackedByOpponent(move.TargetSquare))
        {
            score += 40 + GetPieceValue(move.MovePieceType);
        }
        return score;
    }

    int GetPieceValue(PieceType piece)
    {
        switch (piece)
        {
            case PieceType.Pawn: return 100;
            case PieceType.Knight: return 300;
            case PieceType.Bishop: return 300;
            case PieceType.Rook: return 500;
            case PieceType.Queen: return 900;
        }
        return 0;
    }
}