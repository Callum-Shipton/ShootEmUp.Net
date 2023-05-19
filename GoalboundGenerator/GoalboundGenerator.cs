using MemoryPack;
using MemoryPack.Compression;

using SkiaSharp;

using System.Collections.Concurrent;

namespace GoalboundGenerator;

public static class GoalboundGenerator
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

    private static HashSet<(int, int)> walls = new();
    private static ConcurrentDictionary<int, GoalboundingTile[,]> workingGoalboundingMaps = new();

    private static int mapWidth;
    private static int mapHeight;

    public static async Task Main(string[] args)
    {
        LoadMap();

        for (int i = 1; i <= MAXIMUM_SIZE; i++)
        {
            workingGoalboundingMaps[i] = new GoalboundingTile[mapWidth, mapHeight];
        }

        CreateGoalBoundingBoxes();

        await SaveGoalbounder();
    }

    private static async Task SaveGoalbounder()
    {
        Dictionary<int, GoalboundingTile[,]> goalboundingMaps = new();

        foreach (var bound in workingGoalboundingMaps)
        {
            goalboundingMaps[bound.Key] = bound.Value;
        }

        GoalBounder goalbounder = new GoalBounder(goalboundingMaps);

        try
        {
            using var compressor = new BrotliCompressor();
            MemoryPackSerializer.Serialize(compressor, goalbounder);
            var bytes = compressor.ToArray();
            await File.WriteAllBytesAsync(OUT_MAP_FILE, bytes);
        }
        catch (IOException i)
        {
            Console.WriteLine($"Failed to write bounds: {i.Message}");
        }
    }

    private static void LoadMap()
    {
        using FileStream pngStream = new FileStream(IN_MAP_FILE, FileMode.Open, FileAccess.Read);
        using var image = SKBitmap.Decode(pngStream);

        mapWidth = image.Width;
        mapHeight = image.Height;

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
                        walls.Add((x, y));
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
            Parallel.For(0, mapWidth, (x) =>
            {
                for (int y = 0; y < mapHeight; y++)
                {
                    CreateGoalBoundingBox(size, x, y);
                }
                Console.WriteLine($"size:{size}, row:{x} - Completed");
            });
        }
    }

    private static void CreateGoalBoundingBox(int size, int x, int y)
    {
        DirectionNode start = new(x, y, size, null);

        if (!start.ContainsWall(walls))
        {
            // queue for tiles to be looked at
            Queue<DirectionNode> open = new();

            // list of already viewed tiles
            HashSet<DirectionNode> closed = new();

            List<DirectionNode> startingNodes = GenerateChildNodes(start);

            foreach (var node in startingNodes)
            {
                open.Enqueue(node);
                closed.Add(node);
            }

            Dictionary<Direction, WorkingBoundingBox> workingBoxes = InitBoundingBoxes(startingNodes);

            FillMap(open, closed, workingBoxes);

            Dictionary<Direction, BoundingBox> boxes = new();
            foreach (var box in workingBoxes)
            {
                boxes[box.Key] = box.Value;
            }

            workingGoalboundingMaps[size][x, y] = new GoalboundingTile(boxes);
        }
    }

    private static List<DirectionNode> GenerateChildNodes(DirectionNode startNode)
    {
        List<DirectionNode> childNodes = new();

        int startX = startNode.X;
        int startY = startNode.Y;
        int size = startNode.Size;

        DirectionNode north = new DirectionNode(startX, startY - 1, size, startNode.Direction ?? Direction.North);
        if (!north.MovesIntoWall(walls, Direction.North))
        {
            childNodes.Add(north);
        }

        DirectionNode west = new DirectionNode(startX - 1, startY, size, startNode.Direction ?? Direction.West);
        if (!west.MovesIntoWall(walls, Direction.West))
        {
            childNodes.Add(west);
        }

        DirectionNode south = new DirectionNode(startX, startY + 1, size, startNode.Direction ?? Direction.South);
        if (!south.MovesIntoWall(walls, Direction.South))
        {
            childNodes.Add(south);
        }

        DirectionNode east = new DirectionNode(startX + 1, startY, size, startNode.Direction ?? Direction.East);
        if (!east.MovesIntoWall(walls, Direction.East))
        {
            childNodes.Add(east);
        }

        DirectionNode northWest = new DirectionNode(startX - 1, startY - 1, size, startNode.Direction ?? Direction.NorthWest);
        if (!northWest.MovesIntoWall(walls, Direction.NorthWest))
        {
            childNodes.Add(northWest);
        }

        DirectionNode southWest = new DirectionNode(startX - 1, startY + 1, size, startNode.Direction ?? Direction.SouthWest);
        if (!southWest.MovesIntoWall(walls, Direction.SouthWest))
        {
            childNodes.Add(southWest);
        }

        DirectionNode southEast = new DirectionNode(startX + 1, startY + 1, size, startNode.Direction ?? Direction.SouthEast);
        if (!southEast.MovesIntoWall(walls, Direction.SouthEast))
        {
            childNodes.Add(southEast);
        }

        DirectionNode northEast = new DirectionNode(startX + 1, startY - 1, size, startNode.Direction ?? Direction.NorthEast);
        if (!northEast.MovesIntoWall(walls, Direction.NorthEast))
        {
            childNodes.Add(northEast);
        }

        return childNodes;
    }

    private static Dictionary<Direction, WorkingBoundingBox> InitBoundingBoxes(IEnumerable<DirectionNode> startingNodes)
    {
        Dictionary<Direction, WorkingBoundingBox> boxes = new();

        foreach (var node in startingNodes)
        {
#pragma warning disable CS8629 // Nullable value type may be null.
            boxes[node.Direction.Value] = new WorkingBoundingBox(node.X, node.Y);
#pragma warning restore CS8629 // Nullable value type may be null.
        }
        return boxes;
    }

    private static void FillMap(Queue<DirectionNode> open, ICollection<DirectionNode> closed, Dictionary<Direction, WorkingBoundingBox> boxes)
    {
        while (open.Any())
        {
            var node = open.Dequeue();

#pragma warning disable CS8629 // Nullable value type may be null.
            boxes[node.Direction.Value].ExpandBoxToFitPoint(node.X, node.Y);
#pragma warning restore CS8629 // Nullable value type may be null.

            List<DirectionNode> childNodes = GenerateChildNodes(node);

            foreach (var child in childNodes)
            {
                if (!closed.Contains(child))
                {
                    open.Enqueue(child);
                    closed.Add(child);
                }
            }
        }
    }
}
