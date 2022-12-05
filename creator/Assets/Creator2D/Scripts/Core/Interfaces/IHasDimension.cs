namespace ObjectModel
{
    public interface IHasDimension : IItemComponent
    {
        float Length { set; get; }
        float Width { set; get; }
        float Height { set; get; }
    }

    public class Dimension : IHasDimension
    {
        private float _length = 0f;
        private float _width = 0f;
        private float _height = 0f;
        public float Length
        {
            get => _length;
            set => _length = value;
        }
        public float Width
        {
            get => _width;
            set => _width = value;
        }
        public float Height
        {
            get => _height;
            set => _height = value;
        }

        public Dimension() { }

        public Dimension(float length, float width, float height)
        {
            _length = length;
            _width = width;
            _height = height;
        }

        public IItemComponent Clone()
        {
            return new Dimension(_length, _width, _height);
        }
    }
}
