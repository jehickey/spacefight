using UnityEngine;
using UnityEngine.InputSystem;

public class CameraCursor : MonoBehaviour
{
    public float Distance = 1.0f;
    public float Radii = 1.0f;

    public float MinRadii = 1.01f;
    public float MaxRadii = 3f;
    public float StartingRadii;

    public float ZoomFactor = .1f;
    public float RotationFactor = 1.0f;

    public float MinDistance;
    public float MaxDistance;

    public float RotateX;
    public float RotateY;

    public float lightRotateX;
    public float lightRotateY;

    public float camRotateX;
    public float camRotateY;

    public bool OrbitMode;
    public bool LightMode;
    public bool AngleMode;

    public bool ShowPoles = true;
    public float PoleThickness = .01f;
    public float PoleBuffer = .1f;
    public Transform tPoles;
    public Transform tEquator;


    Body body;
    PlanetTestControls controls;
    new Camera camera;
    new Light light;

    void OnEnable()
    {
        controls = new PlanetTestControls();
        controls.Enable();
        body = FindFirstObjectByType<Body>();
        light = GetComponentInChildren<Light>();
        camera = GetComponentInChildren<Camera>();
        if (body)
        {
            Distance = body.Radius * StartingRadii;
        }
    }

    private void OnDisable()
    {
        controls?.Disable();
    }

    void Update()
    {
        if (!camera || !body || !light)
        {
            Debug.Log("Missing critical components");
            return;
        }
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        //mouse inputs
        float mouseX = controls.Controls.RotateX.ReadValue<float>();
        float mouseY = controls.Controls.RotateY.ReadValue<float>();
        float zoom = controls.Controls.Zoom.ReadValue<float>();
        //modes
        LightMode = controls.Controls.MoveLightMode.IsPressed();
        AngleMode = controls.Controls.AngleCameraMode.IsPressed();
        OrbitMode = (!LightMode && !AngleMode);

        //establish distance limits and current radii
        MinDistance = body.Radius * MinRadii;
        MaxDistance = body.Radius * MaxRadii;
        float t = Mathf.InverseLerp(1, MaxRadii, Radii);
        float slowdown = t * t * (3f - 2f * t);     //or Mathf.Pow(t, 2.5f);
        Radii = Distance / body.Radius;
        //set camera distance
        Distance += zoom * ZoomFactor * slowdown;
        Distance = Mathf.Clamp(Distance, MinDistance, MaxDistance);

        //mouse rotates around the planet
        if (!LightMode && !AngleMode)
        {
            RotateX -= mouseX * RotationFactor * slowdown;
            RotateY += mouseY * RotationFactor * slowdown;
            RotateY = Mathf.Clamp(RotateY, -.24f, .24f);
        }
        if (LightMode)
        {
            lightRotateX -= mouseX;// * RotationFactor;
            lightRotateY += mouseY;// * RotationFactor;
            lightRotateX = Mathf.Clamp(lightRotateX, -45f, 45f);
            lightRotateY = Mathf.Clamp(lightRotateY, -45f, 45f);
        }
        if (AngleMode)
        {
            camRotateX -= mouseX;// * RotationFactor;
            camRotateY += mouseY;// * RotationFactor;
            camRotateX = Mathf.Clamp(camRotateX, -45f, 45f);
            camRotateY = Mathf.Clamp(camRotateY, -45f, 45f);
        }




        //get current position from rotation settings
        float yaw = RotateX * 2f * Mathf.PI;
        float pitch = RotateY * 2f * Mathf.PI;
        float x = Mathf.Cos(pitch) * Mathf.Cos(yaw);
        float y = Mathf.Sin(pitch);
        float z = Mathf.Cos(pitch) * Mathf.Sin(yaw);
        Vector3 position = new Vector3(x, y, z);

        //set camera position and orientation
        camera.transform.position = body.transform.position + position * Distance;
        camera.transform.LookAt(body.transform.position);
        camera.transform.Rotate(new Vector3(camRotateY, camRotateX, 0));

        //angle the spotlight
        light.transform.rotation = camera.transform.rotation;
        //light.transform.localEulerAngles = new Vector3(lightRotateX, 0, lightRotateY);
        light.transform.Rotate(new Vector3(lightRotateY, lightRotateX, 0));

        UpdatePoles();
    }

    private void UpdatePoles()
    {
        if (!body) return;
        if (controls.Controls.ShowPoles.WasPressedThisFrame()) ShowPoles = !ShowPoles;
        float length = body.Radius * 2f + PoleBuffer;
        if (tPoles)
        {
            tPoles.gameObject.SetActive(ShowPoles);
            tPoles.position = body.transform.position;
            tPoles.localScale = new Vector3(PoleThickness, length/2f, PoleThickness);
        }

        if (tEquator)
        {
            tEquator.gameObject.SetActive(ShowPoles);
            tPoles.position = body.transform.position;
            tEquator.localScale = new Vector3(length, PoleThickness, length);
        }

    }

}
