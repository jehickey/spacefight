using UnityEngine;

public class Weapon : MonoBehaviour
{
    public float FireRate = 1;      //shots per second
    public GameObject projectilePrefab;
    public bool doFire;

    private Ship ship;
    private Emitter emitter;
    private Recoil recoil;
    private float lastFireTime;


    void Start()
    {
        emitter = GetComponentInChildren<Emitter>();
        if (!emitter) Debug.LogError("Weapon is missing an emitter");
        recoil = GetComponentInChildren<Recoil>();
        if (!recoil) Debug.LogError("Weapon is missing a recoil");
        ship = GetComponentInParent<Ship>();
        if (!ship) Debug.LogError("Weapon can't find Ship controller");
    }

    void Update()
    {
        
        if (doFire)
            {
                doFire = false;
                Fire();
        }

    }


    public void Fire()
    {
        if (Time.time - lastFireTime < 1/FireRate) return; //enforce fire rate

        if (recoil) recoil.Fire();
        if (emitter && projectilePrefab)
        {
            GameObject obj = Instantiate(projectilePrefab, emitter.transform.position, emitter.transform.rotation);
            Projectile projectile = obj.GetComponent<Projectile>();
            if (projectile)
            {
                //projectile.Speed += ship.Speed;
            }

            //emitter flare
            Flare flare = Flare.Spawn(emitter.transform.position, Color.white, 0.25f, 0.25f, 0.001f, 0.01f);
            flare.transform.parent = emitter.transform;
        }
        lastFireTime = Time.time;
    }

}
