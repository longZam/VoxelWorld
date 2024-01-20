using Grpc.Core;
using VoxelWorld.Core.Proto;
using VoxelWorld.Core.WorldLoaders;

namespace VoxelWorld.Core;


public class NetworkChunkLoader : IChunkLoader
{
    private readonly ChunkLoader.ChunkLoaderClient chunkLoaderClient;
    private readonly Metadata? headers;


    public NetworkChunkLoader(ChunkLoader.ChunkLoaderClient chunkLoaderClient, Metadata? headers = null)
    {
        this.chunkLoaderClient = chunkLoaderClient;
        this.headers = headers;
    }

    public async Task<Chunk> LoadChunkAsync(Vector2Int chunkPosition, CancellationToken cancellationToken = default)
    {
        LoadChunkRequest request = new()
        {
            WorldName = "world", // todo: 어떻게든 worldName을 주입받아야 한다.
            ChunkPosition = chunkPosition
        };

        var response = await chunkLoaderClient.LoadChunkAsync(request: request,
                                                            headers: headers,
                                                            cancellationToken: cancellationToken);

        return await Task.Run(() =>
        {
            Chunk chunk = new();
            chunk.Deserialize(response);
            return chunk;
        }, cancellationToken);
    }
}