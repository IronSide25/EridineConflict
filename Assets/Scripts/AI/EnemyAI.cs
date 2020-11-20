using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EnemyAI : MonoBehaviour//lower - better for enemy!!! we are minimalizing player cost function
{
    public static EnemyAI Instance;

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

    void Awake()
    {
        if (!Instance)
            Instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        Invoke("AssignTargets", 5);
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void AssignTargets()
    {
        playerFormations = SelectionManager.instance.playerFormations.ToList();
        enemyFormations = SelectionManager.instance.enemyFormations;

        UnifyPlayerFormations();
        Debug.Log("Unified player formations count: " + playerFormations.Count);

        playerFormationsCountBefore = new float[playerFormations.Count];
        playerFormationsCountAfter = new float[playerFormations.Count];
        for (int i = 0; i < playerFormations.Count; i++)
        {
            playerFormationsCountBefore[i] = playerFormations[i].GetLength();
        }

        int[] solution = ExhaustiveSearch();//ex. [0,1,2] first enemy formation attack player formation at playerFormations[0]
        //int[] solution = WTAGreedyMMR();

        for (int i = 0; i < enemyFormations.Count; i++)
        {
            Debug.Log("Enemy Type: " + enemyFormations[i][0].GetComponent<StarshipAI>().typeIndex + " attacks player type: " + playerFormations[solution[i]].shipsInFormation[0].GetComponent<StarshipAI>().typeIndex + " Formation index: " + solution[i]);
            for (int j = 0; j < enemyFormations[i].Length; j++)//each enemy ship in current formation attacks random player ship from attacked player formation
            {
                enemyFormations[i][j].GetComponent<StarshipAI>().SetAttack(playerFormations[solution[i]].shipsInFormation[0], enemyFormations[i][j].GetComponent<StarshipAI>().formationHelper);
            }
        }
    }

    int[] ExhaustiveSearch()
    {
        int[] solution = new int[enemyFormations.Count];
        float solutionValue = float.PositiveInfinity;

        int noOfIterations = (int)Mathf.Pow(playerFormations.Count, enemyFormations.Count);
        int iterationNo = 0;

        while (iterationNo < noOfIterations)
        {
            int[] feasibleAllocations = GetFeasibleAllocations(iterationNo);
            float feasibleSolutionValue = CalculateSolutionValue(feasibleAllocations);
            if (feasibleSolutionValue < solutionValue)
            {
                solution = feasibleAllocations;
                solutionValue = feasibleSolutionValue;
            }
            iterationNo++;
        }
        Debug.Log("EXHAUSTIVE SEARCH SOLUTION: " + solutionValue);
        return solution;
    }

    int[] GetFeasibleAllocations(int iterationNo)
    {
        List<int> feasibleAllocations = new List<int>();
        while (iterationNo > 0)
        {
            feasibleAllocations.Add(iterationNo % playerFormations.Count + 0);
            iterationNo = iterationNo / playerFormations.Count;
        }
        while (feasibleAllocations.Count < enemyFormations.Count)
        {
            feasibleAllocations.Add(0);
        }
        return feasibleAllocations.ToArray();
    }

    float CalculateSolutionValue(int[] solution)
    {
        float enemySurvivorsSum = 0;//rename
        for (int it = 0; it < playerFormations.Count; it++)
        {
            playerFormationsCountAfter[it] = playerFormations[it].GetLength();
        }

        int i = 0;
        while (i < playerFormations.Count)//player formations (targets)
        {
            int k = 0;
            float effectivenessSum = 0;// cause many enemy formations can attack one player formation
            float costSum = 0;
            int noUnits = 0;
            while (k < enemyFormations.Count)//enemy formations (weapons)
            {
                if (solution[k] == i)
                {
                    int enemyTypeIndex = enemyFormations[k][0].GetComponent<StarshipAI>().typeIndex;
                    int playerTypeIndex = playerFormations[i].shipsInFormation[0].GetComponent<StarshipAI>().typeIndex;//TO NIE UWZGLĘDNIA RÓŻNYCH TYPOW W JEDNEJ FORMACJI

                    effectivenessSum += starshipTypesEffectiveness[enemyTypeIndex, playerTypeIndex] * enemyFormations[k].Length;
                    costSum += starshipTypesValues[enemyTypeIndex] * enemyFormations[k].Length;
                    noUnits += enemyFormations[k].Length;
                }
                k++;
            }
            
            if (noUnits > 0)// cause many enemy formations can attack one player formation
            {
                float averageEffectiveness = effectivenessSum / noUnits;
                float averageSum = costSum / noUnits;
                float predictedSurvivors = GetExpectedRemainingUnits(noUnits, playerFormations[i].GetLength(), averageEffectiveness);
                if (predictedSurvivors > 0) //player wins, we are counting player survivors, they are highering
                {
                    float loss = playerFormationsCountBefore[i] - predictedSurvivors;
                    if (loss > 0)
                        playerFormationsCountAfter[i] -= loss;
                }
                else//enemy wins we are counting enemy survivors, they are lowering 
                {
                    playerFormationsCountAfter[i] = 0;
                    enemySurvivorsSum += predictedSurvivors * averageSum;
                }
            }
            i++;
        }
        float solutionValue = 0;
        solutionValue = enemySurvivorsSum;
        for (int it = 0; it < playerFormationsCountAfter.Length; it++)
        {          
            if (playerFormationsCountAfter[it] > 0)
            {
                solutionValue += playerFormationsCountAfter[it] * starshipTypesValues[playerFormations[it].shipsInFormation[0].GetComponent<StarshipAI>().typeIndex];
            }              
        }
        return solutionValue;
    }

    int[] WTAGreedyMMR()
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
                    int playerTypeIndex = playerFormations[i].shipsInFormation[0].GetComponent<StarshipAI>().typeIndex;//TO NIE UWZGLĘDNIA RÓŻNYCH TYPOW W JEDNEJ FORMACJI
                    float predictedSurvivors = GetExpectedRemainingUnits((int)enemyFormationCounts[k], (int)playerFormationCounts[i], starshipTypesEffectiveness[enemyTypeIndex, playerTypeIndex]);//maybe change ints to floats

                    float decrease = 0;//decrease of player army value
                    if (predictedSurvivors > 0) //player wins, we are counting player survivors, they are highering
                    {
                        decrease += (playerFormationCounts[i] - Mathf.Abs(predictedSurvivors)) * starshipTypesValues[playerTypeIndex];
                        decrease -= enemyFormationCounts[k] * starshipTypesValues[enemyTypeIndex];
                    }
                    else//enemy wins we are counting enemy survivors, they are lowering 
                    {
                        decrease -= (enemyFormationCounts[k] - Mathf.Abs(predictedSurvivors)) * starshipTypesValues[enemyTypeIndex];//how many ships cost enemy lost
                        decrease += playerFormationCounts[i] * starshipTypesValues[playerTypeIndex];
                    }
                    //float decrease = predictedSurvivors * starshipTypesValues[predictedSurvivors > 0 ? playerTypeIndex : enemyTypeIndex];

                    if (decrease > maxDecrease)
                    {
                        maxDecrease = decrease;
                        allocatedTarget = i;
                        predictedSurvivorsAllocated = predictedSurvivors;
                    }
                }
                i++;
            }

            if (predictedSurvivorsAllocated > 0) //player wins, we are counting player survivors, they are highering
            {
                enemyFormationCounts[k] = 0;
                playerFormationCounts[allocatedTarget] = predictedSurvivorsAllocated;
            }
            else//enemy wins we are counting enemy survivors, they are lowering 
            {
                playerFormationCounts[allocatedTarget] = 0;
                enemyFormationCounts[k] = predictedSurvivorsAllocated;
            }
            solution[k] = allocatedTarget;
            k++;          
        }
        solutionValue = CalculateSolutionValue(solution);
        Debug.Log("GREEDY MMR SOLUTION: " + solutionValue);
        return solution;
    }

    float GetExpectedRemainingUnits(int enemyCount, int playerCount, float playerAttritionCoef)//GetExpectedSurvivedValue
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

    float GetProbability(int n, int m, float r)
    {
        if (n == 0)
            return 0;
        else if (m == 0)
            return 1;
        return (r * GetProbability(n, m - 1, r)) + ((1 - r) * GetProbability(n - 1, m, r));
    }

    void UnifyPlayerFormations()
    {
        List<FormationHelper> tempList = new List<FormationHelper>();
        foreach(FormationHelper helper in playerFormations)
        {
            var groups = helper.shipsInFormation.GroupBy(ship => ship.GetComponent<StarshipAI>().typeIndex);
            tempList.Add(helper);
            if (groups.Count() > 1)
            {
                for(int i = 1; i < groups.Count(); i++)
                {
                    FormationHelper newFormationHelper = new FormationHelper();
                    newFormationHelper.shipsInFormation.AddRange(groups.ElementAt(i));
                    tempList.Add(newFormationHelper);
                }
            }
        }
        playerFormations = tempList;
    }
}
