using System.Diagnostics;
using System.Threading.Tasks.Dataflow;
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

    public async Task<Chunk> LoadChunkAsync(Vector3Int chunkPosition, CancellationToken cancellationToken = default)
    {
        var response = await chunkLoaderClient.LoadChunkAsync(new() { ChunkPosition = chunkPosition },
                                                            headers: headers,
                                                            cancellationToken: cancellationToken);

        return await Task.Run(() =>
        {
            Chunk chunk = new Chunk();
            chunk.Deserialize(response);
            return chunk;
        }, cancellationToken);
    }
}