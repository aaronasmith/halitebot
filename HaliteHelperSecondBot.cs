using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;


namespace SecondBot
{
    /// <summary>
    /// Helpful for debugging.
    /// </summary>
    public static class Log
    {
        private static string _logPath;

        /// <summary>
        /// File must exist
        /// </summary>
        public static void Setup(string logPath) {
            _logPath = logPath;
        }

        public static void Information(string message) {
            if (!string.IsNullOrEmpty(_logPath))
                File.AppendAllLines(_logPath, new[] {string.Format("{0}: {1}", DateTime.Now.ToShortTimeString(), message)});
        }

        public static void Error(Exception exception) {
            Log.Information(string.Format("ERROR: {0} {1}", exception.Message, exception.StackTrace));
        }
    }

    public static class Networking
    {
        private static string ReadNextLine() {
            var str = Console.ReadLine();
            if (str == null) throw new ApplicationException("Could not read next line from stdin");
            return str;
        }

        private static void SendString(string str) {
            Console.WriteLine(str);
        }

        /// <summary>
        /// Call once at the start of a game to load the map and player tag from the first four stdin lines.
        /// </summary>
        public static Map getInit(out ushort playerTag) {

            // Line 1: Player tag
            if (!ushort.TryParse(ReadNextLine(), out playerTag))
                throw new ApplicationException("Could not get player tag from stdin during init");

            // Lines 2-4: Map
            var map = Map.ParseMap(ReadNextLine(), ReadNextLine(), ReadNextLine());
            return map;
        }

        /// <summary>
        /// Call every frame to update the map to the next one provided by the environment.
        /// </summary>
        public static void getFrame(ref Map map) {
            map.Update(ReadNextLine());
        }


        /// <summary>
        /// Call to acknowledge the initail game map and start the game.
        /// </summary>
        public static void SendInit(string botName) {
            SendString(botName);
        }

        /// <summary>
        /// Call to send your move orders and complete your turn.
        /// </summary>
        public static void SendMoves(IEnumerable<Move> moves) {
            SendString(Move.MovesToString(moves));
        }
    }

    public enum Direction
    {
        Still = 0,
        North = 1,
        East = 2,
        South = 3,
        West = 4
    }

    public struct Site
    {
        public ushort Owner { get; internal set; }
        public ushort Strength { get; internal set; }
        public ushort Production { get; internal set; }
    }

    public struct Location
    {
        public ushort X;
        public ushort Y;

        public Location(ushort x, ushort y) : this() {
            X = x;
            Y = y;
        }

        public bool Equals(Location other) {
            return X == other.X && Y == other.Y;
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) return false;
            return obj is Location && Equals((Location) obj);
        }

        public override int GetHashCode() {
            unchecked {
                return (X.GetHashCode() * 397) ^ Y.GetHashCode();
            }
        }
    }

    public struct Move
    {
        public Location Location;
        public Direction Direction;

        public Move(Location location, Direction direction) {
            Location = location;
            Direction = direction;
        }

        internal static string MovesToString(IEnumerable<Move> moves) {
            return string.Join(" ", moves.Select(m => string.Format("{0} {1} {2}", m.Location.X, m.Location.Y, (int) m.Direction)));
        }
    }

    /// <summary>
    /// State of the game at every turn. Use <see cref="GetInitialMap"/> to get the map for a new game from
    /// stdin, and use <see cref="NextTurn"/> to update the map after orders for a turn have been executed.
    /// </summary>
    public class Map
    {
        private Dictionary<int, Dictionary<Direction, Location>> rowEdges;
        private Dictionary<int, Dictionary<Direction, Location>> columnEdges;

        public void Update(string gameMapStr) {
            var gameMapValues = new Queue<string>(gameMapStr.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries));

            ushort x = 0, y = 0;
            while (y < Height) {
                ushort counter, owner;
                if (!ushort.TryParse(gameMapValues.Dequeue(), out counter))
                    throw new ApplicationException("Could not get some counter from stdin");
                if (!ushort.TryParse(gameMapValues.Dequeue(), out owner))
                    throw new ApplicationException("Could not get some owner from stdin");
                while (counter > 0) {
                    _sites[x, y].Owner = owner;
                    x++;
                    if (x == Width) {
                        x = 0;
                        y++;
                    }
                    counter--;
                }
            }

            var strengthValues = gameMapValues; // Referencing same queue, but using a name that is more clear
            for (y = 0; y < Height; y++) {
                for (x = 0; x < Width; x++) {
                    ushort strength;
                    if (!ushort.TryParse(strengthValues.Dequeue(), out strength))
                        throw new ApplicationException("Could not get some strength value from stdin");
                    _sites[x, y].Strength = strength;
                }
            }
        }

        /// <summary>
        /// Get a read-only structure representing the current state of the site at the supplied coordinates.
        /// </summary>
        public Site this[ushort x, ushort y] {
            get {
                if (x >= Width)
                    throw new IndexOutOfRangeException(string.Format("Cannot get site at ({0},{1}) beacuse width is only {2}", x, y, Width));
                if (y >= Height)
                    throw new IndexOutOfRangeException(string.Format("Cannot get site at ({0},{1}) beacuse height is only {2}", x, y, Height));
                return _sites[x, y];
            }
        }

        /// <summary>
        /// Get a read-only structure representing the current state of the site at the supplied location.
        /// </summary>
        public Site this[Location location] => this[location.X, location.Y];

        /// <summary>
        /// Returns the width of the map.
        /// </summary>
        public ushort Width => (ushort) _sites.GetLength(0);

        /// <summary>
        ///  Returns the height of the map.
        /// </summary>
        public ushort Height => (ushort) _sites.GetLength(1);

        #region Implementation

        private readonly Site[,] _sites;

        private Map(ushort width, ushort height) {
            _sites = new Site[width, height];
            for (ushort x = 0; x < width; x++) {
                for (ushort y = 0; y < height; y++) {
                    _sites[x, y] = new Site();
                }
            }
        }

        private static Tuple<ushort, ushort> ParseMapSize(string mapSizeStr) {
            ushort width, height;
            var parts = mapSizeStr.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2 || !ushort.TryParse(parts[0], out width) || !ushort.TryParse(parts[1], out height))
                throw new ApplicationException("Could not get map size from stdin during init");
            return Tuple.Create(width, height);
        }

        public static Map ParseMap(string mapSizeStr, string productionMapStr, string gameMapStr) {
            var mapSize = ParseMapSize(mapSizeStr);
            var map = new Map(mapSize.Item1, mapSize.Item2);

            var productionValues = new Queue<string>(productionMapStr.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries));

            ushort x, y;
            for (y = 0; y < map.Height; y++) {
                for (x = 0; x < map.Width; x++) {
                    ushort production;
                    if (!ushort.TryParse(productionValues.Dequeue(), out production))
                        throw new ApplicationException("Could not get some production value from stdin");
                    map._sites[x, y].Production = production;
                }
            }

            map.Update(gameMapStr);

            return map;
        }

        #endregion

        public void ResetCache() {
            rowEdges = new Dictionary<int, Dictionary<Direction, Location>>();
            columnEdges = new Dictionary<int, Dictionary<Direction, Location>>();
        }

        public Direction FindClosestEdge(Location location, ushort id) {
            var random = new Random();

            var east = FindEdgeInDirection(location, Direction.East, id);
            //Log.Information($"({location.X},{location.Y}):East:({east.X},{east.Y})");
            var west = FindEdgeInDirection(location, Direction.West, id);
            //Log.Information($"({location.X},{location.Y}):West:({west.X},{west.Y})");
            var north = FindEdgeInDirection(location, Direction.North, id);
            //Log.Information($"({location.X},{location.Y}):North:({north.X},{north.Y})");
            var south = FindEdgeInDirection(location, Direction.South, id);
            //Log.Information($"({location.X},{location.Y}):South:({south.X},{south.Y})");
            var shortestDistance = ushort.MaxValue;
            var direction = Direction.Still;
            if (!east.Equals(location)) {
                var distance = east.X < location.X ? east.X + (Width - location.X) : east.X - location.X;
                if (distance < shortestDistance) {
                    direction = Direction.East;
                    shortestDistance = (ushort) distance;
                } else if (distance == shortestDistance)
                    direction = random.Next(1) == 0 ? direction : Direction.East;
            }
            if (!west.Equals(location)) {
                var distance = west.X > location.X ? location.X + (Width - west.X) : location.X - west.X;
                if (distance < shortestDistance) {
                    direction = Direction.West;
                    shortestDistance = (ushort) distance;
                } else if (distance == shortestDistance)
                    direction = random.Next(1) == 0 ? direction : Direction.West;
            }
            if (!south.Equals(location)) {
                var distance = south.Y < location.Y ? south.Y + (Height - location.Y) : south.Y - location.Y;
                if (distance < shortestDistance) {
                    direction = Direction.South;
                    shortestDistance = (ushort) distance;
                } else if (distance == shortestDistance)
                    direction = random.Next(1) == 0 ? direction : Direction.South;
            }
            if (!north.Equals(location)) {
                var distance = north.Y > location.Y ? location.Y + (Width - north.Y) : location.Y - north.Y;
                if (distance < shortestDistance) {
                    direction = Direction.North;
                } else if (distance == shortestDistance)
                    direction = random.Next(1) == 0 ? direction : Direction.North;
            }

            return direction;
        }

        private Location FindEdgeInDirection(Location location, Direction direction, ushort id) {
            if (rowEdges == null || columnEdges == null) {
                ResetCache();
            }

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
            } while (!location.Equals(check) && this[check].Owner == id);

            if (rowColumnEdges == null) {
                var edge = new Dictionary<Direction, Location> {{direction, check}};
                rowColumnGroup.Add(rowOrColumnNumber, edge);
            }

            return check;
        }

        public Dictionary<Direction, Site> GetSurroundingSites(Location location) {
            var siteDictionary = new Dictionary<Direction, Site> {
                {Direction.North, GetSiteInDirection(location, Direction.North)},
                {Direction.East, GetSiteInDirection(location, Direction.East)},
                {Direction.South, GetSiteInDirection(location, Direction.South)},
                {Direction.West, GetSiteInDirection(location, Direction.West)}
            };
            return siteDictionary;
        }

        public Site GetSiteInDirection(Location location, Direction direction) {
            return this[GetLocationInDirection(location, direction)];
        }

        public Location GetLocationInDirection(Location location, Direction direction) {
            switch (direction) {
                case Direction.North:
                    return new Location(location.X, location.Y - 1 < 0 ? (ushort) (Height - 1) : (ushort) (location.Y - 1));
                case Direction.East:
                    return new Location(location.X + 1 > (ushort) (Width - 1) ? (ushort) 0 : (ushort) (location.X + 1), location.Y);
                case Direction.South:
                    return new Location(location.X, location.Y + 1 > (ushort) (Height - 1) ? (ushort) 0 : (ushort) (location.Y + 1));
                case Direction.West:
                    return new Location(location.X - 1 < 0 ? (ushort) (Width - 1) : (ushort) (location.X - 1), location.Y);
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
            }

        }
    }
}