using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

public class Shake : MonoBehaviour
{
    public float target = 0;
    public float actual = 0;

    public float MaxAmplitude = 1;
    public float MaxFrequency = 1;

    public float targetDecay = .25f;    //how quickly target decays to 0
    public float actualRate = .5f;      //how quickly actual chases target

    private float tolerance = .01f;     //convergence threshhold - value snap

    void Start()
    {
        
    }

    void Update()
    {
        //decay target
        target = Mathf.MoveTowards(target, 0f, targetDecay * Time.deltaTime);
        target = Mathf.Clamp01(target);
        if (target <= tolerance) target = 0;

        //actual chases target
        actual = Mathf.MoveTowards(actual, target, actualRate * Time.deltaTime);
        if (Mathf.Abs(actual - target) <= tolerance) actual = target;

        //apply shake
        float frequency = MaxFrequency;        //for now this value does not change
        float amplitude = MaxAmplitude * actual;
        Vector3 vibration = NoiseVector(frequency, amplitude);
        vibration = Vector3.ClampMagnitude(vibration, amplitude);
        transform.localPosition = vibration;


    }


    /// <summary>
    /// Adds shake to the system
    /// </summary>
    /// <param name="amount"></param>
    public void Add(float amount)
    {
        amount = Mathf.Clamp01(amount);             //amount must be 0-1
        target = Mathf.Max(target, amount);         //apply to target, if higher than target
    }


    private Vector3 NoiseVector(float frequency, float amplitude)
    {
        float t = Time.time * frequency;

        float x = Mathf.PerlinNoise(t, 0.0f) * 2f - 1f;
        float y = Mathf.PerlinNoise(t, 10.0f) * 2f - 1f;
        float z = Mathf.PerlinNoise(t, 20.0f) * 2f - 1f;

        return new Vector3(x, y, z) * amplitude;
    }

}
