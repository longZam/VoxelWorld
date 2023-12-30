using Grpc.Core;
using VoxelWorld.Core;
using VoxelWorld.Core.Proto;
using VoxelWorld.Core.WorldLoaders;

namespace VoxelWorld.Server.Services;


public sealed partial class ChunkLoaderService : ChunkLoader.ChunkLoaderBase
{
    private readonly World world;


    public ChunkLoaderService(World world)
    {
        this.world = world;
    }

    public override async Task<LoadChunkResponse> LoadChunk(LoadChunkRequest request, ServerCallContext context)
    {
        LoadChunkResponse response = new();

        try
        {
            Chunk chunk = await world.LoadChunkAsync(request.ChunkPosition, context.CancellationToken);
            chunk.Serialize(response);
            response.ChunkPosition = request.ChunkPosition;
        }
        catch (Exception e)
        {
            throw new RpcException(new Status(StatusCode.Unknown, e.Message));
        }

        return response;
    }
}