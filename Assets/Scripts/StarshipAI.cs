﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum AttackType1Phase { Seek, Evade }
public enum AttackType2Phase { Seek, Stop }

public enum UnitBehavior { Aggresive, Defence, Passive }
public enum StarshipClass { Light, Medium, Heavy };

public class StarshipAI : MonoBehaviour
{
    private StarshipSteering starshipSteering;
    public FormationHelper formationHelper;//not sure about that
    public StarshipClass starshipClass;
    private StarshipClass currentEnemyClass;

    public bool isAttacking;
    public bool isShooting;
    public bool isPlayer;
    public float attackDistance;

    private const float timeBetweenSphereCast = 0.5f;
    private float lastSphereCast;

    public Transform target;
    public Vector3 lastTargetPos;

    public bool isInCloseProximity = false;//is evading or stopped
    public float maxEvadeTime;
    public float maxEvadeTimeDeviation;
    float lastEvadeTime;

    IWeapon[] weapons;
    LineRenderer lineRendererMoveHint;
    public Material lineGreen;
    public Material lineRed;
    public bool isSelected;

    public Vector3 fightPivot; //prevents starship from moving away during fight
    public FormationHelper targetFormationHelper;

    public float minSqrDistanceVsLight;
    public float maxSqrDistanceVsLight;
    public float minSqrDistanceVsMedium;
    public float maxSqrDistanceVsMedium;
    public float minSqrDistanceVsHeavy;
    public float maxSqrDistanceVsHeavy;
    private float currentMinSqrDistance;
    private float currentMaxSqrDistance;

    // Start is called before the first frame update
    void Start()
    {
        starshipSteering = GetComponent<StarshipSteering>();
        weapons = GetComponentsInChildren<IWeapon>();
        starshipSteering.projectileSpeed = weapons[0].GetProjectileSpeed();
        isInCloseProximity = false;
        if (gameObject.layer == 8)
            lineRendererMoveHint = GetComponent<LineRenderer>();
        isSelected = false;
        lastSphereCast = Time.time;

        if (gameObject.layer == 8)
            isPlayer = true;
        else
            isPlayer = false;
        /*starshipSteering.maxDesiredPursuitForce = 10;
        starshipSteering.maxDesiredSeekForce = 10;*/
    }

    // Update is called once per frame
    void Update()
    {
        if (isAttacking)
        {
            if (target != null)
            {
                if (starshipClass == StarshipClass.Heavy)
                    AttackStopTactics();
                else
                    AttackEvadeTactics();
            }
            else//target has been destroyed, try to find another target
            {
                bool newTargetSet = false;
                if (targetFormationHelper != null)
                {
                    List<Transform> enemyFormation = targetFormationHelper.GetShipsInFormation();
                    if (enemyFormation.Count > 0)
                    {
                        SetAttack(enemyFormation[Random.Range(0, enemyFormation.Count)].transform, starshipSteering.shipsInFormation, formationHelper, false);
                        newTargetSet = true;
                    }
                }
                if (!newTargetSet)
                {
                    Collider[] hitColliders = Physics.OverlapSphere(transform.position, attackDistance, isPlayer ? 1 << 10 : 1 << 8);
                    if (hitColliders.Length > 0)
                        SetAttack(hitColliders[Random.Range(0, hitColliders.Length)].transform, starshipSteering.shipsInFormation, formationHelper, false);
                    else
                    {
                        EndAttack();
                    }
                }
            }
        }
        else
        {
            if(Time.time - lastSphereCast > timeBetweenSphereCast)
            {
                lastSphereCast = Time.time;
                Collider[] hitColliders = Physics.OverlapSphere(transform.position, attackDistance, isPlayer ? 1 << 10 : 1 << 8);
                if (hitColliders.Length > 0)
                {
                    Transform enemyTransform = hitColliders[Random.Range(0, hitColliders.Length)].transform;
                    StarshipAI enemyStarshipAI = enemyTransform.GetComponent<StarshipAI>();
                    if (enemyStarshipAI.formationHelper != null)
                    {
                        List<Transform> enemyFormationList = enemyStarshipAI.formationHelper.GetShipsInFormation();
                        SetAttack(enemyFormationList[Random.Range(0, enemyFormationList.Count)].transform, starshipSteering.shipsInFormation, formationHelper);
                    }                       
                    else
                        SetAttack(enemyTransform, starshipSteering.shipsInFormation, formationHelper);
                }
            }            
        }

        if (lineRendererMoveHint)//zapytaj czy to powinno być tutaj czy gdzie indziej
        {
            if (starshipSteering.isMoving && isSelected)
            {
                lineRendererMoveHint.enabled = true;
                if (isAttacking && target)
                {
                    lineRendererMoveHint.SetPositions(new Vector3[] { transform.position, target.position });
                    lineRendererMoveHint.material = lineRed;
                }
                else
                {
                    lineRendererMoveHint.SetPositions(new Vector3[] { transform.position, starshipSteering.target });
                    lineRendererMoveHint.material = lineGreen;
                }
            }
            else
            {
                lineRendererMoveHint.enabled = false;
            }
        }
    }

    private void AttackEvadeTactics()
    {
        lastTargetPos = target.position;
        if (!isInCloseProximity)
        {
            if (Vector3.SqrMagnitude(transform.position - target.position) < currentMinSqrDistance /*|| target.GetComponent<StarshipAI>().isEvading*/)
            {
                Vector3 dir = transform.position - starshipSteering.target;
                //dir = dir.normalized + new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f));//randomize
                //dir = Quaternion.Euler(Random.Range(-90, 90), Random.Range(-90, 90), Random.Range(-90, 90)) * dir;
                dir = Quaternion.AngleAxis(90f, Random.insideUnitCircle) * dir;

                //Vector3 newpos = new Vector3(93, -1, 9.3f);   
                //starshipSteering.SetDestinationFormation((transform.position + (dir.normalized * 150)), starshipSteering.shipsInFormation);
                starshipSteering.SetDestinationFormation((fightPivot + (dir.normalized * 150)), starshipSteering.shipsInFormation);// i think thats not optimal, change only steering target
                isInCloseProximity = true;
                lastEvadeTime = Time.time + Random.Range(-maxEvadeTimeDeviation, maxEvadeTimeDeviation);
                starshipSteering.rotateToTarget = false;
                //starshipSteering.SetStop();
                //starshipSteering.evade = true;
            }
        }
        else
        {
            if (Vector3.SqrMagnitude(transform.position - target.position) > currentMaxSqrDistance || Time.time - lastEvadeTime > maxEvadeTime)
            {
                starshipSteering.SetDestinationFormation(target, starshipSteering.shipsInFormation);
                isInCloseProximity = false;
                starshipSteering.rotateToTarget = true;
                starshipSteering.pursuing = true;
            }
        }
    }

    private void AttackStopTactics()
    {
        lastTargetPos = target.position;
        if (!isInCloseProximity)
        {
            if (Vector3.SqrMagnitude(transform.position - target.position) < currentMinSqrDistance)
            {
                isInCloseProximity = true;
                starshipSteering.SetStop();
                //starshipSteering.rotateToTarget = false;
                //starshipSteering.SetStop();
                //starshipSteering.evade = true;
            }
        }
        else
        {
            if (Vector3.SqrMagnitude(transform.position - target.position) > currentMaxSqrDistance)
            {
                starshipSteering.SetDestinationFormation(target, starshipSteering.shipsInFormation);
                isInCloseProximity = false;
                starshipSteering.rotateToTarget = true;
                starshipSteering.pursuing = true;
            }
        }
    }

    public void SetAttack(Transform _target, Transform[] _shipsInFormation, FormationHelper _formationHelper, bool setFightPivot = true)
    {
        isAttacking = true;
        isInCloseProximity = false;
        target = _target;
        starshipSteering.SetDestinationFormation(_target, _shipsInFormation);
        foreach (IWeapon weapon in weapons)
            weapon.Activate(_target);
        starshipSteering.pursuing = true;
        starshipSteering.rotateToTarget = true;
        starshipSteering.allowStopOnDistance = false;
        starshipSteering.allowTransitiveBumping = false;
        formationHelper = _formationHelper;
        if (setFightPivot)
            fightPivot = target.position + (transform.position - target.position) / 2;

        StarshipAI targetStarshipAI = target.GetComponent<StarshipAI>();
        if (targetStarshipAI)
        {
            targetFormationHelper = targetStarshipAI.formationHelper;
            currentEnemyClass = targetStarshipAI.starshipClass;

            if (currentEnemyClass == StarshipClass.Light)
            {
                currentMinSqrDistance = minSqrDistanceVsLight;
                currentMaxSqrDistance = maxSqrDistanceVsLight;
                starshipSteering.SetMoveBehaviorIgnoreFormation();
            }
            else if (currentEnemyClass == StarshipClass.Medium)
            {
                currentMinSqrDistance = minSqrDistanceVsMedium;
                currentMaxSqrDistance = maxSqrDistanceVsMedium;
                starshipSteering.SetMoveBehaviorIgnoreFormation();
            }
            else if (currentEnemyClass == StarshipClass.Heavy)
            {
                currentMinSqrDistance = minSqrDistanceVsHeavy;
                currentMaxSqrDistance = maxSqrDistanceVsHeavy;
                starshipSteering.SetMoveBehaviorIgnoreFormation();
            }
            else//delete this after testing
            {
                currentMinSqrDistance = minSqrDistanceVsMedium;
                currentMaxSqrDistance = maxSqrDistanceVsMedium;
                starshipSteering.SetMoveBehaviorIgnoreFormation();
            }
        }
    }

    public void EndAttack()
    {
        foreach (IWeapon weapon in weapons)
            weapon.Deactivate();
        isAttacking = false;
        starshipSteering.SetDestinationFormation(lastTargetPos, starshipSteering.shipsInFormation);
        starshipSteering.allowStopOnDistance = true;
        starshipSteering.allowTransitiveBumping = true;
    }

    public void SetMove(Vector3 target, Transform[] _shipsInFormation, FormationHelper _formationHelper)
    {
        foreach (IWeapon weapon in weapons)
            weapon.Deactivate();
        starshipSteering.SetDestinationFormation(target, _shipsInFormation);
        starshipSteering.SetMoveBehaviorInFormation();
        isAttacking = false;
        lastTargetPos = target;
        formationHelper = _formationHelper;
        starshipSteering.allowStopOnDistance = true;
        starshipSteering.allowTransitiveBumping = true;
    }
}

