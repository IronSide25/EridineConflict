using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FormationHelper// add set move set attack
{
    //maybe change to hashset
    public List<Transform> shipsInFormation;//change to private later, with setter and getter

    public FormationHelper()
    {
        shipsInFormation = new List<Transform>();
    }

    public FormationHelper(List<Transform> _shipsInFormation)
    {
        shipsInFormation = _shipsInFormation;
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
}
