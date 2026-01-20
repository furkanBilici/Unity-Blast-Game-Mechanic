using UnityEngine;

public class Node : MonoBehaviour
{
    public Block block;
    public int x, y;
    public void SetBlock(Block newBlock)
    {
        block = newBlock;
        if (newBlock == null) return;
        newBlock.spriteRenderer.sortingOrder = y;
        newBlock.currentNode = this;
        newBlock.name = $"Block {x},{y}";
    }
}
