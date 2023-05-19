using MemoryPack;

namespace GoalboundGenerator
{
    [MemoryPackable]
    public partial class GoalboundingTile
    {
        public readonly Dictionary<Direction, BoundingBox> boxes;

        public GoalboundingTile(Dictionary<Direction, BoundingBox> boxes)
        {
            this.boxes = boxes;
        }

        public BoundingBox GetBoundingBox(Direction direction)
        {
            return boxes[direction];
        }
    }
}
