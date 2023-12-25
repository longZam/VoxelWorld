using System.Buffers;
using System.Diagnostics;

namespace VoxelWorld.Core;


public class Chunk
{
    public const int CORNER = 16;
    public const int SIDE = CORNER * CORNER;
    public const int VOLUME = CORNER * CORNER * CORNER;

    private readonly BlockType[] rawData;
    public bool initialized;


    public BlockType this[int x, int y, int z]
    {
        get
        {
            Debug.Assert(0 <= x && x < CORNER, $"x must be 0 <= x < {CORNER}, x = {x}");
            Debug.Assert(0 <= y && y < CORNER, $"y must be 0 <= y < {CORNER}, y = {y}");
            Debug.Assert(0 <= z && z < CORNER, $"z must be 0 <= z < {CORNER}, z = {z}");

            return rawData[x + CORNER * y + SIDE * z];
        }

        set
        {
            Debug.Assert(0 <= x && x < CORNER, $"x must be 0 <= x < {CORNER}, x = {x}");
            Debug.Assert(0 <= y && y < CORNER, $"y must be 0 <= y < {CORNER}, y = {y}");
            Debug.Assert(0 <= z && z < CORNER, $"z must be 0 <= z < {CORNER}, z = {z}");

            rawData[x + CORNER * y + SIDE * z] = value;
        }
    }

    public Chunk(bool clear = false)
    {
        this.rawData = ArrayPool<BlockType>.Shared.Rent(CORNER * CORNER * CORNER);
        
        if (clear)
            Array.Clear(this.rawData, 0, VOLUME);
    }

    ~Chunk()
    {
        ArrayPool<BlockType>.Shared.Return(this.rawData);
    }

    public void Serialize(BinaryWriter binaryWriter)
    {
        binaryWriter.Write(initialized);

        if (initialized)
            for (int i = 0; i < VOLUME; i++)
                binaryWriter.Write((ushort)rawData[i]);
    }

    public void Deserialize(BinaryReader binaryReader)
    {
        initialized = binaryReader.ReadBoolean();

        if (initialized)
            for (int i = 0; i < VOLUME; i++)
                rawData[i] = (BlockType)binaryReader.ReadUInt16();
    }
}