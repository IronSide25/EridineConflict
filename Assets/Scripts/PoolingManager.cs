using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoolingManager : MonoBehaviour
{
    public static PoolingManager instance;
    public List<GameObject> gameobjectList;
    public GameObject prefab;

    public int prealocateCount;
    // Start is called before the first frame update
    void Awake()
    {
        instance = this;
        gameobjectList = new List<GameObject>();
        
        if(prealocateCount > 0)
            for(int i = 0; i < prealocateCount; i++)
            {
                GameObject go = Instantiate(prefab);
                go.SetActive(false);           
                gameobjectList.Add(go);
            }
    }

    public GameObject Spawn()
    {
        foreach(GameObject gOb in gameobjectList)
        {
            if(!gOb.activeSelf)
            {
                gOb.SetActive(true);
                return gOb;
            }
        }
        GameObject go = Instantiate(prefab);
        gameobjectList.Add(go);
        return go;
    }
}
