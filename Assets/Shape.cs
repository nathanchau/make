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
	public bool inPenMode = false; // Two modes for drawing - pen mode, and free paint mode

	// All vertices, organized by plane
	public List<List<WorldPos>> vertices = new List<List<WorldPos>>();

	// List of positions that don't change after every iteration
	public List<WorldPos> posList = new List<WorldPos>();

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

	public Shape(World newWorld, bool newInPenMode) 
	{
		world = newWorld;
		inPenMode = newInPenMode;
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
		shape.posList.AddRange(placedEdgePosList);
		
		// Add to edgeList and vertexPosList
		// If it's the first point being placed, it's going to try to set an edge from null to this point - don't let it
		if (shape.isFirstPoint)
        {
            shape.isFirstPoint = false;
            shape.vertices.Add(new List<WorldPos>());
			shape.planes.Add(new Plane());
			shape.currentPlane = shape.planes[shape.planes.Count - 1];
        }
        else
			shape.currentPlane.edgeList.Add(placedEdgePosList);
		
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
			placedPosList = EditTerrain.SetAllBlocksInPlane(shape.world, shape.posList.Concat(shape.currentPlane.fillPosList).ToList(), shape.currentPlane.vertexPosList, shape.currentPlane.edgeList, shape.currentPlane, shape.hit, new BlockTemp());
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
				placedPosList = EditTerrain.SetAllBlocksInPlane(shape.world, shape.posList.Concat(shape.currentPlane.fillPosList).ToList(), shape.currentPlane.vertexPosList, shape.currentPlane.edgeList, shape.currentPlane, shape.hit, new BlockTemp());
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
				// Store previous plane info
				shape.currentPlane.edgeList.RemoveAt(shape.currentPlane.edgeList.Count - 1);
				shape.currentPlane.vertexPosList.RemoveAt(shape.currentPlane.vertexPosList.Count - 1);
				shape.planes.Add(new Plane());
				shape.currentPlane = shape.planes[shape.planes.Count - 1];
				shape.currentPlane.vertexPosList.Add(tempPos);
                shape.vertices[shape.vertices.Count - 1].RemoveAt(shape.vertices[shape.vertices.Count - 1].Count - 1);
                shape.vertices.Add(new List<WorldPos>());
                shape.vertices[shape.vertices.Count - 1].Add(tempPos);
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
}
