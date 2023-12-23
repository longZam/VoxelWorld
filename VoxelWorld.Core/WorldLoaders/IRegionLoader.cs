namespace VoxelWorld.Core.WorldLoaders;


public interface IRegionLoader
{
    Task<Region> LoadRegionAsync(Vector2Int regionPosition, CancellationToken cancellationToken = default);
}