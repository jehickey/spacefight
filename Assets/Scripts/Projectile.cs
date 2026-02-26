using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float Length;     //meters, in cases where it matters
    public float Speed;      //meters per second
    public float Damage;     //damage units done on impact
    public float TTL;        //time to live, seconds (0 = infinite)
    private float startTime;

    private Simulation sim;

    void Start()
    {
        startTime = Time.time;
        sim = FindFirstObjectByType<Simulation>();
    }


    void Update()
    {
        if (TTL > 0 && Time.time - startTime > TTL)
        {
            Destroy(gameObject);
        }

        transform.position += transform.forward * Speed * sim.SpeedUnit * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        Ship target = other.GetComponent<Ship>();
        Vector3 pos = other.ClosestPoint(transform.position);
        Detonate(target, pos);
    }


    void Detonate(Ship target, Vector3 position)
    {
        if (target)
        {
            target.TakeDamage(Damage);
        }
        Vector3 pos = position;
        Flare.Spawn(pos, Color.yellow, 0.25f, 0.25f, 0.0025f, 0.025f);
        Destroy(gameObject);
    }

}
