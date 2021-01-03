using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static MouseSimulation;

public class LoadTestingScript : MonoBehaviour
{
    SelectionManager selectionManager;
    public Vector2[] targets;
    public float delayDuration;
    private WaitForSeconds waitDelay;
    public Text averageText;
    public Text smallestText;
    float smallestFPS;
    int framesOnStart;
    float timeOnStart;
    bool coroutineRunning = true;
    MousePoint point;
    MouseEventFlags flags;

    // Start is called before the first frame update
    void Start()
    {
        selectionManager = GameObject.Find("Canvas").GetComponent<SelectionManager>();
        waitDelay = new WaitForSeconds(delayDuration);
        StartCoroutine(Test());
        framesOnStart = Time.frameCount;
        timeOnStart = Time.time;
        smallestFPS = float.PositiveInfinity;
        flags = MouseEventFlags.RightUp | MouseEventFlags.RightDown;
    }

    private void Update()
    {
        if (coroutineRunning)
            averageText.text = ((Time.frameCount - framesOnStart) / (Time.time - timeOnStart)).ToString();

        float currentFPS = 1.0f / Time.deltaTime;
        if (currentFPS < smallestFPS)
            smallestFPS = currentFPS;
        smallestText.text = smallestFPS.ToString();
    }

    private IEnumerator Test()
    {
        yield return waitDelay;
        selectionManager.OnSelectAllClick();
        yield return waitDelay;
        smallestFPS = float.PositiveInfinity;
        for (int i = 0; i < targets.Length; i++)
        {
            //selectionManager.OnSelectAllClick();
            point = new MousePoint((int)targets[i].x, (int)targets[i].y);
            SetCursorPos(point.X, point.Y);
            mouse_event((int)flags, point.X, point.Y, 0, 0);
            yield return waitDelay;
        }
        coroutineRunning = false;
    }
}
