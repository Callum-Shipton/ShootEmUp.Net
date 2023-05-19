namespace GoalboundGenerator
{
    public class DirectionNode : Node
    {
        public DirectionNode(int x, int y, int size, Direction? direction) : base(x, y, size)
        {
            Direction = direction;
        }

        public Direction? Direction
        {
            get; set;
        }
    }
}
