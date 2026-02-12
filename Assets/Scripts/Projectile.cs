using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float Speed;      //meters per second
    public float TTL;        //time to live, seconds (0 = infinite)
    private float startTime;


    void Start()
    {
        startTime = Time.time;
    }

    void Update()
    {
        if (TTL > 0 && Time.time - startTime > TTL)
        {
            Destroy(gameObject);
        }

        transform.position += transform.forward * Speed * Time.deltaTime;

    }
}
