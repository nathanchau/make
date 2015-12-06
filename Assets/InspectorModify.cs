using UnityEngine;
using System.Collections;

public class InspectorModify : MonoBehaviour {

	public GameObject mainCamera;


	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
		// Make the inspector look at you
		Vector3 lookPosition = mainCamera.transform.position;
		// [ ] - 6 is a placeholder - make it just so that it's straight up and down
		transform.LookAt(new Vector3(lookPosition.x, 6 + lookPosition.y/5.0f, lookPosition.z));
		transform.Rotate(new Vector3(0, 180, 0));
	}
}
