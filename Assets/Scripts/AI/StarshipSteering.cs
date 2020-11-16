using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class StarshipSteering : MonoBehaviour
{
    Rigidbody rigidbody;
    public Vector3 target;
    public Transform transformTarget;
    public bool useTransformTarget;   

    public bool isMoving;
    public bool isTargeting;
    public bool pursuing;
    public bool compensateMass;
    public bool calculatePrediction;

    public bool allowStopOnDistance;
    public bool allowTransitiveBumping;

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
    public bool limitSeekRotation = false;
    public float allowedSeekRotation = 1.5708f;//90 degr

    [Header("Pursuit behavior")]
    public float maxPrediction = 100;
    public float maxDesiredPursuitForce;
    public float maxPursuitForce;
    public float pursuitMult = 1;
    public float projectileSpeed;
    public bool limitPursuitRotation = false;
    public float allowedPursuitRotation = 1.5708f;//90 degr

    [Header("Look behavior")]
    public float maxAngularAcc = 1;

    [Header("Collision avoidance behavior")]
    public float maxAvoidForce;
    public float sphereCastRadius;
    public float sphereCastDistance;
    public float collisionAvoidanceMult = 1;
    public bool collideWithFormationUnits = true;

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
    public FormationHelper formationHelper;
    public Transform[] shipsInFormation;
    public Plane targetPlane;

    [Header("INFO")]
    public Vector3 calculatedVelocity;
    public float distToTarget { get; private set; }
    public Vector3 desiredVelocity { get; private set; }
    public Vector3 attackPredictedTarget { get; private set; }
    public Vector3 predictedTargetPos { get; private set; }

    public Quaternion desiredRotation;
    public bool evade = false;

    private delegate Vector3 MoveBehavior();
    private MoveBehavior[] currentMoveBehavior;
    private delegate Quaternion RotateBehavior();
    private RotateBehavior currentRotateBehavior;

    private void Awake()
    {
        rigidbody = transform.GetComponent<Rigidbody>();
        SetStop();//delete that later
        isTargeting = false;
        allowStopOnDistance = true;
        allowTransitiveBumping = true;
        SetMoveBehaviorInFormation();
        SetRotationBehaviorLookVelocity();
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

    // W FIXED TIME DELTA JEST NIEPOTRZEBNE
    // Update is called once per frame
    void FixedUpdate()
    {
        if(transformTarget == null)
        {
            useTransformTarget = false;
            pursuing = false;
        }

        if (isMoving)
        {
            //List<Transform> allShipsInFormation = new List<Transform>(formationHelper.GetShipsInFormationRemoveNull());
            List<Transform> allShipsInFormation = new List<Transform>(formationHelper.shipsInFormation);//OPTIMIZE THIS XD
            allShipsInFormation.Remove(transform);
            shipsInFormation = allShipsInFormation.ToArray();

            //check angle and apply preturn
            if (useTransformTarget)
            {
                targetPlane = new Plane(transform.position - target, target);//not optimal as f
                target = transformTarget.position;
            }            
            desiredVelocity = target - transform.position;
            distToTarget = Vector3.Magnitude(desiredVelocity);

            if (distToTarget < slowingRadius)
                target = targetPlane.ClosestPointOnPlane(transform.position);
          
            if (distToTarget < slowingRadius)
                currentMaxSpeed = Mathf.Clamp(maxSpeed * (distToTarget / slowingRadius), 0, currentMaxSpeed);
            if (currentMaxSpeed < maxSpeed)
                currentMaxSpeed += accelerationRate * Time.deltaTime;
            else
                currentMaxSpeed = maxSpeed;

            if(calculatePrediction)
            {
                float prediction;
                if (projectileSpeed <= (distToTarget / maxPrediction))
                    prediction = maxPrediction;
                else
                    prediction = distToTarget / projectileSpeed;
                predictedTargetPos = transformTarget.position + (transformTarget.GetComponent<Rigidbody>().velocity * prediction);//cache rigid
            }

            calculatedVelocity = rigidbody.velocity;
            foreach (MoveBehavior behavior in currentMoveBehavior)
                calculatedVelocity += compensateMass ? (behavior() / rigidbody.mass) : behavior();

            /*Vector3 collisionAvoidanceVec = CollisionAvoidance();
            calculatedVelocity += collisionAvoidanceVec;*/
            /*if(collisionAvoidanceVec.magnitude < moveEpsilon)
                calculatedVelocity += Separation();*/

            if (calculatedVelocity.magnitude < moveEpsilon)//cut small movement
                calculatedVelocity = Vector3.zero;

            calculatedVelocity = Vector3.ClampMagnitude(calculatedVelocity, currentMaxSpeed);//limit steering
            rigidbody.velocity = calculatedVelocity;

            if (calculatedVelocity.magnitude >= moveEpsilon)//rotation
                transform.rotation = currentRotateBehavior();

            if (Vector3.SqrMagnitude(transform.position - target) < distanceToStop* distanceToStop && allowStopOnDistance)//maybe move this to starshipai
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

    private Vector3 SeekPursuitAuto()
    {
        if (pursuing)
            return Pursuit();
        else
            return Seek();
    }

    Vector3 lastDesiredVelocity;//move this upwards
    private Vector3 Seek()
    {
        Vector3 desiredVelocityNorm = desiredVelocity.normalized;
        /*if (dist < slowingRadius)//moved to fixedupdate
            desiredVelocity = desiredVelocity * maxDesiredSeekForce * (dist / slowingRadius);
        else*/
        if(limitSeekRotation)
        {
            if (lastDesiredVelocity != null)
            {
                desiredVelocityNorm = Vector3.RotateTowards(lastDesiredVelocity, desiredVelocityNorm, allowedSeekRotation * Time.fixedDeltaTime, 1f);
            }
            lastDesiredVelocity = desiredVelocityNorm;
        }      

        Vector3 steering = desiredVelocityNorm * maxDesiredSeekForce;//Vector3 steering = desiredVelocity - rigidbody.velocity;//which is better

        if (distToTarget > slowingRadius)
            steering = compensateMass ? steering : (steering / rigidbody.mass);
        if (steering.magnitude < moveEpsilon)
            steering = Vector3.zero;

        return Vector3.ClampMagnitude(steering, maxSeekForce) * seekMult;
    }

    private Vector3 Pursuit()
    {
        Vector3 steering = Vector3.zero;
        //float speed = rigidbody.velocity.magnitude;//this is enemy speed
        float prediction;
        if (projectileSpeed <= (distToTarget / maxPrediction))
            prediction = maxPrediction;
        else
            prediction = distToTarget / projectileSpeed;
        Vector3 predictedTarget = transformTarget.position + (transformTarget.GetComponent<Rigidbody>().velocity * prediction);
        Vector3 desiredVelocityNorm = Vector3.Normalize(predictedTarget - transform.position);

        if (limitPursuitRotation)
        {
            if (lastDesiredVelocity != null)
            {
                desiredVelocityNorm = Vector3.RotateTowards(lastDesiredVelocity, desiredVelocityNorm, allowedPursuitRotation * Time.fixedDeltaTime, 1f);
            }
            lastDesiredVelocity = desiredVelocityNorm;
        }

        steering = desiredVelocityNorm * maxDesiredPursuitForce;
        if (distToTarget > slowingRadius)
            steering = compensateMass ? steering : (steering / rigidbody.mass);
        if (steering.magnitude < moveEpsilon)
            steering = Vector3.zero;

        return (evade ? -Vector3.ClampMagnitude(steering, maxPursuitForce) : Vector3.ClampMagnitude(steering, maxPursuitForce)) * pursuitMult;
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
        return steering * separationForceMultiplier;
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
            Debug.DrawRay(transform.position, steering * cohesionForceMultiplier);
        }
        return steering * cohesionForceMultiplier;
    }

    private Vector3 Alignment()
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
        if (distToTarget < slowingRadius)
            steering *= distToTarget / slowingRadius;

        return steering * alignmentMultiplier;
    }


    private Vector3 CollisionAvoidance()//maybe use calculatedVelocity instead of velocity
    {
        //RaycastHit hit;
        Vector3 avoidanceForce = Vector3.zero;
        Vector3 ahead = transform.position + (rigidbody.velocity.normalized * (sphereCastDistance + sphereCastRadius));
        //RaycastHit[] hits = Physics.SphereCastAll(transform.position, sphereCastRadius, rigidbody.velocity.normalized, sphereCastDistance, (1 << 8) | (1 << 10));
        //if(hits.Length > 0)
        //if (Physics.SphereCast(transform.position, sphereCastRadius, rigidbody.velocity.normalized, out hit, sphereCastDistance, (1 << 8) | (1 << 10)))
        //if (rigidbody.SweepTest(rigidbody.velocity.normalized, out hit, sphereCastDistance))      
        //if (Physics.SphereCast(transform.position - (transform.forward * sphereCastRadius), sphereCastRadius, rigidbody.velocity.normalized, out hit, sphereCastDistance, (1 << 8) | (1 << 10)))
        RaycastHit[] hits = Physics.SphereCastAll(transform.position - (transform.forward * sphereCastRadius), sphereCastRadius, rigidbody.velocity.normalized, sphereCastDistance, (1 << 8) | (1 << 10));
        if (hits.Length > 0)
        {
            foreach(RaycastHit hit in hits)
            if (hit.transform != transform)
                    if (collideWithFormationUnits || !shipsInFormation.Contains(hit.transform))
                    {
                        avoidanceForce = ahead - hit.transform.position;
                        avoidanceForce = Vector3.Normalize(avoidanceForce) * maxAvoidForce;
                        if (avoidanceForce.y > 0)
                            avoidanceForce.y = 1;
                        else
                            avoidanceForce.y = -1;
                        avoidanceForce.y *= maxAvoidForce;
                        //disable this to lower the quality of collision evasion
                        //avoidanceForce.x = 0;
                        //avoidanceForce.z = 0;
                        //Debug.Break();
                        break;
                    }
        }
        avoidanceForce = avoidanceForce / rigidbody.mass;
        if (avoidanceForce.sqrMagnitude < moveEpsilon * moveEpsilon)
            avoidanceForce = Vector3.zero;
        return avoidanceForce * collisionAvoidanceMult;
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
            lookRotation = Quaternion.LookRotation(target - transform.position);
            desiredRotation = lookRotation;
        }                  
        return Quaternion.Lerp(transform.rotation, lookRotation, maxAngularAcc * Time.deltaTime);
    }

    private Quaternion LookPursueTarget()
    {
        /*isTargeting = false;
        Quaternion lookRotation;
        if (evade || !transformTarget)
            lookRotation = Quaternion.LookRotation(rigidbody.velocity);
        else
        {
            float prediction = distToTarget / projectileSpeed;
            attackPredictedTarget = transformTarget.position + (transformTarget.GetComponent<Rigidbody>().velocity * prediction);

            isTargeting = true;
            lookRotation = Quaternion.LookRotation(attackPredictedTarget - transform.position);
            desiredRotation = lookRotation;
        }
        return Quaternion.Lerp(transform.rotation, lookRotation, maxAngularAcc * Time.deltaTime);*/

        isTargeting = false;
        Quaternion lookRotationPred;
        Quaternion lookRotationVel = Quaternion.LookRotation(rigidbody.velocity);

        if (evade || !transformTarget)
            desiredRotation = lookRotationVel;
        else
        {
            float prediction = distToTarget / projectileSpeed;
            attackPredictedTarget = transformTarget.position + (transformTarget.GetComponent<Rigidbody>().velocity * prediction);
            
            lookRotationPred = Quaternion.LookRotation(attackPredictedTarget - transform.position);
            if(Quaternion.Angle(lookRotationPred, lookRotationVel) < 5)//make variable out of this
            {
                desiredRotation = lookRotationPred;
                isTargeting = true;
            }               
            else
                desiredRotation = lookRotationVel;
        }
        return Quaternion.Lerp(transform.rotation, desiredRotation, maxAngularAcc * Time.deltaTime);
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

    public void SetDestinationFormation(Vector3 _target, FormationHelper _formationHelper, bool _pursuing = false)
    {
        formationHelper = _formationHelper;
        useTransformTarget = false;
        target = _target;
        targetPlane = new Plane(transform.position - target, target);
        isMoving = true;
        pursuing = _pursuing;
        rigidbody.constraints = RigidbodyConstraints.FreezeRotation;    
    }

    public void SetDestinationFormation(Transform _targetTransform, FormationHelper _formationHelper, bool _pursuing = false)
    {
        formationHelper = _formationHelper;
        useTransformTarget = true;
        transformTarget = _targetTransform;
        target = transformTarget.position;
        targetPlane = new Plane(transform.position - target, target);
        isMoving = true;
        pursuing = _pursuing;
        rigidbody.constraints = RigidbodyConstraints.FreezeRotation;      
    }

    public void SetMoveBehaviorInFormation()
    {
        currentMoveBehavior = new MoveBehavior[5];
        currentMoveBehavior[0] = new MoveBehavior(SeekPursuitAuto);
        currentMoveBehavior[1] = new MoveBehavior(Separation);
        currentMoveBehavior[2] = new MoveBehavior(Cohesion);
        currentMoveBehavior[3] = new MoveBehavior(Alignment);
        currentMoveBehavior[4] = new MoveBehavior(CollisionAvoidance);
    }

    public void SetMoveBehaviorIgnoreFormation()
    {
        currentMoveBehavior = new MoveBehavior[2];
        currentMoveBehavior[0] = new MoveBehavior(SeekPursuitAuto);
        currentMoveBehavior[1] = new MoveBehavior(CollisionAvoidance);
    }

    public void SetRotationBehaviorLookPursueTarget()
    {
        currentRotateBehavior = new RotateBehavior(LookPursueTarget);
    }

    public void SetRotationBehaviorLookVelocity()
    {
        currentRotateBehavior = new RotateBehavior(LookVelocity);
    }

    public void SetRotationBehaviorLookTarget()
    {
        currentRotateBehavior = new RotateBehavior(LookTarget);
    }

    public void SetStop()
    {
        isMoving = false;
        pursuing = false;
        rigidbody.constraints = RigidbodyConstraints.FreezeAll;
    }

    public void OnCollisionEnter(Collision collision)
    {
        if (allowTransitiveBumping && shipsInFormation.Length > 0 && isMoving)//transitive bumping
            if (Array.Exists(shipsInFormation, t => t == collision.transform))
                if (!collision.transform.GetComponent<StarshipSteering>().isMoving)
                {
                    SetStop();
                }
    }

    public void OnCollisionStay(Collision collision)//test this|| performance !!!
    {
        if (allowTransitiveBumping && shipsInFormation.Length > 0 && isMoving)//transitive bumping
            if (Array.Exists(shipsInFormation, t => t == collision.transform))
                if (!collision.transform.GetComponent<StarshipSteering>().isMoving)
                {
                    SetStop();
                }
    }
}
