using System.Buffers;
using System.Diagnostics;
using Nito.AsyncEx;

namespace VoxelWorld.Core.WorldLoaders;


/// <summary>
/// <para>
/// World의 파일 시스템 최소 저장 단위 .region
/// </para>
/// <para>
/// Region의 모든 공용 및 보호된 멤버는 스레드로부터 안전하며 여러 스레드에서 동시에 사용할 수 있습니다.
/// </para>
/// </summary>
public class Region
{
    public class Factory
    {
        private readonly WorldGenerator worldGenerator;


        public Factory(WorldGenerator worldGenerator)
        {
            this.worldGenerator = worldGenerator;
        }

        public Region Create(in Vector2Int position) => new(worldGenerator, in position);
    }


    /// <summary>
    /// 한 Region 당 X축, Y축 청크 개수
    /// </summary>
    public const int WIDTH = 32;

    private readonly WorldGenerator worldGenerator;
    private readonly Vector2Int position;
    private readonly Chunk[] chunks;
    private readonly bool[] initialized;
    private readonly AsyncReaderWriterLock rwlock;


    private Region(WorldGenerator worldGenerator, in Vector2Int position)
    {
        this.worldGenerator = worldGenerator;
        this.position = position;
        this.chunks = ArrayPool<Chunk>.Shared.Rent(WIDTH * WIDTH);
        this.initialized = ArrayPool<bool>.Shared.Rent(WIDTH * WIDTH);
        this.rwlock = new();

        for (int i = 0; i < WIDTH * WIDTH; i++)
            this.chunks[i] = new Chunk();
        
        Array.Fill(this.initialized, false);
    }

    ~Region()
    {
        ArrayPool<Chunk>.Shared.Return(chunks, true);
        ArrayPool<bool>.Shared.Return(initialized, false);
    }

    public void Serialize(BinaryWriter binaryWriter)
    {
        using (rwlock.ReaderLock())
        {
            for (int i = 0; i < WIDTH * WIDTH; i++)
            {
                binaryWriter.Write(initialized[i]);

                if (initialized[i])
                    chunks[i].Serialize(binaryWriter);
            }
        }
    }

    public void Deserialize(BinaryReader binaryReader)
    {
        using (rwlock.ReaderLock())
        {
            for (int i = 0; i < WIDTH * WIDTH; i++)
            {
                initialized[i] = binaryReader.ReadBoolean();

                if (initialized[i])
                    chunks[i].Deserialize(binaryReader);
            }
        }
    }

    public async Task<Chunk> GetChunkAsync(Vector2Int localPosition, CancellationToken cancellationToken = default)
    {
        using (await rwlock.ReaderLockAsync(cancellationToken))
        {
            int index = LocalPositionToIndex(in localPosition);
            Chunk result = chunks[index];

            if (!initialized[index])
                await Task.Run(() =>
                {
                    Vector3Int worldPositionOffset = World.RegionToWorld(in position);
                    worldGenerator.Generate(position + localPosition, result);
                }, cancellationToken);

            return result;
        }
    }

    private static int LocalPositionToIndex(in Vector2Int localPosition)
    {
        Debug.Assert(0 <= localPosition.x && localPosition.x < WIDTH, $"x must be 0 <= x < {WIDTH}, x = {localPosition.x}");
        Debug.Assert(0 <= localPosition.y && localPosition.y < WIDTH, $"y must be 0 <= y < {WIDTH}, y = {localPosition.y}");

        return localPosition.x + WIDTH * localPosition.y;
    }

    private static Vector2Int IndexToLocalPosition(in int index)
    {
        Debug.Assert(0 <= index && index < WIDTH * WIDTH, $"index must be 0 <= index < {WIDTH * WIDTH}, index = {index}");

        return new(
            index % WIDTH,
            index / WIDTH % WIDTH
        );
    }
}