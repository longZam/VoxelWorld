using System.Diagnostics;
using Grpc.Core;
using Grpc.Net.Client;
using VoxelWorld.Core;
using VoxelWorld.Core.Proto;

var channel = GrpcChannel.ForAddress("http://localhost:5281");
var chunkLoaderClient = new ChunkLoader.ChunkLoaderClient(channel);
NetworkChunkLoader networkChunkLoader = new NetworkChunkLoader(chunkLoaderClient);

List<Task<Chunk>> tasks = new List<Task<Chunk>>();

Stopwatch stopwatch = Stopwatch.StartNew();

for (int z = 0; z < 16; z++)
    for (int y = -16; y < 16; y++)
        for (int x = -16; x < 16; x++)
        {
            Vector3Int position = new(x, y, z);
            // tasks.Add(Task.Run(() => networkChunkLoader.LoadChunkAsync(position)));
            tasks.Add(networkChunkLoader.LoadChunkAsync(position));
        }
            

Console.WriteLine($"{stopwatch.ElapsedMilliseconds}ms 간 작업 예약");
var result = await Task.WhenAll(tasks);
stopwatch.Stop();
Console.WriteLine($"{stopwatch.ElapsedMilliseconds}ms");