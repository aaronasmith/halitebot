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

    private static Dictionary<int, Dictionary<Direction, Location>> rowEdges;
    private static Dictionary<int, Dictionary<Direction, Location>> columnEdges;


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
            rowEdges = new Dictionary<int, Dictionary<Direction, Location>>();
            columnEdges = new Dictionary<int, Dictionary<Direction, Location>>();

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

        //foreach (var available in nonOwned)
        //{
        //    if (available.Value.Strength < site.Strength)
        //    {
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
        var direction = FindClosestEdge(location);
        return new Move(location, direction);
    }

    private static Direction FindClosestEdge(Location location) {
        var east = FindEdgeInDirection(location, Direction.East);
        //Log.Information($"({location.X},{location.Y}):East:({east.X},{east.Y})");
        var west = FindEdgeInDirection(location, Direction.West);
        //Log.Information($"({location.X},{location.Y}):West:({west.X},{west.Y})");
        var north = FindEdgeInDirection(location, Direction.North);
        //Log.Information($"({location.X},{location.Y}):North:({north.X},{north.Y})");
        var south = FindEdgeInDirection(location, Direction.South);
        //Log.Information($"({location.X},{location.Y}):South:({south.X},{south.Y})");
        var shortestDistance = ushort.MaxValue;
        var direction = Direction.Still;
        if (!east.Equals(location)) {
            var distance = Math.Abs(east.X - location.X);
            if (distance < shortestDistance) {
                direction = Direction.East;
                shortestDistance = (ushort)distance;
            }
            if (distance == shortestDistance)
                direction = random.Next(1) == 0 ? direction : Direction.East;
        }
        if (!west.Equals(location)) {
            var distance = Math.Abs(location.X - west.X);
            if (distance < shortestDistance) {
                direction = Direction.West;
                shortestDistance = (ushort)distance;
            }
            if (distance == shortestDistance)
                direction = random.Next(1) == 0 ? direction : Direction.West;
        }
        if (!south.Equals(location))
        {
            var distance = Math.Abs(south.Y - location.Y);
            if (distance < shortestDistance) {
                direction = Direction.South;
                shortestDistance = (ushort)distance;
            }
            if (distance == shortestDistance)
                direction = random.Next(1) == 0 ? direction : Direction.South;
        }
        if (!north.Equals(location))
        {
            var distance = Math.Abs(location.Y - north.Y);
            if (distance < shortestDistance) {
                direction = Direction.North;
                shortestDistance = (ushort)distance;
            }
            if (distance == shortestDistance)
                direction = random.Next(1) == 0 ? direction : Direction.North;
        }
        
        return direction;
    }

    private static Location FindEdgeInDirection(Location location, Direction direction) {
        Dictionary<int, Dictionary<Direction, Location>> rowColumnGroup;
        Dictionary<Direction, Location> rowColumnEdges = null;

        int rowOrColumnNumber;

        switch (direction) {
            case Direction.North:
            case Direction.South:
                rowColumnGroup = columnEdges;
                rowOrColumnNumber = location.X;
                if (columnEdges.ContainsKey(location.X)) {
                    rowColumnEdges = columnEdges[location.X];
                    if (rowColumnEdges.ContainsKey(direction)) {
                        return rowColumnEdges[direction];
                    }
                }
                break;
            case Direction.West:
            case Direction.East:
                rowColumnGroup = rowEdges;
                rowOrColumnNumber = location.Y;
                if (rowEdges.ContainsKey(location.Y)) {
                    rowColumnEdges = rowEdges[location.Y];
                    if (rowColumnEdges.ContainsKey(direction)) {
                        return rowColumnEdges[direction];
                    }
                }
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
        }

        Location check = location;
        do {
            check = GetLocationInDirection(check, direction);
        } while (!location.Equals(check) && map[check].Owner == myID);

        if (rowColumnEdges == null) {
            var edge = new Dictionary<Direction, Location> {{direction, check}};
            rowColumnGroup.Add(rowOrColumnNumber, edge);
        }
        
        return check;
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
