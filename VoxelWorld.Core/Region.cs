using System.Buffers;
using System.Diagnostics;

namespace VoxelWorld.Core;


// world의 최소 파일 단위인 .region의 클래스
public class Region
{
    public const int CHUNK_CORNER = 16;
    public const int CHUNK_SIDE = CHUNK_CORNER * CHUNK_CORNER;
    public const int CHUNK_VOLUME = CHUNK_CORNER * CHUNK_CORNER * CHUNK_CORNER;
    public const int BLOCK_CORNER = CHUNK_CORNER * Chunk.CORNER;
    public const int BLOCK_SIDE = CHUNK_SIDE * Chunk.SIDE;
    public const int BLOCK_VOLUME = CHUNK_VOLUME * Chunk.VOLUME;

    private readonly Vector2Int position;
    private readonly Chunk[] chunks;
    

    public Chunk this[int x, int y, int z]
    {
        get => chunks[LocalToIndex(new Vector3Int(x, y, z))];
    }

    public Region(Vector2Int position, bool clear = false)
    {
        this.position = position;
        this.chunks = ArrayPool<Chunk>.Shared.Rent(CHUNK_VOLUME);

        Vector3Int offset = World.RegionToWorld(position);
        offset = World.WorldToChunk(offset);

        for (int i = 0; i < CHUNK_VOLUME; i++)
            this.chunks[i] = new Chunk(clear);
    }

    ~Region()
    {
        ArrayPool<Chunk>.Shared.Return(chunks, true);
    }

    public void Serialize(BinaryWriter binaryWriter)
    {
        for (int i = 0; i < CHUNK_VOLUME; i++)
        {
            chunks[i].Serialize(binaryWriter);
        }
    }

    public void Deserialize(BinaryReader binaryReader)
    {
        for (int i = 0; i < CHUNK_VOLUME; i++)
        {
            chunks[i].Deserialize(binaryReader);
        }
            
    }

    private static int LocalToIndex(Vector3Int chunkPosition)
    {
        Debug.Assert(0 <= chunkPosition.x && chunkPosition.x < CHUNK_CORNER, $"chunkPosition.x must be 0 <= chunkPosition.x < {CHUNK_CORNER}, chunkPosition.x = {chunkPosition.x}");
        Debug.Assert(0 <= chunkPosition.y && chunkPosition.y < CHUNK_CORNER, $"chunkPosition.y must be 0 <= chunkPosition.y < {CHUNK_CORNER}, chunkPosition.y = {chunkPosition.y}");
        Debug.Assert(0 <= chunkPosition.z && chunkPosition.z < CHUNK_CORNER, $"chunkPosition.z must be 0 <= chunkPosition.z < {CHUNK_CORNER}, chunkPosition.z = {chunkPosition.z}");

        return chunkPosition.x + CHUNK_CORNER * chunkPosition.y + CHUNK_SIDE * chunkPosition.z;
    }

    public static Vector3Int WorldToLocal(Vector3Int chunkPosition)
    {
        Vector3Int localChunkPosition = new Vector3Int(chunkPosition.x % CHUNK_CORNER,
                                                       chunkPosition.y % CHUNK_CORNER,
                                                       chunkPosition.z % CHUNK_CORNER);

        if (localChunkPosition.x < 0) localChunkPosition.x += CHUNK_CORNER;
        if (localChunkPosition.y < 0) localChunkPosition.y += CHUNK_CORNER;
        if (localChunkPosition.z < 0) localChunkPosition.z += CHUNK_CORNER;
    
        return localChunkPosition;
    }
}