using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class ShuffleManager : MonoBehaviour
{
    GridManaging gridmanaging;
    private void Start()
    {
        gridmanaging=FindFirstObjectByType<GridManaging>();
    }
    public void CheckDeadlock()
    {
        //Debug.Log("deadlock check");
        bool hasmove = false;
        Node[,] gridArray = gridmanaging.gridArray;
        int width= gridmanaging.width;
        int height= gridmanaging.height;
        for (int x = 0; x <gridmanaging.width; x++)
        {
            for (int y = 0; y <gridmanaging. height; y++)
            {
                if (gridArray[x, y].block == null) continue;
                int id = gridArray[x, y].block.typeid;
                if (x + 1 < width && gridArray[x + 1, y].block != null &&
                    id == gridArray[x + 1, y].block.typeid)
                    hasmove = true;
                if (x - 1 >= 0 && gridArray[x - 1, y].block != null &&
                    id == gridArray[x - 1, y].block.typeid)
                    hasmove = true;
                if (y + 1 < height && gridArray[x, y + 1].block != null &&
                    id == gridArray[x, y + 1].block.typeid)
                    hasmove = true;
                if (y - 1 >= 0 && gridArray[x, y - 1].block != null &&
                    id == gridArray[x, y - 1].block.typeid)
                    hasmove = true;
                if (hasmove) break;
            }
            if (hasmove) break;
        }
        if (!hasmove)
        {
            // Debug.Log("deadlock sorunu");
            StartCoroutine(FixBoardRoutine());
        }
    }
    List<Block> GetAllBlocks()
    {
        Node[,] gridArray = gridmanaging.gridArray;
        int width = gridmanaging.width;
        int height = gridmanaging.height;
        List<Block> list = new List<Block>();

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                if (gridArray[x, y].block != null)
                    list.Add(gridArray[x, y].block);

        return list;
    }
    void ShuffleBlocks(List<Block> blocks)
    {
        for (int i = 0; i < blocks.Count; i++)
        {
            Block temp = blocks[i];
            int rnd = Random.Range(i, blocks.Count);
            blocks[i] = blocks[rnd];
            blocks[rnd] = temp;
        }
    }
    List<Block> CreateGuaranteedMatch(List<Block> blocks, int size)
    {
        
        List<Block> result = new List<Block>();
        int color = blocks[0].typeid;

        for (int i = 0; i < size; i++)
        {
            Block b = blocks[0];
            blocks.RemoveAt(0);
            b.Init(color, gridmanaging.colorAssets[color].sprites);
            result.Add(b);
        }
        return result;
    }
    IEnumerator FixBoardRoutine()
    {
        Node[,] gridArray = gridmanaging.gridArray;
        int width = gridmanaging.width;
        int height = gridmanaging.height;
        gridmanaging.isTouchLocked = true;
        yield return new WaitForSeconds(0.5f);

        List<Block> allBlocks = GetAllBlocks();
        ShuffleBlocks(allBlocks);

        int matchSize = Mathf.Min(Random.Range(2, 5), allBlocks.Count);

        List<Block> vipBlocks = CreateGuaranteedMatch(allBlocks, matchSize);
        List<Vector2Int> targetCoords = GetRandomConnectedCoordinates(matchSize);

        for (int i = 0; i < vipBlocks.Count; i++)
        {
            Vector2Int c = targetCoords[i];
            PlaceBlockAt(vipBlocks[i], c.x, c.y);
        }

        HashSet<Vector2Int> reserved = new HashSet<Vector2Int>(targetCoords);

        int index = 0;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (reserved.Contains(new Vector2Int(x, y))) continue;
                if (index >= allBlocks.Count) break;

                PlaceBlockAt(allBlocks[index], x, y);
                index++;
            }
        }

        yield return new WaitForSeconds(0.5f);
        gridmanaging. UpdateAllVisuals();
        gridmanaging.isTouchLocked = false;
    }

    void PlaceBlockAt(Block b, int x, int y)
    {
        //Debug.Log("place block");
        Node[,] gridArray = gridmanaging.gridArray;
        int width = gridmanaging.width;
        int height = gridmanaging.height;
        gridArray[x, y].SetBlock(b);
        b.Move(gridArray[x, y].transform.position);
    }

    List<Vector2Int> GetRandomConnectedCoordinates(int count)
    {
        HashSet<Vector2Int> result = new HashSet<Vector2Int>();
        int width = gridmanaging.width;
        int height = gridmanaging.height;
        Vector2Int start = new Vector2Int(
            Random.Range(0, width),
            Random.Range(0, height)
        );

        result.Add(start);

        Vector2Int[] dirs =
        {
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.left,
        Vector2Int.right
    };

        while (result.Count < count)
        {
            Vector2Int current = result.ElementAt(Random.Range(0, result.Count));

            List<Vector2Int> neighbors = new List<Vector2Int>();

            foreach (var dir in dirs)
            {
                Vector2Int next = current + dir;

                if (next.x >= 0 && next.x < width &&
                    next.y >= 0 && next.y < height &&
                    !result.Contains(next))
                {
                    neighbors.Add(next);
                }
            }

            if (neighbors.Count > 0)
                result.Add(neighbors[Random.Range(0, neighbors.Count)]);
        }

        return new List<Vector2Int>(result);
    }
}

