using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class InspectorModify : MonoBehaviour {

	public Camera mainCamera;
    public Image inspector;
    public GameObject inspectorPlaceholder;
    public Button inspectorSection;

    public bool isMinimized = true;

	// Default scale for inspector
	public const float defaultScale = 0.05f;
	public float scaleFactor = 1.0f;

	// The shape the inspector is describing
	public Shape shape;

    // The list of sections in the inspector
    private List<Button> sections = new List<Button>();

	// Use this for initialization
	void Start () {
        isMinimized = true;
        recalculateInspectorLayout();
    }

    // Update is called once per frame
    void Update () {

        if (isMinimized)
        {
            CanvasGroup inspectorCanvasGroup = inspector.GetComponent<CanvasGroup>();
            inspectorCanvasGroup.interactable = false;
            inspectorCanvasGroup.blocksRaycasts = false;
            inspectorCanvasGroup.alpha = 0;
            CanvasGroup inspectorPlaceholderCanvasGroup = inspectorPlaceholder.GetComponent<CanvasGroup>();
            inspectorPlaceholderCanvasGroup.interactable = true;
            inspectorPlaceholderCanvasGroup.blocksRaycasts = true;
            inspectorPlaceholderCanvasGroup.alpha = 1;
        }
        else
        {
            CanvasGroup inspectorCanvasGroup = inspector.GetComponent<CanvasGroup>();
            inspectorCanvasGroup.interactable = true;
            inspectorCanvasGroup.blocksRaycasts = true;
            inspectorCanvasGroup.alpha = 1;
//			if (UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject)
//			{
//				if (UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject.tag == "inspector")
//					inspectorCanvasGroup.alpha = 1;
//			}
			CanvasGroup inspectorPlaceholderCanvasGroup = inspectorPlaceholder.GetComponent<CanvasGroup>();
            inspectorPlaceholderCanvasGroup.interactable = false;
            inspectorPlaceholderCanvasGroup.blocksRaycasts = false;
            inspectorPlaceholderCanvasGroup.alpha = 0;
        }

        //rewriteInspectorCode();

        if (shape.vertices.Count > 0)
		{
			// Position the inspector
			// Depth should be middle depth of current object
			// Position should be to the right of object from camera's perspective
			
			// Convert all block positions to screen space
			List<Vector3> screenSpacePosList = new List<Vector3>();
			foreach (List<WorldPos> tempPosList in shape.vertices)
			{
                foreach (WorldPos worldPos in tempPosList)
                {
                    Vector3 screenPos = mainCamera.WorldToScreenPoint(WorldPos.VectorFromWorldPos(worldPos));
                    screenSpacePosList.Add(screenPos);
                }
            }
			// Find right-most and
			// Find center of mass - we'll use this for the depth and y-axis value
			Vector3 screenPosSum = new Vector3(0.0f, 0.0f, 0.0f);
			float farRight = 0.0f;
			foreach (Vector3 screenPos in screenSpacePosList)
			{
				if (screenPos.x > farRight)
					farRight = screenPos.x;
				screenPosSum += screenPos;
			}
			Vector3 screenPosCenter = screenPosSum/screenSpacePosList.Count;
			// screenPosRight gives the point at the far right side of the object in the center of mass
			Vector3 screenPosRight = new Vector3(farRight, screenPosCenter.y, screenPosCenter.z);

			scaleFactor = 1 + (screenPosRight.z - 35.0f) / 35.0f; // This was arrived at empirically
			scaleFactor = Mathf.Max(scaleFactor, 0.0f);

			// Add a bit to get the position we want the inspector to be centered at
			// [ ] - Should this be scaled?
			screenPosRight += new Vector3(160, 50, -3);
			
			// Convert back to world space
			Vector3 worldPosRight = mainCamera.ScreenToWorldPoint(screenPosRight);
			
			// Position the inspector
			transform.position = worldPosRight;
			
			// Scale the inspector
			// Based on the distance from inspector and main camera
			transform.localScale = new Vector3(scaleFactor * defaultScale, scaleFactor * defaultScale, scaleFactor * defaultScale);
			
			// Make the inspector panel look at you
			Vector3 lookPosition = mainCamera.transform.position;
			// [ ] - 6 is a placeholder - make it just so that it's straight up and down
			transform.LookAt(new Vector3(lookPosition.x, lookPosition.y/1.0f, lookPosition.z), mainCamera.transform.up);
			transform.Rotate(new Vector3(0, 180, 0));
		}
		else
		{
			// Just minimize the inspector and send it far far away if not in use
			isMinimized = true;
			transform.position = new Vector3(10000, 10000, 10000);
		}
	}

    public void SetInspectorMinimized(bool newMinimized)
    {
        isMinimized = newMinimized;
		// Deselect input field 
		UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
        recalculateInspectorLayout();
    }

	public void recalculateInspectorLayout()
	{
        float yval = 0.0f;
        for (int i = 0; i < shape.vertices.Count; i++)
        {
            // Lazily instantiate a new inspector section
            Button section;
            if (i < sections.Count)
                section = sections[i];
            else
            {
                section = Instantiate(inspectorSection);
                sections.Add(section);
                section.transform.SetParent(inspector.transform);
                section.transform.SetSiblingIndex(0);
                InspectorSectionModify tempSectionModify = section.GetComponent<InspectorSectionModify>();
                tempSectionModify.inspectorModify = this;
            }
            // Set position
            RectTransform sectionTransform = section.GetComponent<RectTransform>();
            sectionTransform.anchoredPosition3D = new Vector3(-125, yval, 0);
            sectionTransform.localEulerAngles = new Vector3(0, 0, 0);
            sectionTransform.localScale = new Vector3(1, 1, 1);
            // Set title
            Text sectionTitle = section.GetComponentInChildren<Text>();
            if (sectionTitle)
                sectionTitle.text = string.Format("plane {0}", i+1);
            // Set text in text editor
            Text sectionText = null;
            Text[] sectionTexts = section.GetComponentsInChildren<Text>();
            foreach (Text currentText in sectionTexts)
            {
                if (currentText.transform.tag == "inspector")
                {
                    sectionText = currentText;
                    break;
                }
            }
            string codeString = "";
            foreach (WorldPos pos in shape.vertices[i])
            {
                codeString += string.Format("vertex{0} = ({1}, {2}, {3});\n", shape.vertices[i].IndexOf(pos), pos.x, pos.y, pos.z);
            }
            codeString += "\n";
            if (sectionText)
                sectionText.text = codeString;
            // Increment counter for next section's position
            InspectorSectionModify sectionModify = section.GetComponent<InspectorSectionModify>();
            if (sectionModify)
            {
                if (sectionModify.isMinimized)
                    yval -= 31.0f;
                else
                {
                    yval -= (31.0f + sectionText.rectTransform.rect.height);
                }
            }
        }

        // Change inspector size
        float inspectorHeight = -yval + 40.0f;
        //inspectorHeight = Mathf.Max(inspectorHeight, 120.0f);
        // [ ] - Maybe change this eventually
        inspectorHeight = Mathf.Min(inspectorHeight, 500.0f);
        inspector.rectTransform.sizeDelta = new Vector2(250.0f, inspectorHeight);
    }

    private void setOnlySectionExpanded(int sectionIndex)
    {
        for (int i = 0; i < sections.Count; i++)
        {
            if (i == sectionIndex)
            {
                InspectorSectionModify sectionModify = sections[i].GetComponent<InspectorSectionModify>();
                if (sectionModify)
                    sectionModify.isMinimized = false;
            }
            else
            {
                InspectorSectionModify sectionModify = sections[i].GetComponent<InspectorSectionModify>();
                if (sectionModify)
                    sectionModify.isMinimized = true;
            }
        }
    }
	public void minimizeAllSections()
	{
		foreach (Button section in sections)
		{
			InspectorSectionModify sectionModify = section.GetComponent<InspectorSectionModify>();
			if (sectionModify)
				sectionModify.isMinimized = true;
		}
	}

	public void destroyAllSections()
	{
		foreach (Button section in sections)
		{
			Destroy(section.gameObject);
		}
		sections = new List<Button>();
	}
}
