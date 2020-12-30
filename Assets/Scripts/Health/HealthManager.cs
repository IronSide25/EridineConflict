using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthManager : MonoBehaviour
{
    public static event Action<HealthManager,Vector3> OnHealthManagerAdded = delegate { };
    public static event Action<HealthManager> OnHealthManagerRemoved = delegate { };

    public static event Action<Transform> OnStarshipAdded = delegate { };
    public static event Action<Transform> OnStarshipRemoved = delegate { };

    public event Action<float> OnHealthChanged = delegate { };

    public float maxHealth;
    public float armor = 0;
    public float currentHealth;
    public float explosionScale = 0.2f;
    public Vector3 healthBarScale;   

    private void Start()
    {
        currentHealth = maxHealth;
        if (gameObject.tag == "Player" || gameObject.tag == "Enemy")
        {
            OnStarshipAdded(transform);
        }
    }

    public void AddHealthBar()
    {
        OnHealthManagerAdded(this, healthBarScale);
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
            GameObject go = PoolingManager.instance.Spawn();
            go.transform.position = transform.position;
            go.transform.rotation = Quaternion.identity;
            go.transform.localScale = new Vector3(explosionScale, explosionScale, explosionScale);
            foreach (Transform tr in go.GetComponentsInChildren<Transform>())
                tr.localScale = new Vector3(explosionScale, explosionScale, explosionScale);
            Destroy(gameObject);
        }
    }

    public void OnParticleCollision(GameObject other)
    {
        IWeapon weapon = other.GetComponentInParent<IWeapon>();
        if(weapon != null)
        {
            DealDamage(weapon.GetDamage());
            gameObject.SendMessage("OnDamageReceived", other.transform.root);
        }        
    }

    public void DealDamage(float value)
    {
        value -= armor;
        if (value < 0)
            value = 0;
        currentHealth -= value;
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
