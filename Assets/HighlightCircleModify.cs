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

        // Draw lines to the highlighted point
        Debug.DrawLine(WorldPos.VectorFromWorldPos(currentPos), new Vector3(currentPos.x, 0, 0), Color.white, 0.0f, false);
        Debug.DrawLine(WorldPos.VectorFromWorldPos(currentPos), new Vector3(0, currentPos.y, 0), Color.white, 0.0f, false);
        Debug.DrawLine(WorldPos.VectorFromWorldPos(currentPos), new Vector3(0, 0, currentPos.z), Color.white, 0.0f, false);
        Debug.DrawLine(new Vector3(0, 0, 0), new Vector3(currentPos.x, 0, 0), Color.white, 0.0f, false);
        Debug.DrawLine(new Vector3(0, 0, 0), new Vector3(0, currentPos.y, 0), Color.white, 0.0f, false);
        Debug.DrawLine(new Vector3(0, 0, 0), new Vector3(0, 0, currentPos.z), Color.white, 0.0f, false);
    }
}
