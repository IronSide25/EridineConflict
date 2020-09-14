using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthManager : MonoBehaviour
{
    public static event Action<HealthManager> OnHealthManagerAdded = delegate { };
    public static event Action<HealthManager> OnHealthManagerRemoved = delegate { };


    public float maxHealth;
    float currentHealth;
    public GameObject explosionPrefab;

    public event Action<float> OnHealthChanged = delegate { };

    
    // Start is called before the first frame update
    void OnEnable()
    {
        currentHealth = maxHealth;
        OnHealthManagerAdded(this);
    }

    public void AddHealthBar()
    {
        OnHealthManagerAdded(this);
    }

    public void RemoveHealthBar()
    {
        OnHealthManagerRemoved(this);
    }

    // Update is called once per frame
    void Update()
    {
        if (currentHealth < 0)
        {
            Destroy(gameObject);
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        }
    }

    public void OnParticleCollision(GameObject other)
    {

        ModifyHealth(-2);
    }

    public void ModifyHealth(int value)
    {
        currentHealth += value;
        OnHealthChanged(currentHealth / maxHealth);
    }

    private void OnDisable()
    {
        OnHealthManagerRemoved(this);
    }
}
