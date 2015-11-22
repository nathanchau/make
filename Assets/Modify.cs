using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Allows for _creation and deletion of blocks
// as well as navigation around the object

public class Modify : MonoBehaviour
{
	public GameObject cursorCube;

    Vector2 rot;
	private Vector3 point; //coordinate/point where the camera will look [ ]-Set to center of gravity
	WorldPos cursorPos;

    public bool inPenMode = false; // Two modes for drawing - pen mode, and free paint mode

    // Set of variables for click, hold, drag painting of blocks
    private const float clickHoldDuration = 0.05f;
    private float lastClickTime;
    private RaycastHit lastHit = default(RaycastHit);
    private int numVerticesOnCurrentPlane = 0;

    // List of positions of cubes that have been added
    List<WorldPos> posList = new List<WorldPos>();

    // Pen Mode Variables
    // List of vertices that have been added (for pen mode)
    List<WorldPos> vertexPosList = new List<WorldPos>();
    // Current Plane
    Plane currentPlane = new Plane();

    void Start()
	{
		point = new Vector3 (0.0f, 0.0f);
		transform.LookAt (point);
	}

    void Update()
    {
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

        // Toggle between pen mode and free paint mode
        if (Input.GetKeyDown(KeyCode.Space))
        {
            inPenMode = !inPenMode;
        }

        // Continuously change cursor cube position to be at position where block would be placed
        RaycastHit mouseHit;

		if (Physics.Raycast (Camera.main.ScreenPointToRay (Input.mousePosition), out mouseHit, 200)) {
			cursorPos = EditTerrain.GetBlockPos(mouseHit, true);
			cursorCube.transform.position = new Vector3(cursorPos.x, cursorPos.y, cursorPos.z);
		}

        if (!inPenMode) // Free Paint Mode
        {
            if (Input.GetMouseButtonDown(1))
            { // Right Click
                RaycastHit hit;
                if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 200))
                {
                    EditTerrain.SetBlock(hit, new BlockAir());
                }
            }
            else if (Input.GetMouseButtonDown(0))
            { // Left Click
                lastClickTime = Time.time;
                posList = new List<WorldPos>();
                RaycastHit hit;
                if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 200))
                {
                    lastHit = hit;
                    // Check to make sure that we're beside grass block
                    // This ensures that we add at max 1 layer painting
                    if (EditTerrain.IsAdjacentBlockGrass(hit, true))
                    {
                        // Check to make sure the block we're setting is air
                        Block block = EditTerrain.GetBlock(hit, true);
                        if (block is BlockAir)
                        {
                            WorldPos placedPos = EditTerrain.SetBlock(hit, new BlockTemp(), true);
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
                        if (EditTerrain.IsAdjacentBlockGrass(hit, true))
                        {
                            // Check to make sure the block we're setting is air
                            Block block = EditTerrain.GetBlock(hit, true);
                            if (block is BlockAir)
                            {
                                // Instead of just setting block at current position, we want to set all blocks that
                                //  we might have missed instead - so we interpolate between last position and current
                                //  and set all blocks we intersected
                                List<WorldPos> placedPosList = EditTerrain.SetAllBlocksBetween(lastHit, hit, new BlockTemp(), true);
                                posList.AddRange(placedPosList);
                                lastHit = hit;
                            }
                        }
                    }
                }
                else
                {
                    //lastHit = default(RaycastHit);
                }
            }

            if (Input.GetMouseButtonUp(0))
            {
                // Change all newly added blocks to the right block type
                RaycastHit hit;
                if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 200))
                {
                    EditTerrain.SetAllBlocksGivenPos(posList, hit, new BlockGrass());
                }
                else
                {
                    EditTerrain.SetAllBlocksGivenPos(posList, lastHit, new BlockGrass());
                }

                // Null out lastHit
                lastHit = default(RaycastHit);
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
                    if (EditTerrain.IsAdjacentBlockGrass(hit, true))
                    {
                        // Check to make sure the block we're setting is air
                        Block block = EditTerrain.GetBlock(hit, true);
                        if (block is BlockAir)
                        {
                            // Instead of just setting block at current position, we want to set all blocks 
                            //  between as well - so we interpolate between last position and current
                            //  and set all blocks we intersected
                            List<WorldPos> placedPosList = EditTerrain.SetAllBlocksBetween(lastHit, hit, new BlockTemp(), true);
                            posList.AddRange(placedPosList);
                            vertexPosList.Add(placedPosList[placedPosList.Count - 1]);
                            lastHit = hit;
                            numVerticesOnCurrentPlane++;
                        }
                    }

                    // If 3 points placed so far, then have to fill in the plane
                    if (numVerticesOnCurrentPlane == 3)
                    {
                        Vector3 p1 = WorldPos.VectorFromWorldPos(vertexPosList[0]);
                        Vector3 p2 = WorldPos.VectorFromWorldPos(vertexPosList[1]);
                        Vector3 p3 = WorldPos.VectorFromWorldPos(vertexPosList[2]);
                        currentPlane = Plane.newPlaneWithPoints(p1, p2, p3);
                        // Fill in the plane

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
                            // Fill in the plane again

                        }
                    }
                }
                else // If raycast hits nothing
                {
                }
            }
            if (Input.GetMouseButtonDown(1))
            { // Right Click
                // Change all newly added blocks to the right block type
                RaycastHit hit;
                if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 200))
                {
                    EditTerrain.SetAllBlocksGivenPos(posList, hit, new BlockGrass());
                }
                else
                {
                    EditTerrain.SetAllBlocksGivenPos(posList, lastHit, new BlockGrass());
                }

                // Null out lastHit
                lastHit = default(RaycastHit);
            }

        }



        // Navigation
        transform.position += transform.forward * Input.GetAxis ("Vertical"); // ZOOM IN
		// Orbit the origin
		transform.position += transform.up * Input.GetAxis ("Zoom"); // Strafe up and down
		transform.position += transform.right * Input.GetAxis ("Horizontal"); // Strafe left and right
		transform.LookAt (point);
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