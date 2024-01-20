using VoxelWorld.Core.WorldLoaders;
using VoxelWorld.Server.Services;
using VoxelWorld.Server.Test;

var builder = WebApplication.CreateBuilder(args);
string root = AppDomain.CurrentDomain.BaseDirectory;

builder.Services.AddSingleton(provider => new DirectoryInfo(root));
builder.Services.AddSingleton<IWorldGeneratorFactory, TestWorldGeneratorFactory>();
builder.Services.AddSingleton<WorldLoader>();

// gRPC 서비스 등록
builder.Services.AddGrpc(options =>
{
    options.ResponseCompressionLevel = System.IO.Compression.CompressionLevel.Optimal;
    options.ResponseCompressionAlgorithm = "gzip";
});

var app = builder.Build();

// 서버 -> 클라이언트 청크 데이터 전송 서비스 등록
app.MapGrpcService<ChunkLoaderService>();

app.Run();