using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class InspectorSectionModify : MonoBehaviour {

    public Text sectionTitle;
    public Image chevron;
    public Image highlight;
    public InputField textEditor;

    public bool isMinimized = true;

    public InspectorModify inspectorModify;

	// Use this for initialization
	void Start () {
	    
	}
	
	// Update is called once per frame
	void Update () {
        CanvasGroup textEditorCanvasGroup = textEditor.GetComponent<CanvasGroup>();
        CanvasGroup highlightCanvasGroup = highlight.GetComponent<CanvasGroup>();
        if (isMinimized)
        {
            // Hide text editor
            textEditorCanvasGroup.alpha = 0;
            textEditorCanvasGroup.interactable = false;
            textEditorCanvasGroup.blocksRaycasts = false;
            // Hide highlight
            highlightCanvasGroup.alpha = 0;
            highlightCanvasGroup.interactable = false;
            highlightCanvasGroup.blocksRaycasts = false;
            // Chevron pointing right
            chevron.rectTransform.localEulerAngles = new Vector3(0, 0, 90);
        }
        else
        {
            // Show text editor
            textEditorCanvasGroup.alpha = 1;
            textEditorCanvasGroup.interactable = true;
            textEditorCanvasGroup.blocksRaycasts = true;
            // Show highlight
            highlightCanvasGroup.alpha = 1;
            highlightCanvasGroup.interactable = true;
            highlightCanvasGroup.blocksRaycasts = true;
            // Chevron pointing down
            chevron.rectTransform.localEulerAngles = new Vector3(0, 0, 0);
        }
    }

    public void setSectionMinimized(bool newMinimized)
    {
        isMinimized = newMinimized;
        // [ ] - If you're maximizing the section, should you focus the text editor?
        UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
        inspectorModify.recalculateInspectorLayout();
    }

    public void toggleSectionMinimized()
    {
        isMinimized = !isMinimized;
        UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
        inspectorModify.recalculateInspectorLayout();
    }
}
