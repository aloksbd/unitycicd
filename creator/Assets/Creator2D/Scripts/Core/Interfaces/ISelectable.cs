namespace ObjectModel
{
    public interface ISelectable : IItemComponent
    {
        bool IsSelected { get; }

        void Select();
        void Deselect();
    }

    public class Selectable : ISelectable
    {
        private bool _isSelected;
        public bool IsSelected { get => _isSelected; }

        public void Select()
        {
            _isSelected = true;
        }

        public void Deselect()
        {
            _isSelected = false;
        }

        public IItemComponent Clone()
        {
            return new Selectable();
        }
    }
}