using System.Collections.Generic;

public class MatchFinder
{
    public void FindMatches(int startx, int starty, int targetId, List<Block> result, bool[,] visited, Node[,] gridArray)//güncel fonk. stack overflow olmamasý için yaptýðým iteratif fonksiyon
    {
        Stack<(int x, int y)> stack = new Stack<(int x, int y)>();
        stack.Push((startx, starty));
        while (stack.Count > 0)
        {
            var (x, y) = stack.Pop();
            if (x < 0 || y < 0 || x >= gridArray.GetLength(0) || y >= gridArray.GetLength(1)) continue;
            if (visited[x, y]) continue;
            if (gridArray[x, y].block == null) continue;
            if (gridArray[x, y].block.typeid != targetId) continue;
            visited[x, y] = true;
            result.Add(gridArray[x, y].block);
            stack.Push((x + 1, y));
            stack.Push((x , y+1));
            stack.Push((x - 1, y));
            stack.Push((x, y-1));
        }
    }
    public void FindMatches2(int x, int y, int targetId, List<Block> result, bool[,] visited, Node[,] gridArray)//rekürsif ilk, eski fonksiyon
    {
        //Debug.Log("find matches");
        if (x < 0 || y < 0 || x >= gridArray.GetLength(0) || y >= gridArray.GetLength(1)) return;
        if (visited[x, y]) return;
        if (gridArray[x, y].block == null) return;
        if (gridArray[x, y].block.typeid != targetId) return;
        visited[x, y] = true;
        result.Add(gridArray[x, y].block);
        FindMatches(x + 1, y, targetId, result, visited, gridArray);
        FindMatches(x - 1, y, targetId, result, visited, gridArray);
        FindMatches(x, y + 1, targetId, result, visited, gridArray);
        FindMatches(x, y - 1, targetId, result, visited, gridArray);
    }
}
