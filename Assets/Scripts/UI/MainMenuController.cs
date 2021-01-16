using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    public GameObject mainPanel;
    public GameObject selectLevelPanel;
    public GameObject aboutPanel;
    public GameObject[] levelButtons;

    // Start is called before the first frame update
    void Start()
    {
        int highestFinishedLevel = 0;
        if (PlayerPrefs.HasKey("highestFinishedLevel"))
        {
            highestFinishedLevel = PlayerPrefs.GetInt("highestFinishedLevel");            
        }
        else
        {
            PlayerPrefs.SetInt("highestFinishedLevel", 0);
            PlayerPrefs.Save();
        }

        for(int i = 0; i< levelButtons.Length; i++)
        {
            if(i > highestFinishedLevel)//level is locked
            {
                levelButtons[i].GetComponent<Button>().interactable = false;
                Text text = levelButtons[i].GetComponentInChildren<Text>();
                text.color = Color.red;
            }
        }
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
