using MemoryPack;

namespace GoalboundGenerator
{
    [MemoryPackable]
    public partial record BoundingBox(int X, int Y, int Z, int W)
    {
        public bool ContainsPoint(int x, int y, int size)
        {
            return (x >= X)
                && (x <= Z + (size - 1))
                && (y >= Y)
                && (y <= W + (size - 1));
        }
    }

    public class WorkingBoundingBox
    {
        private int _x;
        private int _y;
        private int _z;
        private int _w;

        public WorkingBoundingBox(int x, int y)
        {
            _x = x;
            _y = y;
            _z = x;
            _w = y;
        }

        public void ExpandBoxToFitPoint(int x, int y)
        {
            _x = Math.Min(x, _x);
            _y = Math.Min(y, _y);
            _z = Math.Max(x, _z);
            _w = Math.Max(y, _w);
        }

        public static implicit operator BoundingBox(WorkingBoundingBox box) => new(box._x, box._y, box._z, box._w);
    }
}
