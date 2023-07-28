using ChessChallenge.API;
using System;
using System.Linq;

public class MyBot : IChessBot
{
    private bool _abortSearch;
    private int[] _pieceWeights = {0, 100, 310, 330, 500, 1000, 10000 };
    private int[] _gamePhase = {0, 0, 1, 1, 2, 4, 0};
    private ulong[] _psts = {657614902731556116, 420894446315227099, 384592972471695068, 312245244820264086, 364876803783607569, 366006824779723922, 366006826859316500, 786039115310605588, 421220596516513823, 366011295806342421, 366006826859316436, 366006896669578452, 162218943720801556, 440575073001255824, 657087419459913430, 402634039558223453, 347425219986941203, 365698755348489557, 311382605788951956, 147850316371514514, 329107007234708689, 402598430990222677, 402611905376114006, 329415149680141460, 257053881053295759, 291134268204721362, 492947507967247313, 367159395376767958, 384021229732455700, 384307098409076181, 402035762391246293, 328847661003244824, 365712019230110867, 366002427738801364, 384307168185238804, 347996828560606484, 329692156834174227, 365439338182165780, 386018218798040211, 456959123538409047, 347157285952386452, 365711880701965780, 365997890021704981, 221896035722130452, 384289231362147538, 384307167128540502, 366006826859320596, 366006826876093716, 366002360093332756, 366006824694793492, 347992428333053139, 457508666683233428, 329723156783776785, 329401687190893908, 366002356855326100, 366288301819245844, 329978030930875600, 420621693221156179, 422042614449657239, 384602117564867863, 419505151144195476, 366274972473194070, 329406075454444949, 275354286769374224, 366855645423297932, 329991151972070674, 311105941360174354, 256772197720318995, 365993560693875923, 258219435335676691, 383730812414424149, 384601907111998612, 401758895947998613, 420612834953622999, 402607438610388375, 329978099633296596, 67159620133902};
    private double _negativeInfinity = Double.NegativeInfinity;
    private double _positiveInfinity = Double.PositiveInfinity;
    private int _currentPly;

    public Move Think(Board board, Timer timer)
    {
        _currentPly = board.PlyCount;
        _abortSearch = false;
        Move bestMove = Move.NullMove;
        
        for (int currentDepth = 1; currentDepth <= 100; currentDepth++)
        {
            Move iterationBestMove = RootNegamax(board, timer, currentDepth);

            if (_abortSearch) break;
            
            bestMove = iterationBestMove;
        }

        return bestMove;
    }

    private Move RootNegamax(Board board, Timer timer, int depth)
    {
        Move[] moves = board.GetLegalMoves();
        Move[] orderedMoves = moves.OrderBy(x => x.IsCapture).ToArray();
        
        Move bestMove = orderedMoves[0];
        double bestScore = _negativeInfinity;

        foreach (Move move in orderedMoves)
        {
            if (timer.MillisecondsElapsedThisTurn >= timer.MillisecondsRemaining / 30) _abortSearch = true;
            
            board.MakeMove(move);
            double score = -Negamax(_negativeInfinity, _positiveInfinity, board, depth);
            board.UndoMove(move);

            if (score > bestScore)
            {
                bestScore = score;
                bestMove = move;
            }
        }
        
        return bestMove;
    }

    // https://www.chessprogramming.org/Negamax
    private double Negamax(double alpha, double beta, Board board, int depth)
    {
        if (_abortSearch) return 0;
        if (depth == 0) return Quiesce(board, alpha, beta, 2);

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
    
    private int GetPstValue(int psq) {
        return (int)(((_psts[psq / 10] >> (6 * (psq % 10))) & 63) - 20) * 8;
    }

    private int EvaluatePeStO(Board board)
    {
        if (board.IsInCheckmate())
        {
            return board.IsWhiteToMove ? 10000 - (board.PlyCount - _currentPly) : -10000 + (board.PlyCount - _currentPly);
        }
        
        int phase = 0, mg = 0, eg = 0;
        
        foreach (bool isWhite in new[] {true, false}) {
            for (var pieceType = PieceType.Pawn; pieceType <= PieceType.King; pieceType++)
            {
                int piece = (int)pieceType;
                ulong pieceBitBoard = board.GetPieceBitboard(pieceType, isWhite);

                while (pieceBitBoard != 0)
                {
                    phase += _gamePhase[piece];
                    int index = 128 * (piece - 1) + BitboardHelper.ClearAndGetIndexOfLSB(ref pieceBitBoard) ^ (isWhite ? 56 : 0);
                    mg += GetPstValue(index) + _pieceWeights[piece];
                    eg += GetPstValue(index + 64) + _pieceWeights[piece];
                }
            }

            mg = -mg;
            eg = -eg;
        }

        return (mg * phase + eg * (24 - phase)) / 24 * (board.IsWhiteToMove ? 1 : -1);
    }
    
    private double Quiesce(Board board, double alpha, double beta, int limit) {
        int standPat = EvaluatePeStO(board);
        if (limit == 0) return standPat;
        if( standPat >= beta ) return beta;
        if( alpha < standPat ) alpha = standPat;

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
}