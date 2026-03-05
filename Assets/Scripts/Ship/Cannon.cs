using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

public class Cannon : MonoBehaviour
{
    public GameObject Target;

    //public Weapon weapon;

    public float angleToTarget = 0;
    public Vector3 localTargetDir;
    public float FiringRange = 3;
    public float FiringAngle = 5;
    public float precisionAngle = 5;

    public Vector3 vectorToTarget;
    public float distanceToTarget;

    public Ship ship;
    public SteeringSystem steering;
    public WeaponsSystem weapons;

    public WeaponsSystem shipWeapons;

    void Start()
    {
        ship = GetComponentInParent<Ship>();
        steering = GetComponent<SteeringSystem>();
        weapons = GetComponent<WeaponsSystem>();
        if (ship) shipWeapons = ship.GetComponent<WeaponsSystem>();

    }

    void Update()
    {
        if (!Game.I) return;
        if (!shipWeapons || !shipWeapons.enabled) return;

        //target the player, if present
        if (!Target) Target = Game.I.PlayerShip?.gameObject;

        if (Target)
        {
            angleToTarget = Vector3.Angle(Vector3.forward, localTargetDir);
            vectorToTarget = (Target.transform.position - transform.position);//.normalized;
            distanceToTarget = vectorToTarget.magnitude;
            localTargetDir = transform.InverseTransformDirection(vectorToTarget);
            vectorToTarget.Normalize();
        }

        Aim();
        Attack();
    }

    private void LateUpdate()
    {
        if (steering)
        {
            transform.Rotate(steering.Result, Space.Self);
        }
    }

    void Aim()
    {
        if (!Target || !steering) return;
        float precision = 1;
        if (angleToTarget < precisionAngle)
        {
            precision = Mathf.Lerp(0.25f, 1f, angleToTarget / precisionAngle);
        }


        steering.Stick = BotControl.Aim(gameObject, Target) * precision;
    }


    void Attack()
    {
        if (distanceToTarget > FiringRange) return;
        if (angleToTarget > FiringAngle) return;
        if (weapons) weapons.Fire();

    }

}
