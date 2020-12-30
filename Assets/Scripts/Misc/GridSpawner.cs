using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridSpawner : MonoBehaviour
{
    public GameObject prefab;
    public int gridWidth = 10;
    public int gridHeight = 10;
    public float gridSeparation = 2;
    private List<Transform> ships;
    private FormationHelper formationHelper;

    public bool setMoveAtStart;
    public Transform setMoveDestination;

    public UnitBehavior unitBehavior;


    void Start()
    {
        ships = new List<Transform>();
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                /*Transform spawnedObject = Instantiate(prefab, 
                    new Vector3(transform.position.x + (x * gridSeparation), transform.position.y, transform.position.z + (z * gridSeparation)), 
                    transform.rotation).transform;*/

                Transform spawnedObject = Instantiate(prefab,
                    new Vector3(transform.position.x , transform.position.y + (x * gridSeparation), transform.position.z + (z * gridSeparation)),
                    transform.rotation).transform;
                ships.Add(spawnedObject);
            }
        }

        formationHelper = new FormationHelper(ships);

        if(ships[0].GetComponent<StarshipAI>().isPlayer)
        {
            //EnemyAI.Instance.playerFormations.Add(ships.ToArray());
            SelectionManager.instance.playerFormations.Add(formationHelper);
        }
        else
        {
            SelectionManager.instance.enemyFormations.Add(ships.ToArray());
            //EnemyAI.Instance.enemyFormations.Add(ships.ToArray());
        }

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

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(transform.position, 1);
    }
}
