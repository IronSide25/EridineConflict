using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisableScript : MonoBehaviour
{
    private float startTime;
    public float awakeTime = 10;
    // Start is called before the first frame update
    void OnEnable()
    {
        startTime = Time.time;
    }

    // Update is called once per frame
    void Update()
    {
        if (Time.time - startTime > awakeTime)
            gameObject.SetActive(false);
    }
}
