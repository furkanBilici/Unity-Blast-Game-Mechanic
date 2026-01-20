using System.Collections.Generic;
using UnityEngine;

public class ObjectPooler : MonoBehaviour
{
    private Queue<Block> blockPool = new Queue<Block>();
    public Block blockPrefab;
    public Block GetBlockFromPool()
    {
        //Debug.Log("get block");
        if (blockPool.Count > 0)
        {
            Block b = blockPool.Dequeue();
            b.gameObject.SetActive(true);
            return b;
        }
        return Instantiate(blockPrefab);
    }
    public void ReturnToPool(Block b)
    {
        //Debug.Log("return pool");
        b.gameObject.SetActive(false);
        blockPool.Enqueue(b);
    }
}
