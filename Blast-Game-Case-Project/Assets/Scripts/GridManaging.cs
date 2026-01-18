using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct ColorData
{
    public string name;
    public Sprite[] sprites;
}
public class GridManaging : MonoBehaviour
{
    [Header("Grid ayarlarý")]
    [SerializeField] int width = 5;
    [SerializeField] int height = 5;
    [SerializeField] float spacing = 1;

    [Header("Prefablar")]
    public Node gridPrefab;
    public Block blockPrefab;
    public Transform boardCenter;

    [Header("Veri ve kural kýsmý")]
    public ColorData[] colorAssets;
    public int conditionA = 5;      
    public int conditionB = 7;
    public int conditionC = 9;

    [SerializeField] Node[,] gridArray;

    private Queue<Block> blockPool = new Queue<Block>();

    private bool isTouchLocked = false;

    private void Start()
    {
        Debug.Log("Start");
        GenerateGrid();
    }

    private void GenerateGrid()
    {
        Debug.Log("generate grid");
        gridArray = new Node[width, height];

        float startX = -((width * spacing) / 2.0f) + (spacing / 2);
        float startY = -((height * spacing) / 2.0f) + (spacing / 2);
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2 pos = new Vector2(startX + (x * spacing), startY + (y * spacing));
                Node newNode = Instantiate(gridPrefab, pos, Quaternion.identity);
                newNode.transform.SetParent(transform);
                newNode.x = x;
                newNode.y = y;
                newNode.name = $"Node {x},{y}";
                gridArray[x, y] = newNode;

                SpawnBlockAt(x, y, pos);
            }
        }
        UpdateAllVisuals(); 
    }
    public void GenerateNewGrid()
    {
        StopAllCoroutines();
        CleanGrid();
        RefillGrid();
        UpdateAllVisuals();
        isTouchLocked = false;
    }
    private void CleanGrid()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (gridArray[x, y].block != null && gridArray[x, y] != null)
                {
                    ReturnToPool(gridArray[x, y].block);
                    gridArray[x, y].block = null;
                }
                    
            }
        }

    }
    private void RefillGrid()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2 pos = gridArray[x, y].transform.position;
                SpawnBlockAt(x,y, pos); 
            }
        }
    }
    Block GetBlockFromPool()
    {
        Debug.Log("get block");
        if (blockPool.Count > 0)
        {
            Block b = blockPool.Dequeue();
            b.gameObject.SetActive(true);
            return b;
        }
        return Instantiate(blockPrefab);
    }
    void ReturnToPool(Block b)
    {
        Debug.Log("return pool");
        b.gameObject.SetActive(false);
        blockPool.Enqueue(b);
    }
    
    void SpawnBlockAt(int x, int y, Vector2 pos) 
    {
        Debug.Log("spawn block");
        Block b = GetBlockFromPool();
        b.transform.position = pos;
        b.transform.localPosition = pos;
        int randomColor = Random.Range(0, colorAssets.Length);
        b.Init(randomColor,colorAssets[randomColor].sprites);
        gridArray[x, y].SetBlock(b);
    }
    public void ClickNode(Node clickedNode)
    {
        Debug.Log("click node");
        if (isTouchLocked || clickedNode.block == null) return;
        List<Block> matches = new List<Block>();
        bool[,] visited = new bool[width, height];
        FindMatches(clickedNode.x, clickedNode.y, clickedNode.block.typeid, matches, visited);
        if (matches.Count >= 2)
        {
            foreach (Block block in matches)
            {
                Node node = block.currentNode;
                node.block = null;
                ReturnToPool(block);
            }
            StartCoroutine(ApplyGravityRoutine());
        }
    }
    
    IEnumerator ApplyGravityRoutine()
    {
        Debug.Log("apply gravity");
        isTouchLocked = true;
        yield return new WaitForSeconds(0.1f);
        for (int x = 0; x < width; x++)
        {
            int emptyCount = 0;
            for (int y = 0; y < height; y++)
            {
                if (gridArray[x, y].block == null)
                {
                    emptyCount++;
                }
                else if (emptyCount > 0)
                {
                    Block b = gridArray[x, y].block;
                    gridArray[x, y].block = null;
                    int targety = y - emptyCount;
                    gridArray[x, targety].SetBlock(b);
                    b.Move(gridArray[x, targety].transform.position);
                }
            }
            for(int i=1; i<=emptyCount; i++)
            {
                int targety = height - i;
                Vector3 spawnPos = gridArray[x, targety].transform.position;
                spawnPos.y += 5f;
                SpawnBlockAt(x,targety,spawnPos);
                gridArray[x,targety].block.Move(gridArray[x, targety].transform.position);
                
            }
            //Debug.Log(emptyCount);
        }
        yield return new WaitForSeconds(0.4f);
        UpdateAllVisuals();
        CheckDeadlock();
        
        isTouchLocked = false;
    }
    void FindMatches(int x, int y, int targetId, List<Block> result, bool[,] visited)
    {
        //Debug.Log("find matches");
        if (x < 0 || y < 0 || x >= width || y >= height) return;
        if (visited[x, y]) return;
        if (gridArray[x, y].block == null) return;
        if (gridArray[x, y].block.typeid != targetId) return;
        visited[x, y] = true;
        result.Add(gridArray[x, y].block);
        FindMatches(x + 1, y, targetId, result, visited);
        FindMatches(x - 1, y, targetId, result, visited);
        FindMatches(x, y + 1, targetId, result, visited);
        FindMatches(x, y - 1, targetId, result, visited);
    }
    private void UpdateAllVisuals()
    {
        //Debug.Log("update visuals");
        bool[,] visited = new bool[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if(!visited[x, y] && gridArray[x, y].block != null)
                {
                    List<Block> group = new List<Block>();
                    FindMatches(x, y, gridArray[x,y].block.typeid , group, visited);
                    int state = 0;
                    if (group.Count >= conditionC) state = 3;
                    else if (group.Count >= conditionB) state = 2;
                    else if (group.Count >= conditionA) state = 1;
                    foreach (Block block in group)
                    {
                        block.UpdateVisualState(state);
                    }
                }
            }
        }
    }
    void CheckDeadlock()
    {
        Debug.Log("deadlock check");
        bool hasmove = false;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
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
            Debug.Log("deadlock sorunu");
            StartCoroutine(FixBoardRoutine());
        }
    }
    IEnumerator FixBoardRoutine()
    {
        Debug.Log("fix board");
        isTouchLocked = true;
            yield return new WaitForSeconds(0.5f);

            List<Block> allBlocks = new List<Block>();
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (gridArray[x, y].block != null)
                        allBlocks.Add(gridArray[x, y].block);
                }
            }

            for (int i = 0; i < allBlocks.Count; i++)
            {
                Block temp = allBlocks[i];
                int rnd = Random.Range(i, allBlocks.Count);
                allBlocks[i] = allBlocks[rnd];
                allBlocks[rnd] = temp;
            }

            int matchSize = Random.Range(2, 5);
            if (allBlocks.Count < matchSize) matchSize = allBlocks.Count;

            int targetColor = allBlocks[0].typeid;

            List<Block> vipBlocks = new List<Block>();

            for (int i = 0; i < matchSize; i++)
            {
                Block b = allBlocks[0]; 
                allBlocks.RemoveAt(0);

                b.Init(targetColor, colorAssets[targetColor].sprites);
                vipBlocks.Add(b);
            }

            List<Vector2Int> targetCoords = GetRandomConnectedCoordinates(matchSize);

            for (int i = 0; i < vipBlocks.Count; i++)
            {
                Vector2Int coord = targetCoords[i];
                PlaceBlockAt(vipBlocks[i], coord.x, coord.y);
            }

            int currentBlockIndex = 0;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (targetCoords.Contains(new Vector2Int(x, y))) continue;

                    if (currentBlockIndex < allBlocks.Count)
                    {
                        PlaceBlockAt(allBlocks[currentBlockIndex], x, y);
                        currentBlockIndex++;
                    }
                }
            }

            yield return new WaitForSeconds(0.5f);
            UpdateAllVisuals();
            isTouchLocked = false;
        }
    void PlaceBlockAt(Block b, int x, int y)
    {
        Debug.Log("place block");

        gridArray[x, y].SetBlock(b);
        b.Move(gridArray[x, y].transform.position);
    }

    List<Vector2Int> GetRandomConnectedCoordinates(int count)
    {
        Debug.Log("get random coordinate");

        List<Vector2Int> result = new List<Vector2Int>();

            int startX = Random.Range(0, width);
            int startY = Random.Range(0, height);
            result.Add(new Vector2Int(startX, startY));

            while (result.Count < count)
            {
                Vector2Int current = result[Random.Range(0, result.Count)];

                List<Vector2Int> neighbors = new List<Vector2Int>();

                Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

                foreach (var dir in dirs)
                {
                    Vector2Int next = current + dir;

                    if (next.x >= 0 && next.x < width && next.y >= 0 && next.y < height)
                    {
                        if (!result.Contains(next))
                        {
                            neighbors.Add(next);
                        }
                    }
                }

                if (neighbors.Count > 0)
                {
                    result.Add(neighbors[Random.Range(0, neighbors.Count)]);
                }
                else
                {
                    // nadir durum, etrafý dolu nokta. Döngü baþa döner, listedeki baþka bir noktadan dal vermeyi dener
                }
            }

            return result;
        }
    }
