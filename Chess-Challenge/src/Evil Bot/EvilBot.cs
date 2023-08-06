using ChessChallenge.API;
using System;

namespace ChessChallenge.Example
{
    using ChessChallenge.API;
using System;

public class EvilBot : IChessBot
{
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

    int[] whiteKnightDesiredPositions = {   0, 0, 0, 0, 0, 0, 0, 0, 
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
    int[] rookOpeningDesiredFile = {0, 0, 5, 10, 10, 10, 0, 0};

    public Move Think(Board board, Timer timer)
    {
        double depth = 3;
        if (timer.MillisecondsRemaining < 20000)
        {
            depth = 2;
        }
        currentEval = Eval(board, true);

        isEndgame = CountMaterialOfColour(board, true) + CountMaterialOfColour(board, false) < 2800;

        (int bestEval, Move bestMove) = MinMax(board, depth, Int16.MinValue, Int16.MaxValue);
        
        Console.Write("Eval: "); //DEBUG
        Console.WriteLine(bestEval * (board.IsWhiteToMove ? 1 : -1)); //DEBUG
        Console.Write("Current eval: "); //DEBUG
        Console.WriteLine(currentEval); //DEBUG
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
        // kings
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
        eval *= (colour ? 1 : -1);
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

        //bishops
        for (int i = 0; i < bishops.Count; i++)
        {
            int index = bishops.GetPiece(i).Square.Index;
            if (!colour)
                index = 63 - index;
            eval += whiteBishopDesiredPositions[index];
        }

        // knights
        for (int i = 0; i < knights.Count; i++)
        {
            eval += whiteKnightDesiredPositions[pawns.GetPiece(i).Square.Index];
        }

        //rooks
        if (!isEndgame)
        {
            for (int i = 0; i < rooks.Count; i++)
            {
                eval += rookOpeningDesiredFile[rooks.GetPiece(i).Square.File];
            }
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
    public class OrherEvilBot : IChessBot
    {
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
                                                0, 5, 20, 20, 20, 22, 5, 0, 
                                                0, 5, 20, 20, 20, 20, 5, 0, 
                                                0, 10, 20, 20, 20, 20, 10, 0, 
                                                0, 5, 20, 20, 20, 20, 5, 0, 
                                                0, 0, 0, 5, 5, 0, 0, 0, 
                                                0, 0, 0, 0, 0, 0, 0, 0};

        public Move Think(Board board, Timer timer)
        {
            double depth = 3;
            if (timer.MillisecondsRemaining < 20000)
            {
                depth = 2;
                if (timer.MillisecondsRemaining < 5000)
                {
                    depth = 1;
                }
            }
            currentEval = Eval(board);

            Move[] allMoves = board.GetLegalMoves();
            Move bestMove = allMoves[0];
            int bestEval = 0;
            int colourMultiplier;
            if (board.IsWhiteToMove)
                colourMultiplier = 1;
            else
                colourMultiplier = -1;


            bestEval = Int16.MinValue;
            foreach (Move move in allMoves)
            {
                int a = Int16.MinValue;
                int b = Int16.MaxValue;
                board.MakeMove(move);
                int eval = colourMultiplier * MinMax(board, depth, a, b);
                if (eval > bestEval)
                {
                    bestMove = move;
                    bestEval = eval;
                }
                board.UndoMove(move);
            }
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
                return Int32.MaxValue;
            }
            if (eval - currentEval <= -clearlyWinningDifference)
            {
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
                        break;
                    }
                    b = Math.Min(b, bestEval);

                }
                return bestEval;
            }
        }

        int Eval(Board board)
        {
            //positionsEvaluated++; //DEBUG
            if (board.IsDraw()) 
                return 0;

            if (board.IsInCheckmate())
            {
                if (board.IsWhiteToMove) 
                    return Int16.MinValue;
                else 
                    return Int16.MaxValue;
            }

            int eval = 0;
            PieceList whitePawns = board.GetPieceList(PieceType.Pawn, true);
            eval += 100 * whitePawns.Count;
            PieceList blackPawns = board.GetPieceList(PieceType.Pawn, false);
            eval -= 100 * blackPawns.Count;
            PieceList whiteKnights = board.GetPieceList(PieceType.Knight, true);
            eval += 300 * whiteKnights.Count;
            PieceList blackKnights = board.GetPieceList(PieceType.Knight, false);
            eval -= 300 * blackKnights.Count;
            eval += 300 * board.GetPieceList(PieceType.Bishop, true).Count;
            eval -= 300 * board.GetPieceList(PieceType.Bishop, false).Count;
            eval += 500 * board.GetPieceList(PieceType.Rook, true).Count;
            eval -= 500 * board.GetPieceList(PieceType.Rook, false).Count;
            eval += 900 * board.GetPieceList(PieceType.Queen, true).Count;
            eval -= 900 * board.GetPieceList(PieceType.Queen, false).Count;

            // bonusess:
            //pawns
            for (int i = 0; i < whitePawns.Count; i++)
            {
                eval += whitePawnDesiredPositions[whitePawns.GetPiece(i).Square.Index];
            }
            for (int i = 0; i < blackPawns.Count; i++)
            {
                eval -= whitePawnDesiredPositions[63 - blackPawns.GetPiece(i).Square.Index]; // we use this tric for symmetric desired positions
            }

            // knights
            for (int i = 0; i < whiteKnights.Count; i++)
            {
                eval += whiteKnightDesiredPositions[whiteKnights.GetPiece(i).Square.Index];
            }
            for (int i = 0; i < blackKnights.Count; i++)
            {
                eval -= whiteKnightDesiredPositions[63 - blackKnights.GetPiece(i).Square.Index]; // we use this tric for symmetric desired positions
            }
            return eval;
        }
    }
}