namespace GoalboundGenerator
{
#pragma warning disable S4035 // Classes implementing "IEquatable<T>" should be sealed
    public class Node : IEquatable<Node>
#pragma warning restore S4035 // Classes implementing "IEquatable<T>" should be sealed
    {
        public int X
        {
            get; set;
        }

        public int Y
        {
            get; set;
        }

        public int Size
        {
            get; set;
        }

        public Node(int x, int y, int size)
        {
            X = x;
            Y = y;
            Size = size;
        }

        public bool MovesIntoWall(ICollection<(int, int)> walls, Direction direction)
        {
            switch (direction)
            {
                case Direction.NorthWest:
                    return CheckEdgesForWalls(X, Y, Size + 1, walls, Direction.North)
                            || CheckEdgesForWalls(X, Y, Size + 1, walls, Direction.West);
                case Direction.NorthEast:
                    return CheckEdgesForWalls(X - 1, Y, Size + 1, walls, Direction.North)
                            || CheckEdgesForWalls(X - 1, Y, Size + 1, walls, Direction.East);
                case Direction.SouthWest:
                    return CheckEdgesForWalls(X, Y - 1, Size + 1, walls, Direction.South)
                            || CheckEdgesForWalls(X, Y - 1, Size + 1, walls, Direction.West);
                case Direction.SouthEast:
                    return CheckEdgesForWalls(X - 1, Y - 1, Size + 1, walls, Direction.South)
                            || CheckEdgesForWalls(X - 1, Y - 1, Size + 1, walls, Direction.East);
                default:
                    return CheckEdgesForWalls(X, Y, Size, walls, direction);
            }
        }

        private static bool CheckEdgesForWalls(int x, int y, int size, ICollection<(int, int)> walls, Direction direction)
        {
            switch (direction)
            {
                case Direction.North:
                    for (int i = x; i < (x + size); i++)
                    {
                        if (walls.Contains((i, y)))
                        {
                            return true;
                        }
                    }
                    break;
                case Direction.South:
                    for (int i = x; i < (x + size); i++)
                    {
                        if (walls.Contains((i, (y + size) - 1)))
                        {
                            return true;
                        }
                    }
                    break;
                case Direction.West:
                    for (int j = y; j < (y + size); j++)
                    {
                        if (walls.Contains((x, j)))
                        {
                            return true;
                        }
                    }
                    break;
                case Direction.East:
                    for (int j = y; j < (y + size); j++)
                    {
                        if (walls.Contains(((x + size) - 1, j)))
                        {
                            return true;
                        }
                    }
                    break;
            }
            return false;
        }

        public bool ContainsWall(ICollection<(int, int)> walls)
        {
            for (int i = X; i < (X + Size); i++)
            {
                for (int j = Y; j < (Y + Size); j++)
                {
                    if (walls.Contains((i, j)))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as Node);
        }

        public bool Equals(Node? other)
        {
            return other is not null &&
                   X == other.X &&
                   Y == other.Y;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y);
        }

        public static bool operator ==(Node? left, Node? right)
        {
            return EqualityComparer<Node>.Default.Equals(left, right);
        }

        public static bool operator !=(Node? left, Node? right)
        {
            return !(left == right);
        }
    }
}