﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class InspectorModify : MonoBehaviour {

	public Camera mainCamera;

	// Default scale for inspector
	public const float defaultScale = 0.05f;
	public float scaleFactor = 1.0f;

	// All edge blocks in current selection
	//  This is used to calculate the position of inspector
	public List<WorldPos> vertexPosList;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {

		if (vertexPosList.Count > 0)
		{
			// Position the inspector
			// Depth should be middle depth of current object
			// Position should be to the right of object from camera's perspective
			
			// Convert all block positions to screen space
			List<Vector3> screenSpacePosList = new List<Vector3>();
			foreach (WorldPos worldPos in vertexPosList)
			{
				Vector3 screenPos = mainCamera.WorldToScreenPoint(WorldPos.VectorFromWorldPos(worldPos));
				screenSpacePosList.Add(screenPos);
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

			scaleFactor = 1 + (screenPosRight.z - 30.0f) / 45.0f; // This was arrived at empirically
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
			transform.LookAt(new Vector3(lookPosition.x, 6 + lookPosition.y/3.0f, lookPosition.z));
			transform.Rotate(new Vector3(0, 180, 0));
		}
	}
}