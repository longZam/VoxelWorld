using VoxelWorld.Core.WorldLoaders;

namespace VoxelWorld.Core;


public class NetworkChunkLoader : IChunkLoader
{
    public Task<Chunk> LoadChunkAsync(Vector3Int chunkPosition, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}