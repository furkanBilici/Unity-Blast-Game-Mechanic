using TMPro;
using UnityEngine;

public class UiManager : MonoBehaviour
{
    GridManaging gridManager;
    [SerializeField] TextMeshProUGUI points;
    private int point = 0;//normalde ayrý olarak baþka managerde tutulmalý
    private void Start()
    {
        gridManager=FindFirstObjectByType<GridManaging>();
        UpdateText();
    }
    public void GenerateNewGrid()
    {
        gridManager.GenerateNewGrid();
        CleanPoints();
    }
    public void CleanPoints()
    {
        point = 0;
        UpdateText();
    }
    public void AddPoints(int addpoint)
    {
        point += addpoint;
        UpdateText();
    }
    public void UpdateText()
    {
        points.text=point.ToString();
    }
}
