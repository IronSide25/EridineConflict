using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/StarshipDataScriptableObject", order = 1)]
public class StarshipDataScriptableObject : ScriptableObject
{
    public float maxSpeed = 1;
    public float accelerationRate;
    public float noMovingMaxAngularAcc = 0.1f;
    public float moveEpsilon = 0.1f;
    public float distanceToStop = 1;
    public float slowingRadius = 5;

    [Header("Seek behavior")]
    public float maxSeekForce;
    public float seekMult = 1;
    public bool limitSeekRotation = false;
    public float allowedSeekRotation = 1.5708f;//90 degr

    [Header("Pursuit behavior")]
    public float maxPrediction = 100;
    public float maxPursuitForce;
    public float pursuitMult = 1;
    public float projectileSpeed;//not SO
    public bool limitPursuitRotation = false;
    public float allowedPursuitRotation = 1.5708f;//90 degr

    [Header("Look behavior")]
    public float maxAngularAcc = 1;

    [Header("Collision avoidance behavior")]
    public float maxAvoidForce;
    public float sphereCastRadius;
    public float sphereCastDistance;
    public float collisionAvoidanceMult = 1;
    public bool collideWithFormationUnits = true;//not SO

    [Header("Separation behavior")]
    //public float threshold = 2f;
    public float thresholdSqr = 4;
    public float decayCoefficient = -25f;
    public float separationForceMultiplier = 1f;
    public float maxSeparationForce;
    public float separationStrength;

    [Header("Cohesion behavior")]
    //public float viewAngle = 60;//remove
    public float maxCohesionForce = 1f;
    public float cohesionForceMultiplier = 1f;

    [Header("Alingment behavior")]
    public float alignDistance = 10f;
    public float alignmentMultiplier = 5;
    public float maxAlingmentForce;
}
