using System.Diagnostics.CodeAnalysis;

namespace VoxelWorld.Core;


public class Octree<T>
    where T : notnull
{
    private interface INode { }

    private sealed class LeafNode : INode
    {
        public readonly Vector3Int position;
        public readonly T item;


        public LeafNode(Vector3Int position, T item)
        {
            this.position = position;
            this.item = item;
        }
    }

    private sealed class InternalNode : INode
    {
        public readonly INode?[] children = new INode?[8];
        public readonly Vector3Int min, max, center;

        public InternalNode(Vector3Int min, Vector3Int max)
        {
            this.min = min;
            this.max = max;
            this.center = (max + min + Vector3Int.One) / 2;
        }
    }


    private readonly InternalNode root;
    private readonly ReaderWriterLockSlim rwlock;


    public Octree(Vector3Int min, Vector3Int max)
    {
        this.root = new InternalNode(min, max);
        this.rwlock = new ReaderWriterLockSlim();
    }


    public void Insert(Vector3Int position, T item)
    {
        rwlock.EnterWriteLock();

        try
        {
            InternalNode current = root;

            while (true)
            {
                int offset = GetOffsetFromPositionalRelationship(current.center, position);
                INode? child = current.children[offset];

                if (child is LeafNode childLeafNode)
                {
                    if (childLeafNode.position == position)
                        throw new ArgumentException($"Item with same key has already been added");

                    Vector3Int newChildSize = (current.max - current.min + Vector3Int.One) / 2; // 여기서 문제?
                    Vector3Int newChildMin = GetChildMinFromOffset(offset, current.min, newChildSize);

                    InternalNode newChildInternalNode = new InternalNode(newChildMin, newChildMin + newChildSize);
                    int childLeafOffset = GetOffsetFromPositionalRelationship(newChildInternalNode.center, childLeafNode.position);
                    newChildInternalNode.children[childLeafOffset] = childLeafNode;
                    current.children[offset] = newChildInternalNode;
                    current = newChildInternalNode;
                }
                else if (child is InternalNode childInternalNode)
                {
                    current = childInternalNode;
                }
                else
                {
                    current.children[offset] = new LeafNode(position, item);
                    break;
                }
            }

        }
        finally
        {
            rwlock.ExitWriteLock();
        }
    }

    public bool Remove(Vector3Int position)
    {
        rwlock.EnterWriteLock();
        try
        {
            return Remove(root, position);
        }
        finally
        {
            rwlock.ExitWriteLock();
        }
    }

    private bool Remove(InternalNode parent, Vector3Int position)
    {
        int offset = GetOffsetFromPositionalRelationship(parent.center, position);

        INode? child = parent.children[offset];

        if (child is InternalNode internalNode)
        {
            if (Remove(internalNode, position))
            {
                for (int i = 0; i < 8; i++)
                    if (internalNode.children[i] != null)
                        return true;

                // 자식이 하나도 없는 내부 노드를 제거
                parent.children[offset] = null;
                return true;
            }

            return false;
        }

        if (child is LeafNode leafNode)
        {
            parent.children[offset] = null;
            return true;
        }

        return false;
    }

    public void Preorder(Action<Vector3Int, T> action)
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

    private void Preorder(INode? node, Action<Vector3Int, T> action)
    {
        if (node == null)
            return;

        if (node is InternalNode internalNode)
        {
            for (int i = 0; i < 8; i++)
                Preorder(internalNode.children[i], action);

            return;
        }

        if (node is LeafNode leafNode)
        {
            action.Invoke(leafNode.position, leafNode.item);
            return;
        }
    }

    public bool TrySearch(Vector3Int position, [NotNullWhen(true)] out T? result)
    {
        rwlock.EnterReadLock();

        try
        {
            InternalNode current = root;

            while (true)
            {
                int offset = GetOffsetFromPositionalRelationship(current.center, position);
                var child = current.children[offset];

                if (child is InternalNode childInternalNode)
                {
                    current = childInternalNode;
                    continue;
                }
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

        }
        finally
        {
            rwlock.ExitReadLock();
        }

    }

    public bool BoundChecking(Vector3Int position)
    {
        return root.min.x <= position.x && position.x <= root.max.x &&
            root.min.y <= position.y && position.y <= root.max.y &&
            root.min.z <= position.z && position.z <= root.max.z;
    }

    private static Vector3Int GetChildMinFromOffset(int offset, Vector3Int parentMin, Vector3Int childSize)
    {
        Vector3Int childMin = parentMin;

        if ((offset & 1) != 0)
            childMin.x += childSize.x;
        if ((offset & 2) != 0)
            childMin.y += childSize.y;
        if ((offset & 4) != 0)
            childMin.z += childSize.z;

        return childMin;
    }

    private static int GetOffsetFromPositionalRelationship(Vector3Int start, Vector3Int end)
    {
        int offset = 0;

        if (start.x <= end.x)
            offset += 1;
        if (start.y <= end.y)
            offset += 2;
        if (start.z <= end.z)
            offset += 4;

        return offset;
    }
}