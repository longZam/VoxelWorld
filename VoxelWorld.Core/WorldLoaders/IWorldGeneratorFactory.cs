namespace VoxelWorld.Core.WorldLoaders;


public interface IWorldGeneratorFactory
{
    WorldGenerator Create(WorldConfig worldConfig);
}