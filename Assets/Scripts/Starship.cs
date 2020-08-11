using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//do każdego zachowania
// - maxForce
// - multiplier


public class Starship : MonoBehaviour
{

    Rigidbody rigidbody;
    public bool isMoving;
    public Vector3 target;
    public float maxVelocity = 1;
    public float maxSpeed = 1;
    public float maxForce = 1;
    public float maxAngularAcc = 1;
    public float moveEpsilon = 0.1f;

    public float distanceToStop = 1;
    public float slowingRadius = 5;


    public Vector3 calculatedVelocity;
    public Vector3 velocity;

    //avoidance
    public float maxAvoidForce;
    public float sphereCastRadius;
    public float sphereCastDistance;

    //separation
    
    public float threshold = 2f;
    public float decayCoefficient = -25f;
    public float separationForceMultiplier = 1f;

    //cohesion
    public float viewAngle = 60;
    public float cohesionForceMultiplier = 1f;

    //alingment
    public float alignDistance = 10f;
    public float alignmentMultiplier = 5;

    //formation
    public Transform[] shipsInFormation;
    public Plane targetPlane;


    private void Awake()
    {
        rigidbody = transform.GetComponent<Rigidbody>();
        Starship[] starships = FindObjectsOfType<Starship>();
        /*targets = new Transform[starships.Length - 1];
        int count = 0;
        foreach (Starship starship in starships)
            if (starship.gameObject != gameObject)
            {
                targets[count] = starship.transform;
                count++;
            }*/
    }

    // Start is called before the first frame update
    void Start()
    {            
        isMoving = false;       
        calculatedVelocity = Vector3.zero;
        SetDestination(transform.position);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (isMoving)
        {
            Vector3 desiredVelocity = target - transform.position;
            float dist = Vector3.Magnitude(desiredVelocity);
            if (dist < slowingRadius)
            {
                target = targetPlane.ClosestPointOnPlane(transform.position);
            }

            Vector3 calculatedVelocity = rigidbody.velocity;
            calculatedVelocity += Seek();

            calculatedVelocity += Separation();
            calculatedVelocity += Cohesion();
            calculatedVelocity += Alignment();

            calculatedVelocity += CollisionAvoidance();
            /*Vector3 collisionAvoidanceVec = CollisionAvoidance();
            calculatedVelocity += collisionAvoidanceVec;*/

            /*if(collisionAvoidanceVec.magnitude < moveEpsilon)
                calculatedVelocity += Separation();*/

            if (calculatedVelocity.magnitude < moveEpsilon)
                calculatedVelocity = Vector3.zero;

            calculatedVelocity = Vector3.ClampMagnitude(calculatedVelocity, maxSpeed);
            rigidbody.velocity = calculatedVelocity;

            Debug.DrawRay(transform.position, rigidbody.velocity, Color.yellow);

            if (calculatedVelocity.magnitude >= moveEpsilon)
                transform.rotation = LookVelocity();

            if (Vector3.SqrMagnitude(transform.position - target) < distanceToStop* distanceToStop)
            {
                isMoving = false;
                rigidbody.constraints = RigidbodyConstraints.FreezeAll;
            }
        }
        else
            rigidbody.constraints = RigidbodyConstraints.FreezeAll;

    }

    private Vector3 Seek()
    {
        Vector3 desiredVelocity = target - transform.position;
        float dist = Vector3.Magnitude(desiredVelocity);
        if (dist < slowingRadius)
            desiredVelocity = Vector3.Normalize(desiredVelocity) * maxVelocity * (dist / slowingRadius);
        else
            desiredVelocity = Vector3.Normalize(desiredVelocity) * maxVelocity;

        Vector3 steering = desiredVelocity - rigidbody.velocity;
        Vector3.ClampMagnitude(steering, maxForce);

        if (dist > slowingRadius)
            steering = steering / rigidbody.mass;

        if (steering.magnitude < moveEpsilon)
            steering = Vector3.zero;

        return Vector3.ClampMagnitude(steering, maxSpeed);
    }

    private Vector3 Separation()
    {
        Vector3 steering = Vector3.zero;
        if(shipsInFormation.Length > 0)
        {
            foreach (Transform ship in shipsInFormation)
            {
                Vector3 direction = ship.position - transform.position;
                float distance = direction.magnitude;
                if (distance < threshold)
                {
                    float strength = Mathf.Min(decayCoefficient / (distance * distance), maxSpeed);
                    direction.Normalize();
                    steering = strength * direction;
                }
            }
            steering *= separationForceMultiplier;
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
            steering *= cohesionForceMultiplier;
        }

        return steering;
    }

    private Vector3 Alignment()
    {
        Vector3 steering = Vector3.zero;
        int count = 0;
        foreach (Transform ship in shipsInFormation)
        {
            Vector3 dir = ship.position - transform.position;
            if(dir.magnitude < alignDistance)
            {
                steering += ship.GetComponent<Rigidbody>().velocity;
                count++;
            }
        }

        if(count > 0)
        {
            steering = steering / count;
            if (steering.magnitude > maxVelocity)
                steering = steering.normalized * maxVelocity;
        }

        Vector3 desiredVelocity = target - transform.position;
        float dist = Vector3.Magnitude(desiredVelocity);
        if (dist < slowingRadius)
            steering *= dist / slowingRadius;



        return steering * alignmentMultiplier;
    }



    private Vector3 CollisionAvoidance()//maybe use calculatedVelocity instead of velocity
    {
        RaycastHit hit;
        Vector3 avoidanceForce = Vector3.zero;
        Vector3 ahead = transform.position + (rigidbody.velocity.normalized * (sphereCastDistance + sphereCastRadius));
        Debug.DrawRay(transform.position, rigidbody.velocity.normalized * (sphereCastDistance + sphereCastRadius), Color.red);
        if (Physics.SphereCast(transform.position, sphereCastRadius, rigidbody.velocity.normalized, out hit, sphereCastDistance, 1 << 8))
        {

            if (hit.transform != transform)
            {                
                avoidanceForce = ahead - hit.transform.position;
                Debug.DrawRay(transform.position, avoidanceForce, Color.green);
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

        if (avoidanceForce.magnitude < moveEpsilon)
            avoidanceForce = Vector3.zero;

        return avoidanceForce;
    }

    private Quaternion LookVelocity()
    {
        Quaternion lookRotation = Quaternion.LookRotation(rigidbody.velocity);      
        return Quaternion.Lerp(transform.rotation, lookRotation, maxAngularAcc * Time.deltaTime);
    }

    public void SetDestination(Vector3 _target)
    {
        target = _target;
        targetPlane = ConstructPlane();
        isMoving = true;
        rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
    }

    public void SetDestinationFormation(Vector3 _target, Transform[] _shipsInFormation)
    {
        target = _target;
        targetPlane = ConstructPlane();
        isMoving = true;
        rigidbody.constraints = RigidbodyConstraints.FreezeRotation;

        shipsInFormation = new Transform[_shipsInFormation.Length - 1];
        int count = 0;
        foreach (Transform ship in _shipsInFormation)
            if (ship != transform)
            {
                shipsInFormation[count] = ship;
                count++;
            }
    }

    public Plane ConstructPlane()
    {
        Vector3 inNormal = transform.position - target;
        return new Plane(inNormal, target);
    }

    public void OnCollisionEnter(Collision collision)
    {
        if (shipsInFormation.Length > 0)
            if (Array.Exists(shipsInFormation, t => t == collision.transform))
                if (!isMoving)
                {
                    collision.transform.GetComponent<Starship>().isMoving = false;
                    rigidbody.constraints = RigidbodyConstraints.FreezeAll;
                }                  
    }

    public void OnCollisionStay(Collision collision)
    {
        
    }

    //linePnt - point the line passes through
    //lineDir - unit vector in direction of line, either direction works
    //pnt - the point to find nearest on line for
    public static Vector3 NearestPointOnLine(Vector3 linePnt, Vector3 lineDir, Vector3 pnt)
    {
        lineDir.Normalize();//this needs to be a unit vector
        var v = pnt - linePnt;
        var d = Vector3.Dot(v, lineDir);
        return linePnt + lineDir * d;
    }
}
