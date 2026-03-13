using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class Destructable : MonoBehaviour
{

    public GameObject RootObject;       //in case this object is part of another
    public float Health;
    public float MaxHealth = 10;
    public AudioClip clipExplosion;
    public SoundMachine soundGotHit;
    public Transform lastHitBy;
    public bool PartSeparation = false;
    public float SeparationForce = 1;
    public float SeparationSpin = 1;
    public float SeparationTTL = 3;
    public float TTL = 0;

    public bool doDestroy = false;

    private Ship ship;
    private float startTime;

    private void OnEnable()
    {
        //if (!soundGotHit) Debug.Log($"Destructable on {name} has no SoundMachine");

        Health = MaxHealth;
        if (RootObject) ship = RootObject.GetComponent<Ship>();
        if (!RootObject) ship = GetComponent<Ship>();
        if (soundGotHit && soundGotHit.Sound == null) soundGotHit.Sound = Game.I.defaultSoundHit;
        if (clipExplosion == null) clipExplosion = Game.I.defaultSoundExplosion;
        startTime = Time.time;
    }


    void Update()
    {
        //if destroyed and a rootobject is set, blow that up
        UpdateHealth();
        if (doDestroy) Die();
        if (TTL > 0 && Time.time - startTime > TTL) Die();
    }

    public void TakeDamage(float damage, Transform origin = null)
    {
        if (damage <= 0) return;
        Health -= damage;
        if (soundGotHit) soundGotHit.Play();
        if (origin) lastHitBy = origin;
        if (ship) ship.TakeDamage(damage, origin);
    }

    private void UpdateHealth()
    {
        Health = Mathf.Clamp(Health, 0, MaxHealth);
        //Did they die?
        if (Health <= 0)
        {
            //destroy the object (or its root)
            Die();
        }
    }

    private void Die()
    {
        Flare flare = Flare.Spawn(transform.position, Color.white, 2f, 0.15f, 0.005f, 0.05f, clipExplosion);
        if (RootObject)
        {
            Destroy(RootObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        //scoring
        if (!Game.I) return;        //sometimes this gets called after Game.I is destroyed
        if (!ship) return;
        if (ship == Game.I.PlayerShip)
        {
            Game.I.AddDeath();
        }
        else
        {
            if (Game.I.PlayerShip && lastHitBy == Game.I.PlayerShip.transform) Game.I.AddKill();
        }
        SplitParts();
    }



    private void SplitParts()
    {
        if (!PartSeparation) return;

        foreach (Transform part in transform)
        {
            part.parent = null;
            Rigidbody body = part.GetComponent<Rigidbody>();
            if (!body) body = part.AddComponent<Rigidbody>();
            if (!body) Debug.Log("Failed to add a rigidbody!");
            body.useGravity = false;
            body.isKinematic = false;
            body.linearVelocity = Random.insideUnitSphere * SeparationForce;
            body.angularVelocity = Random.insideUnitSphere * SeparationSpin;
            Destructable destructable = part.GetComponent<Destructable>();
            if (!destructable) destructable = part.AddComponent<Destructable>();
            destructable.TTL = SeparationTTL * (1+Random.Range(-.5f, .5f));
            //Debug.Log($"Assigning TTL {destructable.TTL}");
        }

    }
}