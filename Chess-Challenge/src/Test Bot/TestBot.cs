using ChessChallenge.API;

public class TestBot : IChessBot
{
    public Move Think(Board board, Timer timer)
    {
        return Move.NullMove;
    }
}   
