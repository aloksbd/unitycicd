using System.Collections.Generic;
using System;

namespace ObjectModel
{
    public interface IDropTarget
    {
        List<int> DropMask { get; }
        void Dropped(IDragSource dragSource);
    }

    public class DropTarget : IDropTarget
    {

        private List<int> _dropMask;
        public List<int> DropMask { get => _dropMask; }
        private Action<IDragSource> _place;

        public DropTarget(List<int> dropMask)
        {
            _dropMask = dropMask;
        }

        public void Dropped(IDragSource dragSource)
        {
            if (_dropMask.Contains(dragSource.Mask))
            {
                _place(dragSource);
            }
        }

        public void SetPlace(Action<IDragSource> place)
        {
            _place = place;
        }
    }
}