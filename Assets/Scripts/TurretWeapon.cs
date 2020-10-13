using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurretWeapon : MonoBehaviour, IWeapon
{
    StarshipSteering starshipSteering;
    private Transform target;

    public ParticleSystem[] lasers;
    public Transform turretBase;
    public float maxRotationX = 15;
    public float shootingSpeed = 1f;
    public float rotationSpeed = 2;
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

        foreach(ParticleSystem laser in lasers)
            laser.Stop(false, ParticleSystemStopBehavior.StopEmitting);
        isActive = false;

        isWeaponAlive = true;
    }

    // Update is called once per frame
    void Update()
    {
        if(isActive && isWeaponAlive)
        {
            float prediction = starshipSteering.distToTarget / projectileSpeed;
            Vector3 attackPredictedTarget = starshipSteering.transformTarget.position + (starshipSteering.transformTarget.GetComponent<Rigidbody>().velocity * prediction);
            Vector3 direction = (attackPredictedTarget - transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            if (!(lookRotation.eulerAngles.x > maxRotationX && lookRotation.eulerAngles.x < 90))
            {
                turretBase.rotation = Quaternion.RotateTowards(turretBase.rotation, lookRotation, rotationSpeed * Time.deltaTime);
                if (Time.time - lastShootTime > shootingSpeed)
                {
                    lastShootTime = Time.time;
                    foreach (ParticleSystem laser in lasers)
                        laser.Emit(1);
                }
            }
        }
    }

    public void Activate(Transform target)
    {
        lastShootTime = Time.time + Random.Range(-0.5f, 0.5f);
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
