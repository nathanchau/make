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
	bool isFirstPoint = true;
	public Shape currentShape;

	List<WorldPos> posList = new List<WorldPos>();

    void Start()
	{
		point = new Vector3 (0.0f, 0.0f);
		transform.LookAt (point);
		currentShape = new Shape(world, mode);
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
        if (Input.GetMouseButtonDown(2))
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
									currentShape = new Shape(world, mode);
									Shape.addVertexWithHit(currentShape, hit);
									isFirstPoint = false;
									inspectorModify.shape = currentShape;
								}
								else
									Shape.addVertexWithHit(currentShape, hit);

                                // Recalculate the layout for inspector
                                inspectorModify.recalculateInspectorLayout();
                            }
                        }
                    }
                }
                if (Input.GetMouseButtonDown(1))
                { // Right Click
                  // Change all newly added blocks to the right block type
					posList = new List<WorldPos>(currentShape.posList);
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

                    // Reset everything
                    // Null out lastHit
                    lastHit = default(RaycastHit);
					currentShape = new Shape(world, mode);
					inspectorModify.shape = currentShape;

                    // Reset first counters
                    isFirstPoint = true;

					// Destroy all sections in inspector
//					inspectorModify.minimizeAllSections();
					inspectorModify.destroyAllSections();
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