using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Tests
{
    public class StarshipAITests
    {
        //player
        FormationHelper formationHelper1;
        StarshipSteering starshipSteering1;
        StarshipAI starshipAI1;
        Transform tr1;
        //enemy
        FormationHelper formationHelper2;
        StarshipSteering starshipSteering2;
        StarshipAI starshipAI2;
        Transform tr2;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            SceneManager.LoadScene("StarshipAITestScene");
            yield return new WaitForSeconds(.1f);

            tr1 = GameObject.FindGameObjectWithTag("Player").transform;
            starshipSteering1 = tr1.GetComponent<StarshipSteering>();
            starshipAI1 = tr1.GetComponent<StarshipAI>();
            formationHelper1 = new FormationHelper(new List<Transform>(new Transform[] {tr1}));
            starshipAI1.formationHelper = formationHelper1;

            tr2 = GameObject.FindGameObjectWithTag("Enemy").transform;
            starshipSteering2 = tr2.GetComponent<StarshipSteering>();
            starshipAI2 = tr2.GetComponent<StarshipAI>();
            formationHelper2 = new FormationHelper(new List<Transform>(new Transform[] { tr2 }));
            starshipAI2.formationHelper = formationHelper2;
        }

        [UnityTest]
        public IEnumerator TestMoving()
        {
            ResetShips();
            Vector3 dest = new Vector3(0, 10, 0);
            float dist = Vector3.Distance(dest, tr1.position);
            starshipAI1.SetMove(dest, formationHelper1);
            yield return new WaitForSeconds(.5f);
            Assert.True(starshipSteering1.isMoving);
            Assert.False(starshipAI1.isAttacking);
            Assert.Greater(dist, Vector3.Distance(dest, tr1.position));
            starshipSteering1.SetStop();
            yield return new WaitForSeconds(.5f);
            Rigidbody rig = tr1.GetComponent<Rigidbody>();
            Assert.Zero(rig.velocity.magnitude);
            Assert.False(starshipSteering1.isMoving);
        }

        [UnityTest]
        public IEnumerator TestMovingAttack()
        {
            ResetShips();
            starshipAI1.SetAttack(tr2, formationHelper1);
            float dist = Vector3.Distance(tr2.position, tr1.position);
            yield return new WaitForSeconds(.5f);
            Assert.True(starshipSteering1.isMoving);
            Assert.True(starshipAI1.isAttacking);
            Assert.Greater(dist, Vector3.Distance(tr2.position, tr1.position));
            starshipAI1.EndAttack();
            starshipSteering1.SetStop();
            starshipAI2.EndAttack();
            starshipSteering2.SetStop();
            tr1.position = new Vector3(starshipAI1.attackDistance, 0, 0);
            tr2.position = new Vector3(-starshipAI1.attackDistance, 0, 0);
            yield return new WaitForSeconds(.5f);
            Rigidbody rig = tr1.GetComponent<Rigidbody>();
            Assert.Zero(rig.velocity.magnitude);
            Assert.False(starshipSteering1.isMoving);
            Assert.False(starshipAI1.isAttacking);
        }

        [UnityTest]
        public IEnumerator TestUnitBehavior()
        {
            ResetShips();
            starshipAI1.unitBehavior = UnitBehavior.Passive;
            starshipAI2.unitBehavior = UnitBehavior.Passive;
            tr1.position = new Vector3(starshipAI1.attackDistance/2, 0, 0);
            tr2.position = new Vector3(0, 0, 0);
            yield return new WaitForSeconds(0.1f);
            Assert.False(starshipAI1.isAttacking);
            starshipAI1.unitBehavior = UnitBehavior.Defensive;
            starshipAI2.unitBehavior = UnitBehavior.Defensive;
            yield return new WaitForSeconds(0.1f);
            Assert.False(starshipAI1.isAttacking);
            starshipAI1.unitBehavior = UnitBehavior.Aggresive;
            starshipAI2.unitBehavior = UnitBehavior.Aggresive;
            yield return new WaitForSeconds(0.5f);
            Assert.True(starshipAI1.isAttacking);
        }

        private void ResetShips()
        {
            tr1.position = new Vector3(starshipAI1.attackDistance, 0, 0);
            tr2.position = new Vector3(-starshipAI1.attackDistance, 0, 0);
            starshipSteering1.SetStop();
            starshipAI1.EndAttack();
            starshipSteering2.SetStop();
            starshipAI2.EndAttack();
        }
    }
}
