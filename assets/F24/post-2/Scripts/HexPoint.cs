using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

[System.Serializable]
public class HexPoint
{
    public Vector3Int cubicCoord;
    public bool isTop;

    public HexPoint(Vector3Int cubicCoord, bool isTop)
    {
        this.cubicCoord = cubicCoord;
        this.isTop = isTop;
    }

    public static readonly float Sqrt3Over2 = Mathf.Sqrt(3) / 2;

    public Vector3 getPosition()
    {
        //calculate center of hexagon from
        Vector3 hexCenter = new Vector3(
            Sqrt3Over2 * (cubicCoord.x - cubicCoord.z),
            1.5f * cubicCoord.y,
            0f
        );

        //offset for top/bottom point
        hexCenter.y += isTop ? +1 : -1;

        return hexCenter;
    }

    public static float Distance(HexPoint p1, HexPoint p2)
    {
        //get positions
        Vector3 p1Pos = p1.getPosition();
        Vector3 p2Pos = p2.getPosition();

        //calauclate distance
        return (p1Pos - p2Pos).magnitude;
    }

    public HexPoint SetTop(bool isTop)
    {
        this.isTop = isTop;
        return this;
    }

    public static bool operator ==(HexPoint p1, HexPoint p2)
    {
        return p1.cubicCoord == p2.cubicCoord && p1.isTop == p2.isTop;
    }

    public static bool operator !=(HexPoint p1, HexPoint p2)
    {
        return p1.cubicCoord != p2.cubicCoord || p1.isTop != p2.isTop;
    }

    public override bool Equals(object obj)
    {
        if (obj is HexPoint node)
        {
            return this == node;
        }
        return false;
    }

    public override int GetHashCode()
    {
        // Generate a hash code based on the coordinates
        return (cubicCoord, isTop).GetHashCode();
    }

    public override string ToString()
    {
        return cubicCoord.ToString() + " " + (isTop?"Top":"Bottom");
    }
}
