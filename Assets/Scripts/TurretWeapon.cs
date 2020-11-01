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
    public bool info;
    // Update is called once per frame
    void Update()
    {
        if(isActive && isWeaponAlive)
        {
            if (starshipSteering.distToTarget < range)//i think its working fine, but test if rot z is always zero
            {
                float prediction = starshipSteering.distToTarget / projectileSpeed;
                Vector3 attackPredictedTarget = starshipSteering.transformTarget.position + (starshipSteering.transformTarget.GetComponent<Rigidbody>().velocity * prediction);
                Vector3 direction = (attackPredictedTarget - transform.position).normalized;
                Quaternion lookRotation = Quaternion.LookRotation(direction);
                Vector3 desRot = lookRotation.eulerAngles;
                desRot.z = turretBase.rotation.eulerAngles.z;
                lookRotation.eulerAngles = desRot;

                Vector3 safeEuler = turretBase.localRotation.eulerAngles;

                Quaternion q = Quaternion.RotateTowards(turretBase.rotation, lookRotation, rotationSpeed * Time.deltaTime);
                Vector3 qVec = q.eulerAngles;
                qVec.z = 0;
                turretBase.rotation = Quaternion.Euler(qVec);
                Vector3 newRot = turretBase.localRotation.eulerAngles;
                Quaternion safeRotation = Quaternion.Euler(new Vector3(safeEuler.x, newRot.y, 0));//rotation with limited x 

                if (!(turretBase.localRotation.eulerAngles.x > maxRotationX && turretBase.localRotation.eulerAngles.x < 90))
                {
                    info = true;
                    if (Time.time - lastShootTime > shootingSpeed && Quaternion.Angle(turretBase.rotation, lookRotation) < 5)//make variable out of this
                    {
                        lastShootTime = Time.time;
                        foreach (ParticleSystem laser in lasers)
                            laser.Emit(1);
                    }
                }
                else
                {
                    info = false;
                    turretBase.localRotation = safeRotation;
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
