using System.Buffers;
using System.Diagnostics;
using VoxelWorld.Core.Proto;

namespace VoxelWorld.Core;


public class Chunk
{
    public const int CORNER_BIT = 4;
    public const int CORNER = CORNER_BIT * CORNER_BIT;
    public const int SIDE = CORNER * CORNER;
    public const int VOLUME = CORNER * CORNER * CORNER;

    private readonly BlockType[] rawData;
    public bool initialized;


    public BlockType this[Vector3Int position]
    {
        get
        {
            Debug.Assert(0 <= position.x && position.x < CORNER, $"x must be 0 <= x < {CORNER}, x = {position.x}");
            Debug.Assert(0 <= position.y && position.y < CORNER, $"y must be 0 <= y < {CORNER}, y = {position.y}");
            Debug.Assert(0 <= position.z && position.z < CORNER, $"z must be 0 <= z < {CORNER}, z = {position.z}");

            return rawData[position.x + CORNER * position.y + SIDE * position.z];
        }

        set
        {
            Debug.Assert(0 <= position.x && position.x < CORNER, $"x must be 0 <= x < {CORNER}, x = {position.x}");
            Debug.Assert(0 <= position.y && position.y < CORNER, $"y must be 0 <= y < {CORNER}, y = {position.y}");
            Debug.Assert(0 <= position.z && position.z < CORNER, $"z must be 0 <= z < {CORNER}, z = {position.z}");

            rawData[position.x + CORNER * position.y + SIDE * position.z] = value;
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

    public void Serialize(LoadChunkResponse response)
    {
        response.Initialized = initialized;

        if (initialized)
            for (int i = 0; i < VOLUME; i++)
                response.RawData.Add((uint)rawData[i]);
    }
    
    public void Deserialize(LoadChunkResponse response)
    {
        initialized = response.Initialized;

        if (initialized)
            for (int i = 0; i < VOLUME; i++)
                rawData[i] = (BlockType)response.RawData[i];
    }
}