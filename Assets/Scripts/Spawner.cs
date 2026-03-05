using System.Collections.Generic;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    public GameObject Prototype;
    public float Radius = 1;
    public int Count = 10;
    public float SpawnRate = 1f;    //number of ships spawned per second
    public int Inventory = 0;
    public Vector3 LaunchDirection = Vector3.zero;
    private Vector3 worldDir = Vector3.zero;


    private List<GameObject> index = new List<GameObject>();
    public Team team;
    private float lastSpawnTime = -1f;


    private void OnEnable()
    {
    }

    void Update()
    {
        worldDir = transform.TransformDirection(LaunchDirection).normalized;
        if (Inventory >0) MaintainCount();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, Radius);
        if (LaunchDirection != Vector3.zero)
        {
            Gizmos.color = Color.green;
            if (worldDir == Vector3.zero) worldDir = transform.TransformDirection(LaunchDirection).normalized;
            Gizmos.DrawLine(transform.position, transform.position + worldDir * 0.5f);
        }
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

        if (LaunchDirection.magnitude > 0)
        {
            orientation = Quaternion.LookRotation(worldDir);
        }

        GameObject obj = Instantiate(Prototype, position, orientation);
        index.Add(obj);
        Ship ship = obj.GetComponent<Ship>();
        ship.FreshSpawnCountdown = Game.I.ActivationCountdown;
        if (ship && team)
        {
            ship.team = team;
            team.Ships.Add(ship);
        }
        lastSpawnTime = Time.time;
        Inventory--;
    }
}
