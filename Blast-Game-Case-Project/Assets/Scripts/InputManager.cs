using UnityEngine;
using UnityEngine.InputSystem;
public class InputManager : MonoBehaviour
{
    GridManaging gridMnaging;
    private void Start()
    {
        gridMnaging = FindFirstObjectByType<GridManaging>();
    }
    private void Update()
    {
        if (Pointer.current != null) 
        {
            if (Pointer.current.press.wasPressedThisFrame) 
            {
                Vector2 screenpos=Pointer.current.position.ReadValue();
                Vector2 worldpos=Camera.main.ScreenToWorldPoint(screenpos);

                RaycastHit2D hit = Physics2D.Raycast(worldpos,Vector2.zero);
                if (hit.collider != null)
                {
                    Node node = hit.collider.GetComponent<Node>();
                    if (node != null)
                    {
                        gridMnaging.ClickNode(node);
                    }
                }
            }
        }
    }
}
