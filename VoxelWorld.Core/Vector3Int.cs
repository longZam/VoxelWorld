namespace VoxelWorld.Core;

public struct Vector3Int
{
    public static readonly Vector3Int Zero = new Vector3Int(0, 0, 0);
    public static readonly Vector3Int One = new Vector3Int(1, 1, 1);


    public int x, y, z;


    public Vector3Int(int x, int y, int z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public static Vector3Int operator +(Vector3Int v, Vector3Int w)
    {
        return new(v.x + w.x, v.y + w.y, v.z + w.z);
    }
    
    public static Vector3Int operator -(Vector3Int v, Vector3Int w)
    {
        return v + -w;
    }

    public static Vector3Int operator -(Vector3Int v)
    {
        return v * -1;
    }

    public static Vector3Int operator *(Vector3Int v, int scala)
    {
        return new(v.x * scala, v.y * scala, v.z * scala);
    }

    public static Vector3Int operator *(int scala, Vector3Int v)
    {
        return v * scala;
    }

    public static Vector3Int operator /(Vector3Int v, int scala)
    {
        return new(v.x / scala, v.y / scala, v.z / scala);
    }

    public static bool operator ==(Vector3Int v, Vector3Int w)
    {
        return v.x == w.x &&
            v.y == w.y &&
            v.z == w.z;
    }

    public static bool operator !=(Vector3Int v, Vector3Int w)
    {
        return !(v == w);
    }

    public override readonly bool Equals(object? obj)
    {
        if (obj == null)
            return false;
        
        return Equals((Vector3Int)obj);
    }

    public readonly bool Equals(Vector3Int obj) => obj == this;

    public override readonly int GetHashCode() => HashCode.Combine(x.GetHashCode(), y.GetHashCode(), z.GetHashCode());

    public override readonly string ToString()
    {
        return $"({x}, {y}, {z})";
    }
}