using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Halite
{
    public class MapTests
    {
        [Fact]
        public void StaysStillWhenOwnsEntireRow() {
            var height = 30;
            var width = 30;

            List<string> ownerShip = new List<string>();
            for (int i = 0; i < height; i++) {
                ownerShip.Add($"{width} 1");
            }

            ownerShip.AddRange(Enumerable.Repeat(1, height * width).Select(i => i.ToString()));

            var map = Map.ParseMap($"{height} {width}", string.Join(" ", Enumerable.Repeat(1, height * width)), string.Join(" ", ownerShip));

            Assert.Equal(Direction.Still, map.FindClosestEdge(new Location(15, 15), 1));
        }

        [Fact]
        public void MovesWestWhenAppropriate()
        {
            var height = 30;
            var width = 30;

            List<string> ownerShip = new List<string>();
            for (int i = 0; i < height; i++)
            {
                ownerShip.Add($"3 1");
                ownerShip.Add($"1 0");
                ownerShip.Add($"26 1");
            }

            ownerShip.AddRange(Enumerable.Repeat(1, height * width).Select(i => i.ToString()));

            var map = Map.ParseMap($"{height} {width}", string.Join(" ", Enumerable.Repeat(1, height * width)), string.Join(" ", ownerShip));

            Assert.Equal(Direction.West, map.FindClosestEdge(new Location(15, 15), 1));
        }

        [Fact]
        public void MovesEastWhenAppropriate()
        {
            var height = 30;
            var width = 30;

            List<string> ownerShip = new List<string>();
            for (int i = 0; i < height; i++)
            {
                ownerShip.Add($"3 1");
                ownerShip.Add($"1 0");
                ownerShip.Add($"26 1");
            }

            ownerShip.AddRange(Enumerable.Repeat(1, height * width).Select(i => i.ToString()));

            var map = Map.ParseMap($"{height} {width}", string.Join(" ", Enumerable.Repeat(1, height * width)), string.Join(" ", ownerShip));

            Assert.Equal(Direction.East, map.FindClosestEdge(new Location(29, 15), 1));
        }
    }
}
