using System.Buffers;
using System.Diagnostics;
using Nito.AsyncEx;
using VoxelWorld.Core.Proto;

namespace VoxelWorld.Core;


/// <summary>
/// <para>
/// (16, 16, 512) 블록 크기의 데이터를 관리하는 클래스
/// </para>
/// <para>
/// Chunk의 모든 공용 및 보호된 멤버는 스레드로부터 안전하며 여러 스레드에서 동시에 사용할 수 있습니다.
/// </para>
/// </summary>
public class Chunk
{
    /// <summary>
    /// 한 청크 당 X축, Y축 블록 개수
    /// </summary>
    public const int WIDTH = 16;
    /// <summary>
    /// 한 청크 당 Z축 블록 개수
    /// </summary>
    public const int HEIGHT = 512;
    /// <summary>
    /// 한 청크 당 밑면의 블록 개수
    /// </summary>
    public const int BOTTOM = WIDTH * WIDTH;
    /// <summary>
    /// 한 청크 당 블록 개수
    /// </summary>
    public const int VOLUME = BOTTOM * HEIGHT;

    private readonly ushort[] rawData;
    private readonly AsyncReaderWriterLock rwlock;


    /// <summary>
    /// <para>
    /// 해당 LocalPosition의 블럭을 읽고 쓸 수 있습니다.
    /// </para>
    /// <para>
    /// 스레드로부터 안전하기 위해 매 접근마다 lock을 사용합니다.
    /// 병렬로 동시에 읽거나 쓰려면 ParallelRead, ParallelWrite 함수의 사용을 고려하십시오.
    /// </para>
    /// </summary>
    public ushort this[in Vector3Int localPosition]
    {
        get
        {
            using (rwlock.ReaderLock())
            {
                return rawData[LocalPositionToIndex(localPosition)];
            }
        }
        set
        {
            using (rwlock.WriterLock())
            {
                rawData[LocalPositionToIndex(localPosition)] = value;
            }
        }
    }

    public Chunk()
    {
        this.rawData = ArrayPool<ushort>.Shared.Rent(VOLUME);
        this.rwlock = new();

        Array.Clear(this.rawData, 0, VOLUME);
    }

    ~Chunk()
    {
        ArrayPool<ushort>.Shared.Return(this.rawData);
    }

    public void Serialize(BinaryWriter binaryWriter)
    {
        using (rwlock.ReaderLock())
        {
            for (int i = 0; i < VOLUME; i++)
                binaryWriter.Write(rawData[i]);
        }
    }

    public void Deserialize(BinaryReader binaryReader)
    {
        using (rwlock.WriterLock())
        {
            for (int i = 0; i < VOLUME; i++)
                rawData[i] = binaryReader.ReadUInt16();
        }
    }

    public void Serialize(LoadChunkResponse response)
    {
        using (rwlock.ReaderLock())
        {
            for (int i = 0; i < VOLUME; i++)
                response.RawData.Add(rawData[i]);
        }
    }

    public void Deserialize(LoadChunkResponse response)
    {
        using (rwlock.WriterLock())
        {
            for (int i = 0; i < VOLUME; i++)
                rawData[i] = (ushort)response.RawData[i];
        }
    }

    public void CopyRawData(Span<ushort> destination)
    {
        if (destination.Length != VOLUME)
            throw new ArgumentException($"Buffer size must be exactly {VOLUME}.", nameof(destination));
        
        using (rwlock.ReaderLock())
        {
            rawData.CopyTo(destination);
        }
    }

    public delegate void ParallelReadAction(in Vector3Int localPosition, ushort block);
    public delegate ushort ParallelWriteAction(in Vector3Int localPosition, ushort previousBlock);

    /// <summary>
    /// 청크 내 모든 블럭에 읽기 작업을 병렬로 수행합니다.
    /// </summary>
    /// <param name="action"></param>
    public void ParallelRead(ParallelReadAction action)
    {
        using (rwlock.ReaderLock())
        {
            Parallel.For(0, VOLUME, i =>
            {
                action(IndexToLocalPosition(i), rawData[i]);
            });
        }
    }

    /// <summary>
    /// 청크 내 모든 블럭에 쓰기 작업을 병렬로 수행합니다.
    /// </summary>
    /// <param name="action"></param>
    public void ParallelWrite(ParallelWriteAction action)
    {
        using (rwlock.WriterLock())
        {
            Parallel.For(0, VOLUME, i =>
            {
                rawData[i] = action(IndexToLocalPosition(i), rawData[i]);
            });
        }
    }

    private static int LocalPositionToIndex(in Vector3Int localPosition)
    {
        Debug.Assert(0 <= localPosition.x && localPosition.x < WIDTH, $"x must be 0 <= x < {WIDTH}, x = {localPosition.x}");
        Debug.Assert(0 <= localPosition.y && localPosition.y < WIDTH, $"y must be 0 <= y < {WIDTH}, y = {localPosition.y}");
        Debug.Assert(0 <= localPosition.z && localPosition.z < HEIGHT, $"z must be 0 <= z < {HEIGHT}, z = {localPosition.z}");

        return localPosition.x + WIDTH * localPosition.y + BOTTOM * localPosition.z;
    }

    private static Vector3Int IndexToLocalPosition(int index)
    {
        Debug.Assert(0 <= index && index < VOLUME, $"index must be 0 <= index < {VOLUME}, index = {index}");

        return new(
            index % WIDTH,
            index / WIDTH % WIDTH,
            index / BOTTOM % HEIGHT
        );
    }
}