namespace VoxelWorld.Core.WorldLoaders;


/// <summary>
/// 비동기 청크 로딩을 추상화하여 제공하는 인터페이스
/// </summary>
public interface IChunkLoader
{
    /// <summary>
    /// 대상 좌표의 청크 클래스를 비동기로 로딩하여 반환합니다.
    /// </summary>
    Task<Chunk> LoadChunkAsync(Vector2Int chunkPosition, CancellationToken cancellationToken = default);
}