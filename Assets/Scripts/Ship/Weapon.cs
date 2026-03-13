using UnityEngine;

public class Weapon : MonoBehaviour
{
    public float FireRate = 1;      //shots per second
    public GameObject projectilePrefab;
    public bool doFire;

    public float FireVolume = 1;
    public float FirePitch = 1;
    public float FireRandomization = .25f;

    private Ship ship;
    private Emitter emitter;
    private Recoil recoil;
    private float lastFireTime;

    //private new AudioSource audio;
    public SoundMachine sound;

    void Start()
    {
        emitter = GetComponentInChildren<Emitter>();
        if (!emitter) Debug.LogError("Weapon is missing an emitter");
        recoil = GetComponentInChildren<Recoil>();
        //if (!recoil) Debug.LogError("Weapon is missing a recoil");
        ship = GetComponentInParent<Ship>();
        //if (!ship) Debug.LogError("Weapon can't find Ship controller");

        
        //audio = GetComponent<AudioSource>();
        //if (ship != sim.PlayerShip) Destroy(audio);
        if (!sound) sound = GetComponent<SoundMachine>();

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
            if (projectile && ship)
            {
                projectile.parentOrigin = ship.transform;
                //projectile.Speed += ship.Speed;
            }

            //emitter flare
            Flare flare = Flare.Spawn(emitter.transform.position, Color.white, 0.25f, 0.25f, 0.001f, 0.01f);
            flare.transform.parent = emitter.transform;
            if (ship == Game.I.PlayerShip) flare.LightRadiusMultiplier = 2f;
            playFireSound();
        }
        lastFireTime = Time.time;
    }


    private void playFireSound()
    {
        if (!sound) return;
        float pitch = FirePitch * (1 + Random.value * FireRandomization);
        float volume = FireVolume * (1 + Random.value * FireRandomization);
        sound.Pitch = pitch;
        sound.Volume = volume * Game.I.AudioLevelWeapons;
        sound.Looping = false;
        //if (transform.position.x > 0) audio.panStereo = 1;
        //if (transform.position.x < 0) audio.panStereo = -1;
        sound.Play();
    }

}
