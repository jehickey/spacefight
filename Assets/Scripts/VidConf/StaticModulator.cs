using System.Collections;
using UnityEngine;

[System.Serializable]
public class StaticModulator : MonoBehaviour
{
    public float Output;     //how strong the current static level is

    public float BaselineSpeedMin = .1f;
    public float BaselineSpeedMax = .5f;
    public float BaselineAmpMin = .1f;
    public float BaselineAmpMax = .3f;

    public float baselineSpeed;
    public float baselineAmp;

    public float ReinitFrequency = 1;
    public float ReinitFrequencyMutation = .5f;
    public float lastInitTime = 0;
    public float initWait = 0;

    public float minSpikePeak;
    public float maxSpikePeak;
    public float minSpikeDuration;
    public float maxSpikeDuration;
    public float minSpikeInterval;
    public float maxSpikeInterval;

    float spikeValue;
    bool spikeActive;


    //public void Update()
    private void Update()
    {
        Init();
        Output = Mathf.PerlinNoise(Time.time * baselineSpeed, 0) * baselineAmp;
    }

    private void Init()
    {
        if (Time.time - lastInitTime > initWait)
        {
            baselineSpeed = Random.Range(BaselineSpeedMin, BaselineSpeedMax);
            baselineAmp = Random.Range(BaselineAmpMin, BaselineAmpMax);
            lastInitTime = Time.time;
            initWait = ReinitFrequency * (1+Random.Range(-ReinitFrequencyMutation, ReinitFrequencyMutation));
        }
    }

    IEnumerator SpikeRoutine()
    {
        while (true)
        {
            // Wait a random amount of time before next spike
            float wait = Random.Range(minSpikeInterval, maxSpikeInterval);
            yield return new WaitForSeconds(wait);
            
            yield return StartCoroutine(DoSpike());
        }
        
    }

    IEnumerator DoSpike()
    {
        spikeActive = true;

        float peak = Random.Range(minSpikePeak, maxSpikePeak); // e.g. 0.5–1.0
        float duration = Random.Range(minSpikeDuration, maxSpikeDuration); // e.g. 0.05–0.2 sec

        float t = 0;
        while (t < duration)
        {
            t += Time.deltaTime;
            float normalized = t / duration;

            // Fast attack, fast decay (triangle shape)
            spikeValue = peak * (1f - Mathf.Abs(normalized * 2f - 1f));

            yield return null;
        }

        spikeValue = 0;
        spikeActive = false;
    }


}
