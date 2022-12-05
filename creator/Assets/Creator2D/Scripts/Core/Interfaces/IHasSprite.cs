namespace ObjectModel
{
    interface ISprite
    {
        // path for texture in resource
        string TexturePath { get; }
        void SetPath(string path);
    }

    public class Sprite : ISprite
    {
        private string _texturePath;
        public string TexturePath { get => _texturePath; }

        public void SetPath(string path)
        {
            _texturePath = path;
        }
    }
}