using System.Collections.Generic;
using UnityEngine;

public class WeaponsSystem : MonoBehaviour
{

    public List<Weapon> weapons = new List<Weapon>();
    public int WeaponIndex = 0; //index of next gun in the sequence
    public float WeaponIndexDelay = .1f; //delay between continuation of sequence
    public bool WeaponsFireInterlinked = false; //guns all fire together
    public bool IsFiring = false;
    public float IsFiringCooldown = .25f;
    private float lastFireTime = 0;


    void OnEnable()
    {
        if (weapons.Count == 0)
        {
            foreach (Weapon weapon in GetComponentsInChildren<Weapon>())
            {
                weapons.Add(weapon);
            }
        }
        SelectWeapon();
    }

    void Update()
    {
        UpdateFiringStatus();
    }

    public void SelectWeapon()
    {
        //set firing index delay to keep fire continuous
        if (weapons.Count > 0)
        {
            float rateAvg = 0;
            foreach (Weapon weapon in weapons) rateAvg += weapon.FireRate;
            rateAvg /= weapons.Count;
            if (rateAvg > 0)
            {
                WeaponIndexDelay = (1f / rateAvg) / weapons.Count;
            }
        }

        //if (Time.time - lastFireTime < 1/FireRate) return; //enforce fire rate

    }



    public void Fire()
    {
        if (weapons.Count == 0) return;
        if (Time.time - lastFireTime < WeaponIndexDelay) return;

        //check for weapons and fire them
        if (WeaponsFireInterlinked)
        {
            foreach (Weapon weapon in weapons) weapon.Fire();
        }
        else
        {
            if (WeaponIndex >= weapons.Count) WeaponIndex = 0;
            weapons[WeaponIndex].Fire();
            WeaponIndex++;
        }

        IsFiring = true;
        lastFireTime = Time.time;
    }


    private void UpdateFiringStatus()
    {
        if (IsFiring && Time.time - lastFireTime >= IsFiringCooldown)
        {
            IsFiring = false;
        }
    }


}
