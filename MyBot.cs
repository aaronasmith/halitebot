using System;
using System.Collections.Generic;
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
        
        map = Networking.getInit(out myID);

        /* ------
            Do more prep work, see rules for time limit
        ------ */

        Networking.SendInit(MyBotName); // Acknoweldge the init and begin the game

        while (true) {
            Networking.getFrame(ref map); // Update the map to reflect the moves before this turn

            var moves = new List<Move>();
            for (ushort x = 0; x < map.Width; x++) {
                for (ushort y = 0; y < map.Height; y++) {
                    if (map[x, y].Owner == myID) {
                        moves.Add(Move(new Location { X = x, Y = y }));
                    }
                }
            }

            Networking.SendMoves(moves); // Send moves
        }
    }

    public static Move Move(Location location) {
        var site = map[location];
        //if(site.Strength < site.Production * 5)
        //    return new Move(location, Direction.Still);
        //return new Move(location, random.Next(0, 1) == 0 ? Direction.North : Direction.West);

        if(site.Strength < site.Production * 5)
            return  new Move(location, Direction.Still);

        var siteDictionary = new Dictionary<Direction, Site> {
            { Direction.North, GetSiteInDirection(location, Direction.North) },
            { Direction.East, GetSiteInDirection(location, Direction.East) },
            { Direction.South, GetSiteInDirection(location, Direction.South) },
            { Direction.West, GetSiteInDirection(location, Direction.West) }
        };

        // Find any borders we can take over.
        var nonOwned = siteDictionary.Where(s => s.Value.Owner != myID).OrderBy(s => s.Value.Strength);
        foreach (var available in nonOwned) {
            if (available.Value.Strength < site.Strength) {
                return new Move(location, available.Key);
            }
        }

        // We're on the edge, stay here.
        if (nonOwned.Any()) {
            return new Move(location, Direction.Still);
        }

        // Move northwest toward the edge.
        return new Move(location, random.Next(0, 2) == 1 ? Direction.North : Direction.West);
    }

    public static Site GetSiteInDirection(Location location, Direction direction) {
        switch (direction) {
            case Direction.North:
                return map[location.X, location.Y - 1 < 0 ? (ushort)(map.Height - 1) : (ushort)(location.Y - 1)];
            case Direction.East:
                return map[location.X + 1 > (ushort)(map.Width - 1) ? (ushort)0 : (ushort)(location.X + 1), location.Y];
            case Direction.South:
                return map[location.X, location.Y + 1 > (ushort)(map.Height - 1) ? (ushort)0 : (ushort)(location.Y + 1)];
            case Direction.West:
                return map[location.X - 1 < 0 ? (ushort)(map.Width - 1) : (ushort)(location.X - 1), location.Y];
            default:
                throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
        }
    }
}
