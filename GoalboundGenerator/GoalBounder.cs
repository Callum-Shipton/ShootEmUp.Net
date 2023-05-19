using MemoryPack;

namespace GoalboundGenerator
{
    [MemoryPackable]
    public partial class GoalBounder
    {
        public readonly Dictionary<int, GoalboundingTile[,]> goalBoundingMaps;

        public GoalBounder(Dictionary<int, GoalboundingTile[,]> goalBoundingMaps)
        {
            this.goalBoundingMaps = goalBoundingMaps;
        }

        public GoalboundingTile GetTile(int x, int y, int size)
        {
            return goalBoundingMaps[size][x, y];
        }

        public static GoalBounder ReadFromFile(string filePath)
        {
            return new(new());
        }
    }
}
