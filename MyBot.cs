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

    private static bool debug = true;


    public static void Main(string[] args) {
        Console.SetIn(Console.In);
        Console.SetOut(Console.Out);

        //File.Delete("Aronbot.log");
        //Log.Setup("Aronbot.log");

        map = Networking.getInit(out myID);

        /* ------
            Do more prep work, see rules for time limit
        ------ */

        FindHighProductionAreas();

        Networking.SendInit(MyBotName); // Acknoweldge the init and begin the game

        int move = 1;
        while (true)
        {
            Networking.getFrame(ref map); // Update the map to reflect the moves before this turn
            var moves = new List<Move>();
            map.ResetCache();

            //Log.Information($"Move: {move++}");
            try
            {
                for (ushort x = 0; x < map.Width; x++)
                {
                    for (ushort y = 0; y < map.Height; y++)
                    {
                        if (map[x, y].Owner == myID)
                        {
                            moves.Add(Move(new Location { X = x, Y = y }));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //Log.Error(ex);
            }

            Networking.SendMoves(moves); // Send moves
        }
    }

    private static void FindHighProductionAreas()
    {
        for (ushort x = 0; x < map.Width; x++)
        {
            for (ushort y = 0; y < map.Height; y++)
            {
                
            }
        }
    }

    public static Move Move(Location location) {
        var site = map[location];

        var siteDictionary = map.GetSurroundingSites(location);

        // Find any borders we can take over.
        var nonOwned = siteDictionary.Where(s => s.Value.Owner != myID).OrderByDescending(s => s.Value.Production).ThenBy(s => s.Value.Strength).ToList();

        // We're on the edge or alone, stay here.
        if (nonOwned.Any()) {
            var preferredProduction = nonOwned.GroupBy(g => g.Value.Production);
            foreach (var preferredSquare in preferredProduction.First()) {
                if (preferredSquare.Value.Strength < site.Strength)
                {
                    return new Move(location, preferredSquare.Key);
                }
            }
            return new Move(location, Direction.Still);
        }

        if (site.Strength < site.Production * random.Next(3, 6) && site.Strength != 255)
            return new Move(location, Direction.Still);

        var direction = map.FindClosestEdge(location, myID);
        return new Move(location, direction);
    }
}
