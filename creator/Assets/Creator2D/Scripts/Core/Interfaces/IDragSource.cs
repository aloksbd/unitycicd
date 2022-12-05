namespace ObjectModel
{
    public interface IDragSource
    {
        int Mask { get; }
    }

    public class DragSource : IDragSource
    {
        private int _mask;
        public int Mask { get => _mask; }

        public DragSource(int mask)
        {
            _mask = mask;
        }
    }
}