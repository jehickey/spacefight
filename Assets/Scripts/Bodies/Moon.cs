using Unity.VisualScripting;
using UnityEngine;

public class Moon : Body
{
    //public bool TidalLock = false;
    //public float OrbitRadius = 100;
    //public float OrbitPeriod = 10;

    //public float OrbitPhase = 0; //0-1, current orbital period position

    //public Body parentBody;

    protected override void Start()
    {
        base.Start();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        if (!parentBody)
        {
            if (transform.parent)
            {
                parentBody = transform.parent.GetComponent<Body>();
                if (!parentBody) Debug.LogWarning("Moon has a parent but it's not a Body!");
            }
        }
        if (OrbitPhase==0) OrbitPhase = Random.Range(0f, 1f);
    }

    protected override void Update()
    {
        base.Update();
        if (TidalLock && parentBody)
        {
            RotationPeriod = OrbitPeriod;
        }

        if (Application.isPlaying && OrbitPeriod>0)
        {
            if (Simulation.I)
            {
                OrbitPhase += Simulation.I.TimeDelta / OrbitPeriod;
            }
        }
        SetOrbitalPosition();
    }

    protected override void OnValidate()
    {
        base.OnValidate();
        SetOrbitalPosition();
        //Update();
    }

    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();
        /*
        if (transform.parent)
        {
            int segments = 36;
            Gizmos.color = Color.yellow;
            Vector3 prev = transform.parent.position + new Vector3(OrbitRadius, 0f, 0f);
            float step = 2f * Mathf.PI / segments;
            for (int i = 1; i <= segments; i++)
            {
                float angle = i * step;
                Vector3 nextPoint = transform.parent.position + new Vector3(
                    Mathf.Cos(angle) * OrbitRadius,
                    0f,
                    Mathf.Sin(angle) * OrbitRadius
                );
                Gizmos.DrawLine(prev, nextPoint);
                prev = nextPoint;
            }
        }
        */
        if (parentBody)
        {
            int segments = 36;
            Gizmos.color = Color.yellow;
            Vector3 prev = parentBody.transform.position + new Vector3(OrbitRadius, 0f, 0f);
            float step = 2f * Mathf.PI / segments;
            for (int i = 1; i <= segments; i++)
            {
                float angle = i * step;
                Vector3 nextPoint = parentBody.transform.position + new Vector3(
                    Mathf.Cos(angle) * OrbitRadius,
                    0f,
                    Mathf.Sin(angle) * OrbitRadius
                );
                Gizmos.DrawLine(prev, nextPoint);
                prev = nextPoint;
            }
        }


    }

    private void SetOrbitalPosition()
    {
        if (OrbitPhase > 1) OrbitPhase = 0;
        if (!parentBody) return;
        float angle = -OrbitPhase * 2f * Mathf.PI;
        transform.position = parentBody.transform.position + new Vector3(
            Mathf.Cos(angle) * OrbitRadius,
            0f,
            Mathf.Sin(angle) * OrbitRadius
        );
    }

}
