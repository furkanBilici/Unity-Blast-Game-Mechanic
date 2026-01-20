using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Block : MonoBehaviour
{
    Sprite[] sprites;//Default:0, A:1, B:2, C:3

    public SpriteRenderer spriteRenderer;
    public int typeid = 0;//0:blue, 1:green, 2:pink, 3:purple, 4:red, 5:yellow
    public Node currentNode;
    //private bool ismoving = false;
    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

    }
    public void Init(int typeId, Sprite[] icons)
    {
        this.typeid = typeId;
        this.sprites = icons;
        UpdateVisualState(0);
    }

    public void UpdateVisualState(int v)
    {
        if (sprites != null && v < sprites.Length)
        {
            spriteRenderer.sprite = sprites[v];
        }
    }

    public void Move(Vector2 target)
    {
        StopAllCoroutines();
        StartCoroutine(MovePosition(target));
    }
    IEnumerator MovePosition(Vector2 target)
    {
        //ismoving= true;
        float duration = 0.2f;
        float timer = 0f;
        Vector2 startPos = transform.position;
        while (timer < duration)
        {
            transform.position = Vector2.Lerp(startPos, target, timer / duration);
            yield return null;
            timer += Time.deltaTime;
        }
        transform.position = target;
        //ismoving = false;
    }
}
