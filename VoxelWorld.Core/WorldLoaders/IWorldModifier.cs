namespace VoxelWorld.Core.WorldLoaders;


public interface IWorldModifier
{
    ushort Modify(in Vector3Int worldPosition, ushort previousBlock, PerlinNoise3D noise);
}