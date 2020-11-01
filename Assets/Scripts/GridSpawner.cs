using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridSpawner : MonoBehaviour
{
    public GameObject block1;
    public int worldWidth = 10;
    public int worldHeight = 10;
    public float separation = 2;
    private List<Transform> ships;
    private FormationHelper formationHelper;

    public bool setMoveAtStart;
    public Transform setMoveDestination;

    public UnitBehavior unitBehavior;


    void Start()
    {
        ships = new List<Transform>();
        for (int x = 0; x < worldWidth; x++)
        {
            for (int z = 0; z < worldHeight; z++)
            {
                Transform block = Instantiate(block1, new Vector3(transform.position.x + (x * separation), 0, transform.position.z + (z * separation)), block1.transform.rotation).transform;
                ships.Add(block);
            }
        }

        formationHelper = new FormationHelper();
        formationHelper.shipsInFormation = ships;

        foreach (Transform tr in ships)
        {
            StarshipAI starshipAI = tr.GetComponent<StarshipAI>();
            starshipAI.formationHelper = formationHelper;
            starshipAI.unitBehavior = unitBehavior;
        }
    }
    
    private void Update()
    {
        foreach (Transform tr in ships)
        {
            StarshipAI starshipAI = tr.GetComponent<StarshipAI>();
            starshipAI.formationHelper = formationHelper;
            if (setMoveAtStart)
            {
                Transform[] shipsInFormation = new Transform[ships.Count - 1];
                int count = 0;
                foreach (Transform ship in shipsInFormation)
                    if (ship != tr)
                    {
                        shipsInFormation[count] = ship;
                        count++;
                    }
                starshipAI.SetMove(setMoveDestination.position, formationHelper);
            }
        }
        this.enabled = false;
    }
}
