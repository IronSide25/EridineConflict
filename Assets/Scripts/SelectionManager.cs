using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SelectionManager : MonoBehaviour
{
    public static SelectionManager globalAccess;

    public HashSet<Transform> selectedPlayerStarships;
    public HashSet<Transform> selectedEnemyStarships;
    public List<Transform> playerStarships; // rename and move this somewhere else XD
    public List<Transform> enemyStarships; // rename and move this somewhere else XD

    private Vector3 dragStartPosition;
    private bool allowDrag = false;
    private const float sqrMinDragDistance = 100;
    public RectTransform selectionBoxRect;
    public GameObject selectionGO;

    public float currentSelectionHeight;

    public Transform selectionPlaneFloor;
    public Transform selectionPlaneHeight;
    float selectionPlaneHeightPosY = 0;

    float lightCount;
    float mediumCount;
    float heavyCount;

    public Text lightCountText;
    public Text mediumCountText;
    public Text heavyCountText;
    GameObject lightCountPanel;
    GameObject mediumCountPanel;
    GameObject heavyCountPanel;

    public GameObject setAggresivePanel;
    private Image setAggresiveImage;
    public GameObject setDefensivePanel;
    private Image setDefensiveImage;
    public GameObject setPassivePanel;
    private Image setPassiveImage;

    private EventSystem eventSystem;

    // Start is called before the first frame update
    void Awake()
    {
        globalAccess = this;
        selectedPlayerStarships = new HashSet<Transform>();
        selectedEnemyStarships = new HashSet<Transform>();
        HealthManager.OnStarshipAdded += AddStarship;
        HealthManager.OnStarshipRemoved += RemoveStarship;

        eventSystem = GameObject.Find("EventSystem").GetComponent<EventSystem>();

        lightCountPanel = lightCountText.transform.parent.gameObject;
        mediumCountPanel = mediumCountText.transform.parent.gameObject;
        heavyCountPanel = heavyCountText.transform.parent.gameObject;

        setAggresiveImage = setAggresivePanel.GetComponent<Image>();
        setDefensiveImage = setDefensivePanel.GetComponent<Image>();
        setPassiveImage = setPassivePanel.GetComponent<Image>();

        lightCount = 0;
        mediumCount = 0;
        heavyCount = 0;
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
                        if (starshipAI.starshipClass == StarshipClass.Light)
                            lightCount++;
                        else if (starshipAI.starshipClass == StarshipClass.Medium)
                            mediumCount++;
                        else
                            heavyCount++;                    
                    }                    
                }
            }

            if (Physics.Raycast(ray, out hit, Mathf.Infinity, 1 << 10))
            {
                selectedEnemyStarships.Add(hit.transform);
                AddHealthBar(hit.transform);
                StarshipAI starshipAI = hit.transform.GetComponent<StarshipAI>();
            }

            UpdateBehaviourUIIsActive();
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
                                if (starshipAI.starshipClass == StarshipClass.Light)
                                    lightCount++;
                                else if (starshipAI.starshipClass == StarshipClass.Medium)
                                    mediumCount++;
                                else
                                    heavyCount++;
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
                        StarshipAI starshipAI = tr.GetComponent<StarshipAI>();
                    }
                }
                selectionGO.SetActive(false);
            }
            allowDrag = false;
            UpdateBehaviourUIIsActive();
        }

        if (Input.GetMouseButtonDown(1))//give order
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, 1 << 10))
            {
                foreach (Transform starshipTr in selectedPlayerStarships)
                {
                    Transform[] shipsInFormation = new Transform[selectedPlayerStarships.Count - 1];
                    FormationHelper formationHelper = new FormationHelper();
                    formationHelper.shipsInFormation = new List<Transform>(selectedPlayerStarships);
                    int count = 0;
                    foreach (Transform ship in selectedPlayerStarships)
                        if (ship != starshipTr)
                        {
                            shipsInFormation[count] = ship;
                            count++;
                        }
                    StarshipAI starshipAI = starshipTr.GetComponent<StarshipAI>();
                    starshipAI.SetAttack(hit.transform, shipsInFormation, formationHelper);                   
                }
            }
            else if (Physics.Raycast(ray, out hit, Mathf.Infinity, 1 << 9))
            {
                foreach (Transform starshipTr in selectedPlayerStarships)
                {
                    Transform[] shipsInFormation = new Transform[selectedPlayerStarships.Count - 1];
                    FormationHelper formationHelper = new FormationHelper();
                    formationHelper.shipsInFormation = new List<Transform>(selectedPlayerStarships);
                    int count = 0;
                    foreach (Transform ship in selectedPlayerStarships)
                        if (ship != starshipTr)
                        {
                            shipsInFormation[count] = ship;
                            count++;
                        }                   
                    StarshipAI starshipAI = starshipTr.GetComponent<StarshipAI>();
                    starshipAI.SetMove(Input.GetKey(KeyCode.LeftControl) ? selectionPlaneHeight.position : hit.point, shipsInFormation, formationHelper);
                    if (Input.GetKey(KeyCode.A))
                        starshipAI.aMove = true;
                    starshipAI.isSelected = true;
                }
            }
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

        lightCountText.text = lightCount.ToString();
        mediumCountText.text = mediumCount.ToString();
        heavyCountText.text = heavyCount.ToString();

        lightCountPanel.SetActive(lightCount > 0);
        mediumCountPanel.SetActive(mediumCount > 0);
        heavyCountPanel.SetActive(heavyCount > 0);
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
        lightCount = 0;
        mediumCount = 0;
        heavyCount = 0;
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
                if (starshipAI.starshipClass == StarshipClass.Light)
                    lightCount--;
                else if (starshipAI.starshipClass == StarshipClass.Medium)
                    mediumCount--;
                else
                    heavyCount--;
            }
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

    void UpdateBehaviourUIIsActive()
    {
        if(selectedPlayerStarships.Count == 0)
        {
            setAggresivePanel.SetActive(false);
            setDefensivePanel.SetActive(false);
            setPassivePanel.SetActive(false);
            setAggresiveImage.enabled = false;
            setDefensiveImage.enabled = false;
            setPassiveImage.enabled = false;
        }
        else
        {
            setAggresivePanel.SetActive(true);
            setDefensivePanel.SetActive(true);
            setPassivePanel.SetActive(true);
        }
    }
}
