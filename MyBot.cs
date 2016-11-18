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
    private static ushort startX;
    private static ushort averageX;
    private static ushort endX;
    private static bool wrapX;
    private static ushort startY;
    private static ushort averageY;
    private static ushort endY;
    private static bool wrapY;

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
        while (true) {
            Networking.getFrame(ref map); // Update the map to reflect the moves before this turn
            var moves = new List<Move>();
            Log.Information($"Move: {move++}");
            try {
                CalculateLocation();
                for (ushort x = 0; x < map.Width; x++) {
                    for (ushort y = 0; y < map.Height; y++) {
                        if (map[x, y].Owner == myID) {
                            moves.Add(Move(new Location {X = x, Y = y}));
                        }
                    }
                }
            }
            catch (Exception ex) {
                Log.Error(ex);
            }

            Networking.SendMoves(moves); // Send moves
        }
    }

    private static void CalculateLocation() {
        wrapX = false;
        wrapY = false;
        startX = 0;
        if (HasOwnershipVertically(startX)) {
            while (startX < map.Width && HasOwnershipVertically((ushort) (map.Width - ++startX))) {}
        }
        // Entire map
        if (startX == map.Width)
        {
            if (debug)
                Log.Information("X: Entire map.");
            averageX = (ushort) (map.Width / 2);
            startX = 0;
            endX = (ushort) (map.Width - 1);
        }
        else if (startX > 0)
        {
            if (debug)
                Log.Information("X: Wrap.");
            int localEndX = startX;
            while (++startX < map.Width && !HasOwnershipVertically(startX)) { }

            if (debug)
                Log.Information($"X: {startX} - {localEndX}");

            var averageLeftSide = localEndX + (startX - map.Width);
            if (averageLeftSide < 0) {
                averageX = (ushort) ((map.Height + localEndX + startX) / 2);
            } else {
                averageX = (ushort) (averageLeftSide / 2);
            }
            endX = (ushort) localEndX;
            wrapX = true;
        }
        else {
            while (startX < map.Width && !HasOwnershipVertically(startX++)) {}
            endX = startX;
            while (++endX < map.Width && HasOwnershipVertically(endX)) {}
            endX--;

            if (debug)
                Log.Information($"X: {startX} - {endX}");
            averageX = (ushort) ((startX + endX - 1) / 2);
        }

        if (debug)
            Log.Information($"AverageX: {averageX}");

        startY = 0;
        if (HasOwnershipHorizontally(startY))
        {
            while (startY < map.Height && HasOwnershipHorizontally((ushort)(map.Height - ++startY))) { }
        }
        // Entire map
        if (startY == map.Height)
        {
            if (debug)
                Log.Information("Y: Entire map.");
            averageY = (ushort)(map.Height / 2);
            startY = 0;
            endY = (ushort)(map.Height - 1);
        }
        else if (startY > 0)
        {
            if (debug)
                Log.Information("Y: Wrap.");
            int localEndY = startY;
            while (startY < map.Height && !HasOwnershipHorizontally(startY++)) { }

            if (debug)
                Log.Information($"Y: {startY} - {localEndY}");
            var averageLeftSide = localEndY + (startY - map.Height);
            if (averageLeftSide < 0) {
                averageY = (ushort) ((map.Height + localEndY + startY) / 2);
            } else {
                averageY = (ushort) (averageLeftSide / 2);
            }
            endY = (ushort) localEndY;
            wrapY = true;
        }
        else
        {
            while (startY < map.Height && !HasOwnershipHorizontally(startY++)) { }
            endY = startY;
            while (endY < map.Height && HasOwnershipHorizontally(endY++)) { }
            endY--;

            if (debug)
                    Log.Information($"Y: {startY} - {endY}");
            averageY = (ushort)((startY + endY - 1) / 2);
        }
        if (debug)
            Log.Information($"AverageY: {averageY}");
    }

    private static bool HasOwnershipHorizontally(ushort yPosition)
    {
        for (ushort x = 0; x < map.Width; x++)
        {
            if (map[x, yPosition].Owner == myID)
            {
                return true;
            }
        }
        return false;
    }

    private static bool HasOwnershipVertically(ushort xPosition) {
        for (ushort y = 0; y < map.Height; y++)
        {
            if (map[xPosition, y].Owner == myID)
            {
                return true;
            }
        }
        return false;
    }

    public static Move Move(Location location) {
        var site = map[location];

        if(site.Strength < site.Production * 5)
            return  new Move(location, Direction.Still);

        var siteDictionary = GetSurroundingSites(location);

        // Find any borders we can take over.
        var nonOwned = siteDictionary.Where(s => s.Value.Owner != myID).ToList();

        foreach (var available in nonOwned.OrderByDescending(s => s.Value.Production).ThenBy(s => s.Value.Strength)) {
            if (available.Value.Strength < site.Strength) {
                return new Move(location, available.Key);
            }
        }

        // We're on the edge or alone, stay here.
        if (nonOwned.Any() || nonOwned.Count() == 4) {
            return new Move(location, Direction.Still);
        }

        if (siteDictionary.Any(s => s.Value.Strength == 255))
        {
            return new Move(location, GetOppositDirection(siteDictionary.First(s => s.Value.Strength == 255).Key));
        }

        var availableDirections = new List<Direction>();


        // Move outward from our average position.
        if (wrapX) {
            if (averageX > startX) {
                if (location.X < startX)
                    availableDirections.Add(Direction.East);
                else {
                    availableDirections.Add(location.X > averageX ? Direction.East : Direction.West);
                }
            } else {
                if (location.X > startX)
                    availableDirections.Add(Direction.West);
                else {
                    availableDirections.Add(location.X > averageX ? Direction.East : Direction.West);
                }
            }
        } else {
            availableDirections.Add(location.X > averageX ? Direction.East : Direction.West);
        }
        if (wrapY) {
            if (averageY > startY) {
                if (location.Y < startY)
                    availableDirections.Add(Direction.South);
                else {
                    availableDirections.Add(location.Y > averageY ? Direction.South : Direction.North);
                }
            } else {
                if (location.Y > startY)
                    availableDirections.Add(Direction.North);
                else {
                    availableDirections.Add(location.Y > averageY ? Direction.North : Direction.South);
                }
            }
        } else {
            availableDirections.Add(location.Y > averageY ? Direction.South : Direction.North);
        }

        var direction = Direction.Still;
        var lowestStrength = ushort.MaxValue;
        foreach (var availableDirection in availableDirections) {
            if (siteDictionary[availableDirection].Strength < lowestStrength) {
                direction = availableDirection;
                lowestStrength = siteDictionary[availableDirection].Strength;
            }
        }

        return new Move(location, direction);
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
