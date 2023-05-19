using MemoryPack;

namespace GoalboundGenerator;

[MemoryPackable]
public partial class GoalBounder
{
    [MemoryPackInclude]
    private readonly Dictionary<int, GoalboundingTile[,]> goalBoundingMaps;

    public GoalBounder(Dictionary<int, GoalboundingTile[,]> goalBoundingMaps)
    {
        this.goalBoundingMaps = goalBoundingMaps;
    }

    public GoalboundingTile GetTile(int x, int y, int size) => goalBoundingMaps[size][x, y];

    public static GoalBounder ReadFromFile() => new(new());
}
