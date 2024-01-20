using System.Diagnostics;
using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Net.Compression;
using VoxelWorld.Core;
using VoxelWorld.Core.Proto;

Console.WriteLine("sample address: http://1.2.3.4:5281");
Console.Write("server address: ");
string address = Console.ReadLine() ?? throw new Exception();

GrpcChannelOptions grpcChannelOptions = new GrpcChannelOptions
{
    CompressionProviders = new[]
    {
        new GzipCompressionProvider(System.IO.Compression.CompressionLevel.Optimal)
    }
};

var channel = GrpcChannel.ForAddress(address, grpcChannelOptions);
var chunkLoaderClient = new ChunkLoader.ChunkLoaderClient(channel);
Metadata headers = new()
{
    { "grpc-internal-encoding-request", "gzip" }
};

NetworkChunkLoader networkChunkLoader = new NetworkChunkLoader(chunkLoaderClient, headers);

List<Task<Chunk>> tasks = new List<Task<Chunk>>();

Stopwatch stopwatch = Stopwatch.StartNew();

for (int y = -4; y < 4; y++)
    for (int x = -4; x < 4; x++)
        tasks.Add(networkChunkLoader.LoadChunkAsync(new(x, y)));

Console.WriteLine($"{stopwatch.ElapsedMilliseconds}ms 간 작업 예약");
var result = await Task.WhenAll(tasks);
stopwatch.Stop();
Console.WriteLine($"{stopwatch.ElapsedMilliseconds}ms");