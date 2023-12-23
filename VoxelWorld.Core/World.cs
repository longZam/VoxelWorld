using VoxelWorld.Core.WorldLoaders;

namespace VoxelWorld.Core;


public class World
{
    // 각 axis에 대해 정렬된 리스트를 만들고, 이진 탐색으로 탐색하기?
    private readonly IChunkLoader chunkLoader;
    private readonly Dictionary<Vector3Int, Chunk> chunks;


    public World(IChunkLoader chunkLoader)
    {
        this.chunkLoader = chunkLoader;
        this.chunks = new Dictionary<Vector3Int, Chunk>();
    }
    
    public static Vector2Int WorldToRegion(Vector3Int worldPosition)
    {
        return worldPosition / Region.BLOCK_CORNER;
    }

    public static Vector3Int WorldToChunk(Vector3Int worldPosition)
    {
        return worldPosition / Chunk.CORNER;
    }

    public static Vector3Int RegionToWorld(Vector2Int regionPosition)
    {
        return new Vector3Int(regionPosition.x * Region.BLOCK_CORNER,
                              regionPosition.y * Region.BLOCK_CORNER,
                              0);
    }

    public static Vector3Int ChunkToWorld(Vector3Int chunkPosition)
    {
        return chunkPosition * Chunk.CORNER;
    }
}