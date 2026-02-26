using UnityEngine;

public class Recoil : MonoBehaviour
{
    public float recoilTime = .25f;
    public float recoverTime = .5f;
    public float displacement = .5f;
    public bool isRecoiling = false;
    public float Factor = 1;
    private float startTime;
    private Vector3 startPosition = Vector3.zero;

    public bool doFire;

    void Start()
    {
        startPosition = transform.localPosition;
        startTime = -recoverTime;  //initializes cycle

    }

    void Update()
    {
        if (isRecoiling)
        {
            Factor = (Time.time - startTime) / recoilTime;
            if (Factor >= 1)
            {
                isRecoiling = false;
                startTime = Time.time;
            }
            
        }
        else
        {
            Factor = 1 - (Time.time - startTime) / recoverTime;
        }
        Factor = Mathf.Clamp01(Factor);
        transform.localPosition = startPosition + Vector3.up * displacement * Factor;

        if (doFire)         {
            doFire = false;
            Fire();
        }

    }

    public void Fire()
    {
        isRecoiling = true;
        startTime = Time.time;
    }
}
