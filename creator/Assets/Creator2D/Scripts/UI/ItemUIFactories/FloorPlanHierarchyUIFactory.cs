using UnityEngine.UIElements;

public class FloorPlanHierarchyUIFactory : IItemUIFactory
{
    public UIItem Create(string name = "") // default empty name because name comes from xml 
    {
        Foldout foldout = CreatorUIController.getRoot().Q<Foldout>("floor-plan-hierarchy");
        if (foldout == null)
        {
            foldout = new Foldout();
            foldout.name = "floor-plan-hierarchy";
            foldout.AddToClassList("bold-font");
            foldout.AddToClassList("full-width");
            CreatorUIController.getRoot().Q<VisualElement>("floor-panel").Add(foldout);
        }
        foldout.value = true;
        // foldout.AddToClassList(WHCSSConstants.PINK_BACKGROUND_COLOR);
        UIItem itemUI = new UIItem(foldout);
        return itemUI;
    }
}