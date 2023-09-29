namespace TestMyBot;
using ChessChallenge.API;
using ChessChallenge.Example;
using System.IO;

[TestClass]
public class TestMyBot
{
    MyBot bot = new MyBot();
    Board init_pos = Board.CreateBoardFromFEN("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");
    Board endgame_pos = Board.CreateBoardFromFEN("1k6/8/8/8/8/7K/8/7R w - - 0 50");
    Board midgame_pos = Board.CreateBoardFromFEN("r1bq1rk1/pp3ppp/2n1p3/3n4/1b1P4/2N2N2/PP2BPPP/R1BQ1RK1 w - - 0 10");
    Board start_game_pos = Board.CreateBoardFromFEN("rn2kbnr/ppp1pppp/8/3q4/6Q1/8/PPPP1PPP/RNB1KBNR b KQkq - 1 4");

    private TestContext testContextInstance;

    /// <summary>
    /// Gets or sets the test context which provides
    /// information about and functionality for the current test run.
    /// </summary>
    public TestContext TestContext
    {
        get { return testContextInstance; }
        set { testContextInstance = value; }
    }

    [TestMethod]
    public void TestGetDistEvalBonus()
    {
        PieceList wrooks = endgame_pos.GetPieceList(PieceType.Rook, true);
        int dist = MyBot.DistanceFromKing(endgame_pos, wrooks.GetPiece(0), false);
        Assert.AreEqual(dist, 13, 0.01, "Distance not measured correctly");
        
        PieceList bKnights = init_pos.GetPieceList(PieceType.Knight, false);
        int bonus = MyBot.GetDistEvalBonus(init_pos, bKnights);
        Assert.AreEqual(bonus, -38, 0.01);
    }

    [TestMethod]
    public void TestMoveScore()
    {
        bot = new MyBot();
        Move[] allMoves = midgame_pos.GetLegalMoves(true);
        //sort start
        int[] moveOrderKeys = new int[allMoves.Length];
        for (int i = 0; i < allMoves.Length; i++)
            moveOrderKeys[i] = bot.GetMoveScore(midgame_pos, allMoves[i]);
        Array.Sort(moveOrderKeys, allMoves);
        //sort end
        Assert.AreEqual(allMoves[0], new Move("c3d5", midgame_pos));
        Assert.AreEqual(moveOrderKeys[0], -304);
    }

    [TestMethod]
    public void TestEval()
    {
        bot = new MyBot();
        Assert.AreEqual(bot.Eval(init_pos), 0);
        Assert.AreEqual(bot.Eval(midgame_pos), 12);
        Assert.AreEqual(bot.Eval(endgame_pos), 472);

        Assert.AreEqual(bot.Eval(start_game_pos), -280);
        Assert.AreEqual(start_game_pos.IsWhiteToMove, false);
        ulong key1 = start_game_pos.ZobristKey ^ ((ulong)start_game_pos.PlyCount << 1) ^ (ulong)(start_game_pos.IsWhiteToMove ? 1 : 0);

        Assert.AreEqual(start_game_pos.TrySkipTurn(), true);
        Assert.AreEqual(bot.Eval(start_game_pos), 280);
        Assert.AreEqual(start_game_pos.IsWhiteToMove, true);
        ulong key2 = start_game_pos.ZobristKey ^ ((ulong)start_game_pos.PlyCount << 1) ^ (ulong)(start_game_pos.IsWhiteToMove ? 1 : 0);
        Assert.AreNotEqual(key1, key2);
    }

    [TestMethod]
    public void TestMoveAlloc()
    {
        System.Span<Move> mySpan = stackalloc Move[128];
        System.Span<Move> slicedSpan = mySpan.Slice(0, 32);
        Assert.AreEqual(mySpan.Length, 128);
        Assert.AreEqual(slicedSpan.Length, 32);        
    }
    
    [TestMethod]
    public void TestMiniMax()
    {
        string[] botMatchStartFens = ChessChallenge.Application.FileHelper.ReadResourceFile("Fens.txt").Split('\n').Where(fen => fen.Length > 0).ToArray();
        string[] referenceOutput = File.ReadAllText("testminimax2.txt").Split("\n");
        string output = "";
        int maxDepth = 6;
        for (int i = 0; i < 100; i++)
        {
            string outputLine = String.Format("{0} ", i) + botMatchStartFens[i];
            bot = new MyBot();
            Board pos = Board.CreateBoardFromFEN(botMatchStartFens[i]);
            int depth = 0;
            int inf = 30000;
            int bestEval;
            do
            {
                depth++;
                bestEval = bot.MiniMax(pos, depth, -inf, inf, false);
                outputLine += String.Format("Eval {0} depth {1} ", bestEval * (init_pos.IsWhiteToMove ? 1 : -1), depth);
            }
            while(depth < maxDepth); //DEBUG
            Assert.AreEqual(referenceOutput[i], outputLine);
            output += "\n" + outputLine;
        }
        File.WriteAllText("testOutput.txt", output);
    }

    [TestMethod]
    public void TestPuzzle()
    {
        string[] puzzleTest = File.ReadAllText("puzzleTest.txt").Split("\n").Where(fen => fen.Length > 0).ToArray();
        for (int i = 0; i < puzzleTest.Length; i++)
        {
            string[] puzzleComponents = puzzleTest[i].Split("@");
            Board board = Board.CreateBoardFromFEN(puzzleComponents[1]);
            Move expectedMove = new Move(puzzleComponents[0], board);

            Timer timer = new Timer(200 *1000);
            bot = new MyBot();
            Assert.AreEqual(expectedMove, bot.Think(board, timer));
        }
    }
}