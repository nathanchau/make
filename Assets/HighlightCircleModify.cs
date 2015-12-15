using UnityEngine;
using System.Collections;

public class HighlightCircleModify : MonoBehaviour {
    public SpriteRenderer circleRenderer;
    public Camera mainCamera;
    public bool isHighlighted = false;
    public WorldPos currentPos = new WorldPos(0, 0, 0);

	// Use this for initialization
	void Start () {
        circleRenderer.enabled = false;
	}
	
	// Update is called once per frame
	void Update () {
        transform.LookAt(mainCamera.transform);
        transform.position = WorldPos.VectorFromWorldPos(currentPos);
        if (isHighlighted)
            circleRenderer.enabled = true;
        else
            circleRenderer.enabled = false;
	}
}
