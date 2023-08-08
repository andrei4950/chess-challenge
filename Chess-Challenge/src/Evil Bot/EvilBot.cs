using ChessChallenge.API;
using System;

namespace ChessChallenge.Example
{
    using ChessChallenge.API;
using System;

    public class EvilBot : IChessBot
    {
        //int positionsEvaluated = 0;
        //int branchesPruned = 0;
        const int clearlyWinningDifference = 1100; 

        const double captureBonusDepth = 0.4;
        int currentEval = 0;
        bool isEndgame;

        int[] whitePawnDesiredPositions = { 0, 0, 0, 0, 0, 0, 0, 0, 
                                            10, 10, 10, 0, 0, 10, 10, 10,
                                            0, 5, 0, 11, 11, 0, 5, 0,
                                            0, 0, 0, 21, 21, 0, 0, 0,
                                            5, 5, 5, 25, 25, 5, 5, 5,
                                            20, 20, 20, 30, 30, 20, 20, 20,
                                            40, 40, 40, 40, 40, 40, 40, 40,
                                            40, 40, 40, 40, 40, 40, 40, 40};

    /* int[] whiteKnightDesiredPositions = {   0, 0, 0, 0, 0, 0, 0, 0, 
                                                0, 0, 0, 5, 5, 0, 0, 0, 
                                                0, 5, 20, 20, 20, 20, 5, 0, 
                                                0, 10, 20, 20, 20, 20, 10, 0, 
                                                0, 10, 20, 20, 20, 20, 10, 0, 
                                                0, 5, 20, 20, 20, 20, 5, 0, 
                                                0, 0, 0, 5, 5, 0, 0, 0, 
                                                0, 0, 0, 0, 0, 0, 0, 0};
        
        int[] whiteBishopDesiredPositions = {   -3, -3, -5, -5, -5, -5, -3, -3, 
                                                -3, 3, -3, -2, -2, -3, 3, -3, 
                                                0, 0, 0, 0, 0, 0, 0, 0, 
                                                0, 0, 3, 0, 0, 3, 0, 0, 
                                                0, 3, 0, 0, 0, 0, 3, 0, 
                                                0, 0, 0, 0, 0, 0, 0, 0, 
                                                0, 0, 0, 0, 0, 0, 0, 0, 
                                                0, 0, 0, 0, 0, 0, 0, 0};

        int[] kingDesiredFile = {2, 15, 30, 0, 15, 0, 30, 2};
        int[] rookOpeningDesiredFile = {0, 0, 5, 10, 10, 10, 0, 0};*/

        public Move Think(Board board, Timer timer)
        {
            currentEval = Eval(board, true);

            isEndgame = CountMaterialOfColour(board, true) + CountMaterialOfColour(board, false) < 2800;
            float depth = 2;
            Move bestMove;
            int initTime, endTime;

            do
            {
                initTime = timer.MillisecondsRemaining;
                (int bestEval, bestMove) = MinMax(board, depth, Int16.MinValue, Int16.MaxValue);
                endTime = timer.MillisecondsRemaining;

                /*Console.Write("Eval: "); //DEBUG
                Console.WriteLine(bestEval * (board.IsWhiteToMove ? 1 : -1)); //DEBUG
                Console.Write("time elapsed: "); //DEBUG
                Console.Write(initTime - endTime); //DEBUG
                Console.Write(" at depth "); //DEBUG
                Console.WriteLine(depth); //DEBUG*/
                depth++;
            }
            while((initTime - endTime) * 400 < endTime && depth < 20);
            return bestMove;
        }

        (int, Move) MinMax(Board board, double depth, int a, int b)
        {
            // Check if node is final node
            if (board.IsDraw()) 
                return (0, Move.NullMove);

            if (board.IsInCheckmate())
                return (Int16.MinValue, Move.NullMove);
                
            int eval = Eval(board, board.IsWhiteToMove);

            // or if we reached depth limit
            if(depth <= 0)
            {
                return (eval, Move.NullMove);
            }

            // do not go deeper if we know we are winning/losing
            int absEval = eval * (board.IsWhiteToMove ? 1 : -1);
            if (absEval - currentEval >= clearlyWinningDifference || absEval - currentEval <= -clearlyWinningDifference)
            {
                return (eval, Move.NullMove);
            }
            
            //get available moves
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

        int Eval(Board board, bool colour)
        {
            int eval = 0;
            /* kings
            if(!isEndgame)
            {
                if (board.GetKingSquare(true).Rank == 0)
                {
                    eval += kingDesiredFile[board.GetKingSquare(true).File];
                }
                if (board.GetKingSquare(false).Rank == 7)
                {
                    eval -= kingDesiredFile[board.GetKingSquare(false).File];
                }
            }
            eval *= (colour ? 1 : -1);*/
            eval += CountMaterialOfColour(board, colour) - CountMaterialOfColour(board, !colour);
            return eval;
        }

        int CountMaterialOfColour(Board board, bool colour)
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
            eval += GetDistEvalBonus(board, knights, colour);
            eval += GetDistEvalBonus(board, bishops, colour);
            eval += GetDistEvalBonus(board, rooks, colour);
            eval += GetDistEvalBonus(board, queens, colour);
            eval += GetDistEvalBonus(board, board.GetPieceList(PieceType.King, colour), colour);
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
            int score = - 2 * _pieceValues[(int)move.CapturePieceType] - 3 * _pieceValues[(int)move.PromotionPieceType];
            if (IsCheck(board, move))
            {
                score -= 80;
            }
            if (board.SquareIsAttackedByOpponent(move.TargetSquare))
            {
                score += 40 + _pieceValues[(int)move.MovePieceType];
            }
            return score;
        }
        private readonly int[] _pieceValues = {0, 100, 300, 300, 500, 900, 0};

        int GetDistEvalBonus(Board board, PieceList pieceList, bool colour)
        {
            int bonus = 0;
            for (int i = 0; i < pieceList.Count; i++)
            {
                bonus -= DistanceFromEnemyKing(board, pieceList.GetPiece(i), colour);
            }
            return bonus * 2;
        }
        int DistanceFromEnemyKing(Board board, Piece piece, bool colour)
        {
            return Math.Abs(board.GetKingSquare(colour).File - piece.Square.File) + Math.Abs(board.GetKingSquare(colour).Rank - piece.Square.Rank);
        }
    }
}