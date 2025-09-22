using UnityEngine;

public abstract class ToolTip : UIWindow
{
    [SerializeField] RectTransform TooltipRect;

    public void SetPosition(Vector2 mousePosition)
    {
        if (TooltipRect == null) return;
        TooltipRect.position = mousePosition;
        WindowBound();
    }

    public void WindowBound()
    {
        if (TooltipRect == null) return;

        Vector2 size = TooltipRect.sizeDelta;
        Vector2 position = TooltipRect.position;
        Vector2 screenSize = new Vector2(Screen.width, Screen.height);

        position.x = Mathf.Clamp(position.x, 0, screenSize.x - size.x);
        position.y = Mathf.Clamp(position.y, 0, screenSize.y - size.y);

        TooltipRect.position = position;
    }

    public bool CheckMousePos(float threshold)
    {
        if (TooltipRect == null) return false;

        Vector2 mousePos = Input.mousePosition;
        Rect rect = new Rect(TooltipRect.position, TooltipRect.sizeDelta);

        //change bounds to threshold
        rect.xMin -= threshold;
        rect.yMin -= threshold;
        rect.xMax += threshold;
        rect.yMax += threshold;

        return rect.Contains(mousePos);
    }
}
