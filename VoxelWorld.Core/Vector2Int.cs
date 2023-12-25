namespace VoxelWorld.Core;

public struct Vector2Int
{
    public int x, y;


    public Vector2Int(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    public static Vector2Int operator +(Vector2Int v, Vector2Int w)
    {
        return new(v.x + w.x, v.y + w.y);
    }
    
    public static Vector2Int operator -(Vector2Int v, Vector2Int w)
    {
        return v + -w;
    }

    public static Vector2Int operator -(Vector2Int v)
    {
        return v * -1;
    }

    public static Vector2Int operator *(Vector2Int v, int scala)
    {
        return new(v.x * scala, v.y * scala);
    }

    public static Vector2Int operator *(int scala, Vector2Int v)
    {
        return v * scala;
    }

    public static Vector2Int operator /(Vector2Int v, int scala)
    {
        return new(v.x / scala, v.y / scala);
    }

    public static bool operator ==(Vector2Int v, Vector2Int w)
    {
        return v.x == w.x &&
            v.y == w.y;
    }

    public static bool operator !=(Vector2Int v, Vector2Int w)
    {
        return !(v == w);
    }

    public override readonly bool Equals(object? obj)
    {
        if (obj == null)
            return false;
        
        return Equals((Vector2Int)obj);
    }

    public readonly bool Equals(Vector2Int obj) => obj == this;

    public override readonly int GetHashCode() => HashCode.Combine(x.GetHashCode(), y.GetHashCode());

    public override readonly string ToString()
    {
        return $"({x}, {y})";
    }
}