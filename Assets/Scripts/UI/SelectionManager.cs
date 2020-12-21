using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SelectionManager : MonoBehaviour
{
    public static SelectionManager instance;

    public HashSet<Transform> selectedPlayerStarships;
    public HashSet<Transform> selectedEnemyStarships;
    public List<Transform> playerStarships; // rename and move this somewhere else XD
    public List<Transform> enemyStarships; // rename and move this somewhere else XD
    public HashSet<FormationHelper> playerFormations;
    public List<Transform[]> enemyFormations;

    private Vector3 dragStartPosition;
    private bool allowDrag = false;
    private const float sqrMinDragDistance = 100;
    public RectTransform selectionBoxRect;
    public GameObject selectionGO;

    [Header("UI")]
    public float currentSelectionHeight;

    public Transform selectionPlaneFloor;
    public Transform selectionPlaneHeight;
    float selectionPlaneHeightPosY = 0;

    //HUD
    public GameObject HUDGameobject;
    private const float starshipIconsStart = 80;
    private const float starshipIconsSpacing = 90;
    public float[] selectedCountByTypeIndex;
    public Text[] textCounts;
    GameObject[] starshipIcons;
    public GameObject selectedStarshipsPanel;
    public GameObject setBehaviorPanel;
    public Image setAggresiveImage;
    public Image setDefensiveImage;
    public Image setPassiveImage;
    public Image enableHUDButtonImage;
    
    private EventSystem eventSystem;

    // Start is called before the first frame update
    void Awake()
    {
        instance = this;
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
        foreach (FormationHelper formationHelper in FormationHelper.formationHelpers)
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
                Outline outline = hit.transform.GetComponent<Outline>();
                if (outline)
                {
                    outline.enabled = true;
                    AddHealthBar(hit.transform);
                    StarshipAI starshipAI = hit.transform.GetComponent<StarshipAI>();                    
                    if (starshipAI)
                        starshipAI.isSelected = true;
                    if (starshipAI.unitBehavior == UnitBehavior.Aggresive)
                        setAggresiveImage.enabled = true;
                    else if (starshipAI.unitBehavior == UnitBehavior.Defensive)
                        setDefensiveImage.enabled = true;
                    else
                        setPassiveImage.enabled = true;

                    if (selectedPlayerStarships.Add(hit.transform))
                    {
                        selectedCountByTypeIndex[starshipAI.typeIndex]++;                 
                    }                    
                }
            }

            if (Physics.Raycast(ray, out hit, Mathf.Infinity, 1 << 10))
            {
                selectedEnemyStarships.Add(hit.transform);
                AddHealthBar(hit.transform);
                StarshipAI starshipAI = hit.transform.GetComponent<StarshipAI>();
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
                        Outline outline = tr.GetComponent<Outline>();
                        if (outline)
                        {
                            outline.enabled = true;
                            AddHealthBar(tr);
                            StarshipAI starshipAI = tr.GetComponent<StarshipAI>();
                            starshipAI.isSelected = true;

                            if (starshipAI.unitBehavior == UnitBehavior.Aggresive)
                                setAggresiveImage.enabled = true;
                            else if(starshipAI.unitBehavior == UnitBehavior.Defensive)
                                setDefensiveImage.enabled = true;
                            else
                                setPassiveImage.enabled = true;

                            if (selectedPlayerStarships.Add(tr))
                            {
                                selectedCountByTypeIndex[starshipAI.typeIndex]++;
                            }
                        }
                    }
                }

                foreach (Transform tr in enemyStarships)
                {
                    Vector3 screenSpace = Camera.main.WorldToScreenPoint(tr.position);
                    screenSpace.z = 0;
                    if (selectionBox.Contains(screenSpace) && tr.tag == "Enemy")
                    {
                        selectedEnemyStarships.Add(tr);
                        AddHealthBar(tr);
                        StarshipAI starshipAI = tr.GetComponent<StarshipAI>();//wtf
                    }
                }
                selectionGO.SetActive(false);
            }
            allowDrag = false;
            UpdateUIActive();
        }

        if (Input.GetMouseButtonDown(1))//give order
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, 1 << 10))
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
            else if (Physics.Raycast(ray, out hit, Mathf.Infinity, 1 << 9))
            {
                FormationHelper formationHelper = new FormationHelper(new List<Transform>(selectedPlayerStarships));
                foreach (Transform starshipTr in selectedPlayerStarships)
                {
                    StarshipAI starshipAI = starshipTr.GetComponent<StarshipAI>();
                    starshipAI.formationHelper.RemoveShip(starshipTr);
                    starshipAI.SetMove(Input.GetKey(KeyCode.LeftControl) ? selectionPlaneHeight.position : hit.point, formationHelper);
                    if (Input.GetKey(KeyCode.A))//wtf
                        starshipAI.aMove = true;
                    //starshipAI.isSelected = true;
                }
                playerFormations.Add(formationHelper);
                playerFormations.RemoveWhere(formation => formation.GetLength() == 0);
            }
            Debug.Log("Player formation No: " + playerFormations.Count);
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
                selectionPlaneHeightPosY -= Input.GetAxis("Mouse ScrollWheel") * 10;
                selectionPlaneHeightPosY = Mathf.Clamp(selectionPlaneHeightPosY, -50, 50);
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

    void ClearSelected()
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
            //tr.GetComponent<Outline>().enabled = false;
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
        }
        else if (starship.tag == "Enemy")
        {
            enemyStarships.Remove(starship);
            selectedEnemyStarships.Remove(starship);
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
        selectedPlayerStarships.RemoveWhere(starship => starship.GetComponent<StarshipAI>().typeIndex != index);//optimize this pls!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        selectedCountByTypeIndex = new float[] { 0, 0, 0, 0, 0 };
        selectedCountByTypeIndex[index] = selectedPlayerStarships.Count;
        UpdateUIActive();
    }

    public void OnStarshipTypeRightClick(int index)
    {
        GameObject infoPanel = starshipIcons[index].FindObject("InfoPanel");
        infoPanel.SetActive(!infoPanel.activeSelf);
    }
}
