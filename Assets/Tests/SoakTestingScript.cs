using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static MouseSimulation;

public class SoakTestingScript : MonoBehaviour
{
    SelectionManager selectionManager;
    public int iterations;
    public float delayDuration;
    private WaitForSeconds waitDelay;
    
    // Start is called before the first frame update
    void Start()
    {
        selectionManager = GameObject.Find("Canvas").GetComponent<SelectionManager>();
        waitDelay = new WaitForSeconds(delayDuration);
        StartCoroutine(Test());
        
    }

    private IEnumerator Test()
    {
        yield return waitDelay;
        for (int i = 0; i < iterations; i++)
        {
            selectionManager.OnSelectAllClick();
            MousePoint point = new MousePoint(UnityEngine.Random.Range(50, 1800), UnityEngine.Random.Range(50, 900));
            SetCursorPos(point.X, point.Y);
            MouseEventFlags flags = MouseEventFlags.RightUp | MouseEventFlags.RightDown;
            mouse_event((int)flags, point.X, point.Y, 0, 0);
            yield return waitDelay;
        }
    }
}
