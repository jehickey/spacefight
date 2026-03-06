using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraCursor : MonoBehaviour
{
    [Header("State Info")]
    public float Distance = 1.0f;
    public float Radii = 1.0f;

    [Header("Main Settings")]
    public int MaxDetail = 8;
    public bool AllowDeformation;
    public float StartingRadii;
    public bool ShowPoles = true;


    [Header("Limits and Sensitivity")]
    public float MinRadii = 1.01f;
    public float MaxRadii = 3f;
    public float ZoomFactor = .1f;
    public float ZoomFactorTerrain = .1f;
    public float RotationFactor = 1.0f;
    public float MinDistance;
    public float MaxDistance;

    [Header("Gizmo Values")]
    public Transform tPoles;
    public Transform tEquator;
    public float PoleThickness = .01f;
    public float PoleBuffer = .1f;

    [Header("UI Modes")]
    public bool OrbitMode;
    public bool LightMode;
    public bool AngleMode;
    public bool TerrainMode;

    [Header("UI Values")]
    public float RotateX;
    public float RotateY;
    public float lightRotateX;
    public float lightRotateY;
    public float camRotateX;
    public float camRotateY;


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

        if (!camera || !body || !light) Debug.Log("Cursor is missing critical components");
    }

    private void OnDisable()
    {
        controls?.Disable();
    }

    private void Start()
    {
        Regenerate();
    }

    void Update()
    {

        //don't go beyond hard limit set by Icosphere generator
        MaxDetail = Mathf.Clamp(MaxDetail, 0, Shapes.Icosphere.MaxSubdivisions);
        //Override Body's maximums (within limits of what Icosphere allows)
        if (MaxDetail > Body.MaxDetailGlobal) {Body.MaxDetailGlobal = MaxDetail;}
        //Apply detail maximum to this body
        body.MaxDetail = MaxDetail;

        if (body) body.TerrainDeformation = AllowDeformation;

        //regenerate planet
        if (body && controls.Controls.Regenerate.WasPressedThisFrame()) Regenerate();
        if (body && controls.Controls.ToggleDeformation.WasPressedThisFrame())
        {
            AllowDeformation = !AllowDeformation;
            Regenerate();
        }

        //mouse inputs
        float mouseX = controls.Controls.RotateX.ReadValue<float>();
        float mouseY = controls.Controls.RotateY.ReadValue<float>();
        float zoom = controls.Controls.Zoom.ReadValue<float>();

        ModeSelect();

        //establish distance limits and current radii
        MinDistance = body.Radius * MinRadii;
        MaxDistance = body.Radius * MaxRadii;
        float t = Mathf.InverseLerp(1, MaxRadii, Radii);
        float slowdown = t * t * (3f - 2f * t);     //or Mathf.Pow(t, 2.5f);
        Radii = Distance / body.Radius;
        //set camera distance
        if (!TerrainMode)
        {
            Distance += zoom * ZoomFactor * slowdown;
            Distance = Mathf.Clamp(Distance, MinDistance, MaxDistance);
        }

        //apply mouse rotation (in applicable modes)
        MouseRotation(mouseX, mouseY, slowdown);

        if (TerrainMode)
        {
            Simulation.I.TerrainMagnitudeScale -= zoom * ZoomFactorTerrain;
            Regenerate();
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

        TogglePoles();
    }

    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            //force regeneration if AllowDeformation changes
            if (body && body.TerrainDeformation != AllowDeformation) Regenerate();
        }
    }

    private void Regenerate()
    {
        body.TerrainDeformation = AllowDeformation;
        body.Regenerate = true;
    }

    void ModeSelect()
    {
        //modes selection
        LightMode = controls.Controls.MoveLightMode.IsPressed();
        AngleMode = controls.Controls.AngleCameraMode.IsPressed();
        TerrainMode = controls.Controls.TerrainMode.IsPressed() && AllowDeformation;
        OrbitMode = (!LightMode && !AngleMode && !TerrainMode);

        //Lock cursor in appropriate modes
        if (LightMode || AngleMode || TerrainMode || OrbitMode)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

    }


    void MouseRotation(float mouseX, float mouseY, float slowdown)
    {
        //mouse-based rotation
        if (OrbitMode)
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
    }

    private void TogglePoles()
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
