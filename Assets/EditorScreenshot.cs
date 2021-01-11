// Created by Long Nguyen Huu
// 2016.05.15
// MIT License

using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;

public class EditorScreenshot //: EditorWindow
{
    /*
    string screenshotFolderPath = "Screenshots";
    string screenshotFilenamePrefix = "screenshot_";
    int nextScreenshotIndex = 0;

    [MenuItem("Window/Editor Screenshot")]
    static void Init()
    {
        GetOrCreateWindow();
    }

    [MenuItem("Tools/Take Screenshot _F11")]
    static void StaticTakeScreenshot()
    {
        GetOrCreateWindow().TakeScreenshot();
    }

    static EditorScreenshot GetOrCreateWindow()
    {
        EditorScreenshot editorScreenshot = GetWindow<EditorScreenshot>(title: "Screenshot");

        if (EditorPrefs.HasKey("EditorScreenshot.screenshotFolderPath"))
            editorScreenshot.screenshotFolderPath = EditorPrefs.GetString("EditorScreenshot.screenshotFolderPath");
        if (EditorPrefs.HasKey("EditorScreenshot.screenshotFilenamePrefix"))
            editorScreenshot.screenshotFilenamePrefix = EditorPrefs.GetString("EditorScreenshot.screenshotFilenamePrefix");
        if (EditorPrefs.HasKey("EditorScreenshot.nextScreenshotIndex"))
            editorScreenshot.nextScreenshotIndex = EditorPrefs.GetInt("EditorScreenshot.nextScreenshotIndex");

        return editorScreenshot;
    }

    void OnGUI()
    {
        EditorGUI.BeginChangeCheck();

        GUIContent savePathLabel = new GUIContent("Save path", "Save path of the screenshots, relative from the project root");
        screenshotFolderPath = EditorGUILayout.TextField(savePathLabel, screenshotFolderPath);
        screenshotFilenamePrefix = EditorGUILayout.TextField("Screenshot prefix", screenshotFilenamePrefix);
        nextScreenshotIndex = EditorGUILayout.IntField("Next screenshot index", nextScreenshotIndex);

        if (EditorGUI.EndChangeCheck())
        {
            EditorPrefs.SetString("EditorScreenshot.screenshotFolderPath", screenshotFolderPath);
            EditorPrefs.SetString("EditorScreenshot.screenshotFilenamePrefix", screenshotFilenamePrefix);
            EditorPrefs.SetInt("EditorScreenshot.nextScreenshotIndex", nextScreenshotIndex);
        }

        if (GUILayout.Button("Take screenshot")) TakeScreenshot();
    }

    void TakeScreenshot()
    {
        // get name of current focused window, which should be "  (UnityEditor.GameView)" if it is a Game view
        string focusedWindowName = EditorWindow.focusedWindow.ToString();
        if (!focusedWindowName.Contains("UnityEditor.GameView"))
        {
            // since no Game view is focused right now, focus on any Game view, or create one if needed
            Type gameViewType = Type.GetType("UnityEditor.GameView,UnityEditor");
            EditorWindow.GetWindow(gameViewType);
        }

        // Tried getting the last focused window, but does not always work (even for focused window!)
        // Type gameViewType = Type.GetType("UnityEditor.GameView,UnityEditor");
        // EditorWindow lastFocusedGameView = (EditorWindow) gameViewType.GetField("s_LastFocusedGameView", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
        // if (lastFocusedGameView != null) {
        // 	lastFocusedGameView.Focus();
        // } else {
        // 	// no Game view created since editor launch, create one
        // 	Type gameViewType = Type.GetType("UnityEditor.GameView,UnityEditor");
        // 	EditorWindow.GetWindow(gameViewType);
        // }

        string path = string.Format("{0}/{1}{2:00}.png", screenshotFolderPath, screenshotFilenamePrefix, nextScreenshotIndex);
        ScreenCapture.CaptureScreenshot(path);

        Debug.LogFormat("Screenshot recorded at {0} ({1})", path, UnityStats.screenRes);

        ++nextScreenshotIndex;
        EditorPrefs.SetInt("EditorScreenshot.nextScreenshotIndex", nextScreenshotIndex);
    }
    */
}