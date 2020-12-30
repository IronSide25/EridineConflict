using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Tests
{
    public class SelectionManagerTests
    {
        SelectionManager selectionManager;
        StarshipAI[] shipsAIs;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            SceneManager.LoadScene("SelectionManagerTestScene");
            yield return new WaitForSeconds(.1f);
            selectionManager = GameObject.Find("Canvas").GetComponent<SelectionManager>();
            GameObject[] ships = GameObject.FindGameObjectsWithTag("Player");
            shipsAIs = new StarshipAI[ships.Length];
            for (int i = 0; i < ships.Length; i++)
                shipsAIs[i] = ships[i].GetComponent<StarshipAI>();
        }

        [Test]
        public void TestSelectAll()
        {
            selectionManager.OnSelectAllClick();
            bool allSelected = true;
            foreach (StarshipAI ai in shipsAIs)
                if (!ai.isSelected)
                    allSelected = false;
            Assert.True(allSelected);
            selectionManager.ClearSelected();
            bool noSelection = true;
            foreach (StarshipAI ai in shipsAIs)
                if (ai.isSelected)
                    allSelected = false;
            Assert.True(noSelection);
        }

        [Test]
        public void TestBehaviorSetting()
        {
            selectionManager.OnSelectAllClick();
            selectionManager.OnSetAggresiveClicked();
            bool behaviorIsSet = true;
            foreach (StarshipAI ai in shipsAIs)
                if (ai.unitBehavior != UnitBehavior.Aggresive)
                    behaviorIsSet = false;
            Assert.True(behaviorIsSet);
            selectionManager.OnSetDeffensiveClicked();
            behaviorIsSet = true;
            foreach (StarshipAI ai in shipsAIs)
                if (ai.unitBehavior != UnitBehavior.Defensive)
                    behaviorIsSet = false;
            Assert.True(behaviorIsSet);
            selectionManager.OnSetPassiveClicked();
            behaviorIsSet = true;
            foreach (StarshipAI ai in shipsAIs)
                if (ai.unitBehavior != UnitBehavior.Passive)
                    behaviorIsSet = false;
            Assert.True(behaviorIsSet);
            selectionManager.ClearSelected();
        }

        [Test]
        public void TestTimeManagement()
        {
            float defaultFixedDeltaTime = SelectionManager.defaultFixedDeltaTime;
            selectionManager.IncrementTimeScale();
            Assert.True(float.Equals(Time.timeScale, 1f));
            Assert.True(float.Equals(Time.fixedDeltaTime, defaultFixedDeltaTime * Time.timeScale));
            selectionManager.DecrementTimeScale();
            Assert.True(float.Equals(Time.timeScale, 1f - selectionManager.timeScaleStep));
            Assert.True(float.Equals(Time.fixedDeltaTime, defaultFixedDeltaTime * Time.timeScale));
            selectionManager.IncrementTimeScale();
            Assert.True(float.Equals(Time.timeScale, 1f));
            Assert.True(float.Equals(Time.fixedDeltaTime, defaultFixedDeltaTime * Time.timeScale));
            for (int i = 0; i < (1/ selectionManager.timeScaleStep) + 2; i++)
                selectionManager.DecrementTimeScale();
            Assert.True(float.Equals(Time.timeScale, 0f));
            Time.timeScale = 1f;
            Time.fixedDeltaTime = defaultFixedDeltaTime;
        }
    }
}
