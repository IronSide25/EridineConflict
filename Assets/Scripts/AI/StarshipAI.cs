using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum UnitBehavior { Aggresive, Defensive, Passive }
public enum StarshipClass { Light, Medium, Heavy };

public class StarshipAI : MonoBehaviour
{
    private StarshipSteering starshipSteering;
    public FormationHelper formationHelper;
    public StarshipClass starshipClass;
    public int typeIndex;
    public UnitBehavior unitBehavior = UnitBehavior.Aggresive;
    private StarshipClass currentEnemyClass;

    public bool isAttacking;
    public bool isPlayer;
    public float attackDistance;
    private bool ignoreFormationIsSet = false;

    private const float timeBetweenSphereCast = 0.5f;
    private float lastSphereCast;

    private Transform target;
    private Vector3 lastTargetPos;

    public bool isInCloseProximity = false;//is evading or stopped
    public float maxEvadeTime;
    public float maxEvadeTimeDeviation;
    private float lastEvadeTime;

    private IWeapon[] weapons;
    private LineRenderer lineRendererMoveHint;
    public Material lineGreen;
    public Material lineRed;
    public bool isSelected;

    private Vector3 fightPivot; //prevents starship from moving away during fight
    public FormationHelper targetFormationHelper;

    [Header("Attack distances")]
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
    }

    // Update is called once per frame
    void Update()
    {
        if (isAttacking)
        {
            if (target != null)
            {
                if(!ignoreFormationIsSet)
                {
                    if(Vector3.SqrMagnitude(transform.position - target.position) < attackDistance * attackDistance)
                    {
                        starshipSteering.SetMoveBehaviorIgnoreFormation();
                        ignoreFormationIsSet = true;
                    }
                }                

                if (starshipClass == StarshipClass.Heavy)
                    AttackStopTactics();
                else
                    AttackEvadeTactics();
            }
            else
            {
                //target has been destroyed, try to find another target in this enemy formation
                bool newTargetSet = false;
                if (targetFormationHelper != null)
                {
                    List<Transform> enemyFormationShips = targetFormationHelper.GetShipsInFormationRemoveNull();
                    if (enemyFormationShips.Count > 0)
                    {
                        SetAttack(enemyFormationShips[Random.Range(0, enemyFormationShips.Count)].transform, formationHelper, false);
                        newTargetSet = true;
                    }
                }
                //try to find closest enemy
                //set others target formation helpers for the same formation
                if (!newTargetSet)
                {
                    Collider[] hitColliders = Physics.OverlapSphere(transform.position, attackDistance, isPlayer ? 1 << 10 : 1 << 8);
                    if (hitColliders.Length > 0)
                    {
                        Transform target = hitColliders[Random.Range(0, hitColliders.Length)].transform.root;
                        formationHelper.SetFormationTarget(target.GetComponent<StarshipAI>().formationHelper);
                        SetAttack(target, formationHelper, false);
                    }                  
                    else
                    {
                        EndAttack();
                    }
                }
            }
        }
        else// not attacking
        {
            if(Time.time - lastSphereCast > timeBetweenSphereCast && (unitBehavior == UnitBehavior.Aggresive))
            {
                lastSphereCast = Time.time;
                Collider[] hitColliders = Physics.OverlapSphere(transform.position, attackDistance, isPlayer ? 1 << 10 : 1 << 8);
                if (hitColliders.Length > 0)
                {
                    Transform enemyTransform = hitColliders[Random.Range(0, hitColliders.Length)].attachedRigidbody.transform;
                    StarshipAI enemyStarshipAI = enemyTransform.GetComponent<StarshipAI>();
                    if (enemyStarshipAI.formationHelper != null)
                    {
                        List<Transform> enemyFormationList = enemyStarshipAI.formationHelper.GetShipsInFormationRemoveNull();
                        SetAttack(enemyFormationList[Random.Range(0, enemyFormationList.Count)].transform, formationHelper);
                        formationHelper.SetFormationAttack(target.GetComponent<StarshipAI>().formationHelper);
                    }                       
                    else
                    {
                        SetAttack(enemyTransform, formationHelper);
                        formationHelper.SetFormationAttack(target.GetComponent<StarshipAI>().formationHelper);
                    }                     
                }
            }            
        }

        if (lineRendererMoveHint)
        {
            if ((starshipSteering.isMoving || isAttacking) && isSelected)
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
        if (!isInCloseProximity)
        {
            if (Vector3.SqrMagnitude(transform.position - target.position) < currentMinSqrDistance)
            {
                //ESCAPE
                Vector3 dir = transform.position - starshipSteering.target;
                dir = Quaternion.AngleAxis(25f, Random.insideUnitCircle) * dir;

                starshipSteering.SetDestinationFormation((fightPivot + (dir.normalized * 150)), starshipSteering.formationHelper);
                isInCloseProximity = true;
                lastEvadeTime = Time.time + Random.Range(-maxEvadeTimeDeviation, maxEvadeTimeDeviation);
                starshipSteering.SetRotationBehaviorLookVelocity();

            }
        }
        else
        {
            if (Vector3.SqrMagnitude(transform.position - target.position) > currentMaxSqrDistance || Time.time - lastEvadeTime > maxEvadeTime)
            {
                //ATTACK
                starshipSteering.SetDestinationFormation(target, starshipSteering.formationHelper, true);
                isInCloseProximity = false;
                starshipSteering.SetRotationBehaviorLookPursueTarget();
            }
        }
    }

    private void AttackStopTactics()
    {
        if (!isInCloseProximity)
        {
            if (Vector3.SqrMagnitude(transform.position - target.position) < currentMinSqrDistance)
            {
                isInCloseProximity = true;
                starshipSteering.SetStop();
            }
        }
        else
        {
            if (Vector3.SqrMagnitude(transform.position - target.position) > currentMaxSqrDistance)
            {
                starshipSteering.SetDestinationFormation(target, starshipSteering.formationHelper);
                isInCloseProximity = false;
                starshipSteering.SetRotationBehaviorLookPursueTarget();
                starshipSteering.pursuing = true;
            }
        }
    }

    public void SetAttack(Transform _target, FormationHelper _formationHelper, bool setFightPivot = true)
    {
        if(!isPlayer)
        {
            unitBehavior = UnitBehavior.Aggresive;
        }

        isAttacking = true;
        isInCloseProximity = false;
        target = _target;
        starshipSteering.SetDestinationFormation(_target, _formationHelper);
        lastTargetPos = _target.position;
        foreach (IWeapon weapon in weapons)
            weapon.Activate(_target);
        starshipSteering.pursuing = true;
        starshipSteering.SetRotationBehaviorLookPursueTarget();
        starshipSteering.allowStopOnDistance = false;
        starshipSteering.allowTransitiveBumping = false;
        starshipSteering.collideWithFormationUnits = true;
        formationHelper = _formationHelper;

        StarshipAI targetStarshipAI = target.GetComponent<StarshipAI>();
        if (targetStarshipAI)
        {
            targetFormationHelper = targetStarshipAI.formationHelper;
            currentEnemyClass = targetStarshipAI.starshipClass;
            Vector3 enemyCenterOfMass = targetFormationHelper.GetCenterOfMass();
            if (setFightPivot)
            {
                fightPivot = enemyCenterOfMass + (formationHelper.GetCenterOfMass() - enemyCenterOfMass) / 2;
            }
            if (currentEnemyClass == StarshipClass.Light)
            {
                currentMinSqrDistance = minSqrDistanceVsLight;
                currentMaxSqrDistance = maxSqrDistanceVsLight;
            }
            else if (currentEnemyClass == StarshipClass.Medium)
            {
                currentMinSqrDistance = minSqrDistanceVsMedium;
                currentMaxSqrDistance = maxSqrDistanceVsMedium;
            }
            else
            {
                currentMinSqrDistance = minSqrDistanceVsHeavy;
                currentMaxSqrDistance = maxSqrDistanceVsHeavy;
            }
        }
        else
        {
            if (setFightPivot)
            {
                fightPivot = target.position + (formationHelper.GetCenterOfMass() - target.position) / 2;
            }
        }
    }

    public void EndAttack()
    {
        if(isAttacking)
        {
            foreach (IWeapon weapon in weapons)
                weapon.Deactivate();
            isAttacking = false;
            starshipSteering.SetDestinationFormation(lastTargetPos, starshipSteering.formationHelper);
            starshipSteering.allowStopOnDistance = true;
            starshipSteering.allowTransitiveBumping = true;
            starshipSteering.collideWithFormationUnits = false;
        }       
    }

    public void SetMove(Vector3 _target, FormationHelper _formationHelper)
    {
        foreach (IWeapon weapon in weapons)
            weapon.Deactivate();
        starshipSteering.SetDestinationFormation(_target, _formationHelper);
        lastTargetPos = _target;
        starshipSteering.SetMoveBehaviorInFormation();
        ignoreFormationIsSet = false;
        isAttacking = false;        
        formationHelper = _formationHelper;
        starshipSteering.allowStopOnDistance = true;
        starshipSteering.allowTransitiveBumping = true;
        starshipSteering.collideWithFormationUnits = false;        
    }

    public void OnDamageReceived(Transform transform)
    {
        if (!isAttacking && unitBehavior != UnitBehavior.Passive)
            SetAttack(transform, formationHelper);
    }

    public void OnDestroy()
    {
        formationHelper.RemoveShip(transform);
    }
}

