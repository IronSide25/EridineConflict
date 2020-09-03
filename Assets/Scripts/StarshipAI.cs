using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum AttackType1Phase { Seek, Evade };
public enum AttackType2Phase { Seek, Stop };

public class StarshipAI : MonoBehaviour
{
    private StarshipSteering starshipSteering;
    private Rigidbody rigidbody;

    public bool isAttacking;
    public bool isShooting;

    public ParticleSystem laser;
    public Transform target;

    float shootingSpeed = 1f;
    float lastShootTime;

    public bool isEvading = false;

    // Start is called before the first frame update
    void Start()
    {
        starshipSteering = GetComponent<StarshipSteering>();
        rigidbody = GetComponent<Rigidbody>();
        starshipSteering.SetDestination(target);
        lastShootTime = Time.time + Random.Range(-0.5f, 0.5f);
        laser.Stop(false, ParticleSystemStopBehavior.StopEmitting);

        starshipSteering.pursuing = true;
        starshipSteering.rotateToTarget = true;

        starshipSteering.maxDesiredPursuitForce = 10;
        starshipSteering.maxDesiredSeekForce = 10;
    }
    // Update is called once per frame
    void Update()
    {       
        if(starshipSteering.rotateToTarget && starshipSteering.isTargeting && starshipSteering.distToTarget < 50)//change to 100
        {
            float angleDiff = Quaternion.Angle(transform.rotation, starshipSteering.desiredRotation);
            if (angleDiff < 5 && Time.time - lastShootTime > shootingSpeed)
            {
                lastShootTime = Time.time;
                laser.Emit(1);
            }
        }        

        if(!isEvading)
        {
            if (Vector3.SqrMagnitude(transform.position - target.position) < 300)
            {
                Vector3 dir = transform.position - starshipSteering.target;
                starshipSteering.SetDestinationFormation((transform.position + (dir.normalized * 150)), starshipSteering.shipsInFormation, false);
                isEvading = true;
                starshipSteering.rotateToTarget = false;
                //starshipSteering.SetStop();
                //starshipSteering.evade = true;
            }
        }
        else
        {
            if (Vector3.SqrMagnitude(transform.position - target.position) > 7000)
            {
                starshipSteering.SetDestinationFormation(target, starshipSteering.shipsInFormation, false);
                isEvading = false;
                starshipSteering.rotateToTarget = true;
                starshipSteering.pursuing = true;
            }
        }

    }
}

