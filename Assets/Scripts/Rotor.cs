using UnityEngine;

public class Rotor : MonoBehaviour
{
    public float Speed = .25f;       //revolutions per second


    void Start()
    {
        
    }

    void Update()
    {
        transform.Rotate(0f, Speed * 360f * Time.deltaTime, 0f, Space.Self);

    }
}
