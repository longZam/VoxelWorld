using System.Buffers;
using System.Diagnostics.CodeAnalysis;

namespace VoxelWorld.Core;


/// <summary>
/// <para>
/// 2차원 정수 공간 희소 쿼드 트리
/// </para>
/// <para>
/// <see cref="QuadTreeInteger{T}" />의 모든 공용 및 보호된 멤버는 스레드로부터 안전하며 여러 스레드에서 동시에 사용할 수 있습니다.
/// </para>
/// </summary>
/// <typeparam name="T"></typeparam>
public class QuadTreeInteger<T>
    where T : notnull
{
    private interface INode { }
    
    private sealed class LeafNode : INode
    {
        public readonly Vector2Int position;
        public readonly T item;

        
        public LeafNode(in Vector2Int position, T item)
        {
            this.position = position;
            this.item = item;
        }
    }

    private sealed class InternalNode : INode
    {
        public readonly INode?[] children;
        public readonly RectInt rect;

        
        public InternalNode(in RectInt rect)
        {
            this.children = ArrayPool<INode?>.Shared.Rent(4);
            this.rect = rect;
        }

        ~InternalNode()
        {
            ArrayPool<INode?>.Shared.Return(children, true);
        }
    }


    private readonly InternalNode root;
    private readonly ReaderWriterLockSlim rwlock;


    public QuadTreeInteger(in RectInt rect)
    {
        this.root = new InternalNode(in rect);
        this.rwlock = new ReaderWriterLockSlim();
    }

    public void Insert(in Vector2Int position, T item)
    {
        rwlock.EnterWriteLock();

        try
        {
            Insert(root, in position, item);
        }
        finally
        {
            rwlock.ExitWriteLock();
        }
    }

    private static void Insert(InternalNode current, in Vector2Int position, T item)
    {
        int offset = GetOffsetFromPositionalRelationship(current.rect.Center, in position);
        INode? child = current.children[offset];

        if (child is LeafNode childLeafNode)
        {
            if (childLeafNode.position == position)
                throw new ArgumentException($"Item with same key has already been added");

            Vector2Int newChildSize = (current.rect.max - current.rect.min + Vector2Int.One) / 2;
            Vector2Int newChildMin = GetChildMinFromOffset(offset, in current.rect.min, in newChildSize);
            RectInt rect = new(in newChildMin, newChildMin + newChildSize);
            InternalNode newChildInternalNode = new(in rect);
            int childLeafOffset = GetOffsetFromPositionalRelationship(newChildInternalNode.rect.Center, in childLeafNode.position);
            newChildInternalNode.children[childLeafOffset] = childLeafNode;
            current.children[offset] = newChildInternalNode;
            Insert(newChildInternalNode, in position, item);
        }
        else if (child is InternalNode childInternalNode)
        {
            Insert(childInternalNode, in position, item);
        }
        else
        {
            current.children[offset] = new LeafNode(in position, item);
        }
    }

    public bool Remove(in Vector2Int position)
    {
        rwlock.EnterWriteLock();

        try
        {
            return Remove(root, in position);
        }
        finally
        {
            rwlock.ExitWriteLock();
        }
    }

    private static bool Remove(InternalNode current, in Vector2Int position)
    {
        int offset = GetOffsetFromPositionalRelationship(current.rect.Center, in position);
        INode? child = current.children[offset];

        if (child is InternalNode childInternalNode)
        {
            if (Remove(childInternalNode, position))
            {
                for (int i = 0; i < 4; i++)
                    if (childInternalNode.children[i] != null)
                        return true;
                
                current.children[offset] = null;
                return true;
            }
        }
        else if (child is LeafNode)
        {
            current.children[offset] = null;
            return true;
        }

        return false;
    }

    public bool TrySearch(in Vector2Int position, [NotNullWhen(true)] out T? result)
    {
        rwlock.EnterReadLock();

        try
        {
            return TrySearch(root, in position, out result);
        }
        finally
        {
            rwlock.ExitReadLock();
        }
    }

    private static bool TrySearch(InternalNode current, in Vector2Int position, [NotNullWhen(true)] out T? result)
    {
        int offset = GetOffsetFromPositionalRelationship(current.rect.Center, in position);
        INode? child = current.children[offset];

        if (child is InternalNode childInternalNode)
            return TrySearch(childInternalNode, in position, out result);
        else if (child is LeafNode childLeafNode)
        {
            if (childLeafNode.position == position)
            {
                result = childLeafNode.item;
                return true;
            }
        }

        result = default;
        return false;
    }

    public void Preorder(Action<Vector2Int, T> action)
    {
        rwlock.EnterReadLock();

        try
        {
            Preorder(root, action);
        }
        finally
        {
            rwlock.ExitReadLock();
        }
    }

    private static void Preorder(InternalNode current, Action<Vector2Int, T> action)
    {
        for (int i = 0; i < 4; i++)
        {
            INode? child = current.children[i];

            if (child is InternalNode next)
                Preorder(next, action);
            else if (child is LeafNode childLeaf)
                action(childLeaf.position, childLeaf.item);
        }
    }

    public void OverlapCubeAll(in RectInt rect, Dictionary<Vector2Int, T> results)
    {
        rwlock.EnterReadLock();

        try
        {
            results.Clear();
            OverlapCubeAll(root, in rect, results);
        }
        finally
        {
            rwlock.ExitReadLock();
        }
    }

    private static void OverlapCubeAll(InternalNode current, in RectInt rect, Dictionary<Vector2Int, T> results)
    {
        if (!RectInt.Overlaps(in rect, in current.rect))
            return;
        
        for (int i = 0; i < 4; i++)
        {
            INode? child = current.children[i];

            if (child is InternalNode childInternalNode)
                OverlapCubeAll(childInternalNode, in rect, results);
            else if (child is LeafNode childLeafNode)
                if (RectInt.Overlaps(in rect, in childLeafNode.position))
                    results.Add(childLeafNode.position, childLeafNode.item);
        }
    }

    private static int GetOffsetFromPositionalRelationship(in Vector2Int start, in Vector2Int end)
    {
        int offset = 0;

        if (start.x <= end.x)
            offset |= 1;
        if (start.y <= end.y)
            offset |= 2;
        
        return offset;
    }

    private static Vector2Int GetChildMinFromOffset(int offset, in Vector2Int parentMin, in Vector2Int childSize)
    {
        Vector2Int childMin = parentMin;
        Vector2Int delta = new(
            (offset & 1) != 0 ? childSize.x : 0,
            (offset & 2) != 0 ? childSize.y : 0
        );

        return childMin + delta;
    }
}