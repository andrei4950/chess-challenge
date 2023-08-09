﻿using ChessChallenge.API;
using System;
using System.Collections.Generic;

/*struct PositionCashe
{
    public int shallowEval, deepEval;
    public Move bestMove;

    public PositionCashe(int shallowEval, int deepEval, Move bestMove)
    {
        this.shallowEval = shallowEval;
        this.deepEval = deepEval;
        this.bestMove = bestMove;
    }

    public PositionCashe(int shallowEval)
    {
        this.shallowEval = shallowEval;
        this.deepEval = shallowEval;
        this.bestMove = Move.NullMove;
    }
}*/

public class MyBot : IChessBot
{
    Dictionary <ulong, int> shallowTranspositionTable = new();
    Dictionary <ulong, int> deepTranspositionTable = new();
    private const int clearlyWinningDifference = 1100; 

    private const double captureBonusDepth = 0.4;
    private int currentEval = 0;
    private bool isEndgame;

    private int[] whitePawnDesiredPositions = { 0, 0, 0, 0, 0, 0, 0, 0, 
                                                10, 10, 10, 0, 0, 10, 10, 10,
                                                0, 5, 0, 11, 11, 0, 5, 0,
                                                0, 0, 0, 21, 21, 0, 0, 0,
                                                5, 5, 5, 25, 25, 5, 5, 5,
                                                20, 20, 20, 30, 30, 20, 20, 20,
                                                40, 40, 40, 40, 40, 40, 40, 40,
                                                40, 40, 40, 40, 40, 40, 40, 40};

    public Move Think(Board board, Timer timer)
    {
        currentEval = Eval(board, true);

        isEndgame = CountMaterialOfColour(board, true) + CountMaterialOfColour(board, false) < 2800;
        float depth = 1;
        Move bestMove;
        int initTime, endTime;

        do
        {
            initTime = timer.MillisecondsRemaining;
            (int bestEval, bestMove) = MinMax(board, depth, Int16.MinValue, Int16.MaxValue);
            endTime = timer.MillisecondsRemaining;

            Console.Write("Eval: "); //DEBUG
            Console.WriteLine(bestEval * (board.IsWhiteToMove ? 1 : -1)); //DEBUG
            Console.Write("time elapsed: "); //DEBUG
            Console.Write(initTime - endTime); //DEBUG
            Console.Write(" at depth "); //DEBUG
            Console.WriteLine(depth); //DEBUG
            depth++;
        }
        while((initTime - endTime) * 400 < endTime && depth < 20);
        //while(true); //DEBUG
        return bestMove;
    }

    public (int, Move) MinMax(Board board, double depth, int a, int b)
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
                deepTranspositionTable[board.ZobristKey] = b * (board.IsWhiteToMove ? 1 : -1);
                return (b, move);
            }
            if (bestEval > a)
            {
                a = bestEval;
                bestMove = move;
            }
        }
        deepTranspositionTable[board.ZobristKey] = a * (board.IsWhiteToMove ? 1 : -1);
        return (a, bestMove);
    }

    public int Eval(Board board, bool colour)
    {
        if(shallowTranspositionTable.TryGetValue(board.ZobristKey, out var value))
        {
            return value * (colour ? 1 : -1);
        }
        int eval = CountMaterialOfColour(board, colour) - CountMaterialOfColour(board, !colour);
        shallowTranspositionTable.Add(board.ZobristKey, eval * (colour ? 1 : -1));
        deepTranspositionTable.Add(board.ZobristKey, eval * (colour ? 1 : -1));
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

    public bool IsCheck(Board board, Move move) // 27 tokens
    {
        board.MakeMove(move);
        bool isCheck = board.IsInCheck();
        board.UndoMove(move);
        return isCheck;
    }

    public int GetMoveScore(Board board, Move move)
    {
        int score;
        board.MakeMove(move);
        if(deepTranspositionTable.TryGetValue(board.ZobristKey, out var value))
        {
            score = value;
        }else
        {
            score = Eval(board, board.IsWhiteToMove);
        }
        board.UndoMove(move);
               
        if (IsCheck(board, move))
        {
            score -= 1;
        }
        return score;
    }
    public readonly int[] _pieceValues = {0, 100, 300, 300, 500, 900, 0};

    public int GetDistEvalBonus(Board board, PieceList pieceList)
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
    public int DistanceFromKing(Board board, Piece piece, bool kingColour)
    {
        return Math.Abs(board.GetKingSquare(kingColour).File - piece.Square.File) + Math.Abs(board.GetKingSquare(kingColour).Rank - piece.Square.Rank);
    }
}