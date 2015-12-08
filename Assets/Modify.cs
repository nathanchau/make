using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

// Allows for _creation and deletion of blocks
// as well as navigation around the object

public class Modify : MonoBehaviour
{
	public GameObject cursorCube;
	public GameObject guidePlane;
	public World world;
	public InspectorModify inspectorModify;

    Vector2 rot;
	private Vector3 point; //coordinate/point where the camera will look [ ]-Set to center of gravity
	WorldPos cursorPos;

	// Variable to keep track of guide plane rotation
	private int guidePlaneConfig = 0;

    // Set of variables for click, hold, drag painting of blocks
    private const float clickHoldDuration = 0.1f;
    private float lastClickTime;
    private RaycastHit lastHit = default(RaycastHit);
	private RaycastHit firstHitOnPlane = default(RaycastHit);
    private int numVerticesOnCurrentPlane = 0;

    // List of positions of cubes that have been added
    List<WorldPos> posList = new List<WorldPos>();

    // Pen Mode Variables
	public bool inPenMode = false; // Two modes for drawing - pen mode, and free paint mode
	bool isFirstPoint = true;
	bool firstPlaneSet = false;
	List<WorldPos> fillPosList = new List<WorldPos>(); // List of positions that have been added for polygon fill
    // Current Plane
    Plane currentPlane = new Plane();
	List<List<WorldPos>> edgeList = new List<List<WorldPos>>(); // List of edges used for current polygon
	List<WorldPos> vertexPosList = new List<WorldPos>(); // List of vertices used for current polygon
	// For previous plane
	List<List<WorldPos>> previousEdgeList = new List<List<WorldPos>>();
	List<WorldPos> previousVertexPosList = new List<WorldPos>();
	// For lofting planes
	List<WorldPos> loftFillPosList = new List<WorldPos>(); // List of positions that have been added for all polygon fills for loft

    void Start()
	{
		point = new Vector3 (0.0f, 0.0f);
		transform.LookAt (point);
		inspectorModify.vertexPosList = vertexPosList;
	}

    void Update()
    {
		// Navigation 
        // Reset to origin as center of orbit
        if (Input.GetKeyDown(KeyCode.R))
        {
            MoveCameraToPoint(new Vector3(0.0f, 0.0f, 0.0f));
        }

        // Change origin to new point
        if (Input.GetMouseButtonDown(2))
        {
            RaycastHit hit;
            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 200))
            {
                WorldPos pos = EditTerrain.GetBlockPos(hit);
                MoveCameraToPoint(new Vector3(pos.x, pos.y, pos.z));
            }
        }

		// Zoom in and out
		transform.position += transform.forward * Input.GetAxis ("Vertical"); // ZOOM IN
		// Orbit the origin
		transform.position += transform.up * Input.GetAxis ("Zoom"); // Strafe up and down
		transform.position += transform.right * Input.GetAxis ("Horizontal"); // Strafe left and right
		transform.LookAt (point);

		// Input Mode
        // Toggle between pen mode and free paint mode
        if (Input.GetKeyDown(KeyCode.Space))
        {
            inPenMode = !inPenMode;
			Renderer renderer = cursorCube.GetComponent<Renderer>();
			if (inPenMode)
				renderer.material.SetColor("_EmissionColor", new Color(0.1F, 0.1F, 0.643F));
			else
				renderer.material.SetColor("_EmissionColor", new Color(0.1F, 0.643F, 0.1F));
		}

		// Cursor
        // Continuously change cursor cube position to be at position where block would be placed
        RaycastHit cursorHit;

		if (Physics.Raycast (Camera.main.ScreenPointToRay (Input.mousePosition), out cursorHit, 200)) {
			cursorPos = EditTerrain.GetBlockPos(cursorHit, true);
			cursorCube.transform.position = new Vector3(cursorPos.x, cursorPos.y, cursorPos.z);
		}

		// Guide Plane
		// If button is pressed down, then move plane around
		if (Input.GetKeyDown(KeyCode.Z))
		{
			Collider[] colChildren = guidePlane.GetComponentsInChildren<Collider>();
			foreach (Collider collider in colChildren) {  
				collider.enabled = false;
			}
		}
		if (Input.GetKey(KeyCode.Z))
		{
			if (Physics.Raycast (Camera.main.ScreenPointToRay (Input.mousePosition), out cursorHit, 200)) {
				cursorPos = EditTerrain.GetBlockPos(cursorHit, true);
				guidePlane.transform.position = new Vector3(cursorPos.x, cursorPos.y, cursorPos.z);
			}
		}
		if (Input.GetKeyUp(KeyCode.Z))
		{
			Collider[] colChildren = guidePlane.GetComponentsInChildren<Collider>();
			foreach (Collider collider in colChildren) {  
				collider.enabled = true;
			}
		}
		// Cycle through configurations of guide plane
		if (Input.GetKeyDown(KeyCode.X))
		{
			if (guidePlaneConfig == 0)
			{
				guidePlane.transform.rotation = Quaternion.Euler(90, 0, 0);
				guidePlaneConfig++;
			}
			else if (guidePlaneConfig == 1)
			{
				guidePlane.transform.rotation = Quaternion.Euler(0, 0, 90);
				guidePlaneConfig++;
			}
			else if (guidePlaneConfig == 2)
			{
				guidePlane.transform.rotation = Quaternion.Euler(0, 0, 0);
				guidePlaneConfig = 0;
			}
		}

        if (!inPenMode) // Free Paint Mode
        {
            if (Input.GetMouseButtonDown(1))
            { // Right Click to Erase
                RaycastHit hit;
                if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 200))
                {
                    EditTerrain.SetBlock(hit, world, new BlockAir());
                }
            }
            else if (Input.GetMouseButtonDown(0))
            { // Left Click to Paint
                lastClickTime = Time.time;
                posList = new List<WorldPos>();
                RaycastHit hit;
                if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 200))
                {
                    lastHit = hit;
                    // Check to make sure that we're beside grass block
                    // This ensures that we add at max 1 layer painting
                    if (EditTerrain.IsAdjacentBlockGrass(hit, world, true) || hit.collider.tag == "guide")
                    {
                        // Check to make sure the block we're setting is air
                        Block block = EditTerrain.GetBlock(hit, world, true);
                        if (block is BlockAir || block == null)
                        {
							WorldPos placedPos = EditTerrain.SetBlock(hit, world, new BlockTemp(), true);
                            posList.Add(placedPos);
                        }
                    }
                }
                else
                {
                    lastHit = default(RaycastHit);
                }
            }

            if (Input.GetMouseButton(0)) // Left Click Hold
            {
                RaycastHit hit;
                if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 200))
                {
                    // Check to make sure enough time has elapsed
                    //   We don't want intended single clicks to result in multiple blocks being placed
                    if ((Time.time - lastClickTime) >= clickHoldDuration)
                    {
                        // Check to make sure that we're beside grass block
                        // This ensures that we add at max 1 layer painting
                        if (EditTerrain.IsAdjacentBlockGrass(hit, world, true))
                        {
                            // Check to make sure the block we're setting is air
                            Block block = EditTerrain.GetBlock(hit, world, true);
                            if (block is BlockAir || block == null)
                            {
                                // Instead of just setting block at current position, we want to set all blocks that
                                //  we might have missed instead - so we interpolate between last position and current
                                //  and set all blocks we intersected
                                List<WorldPos> placedPosList = EditTerrain.SetAllBlocksBetween(lastHit, hit, world, new BlockTemp(), true);
                                posList.AddRange(placedPosList);
                                lastHit = hit;
                            }
                        }
                    }
                }
            }

            if (Input.GetMouseButtonUp(0))
            {
                // Change all newly added blocks to the right block type
                RaycastHit hit;
                if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 200))
                {
                    EditTerrain.SetAllBlocksGivenPos(world, posList, hit, new BlockGrass());
                }
                else
                {
                    EditTerrain.SetAllBlocksGivenPos(world, posList, lastHit, new BlockGrass());
                }

                // Null out lastHit
                lastHit = default(RaycastHit);

				// Null out poslist
				posList = new List<WorldPos>();
            }
        }
        else if (inPenMode) // In Pen Mode
        {
            if (Input.GetMouseButtonDown(0))
            { // Left Click
                lastClickTime = Time.time;
                RaycastHit hit;
                if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 200))
                {
                    // Check to make sure that we're beside grass block
                    // This ensures that we add at max 1 layer painting
                    if (EditTerrain.IsAdjacentBlockGrass(hit, world, true))
                    {
                        // Check to make sure the block we're setting is air
                        Block block = EditTerrain.GetBlock(hit, world, true);
                        if (block is BlockAir || block == null)
                        {
                            // Instead of just setting block at current position, we want to set all blocks 
                            //  between as well - so we interpolate between last position and current
                            //  and set all blocks we intersected
                            List<WorldPos> placedEdgePosList = EditTerrain.SetAllBlocksBetween(lastHit, hit, world, new BlockTemp(), true);
                            posList.AddRange(placedEdgePosList);

							// Add to edgeList and vertexPosList
                            // If it's the first point being placed, it's going to try to set an edge from null to this point - don't let it
                            if (isFirstPoint)
                                isFirstPoint = false;
                            else
                                edgeList.Add(placedEdgePosList);

							vertexPosList.Add(placedEdgePosList[placedEdgePosList.Count - 1]);
							//Debug.Log("z being placed: " + placedEdgePosList[placedEdgePosList.Count - 1].z);
                            lastHit = hit;

                            numVerticesOnCurrentPlane++;
							if (numVerticesOnCurrentPlane == 1)
								firstHitOnPlane = hit;

                            // If 2 points so far
                            // Draw edge back as well, so that we don't break loft algorithm
                            if (numVerticesOnCurrentPlane == 2)
							{
								// Technically we've already drawn this exact edge, so we'll just add to the
								//  edgeList. We also add to the fillPosList because this is an inferred edge
								edgeList.Add(edgeList[edgeList.Count - 1]);
								fillPosList.AddRange(edgeList[edgeList.Count - 1]);
							}
							// Drawing bounding planes
							// If 3 points placed so far
							else if (numVerticesOnCurrentPlane == 3)
							{
								Vector3 p1 = WorldPos.VectorFromWorldPos(vertexPosList[0]);
								Vector3 p2 = WorldPos.VectorFromWorldPos(vertexPosList[1]);
								Vector3 p3 = WorldPos.VectorFromWorldPos(vertexPosList[2]);
								//Debug.Log(p1.z + "," + p2.z + "," + p3.z);
								currentPlane = Plane.newPlaneWithPoints(p1, p2, p3);
								
								// Remove previously inferred edge
								edgeList.RemoveAt(edgeList.Count - 2); // -2 because that's position of end-beginning edge from last time
								
								// Add new edge
								// [ ] - Slight problem here - erases first block - add an optional variable to function
								List<WorldPos> placedPosList = EditTerrain.SetAllBlocksBetween(hit, firstHitOnPlane, world, new BlockTemp(), true);
								fillPosList.AddRange(placedPosList);
								edgeList.Add(placedPosList);
								
								// Set blocks in the planar polygon
								placedPosList = EditTerrain.SetAllBlocksInPlane(world, posList.Concat(fillPosList).ToList(), vertexPosList, edgeList, currentPlane, hit, new BlockTemp());
								fillPosList.AddRange(placedPosList);
							}
							// Else if >3 points placed so far, first have to check if new point is coplanar
							//  If point is coplanar, then have to refill plane
							//  If points isn't coplanar, then reset counter to 1
							else if (numVerticesOnCurrentPlane > 3)
							{
								// Check if point is coplanar
								Vector3 currentPoint = WorldPos.VectorFromWorldPos(vertexPosList[vertexPosList.Count - 1]);
								if (Plane.isCoplanar(currentPlane, currentPoint))
								{
									// Erase current fill
									EditTerrain.SetAllBlocksGivenPos(world, fillPosList, hit, new BlockAir());
									fillPosList = new List<WorldPos>();
									
									// Remove the previously inferred edge
									edgeList.RemoveAt(edgeList.Count - 2); // -2 because that's position of end-beginning edge from last time
									
									// Add edge
									List<WorldPos> placedPosList = EditTerrain.SetAllBlocksBetween(hit, firstHitOnPlane, world, new BlockTemp(), true);
									fillPosList.AddRange(placedPosList);
									edgeList.Add(placedPosList);
									
									// Fill in the plane again
									placedPosList = EditTerrain.SetAllBlocksInPlane(world, posList.Concat(fillPosList).ToList(), vertexPosList, edgeList, currentPlane, hit, new BlockTemp());
									fillPosList.AddRange(placedPosList);
								}
								else {
									// The first plane has been set
									firstPlaneSet = true;
									
									// Store previous plane info
									previousEdgeList = new List<List<WorldPos>>(edgeList);
									previousEdgeList.RemoveAt(previousEdgeList.Count - 1);
									previousVertexPosList = new List<WorldPos>(vertexPosList);
									previousVertexPosList.RemoveAt(previousVertexPosList.Count - 1);
									//foreach (List<WorldPos> edge in previousEdgeList)
									//{
									//    Debug.Log("previousEdgeList first: " + edge[0].x + "," + edge[0].y + "," + edge[0].z + " count: " + edge.Count + " / last: " + edge[edge.Count - 1].x + "," + edge[edge.Count - 1].y + "," + edge[edge.Count - 1].z);
									//}
									
									// Reset plane variables
									currentPlane = new Plane();
									numVerticesOnCurrentPlane = 1;
									firstHitOnPlane = hit;
									WorldPos tempPos = vertexPosList[vertexPosList.Count - 1];
									vertexPosList = new List<WorldPos>();
									vertexPosList.Add(tempPos);
									edgeList = new List<List<WorldPos>>();
								}
							}
                            //foreach (List<WorldPos> edge in edgeList)
                            //{
                            //    Debug.Log("edgeList with #vertices= " + numVerticesOnCurrentPlane + " first: " + edge[0].x + "," + edge[0].y + "," + edge[0].z + " count: " + edge.Count + " / last: " + edge[edge.Count - 1].x + "," + edge[edge.Count - 1].y + "," + edge[edge.Count - 1].z);
                            //}

                            // Drawing lofting planes
                            if (firstPlaneSet)
							{
								// Erase previous lofting plane
								EditTerrain.SetAllBlocksGivenPos(world, loftFillPosList, hit, new BlockAir());
								// Set new lofting plane
								loftFillPosList = EditTerrain.LoftAndFillPlanes(previousVertexPosList, previousEdgeList, vertexPosList, edgeList, hit, world, new BlockTemp());
							}

                            // Set vertices again so we get different coloured vertex
                            foreach (WorldPos pos in vertexPosList)
                            {
                                EditTerrain.SetAllBlocksBetweenPos(pos, pos, world, hit, new BlockTempVertex());
                            }
                            foreach (WorldPos pos in previousVertexPosList)
                            {
                                EditTerrain.SetAllBlocksBetweenPos(pos, pos, world, hit, new BlockTempVertex());
                            }
                        }
                    }
                }
            }
            if (Input.GetMouseButtonDown(1))
            { // Right Click
                // Change all newly added blocks to the right block type
				posList.AddRange(fillPosList);
				posList.AddRange(loftFillPosList);
                RaycastHit hit;
                if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 200))
                {
                    EditTerrain.SetAllBlocksGivenPos(world, posList, hit, new BlockGrass());
                }
                else
                {
                    EditTerrain.SetAllBlocksGivenPos(world, posList, lastHit, new BlockGrass());
                }

				// Reset everything
                // Null out lastHit
                lastHit = default(RaycastHit);

				// Set number of vertices back to 0
				numVerticesOnCurrentPlane = 0;

				// Reset first counters
                isFirstPoint = true;
				firstPlaneSet = false;

				// Null out poslists
				posList = new List<WorldPos>();
				edgeList = new List<List<WorldPos>>();
				vertexPosList = new List<WorldPos>();
				inspectorModify.vertexPosList = vertexPosList;
				fillPosList = new List<WorldPos>();
				loftFillPosList = new List<WorldPos>();
            }

        }
	}

    void MoveCameraToPoint(Vector3 newPoint)
    {
        // Todo: Animate this change of position!!! with a blink? or moving the camera?

        // Change position of camera to new position based on offset between new point and current point
        Vector3 pointDiff = newPoint - point;
        transform.position += pointDiff;

        // Change origin of orbit to new point
        point = newPoint;

        // Later in Update: transform.LookAt(point); 
        // Todo: Think about changing this structure
    }
}