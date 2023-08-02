using ChessChallenge.API;
using System;

namespace ChessChallenge.Example
{
    // A simple bot that can spot mate in one, and always captures the most valuable piece it can.
    // Plays randomly otherwise.
    public class EvilBot : IChessBot
    {
        // Piece values: null, pawn, knight, bishop, rook, queen, king
        public Move Think(Board board, Timer timer)
        {
            //Console.WriteLine(timer.MillisecondsRemaining);
            int depth = 3;
            if (timer.MillisecondsRemaining < 10000)
            {
                depth = 2;
                if (timer.MillisecondsRemaining < 1000)
                {
                    depth = 1;
                }
            }

            Move[] allMoves = board.GetLegalMoves();
            Move bestMove = allMoves[0];
            int bestEval = 0;

            if (board.IsWhiteToMove)
            {
                bestEval = -100000;
                foreach (Move move in allMoves)
                {
                    board.MakeMove(move);
                    if (MinMax(board, depth) > bestEval)
                    {
                        bestMove = move;
                        bestEval = Eval(board);
                    }
                    board.UndoMove(move);
                }
            }
            else
            {
                bestEval = 100000;
                foreach (Move move in allMoves)
                {
                    board.MakeMove(move);
                    if (MinMax(board, depth) < bestEval)
                    {
                        bestMove = move;
                        bestEval = Eval(board);
                    }
                    board.UndoMove(move);
                }
            }
            return bestMove;
        }

        int MinMax(Board board, int depth)
        {
            if(depth == 0 || board.IsInCheckmate())
            {
                return Eval(board);
            }

            if (board.IsWhiteToMove)
            {
                int bestEval = -100000;
                Move[] allMoves = board.GetLegalMoves();

                foreach (Move move in allMoves)
                {
                    board.MakeMove(move);
                    bestEval = Math.Max(MinMax(board, depth - 1), bestEval);
                    board.UndoMove(move);
                }
                return bestEval;
            }
            else
            {
                int bestEval = 100000;
                Move[] allMoves = board.GetLegalMoves();

                foreach (Move move in allMoves)
                {
                    board.MakeMove(move);
                    bestEval = Math.Min(MinMax(board, depth - 1), bestEval);
                    board.UndoMove(move);
                }
                return bestEval;
            }
        }

        int Eval(Board board)
        {
            if (board.IsDraw()) return 0;
            
            if (board.IsInCheckmate())
            {
                if (board.IsWhiteToMove) return -100000;
                else return 100000;
            }

            int eval = 0;

            eval += 100 * board.GetPieceList(PieceType.Pawn, true).Count;
            eval -= 100 * board.GetPieceList(PieceType.Pawn, false).Count;
            eval += 300 * board.GetPieceList(PieceType.Knight, true).Count;
            eval -= 300 * board.GetPieceList(PieceType.Knight, false).Count;
            eval += 300 * board.GetPieceList(PieceType.Bishop, true).Count;
            eval -= 300 * board.GetPieceList(PieceType.Bishop, false).Count;
            eval += 500 * board.GetPieceList(PieceType.Rook, true).Count;
            eval -= 500 * board.GetPieceList(PieceType.Rook, false).Count;
            eval += 900 * board.GetPieceList(PieceType.Queen, true).Count;
            eval -= 900 * board.GetPieceList(PieceType.Queen, false).Count;

            return eval;
        }
    }
}