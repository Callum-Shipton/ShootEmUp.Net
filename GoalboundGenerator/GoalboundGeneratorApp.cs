using MemoryPack;
using MemoryPack.Compression;

using SkiaSharp;

using System.Collections.Concurrent;
using System.Drawing;

namespace GoalboundGenerator;

public static class GoalboundGeneratorApp
{
    private const int MAXIMUM_SIZE = 4;
    private const string IN_MAP_FILE = "../../../../ShootEmUp/Resources/Levels/Level1.png";
    private const string OUT_MAP_FILE = "../../../../ShootEmUp/Resources/Levels/Level1.boundNew";

    private const int BROWNWALL_COLOR = -7864299;
    private const int GREYWALL_COLOR = -8421505;
    private const int LIGHTWATER_COLOR = -16735512;
    private const int DARKWATER_COLOR = -12629812;

    private const int TRANSPORT_COLOR = -6075996;

    private const int GRASS_COLOR = -4856291;
    private const int PATH_COLOR = -1055568;

    private static Map map = new(0, 0);
    private static readonly ConcurrentDictionary<int, GoalboundingTile[,]> workingGoalboundingMaps = new();

    private static int mapWidth;
    private static int mapHeight;

    public static async Task Main()
    {
        LoadMap();

        for (int i = 1; i <= MAXIMUM_SIZE; i++)
        {
            workingGoalboundingMaps[i] = new GoalboundingTile[mapWidth, mapHeight];
        }

        CreateGoalBoundingBoxes();

        await SaveGoalbounder().ConfigureAwait(false);
    }

    private static async Task SaveGoalbounder()
    {
        Dictionary<int, GoalboundingTile[,]> goalboundingMaps = new();

        foreach (var bound in workingGoalboundingMaps)
        {
            goalboundingMaps[bound.Key] = bound.Value;
        }

        var goalbounder = new GoalBounder(goalboundingMaps);

        try
        {
            using var compressor = new BrotliCompressor();
            MemoryPackSerializer.Serialize(compressor, goalbounder);
            byte[] bytes = compressor.ToArray();
            await File.WriteAllBytesAsync(OUT_MAP_FILE, bytes).ConfigureAwait(false);
        }
        catch (IOException i)
        {
            Console.WriteLine($"Failed to write bounds: {i.Message}");
        }
    }

    private static void LoadMap()
    {
        using var pngStream = new FileStream(IN_MAP_FILE, FileMode.Open, FileAccess.Read);
        using var image = SKBitmap.Decode(pngStream);

        mapWidth = image.Width;
        mapHeight = image.Height;
        map = new Map(mapWidth, mapHeight);

        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                int color = (int)(uint)image.GetPixel(x, y);
                switch (color)
                {
                    case BROWNWALL_COLOR:
                    case GREYWALL_COLOR:
                    case LIGHTWATER_COLOR:
                    case DARKWATER_COLOR:
                    case TRANSPORT_COLOR:
                        map.AddWall(x, y);
                        break;
                    case GRASS_COLOR:
                    case PATH_COLOR:
                        break;
                    default:
                        Console.WriteLine("Tile type not found: " + color);
                        break;
                }
            }
        }
    }

    private static void CreateGoalBoundingBoxes()
    {
        for (int size = 1; size <= MAXIMUM_SIZE; size++)
        {
            _ = Parallel.For<(Queue<DirectionNode> Open, HashSet<Point> Closed)>(0, mapWidth, () =>
            {
                // queue for tiles to be looked at
                Queue<DirectionNode> open = new(mapWidth * mapHeight);

                // list of already viewed tiles
                HashSet<Point> closed = new(mapWidth * mapHeight);

                return (open, closed);
            }, (x, _, local) =>
            {
                for (int y = 0; y < mapHeight; y++)
                {
                    local.Open.Clear();
                    local.Closed.Clear();

                    CreateGoalBoundingBox(size, x, y, local.Open, local.Closed);
                }

                Console.WriteLine($"size:{size}, row:{x} - Completed");

                return local;
            }, (_) => { }
            );
        }
    }

    private static void CreateGoalBoundingBox(int size, int x, int y, Queue<DirectionNode> open, HashSet<Point> closed)
    {
        var start = map.GetPoint(x, y, Direction.NotSet);

        if (!map.IsWall(x, y))
        {
            AddChildNodes(open, closed, start, size);

            var workingBoxes = InitBoundingBoxes(open);

            FillMap(open, closed, size, workingBoxes);

            Dictionary<Direction, BoundingBox> boxes = new(8);
            foreach (var box in workingBoxes)
            {
                boxes[box.Key] = box.Value;
            }

            workingGoalboundingMaps[size][x, y] = new GoalboundingTile(boxes);
        }
    }

    private static void AddChildNodes(Queue<DirectionNode> open, ICollection<Point> closed, DirectionNode startNode, int size)
    {
        int startX = startNode.Point.X;
        int startY = startNode.Point.Y;

        bool first = startNode.Direction == Direction.NotSet;

        var north = map.GetPoint(startX, startY - 1, first ? Direction.North : startNode.Direction);
        AddNode(open, closed, size, north);

        var west = map.GetPoint(startX - 1, startY, first ? Direction.West : startNode.Direction);
        AddNode(open, closed, size, west);

        var south = map.GetPoint(startX, startY + 1, first ? Direction.South : startNode.Direction);
        AddNode(open, closed, size, south);

        var east = map.GetPoint(startX + 1, startY, first ? Direction.East : startNode.Direction);
        AddNode(open, closed, size, east);

        var northWest = map.GetPoint(startX - 1, startY - 1, first ? Direction.NorthWest : startNode.Direction);
        AddNode(open, closed, size, northWest);

        var southWest = map.GetPoint(startX - 1, startY + 1, first ? Direction.SouthWest : startNode.Direction);
        AddNode(open, closed, size, southWest);

        var southEast = map.GetPoint(startX + 1, startY + 1, first ? Direction.SouthEast : startNode.Direction);
        AddNode(open, closed, size, southEast);

        var northEast = map.GetPoint(startX + 1, startY - 1, first ? Direction.NorthEast : startNode.Direction);
        AddNode(open, closed, size, northEast);

        static void AddNode(Queue<DirectionNode> open, ICollection<Point> closed, int size, DirectionNode northEast)
        {
            if (!closed.Contains(northEast.Point) && !map.MovesIntoWall(northEast.Point.X, northEast.Point.Y, size, northEast.Direction))
            {
                open.Enqueue(northEast);
                closed.Add(northEast.Point);
            }
        }
    }

    private static Dictionary<Direction, WorkingBoundingBox> InitBoundingBoxes(IEnumerable<DirectionNode> startingNodes)
    {
        Dictionary<Direction, WorkingBoundingBox> boxes = new(8);

        foreach (var node in startingNodes)
        {
            boxes[node.Direction] = new WorkingBoundingBox(node.Point.X, node.Point.Y);
        }

        return boxes;
    }

    private static void FillMap(Queue<DirectionNode> open, ICollection<Point> closed, int size, Dictionary<Direction, WorkingBoundingBox> boxes)
    {
        while (open.Any())
        {
            var node = open.Dequeue();

            boxes[node.Direction].ExpandBoxToFitPoint(node.Point.X, node.Point.Y);

            AddChildNodes(open, closed, node, size);
        }
    }
}
