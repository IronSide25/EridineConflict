using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    public class FormationHelperTests
    {
        FormationHelper formationHelper;
        Transform tr1;
        Transform tr2;
        Rigidbody r1;
        Rigidbody r2;

        [SetUp]
        public void Setup()
        {
            tr1 = new GameObject().transform;
            tr2 = new GameObject().transform;
            r1 = tr1.gameObject.AddComponent<Rigidbody>();
            r2 = tr2.gameObject.AddComponent<Rigidbody>();
            List<Transform> ships = new List<Transform>();
            ships.Add(tr1);
            ships.Add(tr2);
            formationHelper = new FormationHelper(ships);
        }

        [Test]
        public void TestCenterOfMass()
        {
            tr1.position = new Vector3(100, -100, 100);
            tr2.position = new Vector3(50, -50, 50);
            Vector3 centerOfMass = formationHelper.GetCenterOfMass();
            Assert.AreEqual(centerOfMass, Vector3.Lerp(tr1.position, tr2.position, .5f));
            tr1.position = new Vector3(-100, -100, -100);
            tr2.position = new Vector3(-50, -50, -50);
            Vector3 centerOfMass2 = formationHelper.GetCenterOfMass();
            Assert.AreEqual(centerOfMass, centerOfMass2);
            formationHelper.InvalidateCache();
            centerOfMass2 = formationHelper.GetCenterOfMass();
            Assert.AreNotEqual(centerOfMass, centerOfMass2);
        }

        [Test]
        public void TestAverageVelocity()
        {
            r1.velocity = new Vector3(10, 5, 10);
            r2.velocity = new Vector3(0, 10, 0);
            Vector3 avgVelocity = formationHelper.GetAverageVelocity();
            Assert.AreEqual(new Vector3(5, 7.5f, 5), avgVelocity);
            r1.velocity = new Vector3(-10, -5, -10);
            r2.velocity = new Vector3(0, -10, 0);
            Vector3 avgVelocity2 = formationHelper.GetAverageVelocity();
            Assert.AreEqual(avgVelocity, avgVelocity2);
            formationHelper.InvalidateCache();
            avgVelocity2 = formationHelper.GetAverageVelocity();
            Assert.AreNotEqual(avgVelocity, avgVelocity2);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(tr1.gameObject);
            Object.DestroyImmediate(tr2.gameObject);
        }
    }
}
