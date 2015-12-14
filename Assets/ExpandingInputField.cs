using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class ExpandingInputField : InputField {
	
	private RectTransform rectTransform;
	
	protected override void Start() {
		rectTransform = GetComponent<RectTransform>();
		
		onValueChanged.AddListener(new UnityEngine.Events.UnityAction<string>(ResizeInput));
	}
	
	// Resize input field as new lines get added
	private void ResizeInput(string iText) {
		string fullText = text;
		
		Vector2 extents = textComponent.rectTransform.rect.size;
		var settings = textComponent.GetGenerationSettings(extents);
		settings.generateOutOfBounds = false;
		var prefHeight = new TextGenerator().GetPreferredHeight(fullText, settings) + 16;
		
		if(prefHeight > textComponent.rectTransform.rect.height - 16 || prefHeight < textComponent.rectTransform.rect.height + 16) {
			rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, prefHeight);
		}
	}
	
}


