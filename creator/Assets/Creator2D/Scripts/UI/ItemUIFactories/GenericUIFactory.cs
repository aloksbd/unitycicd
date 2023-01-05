public class GenericUIFactory : IItemUIFactory
{
    public UIItem Create(string name)
    {
        return new UIItem(name, false);
    }
}

public sealed class FloorUIFactory : GenericUIFactory { }

public sealed class CeilingUIFactory : GenericUIFactory { }

public class WallUIFactory : GenericUIFactory { }

public class DoorUIFactory : GenericUIFactory { }

public class WindowUIFactory : GenericUIFactory { }

public class RoofUIFactory : GenericUIFactory
{
    public UIItem Create(string name)
    {
        return new UIItem(name, true);
    }
}

public class ElevatorUIFactory : GenericUIFactory { }
