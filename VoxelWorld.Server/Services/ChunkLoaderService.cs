using Grpc.Core;
using VoxelWorld.Core;
using VoxelWorld.Core.Proto;
using VoxelWorld.Core.WorldLoaders;

namespace VoxelWorld.Server.Services;


public sealed partial class ChunkLoaderService : ChunkLoader.ChunkLoaderBase
{
    private readonly WorldLoader worldLoader;


    public ChunkLoaderService(WorldLoader worldLoader)
    {
        this.worldLoader = worldLoader;
    }

    public override async Task<LoadChunkResponse> LoadChunk(LoadChunkRequest request, ServerCallContext context)
    {
        if (!RectInt.Overlaps(in World.CHUNK_BOUNDARY, request.ChunkPosition))
            throw new RpcException(new(StatusCode.OutOfRange, "요청한 위치가 world의 범위를 벗어났습니다."));

        var status = worldLoader.GetWorldsActivationStatus();

        if (!status.TryGetValue(request.WorldName, out bool activation))
            throw new RpcException(new(StatusCode.InvalidArgument, "요청한 world가 존재하지 않습니다."));
        if (!activation)
            throw new RpcException(new(StatusCode.FailedPrecondition, "요청한 world는 현재 활성 상태가 아닙니다."));

        LoadChunkResponse response = new();

        World world = worldLoader.GetWorld(request.WorldName);
        Chunk chunk = await world.LoadChunkAsync(request.ChunkPosition, context.CancellationToken);
        chunk.Serialize(response);

        return response;
    }
}