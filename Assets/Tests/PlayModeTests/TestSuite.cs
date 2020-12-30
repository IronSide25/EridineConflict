using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    public class TestSuite
    {
        [Test]
        public void TestHealthManager()
        {
            HealthManager healthManager = new HealthManager();
            healthManager.currentHealth = 50;
            //healthManager.GetType().GetField("currentHealth", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Default)?.SetValue(healthManager, 50);
            healthManager.DealDamage(10);
            Assert.AreEqual(healthManager.currentHealth, 40);
            Object.Destroy(healthManager);
        }
    }
}
