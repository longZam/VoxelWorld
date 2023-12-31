using System.Diagnostics;
using Grpc.Core;
using VoxelWorld.Core;
using VoxelWorld.Core.Proto;
using VoxelWorld.Core.WorldLoaders;
using VoxelWorld.Server.Services;

var builder = WebApplication.CreateBuilder(args);

// Additional configuration is required to successfully run gRPC on macOS.
// For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682

string worldPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "world");
FileSystemRegionLoader regionLoader = new(worldPath);
WorldGenerator generator = new WorldGenerator(0);

generator.modifiers.Add(0, (prev, noise, worldPosition) =>
{
    if (worldPosition.z > 80)
        return BlockType.Air;
    return noise.Noise3D(worldPosition.x, worldPosition.y, worldPosition.z, 16, 16, 16, 1) > 0.5f ? BlockType.Grass : BlockType.Air;
});
FileSystemChunkLoader chunkLoader = new(regionLoader, generator);

// Add services to the container.
builder.Services.AddGrpc(options =>
{
    options.ResponseCompressionLevel = System.IO.Compression.CompressionLevel.Optimal;
    options.ResponseCompressionAlgorithm = "gzip";
});
builder.Services.AddSingleton<World>();
builder.Services.AddSingleton<IChunkLoader, FileSystemChunkLoader>(provider => chunkLoader);
builder.Services.AddSingleton(provider => regionLoader);
var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<ChunkLoaderService>();

app.Run();
Console.Write("서버 저장 중... ");
await regionLoader.Save();
Console.WriteLine("서버 저장 완료");