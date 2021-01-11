using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    public class HealthManagerTests
    {
        [Test]
        public void TestHealthManager()
        {
            HealthManager healthManager = new HealthManager();
            healthManager.currentHealth = 50;
            healthManager.DealDamage(10);
            Assert.AreEqual(healthManager.currentHealth, 40);
        }
    }
}
