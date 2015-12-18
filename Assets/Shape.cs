using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Shape 
{
	// The idea is to have a data structure where you can basically just input a new vertex, and 
	//  receive a list of points that have been set - the setting of blocks will happen in here

	// Variables
	public World world;
    public WorldState worldState;
    public int mode;

	// All vertices, organized by plane
	public List<List<WorldPos>> vertices = new List<List<WorldPos>>();

	// List of positions that don't change after every iteration
	public List<List<WorldPos>> posList = new List<List<WorldPos>>();

	// List of planes
	public List<Plane> planes = new List<Plane>();
	public Plane currentPlane = new Plane();

	// RaycastHits
	private RaycastHit hit = default(RaycastHit);
	private RaycastHit lastHit = default(RaycastHit);
	private RaycastHit firstHitOnPlane = default(RaycastHit);

    // Positions
    private WorldPos position = new WorldPos();
    private WorldPos lastPosition = new WorldPos();
    private WorldPos firstPositionOnPlane = new WorldPos();

    // Counters
    private int numVerticesOnCurrentPlane = 0;
	private bool isFirstPoint = true;
	private bool firstPlaneSet = false;

	public Shape(World newWorld, WorldState newWorldState, int newMode) 
	{
		world = newWorld;
        worldState = newWorldState;
        mode = newMode;
    }
	
	public static void addVertexWithHit(Shape shape, RaycastHit newHit, bool adjacent)
	{
		shape.hit = newHit;
        shape.position = EditTerrain.GetBlockPos(newHit, adjacent);
		recalculateShape(shape);
	}

	// Call this function when a new vertex added to shape or existing vertex changed - recalculates points in shape
	private static void recalculateShape(Shape shape)
	{
        // Instead of just setting block at current position, we want to set all blocks 
        //  between as well - so we interpolate between last position and current
        //  and set all blocks we intersected
        //List<WorldPos> placedEdgePosList = EditTerrain.SetAllBlocksBetween(shape.lastHit, shape.hit, shape.world, new BlockTemp(), true);
        List<WorldPos> placedEdgePosList;
		//shape.posList.AddRange(placedEdgePosList);
		
		// Add to edgeList and vertexPosList
		// If it's the first point being placed, it's going to try to set an edge from null to this point - don't let it
		if (shape.isFirstPoint)
        {
            placedEdgePosList = EditTerrain.SetAllBlocksBetweenPos(shape.position, shape.position, shape.world, shape.hit, new BlockTemp());
            shape.isFirstPoint = false;
            shape.posList.Add(new List<WorldPos>());
            shape.posList[shape.posList.Count-1].AddRange(placedEdgePosList);
            shape.vertices.Add(new List<WorldPos>());
			shape.planes.Add(new Plane());
			shape.currentPlane = shape.planes[shape.planes.Count - 1];
        }
        else
        {
            placedEdgePosList = EditTerrain.SetAllBlocksBetweenPos(shape.lastPosition, shape.position, shape.world, shape.hit, new BlockTemp());
            shape.currentPlane.edgeList.Add(placedEdgePosList);
            shape.posList[shape.posList.Count - 1].AddRange(placedEdgePosList);
        }

        shape.currentPlane.vertexPosList.Add(placedEdgePosList[placedEdgePosList.Count - 1]);
        //Debug.Log(shape.currentPlane.vertexPosList.Last<WorldPos>().x + "," + shape.currentPlane.vertexPosList.Last<WorldPos>().y + "," + shape.currentPlane.vertexPosList.Last<WorldPos>().z);
		shape.vertices[shape.vertices.Count - 1].Add(placedEdgePosList[placedEdgePosList.Count - 1]);
		//Debug.Log("z being placed: " + placedEdgePosList[placedEdgePosList.Count - 1].z);
		shape.lastHit = shape.hit;
        shape.lastPosition = shape.position;

		shape.numVerticesOnCurrentPlane++;
		if (shape.numVerticesOnCurrentPlane == 1)
        {
            shape.firstHitOnPlane = shape.hit;
            shape.firstPositionOnPlane = shape.position;
        }

        // If 2 points so far
        // Draw edge back as well, so that we don't break loft algorithm
        if (shape.numVerticesOnCurrentPlane == 2)
		{
			// Technically we've already drawn this exact edge, so we'll just add to the
			//  edgeList. We also add to the fillPosList because this is an inferred edge
			shape.currentPlane.edgeList.Add(shape.currentPlane.edgeList[shape.currentPlane.edgeList.Count - 1]);
			shape.currentPlane.fillPosList.AddRange(shape.currentPlane.edgeList[shape.currentPlane.edgeList.Count - 1]);
		}
		// Drawing bounding planes
		// If 3 points placed so far
		else if (shape.numVerticesOnCurrentPlane == 3)
		{
			shape.currentPlane.calculatePlaneVariables();

			// Remove previously inferred edge
			shape.currentPlane.edgeList.RemoveAt(shape.currentPlane.edgeList.Count - 2); // -2 because that's position of end-beginning edge from last time

            // Add new edge
            // [ ] - Slight problem here - erases first block - add an optional variable to function
            //List<WorldPos> placedPosList = EditTerrain.SetAllBlocksBetween(shape.hit, shape.firstHitOnPlane, shape.world, new BlockTemp(), true);
            List<WorldPos> placedPosList = EditTerrain.SetAllBlocksBetweenPos(shape.position, shape.firstPositionOnPlane, shape.world, shape.hit, new BlockTemp());
			shape.currentPlane.fillPosList.AddRange(placedPosList);
			shape.currentPlane.edgeList.Add(placedPosList);
			
			// Set blocks in the planar polygon
			placedPosList = EditTerrain.SetAllBlocksInPlane(shape.world, shape.posList[shape.posList.Count-1].Concat(shape.currentPlane.fillPosList).ToList(), shape.currentPlane.vertexPosList, shape.currentPlane.edgeList, shape.currentPlane, shape.hit, new BlockTemp());
			shape.currentPlane.fillPosList.AddRange(placedPosList);
		}
		// Else if >3 points placed so far, first have to check if new point is coplanar
		//  If point is coplanar, then have to refill plane
		//  If points isn't coplanar, then reset counter to 1
		else if (shape.numVerticesOnCurrentPlane > 3)
		{
			// Check if point is coplanar
			Vector3 currentPoint = WorldPos.VectorFromWorldPos(shape.currentPlane.vertexPosList[shape.currentPlane.vertexPosList.Count - 1]);
			if (Plane.isCoplanar(shape.currentPlane, currentPoint))
			{
				// Erase current fill
				EditTerrain.SetAllBlocksGivenPos(shape.world, shape.currentPlane.fillPosList, shape.hit, new BlockAir(), true, shape.worldState);
				shape.currentPlane.fillPosList = new List<WorldPos>();
				
				// Remove the previously inferred edge
				shape.currentPlane.edgeList.RemoveAt(shape.currentPlane.edgeList.Count - 2); // -2 because that's position of end-beginning edge from last time

                // Add edge
                //List<WorldPos> placedPosList = EditTerrain.SetAllBlocksBetween(shape.hit, shape.firstHitOnPlane, shape.world, new BlockTemp(), true);
                List<WorldPos> placedPosList = EditTerrain.SetAllBlocksBetweenPos(shape.position, shape.firstPositionOnPlane, shape.world, shape.hit, new BlockTemp());
				shape.currentPlane.fillPosList.AddRange(placedPosList);
				shape.currentPlane.edgeList.Add(placedPosList);
				
				// Fill in the plane again
				placedPosList = EditTerrain.SetAllBlocksInPlane(shape.world, shape.posList[shape.posList.Count - 1].Concat(shape.currentPlane.fillPosList).ToList(), shape.currentPlane.vertexPosList, shape.currentPlane.edgeList, shape.currentPlane, shape.hit, new BlockTemp());
				shape.currentPlane.fillPosList.AddRange(placedPosList);
			}
			else
			{
				// The first plane has been set
				shape.firstPlaneSet = true;
				
				//foreach (List<WorldPos> edge in previousEdgeList)
				//{
				//    Debug.Log("previousEdgeList first: " + edge[0].x + "," + edge[0].y + "," + edge[0].z + " count: " + edge.Count + " / last: " + edge[edge.Count - 1].x + "," + edge[edge.Count - 1].y + "," + edge[edge.Count - 1].z);
				//}
				
				// Reset plane variables
				shape.numVerticesOnCurrentPlane = 1;
				shape.firstHitOnPlane = shape.hit;
                shape.firstPositionOnPlane = shape.position;
				WorldPos tempPos = shape.currentPlane.vertexPosList[shape.currentPlane.vertexPosList.Count - 1];
                // Remove edge between planes
                List<WorldPos> tempEdgeList = shape.currentPlane.edgeList[shape.currentPlane.edgeList.Count - 1];
                tempEdgeList.RemoveAt(0);
                EditTerrain.SetAllBlocksGivenPos(shape.world, tempEdgeList, shape.hit, new BlockAir(), true, shape.worldState);
                foreach (WorldPos p in tempEdgeList)
                {
                    shape.posList[shape.posList.Count - 1].Remove(p);
                }
                // Store previous plane info
                shape.currentPlane.edgeList.RemoveAt(shape.currentPlane.edgeList.Count - 1);
				shape.currentPlane.vertexPosList.RemoveAt(shape.currentPlane.vertexPosList.Count - 1);
				shape.planes.Add(new Plane());
				shape.currentPlane = shape.planes[shape.planes.Count - 1];
				shape.currentPlane.vertexPosList.Add(tempPos);
                shape.vertices[shape.vertices.Count - 1].RemoveAt(shape.vertices[shape.vertices.Count - 1].Count - 1);
                shape.vertices.Add(new List<WorldPos>());
                shape.vertices[shape.vertices.Count - 1].Add(tempPos);
                shape.posList.Add(new List<WorldPos>());
                shape.posList[shape.posList.Count - 1].Add(tempPos);
            }
        }
		//foreach (List<WorldPos> edge in edgeList)
		//{
		//    Debug.Log("edgeList with #vertices= " + numVerticesOnCurrentPlane + " first: " + edge[0].x + "," + edge[0].y + "," + edge[0].z + " count: " + edge.Count + " / last: " + edge[edge.Count - 1].x + "," + edge[edge.Count - 1].y + "," + edge[edge.Count - 1].z);
		//}
		
		// Drawing lofting planes
		if (shape.firstPlaneSet)
		{
			// Erase previous lofting plane
			EditTerrain.SetAllBlocksGivenPos(shape.world, shape.currentPlane.loftFillPosList, shape.hit, new BlockAir(), true, shape.worldState);
			// Set new lofting plane
			shape.currentPlane.loftFillPosList = EditTerrain.LoftAndFillPlanes(shape.planes[shape.planes.Count - 2].vertexPosList, shape.planes[shape.planes.Count - 2].edgeList, shape.planes[shape.planes.Count - 1].vertexPosList, shape.planes[shape.planes.Count - 1].edgeList, shape.hit, shape.world, new BlockTemp());
		}
		
		// Set vertices again so we get different coloured vertex
		foreach (Plane plane in shape.planes)
		{
			foreach (WorldPos pos in plane.vertexPosList)
			{
				EditTerrain.SetAllBlocksBetweenPos(pos, pos, shape.world, shape.hit, new BlockTempVertex());
			}
		}
	}

    public void moveVertexFromPosToPos(WorldPos originalPos, WorldPos newPos, RaycastHit newHit)
    {
        // Alright, what do we have to do
        // First, find out which plane the vertex is on - this is the only plane that changes
        int planeIndex = 0;
        for (int i = 0; i < vertices.Count; i++)
        {
            if (vertices[i].Contains(originalPos))
            {
                planeIndex = i;
                break;
            }
        }
        // Recreate this plane with the new set of vertices - should be fairly straightforward knowing all the vertices
        // Replace old vertex with the new vertex
        int posIndex = vertices[planeIndex].IndexOf(originalPos);
        vertices[planeIndex].RemoveAt(posIndex);
        vertices[planeIndex].Insert(posIndex, newPos);

        if (planeIndex == vertices.Count-1 && posIndex == vertices[planeIndex].Count-1)
        {
            lastHit = newHit;
            if (numVerticesOnCurrentPlane == 1)
                firstHitOnPlane = newHit;
        }
        // vertexPosList
        posIndex = planes[planeIndex].vertexPosList.IndexOf(originalPos);
        planes[planeIndex].vertexPosList.RemoveAt(posIndex);
        planes[planeIndex].vertexPosList.Insert(posIndex, newPos);

        // Erase
        EditTerrain.SetAllBlocksGivenPos(world, posList[planeIndex].Concat(planes[planeIndex].fillPosList).ToList(), hit, new BlockAir(), true, worldState);

        // posList, edgeList and fillPosList
        posList[planeIndex] = new List<WorldPos>();
        posList[planeIndex].Add(newPos);
        planes[planeIndex].edgeList = new List<List<WorldPos>>();
        planes[planeIndex].fillPosList = new List<WorldPos>();
        if (vertices[planeIndex].Count > 1)
        {
            List<WorldPos> placedEdgePosList;
            for (int i = 1; i < planes[planeIndex].vertexPosList.Count; i++)
            {
                placedEdgePosList = EditTerrain.SetAllBlocksBetweenPos(planes[planeIndex].vertexPosList[i - 1], planes[planeIndex].vertexPosList[i], world, hit, new BlockTemp());
                planes[planeIndex].edgeList.Add(placedEdgePosList);
                posList[planeIndex].AddRange(placedEdgePosList);
            }
            placedEdgePosList = EditTerrain.SetAllBlocksBetweenPos(planes[planeIndex].vertexPosList[planes[planeIndex].vertexPosList.Count - 1], planes[planeIndex].vertexPosList[0], world, hit, new BlockTemp());
            planes[planeIndex].edgeList.Add(placedEdgePosList);
            posList[planeIndex].AddRange(placedEdgePosList);
            planes[planeIndex].fillPosList.AddRange(placedEdgePosList);
        }

        if (vertices[planeIndex].Count > 2)
        {
            planes[planeIndex].calculatePlaneVariables();
            // Set blocks in the planar polygon
            List<WorldPos> placedFillPosList = EditTerrain.SetAllBlocksInPlane(world, posList[planeIndex].Concat(planes[planeIndex].fillPosList).ToList(), planes[planeIndex].vertexPosList, planes[planeIndex].edgeList, planes[planeIndex], hit, new BlockTemp());
            planes[planeIndex].fillPosList.AddRange(placedFillPosList);
        }

        // Re-loft all the planes
        if (planeIndex > 0)
        {
            // Erase previous lofting plane
            EditTerrain.SetAllBlocksGivenPos(world, planes[planeIndex].loftFillPosList, hit, new BlockAir(), true, worldState);
            // Set new lofting plane
            planes[planeIndex].loftFillPosList = EditTerrain.LoftAndFillPlanes(planes[planeIndex - 1].vertexPosList, planes[planeIndex - 1].edgeList, planes[planeIndex].vertexPosList, planes[planeIndex].edgeList, hit, world, new BlockTemp());
        }
        if (planeIndex < vertices.Count - 1)
        {
            // Have to loft between this and next plane as well
            // Erase previous lofting plane
            EditTerrain.SetAllBlocksGivenPos(world, planes[planeIndex + 1].loftFillPosList, hit, new BlockAir(), true, worldState);
            // Set new lofting plane
            planes[planeIndex+1].loftFillPosList = EditTerrain.LoftAndFillPlanes(planes[planeIndex].vertexPosList, planes[planeIndex].edgeList, planes[planeIndex + 1].vertexPosList, planes[planeIndex + 1].edgeList, hit, world, new BlockTemp());
        }

        // Fill in the planes and edges again just in case they got deleted - just need to set the blocks
        for (int j = 0; j < vertices.Count; j++)
        {
            if (vertices[j].Count > 1)
            {
                for (int i = 1; i < planes[j].vertexPosList.Count; i++)
                {
                    EditTerrain.SetAllBlocksBetweenPos(planes[j].vertexPosList[i - 1], planes[j].vertexPosList[i], world, hit, new BlockTemp());
                }
                EditTerrain.SetAllBlocksBetweenPos(planes[j].vertexPosList[planes[j].vertexPosList.Count - 1], planes[j].vertexPosList[0], world, hit, new BlockTemp());
            }

            if (vertices[j].Count > 2)
            {
                // Set blocks in the planar polygon
                EditTerrain.SetAllBlocksInPlane(world, posList[j].Concat(planes[j].fillPosList).ToList(), planes[j].vertexPosList, planes[j].edgeList, planes[j], hit, new BlockTemp());
            }
        }

        // Set vertices again so we get different coloured vertices
        foreach (Plane plane in planes)
        {
            foreach (WorldPos pos in plane.vertexPosList)
            {
                EditTerrain.SetAllBlocksBetweenPos(pos, pos, world, hit, new BlockTempVertex());
            }
        }
    }
    public void moveShapeFromPosToPos(WorldPos originalPos, WorldPos newPos, RaycastHit newHit)
    {
        // Steps
        // Get delta
        // Shift every point in shape that delta
        Vector3 delta = WorldPos.VectorFromWorldPos(newPos) - WorldPos.VectorFromWorldPos(originalPos);

        // Make a list of all points set in old position, all points set in new position
        List<WorldPos> oldPosList = new List<WorldPos>();
        foreach (List<WorldPos> tempPosList in posList)
        {
            oldPosList.AddRange(tempPosList);
        }
        foreach (Plane plane in planes)
        {
            oldPosList.AddRange(plane.fillPosList);
            oldPosList.AddRange(plane.loftFillPosList);
        }
        List<WorldPos> newPosList = new List<WorldPos>();
        for (int i = 0; i < oldPosList.Count; i++)
        {
            Vector3 newVectorPos = WorldPos.VectorFromWorldPos(oldPosList[i]) + delta;
            newPosList.Add(new WorldPos(Mathf.RoundToInt(newVectorPos.x), Mathf.RoundToInt(newVectorPos.y), Mathf.RoundToInt(newVectorPos.z)));
        }

        // First set all old vertices to blocktemp
        List<WorldPos> flatVertices = new List<WorldPos>();
        foreach (List<WorldPos> posList in vertices)
        {
            flatVertices.AddRange(posList);
        }
        EditTerrain.SetAllBlocksGivenPos(world, flatVertices, hit, new BlockTemp(), true, worldState);

        // Find unique points - set them to blocktemp and blockair respectively
        //List<WorldPos> intersection = newPosList.Intersect<WorldPos>(oldPosList).ToList();
        //List<WorldPos> uniqueNewPosList = newPosList.Where(p => !oldPosList.Any(p2 => p2.Equals(p))).ToList();
        //List<WorldPos> uniqueOldPosList = oldPosList.Where(p => !newPosList.Any(p2 => p2.Equals(p))).ToList();

        //EditTerrain.SetAllBlocksGivenPos(world, uniqueNewPosList, hit, new BlockTemp());
        //EditTerrain.SetAllBlocksGivenPos(world, uniqueOldPosList, hit, new BlockAir());
        EditTerrain.SetAllBlocksGivenPos(world, oldPosList, hit, new BlockAir(), true, worldState);
        EditTerrain.SetAllBlocksGivenPos(world, newPosList, hit, new BlockTemp(), true, worldState);

        // Have to shift: vertices, posList, position, lastPosition, firstPositionOnPlane
        //  in Planes: recalculate plane variables, vertexPosList, edgeList, fillPosList, loftFillPosList
        for (int i = 0; i < vertices.Count; i++)
        {
            for (int j = 0; j < vertices[i].Count; j++)
            {
                Vector3 newVectorPos = WorldPos.VectorFromWorldPos(vertices[i][j]) + delta;
                vertices[i][j] = new WorldPos(Mathf.RoundToInt(newVectorPos.x), Mathf.RoundToInt(newVectorPos.y), Mathf.RoundToInt(newVectorPos.z));
            }
        }
        for (int i = 0; i < posList.Count; i++)
        {
            for (int j = 0; j < posList[i].Count; j++)
            {
                Vector3 newVectorPos = WorldPos.VectorFromWorldPos(posList[i][j]) + delta;
                posList[i][j] = new WorldPos(Mathf.RoundToInt(newVectorPos.x), Mathf.RoundToInt(newVectorPos.y), Mathf.RoundToInt(newVectorPos.z));
            }
        }
        position = position.Add(WorldPos.WorldPosFromVector(delta));
        lastPosition = lastPosition.Add(WorldPos.WorldPosFromVector(delta));
        firstPositionOnPlane = firstPositionOnPlane.Add(WorldPos.WorldPosFromVector(delta));

        foreach (Plane plane in planes)
        {
            for (int i = 0; i < plane.vertexPosList.Count; i++)
            {
                plane.vertexPosList[i] = plane.vertexPosList[i].Add(WorldPos.WorldPosFromVector(delta));
            }
            for (int i = 0; i < plane.edgeList.Count; i++)
            {
                for (int j = 0; j < plane.edgeList[i].Count; j++)
                {
                    plane.edgeList[i][j] = plane.edgeList[i][j].Add(WorldPos.WorldPosFromVector(delta));
                }
            }
            for (int i = 0; i < plane.fillPosList.Count; i++)
            {
                plane.fillPosList[i] = plane.fillPosList[i].Add(WorldPos.WorldPosFromVector(delta));
            }
            for (int i = 0; i < plane.loftFillPosList.Count; i++)
            {
                plane.loftFillPosList[i] = plane.loftFillPosList[i].Add(WorldPos.WorldPosFromVector(delta));
            }
            if (plane.vertexPosList.Count >= 3)
                plane.calculatePlaneVariables();
        }

        // Do a final pass to set new vertices yellow
        flatVertices = new List<WorldPos>();
        foreach (List<WorldPos> posList in vertices)
        {
            flatVertices.AddRange(posList);
        }
        EditTerrain.SetAllBlocksGivenPos(world, flatVertices, hit, new BlockTempVertex(), true, worldState);
    }
}
