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

	// Counters
	private int numVerticesOnCurrentPlane = 0;
	private bool isFirstPoint = true;
	private bool firstPlaneSet = false;

	public Shape(World newWorld, int newMode) 
	{
		world = newWorld;
        mode = newMode;
    }
	
	public static void addVertexWithHit(Shape shape, RaycastHit newHit)
	{
		shape.hit = newHit;
		recalculateShape(shape);
	}

	// Call this function when a new vertex added to shape or existing vertex changed - recalculates points in shape
	private static void recalculateShape(Shape shape)
	{
		// Instead of just setting block at current position, we want to set all blocks 
		//  between as well - so we interpolate between last position and current
		//  and set all blocks we intersected
		List<WorldPos> placedEdgePosList = EditTerrain.SetAllBlocksBetween(shape.lastHit, shape.hit, shape.world, new BlockTemp(), true);
		//shape.posList.AddRange(placedEdgePosList);
		
		// Add to edgeList and vertexPosList
		// If it's the first point being placed, it's going to try to set an edge from null to this point - don't let it
		if (shape.isFirstPoint)
        {
            shape.isFirstPoint = false;
            shape.posList.Add(new List<WorldPos>());
            shape.posList[shape.posList.Count-1].AddRange(placedEdgePosList);
            shape.vertices.Add(new List<WorldPos>());
			shape.planes.Add(new Plane());
			shape.currentPlane = shape.planes[shape.planes.Count - 1];
        }
        else
        {
            shape.currentPlane.edgeList.Add(placedEdgePosList);
            shape.posList[shape.posList.Count - 1].AddRange(placedEdgePosList);
        }

        shape.currentPlane.vertexPosList.Add(placedEdgePosList[placedEdgePosList.Count - 1]);
		shape.vertices[shape.vertices.Count - 1].Add(placedEdgePosList[placedEdgePosList.Count - 1]);
		//Debug.Log("z being placed: " + placedEdgePosList[placedEdgePosList.Count - 1].z);
		shape.lastHit = shape.hit;
		
		shape.numVerticesOnCurrentPlane++;
		if (shape.numVerticesOnCurrentPlane == 1)
			shape.firstHitOnPlane = shape.hit;
		
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
			List<WorldPos> placedPosList = EditTerrain.SetAllBlocksBetween(shape.hit, shape.firstHitOnPlane, shape.world, new BlockTemp(), true);
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
				EditTerrain.SetAllBlocksGivenPos(shape.world, shape.currentPlane.fillPosList, shape.hit, new BlockAir());
				shape.currentPlane.fillPosList = new List<WorldPos>();
				
				// Remove the previously inferred edge
				shape.currentPlane.edgeList.RemoveAt(shape.currentPlane.edgeList.Count - 2); // -2 because that's position of end-beginning edge from last time
				
				// Add edge
				List<WorldPos> placedPosList = EditTerrain.SetAllBlocksBetween(shape.hit, shape.firstHitOnPlane, shape.world, new BlockTemp(), true);
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
				WorldPos tempPos = shape.currentPlane.vertexPosList[shape.currentPlane.vertexPosList.Count - 1];
                // Remove edge between planes
                List<WorldPos> tempEdgeList = shape.currentPlane.edgeList[shape.currentPlane.edgeList.Count - 1];
                tempEdgeList.RemoveAt(0);
                EditTerrain.SetAllBlocksGivenPos(shape.world, tempEdgeList, shape.hit, new BlockAir());
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
			EditTerrain.SetAllBlocksGivenPos(shape.world, shape.currentPlane.loftFillPosList, shape.hit, new BlockAir());
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
        EditTerrain.SetAllBlocksGivenPos(world, posList[planeIndex].Concat(planes[planeIndex].fillPosList).ToList(), hit, new BlockAir());

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
            EditTerrain.SetAllBlocksGivenPos(world, planes[planeIndex].loftFillPosList, hit, new BlockAir());
            // Set new lofting plane
            planes[planeIndex].loftFillPosList = EditTerrain.LoftAndFillPlanes(planes[planeIndex - 1].vertexPosList, planes[planeIndex - 1].edgeList, planes[planeIndex].vertexPosList, planes[planeIndex].edgeList, hit, world, new BlockTemp());
        }
        if (planeIndex < vertices.Count - 1)
        {
            // Have to loft between this and next plane as well
            // Erase previous lofting plane
            EditTerrain.SetAllBlocksGivenPos(world, planes[planeIndex + 1].loftFillPosList, hit, new BlockAir());
            // Set new lofting plane
            planes[planeIndex+1].loftFillPosList = EditTerrain.LoftAndFillPlanes(planes[planeIndex].vertexPosList, planes[planeIndex].edgeList, planes[planeIndex + 1].vertexPosList, planes[planeIndex + 1].edgeList, hit, world, new BlockTemp());
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
}
