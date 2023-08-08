namespace TestMyBot;
using ChessChallenge.API;


[TestClass]
public class TestMyBot
{
    MyBot bot = new MyBot();
    Board init_pos = Board.CreateBoardFromFEN("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");
    Board endgame_pos = Board.CreateBoardFromFEN("1k6/8/8/8/8/7K/8/7R w - - 0 50");
    Board midgame_pos = Board.CreateBoardFromFEN("r1bq1rk1/pp3ppp/2n1p3/3n4/1b1P4/2N2N2/PP2BPPP/R1BQ1RK1 w - - 0 10");


    [TestMethod]
    public void TestGetDistEvalBonus()
    {
        PieceList wrooks = endgame_pos.GetPieceList(PieceType.Rook, true);
        int dist = bot.DistanceFromEnemyKing(endgame_pos, wrooks.GetPiece(0), false);
        Assert.AreEqual(dist, 13, 0.01, "Distance not measured correctly");
        
        PieceList bKnights = init_pos.GetPieceList(PieceType.Knight, false);
        int bonus = bot.GetDistEvalBonus(init_pos, bKnights);
        Assert.AreEqual(bonus, -38, 0.01);
    }

    [TestMethod]
    public void TestMoveScore()
    {
        midgame_pos = Board.CreateBoardFromFEN("r1bq1rk1/pp3ppp/2n1p3/3n4/1b1P4/2N2N2/PP2BPPP/R1BQ1RK1 w - - 0 10");
        Move[] allMoves = midgame_pos.GetLegalMoves();
        //sort start
        int[] moveOrderKeys = new int[allMoves.Length];
        for (int i = 0; i < allMoves.Length; i++)
            moveOrderKeys[i] = bot.GetMoveScore(midgame_pos, allMoves[i]);
        Array.Sort(moveOrderKeys, allMoves);
        //sort end
        Assert.AreEqual(allMoves[0], new Move("c3d5", midgame_pos));
        Assert.AreEqual(moveOrderKeys[0], -260);
    }
}