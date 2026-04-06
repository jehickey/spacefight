using UnityEngine;

public class Projectile : MonoBehaviour
{
    public Color color = Color.white;
    public float Length;     //meters, in cases where it matters
    public float Speed;      //meters per second
    public float Damage;     //damage units done on impact
    public float TTL;        //time to live, seconds (0 = infinite)
    private float startTime;

    public Transform parentOrigin;

    private Material material;

    void Start()
    {
        startTime = Time.time;
        Renderer renderer = GetComponentInChildren<Renderer>();
        if (renderer)
        {
            material = renderer.material;
            float intensity = 15f;
            material.color = color * intensity;
            material.SetColor("_EmissionColor", color*intensity);
        }
    }


    void Update()
    {
        if (TTL > 0 && Time.time - startTime > TTL)
        {
            Destroy(gameObject);
        }

        transform.position += transform.forward * Speed * Simulation.I.SpeedUnit * Time.deltaTime;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.05f);

    }

    private void OnTriggerEnter(Collider other)
    {
        Destructable target = other.GetComponent<Destructable>();
        Vector3 pos = other.ClosestPoint(transform.position);
        if (other.transform == parentOrigin) return;
        Detonate(target, pos);
    }


    void Detonate(Destructable target, Vector3 position)
    {
        if (target) target.TakeDamage(Damage, parentOrigin);
        //Display an explosion
        Vector3 pos = position;
        Flare.Spawn(pos, Color.yellow, 0.25f, 0.25f, 0.0025f, 0.025f);
        //Destroy this projectile
        Destroy(gameObject);
    }

}
