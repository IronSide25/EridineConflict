using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthManager : MonoBehaviour
{
    public static event Action<HealthManager> OnHealthManagerAdded = delegate { };
    public static event Action<HealthManager> OnHealthManagerRemoved = delegate { };

    public static event Action<Transform> OnStarshipAdded = delegate { };
    public static event Action<Transform> OnStarshipRemoved = delegate { };

    public float maxHealth;
    float currentHealth;
    public GameObject explosionPrefab;

    public event Action<float> OnHealthChanged = delegate { };

    private void Start()
    {
        currentHealth = maxHealth;
        //OnHealthManagerAdded(this);
        if (gameObject.tag == "Player" || gameObject.tag == "Enemy")
        {
            OnStarshipAdded(transform);
        }
    }

    public void AddHealthBar()
    {
        OnHealthManagerAdded(this);
        OnHealthChanged(currentHealth / maxHealth);
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
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            Destroy(gameObject);
        }
    }

    public void OnParticleCollision(GameObject other)
    {
        IWeapon weapon = other.GetComponent<IWeapon>();
        if(weapon != null)
        {
            ModifyHealth(-weapon.GetDamage());
        }        
    }

    public void ModifyHealth(float value)
    {
        currentHealth += value;
        OnHealthChanged(currentHealth / maxHealth);
    }

    private void OnDestroy()//OnDisable
    {
        OnHealthManagerRemoved(this);
        if (gameObject.tag == "Player" || gameObject.tag == "Enemy")
        {
            OnStarshipRemoved(transform);
        }
    }
}
