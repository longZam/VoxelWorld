namespace VoxelWorld.Core;



public struct Bounds
{
    public Vector3Int min, max;
    public readonly Vector3Int Center => (max + min + Vector3Int.One) / 2;


    public Bounds(Vector3Int min, Vector3Int max)
    {
        this.min = min;
        this.max = max;
    }

    public static bool Overlaps(Bounds a, Bounds b)
    {
        // X 축에 대한 충돌 여부 확인
        if (a.max.x < b.min.x || a.min.x > b.max.x)
            return false;

        // Y 축에 대한 충돌 여부 확인
        if (a.max.y < b.min.y || a.min.y > b.max.y)
            return false;

        // Z 축에 대한 충돌 여부 확인
        if (a.max.z < b.min.z || a.min.z > b.max.z)
            return false;

        return true;
    }
}