using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class Spawner : MonoBehaviour
{
    public GameObject Prototype;
    public float Radius = 1;
    public int Count = 10;
    public float SpawnRate = 1f;    //number of ships spawned per second

    private List<GameObject> index = new List<GameObject>();
    private Team team;
    private float lastSpawnTime = -1f;

    void Start()
    {
        
    }

    private void OnEnable()
    {
        team = GetComponent<Team>();
    }

    void Update()
    {
        MaintainCount();

    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, Radius);
    }

    void MaintainCount()
    {
        if (!Prototype) return;
        if (index.Count < Count && SpawnRate > 0)
        {
            if (Time.time - lastSpawnTime >= 1f / SpawnRate) SpawnPrototype();
        }
    }

    void SpawnPrototype()
    {
        if (!Prototype) return;
        Vector3 position = transform.position + Random.insideUnitSphere * Radius;
        Quaternion orientation = Random.rotation;
        GameObject obj = Instantiate(Prototype, position, orientation);
        index.Add(obj);
        Ship ship = obj.GetComponent<Ship>();
        if (ship && team)
        {
            ship.team = team;
            team.Ships.Add(ship);
        }
        lastSpawnTime = Time.time;
    }
}
