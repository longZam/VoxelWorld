
using System.Collections.Concurrent;

namespace VoxelWorld.Core.WorldLoaders;


public class EmptyRegionLoader : IRegionLoader
{
    public Task<Region> LoadRegionAsync(Vector2Int regionPosition, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new Region(regionPosition, true));
    }
}