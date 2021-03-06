﻿using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SelectionManager : MonoBehaviour
{
    public static SelectionManager instance;

    private HashSet<Transform> selectedPlayerStarships;
    private HashSet<Transform> selectedEnemyStarships;
    private List<Transform> playerStarships;
    private List<Transform> enemyStarships;
    public HashSet<FormationHelper> playerFormations;
    public List<Transform[]> enemyFormations;
    public int levelIndex;

    [Header("Selection")]
    private Vector3 dragStartPosition;
    private bool allowDrag = false;
    private const float sqrMinDragDistance = 100;
    public float sqrMaxOrderDistance = 62500;
    public RectTransform selectionBoxRect;
    public GameObject selectionGO;
    
    [Header("UI")]
    public Transform selectionPlaneFloor;
    public Transform selectionPlaneHeight;
    private float selectionPlaneHeightPosY = 0;
    public float selectionPlaneScrollSpeed = 10;
    public float maxSelectionPlaneHeight = 50;

    [Header("HUD")]
    public GameObject HUDGameobject;
    private const float starshipIconsStart = 80;
    private const float starshipIconsSpacing = 90;
    private float[] selectedCountByTypeIndex;
    public Text[] textCounts;
    GameObject[] starshipIcons;
    public GameObject selectedStarshipsPanel;
    public GameObject setBehaviorPanel;
    public Image setAggresiveImage;
    public Image setDefensiveImage;
    public Image setPassiveImage;
    public Image enableHUDButtonImage;
    public GameObject tooFarTextGO;
    public GameObject winPanel;
    public GameObject defeatPanel;

    public Text timeSpeedText;
    public float timeScaleStep = 0.05f;
    public const float defaultFixedDeltaTime = 0.02f;
    private EventSystem eventSystem;


    // Start is called before the first frame update
    void Awake()
    {
        instance = this;
        playerStarships = new List<Transform>();
        enemyStarships = new List<Transform>();
        selectedPlayerStarships = new HashSet<Transform>();
        selectedEnemyStarships = new HashSet<Transform>();
        playerFormations = new HashSet<FormationHelper>();
        enemyFormations = new List<Transform[]>();
        HealthManager.OnStarshipAdded += AddStarship;
        HealthManager.OnStarshipRemoved += RemoveStarship;

        eventSystem = GameObject.Find("EventSystem").GetComponent<EventSystem>();

        setBehaviorPanel.SetActive(false);
        selectedStarshipsPanel.SetActive(false);
        selectedCountByTypeIndex = new float[] { 0, 0, 0, 0, 0 };
        starshipIcons = new GameObject[textCounts.Length];
        for(int i = 0; i < textCounts.Length; i++)
            starshipIcons[i] = textCounts[i].transform.parent.gameObject;
    }

    private void FixedUpdate()
    {
        foreach (FormationHelper formationHelper in FormationHelper.instances)
            formationHelper.InvalidateCache();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0) && !eventSystem.IsPointerOverGameObject())
        {
            dragStartPosition = Input.mousePosition;
            allowDrag = true;

            if (!(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
                ClearSelected();

            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit, Camera.main.farClipPlane, 1 << 8))
            {
                SelectStarship(hit.transform);
            }
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, 1 << 10))
            {
                selectedEnemyStarships.Add(hit.transform);
                AddHealthBar(hit.transform);
            }
            UpdateUIActive();
        }
        else if (Input.GetMouseButton(0))//dragging
        {
            if (Vector3.SqrMagnitude(dragStartPosition - Input.mousePosition) > sqrMinDragDistance && allowDrag)
            {
                selectionGO.SetActive(true);
                selectionBoxRect.position = dragStartPosition - ((dragStartPosition - Input.mousePosition) / 2);
                float sizeX = Mathf.Abs(dragStartPosition.x - Input.mousePosition.x);
                float sizeY = Mathf.Abs(dragStartPosition.y - Input.mousePosition.y);
                selectionBoxRect.sizeDelta = new Vector2(sizeX, sizeY);
            }
        }
        else if (Input.GetMouseButtonUp(0))//end of drag
        {
            if (allowDrag && Vector3.SqrMagnitude(dragStartPosition - Input.mousePosition) > sqrMinDragDistance)
            {
                if (!(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
                    ClearSelected();

                Rect selectionBox = new Rect(Mathf.Min(dragStartPosition.x, Input.mousePosition.x), Mathf.Min(dragStartPosition.y, Input.mousePosition.y),
                Mathf.Abs(dragStartPosition.x - Input.mousePosition.x), Mathf.Abs(dragStartPosition.y - Input.mousePosition.y));
                foreach (Transform tr in playerStarships)
                {
                    Vector3 screenSpace = Camera.main.WorldToScreenPoint(tr.position);
                    screenSpace.z = 0;
                    if (selectionBox.Contains(screenSpace) && tr.tag == "Player")
                    {
                        SelectStarship(tr);
                    }
                }

                foreach (Transform tr in enemyStarships)
                {
                    Vector3 screenSpace = Camera.main.WorldToScreenPoint(tr.position);
                    screenSpace.z = 0;
                    if (selectionBox.Contains(screenSpace) && tr.tag == "Enemy")
                    {
                        selectedEnemyStarships.Add(tr);
                        Outline outline = tr.GetComponent<Outline>();
                        if (outline)
                            outline.enabled = true;

                        AddHealthBar(tr);
                    }
                }
                selectionGO.SetActive(false);
            }
            allowDrag = false;
            UpdateUIActive();
        }

        if (Input.GetMouseButtonDown(1) && selectedPlayerStarships.Count > 0 && !eventSystem.IsPointerOverGameObject())//give order
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, 1 << 10))//enemy ship hit
            {
                FormationHelper formationHelper = new FormationHelper(new List<Transform>(selectedPlayerStarships));
                foreach (Transform starshipTr in selectedPlayerStarships)
                {                                
                    StarshipAI starshipAI = starshipTr.GetComponent<StarshipAI>();
                    starshipAI.formationHelper.RemoveShip(starshipTr);
                    starshipAI.SetAttack(hit.transform, formationHelper);                   
                }
                playerFormations.Add(formationHelper);
                playerFormations.RemoveWhere(formation => formation.GetLength() == 0);
            }
            else if (Physics.Raycast(ray, out hit, Mathf.Infinity, 1 << 9))//selection plane hit
            {
                if(Vector3.SqrMagnitude(hit.point - Vector3.zero) < sqrMaxOrderDistance)
                {
                    FormationHelper formationHelper = new FormationHelper(new List<Transform>(selectedPlayerStarships));
                    foreach (Transform starshipTr in selectedPlayerStarships)
                    {
                        StarshipAI starshipAI = starshipTr.GetComponent<StarshipAI>();
                        starshipAI.formationHelper.RemoveShip(starshipTr);
                        starshipAI.SetMove(Input.GetKey(KeyCode.LeftControl) ? selectionPlaneHeight.position : hit.point, formationHelper);
                    }
                    playerFormations.Add(formationHelper);
                    playerFormations.RemoveWhere(formation => formation.GetLength() == 0);
                }    
                else
                {
                    tooFarTextGO.SetActive(true);
                }
            }
            else
            {
                tooFarTextGO.SetActive(true);
            }
            //Debug.Log("Player formation No: " + playerFormations.Count);
        }

        if (Input.GetKey(KeyCode.LeftControl) && selectedPlayerStarships.Count > 0)
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, 1 << 9))
            {
                selectionPlaneFloor.gameObject.SetActive(true);
                selectionPlaneHeight.gameObject.SetActive(true);
                selectionPlaneFloor.position = hit.point;
                selectionPlaneHeightPosY -= Input.GetAxis("Mouse ScrollWheel") * selectionPlaneScrollSpeed;
                selectionPlaneHeightPosY = Mathf.Clamp(selectionPlaneHeightPosY, -maxSelectionPlaneHeight, maxSelectionPlaneHeight);
                selectionPlaneHeight.position = new Vector3(hit.point.x, selectionPlaneHeightPosY, hit.point.z);
            }
            else
            {
                selectionPlaneFloor.gameObject.SetActive(false);
                selectionPlaneHeight.gameObject.SetActive(false);
                selectionPlaneHeightPosY = 0;
            }
        }
        else
        {
            selectionPlaneFloor.gameObject.SetActive(false);
            selectionPlaneHeight.gameObject.SetActive(false);
            selectionPlaneHeightPosY = 0;
        }

        int starshipIconCount = 0;
        for(int i = 0; i< textCounts.Length; i++)
        {
            textCounts[i].text = selectedCountByTypeIndex[i].ToString();           
            starshipIcons[i].SetActive(selectedCountByTypeIndex[i] > 0);
            if(starshipIcons[i].activeSelf)
            {
                Vector3 pos = starshipIcons[i].transform.position;
                pos.x = starshipIconsStart + (starshipIconCount * starshipIconsSpacing);
                starshipIcons[i].transform.position = pos;
                starshipIconCount++;
            }         
        }
    }

    void SelectStarship(Transform shipTransform)
    {
        Outline outline = shipTransform.GetComponent<Outline>();
        if (outline)
        {
            outline.enabled = true;
            AddHealthBar(shipTransform);
            StarshipAI starshipAI = shipTransform.GetComponent<StarshipAI>();
            starshipAI.isSelected = true;

            if (starshipAI.unitBehavior == UnitBehavior.Aggresive)
                setAggresiveImage.enabled = true;
            else if (starshipAI.unitBehavior == UnitBehavior.Defensive)
                setDefensiveImage.enabled = true;
            else
                setPassiveImage.enabled = true;

            if (selectedPlayerStarships.Add(shipTransform))
            {
                selectedCountByTypeIndex[starshipAI.typeIndex]++;
            }
        }
    }

    public void ClearSelected()
    {
        foreach (Transform tr in selectedPlayerStarships)
        {
            tr.GetComponent<Outline>().enabled = false;
            RemoveHealthBar(tr);
            StarshipAI starshipAI = tr.GetComponent<StarshipAI>();
            if (starshipAI)
                starshipAI.isSelected = false;
        }
        selectedPlayerStarships.Clear();
        foreach (Transform tr in selectedEnemyStarships)
        {
            tr.GetComponent<Outline>().enabled = false;
            RemoveHealthBar(tr);
            StarshipAI starshipAI = tr.GetComponent<StarshipAI>();
            if (starshipAI)
                starshipAI.isSelected = false;
        }
        selectedEnemyStarships.Clear();
        selectedCountByTypeIndex = new float[] { 0, 0, 0, 0, 0 };
    }

    void AddStarship(Transform starship)
    {
        if (starship.tag == "Player")
        {
            playerStarships.Add(starship);
        }
        else if (starship.tag == "Enemy")
        {
            enemyStarships.Add(starship);
        }
    }

    void RemoveStarship(Transform starship)
    {
        if (starship.tag == "Player")
        {
            playerStarships.Remove(starship);
            if(selectedPlayerStarships.Remove(starship))
            {
                StarshipAI starshipAI = starship.GetComponent<StarshipAI>();
                selectedCountByTypeIndex[starshipAI.typeIndex]--;
            }
            if (selectedPlayerStarships.Count == 0)
                UpdateUIActive();

            if(playerStarships.Count == 0 && defeatPanel)
            {
                defeatPanel.SetActive(true);
            }
        }
        else if (starship.tag == "Enemy")
        {
            enemyStarships.Remove(starship);
            selectedEnemyStarships.Remove(starship);

            if (enemyStarships.Count == 0 && winPanel)
            {
                winPanel.SetActive(true);
                if(PlayerPrefs.HasKey("highestFinishedLevel"))
                {
                    int highestFinishedLevel = PlayerPrefs.GetInt("highestFinishedLevel");
                    if(levelIndex > highestFinishedLevel)
                    {
                        PlayerPrefs.SetInt("highestFinishedLevel", levelIndex);
                        PlayerPrefs.Save();
                    }
                }
            }
        }
    }

    void RemoveHealthBar(Transform shipTransform)
    {
        HealthManager healthManager = shipTransform.GetComponent<HealthManager>();
        if (healthManager)
            healthManager.RemoveHealthBar();
    }

    void AddHealthBar(Transform shipTransform)
    {
        HealthManager healthManager = shipTransform.GetComponent<HealthManager>();
        if (healthManager)
            healthManager.AddHealthBar();
    }

    public void OnSetAggresiveClicked()
    {
        if(selectedPlayerStarships.Count > 0)
        {
            setAggresiveImage.enabled = true;
            setDefensiveImage.enabled = false;
            setPassiveImage.enabled = false;
        }
        foreach (Transform tr in selectedPlayerStarships)
        {          
            StarshipAI starshipAI = tr.GetComponent<StarshipAI>();
            if (starshipAI)
            {
                starshipAI.unitBehavior = UnitBehavior.Aggresive;
            }
        }
    }

    public void OnSetDeffensiveClicked()
    {
        if (selectedPlayerStarships.Count > 0)
        {
            setAggresiveImage.enabled = false;
            setDefensiveImage.enabled = true;
            setPassiveImage.enabled = false;
        }
        foreach (Transform tr in selectedPlayerStarships)
        {
            StarshipAI starshipAI = tr.GetComponent<StarshipAI>();
            if (starshipAI)
            {
                starshipAI.unitBehavior = UnitBehavior.Defensive;
            }
        }
    }

    public void OnSetPassiveClicked()
    {
        if (selectedPlayerStarships.Count > 0)
        {
            setAggresiveImage.enabled = false;
            setDefensiveImage.enabled = false;
            setPassiveImage.enabled = true;
        }
        foreach (Transform tr in selectedPlayerStarships)
        {
            StarshipAI starshipAI = tr.GetComponent<StarshipAI>();
            if (starshipAI)
            {
                starshipAI.unitBehavior = UnitBehavior.Passive;
            }              
        }
    }

    void UpdateUIActive()
    {
        if(setBehaviorPanel)
        {
            if (selectedPlayerStarships.Count == 0)
            {
                setBehaviorPanel.SetActive(false);
                selectedStarshipsPanel.SetActive(false);
                setAggresiveImage.enabled = false;
                setDefensiveImage.enabled = false;
                setPassiveImage.enabled = false;
            }
            else
            {
                setBehaviorPanel.SetActive(true);
                selectedStarshipsPanel.SetActive(true);
            }
        }       
    }

    public void OnEnableHUDButtonClick()
    {
        HUDGameobject.SetActive(!HUDGameobject.activeSelf);
        if (HUDGameobject.activeSelf)
            enableHUDButtonImage.color = Color.green;
        else
            enableHUDButtonImage.color = Color.white;
    }

    public void OnMainMenuButtonClick()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }

    public void OnStarshipTypeButtonClick(int index)
    {
        foreach(Transform ship in selectedPlayerStarships)
        {
            StarshipAI starshipAI = ship.GetComponent<StarshipAI>();
            if (starshipAI.typeIndex != index)
            {
                starshipAI.isSelected = false;
                Outline outline = ship.GetComponent<Outline>();
                if (outline)
                {
                    outline.enabled = false;
                }
                RemoveHealthBar(ship);
            }
        }
        selectedPlayerStarships.RemoveWhere(starship => starship.GetComponent<StarshipAI>().typeIndex != index);
        selectedCountByTypeIndex = new float[] { 0, 0, 0, 0, 0 };
        selectedCountByTypeIndex[index] = selectedPlayerStarships.Count;
        UpdateUIActive();
    }

    public void OnStarshipTypeRightClick(int index)
    {
        for(int i = 0; i< starshipIcons.Length;i++)
        {
            GameObject panel = starshipIcons[i].FindObject("InfoPanel");
            if (i != index)
            {            
                panel.SetActive(false);
            }
            else
            {
                panel.SetActive(!panel.activeSelf);
            }
        }
    }

    public void OnSelectAllClick()
    {
        ClearSelected();
        foreach (Transform tr in playerStarships)
        {
            SelectStarship(tr);
        }
    }

    public void IncrementTimeScale()
    {
        float value = Time.timeScale;
        if (Time.timeScale < 1)
            value += timeScaleStep;
        Time.timeScale = Mathf.Clamp(value, 0, 10);
        Time.fixedDeltaTime = defaultFixedDeltaTime * Time.timeScale;
        timeSpeedText.text = "Time speed: " + Mathf.Round(100 * Time.timeScale) + "%";
    }

    public void DecrementTimeScale()
    {
        float value = Time.timeScale;
        if (Time.timeScale > 0)
            value -= timeScaleStep;
        Time.timeScale = Mathf.Clamp(value, 0, 10);
        Time.fixedDeltaTime = defaultFixedDeltaTime * Time.timeScale;
        timeSpeedText.text = "Time speed: " + Mathf.Round(100 * Time.timeScale) + "%";
    }

    private void OnDestroy()
    {
        Time.timeScale = 1;
        Time.fixedDeltaTime = defaultFixedDeltaTime;
    }
}