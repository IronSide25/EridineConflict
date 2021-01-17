using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    public bool useExhaustive = true;
    public bool autoAttack;
    public float attackDelay;
    private bool targetsAssigned;

    private List<Transform[]> enemyFormations;//weapons
    private List<FormationHelper> playerFormations;//targets
    private float[] playerFormationsCountBefore;
    private float[] playerFormationsCountAfter;

    private static float[] starshipTypesValues = new float[] { 5f, 5f, 25f, 25f, 100f };//dostroić to potem   zamiast 100 daj 70
    private static float[,] starshipTypesEffectiveness = new float[,] {
            {0.5f, 0.7f, 0.1f, 0.15f, 0.05f },//small fighter
            {0.3f, 0.5f, 0.25f, 0.35f, 0.10f },//small bomber
            {0.9f, 0.75f, 0.5f, 0.7f, 0.20f },//medium bomber
            {0.8f, 0.7f, 0.4f, 0.5f, 0.35f },//medium destroyer
            {0.95f, 0.9f, 0.80f, 0.65f, 0.5f }};//large cruiser

  
    // Start is called before the first frame update
    void Start()
    {
        targetsAssigned = false;
        if (autoAttack)
            Invoke("AssignTargets", attackDelay);
    }

    public void AssignTargets()
    {
        if(!targetsAssigned)
        {
            playerFormations = SelectionManager.instance.playerFormations.ToList();
            enemyFormations = SelectionManager.instance.enemyFormations;

            UnifyPlayerFormations();
            //Debug.Log("Unified player formations count: " + playerFormations.Count);

            playerFormationsCountBefore = new float[playerFormations.Count];
            playerFormationsCountAfter = new float[playerFormations.Count];
            for (int i = 0; i < playerFormations.Count; i++)
            {
                playerFormationsCountBefore[i] = playerFormations[i].GetLength();
            }

            int[] solution;
            if (useExhaustive)
                solution = ExhaustiveSearch();
            else
                solution = WTAGreedyMMR();

            for (int i = 0; i < enemyFormations.Count; i++)
            {
                //Debug.Log("Enemy Type: " + enemyFormations[i][0].GetComponent<StarshipAI>().typeIndex + " attacks player type: " + playerFormations[solution[i]].shipsInFormation[0].GetComponent<StarshipAI>().typeIndex + " Formation index: " + solution[i]);
                for (int j = 0; j < enemyFormations[i].Length; j++)//each enemy ship in current formation attacks random player ship from attacked player formation
                {
                    FormationHelper targetHelper = playerFormations[solution[i]];
                    enemyFormations[i][j].GetComponent<StarshipAI>().SetAttack(targetHelper.shipsInFormation[UnityEngine.Random.Range(0, targetHelper.shipsInFormation.Count)], enemyFormations[i][j].GetComponent<StarshipAI>().formationHelper);
                }
            }
            targetsAssigned = true;
        }       
    }

    private int[] ExhaustiveSearch()
    {
        int[] solution = new int[enemyFormations.Count];
        float solutionValue = float.PositiveInfinity;

        int noOfIterations = (int)Mathf.Pow(playerFormations.Count, enemyFormations.Count);
        int iterationNo = 0;

        while (iterationNo < noOfIterations)
        {
            int[] possibleAllocations = GetPossibleAllocations(iterationNo);
            float possibleSolutionValue = CalculateSolutionValue(possibleAllocations);
            if (possibleSolutionValue < solutionValue)
            {
                solution = possibleAllocations;
                solutionValue = possibleSolutionValue;
            }
            iterationNo++;
        }
        //Debug.Log("EXHAUSTIVE SEARCH SOLUTION: " + solutionValue);
        return solution;
    }

    private int[] GetPossibleAllocations(int iterationNo)
    {
        List<int> possibleAllocations = new List<int>();
        while (iterationNo > 0)
        {
            possibleAllocations.Add(iterationNo % playerFormations.Count + 0);
            iterationNo = iterationNo / playerFormations.Count;
        }
        while (possibleAllocations.Count < enemyFormations.Count)
        {
            possibleAllocations.Add(0);
        }
        return possibleAllocations.ToArray();
    }

    private float CalculateSolutionValue(int[] solution)
    {
        float enemySurvSum = 0;//rename
        for (int it = 0; it < playerFormations.Count; it++)
        {
            playerFormationsCountAfter[it] = playerFormations[it].GetLength();
        }

        int i = 0;
        while (i < playerFormations.Count)//player formations (targets)
        {
            int k = 0;
            float effectivenessSum = 0;
            float costSum = 0;
            int noUnits = 0;
            while (k < enemyFormations.Count)//enemy formations (weapons)
            {
                if (solution[k] == i)
                {
                    int enemyTypeIndex = enemyFormations[k][0].GetComponent<StarshipAI>().typeIndex;
                    int playerTypeIndex = playerFormations[i].shipsInFormation[0].GetComponent<StarshipAI>().typeIndex;

                    effectivenessSum += starshipTypesEffectiveness[enemyTypeIndex, playerTypeIndex] * enemyFormations[k].Length;
                    costSum += starshipTypesValues[enemyTypeIndex] * enemyFormations[k].Length;
                    noUnits += enemyFormations[k].Length;
                }
                k++;
            }
            
            if (noUnits > 0)
            {
                float averageEffectiveness = effectivenessSum / noUnits;
                float averageSum = costSum / noUnits;
                float predictedSurvivors = GetExpectedSurvivorsValue(noUnits, playerFormations[i].GetLength(), averageEffectiveness);
                if (predictedSurvivors > 0) //player wins, we are counting player survivors, they are making score higher
                {
                    float loss = playerFormationsCountBefore[i] - predictedSurvivors;
                    if (loss > 0)
                        playerFormationsCountAfter[i] -= loss;
                }
                else//enemy wins we are counting enemy survivors, they are lowering the score
                {
                    playerFormationsCountAfter[i] = 0;
                    enemySurvSum += predictedSurvivors * averageSum;
                }
            }
            i++;
        }
        float solutionValue = 0;
        solutionValue = enemySurvSum;
        for (int it = 0; it < playerFormationsCountAfter.Length; it++)
        {          
            if (playerFormationsCountAfter[it] > 0)
            {
                solutionValue += playerFormationsCountAfter[it] * starshipTypesValues[playerFormations[it].shipsInFormation[0].GetComponent<StarshipAI>().typeIndex];
            }              
        }
        return solutionValue;
    }

    private int[] WTAGreedyMMR()
    {
        float[] playerFormationCounts = new float[playerFormations.Count];
        float[] enemyFormationCounts = new float[enemyFormations.Count];
        for (int i = 0; i < playerFormations.Count; i++)
            playerFormationCounts[i] = playerFormations[i].GetLength();
        for (int i = 0; i < enemyFormations.Count; i++)
            enemyFormationCounts[i] = enemyFormations[i].Length;

        int[] solution = new int[enemyFormations.Count];
        float solutionValue = float.PositiveInfinity;
        int k = 0;
        while (k < enemyFormations.Count)
        {
            float maxDecrease = 0;
            int i = 0;
            int allocatedTarget = 0;
            float predictedSurvivorsAllocated = 0;
            while (i < playerFormations.Count)
            {
                if(playerFormationCounts[i] > 0)
                {
                    int enemyTypeIndex = enemyFormations[k][0].GetComponent<StarshipAI>().typeIndex;
                    int playerTypeIndex = playerFormations[i].shipsInFormation[0].GetComponent<StarshipAI>().typeIndex;
                    float predictedSurvivors = GetExpectedSurvivorsValue((int)enemyFormationCounts[k], (int)playerFormationCounts[i], starshipTypesEffectiveness[enemyTypeIndex, playerTypeIndex]);

                    float decrease = 0;//decrease of player army value
                    if (predictedSurvivors > 0) //player wins, we are counting player survivors, they are making score higher
                    {
                        decrease += (playerFormationCounts[i] - Mathf.Abs(predictedSurvivors)) * starshipTypesValues[playerTypeIndex];
                        decrease -= enemyFormationCounts[k] * starshipTypesValues[enemyTypeIndex];
                    }
                    else//enemy wins we are counting enemy survivors, they are lowering 
                    {
                        decrease -= (enemyFormationCounts[k] - Mathf.Abs(predictedSurvivors)) * starshipTypesValues[enemyTypeIndex];//how many cost enemy lost
                        decrease += playerFormationCounts[i] * starshipTypesValues[playerTypeIndex];
                    }
                    if (decrease > maxDecrease)
                    {
                        maxDecrease = decrease;
                        allocatedTarget = i;
                        predictedSurvivorsAllocated = predictedSurvivors;
                    }
                }
                i++;
            }

            if (predictedSurvivorsAllocated > 0) //player wins, we are counting player survivors, they are making score higher
            {
                enemyFormationCounts[k] = 0;
                playerFormationCounts[allocatedTarget] = predictedSurvivorsAllocated;
            }
            else//enemy wins we are counting enemy survivors, they are lowering the score
            {
                playerFormationCounts[allocatedTarget] = 0;
                enemyFormationCounts[k] = predictedSurvivorsAllocated;
            }
            solution[k] = allocatedTarget;
            k++;          
        }
        solutionValue = CalculateSolutionValue(solution);
        //Debug.Log("GREEDY MMR SOLUTION: " + solutionValue);
        return solution;
    }

    private float GetExpectedSurvivorsValue(int enemyCount, int playerCount, float playerAttritionCoef)
    {
        float enemyAttritionCoef = 1 - playerAttritionCoef;
        float relativeEffectivenessEnemy = Mathf.Sqrt(enemyAttritionCoef / playerAttritionCoef);
        float relativeEffectivenessPlayer = Mathf.Sqrt(playerAttritionCoef / enemyAttritionCoef);

        if (((float)enemyCount / playerCount) > relativeEffectivenessEnemy)//enemy wins, player value decreases
        {
            return -Mathf.Sqrt((enemyCount * enemyCount) - (enemyAttritionCoef / playerAttritionCoef) * (playerCount * playerCount));
        }
        else if (((float)enemyCount / playerCount) < relativeEffectivenessEnemy)//player wins, player value increase
        {
            return Mathf.Sqrt((playerCount * playerCount) - (playerAttritionCoef / enemyAttritionCoef) * (enemyCount * enemyCount));
        }
        else
        {
            return 0;
        }
    }

    private void UnifyPlayerFormations()
    {
        List<FormationHelper> tempList = new List<FormationHelper>();
        foreach(FormationHelper helper in playerFormations)
        {
            var groups = helper.shipsInFormation.GroupBy(ship => ship.GetComponent<StarshipAI>().typeIndex);
            if (groups.Count() > 1)
            {
                for(int i = 0; i < groups.Count(); i++)
                {
                    FormationHelper newFormationHelper = new FormationHelper();
                    newFormationHelper.shipsInFormation.AddRange(groups.ElementAt(i));
                    newFormationHelper.RefreshHashSet();
                    tempList.Add(newFormationHelper);
                    //Debug.Log(newFormationHelper.shipsInFormation.Count);
                }
            }
            else
            {
                tempList.Add(helper);
            }
        }
        playerFormations = tempList;
    }

    public void OnTriggerEnter(Collider other)
    {
        AssignTargets();
    }
}
