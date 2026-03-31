using System;
using UnityEngine;

public class Button : MonoBehaviour
{
    [Header("Button Settings")]
    public Vector3 ThrowDistance = new Vector3(0, .1f, 0);
    public float ThrowTime = 1;
    public float ReturnTime = 1;
    public float LightLevel = 0;

    [Header("Button Status")]
    [ReadOnly]
    public bool Pressing = false;
    [ReadOnly]
    public bool isThrowing = false;
    [ReadOnly]
    public bool isReturning = false;
    public bool doPress = false;

    public bool ReturnBlocked = false;

    public event Action Pressed;

    private float ThrowStartTime = 0;
    private float ReturnStartTime = 0;
    private Color LightColor = new Color();
    private Vector3 restPosition = new Vector3();

    private Material material;
    private Renderer render;
    private MaterialPropertyBlock light_mpb;
    


    void Start()
    {
        restPosition = transform.localPosition;
        render = GetComponent<Renderer>();
        material = render.material;
        light_mpb = new MaterialPropertyBlock();
        LightColor = material.color;
        material.EnableKeyword("_EMISSION");
    }

    void Update()
    {
        ManageLight();

        if (doPress)
        {
            Press();
            doPress = false;
        }

        if (!Pressing)
        {
            transform.localPosition = restPosition;
            isThrowing = false;
            isReturning = false;
            ThrowStartTime = 0;
            ReturnStartTime = 0;
            return;
        }
        Vector3 downPosition = restPosition + ThrowDistance;

        if (isThrowing)
        {
            if (ThrowStartTime == 0) ThrowStartTime = Time.time;
            //move towards down position
            float downProgress = Mathf.InverseLerp(0, ThrowTime, Time.time - ThrowStartTime);
            transform.localPosition = Vector3.Lerp(restPosition, downPosition, downProgress);
            isReturning = false;
            if (Time.time - ThrowStartTime > ThrowTime)
            {
                isThrowing = false;
                isReturning = true;
            }
        }

        if (isReturning && !ReturnBlocked)
        {
            if (ReturnStartTime == 0) ReturnStartTime = Time.time;
            float upProgress = Mathf.InverseLerp(0, ReturnTime, Time.time - ReturnStartTime);
            transform.localPosition = Vector3.Lerp(downPosition, restPosition, upProgress);
            if (Time.time - ReturnStartTime > ReturnTime)
            {
                isReturning = false;
                Pressing = false;
            }
        }


    }

    public void Press()
    {
        Pressed?.Invoke();
        Pressing = true;
        isThrowing = true;
        isReturning = false;
        ReturnBlocked = false;
    }


    private void ManageLight()
    {
        float currentLightLevel = LightLevel;
        if (LightLevel <= 0 || !Pressing) currentLightLevel = 0;

        render.GetPropertyBlock(light_mpb);
        material.SetColor("_EmissionColor", LightColor * currentLightLevel);
        render.SetPropertyBlock(light_mpb);
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other == null) return;
        Grabber grabber = other.GetComponent<Grabber>();
        if (grabber)
        {
            Press();
            ReturnBlocked = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        ReturnBlocked = false;
    }

}
