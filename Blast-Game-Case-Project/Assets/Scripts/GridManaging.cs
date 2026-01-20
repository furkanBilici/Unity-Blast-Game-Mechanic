using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    //public Block blockPrefab;
    public Transform boardCenter;

    [Header("Veri ve kural kýsmý")]
    public ColorData[] colorAssets;
    public int conditionA = 5;
    public int conditionB = 7;
    public int conditionC = 9;

    [SerializeField] Node[,] gridArray;

    bool[,] visited;

    ObjectPooler pooler;
    MatchFinder matchFinder = new MatchFinder();

    private bool isTouchLocked = false;

    private void Start()
    {
        visited = new bool[width, height];
        pooler = FindFirstObjectByType<ObjectPooler>();
        //Debug.Log("Start");
        GenerateGrid();

    }

    private void GenerateGrid()
    {
        //Debug.Log("generate grid");
        gridArray = new Node[width, height];

        float startX = -((width * spacing) / 2.0f) + (spacing / 2) + boardCenter.transform.position.x;
        float startY = -((height * spacing) / 2.0f) + (spacing / 2) + boardCenter.transform.position.y;
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
                    pooler.ReturnToPool(gridArray[x, y].block);
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
                SpawnBlockAt(x, y, pos);
            }
        }
    }

    void SpawnBlockAt(int x, int y, Vector2 pos)
    {
        //Debug.Log("spawn block");
        Block b = pooler.GetBlockFromPool();
        b.transform.position = pos;
        b.transform.localPosition = pos;
        int randomColor = Random.Range(0, colorAssets.Length);
        b.Init(randomColor, colorAssets[randomColor].sprites);
        gridArray[x, y].SetBlock(b);
    }
    public void ClickNode(Node clickedNode)
    {
        //Debug.Log("click node");
        if (isTouchLocked || clickedNode.block == null) return;
        List<Block> matches = new List<Block>();
        //bool[,] visited = new bool[width, height]; -- yeni açmak yerine temizlenip tkrar kullanýlýyor
        System.Array.Clear(visited, 0, visited.Length);
        matchFinder.FindMatches(clickedNode.x, clickedNode.y, clickedNode.block.typeid, matches, visited, gridArray);
        if (matches.Count >= 2)
        {
            isTouchLocked = true;
            StartCoroutine(GatherBlocks(matches, clickedNode));
        }

    }
    IEnumerator GatherBlocks(List<Block> matches, Node clickedNode)
    {
        //Debug.Log("gather blocks");
        float duration = 0.2f;
        foreach (Block block in matches)
        {
            block.Move(clickedNode.transform.position);
        }
        yield return new WaitForSeconds(duration);
        foreach (Block block in matches)
        {
            Node node = block.currentNode;
            node.block = null;
            pooler.ReturnToPool(block);
        }
        StartCoroutine(ApplyGravityRoutine());
    }

    IEnumerator ApplyGravityRoutine()
    {
        //Debug.Log("apply gravity");
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
            for (int i = 1; i <= emptyCount; i++)
            {
                int targety = height - i;
                Vector3 spawnPos = gridArray[x, targety].transform.position;
                spawnPos.y += 5f;
                SpawnBlockAt(x, targety, spawnPos);
                gridArray[x, targety].block.Move(gridArray[x, targety].transform.position);

            }
            //Debug.Log(emptyCount);
        }
        yield return new WaitForSeconds(0.4f);
        UpdateAllVisuals();
        CheckDeadlock();

        isTouchLocked = false;
    }

    private void UpdateAllVisuals()
    {
        //Debug.Log("update visuals");
        //bool[,] visited = new bool[width, height];
        System.Array.Clear(visited, 0, visited.Length);
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (!visited[x, y] && gridArray[x, y].block != null)
                {
                    List<Block> group = new List<Block>();
                    matchFinder.FindMatches(x, y, gridArray[x, y].block.typeid, group, visited, gridArray);
                    int state = 0;
                    if (group.Count > conditionC) state = 3;
                    else if (group.Count > conditionB) state = 2;
                    else if (group.Count > conditionA) state = 1;
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
        //Debug.Log("deadlock check");
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
            // Debug.Log("deadlock sorunu");
            StartCoroutine(FixBoardRoutine());
        }
    }
    List<Block> GetAllBlocks()
    {
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
            b.Init(color, colorAssets[color].sprites);
            result.Add(b);
        }
        return result;
    }

    IEnumerator FixBoardRoutine()
    {
        isTouchLocked = true;
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
        UpdateAllVisuals();
        isTouchLocked = false;
    }

    void PlaceBlockAt(Block b, int x, int y)
    {
        //Debug.Log("place block");

        gridArray[x, y].SetBlock(b);
        b.Move(gridArray[x, y].transform.position);
    }

    List<Vector2Int> GetRandomConnectedCoordinates(int count)
    {
        HashSet<Vector2Int> result = new HashSet<Vector2Int>();

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