using System.Drawing;

namespace GoalboundGenerator
{
    public class Map
    {
        private readonly bool[,] walls;
        private readonly DirectionNode[][][] points;
        private readonly int width;
        private readonly int height;

        private readonly DirectionNode invalidPoint = new(new Point(int.MinValue, int.MinValue), Direction.NotSet);

        public Map(int width, int height)
        {
            walls = new bool[width, height];
            points = new DirectionNode[width][][];

            for (int i = 0; i < width; i++)
            {
                points[i] = new DirectionNode[height][];

                for (int j = 0; j < height; j++)
                {
                    var directions = Enum.GetValues<Direction>();
                    points[i][j] = new DirectionNode[directions.Length];
                    foreach (var direction in directions)
                    {
                        points[i][j][(int)direction] = new(new Point(i, j), direction);
                    }

                }
            }

            this.width = width;
            this.height = height;
        }

        public DirectionNode GetPoint(int x, int y, Direction direction)
        {
            if (x < 0 || y < 0 || x >= width || y >= height)
                return invalidPoint;
            else
                return points[x][y][(int)direction];
        }

        public void AddWall(int x, int y)
        {
            walls[x, y] = true;
        }

        public bool IsWall(int x, int y)
        {
            if (x < 0 || y < 0 || x >= width || y >= height)
                return true;
            else
                return walls[x, y];
        }

        public bool MovesIntoWall(int x, int y, int size, Direction direction)
        {
            return direction switch
            {
                Direction.NorthWest => CheckEdgesForWalls(x, y, size + 1, Direction.North)
                                            || CheckEdgesForWalls(x, y, size + 1, Direction.West),
                Direction.NorthEast => CheckEdgesForWalls(x - 1, y, size + 1, Direction.North)
                                            || CheckEdgesForWalls(x - 1, y, size + 1, Direction.East),
                Direction.SouthWest => CheckEdgesForWalls(x, y - 1, size + 1, Direction.South)
                                            || CheckEdgesForWalls(x, y - 1, size + 1, Direction.West),
                Direction.SouthEast => CheckEdgesForWalls(x - 1, y - 1, size + 1, Direction.South)
                                            || CheckEdgesForWalls(x - 1, y - 1, size + 1, Direction.East),
                _ => CheckEdgesForWalls(x, y, size, direction),
            };
        }

        private bool CheckEdgesForWalls(int x, int y, int size, Direction direction)
        {
            switch (direction)
            {
                case Direction.North:
                    for (int i = x; i < (x + size); i++)
                    {
                        if (IsWall(i, y))
                        {
                            return true;
                        }
                    }
                    break;
                case Direction.South:
                    for (int i = x; i < (x + size); i++)
                    {
                        if (IsWall(i, (y + size) - 1))
                        {
                            return true;
                        }
                    }
                    break;
                case Direction.West:
                    for (int j = y; j < (y + size); j++)
                    {
                        if (IsWall(x, j))
                        {
                            return true;
                        }
                    }
                    break;
                case Direction.East:
                    for (int j = y; j < (y + size); j++)
                    {
                        if (IsWall((x + size) - 1, j))
                        {
                            return true;
                        }
                    }
                    break;
                default:
                    return true;
            }
            return false;
        }
    }
}
