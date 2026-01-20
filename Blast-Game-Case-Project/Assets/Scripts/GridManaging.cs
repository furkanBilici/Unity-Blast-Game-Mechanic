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
    [SerializeField] private int _width = 5;
    [SerializeField] private int _height = 5;
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
    [HideInInspector] public int width => _width;
    [HideInInspector] public int height => _height;

    [SerializeField] public Node[,] gridArray;//private

    bool[,] visited;

    ObjectPooler pooler;
    MatchFinder matchFinder = new MatchFinder();
    ShuffleManager shuffleManager;
    UiManager uiManager;

    public bool isTouchLocked = false;

    private void Start()
    {
        visited = new bool[width, height];
        pooler = FindFirstObjectByType<ObjectPooler>();
        shuffleManager = FindFirstObjectByType<ShuffleManager>();
        uiManager = FindFirstObjectByType<UiManager>();
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
            Block clickedBlock = clickedNode.block;
            isTouchLocked = true;
            clickedBlock.spriteRenderer.sortingOrder = height+1;
            clickedBlock.transform.localScale*=1.2f;
            StartCoroutine(GatherBlocks(matches, clickedNode));
        }

    }
    IEnumerator GatherBlocks(List<Block> matches, Node clickedNode)
    {
        //Debug.Log("gather blocks");
        int point = 0;
        float duration = 0.2f;
        foreach (Block block in matches)
        {
            block.Move(clickedNode.transform.position);
        }
        yield return new WaitForSeconds(duration);
        clickedNode.block.transform.localScale /= 1.2f;
        foreach (Block block in matches)
        {
            Node node = block.currentNode;
            node.block = null;
            pooler.ReturnToPool(block);
            point++;
        }
        if(point<conditionA)
            uiManager.AddPoints(point);
        else if(point<conditionB)
            uiManager.AddPoints(point*2);
        else if(point<conditionC) uiManager.AddPoints(point*3);
        else uiManager.AddPoints(point*4);
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
        shuffleManager.CheckDeadlock();

        isTouchLocked = false;
    }

    public void UpdateAllVisuals()
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
}
   