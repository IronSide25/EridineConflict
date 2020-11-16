using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public Image healthImageForeground;
    public Image healthImageBackground;
    public float positionOffset;

    private HealthManager healthManager;
    private Renderer targetRenderer;

    public void Start()
    {
        targetRenderer = healthManager.gameObject.GetComponent<Renderer>();
        if (healthManager.gameObject.layer == 10)
            healthImageForeground.color = Color.red;
        else if (healthManager.gameObject.layer == 8)
            healthImageForeground.color = Color.green;
    }

    public void SetHealth(HealthManager healthManager)
    {
        this.healthManager = healthManager;
        healthManager.OnHealthChanged += HandleOnHealthChanged;
    }

    private void HandleOnHealthChanged(float percent)
    {
        healthImageForeground.fillAmount = percent;
    }

    private void LateUpdate()
    {
        if(targetRenderer.isVisible)
        {
            healthImageForeground.enabled = true;
            healthImageBackground.enabled = true;
            transform.position = Camera.main.WorldToScreenPoint(healthManager.transform.position + Vector3.up * positionOffset);
        }            
        else
        {
            healthImageForeground.enabled = false;
            healthImageBackground.enabled = false;
        }
    }

    private void OnDestroy()
    {
        healthManager.OnHealthChanged -= HandleOnHealthChanged;
    }
}
