using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PooledObject : MonoBehaviour
{
    public float lifetime = 5;
    private float startTime;

    public bool restartParticleSystem = false;

    // Start is called before the first frame update
    void Start()
    {
        startTime = Time.time;
    }

    private void OnEnable()
    {
        startTime = Time.time;
        if (restartParticleSystem)
        {
            ParticleSystem ps = gameObject.GetComponent<ParticleSystem>();
            if(ps)
            {
                ps.Simulate(0, true, true);
                ps.Play();
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Time.time - startTime > lifetime)
            gameObject.SetActive(false);
    }
}
