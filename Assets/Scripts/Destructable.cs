using UnityEngine;

public class Destructable : MonoBehaviour
{

    public GameObject RootObject;       //in case this object is part of another
    public float Health;
    public float MaxHealth;
    public AudioClip clipExplosion;
    public SoundMachine soundGotHit;
    public Transform lastHitBy;

    private Ship ship;

    private void OnEnable()
    {
        if (!soundGotHit) Debug.Log($"Destructable on {name} has no SoundMachine");

        Health = MaxHealth;
        if (RootObject) ship = RootObject.GetComponent<Ship>();
        if (!RootObject) ship = GetComponent<Ship>();
        if (soundGotHit && soundGotHit.Sound == null) soundGotHit.Sound = Game.I.defaultSoundHit;
        if (clipExplosion == null) clipExplosion = Game.I.defaultSoundExplosion;
    }


    void Update()
    {
        //if destroyed and a rootobject is set, blow that up
        UpdateHealth();
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
            Flare flare = Flare.Spawn(transform.position, Color.white, 2f, 0.15f, 0.025f, 0.25f, clipExplosion);
            //destroy the object (or its root)
            if (RootObject)
            {
                Destroy(RootObject);
            }
            else
            {
                Destroy(gameObject);
            }
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
    }
}