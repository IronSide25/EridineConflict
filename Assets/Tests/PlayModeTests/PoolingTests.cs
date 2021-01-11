using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Tests
{
    public class PoolingTests
    {
        PoolingManager poolingManager;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            SceneManager.LoadScene("PoolingTestScene");
            yield return new WaitForSeconds(0.1f);
            poolingManager = PoolingManager.Instance;
        }

        [UnityTest]
        public IEnumerator TestPooling()
        {
            GameObject go = poolingManager.Spawn();
            PooledObject pooledObject = go.GetComponent<PooledObject>();
            pooledObject.lifetime = 1;
            yield return new WaitForSeconds(pooledObject.lifetime + 0.1f);
            Assert.False(go.activeSelf);
        }

        [Test]
        public void TestAdditionalAllocation()
        {
            HashSet<GameObject> goSet = new HashSet<GameObject>();
            for(int i = 0; i < poolingManager.prealocateCount + 5; i++)
            {
                goSet.Add(poolingManager.Spawn());
            }
            Assert.AreEqual(goSet.Count, poolingManager.prealocateCount + 5);
        }
    }
}
