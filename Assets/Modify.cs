using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Allows for _creation and deletion of blocks
// as well as navigation around the object

public class Modify : MonoBehaviour
{
	public GameObject cursorCube;
	public GameObject guidePlane;
	public World world;
	public InspectorModify inspectorModify;
    public Toggle penToolToggle;
    public Toggle freePaintToggle;
    public Toggle selectToggle;

    // Holds world state
    public WorldState worldState = new WorldState();

    // Modes
    public int mode = 0;
    private static int PAINTMODE = 0;
    private static int PENMODE = 1;
    private static int SELECTMODE = 2;
    private static int INTERACTMODE = 3;

    Vector2 rot;
	private Vector3 point; //coordinate/point where the camera will look [ ]-Set to center of gravity
	WorldPos cursorPos;

	// Variable to keep track of guide plane rotation
	private int guidePlaneConfig = 0;

    // Set of variables for click, hold, drag painting of blocks
    private const float clickHoldDuration = 0.1f;
    private float lastClickTime;
    private RaycastHit lastHit = default(RaycastHit);

	public bool inInputField = false;

    // Pen Mode Variables
	bool isFirstPoint = true; // isFirstPoint also tells us whether we're in middle of pen tool creation - !isFirstPoint = inCreation
	public Shape currentShape;

	List<WorldPos> posList = new List<WorldPos>();

    // Select Mode Variables
    public HighlightCircleModify highlightCircleModify;
    public bool isDraggingVertex = false;
    public bool isDraggingShape = false;
    private WorldPos lastDragPos;
    private int dragPlaneIndex = 0;
    private int dragPosIndex = 0;
    private Vector3 dragMousePos = new Vector3(); // We're using these variables basically as a way to verify intent to move
    private float startDragTolerance = 5.0f;
    private bool startedDragging = false;

    void Start()
	{
		point = new Vector3 (0.0f, 0.0f);
		transform.LookAt (point);
		currentShape = new Shape(world, worldState, mode);
		inspectorModify.shape = currentShape;

	}

    void Update()
    {
		// Check for whether currently focusing an input field
		// If that's the case, can't use keypresses for anything else
		GameObject go = EventSystem.current.currentSelectedGameObject;
		InputField inputField = null; //creating dummy, null, InputField component
		if (go != null) 
			inputField = go.GetComponent<InputField>(); //trying to get inputField component
		//if inputField still equals null, it means user isn't in edit field
		if (inputField == null) 
			inInputField = false;
		else
			inInputField = true;

		// Navigation 
		// Reset to origin as center of orbit
		if (Input.GetKeyDown(KeyCode.R) && !inInputField)
        {
            MoveCameraToPoint(new Vector3(0.0f, 0.0f, 0.0f));
        }

        // Change origin to new point
        if (Input.GetKeyDown(KeyCode.T) && !inInputField)
        {
            RaycastHit hit;
            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 200))
            {
                WorldPos pos = EditTerrain.GetBlockPos(hit);
                MoveCameraToPoint(new Vector3(pos.x, pos.y, pos.z));
            }
        }

		if (!inInputField)
		{
			// Zoom in and out
			transform.position += transform.forward * Input.GetAxis ("Vertical"); // ZOOM IN
			// Orbit the origin
			transform.position += transform.up * Input.GetAxis ("Zoom"); // Strafe up and down
			transform.position += transform.right * Input.GetAxis ("Horizontal"); // Strafe left and right
			transform.LookAt (point);
		}

		// Input Mode
        // Toggle between pen mode and free paint mode
        if (Input.GetKeyDown(KeyCode.Space) && !inInputField)
        {
            ToggleMode();
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
		// [ ] - Might be problems here if you start moving plane, then select an input field
		if (Input.GetKeyDown(KeyCode.Z) && !inInputField)
		{
			Collider[] colChildren = guidePlane.GetComponentsInChildren<Collider>();
			foreach (Collider collider in colChildren) {  
				collider.enabled = false;
			}
		}
		if (Input.GetKey(KeyCode.Z) && !inInputField)
		{
			if (Physics.Raycast (Camera.main.ScreenPointToRay (Input.mousePosition), out cursorHit, 200)) {
				cursorPos = EditTerrain.GetBlockPos(cursorHit, true);
				guidePlane.transform.position = new Vector3(cursorPos.x, cursorPos.y, cursorPos.z);
			}
		}
		if (Input.GetKeyUp(KeyCode.Z) && !inInputField)
		{
			Collider[] colChildren = guidePlane.GetComponentsInChildren<Collider>();
			foreach (Collider collider in colChildren) {  
				collider.enabled = true;
			}
		}
		// Cycle through configurations of guide plane
		if (Input.GetKeyDown(KeyCode.X) && !inInputField)
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

        // Check that we're not clicking on a UI element
        if (!EventSystem.current.IsPointerOverGameObject())
        {

            if (mode == PAINTMODE) // Free Paint Mode
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
            else if (mode == PENMODE) // In Pen Mode
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
								if (isFirstPoint)
								{
									currentShape = new Shape(world, worldState, mode);
									Shape.addVertexWithHit(currentShape, hit, true);
									isFirstPoint = false;
									inspectorModify.shape = currentShape;
								}
								else
									Shape.addVertexWithHit(currentShape, hit, true);

                                // Recalculate the layout for inspector
                                inspectorModify.recalculateInspectorLayout();
                            }
                        }
                    }
                }
                else if (Input.GetMouseButtonDown(1))
                { // Right Click
                  // Change all newly added blocks to the right block type
                    posList = new List<WorldPos>();
                    foreach (List<WorldPos> tempPosList in currentShape.posList)
                    {
                        posList.AddRange(tempPosList);
                    }
					foreach (Plane plane in currentShape.planes)
					{
						posList.AddRange(plane.fillPosList);
						posList.AddRange(plane.loftFillPosList);
					}
                    RaycastHit hit;
                    if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 200))
                    {
                        EditTerrain.SetAllBlocksGivenPos(world, posList, hit, new BlockGrass());
                    }
                    else
                    {
                        EditTerrain.SetAllBlocksGivenPos(world, posList, lastHit, new BlockGrass());
                    }

                    // Store the shape we made in the world state
                    worldState.storeShape(currentShape);

                    // Reset everything
                    // Null out lastHit
                    lastHit = default(RaycastHit);
					currentShape = new Shape(world, worldState, mode);
					inspectorModify.shape = currentShape;

                    // Reset first counters
                    isFirstPoint = true;

					// Destroy all sections in inspector
//					inspectorModify.minimizeAllSections();
					inspectorModify.destroyAllSections();

                    // Remove highlight circle
                    highlightCircleModify.isHighlighted = false;
                }
                else if (Input.GetMouseButtonDown(2))
                {
                    // Middle click
                    // Check if on vertex of current shape, in middle of current shape, or neither
                    RaycastHit hit;
                    // Make raycast not hit UI objects (Guide Plane)
                    LayerMask mask = 1 << LayerMask.NameToLayer("UI");
                    mask = ~mask;
                    if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 200, mask))
                    {
                        // Get the position that we're pointing at
                        WorldPos currentPos = EditTerrain.GetBlockPos(hit);
                        // Check if it's within shape
                        posList = new List<WorldPos>();
                        foreach (List<WorldPos> tempPosList in currentShape.posList)
                        {
                            posList.AddRange(tempPosList);
                        }
                        foreach (Plane plane in currentShape.planes)
                        {
                            posList.AddRange(plane.fillPosList);
                            posList.AddRange(plane.loftFillPosList);
                        }
                        if (posList.Contains(currentPos))
                        {
                            // Check if it's within vertices
                            bool isVertex = false;
                            foreach (List<WorldPos> tempPosList in currentShape.vertices)
                            {
                                List<WorldPos> vertexPosList = new List<WorldPos>(tempPosList);
                                if (vertexPosList.Contains(currentPos))
                                {
                                    isVertex = true;
                                    dragPlaneIndex = currentShape.vertices.IndexOf(tempPosList);
                                    dragPosIndex = vertexPosList.IndexOf(currentPos);
                                    break;
                                }
                            }
                            if (isVertex)
                            {
                                isDraggingVertex = true;
                                inspectorModify.highlightVertex(dragPlaneIndex, dragPosIndex);
                                highlightCircleModify.currentPos = currentPos;
                                highlightCircleModify.isHighlighted = true;
                                lastDragPos = currentPos;
                                // Move guide plane to be centered at new position
                                guidePlane.transform.position = new Vector3(currentPos.x, currentPos.y, currentPos.z);
                                // Get current screen space position of cursor
                                dragMousePos = Input.mousePosition;
                                startedDragging = false;
                            }
                            else
                            {
                                isDraggingShape = true;
                                lastDragPos = currentPos;
                                inspectorModify.turnOffAllHighlights();
                                highlightCircleModify.isHighlighted = false;
                                // Move guide plane to be centered at new position
                                guidePlane.transform.position = new Vector3(currentPos.x, currentPos.y, currentPos.z);
                                dragMousePos = Input.mousePosition;
                                startedDragging = false;
                            }
                            selectToggle.isOn = true;
                        }
                        else
                        {
                        }
                    }
                }
            }
            else if (mode == SELECTMODE)
            {
                // Not in the middle of using pen tool
                if (isFirstPoint == true)
                {
                    if (Input.GetMouseButtonDown(0))
                    {
                        RaycastHit hit;
                        // Make raycast not hit UI objects (Guide Plane)
                        LayerMask mask = 1 << LayerMask.NameToLayer("UI");
                        mask = ~mask;
                        if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 200, mask))
                        {
                            // Get the position that we're pointing at
                            WorldPos currentPos = EditTerrain.GetBlockPos(hit);
                            List<Shape> shapeList = worldState.shapesAtPos(currentPos);
                            if (shapeList.Count > 0)
                            {
                                // Find object that was clicked on in worldState, throw back into edit mode
                                // Need to:
                                // - Set currentShape to the shape
                                // [ ] Currently fairly naive - we don't really have a way to choose which one if two objects are at point
                                // This is rare anyways
                                currentShape = shapeList[0];

                                // Remove shape from worldState - it's in progress again
                                worldState.removeShape(currentShape);

                                // - Set isFirstPoint to false
                                isFirstPoint = false;

                                // - Set the blocks to the right colours
                                posList = new List<WorldPos>();
                                foreach (List<WorldPos> tempPosList in currentShape.posList)
                                {
                                    posList.AddRange(tempPosList);
                                }
                                foreach (Plane plane in currentShape.planes)
                                {
                                    posList.AddRange(plane.fillPosList);
                                    posList.AddRange(plane.loftFillPosList);
                                }
                                EditTerrain.SetAllBlocksGivenPos(world, posList, hit, new BlockTemp(), true, worldState);

                                // -- Set vertices to right colour
                                List<WorldPos> flatVertices = new List<WorldPos>();
                                foreach (List<WorldPos> tempPosList in currentShape.vertices)
                                {
                                    flatVertices.AddRange(tempPosList);
                                }
                                EditTerrain.SetAllBlocksGivenPos(world, flatVertices, hit, new BlockTempVertex(), true, worldState);

                                // Set up inspector
                                inspectorModify.shape = currentShape;
                                inspectorModify.recalculateInspectorLayout();

                                // Throw it into pen mode drag
                                // Check if it's within vertices
                                bool isVertex = false;
                                if (flatVertices.Contains(currentPos))
                                    isVertex = true;
                                if (isVertex)
                                {
                                    isDraggingVertex = true;
                                    inspectorModify.highlightVertex(dragPlaneIndex, dragPosIndex);
                                    highlightCircleModify.currentPos = currentPos;
                                    highlightCircleModify.isHighlighted = true;
                                    lastDragPos = currentPos;
                                    // Move guide plane to be centered at new position
                                    guidePlane.transform.position = new Vector3(currentPos.x, currentPos.y, currentPos.z);
                                    // Get current screen space position of cursor
                                    dragMousePos = Input.mousePosition;
                                    startedDragging = false;
                                }
                                else
                                {
                                    isDraggingShape = true;
                                    lastDragPos = currentPos;
                                    inspectorModify.turnOffAllHighlights();
                                    highlightCircleModify.isHighlighted = false;
                                    // Move guide plane to be centered at new position
                                    guidePlane.transform.position = new Vector3(currentPos.x, currentPos.y, currentPos.z);
                                    dragMousePos = Input.mousePosition;
                                    startedDragging = false;
                                }
                            }
                        }
                    }
                }
                // If we're in the middle of using pen tool
                else if (isFirstPoint == false)
                {
                    if (Input.GetMouseButtonDown(0))
                    {
                        // Middle click
                        // Check if on vertex of current shape, in middle of current shape, or neither
                        RaycastHit hit;
                        // Make raycast not hit UI objects (Guide Plane)
                        LayerMask mask = 1 << LayerMask.NameToLayer("UI");
                        mask = ~mask;
                        if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 200, mask))
                        {
                            // Get the position that we're pointing at
                            WorldPos currentPos = EditTerrain.GetBlockPos(hit);
                            // Check if it's within shape
                            posList = new List<WorldPos>();
                            foreach (List<WorldPos> tempPosList in currentShape.posList)
                            {
                                posList.AddRange(tempPosList);
                            }
                            foreach (Plane plane in currentShape.planes)
                            {
                                posList.AddRange(plane.fillPosList);
                                posList.AddRange(plane.loftFillPosList);
                            }
                            if (posList.Contains(currentPos))
                            {
                                // Check if it's within vertices
                                bool isVertex = false;
                                foreach (List<WorldPos> tempPosList in currentShape.vertices)
                                {
                                    List<WorldPos> vertexPosList = new List<WorldPos>(tempPosList);
                                    if (vertexPosList.Contains(currentPos))
                                    {
                                        isVertex = true;
                                        dragPlaneIndex = currentShape.vertices.IndexOf(tempPosList);
                                        dragPosIndex = vertexPosList.IndexOf(currentPos);
                                        break;
                                    }
                                }
                                if (isVertex)
                                {
                                    isDraggingVertex = true;
                                    inspectorModify.highlightVertex(dragPlaneIndex, dragPosIndex);
                                    highlightCircleModify.currentPos = currentPos;
                                    highlightCircleModify.isHighlighted = true;
                                    lastDragPos = currentPos;
                                    // Move guide plane to be centered at new position
                                    guidePlane.transform.position = new Vector3(currentPos.x, currentPos.y, currentPos.z);
                                    // Get current screen space position of cursor
                                    dragMousePos = Input.mousePosition;
                                    startedDragging = false;
                                }
                                else
                                {
                                    isDraggingShape = true;
                                    lastDragPos = currentPos;
                                    inspectorModify.turnOffAllHighlights();
                                    highlightCircleModify.isHighlighted = false;
                                    // Move guide plane to be centered at new position
                                    guidePlane.transform.position = new Vector3(currentPos.x, currentPos.y, currentPos.z);
                                    dragMousePos = Input.mousePosition;
                                    startedDragging = false;
                                }
                            }
                            else
                            {
                            }
                        }
                    }
                    else if (Input.GetMouseButton(0) || Input.GetMouseButton(2))
                    {
                        if (isDraggingVertex)
                        {
                            RaycastHit hit;
                            // Make raycast only hit UI objects (Guide Plane)
                            LayerMask mask = 1 << LayerMask.NameToLayer("UI");
                            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 200, mask))
                            {
                                // Check if mouse has moved at all - if it hasn't, we don't want to move the object
                                if (startedDragging || (dragMousePos - Input.mousePosition).magnitude > startDragTolerance)
                                {
                                    startedDragging = true;
                                    // Get the position that we're pointing at
                                    WorldPos currentPos = EditTerrain.GetBlockPos(hit);
                                    if (currentPos.x != lastDragPos.x || currentPos.y != lastDragPos.y || currentPos.z != lastDragPos.z)
                                    {
                                        // Add a check for >3 vertices - if so, need to constrain to plane
                                        if (currentShape.planes[dragPlaneIndex].vertexPosList.Count > 3)
                                        {
                                            if (Plane.isCoplanar(currentShape.planes[dragPlaneIndex], WorldPos.VectorFromWorldPos(currentPos)))
                                            {
                                                currentShape.moveVertexFromPosToPos(lastDragPos, currentPos, hit);
                                                lastDragPos = currentPos;
                                                highlightCircleModify.currentPos = currentPos;
                                                // [ ] Could definitely make this more efficient if you had a function just for updating the text
                                                inspectorModify.recalculateInspectorLayout();
                                            }
                                        }
                                        else
                                        {
                                            currentShape.moveVertexFromPosToPos(lastDragPos, currentPos, hit);
                                            lastDragPos = currentPos;
                                            highlightCircleModify.currentPos = currentPos;
                                            inspectorModify.recalculateInspectorLayout();
                                        }
                                    }
                                }
                            }
                        }
                        else if (isDraggingShape)
                        {
                            RaycastHit hit;
                            // Make raycast only hit UI objects (Guide Plane)
                            LayerMask mask = 1 << LayerMask.NameToLayer("UI");
                            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 200, mask))
                            {
                                // Check if mouse has moved at all - if it hasn't, we don't want to move the object
                                if (startedDragging || (dragMousePos - Input.mousePosition).magnitude > startDragTolerance)
                                {
                                    startedDragging = true;
                                    // Get the position that we're pointing at
                                    WorldPos currentPos = EditTerrain.GetBlockPos(hit);
                                    if (currentPos.x != lastDragPos.x || currentPos.y != lastDragPos.y || currentPos.z != lastDragPos.z)
                                    {
                                        currentShape.moveShapeFromPosToPos(lastDragPos, currentPos, hit);
                                        lastDragPos = currentPos;
                                        inspectorModify.recalculateInspectorLayout();
                                    }
                                }
                            }
                        }
                    }
                    else if (Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(2))
                    {
                        if (isDraggingVertex)
                        {
                            RaycastHit hit;
                            // Make raycast only hit UI objects (Guide Plane)
                            LayerMask mask = 1 << LayerMask.NameToLayer("UI");
                            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 200, mask))
                            {
                                // Check if mouse has moved at all - if it hasn't, we don't want to move the object
                                if (startedDragging || (dragMousePos - Input.mousePosition).magnitude > startDragTolerance)
                                {
                                    startedDragging = true;
                                    // Get the position that we're pointing at
                                    WorldPos currentPos = EditTerrain.GetBlockPos(hit);
                                    if (currentPos.x != lastDragPos.x || currentPos.y != lastDragPos.y || currentPos.z != lastDragPos.z)
                                    {
                                        // Add a check for >3 vertices - if so, need to constrain to plane
                                        if (currentShape.planes[dragPlaneIndex].vertexPosList.Count > 3)
                                        {
                                            if (Plane.isCoplanar(currentShape.planes[dragPlaneIndex], WorldPos.VectorFromWorldPos(currentPos)))
                                            {
                                                currentShape.moveVertexFromPosToPos(lastDragPos, currentPos, hit);
                                                lastDragPos = currentPos;
                                                highlightCircleModify.currentPos = currentPos;
                                                inspectorModify.recalculateInspectorLayout();
                                            }
                                        }
                                        else
                                        {
                                            currentShape.moveVertexFromPosToPos(lastDragPos, currentPos, hit);
                                            lastDragPos = currentPos;
                                            highlightCircleModify.currentPos = currentPos;
                                            inspectorModify.recalculateInspectorLayout();
                                        }
                                    }
                                }
                                isDraggingVertex = false;
                                if (Input.GetMouseButtonUp(2))
                                    penToolToggle.isOn = true;
                            }
                        }
                        else if (isDraggingShape)
                        {
                            RaycastHit hit;
                            // Make raycast only hit UI objects (Guide Plane)
                            LayerMask mask = 1 << LayerMask.NameToLayer("UI");
                            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 200, mask))
                            {
                                // Check if mouse has moved at all - if it hasn't, we don't want to move the object
                                if (startedDragging || (dragMousePos - Input.mousePosition).magnitude > startDragTolerance)
                                {
                                    startedDragging = true;
                                    // Get the position that we're pointing at
                                    WorldPos currentPos = EditTerrain.GetBlockPos(hit);
                                    if (currentPos.x != lastDragPos.x || currentPos.y != lastDragPos.y || currentPos.z != lastDragPos.z)
                                    {
                                        currentShape.moveShapeFromPosToPos(lastDragPos, currentPos, hit);
                                        lastDragPos = currentPos;
                                        inspectorModify.recalculateInspectorLayout();
                                    }
                                }
                                isDraggingShape = false;
                                if (Input.GetMouseButtonUp(2))
                                    penToolToggle.isOn = true;
                            }
                        }
                    }
                    else if (Input.GetMouseButtonDown(1))
                    {
                        // [ ] Not sure about this behaviour - need to think it through more
                        // Change all newly added blocks to the right block type
                        posList = new List<WorldPos>();
                        foreach (List<WorldPos> tempPosList in currentShape.posList)
                        {
                            posList.AddRange(tempPosList);
                        }
                        foreach (Plane plane in currentShape.planes)
                        {
                            posList.AddRange(plane.fillPosList);
                            posList.AddRange(plane.loftFillPosList);
                        }
                        RaycastHit hit;
                        if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 200))
                        {
                            EditTerrain.SetAllBlocksGivenPos(world, posList, hit, new BlockGrass());
                        }
                        else
                        {
                            EditTerrain.SetAllBlocksGivenPos(world, posList, lastHit, new BlockGrass());
                        }

                        // Store the shape we made in the world state
                        worldState.storeShape(currentShape);

                        // Reset everything
                        // Null out lastHit
                        lastHit = default(RaycastHit);
                        currentShape = new Shape(world, worldState, mode);
                        inspectorModify.shape = currentShape;

                        // Reset first counters
                        isFirstPoint = true;

                        // Destroy all sections in inspector
                        //					inspectorModify.minimizeAllSections();
                        inspectorModify.destroyAllSections();

                        // Remove highlight circle
                        highlightCircleModify.isHighlighted = false;
                    }
                }
            }
        }
        else // Over a UI object - just resolve existing
        {
            if (mode == PAINTMODE)
            {
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
            else if (mode == PENMODE)
            {
                if (Input.GetMouseButtonUp(2))
                {
                    if (isDraggingVertex)
                    {
                        RaycastHit hit;
                        if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 200))
                        {
                            // Get the position that we're pointing at
                            WorldPos currentPos = EditTerrain.GetBlockPos(hit);
                            if (currentPos.x != lastDragPos.x || currentPos.y != lastDragPos.y || currentPos.z != lastDragPos.z)
                            {
                                // Add a check for >3 vertices - if so, need to constrain to plane
                                if (currentShape.planes[dragPlaneIndex].vertexPosList.Count > 3)
                                {
                                    if (Plane.isCoplanar(currentShape.planes[dragPlaneIndex], WorldPos.VectorFromWorldPos(currentPos)))
                                    {
                                        currentShape.moveVertexFromPosToPos(lastDragPos, currentPos, hit);
                                        lastDragPos = currentPos;
                                        highlightCircleModify.currentPos = currentPos;
                                        inspectorModify.recalculateInspectorLayout();
                                    }
                                }
                                else
                                {
                                    currentShape.moveVertexFromPosToPos(lastDragPos, currentPos, hit);
                                    lastDragPos = currentPos;
                                    highlightCircleModify.currentPos = currentPos;
                                    inspectorModify.recalculateInspectorLayout();
                                }
                            }
                            isDraggingVertex = false;
                        }
                    }
                }
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

    void ToggleMode()
    {
        // Here we have to navigate through the state machine - space bar just cycles through it
        bool penToolInProgress = !isFirstPoint;
        if (mode == PAINTMODE)
            mode++;
        else if (mode == PENMODE)
            mode++;
        else if (mode == SELECTMODE)
        {
            if (penToolInProgress)
                mode = PENMODE;
            else
                mode = PAINTMODE;
        }

        Renderer renderer = cursorCube.GetComponent<Renderer>();
        if (mode == PAINTMODE)
        {
            renderer.enabled = true;
            renderer.material.SetColor("_EmissionColor", new Color(0.1F, 0.643F, 0.1F));
            freePaintToggle.isOn = true;
        }
        else if (mode == PENMODE)
        {
            // Set cursor colour
            renderer.enabled = true;
            renderer.material.SetColor("_EmissionColor", new Color(0.1F, 0.1F, 0.643F));
            // Set pen tool button to pressed state
            penToolToggle.isOn = true;
        }
        else if (mode == SELECTMODE)
        {
            renderer.enabled = false;
            selectToggle.isOn = true;
        }
        else if (mode == INTERACTMODE)
        {

        }
    }

    public void SetMode(int newMode)
    {
        // Here we explicitly set one mode
        // [ ] Do we have to do checks for possible modes here?
        mode = newMode;

        Renderer renderer = cursorCube.GetComponent<Renderer>();
        if (mode == PAINTMODE)
        {
            renderer.enabled = true;
            renderer.material.SetColor("_EmissionColor", new Color(0.1F, 0.643F, 0.1F));
            freePaintToggle.isOn = true;
        }
        else if (mode == PENMODE)
        {
            // Set cursor colour
            renderer.enabled = true;
            renderer.material.SetColor("_EmissionColor", new Color(0.1F, 0.1F, 0.643F));
            // Set pen tool button to pressed state
            penToolToggle.isOn = true;
        }
        else if (mode == SELECTMODE)
        {
            renderer.enabled = false;
            selectToggle.isOn = true;
        }
        else if (mode == INTERACTMODE)
        {

        }
    }

    public void setPaintMode(bool paintModeOn)
    {
        if (paintModeOn)
            SetMode(PAINTMODE);
    }
    public void SetPenMode(bool penModeOn)
    {
        if (penModeOn)
            SetMode(PENMODE);
    }
    public void setSelectMode(bool selectModeOn)
    {
        if (selectModeOn)
            SetMode(SELECTMODE);
    }
    public void setInteractMode(bool interactModeOn)
    {
        if (interactModeOn)
            SetMode(INTERACTMODE);
    }
}