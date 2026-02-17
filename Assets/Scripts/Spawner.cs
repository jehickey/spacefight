using System.Collections.Generic;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    public GameObject Prototype;
    public float Radius = 1;
    public int Count = 10;

    private List<GameObject> index = new List<GameObject>();

    void Start()
    {
        
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
        while (index.Count < Count)
        {
            int startingCount = index.Count;
            SpawnPrototype();
            if (index.Count == startingCount) break;
        }
    }

    void SpawnPrototype()
    {
        if (!Prototype) return;
        Vector3 position = transform.position + Random.insideUnitSphere * Radius;
        Quaternion orientation = Random.rotation;
        GameObject obj = Instantiate(Prototype, position, orientation);
        index.Add(obj);
    }
}
