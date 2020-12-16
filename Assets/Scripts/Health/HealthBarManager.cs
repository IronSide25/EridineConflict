﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthBarManager : MonoBehaviour
{
    public HealthBar healthBarPrefab;
    public Transform healthBarParent;
    private Dictionary<HealthManager, HealthBar> healthBars = new Dictionary<HealthManager, HealthBar>();

    private void Awake()
    {
        HealthManager.OnHealthManagerAdded += AddHealthBar;
        HealthManager.OnHealthManagerRemoved += RemoveHealthBar;
    }

    void AddHealthBar(HealthManager healthManager, Vector3 scale)
    {
        if(healthBars.ContainsKey(healthManager) == false)
        {
            HealthBar healthBar = Instantiate(healthBarPrefab, transform);
            healthBar.transform.parent = healthBarParent;
            healthBar.transform.localScale = scale;
            healthBars.Add(healthManager, healthBar);
            healthBar.SetHealth(healthManager);
        }
    }

    void RemoveHealthBar(HealthManager healthManager)
    {
        if (healthBars.ContainsKey(healthManager) && healthBars[healthManager])//check if "&& healthBars[healthManager]" is required later
        {
            Destroy(healthBars[healthManager].gameObject);
            healthBars.Remove(healthManager);
        }
    }
}
