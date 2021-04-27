using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlatformController : MonoBehaviour
{
    public Transform[] points;
    private int currentPoint = 0;

    public float speed = 1.0f;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if(Vector3.Distance(transform.position, points[currentPoint].position) < 0.5f)
        {
            //Code for moving onto next point
            if(currentPoint >= points.Length - 1)
            {
                currentPoint = 0;
            }
            else
            {
                currentPoint++;
            }
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, points[currentPoint].position, Time.deltaTime * speed);
        }
    }

}
