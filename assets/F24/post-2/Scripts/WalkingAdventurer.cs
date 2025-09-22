using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WalkingAdventurer : MonoBehaviour
{
    Adventurer adventurer;   //adventerer data 
    public List<Vector3> points;  // Array of points to follow
    public float speed = 1f;  // Speed of movement

    private int currentPointIndex = 0;
    private bool isMoving = false;

    public void StartPath(Adventurer adventurer, List<Vector3> points, float speed)
    {
        this.adventurer = adventurer;
        this.points = points;
        this.speed = speed;

        SetSprite();

        if (points.Count > 0)
        {
            isMoving = true;
            transform.position = points[0];
        }
        else
        {
            Debug.LogWarning("No points set for RouteFollower.");
        }
    }

    void Update()
    {
        if (isMoving && points.Count > 0)
        {
            MoveAlongRoute();
        }
    }



    //move adventerer character from point to point
    private void MoveAlongRoute()
    {
        Vector3 targetPoint = points[currentPointIndex];
        transform.position = Vector3.MoveTowards(transform.position, targetPoint, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPoint) < 0.01f)
        {
            currentPointIndex++;
            if (currentPointIndex >= points.Count)
            {
                isMoving = false;
                EndRoute();
            }
        }
    }

    private void EndRoute()
    {
        Destroy(gameObject);
    }


    public void SetSprite()
    {
        SpriteRenderer sr = gameObject.GetComponent<SpriteRenderer>();
        sr.sprite = adventurer.character.bodySprite;
    }
}
