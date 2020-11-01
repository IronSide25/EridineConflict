using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaserWeapon : MonoBehaviour, IWeapon
{
    StarshipSteering starshipSteering;

    public ParticleSystem laser;
    public float shootingSpeed = 1f;
    public float damagePerShot = 2;
    public float range = 100f;
    public float projectileSpeed = 100f;
    float lastShootTime;

    private bool isActive;
    private bool isWeaponAlive;

    // Start is called before the first frame update
    void Start()
    {
        starshipSteering = GetComponentInParent<StarshipSteering>();
        lastShootTime = Time.time + Random.Range(-0.25f, 0.25f);
        laser.Stop(false, ParticleSystemStopBehavior.StopEmitting);
        isActive = false;
        isWeaponAlive = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (isActive && isWeaponAlive)
        {
            if (starshipSteering.isTargeting && starshipSteering.distToTarget < range)
            {
                float angleDiff = Quaternion.Angle(starshipSteering.transform.rotation, starshipSteering.desiredRotation);
                if (angleDiff < 5 && Time.time - lastShootTime > shootingSpeed)
                {
                    lastShootTime = Time.time;
                    laser.Emit(1);
                }
            }
        }            
    }

    public void Activate(Transform target)
    {
        //lastShootTime = Time.time + Random.Range(-0.5f, 0.5f);
        isActive = true;
    }

    public void Deactivate()
    {
        isActive = false;
    }

    public void Destroy()
    {
        isActive = false;
        isWeaponAlive = false;
    }

    public float GetDamage()
    {
        return damagePerShot;
    }

    public float GetRange()
    {
        throw new System.NotImplementedException();
    }

    public float GetProjectileSpeed()
    {
        return projectileSpeed;
    }
}
