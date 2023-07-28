using ChessChallenge.API;
using System;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;

namespace ChessChallenge.Example
{
    public class EvilBot : IChessBot
    {
    // use https://bitboard-editor.vercel.app/ for editing these values
    public ulong whitePawnOpeningTable = 0x183c3ce700;
    public ulong blackPawnOpeningTable = 0xe73c3c18000000;
    public ulong whitePawnMiddleTable = 0x3c7ee7c300;
    public ulong blackPawnMiddleTable = 0xc3e77e3c000000;
    public ulong whiteKingTable = 0xc7;
    public ulong blackKingTable = 0xc700000000000000;
    public ulong whiteKnightTable = 0x667e7e7e1800;
    public ulong blackKnightTable = 0x187e7e7e660000;
    public ulong whiteBishopTable = 0x3c7effff4200;
    public ulong blackBishopTable = 0x42ffff7e3c0000;
    public ulong whiteRookTable = 0xff3c3c3c3c3c3c;
    public ulong blackRookTable = 0x3c3c3c3c3c3cff00;
    public ulong whiteQueenTable = 0x7e7e7e7e7e00;
    public ulong blackQueenTable = 0x7e7e7e7e7e0000;
    public ulong whiteKingEndGameTable = 0xffffffffff0000;
    public ulong blackKingEndGameTable = 0xffffffffff00;
    public ulong whitePawnEndGameTable = 0xffffff00000000;
    public ulong blackPawnEndGameTable = 0xffffff00;

    public int GameState = 0;
    private bool _abortSearch = false;
    
    public int[] pieceWeights = {0, 100, 310, 330, 500, 1000, 10000 };
    public Move Think(Board board, Timer timer)
    {
        return IterativeDeepening(board, timer);
    }

    public Move RootNegamax(Board board, Timer timer, int depth)
    {
        // Console.WriteLine("Current Eval: " + Evaluate(board));
        // Console.WriteLine("Current Game State: " + (IsEndGame ? "End game" : "Early game/Mid game"));

        // shuffle the moves for randomness in case eval finds equal positions
        Move[] moves = board.GetLegalMoves();
        Move[] orderedMoves = moves.OrderBy(x => x.IsCapture).ToArray();

        ChecKGameState(board);

        Move bestMove = orderedMoves[0];
        double bestScore = Double.NegativeInfinity;

        foreach (Move move in orderedMoves)
        {
            if (timer.MillisecondsElapsedThisTurn > 500)
            {
                _abortSearch = true;
            }
            
            board.MakeMove(move);
            double score = -Negamax(Double.NegativeInfinity, Double.PositiveInfinity, board, depth);
            board.UndoMove(move);

            if (score > bestScore)
            {
                bestScore = score;
                bestMove = move;
            }
        }

        // root negamax

        return bestMove;
    }

    public Move IterativeDeepening(Board board, Timer timer)
    {
        _abortSearch = false;
        Move bestMove = Move.NullMove;
        
        for (int currentDepth = 1; currentDepth <= 20; currentDepth++)
        {
            Move iterationBestMove = RootNegamax(board, timer, currentDepth);

            if (_abortSearch) break;

            // Console.WriteLine("Current Depth: " + currentDepth + " Best Move: " + iterationBestMove);
            
            bestMove = iterationBestMove;
        }

        return bestMove;
    }

    public void ChecKGameState(Board board)
    {
        PieceList[] allPieceLists = board.GetAllPieceLists();

        int piecesCount = allPieceLists[1].Count + allPieceLists[2].Count + allPieceLists[3].Count +
                          allPieceLists[4].Count;

        if (piecesCount > 12) GameState = 0;
        else if (piecesCount < 5) GameState = 2;
        else GameState = 1;
    }

    // https://www.chessprogramming.org/Negamax
    public double Negamax(double alpha, double beta, Board board, int depth)
    {
        if (_abortSearch) return 0;
        if (depth == 0) return Quiesce(board, alpha, beta, 2);

        double max = Double.NegativeInfinity;
        Move[] moves = board.GetLegalMoves();
        Move[] orderedMoves = moves.OrderBy(x => x.IsCapture).ToArray();
 
        foreach (Move move in orderedMoves)
        {
            board.MakeMove(move);
            double score = -Negamax(-beta, -alpha, board, depth - 1);
            board.UndoMove(move);

            if( score >= beta ) return beta;
            if (score > alpha) alpha = score;
        }

        return alpha;
    }
    
    // simplified eval based on material weight and """bitboard piece square tables"""
    public int EvaluateSimple(Board board)
    {
        int weightInverter = (board.IsWhiteToMove ? 1 : -1);
        
        if (board.IsInCheckmate())
        {
            return pieceWeights[6] * weightInverter;
        }
        
        int materialWeight = 0;
    
        PieceList[] allPieceLists = board.GetAllPieceLists();
    
        foreach (PieceList list in allPieceLists)
        {
            materialWeight += (pieceWeights[(int)list.TypeOfPieceInList] * list.Count) *
                              (list.IsWhitePieceList ? 1 : -1);
        }
        
        materialWeight += PopCount(board, PieceType.Knight, true, whiteKnightTable);
        materialWeight -= PopCount(board, PieceType.Knight, false, blackKnightTable);
        materialWeight += PopCount(board, PieceType.Bishop, true, whiteBishopTable);
        materialWeight -= PopCount(board, PieceType.Bishop, false, blackBishopTable);
        materialWeight += PopCount(board, PieceType.Rook, true, whiteRookTable);
        materialWeight -= PopCount(board, PieceType.Rook, false, blackRookTable);
        materialWeight += PopCount(board, PieceType.Queen, true, whiteQueenTable);
        materialWeight -= PopCount(board, PieceType.Queen, false, blackQueenTable);
        
        if (GameState == 2) {
            materialWeight += PopCount(board, PieceType.Pawn, true, whitePawnEndGameTable);
            materialWeight -= PopCount(board, PieceType.Pawn, false, blackPawnEndGameTable);
            materialWeight += PopCount(board, PieceType.King, true, whiteKingEndGameTable);
            materialWeight -= PopCount(board, PieceType.King, false, blackKingEndGameTable);
        }
        
        else if (GameState == 1)
        {
            materialWeight += PopCount(board, PieceType.Pawn, true, whitePawnMiddleTable);
            materialWeight -= PopCount(board, PieceType.Pawn, false, blackPawnMiddleTable);
            materialWeight += PopCount(board, PieceType.King, true, whiteKingTable);
            materialWeight -= PopCount(board, PieceType.King, false, blackKingTable);
        }
    
        else
        {
            materialWeight += PopCount(board, PieceType.Pawn, true, whitePawnOpeningTable);
            materialWeight -= PopCount(board, PieceType.Pawn, false, blackPawnOpeningTable);
        }
        
        return materialWeight * weightInverter;
    }
    
    public double Quiesce(Board board, double alpha, double beta, int limit) {
        int stand_pat = EvaluateSimple(board);
        if (limit == 0) return stand_pat;
        if( stand_pat >= beta ) return beta;
        if( alpha < stand_pat ) alpha = stand_pat;

        Move[] captures = board.GetLegalMoves(true);

        foreach (Move capture in captures)
        {
            board.MakeMove(capture);
            double score = -Quiesce(board, -beta, -alpha, limit - 1);
            board.UndoMove(capture);

            if( score >= beta ) return beta;
            if( score > alpha ) alpha = score;
        }
        
        return alpha;
    }

    public int PopCount(Board board, PieceType type, bool isWhite, ulong pieceTable)
    {
        return (int)System.Runtime.Intrinsics.X86.Popcnt.X64.PopCount(board.GetPieceBitboard(type, isWhite) & pieceTable);

    }
    }
}