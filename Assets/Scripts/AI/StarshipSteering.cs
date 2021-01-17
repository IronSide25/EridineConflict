using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;

public class StarshipSteering : MonoBehaviour
{
    Rigidbody starshipRigidbody;
    public Vector3 target;
    public Transform transformTarget;
    public bool useTransformTarget;
    private Rigidbody transformTargetRigidbody;

    public bool isMoving;
    public bool isTargeting;
    public bool pursuing;
    public bool compensateMass;
    public bool calculatePrediction;
    public bool allowStopOnDistance;
    public bool allowTransitiveBumping;   
    private float currentMaxSpeed;

    public StarshipDataScriptableObject values;

    
    public float projectileSpeed;
    public bool collideWithFormationUnits = true;

    [Header("Formation behavior")]
    public FormationHelper formationHelper;
    public HashSet<Transform> shipsInFormation;
    public Plane targetPlane;

    [Header("INFO")]
    public Vector3 calculatedVelocity;
    Vector3 lastDesiredVelocity;
    public float distToTarget { get; private set; }
    public Vector3 desiredVelocity { get; private set; }
    public Vector3 attackPredictedTarget { get; private set; }
    public Vector3 predictedTargetPos { get; private set; }

    public Quaternion desiredRotation;
    public bool evade = false;
    private int layer;

    private delegate Vector3 MoveBehavior();
    private MoveBehavior[] currentMoveBehavior;
    private delegate Quaternion RotateBehavior();
    private RotateBehavior currentRotateBehavior;
    private RaycastHit[] hitsBuffer;
    private Collider[] collidersBuffer;
    public int maxRaycastBufferCount = 15;
    public int maxCollisionBufferCount = 15;

    private void Awake()
    {
        starshipRigidbody = transform.GetComponent<Rigidbody>();
        SetStop();
        isTargeting = false;
        allowStopOnDistance = true;
        allowTransitiveBumping = true;
        SetMoveBehaviorInFormation();
        SetRotationBehaviorLookVelocity();
        layer = gameObject.layer;

        hitsBuffer = new RaycastHit[maxRaycastBufferCount];
        collidersBuffer = new Collider[maxCollisionBufferCount];
    }

    // Start is called before the first frame update
    void Start()
    {
        calculatedVelocity = Vector3.zero;
        lastDesiredVelocity = transform.forward.normalized;
    }

    void FixedUpdate()
    {
        if(transformTarget == null)
        {
            useTransformTarget = false;
            pursuing = false;
        }

        if (isMoving)
        {
            shipsInFormation = formationHelper.shipsHashSet;

            if (useTransformTarget)
            {
                targetPlane = new Plane(transform.position - target, target);
                target = transformTarget.position;
            }            
            desiredVelocity = target - transform.position;//its calculated only once, and then used in seek and pusuit behaviors
            distToTarget = Vector3.Magnitude(desiredVelocity);//its calculated only once, and then used in behaviors

            if (distToTarget < (values.slowingRadius + values.distanceToStop))
            {
                target = targetPlane.ClosestPointOnPlane(transform.position);
                currentMaxSpeed = Mathf.Clamp(values.maxSpeed * (distToTarget / (values.slowingRadius + values.distanceToStop)), 0, currentMaxSpeed);
            }

            if (currentMaxSpeed < values.maxSpeed)
                currentMaxSpeed += values.accelerationRate * Time.deltaTime;
            else
                currentMaxSpeed = values.maxSpeed;

            if(calculatePrediction)
            {
                float prediction;
                if (projectileSpeed <= (distToTarget / values.maxPrediction))
                    prediction = values.maxPrediction;
                else
                    prediction = distToTarget / projectileSpeed;
                predictedTargetPos = transformTarget.position + (transformTargetRigidbody.velocity * prediction);
            }

            calculatedVelocity = starshipRigidbody.velocity;
            foreach (MoveBehavior behavior in currentMoveBehavior)
                calculatedVelocity += compensateMass ? (behavior() / starshipRigidbody.mass) : behavior();

            if (calculatedVelocity.magnitude < values.moveEpsilon)//cut small movement
                calculatedVelocity = Vector3.zero;

            calculatedVelocity = Vector3.ClampMagnitude(calculatedVelocity, currentMaxSpeed);//limit steering
            starshipRigidbody.velocity = calculatedVelocity;

            if (calculatedVelocity.magnitude >= values.moveEpsilon)//rotation
                transform.rotation = currentRotateBehavior();

            if (Vector3.SqrMagnitude(transform.position - target) < values.distanceToStop * values.distanceToStop && allowStopOnDistance)
            {
                SetStop();
            }            
        }
        else
        {
            starshipRigidbody.constraints = RigidbodyConstraints.FreezeAll;
            Quaternion targetRot = Quaternion.LookRotation(Vector3.ProjectOnPlane(transform.forward, Vector3.up), Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, values.noMovingMaxAngularAcc);
        }
    }

    private Vector3 SeekPursuitAuto()
    {
        if (pursuing)
            return Pursuit();
        else
            return Seek();
    }

    
    private Vector3 Seek()
    {
        Vector3 desiredVelocityNorm = desiredVelocity.normalized;
        if(values.limitSeekRotation)
        {
            if (lastDesiredVelocity != null)
            {
                desiredVelocityNorm = Vector3.RotateTowards(lastDesiredVelocity, desiredVelocityNorm, values.allowedSeekRotation * Time.fixedDeltaTime, 1f);
            }
            lastDesiredVelocity = desiredVelocityNorm;
        }      
        Vector3 steering = desiredVelocityNorm * values.maxSeekForce;
        if (distToTarget > (values.slowingRadius + values.distanceToStop))
            steering = compensateMass ? steering : (steering / starshipRigidbody.mass);
        if (steering.magnitude < values.moveEpsilon)
            steering = Vector3.zero;
        return steering * values.seekMult;
    }

    private Vector3 Pursuit()
    {
        Vector3 steering = Vector3.zero;
        float prediction;
        if (projectileSpeed <= (distToTarget / values.maxPrediction))
            prediction = values.maxPrediction;
        else
            prediction = distToTarget / projectileSpeed;
        Vector3 predictedTarget = transformTarget.position + (transformTargetRigidbody.velocity * prediction);
        Vector3 desiredVelocityNorm = Vector3.Normalize(predictedTarget - transform.position);

        if (values.limitPursuitRotation)
        {
            if (lastDesiredVelocity != null)
            {
                desiredVelocityNorm = Vector3.RotateTowards(lastDesiredVelocity, desiredVelocityNorm, values.allowedPursuitRotation * Time.fixedDeltaTime, 1f);
            }
            lastDesiredVelocity = desiredVelocityNorm;
        }

        steering = desiredVelocityNorm * values.maxPursuitForce;
        if (distToTarget > (values.slowingRadius + values.distanceToStop))
            steering = compensateMass ? steering : (steering / starshipRigidbody.mass);
        if (steering.magnitude < values.moveEpsilon)
            steering = Vector3.zero;

        return (evade ? -steering : steering) * values.pursuitMult;
    }

    private Vector3 Separation()
    {
        //Profiler.BeginSample("Separation");
        Vector3 steering = Vector3.zero;
        if(shipsInFormation.Count > 1)
        {
            foreach (Transform ship in shipsInFormation)
            {
                Vector3 direction = ship.position - transform.position;                
                float distanceSqr = direction.sqrMagnitude;
                if (distanceSqr < values.thresholdSqr && distanceSqr > float.Epsilon)
                {
                    float strength = Mathf.Min(values.decayCoefficient / distanceSqr, values.maxSeparationForce);
                    direction.Normalize();
                    steering += strength * direction;
                }
            }
        }
        //Profiler.EndSample();
        return steering * values.separationForceMultiplier;
    }

    private Vector3 SeparationPhysics()
    {
        //Profiler.BeginSample("Separation");
        Vector3 steering = Vector3.zero;
        float threshold = (float)Math.Sqrt(values.thresholdSqr);
        if (shipsInFormation.Count > 1)
        {
            int collAmount = Physics.OverlapSphereNonAlloc(transform.position, threshold, collidersBuffer, (1 << layer));
            for(int i = 0; i < collAmount; i++)
            {
                Collider ship = collidersBuffer[i];
                if (ship)
                {
                    Transform tr = ship.attachedRigidbody.transform;
                    if (shipsInFormation.Contains(tr))
                    {
                        Vector3 direction = tr.position - transform.position;
                        float distanceSqr = direction.sqrMagnitude;

                        if (distanceSqr > float.Epsilon)
                        {
                            float strength = Mathf.Min(values.decayCoefficient / distanceSqr, values.separationStrength);
                            direction.Normalize();
                            steering += strength * direction;
                        }
                    }
                }                                
            }
        }

        if (steering.magnitude > values.maxSeparationForce)
            steering = steering.normalized * values.maxSeparationForce;
        //Profiler.EndSample();
        return steering * values.separationForceMultiplier;
    }

    private Vector3 Cohesion()
    {
        //Profiler.BeginSample("Cohesion");
        Vector3 steering = Vector3.zero;
        if(shipsInFormation.Count > 1)
        {
            steering = formationHelper.GetCenterOfMass() - transform.position;
            steering.Normalize();
            steering *= values.maxCohesionForce;
        }      
        //Profiler.EndSample();
        return steering * values.cohesionForceMultiplier;
    }

    private Vector3 Alignment()
    {
        //Profiler.BeginSample("Alignment");
        Vector3 steering = Vector3.zero;
        if (shipsInFormation.Count > 1)
        {
            int count = 0;
            int collAmount = Physics.OverlapSphereNonAlloc(transform.position, values.alignDistance, collidersBuffer, (1 << layer));
            for(int i = 0; i < collAmount; i++)
            {
                Collider ship = collidersBuffer[i];
                if (ship)
                {
                    Transform tr = ship.attachedRigidbody.transform;
                    Vector3 dir = tr.position - transform.position;
                    steering += tr.GetComponent<Rigidbody>().velocity;
                    count++;
                }               
            }
            if (count > 0)
            {
                steering = steering / count;
                if (steering.magnitude > values.maxAlingmentForce)
                    steering = steering.normalized * values.maxAlingmentForce;
            }
        }
        //Profiler.EndSample();
        return steering * values.alignmentMultiplier;
    }

    private Vector3 CollisionAvoidance()
    {
        //Profiler.BeginSample("CollisionAvoidance");
        Vector3 avoidanceForce = Vector3.zero;
        Vector3 ahead = transform.position + (starshipRigidbody.velocity.normalized * (values.sphereCastDistance + values.sphereCastRadius));
        int collAmount = Physics.SphereCastNonAlloc(transform.position - (transform.forward * values.sphereCastRadius), 
            values.sphereCastRadius, starshipRigidbody.velocity.normalized, hitsBuffer, values.sphereCastDistance, (1 << 8) | (1 << 10));

        if (hitsBuffer.Length > 0)
        {
            for(int i = 0; i < collAmount; i++)
            {
                if (hitsBuffer[i].transform != transform)
                    if (collideWithFormationUnits || !shipsInFormation.Contains(hitsBuffer[i].transform))
                    {
                        avoidanceForce = ahead - hitsBuffer[i].transform.position;
                        avoidanceForce = Vector3.Normalize(avoidanceForce) * values.maxAvoidForce;
                        if (avoidanceForce.y > 0)
                            avoidanceForce.y = 1;
                        else
                            avoidanceForce.y = -1;
                        avoidanceForce.y *= values.maxAvoidForce;
                        break;
                    }
            }            
        }
        avoidanceForce = avoidanceForce / starshipRigidbody.mass;
        if (avoidanceForce.sqrMagnitude < values.moveEpsilon * values.moveEpsilon)
            avoidanceForce = Vector3.zero;
        //Profiler.EndSample();
        return avoidanceForce * values.collisionAvoidanceMult;
    }

    private Quaternion LookVelocity()
    {
        isTargeting = false;
        Quaternion lookRotation = Quaternion.LookRotation(starshipRigidbody.velocity);   
        desiredRotation = lookRotation;
        return Quaternion.Lerp(transform.rotation, lookRotation, values.maxAngularAcc * Time.deltaTime);
    }

    private Quaternion LookTarget()
    {
        isTargeting = false;
        Quaternion lookRotation;
        if (evade)
            lookRotation = Quaternion.LookRotation(starshipRigidbody.velocity);
        else
        {
            lookRotation = Quaternion.LookRotation(target - transform.position);
            desiredRotation = lookRotation;
        }                  
        return Quaternion.Lerp(transform.rotation, lookRotation, values.maxAngularAcc * Time.deltaTime);
    }

    private Quaternion LookPursueTarget()
    {
        isTargeting = false;
        Quaternion lookRotationPred;
        Quaternion lookRotationVel = Quaternion.LookRotation(starshipRigidbody.velocity);

        if (evade || !transformTarget)
            desiredRotation = lookRotationVel;
        else
        {
            float prediction = distToTarget / projectileSpeed;
            attackPredictedTarget = transformTarget.position + (transformTargetRigidbody.velocity * prediction);
            
            lookRotationPred = Quaternion.LookRotation(attackPredictedTarget - transform.position);
            if(Quaternion.Angle(lookRotationPred, lookRotationVel) < values.maxLookRotationAngle)
            {
                desiredRotation = lookRotationPred;
                isTargeting = true;
            }               
            else
                desiredRotation = lookRotationVel;
        }
        return Quaternion.Lerp(transform.rotation, desiredRotation, values.maxAngularAcc * Time.deltaTime);
    }

    public void SetDestination(Vector3 _target)
    {
        useTransformTarget = false;
        target = _target;
        targetPlane = new Plane(transform.position - target, target);
        isMoving = true;
        pursuing = false;
        starshipRigidbody.constraints = RigidbodyConstraints.FreezeRotation;
    }

    public void SetDestination(Transform _targetTransform)
    {
        useTransformTarget = true;
        transformTarget = _targetTransform;
        transformTargetRigidbody = transformTarget.GetComponent<Rigidbody>();
        target = transformTarget.position;
        targetPlane = new Plane(transform.position - target, target);
        isMoving = true;
        pursuing = false;
        starshipRigidbody.constraints = RigidbodyConstraints.FreezeRotation;
    }

    public void SetDestinationFormation(Vector3 _target, FormationHelper _formationHelper, bool _pursuing = false)
    {
        formationHelper = _formationHelper;
        useTransformTarget = false;
        target = _target;
        targetPlane = new Plane(transform.position - target, target);
        isMoving = true;
        pursuing = _pursuing;
        starshipRigidbody.constraints = RigidbodyConstraints.FreezeRotation;    
    }

    public void SetDestinationFormation(Transform _targetTransform, FormationHelper _formationHelper, bool _pursuing = false)
    {
        formationHelper = _formationHelper;
        useTransformTarget = true;
        transformTarget = _targetTransform;
        transformTargetRigidbody = transformTarget.GetComponent<Rigidbody>();
        target = transformTarget.position;
        targetPlane = new Plane(transform.position - target, target);
        isMoving = true;
        pursuing = _pursuing;
        starshipRigidbody.constraints = RigidbodyConstraints.FreezeRotation;      
    }

    public void SetMoveBehaviorInFormation()
    {
        currentMoveBehavior = new MoveBehavior[5];
        currentMoveBehavior[0] = new MoveBehavior(SeekPursuitAuto);
        //currentMoveBehavior[1] = new MoveBehavior(Separation);
        currentMoveBehavior[1] = new MoveBehavior(SeparationPhysics);
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
        starshipRigidbody.constraints = RigidbodyConstraints.FreezeAll;
    }

    public void TransitiveBumping(Collision collision)
    {
        if (allowTransitiveBumping && isMoving)//transitive bumping
        {
            StarshipSteering otherSteering = collision.transform.GetComponent<StarshipSteering>();
            if (shipsInFormation.Count > 0 && shipsInFormation.Any(t => t == collision.transform) && !otherSteering.isMoving)
            {
                SetStop();
            }
            else if (Vector3.SqrMagnitude(otherSteering.target - target) < 25 && !otherSteering.isMoving)
            {
                SetStop();
            }
        }
    }

    public void OnCollisionEnter(Collision collision)
    {
        TransitiveBumping(collision);
    }

    public void OnCollisionStay(Collision collision)
    {
        TransitiveBumping(collision);
    }
}
