using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FormationHelper
{
    public List<Transform> shipsInFormation;//change to private later, with setter and getter

    public List<Transform> GetShipsInFormation()
    {
        shipsInFormation.RemoveAll(item => item == null);
        return shipsInFormation;
    }
}
