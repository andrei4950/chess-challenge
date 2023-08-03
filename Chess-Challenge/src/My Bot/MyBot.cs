﻿using ChessChallenge.API;
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
        
        Console.Write("Eval: "); //DEBUG
        Console.WriteLine(bestEval/100.0 * colourMultiplier); //DEBUG
        /*Console.Write("Positions evaluated: "); //DEBUG
        Console.WriteLine(positionsEvaluated); //DEBUG
        Console.Write("Branches pruned: "); //DEBUG
        Console.WriteLine(branchesPruned); //DEBUG
        */
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
            //branchesPruned++; //DEBUG
            return Int32.MaxValue;
        }
        if (eval - currentEval <= -clearlyWinningDifference)
        {
            //branchesPruned++; //DEBUG
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
                    //branchesPruned++; //DEBUG
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
                    //branchesPruned++; //DEBUG
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

        return countMaterialOfColour(board, true) - countMaterialOfColour(board, false);
    }

    int countMaterialOfColour(Board board, bool colour)
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
}