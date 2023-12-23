namespace VoxelWorld.Core.WorldLoaders;


public interface IChunkLoader
{
    Task<Chunk> LoadChunkAsync(Vector3Int chunkPosition, CancellationToken cancellationToken = default);
}