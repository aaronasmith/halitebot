using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

public class MyBot
{
    public const string MyBotName = "A-ronbot";


    private static Map map;
    private static Random random = new Random();
    private static ushort myID;
    

    public static void Main(string[] args) {
        Console.SetIn(Console.In);
        Console.SetOut(Console.Out);

        //File.Delete("Aronbot.log");
        //Log.Setup("Aronbot.log");

        map = Networking.getInit(out myID);

        /* ------
            Do more prep work, see rules for time limit
        ------ */
        
        Networking.SendInit(MyBotName); // Acknoweldge the init and begin the game

        while (true)
        {
            Networking.getFrame(ref map); // Update the map to reflect the moves before this turn
            var moves = new List<Move>();
            map.ResetCache();

            //Log.Information($"Move: {move++}");
            for (ushort x = 0; x < map.Width; x++) {
                for (ushort y = 0; y < map.Height; y++) {
                    if (map[x, y].Owner == myID) {
                        moves.Add(Move(new Location {X = x, Y = y}));
                    }
                }
            }

            Networking.SendMoves(moves); // Send moves
        }
    }

    public static Move Move(Location location) {
        var site = map[location];

        if (site.Strength < site.Production * random.Next(3, 6) && site.Strength != 255)
            return new Move(location, Direction.Still);

        var siteDictionary = map.GetSurroundingSites(location);

        var direction = map.FindClosestEdge(location, myID);
        if(direction != Direction.Still && siteDictionary[direction].Owner != myID && siteDictionary[direction].Strength >= site.Strength)
            return new Move(location, Direction.Still);

        return new Move(location, direction);
    }
}
