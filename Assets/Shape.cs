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

	// All vertices
	public List<WorldPos> vertices = new List<WorldPos>();

	public List<WorldPos> posList = new List<WorldPos>();
	public List<WorldPos> fillPosList = new List<WorldPos>(); // List of positions that have been added for polygon fill
	// For current plane
	private Plane currentPlane = new Plane();
	private List<List<WorldPos>> edgeList = new List<List<WorldPos>>(); // List of edges used for current polygon
	private List<WorldPos> vertexPosList = new List<WorldPos>(); // List of vertices used for current polygon
	// For previous plane
	private List<List<WorldPos>> previousEdgeList = new List<List<WorldPos>>();
	private List<WorldPos> previousVertexPosList = new List<WorldPos>();
	// For lofting planes
	public List<WorldPos> loftFillPosList = new List<WorldPos>(); // List of positions that have been added for all polygon fills for loft

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
			shape.isFirstPoint = false;
		else
			shape.edgeList.Add(placedEdgePosList);
		
		shape.vertexPosList.Add(placedEdgePosList[placedEdgePosList.Count - 1]);
		shape.vertices.Add(placedEdgePosList[placedEdgePosList.Count - 1]);
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
			shape.edgeList.Add(shape.edgeList[shape.edgeList.Count - 1]);
			shape.fillPosList.AddRange(shape.edgeList[shape.edgeList.Count - 1]);
		}
		// Drawing bounding planes
		// If 3 points placed so far
		else if (shape.numVerticesOnCurrentPlane == 3)
		{
			Vector3 p1 = WorldPos.VectorFromWorldPos(shape.vertexPosList[0]);
			Vector3 p2 = WorldPos.VectorFromWorldPos(shape.vertexPosList[1]);
			Vector3 p3 = WorldPos.VectorFromWorldPos(shape.vertexPosList[2]);
			//Debug.Log(p1.z + "," + p2.z + "," + p3.z);
			shape.currentPlane = Plane.newPlaneWithPoints(p1, p2, p3);
			
			// Remove previously inferred edge
			shape.edgeList.RemoveAt(shape.edgeList.Count - 2); // -2 because that's position of end-beginning edge from last time
			
			// Add new edge
			// [ ] - Slight problem here - erases first block - add an optional variable to function
			List<WorldPos> placedPosList = EditTerrain.SetAllBlocksBetween(shape.hit, shape.firstHitOnPlane, shape.world, new BlockTemp(), true);
			shape.fillPosList.AddRange(placedPosList);
			shape.edgeList.Add(placedPosList);
			
			// Set blocks in the planar polygon
			placedPosList = EditTerrain.SetAllBlocksInPlane(shape.world, shape.posList.Concat(shape.fillPosList).ToList(), shape.vertexPosList, shape.edgeList, shape.currentPlane, shape.hit, new BlockTemp());
			shape.fillPosList.AddRange(placedPosList);
		}
		// Else if >3 points placed so far, first have to check if new point is coplanar
		//  If point is coplanar, then have to refill plane
		//  If points isn't coplanar, then reset counter to 1
		else if (shape.numVerticesOnCurrentPlane > 3)
		{
			// Check if point is coplanar
			Vector3 currentPoint = WorldPos.VectorFromWorldPos(shape.vertexPosList[shape.vertexPosList.Count - 1]);
			if (Plane.isCoplanar(shape.currentPlane, currentPoint))
			{
				// Erase current fill
				EditTerrain.SetAllBlocksGivenPos(shape.world, shape.fillPosList, shape.hit, new BlockAir());
				shape.fillPosList = new List<WorldPos>();
				
				// Remove the previously inferred edge
				shape.edgeList.RemoveAt(shape.edgeList.Count - 2); // -2 because that's position of end-beginning edge from last time
				
				// Add edge
				List<WorldPos> placedPosList = EditTerrain.SetAllBlocksBetween(shape.hit, shape.firstHitOnPlane, shape.world, new BlockTemp(), true);
				shape.fillPosList.AddRange(placedPosList);
				shape.edgeList.Add(placedPosList);
				
				// Fill in the plane again
				placedPosList = EditTerrain.SetAllBlocksInPlane(shape.world, shape.posList.Concat(shape.fillPosList).ToList(), shape.vertexPosList, shape.edgeList, shape.currentPlane, shape.hit, new BlockTemp());
				shape.fillPosList.AddRange(placedPosList);
			}
			else
			{
				// The first plane has been set
				shape.firstPlaneSet = true;
				
				// Store previous plane info
				shape.previousEdgeList = new List<List<WorldPos>>(shape.edgeList);
				shape.previousEdgeList.RemoveAt(shape.previousEdgeList.Count - 1);
				shape.previousVertexPosList = new List<WorldPos>(shape.vertexPosList);
				shape.previousVertexPosList.RemoveAt(shape.previousVertexPosList.Count - 1);
				//foreach (List<WorldPos> edge in previousEdgeList)
				//{
				//    Debug.Log("previousEdgeList first: " + edge[0].x + "," + edge[0].y + "," + edge[0].z + " count: " + edge.Count + " / last: " + edge[edge.Count - 1].x + "," + edge[edge.Count - 1].y + "," + edge[edge.Count - 1].z);
				//}
				
				// Reset plane variables
				shape.currentPlane = new Plane();
				shape.numVerticesOnCurrentPlane = 1;
				shape.firstHitOnPlane = shape.hit;
				WorldPos tempPos = shape.vertexPosList[shape.vertexPosList.Count - 1];
				shape.vertexPosList = new List<WorldPos>();
				shape.vertexPosList.Add(tempPos);
				shape.edgeList = new List<List<WorldPos>>();
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
			EditTerrain.SetAllBlocksGivenPos(shape.world, shape.loftFillPosList, shape.hit, new BlockAir());
			// Set new lofting plane
			shape.loftFillPosList = EditTerrain.LoftAndFillPlanes(shape.previousVertexPosList, shape.previousEdgeList, shape.vertexPosList, shape.edgeList, shape.hit, shape.world, new BlockTemp());
		}
		
		// Set vertices again so we get different coloured vertex
		foreach (WorldPos pos in shape.vertexPosList)
		{
			EditTerrain.SetAllBlocksBetweenPos(pos, pos, shape.world, shape.hit, new BlockTempVertex());
		}
		foreach (WorldPos pos in shape.previousVertexPosList)
		{
			EditTerrain.SetAllBlocksBetweenPos(pos, pos, shape.world, shape.hit, new BlockTempVertex());
		}
	}
}
