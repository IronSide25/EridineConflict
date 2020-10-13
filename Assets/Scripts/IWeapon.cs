using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IWeapon
{
    void Activate(Transform target);
    void Deactivate();
    void Destroy();
    float GetDamage();
    float GetRange();
    float GetProjectileSpeed();
}
