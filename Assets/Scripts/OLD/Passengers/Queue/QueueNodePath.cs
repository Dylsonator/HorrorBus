using System.Collections.Generic;
using UnityEngine;

public sealed class QueueNodePath : MonoBehaviour
{
    [Tooltip("Auto-built from child transforms (0 = door/front).")]
    [SerializeField] private List<Transform> nodes = new();

    public IReadOnlyList<Transform> Nodes => nodes;
    public int Count => nodes != null ? nodes.Count : 0;

    private void Awake() => RebuildFromChildren();
    private void OnValidate() => RebuildFromChildren();

    private void RebuildFromChildren()
    {
        if (nodes == null) nodes = new List<Transform>();
        nodes.Clear();

        for (int i = 0; i < transform.childCount; i++)
            nodes.Add(transform.GetChild(i));
    }

    public Transform GetNode(int index)
    {
        if (nodes == null || nodes.Count == 0) return null;
        index = Mathf.Clamp(index, 0, nodes.Count - 1);
        return nodes[index];
    }
}
