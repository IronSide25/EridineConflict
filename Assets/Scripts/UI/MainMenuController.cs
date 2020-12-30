using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    public GameObject mainPanel;
    public GameObject selectLevelPanel;
    public GameObject aboutPanel;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnSelectLevelClick()
    {
        mainPanel.SetActive(false);
        selectLevelPanel.SetActive(true);
    }

    public void OnAboutClick()
    {
        mainPanel.SetActive(false);
        aboutPanel.SetActive(true);
    }

    public void ReturnToMainClick()
    {
        mainPanel.SetActive(true);
        selectLevelPanel.SetActive(false);
        aboutPanel.SetActive(false);
    }

    public void LoadLevelClick(int levelID)
    {
        SceneManager.LoadScene(levelID);
    }

    public void OnExitGameClick()
    {
        Application.Quit();
    }
}
