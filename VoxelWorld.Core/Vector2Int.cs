namespace VoxelWorld.Core;

public readonly struct Vector2Int
{
    public static readonly Vector2Int Zero = new(0, 0);
    public static readonly Vector2Int One = new(1, 1);
    public static readonly Vector2Int Max = new(int.MaxValue, int.MaxValue);
    public static readonly Vector2Int Min = new(int.MinValue, int.MinValue);


    public readonly int x, y;


    public Vector2Int(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    public static implicit operator Proto.Vec2Int(Vector2Int v)
    {
        return new()
        {
            X = v.x,
            Y = v.y
        };
    }

    public static implicit operator Vector2Int(Proto.Vec2Int v)
    {
        return new(v.X, v.Y);
    }

    public static Vector2Int operator +(in Vector2Int v, in Vector2Int w)
    {
        return new(v.x + w.x, v.y + w.y);
    }
    
    public static Vector2Int operator -(in Vector2Int v, in Vector2Int w)
    {
        return v + -w;
    }

    public static Vector2Int operator -(in Vector2Int v)
    {
        return v * -1;
    }

    public static Vector2Int operator *(in Vector2Int v, int scala)
    {
        return new(v.x * scala, v.y * scala);
    }

    public static Vector2Int operator *(int scala, in Vector2Int v)
    {
        return v * scala;
    }

    public static Vector2Int operator /(in Vector2Int v, int scala)
    {
        return new(v.x / scala, v.y / scala);
    }

    public static bool operator ==(in Vector2Int v, in Vector2Int w)
    {
        return v.x == w.x &&
            v.y == w.y;
    }

    public static bool operator !=(in Vector2Int v, in Vector2Int w)
    {
        return !(v == w);
    }

    public override readonly bool Equals(object? obj)
    {
        if (obj == null)
            return false;
        
        return Equals((Vector2Int)obj);
    }

    public readonly bool Equals(in Vector2Int obj) => obj == this;

    public override readonly int GetHashCode() => HashCode.Combine(x.GetHashCode(), y.GetHashCode());

    public override readonly string ToString()
    {
        return $"({x}, {y})";
    }
}