using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    public class ExtensionsTests
    {
        [Test]
        public void TestCenterOfMass()
        {
            GameObject child3_1 = new GameObject();
            GameObject child3_2 = new GameObject();
            GameObject child2 = new GameObject();
            GameObject child1 = new GameObject();
            GameObject parent = new GameObject();
            child3_1.transform.parent = child2.transform;
            child3_2.transform.parent = child2.transform;
            child2.transform.parent = child1.transform;
            child1.transform.parent = parent.transform;

            child3_1.name = "ImportantGameObject";
            child3_2.name = "AnotherImportantGameObject";

            Transform importantGameObject = parent.FindObject("ImportantGameObject").transform;
            Transform anotherImportantGameObject = parent.FindObject("AnotherImportantGameObject").transform;

            Assert.AreEqual(importantGameObject.name, "ImportantGameObject");
            Assert.AreEqual(anotherImportantGameObject.name, "AnotherImportantGameObject");

            Object.DestroyImmediate(child3_1);
            Object.DestroyImmediate(child3_2);
            Object.DestroyImmediate(child2);
            Object.DestroyImmediate(child1);
            Object.DestroyImmediate(parent);
        }
    }
}
