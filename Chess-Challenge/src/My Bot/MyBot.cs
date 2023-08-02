using ChessChallenge.API;
using System;

public class MyBot : IChessBot
{
    int positionsEvaluated = 0;
    int branchesPruned = 0;
    const int clearlyWinningDifference = 2000; 

    const double captureBonusDepth = 0.4;
    int currentEval = 0;

    // Piece values: null, pawn, knight, bishop, rook, queen, king
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };

    public Move Think(Board board, Timer timer)
    {
        positionsEvaluated = 0; //DEBUG
        branchesPruned = 0; //DEBUG

        double depth = 3;
        if (timer.MillisecondsRemaining < 10000)
        {
            depth = 2;
            if (timer.MillisecondsRemaining < 1000)
            {
                depth = 1;
            }
        }
        currentEval = Eval(board);
        Move[] allMoves = board.GetLegalMoves();
        Move bestMove = allMoves[0];
        int bestEval = 0;

        if (board.IsWhiteToMove)
        {
            bestEval = Int16.MinValue;
            foreach (Move move in allMoves)
            {
                int a = Int16.MinValue;
                int b = Int16.MaxValue;
                board.MakeMove(move);
                int eval = MinMax(board, depth, a, b);
                if (eval > bestEval)
                {
                    bestMove = move;
                    bestEval = eval;
                }
                board.UndoMove(move);
            }
        }
        else
        {
            bestEval = Int16.MaxValue;
            foreach (Move move in allMoves)
            {
                int a = Int16.MinValue;
                int b = Int16.MaxValue;
                board.MakeMove(move);
                int eval = MinMax(board, depth, a, b);
                if (eval < bestEval)
                {
                    bestMove = move;
                    bestEval = eval;
                }
                board.UndoMove(move);
            }
        }
        Console.Write("Eval: "); //DEBUG
        Console.WriteLine(bestEval/100); //DEBUG
        Console.Write("Positions evaluated: "); //DEBUG
        Console.WriteLine(positionsEvaluated); //DEBUG
        Console.Write("Branches pruned: "); //DEBUG
        Console.WriteLine(branchesPruned); //DEBUG
        return bestMove;
    }

    int MinMax(Board board, double depth, int a, int b)
    {
        int eval = Eval(board);
        if(depth <= 0 || board.IsInCheckmate())
        {
            return eval;
        }
        if (eval - currentEval >= clearlyWinningDifference)
        {
            Console.WriteLine("Pruned for clearlyWinningDifference");
            branchesPruned++; //DEBUG
            return Int32.MaxValue;
        }
        if (eval - currentEval <= -clearlyWinningDifference)
        {
            Console.WriteLine("Pruned for clearlyWinningDifference");
            branchesPruned++; //DEBUG
            return Int32.MinValue;
        }

        if (board.IsWhiteToMove)
        {
            int bestEval = Int16.MinValue;
            Move[] allMoves = board.GetLegalMoves();

            foreach (Move move in allMoves)
            {
                board.MakeMove(move);
                if (move.IsCapture) bestEval = Math.Max(MinMax(board, depth - 1 + captureBonusDepth, a, b), bestEval);
                else bestEval = Math.Max(MinMax(board, depth - 1, a, b), bestEval);
                board.UndoMove(move);
                if (bestEval > b)
                {
                    branchesPruned++; //DEBUG
                    break;
                }
                a = Math.Max(a, bestEval);
            }
            return bestEval;
        }
        else
        {
            int bestEval = Int16.MaxValue;
            Move[] allMoves = board.GetLegalMoves();

            foreach (Move move in allMoves)
            {
                board.MakeMove(move);
                if (move.IsCapture) bestEval = Math.Min(MinMax(board, depth - 1 + captureBonusDepth, a, b), bestEval);
                else bestEval = Math.Min(MinMax(board, depth - 1, a, b), bestEval);
                board.UndoMove(move);
                if (bestEval < a)
                {
                    branchesPruned++; //DEBUG
                    break;
                }
                b = Math.Min(b, bestEval);

            }
            return bestEval;
        }
    }

    int Eval(Board board)
    {
        positionsEvaluated++; //DEBUG
        if (board.IsDraw()) return 0;
        
        if (board.IsInCheckmate())
        {
            if (board.IsWhiteToMove) return Int16.MinValue;
            else return Int16.MaxValue;
        }

        int eval = 0;

        eval += pieceValues[1] * board.GetPieceList(PieceType.Pawn, true).Count;
        eval -= pieceValues[1] * board.GetPieceList(PieceType.Pawn, false).Count;
        eval += pieceValues[2] * board.GetPieceList(PieceType.Knight, true).Count;
        eval -= pieceValues[2] * board.GetPieceList(PieceType.Knight, false).Count;
        eval += pieceValues[3] * board.GetPieceList(PieceType.Bishop, true).Count;
        eval -= pieceValues[3] * board.GetPieceList(PieceType.Bishop, false).Count;
        eval += pieceValues[4] * board.GetPieceList(PieceType.Rook, true).Count;
        eval -= pieceValues[4] * board.GetPieceList(PieceType.Rook, false).Count;
        eval += pieceValues[5] * board.GetPieceList(PieceType.Queen, true).Count;
        eval -= pieceValues[5] * board.GetPieceList(PieceType.Queen, false).Count;

        return eval;
    }
}