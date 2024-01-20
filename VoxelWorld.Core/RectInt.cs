namespace VoxelWorld.Core;


public readonly struct RectInt
{
    public readonly Vector2Int min, max;
    public readonly Vector2Int Center => (max + min) / 2;


    public RectInt(in Vector2Int min, in Vector2Int max)
    {
        this.min = min;
        this.max = max;
    }

    public static bool Overlaps(in RectInt a, in RectInt b)
    {
        // X 축에 대한 충돌 여부 확인
        if (a.max.x < b.min.x || a.min.x > b.max.x)
            return false;

        // Y 축에 대한 충돌 여부 확인
        if (a.max.y < b.min.y || a.min.y > b.max.y)
            return false;

        return true;
    }

    public static bool Overlaps(in RectInt rect, in Vector2Int point)
    {
        // X 축에 대한 충돌 여부 확인
        if (point.x < rect.min.x || point.x > rect.max.x)
            return false;

        // Y 축에 대한 충돌 여부 확인
        if (point.y < rect.min.y || point.y > rect.max.y)
            return false;

        return true;
    }
}