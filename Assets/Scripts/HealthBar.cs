﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public Image healthImage;
    public float positionOffset;

    private HealthManager healthManager;

    public void SetHealth(HealthManager healthManager)
    {
        this.healthManager = healthManager;
        healthManager.OnHealthChanged += HandleOnHealthChanged;
    }

    private void HandleOnHealthChanged(float percent)
    {
        healthImage.fillAmount = percent;
    }

    private void LateUpdate()
    {
        transform.position = Camera.main.WorldToScreenPoint(healthManager.transform.position + Vector3.up * positionOffset);
    }

    private void OnDestroy()
    {
        healthManager.OnHealthChanged -= HandleOnHealthChanged;
    }
}
