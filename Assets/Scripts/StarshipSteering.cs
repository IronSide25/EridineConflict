using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum StarshipClass {Small, Medium, Large};

public class StarshipSteering : MonoBehaviour
{
    Rigidbody rigidbody;
    public Vector3 target;
    public Transform transformTarget;
    public bool useTransformTarget;

    public bool isMoving;
    public bool rotateToTarget;
    public bool isTargeting;
    public bool pursuing;
    public float maxSpeed = 1;
    public float accelerationRate;
    float currentMaxSpeed;
    
    public float noMovingMaxAngularAcc = 0.1f;
    public float moveEpsilon = 0.1f;
    public float distanceToStop = 1;
    public float slowingRadius = 5;
   
    [Header("Seek behavior")]
    public float maxDesiredSeekForce;
    public float maxSeekForce;
    public float seekMult = 1;

    [Header("Pursuit behavior")]
    public float maxPrediction = 100;
    public float maxDesiredPursuitForce;
    public float maxPursuitForce;
    public float pursuitMult = 1;

    [Header("Look behavior")]
    public float maxAngularAcc = 1;

    [Header("Collision voidance behavior")]
    public float maxAvoidForce;
    public float sphereCastRadius;
    public float sphereCastDistance;
    public float collisionAvoidanceMult = 1;

    [Header("Separation behavior")]
    public float threshold = 2f;
    public float decayCoefficient = -25f;
    public float separationForceMultiplier = 1f;
    public float maxSeparationForce;

    [Header("Cohesion behavior")]
    public float viewAngle = 60;
    public float cohesionForceMultiplier = 1f;

    [Header("Alingment behavior")]
    public float alignDistance = 10f;
    public float alignmentMultiplier = 5;
    public float maxAlingmentForce;

    [Header("Formation behavior")]
    public Transform[] shipsInFormation;
    public Plane targetPlane;

    [Header("INFO")]
    public Vector3 calculatedVelocity;
    public float distToTarget;

    public Quaternion desiredRotation;
    public bool evade = false;

    private void Awake()
    {
        rigidbody = transform.GetComponent<Rigidbody>();
        StarshipSteering[] starships = FindObjectsOfType<StarshipSteering>();
        SetStop();//delete that later
        isTargeting = false;
        rotateToTarget = false;
    }

    // Start is called before the first frame update
    void Start()
    {
        //SetStop();
        //SetDestination(transform.position);
        calculatedVelocity = Vector3.zero;       
    }

    private void Update()
    {
        //check for stuck here!!!

    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (isMoving)
        {
            //check angle and apply preturn
            if (useTransformTarget)
            {
                targetPlane = new Plane(transform.position - target, target);//not optimal as f
                target = transformTarget.position;
            }            

            Vector3 desiredVelocity = target - transform.position;
            distToTarget = Vector3.Magnitude(desiredVelocity);

            if (distToTarget < slowingRadius)
            {
                target = targetPlane.ClosestPointOnPlane(transform.position);
            }
          
            if (distToTarget < slowingRadius)
                currentMaxSpeed = Mathf.Clamp(maxSpeed * (distToTarget / slowingRadius), 0, currentMaxSpeed);
            if (currentMaxSpeed < maxSpeed)
                currentMaxSpeed += accelerationRate * Time.deltaTime;
            else
                currentMaxSpeed = maxSpeed;

            Vector3 calculatedVelocity = rigidbody.velocity;
            if(pursuing)
                calculatedVelocity += Pursuit(desiredVelocity, distToTarget) * seekMult;
            else
                calculatedVelocity += Seek(desiredVelocity, distToTarget) * seekMult;

            calculatedVelocity += Separation() * separationForceMultiplier;
            calculatedVelocity += Cohesion() * cohesionForceMultiplier;
            calculatedVelocity += Alignment(desiredVelocity, distToTarget) * alignmentMultiplier;
            calculatedVelocity += CollisionAvoidance() * collisionAvoidanceMult;

            /*Vector3 collisionAvoidanceVec = CollisionAvoidance();
            calculatedVelocity += collisionAvoidanceVec;*/
            /*if(collisionAvoidanceVec.magnitude < moveEpsilon)
                calculatedVelocity += Separation();*/

            if (calculatedVelocity.magnitude < moveEpsilon)//cut small movement
                calculatedVelocity = Vector3.zero;

            calculatedVelocity = Vector3.ClampMagnitude(calculatedVelocity, currentMaxSpeed);//limit steering
            rigidbody.velocity = calculatedVelocity;

            if (calculatedVelocity.magnitude >= moveEpsilon)//rotation
                if (rotateToTarget)
                    transform.rotation = LookTarget();
                else
                    transform.rotation = LookVelocity();

            if (Vector3.SqrMagnitude(transform.position - target) < distanceToStop* distanceToStop)
            {
                SetStop();
            }
        }
        else
        {
            rigidbody.constraints = RigidbodyConstraints.FreezeAll;
            Quaternion targetRot = Quaternion.LookRotation(Vector3.ProjectOnPlane(transform.forward, Vector3.up), Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, noMovingMaxAngularAcc);
        }
    }

    private Vector3 Seek(Vector3 desiredVelocity, float distanceToTarget)
    {
        desiredVelocity = target - transform.position;
        /*if (dist < slowingRadius)//moved to fixedupdate
            desiredVelocity = Vector3.Normalize(desiredVelocity) * maxDesiredSeekForce * (dist / slowingRadius);
        else*/
        desiredVelocity = Vector3.Normalize(desiredVelocity) * maxDesiredSeekForce;

        //Vector3 steering = desiredVelocity - rigidbody.velocity;//which is better
        Vector3 steering = desiredVelocity;

        if (distanceToTarget > slowingRadius)
            steering = steering / rigidbody.mass;
        if (steering.magnitude < moveEpsilon)
            steering = Vector3.zero;

        return Vector3.ClampMagnitude(steering, maxSeekForce);
    }

    private Vector3 Pursuit(Vector3 desiredVelocity, float distanceToTarget)
    {
        Vector3 steering = Vector3.zero;
        float speed = rigidbody.velocity.magnitude;
        float prediction;
        if (speed <= (distanceToTarget / maxPrediction))
            prediction = maxPrediction;
        else
            prediction = distanceToTarget / speed;
        Vector3 predictedTarget = transformTarget.position + (transformTarget.GetComponent<Rigidbody>().velocity * prediction);
        steering = predictedTarget - transform.position;
        steering = Vector3.Normalize(steering) * maxDesiredPursuitForce;

        if (distanceToTarget > slowingRadius)
            steering = steering / rigidbody.mass;
        if (steering.magnitude < moveEpsilon)
            steering = Vector3.zero;

        return evade ? -Vector3.ClampMagnitude(steering, maxPursuitForce) : Vector3.ClampMagnitude(steering, maxPursuitForce);
    }

    private Vector3 Separation()
    {
        Vector3 steering = Vector3.zero;
        if(shipsInFormation.Length > 0)
        {
            foreach (Transform ship in shipsInFormation)
            {
                Vector3 direction = ship.position - transform.position;
                float distanceSqr = direction.sqrMagnitude;
                if (distanceSqr < threshold * threshold)
                {
                    float strength = Mathf.Min(decayCoefficient / (distanceSqr), maxSeparationForce);
                    direction.Normalize();
                    steering = strength * direction;
                }
            }
        }      
        return steering;
    }

    private Vector3 Cohesion()
    {
        Vector3 centerOfMass = Vector3.zero;
        Vector3 steering = Vector3.zero;
        int count = 0;
        foreach (Transform ship in shipsInFormation)
        {
            Vector3 dir = ship.position - transform.position;
            if(Vector3.Angle(dir, transform.forward) < viewAngle)
            {
                centerOfMass += ship.position;
                count++;
            }
        }
        if(count > 0)
        {
            centerOfMass = centerOfMass / count;
            steering = centerOfMass - transform.position;
            steering.Normalize();
        }
        return steering;
    }

    private Vector3 Alignment(Vector3 desiredVelocity, float distanceToTarget)
    {
        Vector3 steering = Vector3.zero;
        int count = 0;
        foreach (Transform ship in shipsInFormation)
        {
            Vector3 dir = ship.position - transform.position;
            if(dir.sqrMagnitude < alignDistance * alignDistance)
            {
                steering += ship.GetComponent<Rigidbody>().velocity;
                count++;
            }
        }
        if(count > 0)
        {
            steering = steering / count;
            if (steering.magnitude > maxAlingmentForce)
                steering = steering.normalized * maxAlingmentForce;
        }
        desiredVelocity = target - transform.position;
        distanceToTarget = Vector3.Magnitude(desiredVelocity);
        if (distanceToTarget < slowingRadius)
            steering *= distanceToTarget / slowingRadius;

        return steering;
    }


    private Vector3 CollisionAvoidance()//maybe use calculatedVelocity instead of velocity
    {
        RaycastHit hit;
        Vector3 avoidanceForce = Vector3.zero;
        Vector3 ahead = transform.position + (rigidbody.velocity.normalized * (sphereCastDistance + sphereCastRadius));
        if (Physics.SphereCast(transform.position, sphereCastRadius, rigidbody.velocity.normalized, out hit, sphereCastDistance, (1 << 8) | (1 << 10)))
        {
            if (hit.transform != transform)
            {                
                avoidanceForce = ahead - hit.transform.position;
                avoidanceForce = Vector3.Normalize(avoidanceForce) * maxAvoidForce;
                if (avoidanceForce.y > 0)
                    avoidanceForce.y = 1;
                else
                    avoidanceForce.y = -1;
                avoidanceForce.y *= maxAvoidForce;//disable this to lower the quality of collision evasion
                //avoidanceForce.x = 0;
                //avoidanceForce.z = 0;
                //Debug.Break();
            }
        }
        avoidanceForce = avoidanceForce / rigidbody.mass;
        if (avoidanceForce.sqrMagnitude < moveEpsilon * moveEpsilon)
            avoidanceForce = Vector3.zero;
        return avoidanceForce;
    }

    private Quaternion LookVelocity()
    {
        isTargeting = false;
        Quaternion lookRotation = Quaternion.LookRotation(rigidbody.velocity);   
        desiredRotation = lookRotation;
        return Quaternion.Lerp(transform.rotation, lookRotation, maxAngularAcc * Time.deltaTime);
    }

    private Quaternion LookTarget()
    {
        isTargeting = false;
        Quaternion lookRotation;
        if (evade)
            lookRotation = Quaternion.LookRotation(rigidbody.velocity);
        else
        {
            isTargeting = true;
            lookRotation = Quaternion.LookRotation(target - transform.position);
            desiredRotation = lookRotation;
        }                  
        return Quaternion.Lerp(transform.rotation, lookRotation, maxAngularAcc * Time.deltaTime);
    }

    public void SetDestination(Vector3 _target)
    {
        useTransformTarget = false;
        target = _target;
        targetPlane = new Plane(transform.position - target, target);
        isMoving = true;
        pursuing = false;
        rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
    }

    public void SetDestination(Transform _targetTransform)
    {
        useTransformTarget = true;
        transformTarget = _targetTransform;
        target = transformTarget.position;
        targetPlane = new Plane(transform.position - target, target);
        isMoving = true;
        pursuing = false;
        rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
    }

    public void SetDestinationFormation(Vector3 _target, Transform[] _shipsInFormation, bool deleteThisFromFormation = true)
    {
        useTransformTarget = false;
        target = _target;
        targetPlane = new Plane(transform.position - target, target);
        isMoving = true;
        pursuing = false;
        rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
        if(deleteThisFromFormation)
        {
            shipsInFormation = new Transform[_shipsInFormation.Length - 1];
            int count = 0;
            foreach (Transform ship in _shipsInFormation)
                if (ship != transform)
                {
                    shipsInFormation[count] = ship;
                    count++;
                }
        }      
    }

    public void SetDestinationFormation(Transform _targetTransform, Transform[] _shipsInFormation, bool deleteThisFromFormation = true)
    {
        useTransformTarget = true;
        transformTarget = _targetTransform;
        target = transformTarget.position;
        targetPlane = new Plane(transform.position - target, target);
        isMoving = true;
        pursuing = false;
        rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
        if(deleteThisFromFormation)
        {
            shipsInFormation = new Transform[_shipsInFormation.Length - 1];
            int count = 0;
            foreach (Transform ship in _shipsInFormation)
                if (ship != transform)
                {
                    shipsInFormation[count] = ship;
                    count++;
                }
        }       
    }

    public void SetStop()
    {
        isMoving = false;
        pursuing = false;
        rigidbody.constraints = RigidbodyConstraints.FreezeAll;
    }

    public void OnCollisionEnter(Collision collision)
    {
        if (shipsInFormation.Length > 0 && isMoving)//transitive bumping
            if (Array.Exists(shipsInFormation, t => t == collision.transform))
                if (!collision.transform.GetComponent<StarshipSteering>().isMoving)
                {
                    SetStop();
                }
    }
}
