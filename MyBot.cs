using System;
using System.Collections.Generic;
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

        File.Delete("Aronbot.log");
        Log.Setup("Aronbot.log");

        map = Networking.getInit(out myID);

        /* ------
            Do more prep work, see rules for time limit
        ------ */

        Networking.SendInit(MyBotName); // Acknoweldge the init and begin the game

        int move = 1;
        while (true)
        {
            Networking.getFrame(ref map); // Update the map to reflect the moves before this turn
            var moves = new List<Move>();

            Log.Information($"Move: {move++}");
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
                Log.Error(ex);
            }

            Networking.SendMoves(moves); // Send moves
        }
    }

    public static Move Move(Location location) {
        var site = map[location];

        if(site.Strength < site.Production * 5)
            return  new Move(location, Direction.Still);

        var siteDictionary = GetSurroundingSites(location);

        // Find any borders we can take over.
        var nonOwned = siteDictionary.Where(s => s.Value.Owner != myID).OrderByDescending(s => s.Value.Production).ThenBy(s => s.Value.Strength).ToList();

        //foreach (var available in nonOwned) {
        //    if (available.Value.Strength < site.Strength) {
        //        return new Move(location, available.Key);
        //    }
        //}

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

        var bestDirection = Direction.Still;
        if (site.Strength == 255) {
            if (siteDictionary.Any(s => s.Value.Strength > 0 && s.Value.Strength < 255)) {
                bestDirection = siteDictionary.First(s => s.Value.Strength > 0 && s.Value.Strength < 255).Key;
            }
        } else {
            var bestStrength = 0;
            foreach (var s in siteDictionary) {
                if (s.Value.Strength + site.Strength > bestStrength) {
                    bestStrength = (ushort) (s.Value.Strength + site.Strength);
                    bestDirection = s.Key;
                }
            }
        }

        return new Move(location, bestDirection);
    }

    private static Direction GetOppositDirection(Direction direction) {
        switch (direction) {
            case Direction.North:
                return Direction.South;
            case Direction.East:
                return Direction.West;
            case Direction.South:
                return Direction.North;
            case Direction.West:
                return Direction.East;
            default:
                throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
        }
    }

    private static Dictionary<Direction, Site> GetSurroundingSites(Location location) {
        var siteDictionary = new Dictionary<Direction, Site> {
            {Direction.North, GetSiteInDirection(location, Direction.North)},
            {Direction.East, GetSiteInDirection(location, Direction.East)},
            {Direction.South, GetSiteInDirection(location, Direction.South)},
            {Direction.West, GetSiteInDirection(location, Direction.West)}
        };
        return siteDictionary;
    }

    private static Site GetSiteInDirection(Location location, Direction direction) {
        return map[GetLocationInDirection(location, direction)];
    }

    private static Location GetLocationInDirection(Location location, Direction direction)
    {
        switch (direction)
        {
            case Direction.North:
                return new Location(location.X, location.Y - 1 < 0 ? (ushort)(map.Height - 1) : (ushort)(location.Y - 1));
            case Direction.East:
                return new Location(location.X + 1 > (ushort)(map.Width - 1) ? (ushort)0 : (ushort)(location.X + 1), location.Y);
            case Direction.South:
                return new Location(location.X, location.Y + 1 > (ushort)(map.Height - 1) ? (ushort)0 : (ushort)(location.Y + 1));
            case Direction.West:
                return new Location(location.X - 1 < 0 ? (ushort)(map.Width - 1) : (ushort)(location.X - 1), location.Y);
            default:
                throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
        }

    }
}
