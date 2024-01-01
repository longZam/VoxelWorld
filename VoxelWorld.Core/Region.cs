using System.Buffers;
using System.Diagnostics;

namespace VoxelWorld.Core;


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
    /// <summary>
    /// 한 Region 당 X축, Y축 청크 개수
    /// </summary>
    public const int WIDTH = 64;


    private readonly Chunk[] chunks;
    private readonly bool[] initialized;
    private readonly ReaderWriterLockSlim rwlock;


    /// <summary>
    /// 해당 LocalPosition의 Chunk를 읽습니다.
    /// </summary>
    public Chunk this[in Vector2Int localPosition]
    {
        get
        {
            rwlock.EnterReadLock();

            try
            {
                return chunks[LocalPositionToIndex(in localPosition)];
            }
            finally
            {
                rwlock.ExitReadLock();
            }
        }
    }

    public Region(bool clear = false)
    {
        this.chunks = ArrayPool<Chunk>.Shared.Rent(WIDTH * WIDTH);
        this.initialized = ArrayPool<bool>.Shared.Rent(WIDTH * WIDTH);
        this.rwlock = new();

        for (int i = 0; i < WIDTH * WIDTH; i++)
            this.chunks[i] = new Chunk(clear);
        
        if (clear)
            Array.Fill(this.initialized, false);
    }

    ~Region()
    {
        ArrayPool<Chunk>.Shared.Return(chunks, true);
        ArrayPool<bool>.Shared.Return(initialized, false);
    }

    public void Serialize(BinaryWriter binaryWriter)
    {
        rwlock.EnterReadLock();

        try
        {   
            for (int i = 0; i < WIDTH * WIDTH; i++)
            {
                binaryWriter.Write(initialized[i]);

                if (initialized[i])
                    chunks[i].Serialize(binaryWriter);
            }
        }
        finally
        {
            rwlock.ExitReadLock();
        }
    }

    public void Deserialize(BinaryReader binaryReader)
    {
        rwlock.EnterWriteLock();

        try
        {
            for (int i = 0; i < WIDTH * WIDTH; i++)
            {
                initialized[i] = binaryReader.ReadBoolean();

                if (initialized[i])
                    chunks[i].Deserialize(binaryReader);
            }
        }
        finally
        {
            rwlock.ExitWriteLock();
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