using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FormationHelper// add set move set attack
{
    public static List<FormationHelper> formationHelpers;//lepiej to zamien na listę w selectionmanager

    //maybe change to hashset
    public List<Transform> shipsInFormation;//change to private later, with setter and getter
    public Vector3 centerOfMass;
    public Vector3 averageVelocity;
    private bool massCacheIsValid;
    private bool velocityCacheIsValid;

    public FormationHelper()
    {
        shipsInFormation = new List<Transform>();
        massCacheIsValid = false;
        velocityCacheIsValid = false;

        if (formationHelpers == null)
            formationHelpers = new List<FormationHelper>();
        formationHelpers.Add(this);
    }

    public FormationHelper(List<Transform> _shipsInFormation)
    {
        shipsInFormation = _shipsInFormation;
        massCacheIsValid = false;
        velocityCacheIsValid = false;

        if (formationHelpers == null)
            formationHelpers = new List<FormationHelper>();
        formationHelpers.Add(this);
    }

    ~FormationHelper()
    {
        formationHelpers.Remove(this);
    }

    public List<Transform> GetShipsInFormationRemoveNull()
    {
        shipsInFormation.RemoveAll(item => item == null);
        return shipsInFormation;
    }

    public void RemoveShip(Transform ship)
    {
        shipsInFormation.Remove(ship);
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

    public void InvalidateCache()
    {
        massCacheIsValid = false;
        velocityCacheIsValid = false;
    }

    public Vector3 GetCenterOfMass()//moze spróbuj jakoś odjąć pozycję pytającego statku 
    {
        if (massCacheIsValid)
            return centerOfMass;
        else
        {
            //Debug.Log("Recalculating center of mass");
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

    public Vector3 GetAverageVelocity()//moze spróbuj jakoś odjąć pozycję pytającego statku 
    {
        if (velocityCacheIsValid)
            return averageVelocity;
        else
        {
            //Debug.Log("Recalculating average velocity");
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
