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
        Log.Setup("WTF.log");

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

        var siteDictionary = GetSurroundingSites(location);

        // Find any borders we can take over.
        var nonOwned = siteDictionary.Where(s => s.Value.Owner != myID);

        var availableNonOwned = nonOwned;

        //if (nonOwned.Count() == 3) {
        //    var direction = siteDictionary.Single(s => s.Value.Owner == myID).Key;
        //    var otherSiteFriendlySideDirections = GetSurroundingSites(GetLocationInDirection(location, direction)).Where(s => s.Value.Owner == myID && s.Key != GetOppositDirection(direction)).Select(s => s.Key);
        //    availableNonOwned = availableNonOwned.Where(s => !otherSiteFriendlySideDirections.Contains(s.Key));
        //}

        foreach (var available in availableNonOwned.OrderByDescending(s => s.Value.Production).ThenBy(s => s.Value.Strength)) {
            if (available.Value.Strength < site.Strength) {
                return new Move(location, available.Key);
            }
        }

        // We're on the edge or alone, stay here.
        if (nonOwned.Any() || nonOwned.Count() == 4) {
            return new Move(location, Direction.Still);
        }

        if (siteDictionary.Any(s => s.Value.Strength == 255)) {
            return new Move(location, GetOppositDirection(siteDictionary.First(s => s.Value.Strength == 255).Key));
        }
        var availableDirections = siteDictionary.Where(o => site.Strength == 255 || o.Value.Strength + site.Strength < 255).ToList();
        if(!availableDirections.Any())
            return new Move(location, Direction.Still);
        // Move northwest toward the edge.
        return new Move(location, availableDirections.OrderBy(d => d.Key).Skip(random.Next(Math.Min(availableDirections.Count, 2))).First().Key);
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
