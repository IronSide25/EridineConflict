using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class SelectionManager : MonoBehaviour
{
    public static SelectionManager globalAccess;

    public List<Transform> selectedStarships;
    public List<Transform> allStarships; // rename and move this somewhere else XD
    private Vector3 dragStartPosition;
    private const float sqrMinDragDistance = 100;
    public RectTransform selectionBoxRect;
    public GameObject selectionGO;

    public float currentSelectionHeight;
    // Start is called before the first frame update
    void Start()
    {
        globalAccess = this;
        selectedStarships = new List<Transform>();

        StarshipSteering[] starships = FindObjectsOfType<StarshipSteering>();
        foreach (StarshipSteering starship in starships)
            if(starship.gameObject.tag == "Player")
            allStarships.Add(starship.transform);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            dragStartPosition = Input.mousePosition;

            if (!(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
                ClearSelected();

            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit, Camera.main.farClipPlane, 1 << 8))
            {
                Transform objectHit = hit.transform;
                Outline outline = objectHit.GetComponent<Outline>();
                if (outline)
                {
                    outline.enabled = true;
                    selectedStarships.Add(objectHit);
                    HealthManager healthManager = objectHit.GetComponent<HealthManager>();
                    if (healthManager)
                        healthManager.AddHealthBar();

                }
            }
        }
        else if (Input.GetMouseButton(0))
        {
            if(Vector3.SqrMagnitude(dragStartPosition - Input.mousePosition) > sqrMinDragDistance)
            {
                selectionGO.SetActive(true);
                selectionBoxRect.position = dragStartPosition - ((dragStartPosition - Input.mousePosition) / 2);
                float sizeX = Mathf.Abs(dragStartPosition.x - Input.mousePosition.x);
                float sizeY = Mathf.Abs(dragStartPosition.y - Input.mousePosition.y);
                selectionBoxRect.sizeDelta = new Vector2(sizeX, sizeY);
                ClearSelected();
            }            
        }
        else if(Input.GetMouseButtonUp(0))
        {
            if(Vector3.SqrMagnitude(dragStartPosition - Input.mousePosition) > sqrMinDragDistance)
            {
                Rect selectionBox = new Rect(Mathf.Min(dragStartPosition.x, Input.mousePosition.x), Mathf.Min(dragStartPosition.y, Input.mousePosition.y),
                Mathf.Abs(dragStartPosition.x - Input.mousePosition.x), Mathf.Abs(dragStartPosition.y - Input.mousePosition.y));
                foreach (Transform tr in allStarships)
                {
                    Vector3 screenSpace = Camera.main.WorldToScreenPoint(tr.position);
                    screenSpace.z = 0;
                    if (selectionBox.Contains(screenSpace) && tr.tag == "Player")
                    {
                        Outline outline = tr.GetComponent<Outline>();
                        if (outline)
                        {
                            outline.enabled = true;
                            selectedStarships.Add(tr);
                            HealthManager healthManager = tr.GetComponent<HealthManager>();
                            if (healthManager)
                                healthManager.AddHealthBar();
                        }
                    }
                }
                selectionGO.SetActive(false);
            }          
        }


        if (Input.GetMouseButtonDown(1))
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, 1 << 9))
            {
                foreach (Transform starshipTr in selectedStarships)
                {
                    StarshipSteering starshipScript = starshipTr.GetComponent<StarshipSteering>();
                    //starshipScript.SetDestination(hit.point);
                    starshipScript.SetDestinationFormation(hit.point, selectedStarships.ToArray());
                }
            }           
        }
    }

    void ClearSelected()
    {
        foreach(Transform tr in selectedStarships)
        {
            tr.GetComponent<Outline>().enabled = false;
            HealthManager healthManager = tr.GetComponent<HealthManager>();
            if (healthManager)
                healthManager.RemoveHealthBar();
        }
        selectedStarships.Clear();
    }
}
