using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FormationHelper
{
    public static List<FormationHelper> instances;

    public List<Transform> shipsInFormation;
    public HashSet<Transform> shipsHashSet;//for optimization
    private Vector3 centerOfMass;
    private Vector3 averageVelocity;
    private bool massCacheIsValid;
    private bool velocityCacheIsValid;

    public FormationHelper()
    {
        shipsInFormation = new List<Transform>();
        shipsHashSet = new HashSet<Transform>();
        massCacheIsValid = false;
        velocityCacheIsValid = false;

        if (instances == null)
            instances = new List<FormationHelper>();
        instances.Add(this);
    }

    public FormationHelper(List<Transform> _shipsInFormation)
    {
        shipsInFormation = _shipsInFormation;
        shipsHashSet = new HashSet<Transform>(shipsInFormation);
        massCacheIsValid = false;
        velocityCacheIsValid = false;

        if (instances == null)
            instances = new List<FormationHelper>();
        instances.Add(this);
    }

    ~FormationHelper()
    {
        instances.Remove(this);
    }

    public void RefreshHashSet()
    {
        shipsHashSet = new HashSet<Transform>(shipsInFormation);
    }

    public List<Transform> GetShipsInFormationRemoveNull()
    {
        shipsInFormation.RemoveAll(item => item == null);
        shipsHashSet.RemoveWhere(item => item == null);
        return shipsInFormation;
    }

    public void RemoveShip(Transform ship)
    {
        shipsInFormation.Remove(ship);
        shipsHashSet.Remove(ship);
    }

    public int GetLength()
    {
        return shipsInFormation.Count;
    }

    public void SetFormationTarget(FormationHelper enemyFormationHelper)
    {
        foreach(Transform ship in shipsInFormation)
        {
            ship.GetComponent<StarshipAI>().targetFormationHelper = enemyFormationHelper;
        }
    }

    public void SetFormationAttack(FormationHelper enemyFormationHelper)
    {
        foreach (Transform ship in shipsInFormation)
        {
            List<Transform> shipsInFormation = enemyFormationHelper.shipsInFormation;
            ship.GetComponent<StarshipAI>().SetAttack(shipsInFormation[Random.Range(0, shipsInFormation.Count)], this, true);
        }
    }

    public void InvalidateCache()
    {
        massCacheIsValid = false;
        velocityCacheIsValid = false;
    }

    public Vector3 GetCenterOfMass()
    {
        if (massCacheIsValid)
            return centerOfMass;
        else
        {
            int count = 0;
            foreach (Transform ship in shipsInFormation)
            {
                centerOfMass += ship.position;
                count++;
            }
            if (count > 0)
                centerOfMass = centerOfMass / count;
            else
                centerOfMass = Vector3.zero;
            massCacheIsValid = true;
            return centerOfMass;
        }
    }

    public Vector3 GetAverageVelocity()
    {
        if (velocityCacheIsValid)
            return averageVelocity;
        else
        {
            int count = 0;
            foreach (Transform ship in shipsInFormation)
            {
                averageVelocity += ship.GetComponent<Rigidbody>().velocity;
                count++;
            }
            if (count > 0)
                averageVelocity = averageVelocity / count;
            else
                averageVelocity = Vector3.zero;

            velocityCacheIsValid = true;
            return averageVelocity;
        }
    }
}
