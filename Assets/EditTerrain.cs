using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public static class EditTerrain
{
    public static WorldPos GetBlockPos(Vector3 pos)
    {
        WorldPos blockPos = new WorldPos(
            Mathf.RoundToInt(pos.x),
            Mathf.RoundToInt(pos.y),
            Mathf.RoundToInt(pos.z)
            );

        return blockPos;
    }

    public static WorldPos GetBlockPos(RaycastHit hit, bool adjacent = false)
    {
        Vector3 pos = new Vector3(
            MoveWithinBlock(hit.point.x, hit.normal.x, adjacent),
            MoveWithinBlock(hit.point.y, hit.normal.y, adjacent),
            MoveWithinBlock(hit.point.z, hit.normal.z, adjacent)
            );

        return GetBlockPos(pos);
    }

    static float MoveWithinBlock(float pos, float norm, bool adjacent = false)
    {
        if (pos - (int)pos == 0.5f || pos - (int)pos == -0.5f)
        {
            if (adjacent)
            {
                pos += (norm / 2);
            }
            else
            {
                pos -= (norm / 2);
            }
        }

        return (float)pos;
    }

    public static WorldPos SetBlock(RaycastHit hit, Block block, bool adjacent = false)
    {
        Chunk chunk = hit.collider.GetComponent<Chunk>();
        if (chunk == null)
            return new WorldPos();

        WorldPos pos = GetBlockPos(hit, adjacent);

        chunk.world.SetBlock(pos.x, pos.y, pos.z, block);

        return pos;
    }

    public static Block GetBlock(RaycastHit hit, bool adjacent = false)
    {
        Chunk chunk = hit.collider.GetComponent<Chunk>();
        if (chunk == null)
            return null;

        WorldPos pos = GetBlockPos(hit, adjacent);

        Block block = chunk.world.GetBlock(pos.x, pos.y, pos.z);

        return block;
    }

    public static bool IsAdjacentBlockGrass(RaycastHit hit, bool adjacent = false)
    {
        Chunk chunk = hit.collider.GetComponent<Chunk>();
        if (chunk == null)
            return false;

        WorldPos pos = GetBlockPos(hit, adjacent);

        // Check all blocks adjacent - if adjacent, then return true
        if (chunk.world.GetBlock(pos.x, pos.y - 1, pos.z) is BlockGrass)
            return true;
        else if (chunk.world.GetBlock(pos.x + 1, pos.y, pos.z) is BlockGrass)
            return true;
        else if (chunk.world.GetBlock(pos.x - 1, pos.y, pos.z) is BlockGrass)
            return true;
        else if (chunk.world.GetBlock(pos.x, pos.y + 1, pos.z) is BlockGrass)
            return true;
        else if (chunk.world.GetBlock(pos.x, pos.y, pos.z + 1) is BlockGrass)
            return true;
        else if (chunk.world.GetBlock(pos.x, pos.y, pos.z - 1) is BlockGrass)
            return true;

        return false;
    }

    // Sets all blocks between hit1 and hit2, exclusive hit1, inclusive hit2
    public static List<WorldPos> SetAllBlocksBetween(RaycastHit hit1, RaycastHit hit2, Block block, bool adjacent = false)
    {
        // If previous hit was defaulted out, then just setblock
        if (hit1.point == default(RaycastHit).point)
        {
            List<WorldPos> posList1 = new List<WorldPos>();
            WorldPos tempPos1 = SetBlock(hit2, block, adjacent);
            posList1.Add(tempPos1);
            return posList1;
        }

        // 3D extension of Breseham's Line Drawing Algorithm
        //  adapted to use floating point numbers instead of error values

        // Note: Very unlikely that you somehow traverse an entire chunk between update calls, but if it somehow happens,
        //  it's not handled - ie. if there's an unset chunk halfway between two hits, and you call this function
        //  then you'll try to set a block on an empty chunk - nothing bad will happen I don't think, it just won't work
        Chunk chunk = hit2.collider.GetComponent<Chunk>();
        if (chunk == null)
            return new List<WorldPos>();

        // Get endpoints
        WorldPos pos1 = GetBlockPos(hit1, adjacent);
        WorldPos pos2 = GetBlockPos(hit2, adjacent);
        Vector3 p1 = new Vector3(pos1.x, pos1.y, pos1.z);
        Vector3 p2 = new Vector3(pos2.x, pos2.y, pos2.z);

        // Set up variables - mostly calculating the step, which is number of pixels you're going to display
        Vector3 tempPos = p1;
        Vector3 difference = p2 - p1;
        float N = Mathf.Max(Mathf.Abs(p2.x - p1.x), Mathf.Abs(p2.y - p1.y), Mathf.Abs(p2.z - p1.z));
        Vector3 step = difference / N;
        // List of all positions used
        List<WorldPos> posList = new List<WorldPos>();

        // Makes sense really - you know how many points you have to display, each point is a step along
        //  the axis of longest distance, so you just do it with floats, round it
        for (int i = 0; i < N; i++)
        {
            tempPos = tempPos + step;
            posList.Add(new WorldPos((int)tempPos.x, (int)tempPos.y, (int)tempPos.z));

            // Note: Have to check if replacing blockgrass or block air in this function - kind of inconsistent
            Block tempBlock = chunk.world.GetBlock((int)tempPos.x, (int)tempPos.y, (int)tempPos.z);
            if (tempBlock is BlockAir)
                chunk.world.SetBlock((int)tempPos.x, (int)tempPos.y, (int)tempPos.z, block);
        }

        // Note: this might not be ideal for "shading in" parts of solids, because it doesn't fill every point it intersects, 
        //  but rather a single line between the two points

        return posList;
    }

	// Same as above, just using worldpos varibles for endpoints instead
	public static List<WorldPos> SetAllBlocksBetweenPos(WorldPos pos1, WorldPos pos2, RaycastHit hit, Block block)
	{
		Chunk chunk = hit.collider.GetComponent<Chunk>();
		if (chunk == null)
			return new List<WorldPos>();
		
		Vector3 p1 = WorldPos.VectorFromWorldPos(pos1);
		Vector3 p2 = WorldPos.VectorFromWorldPos(pos2);
		// Set up variables - mostly calculating the step, which is number of pixels you're going to display
		Vector3 tempPos = p1;
		Vector3 difference = p2 - p1;
		float N = Mathf.Max(Mathf.Abs(p2.x - p1.x), Mathf.Abs(p2.y - p1.y), Mathf.Abs(p2.z - p1.z));
		Vector3 step = difference / N;
		// List of all positions used
		List<WorldPos> posList = new List<WorldPos>();
		
		// Makes sense really - you know how many points you have to display, each point is a step along
		//  the axis of longest distance, so you just do it with floats, round it
		for (int i = 0; i < N; i++)
		{
			tempPos = tempPos + step;
			posList.Add(new WorldPos((int)tempPos.x, (int)tempPos.y, (int)tempPos.z));
			
			// Note: Have to check if replacing blockgrass or block air in this function - kind of inconsistent
			Block tempBlock = chunk.world.GetBlock((int)tempPos.x, (int)tempPos.y, (int)tempPos.z);
			if (tempBlock is BlockAir)
				chunk.world.SetBlock((int)tempPos.x, (int)tempPos.y, (int)tempPos.z, block);
		}
		
		// Note: this might not be ideal for "shading in" parts of solids, because it doesn't fill every point it intersects, 
		//  but rather a single line between the two points
		
		return posList;
	}
	
	public static bool SetAllBlocksGivenPos(List<WorldPos> posList, RaycastHit lastHit, Block block)
    {
        Chunk chunk = lastHit.collider.GetComponent<Chunk>();
        if (chunk == null)
            return false;

        foreach (WorldPos pos in posList)
        {
            chunk.world.SetBlock(pos.x, pos.y, pos.z, block);
        }
        return true;
    }

	public static List<WorldPos> SetAllBlocksInPlane(List<WorldPos> posList, List<List<WorldPos>> edgeList, Plane plane, RaycastHit lastHit, Block block)
	{
		Chunk chunk = lastHit.collider.GetComponent<Chunk>();
		if (chunk == null)
			return new List<WorldPos>();

		List<WorldPos> filledPosList = new List<WorldPos>();

		// Using equation z = -A/C x - B/C y - D/C where C(z) has largest coefficient
		// [ ] - How do you make sure it generalizes like this?
		float A = plane.normal.x;
		float B = plane.normal.y;
		float C = plane.normal.z;
		float D = plane.offset;
		//Debug.Log("A: " + A + ", B: " + B + ", C: " + C + ", D: " + D);

		// Pretty easy actually - in theory, all you do is the regular scan line algorithm over two coordinates
		//  Then, over last coordinate, which ideally you have the least variation over, you evaluate the point
		//  based on the other two, say x and y, and then fill at that point

		// 3 Cases - if Least variation is in x, y or z

		if (Mathf.Abs(C) > Mathf.Abs(A) && Mathf.Abs(C) > Mathf.Abs(B)) // z
		{
			//Debug.Log("Case 1: x-y");
			// Get ymin, ymax
			posList.Sort(SortByY);
			int ymin = posList[0].y;
			int ymax = posList[posList.Count-1].y;
			//Debug.Log("ymin: " + ymin);
			//Debug.Log("ymax: " + ymax);
			
			// For each y, get all points that intersect scan line
			List<WorldPos> scanIntersection;
			for (int y = ymin + 1; y < ymax; y++)
			{
				//Debug.Log("y = " + y);
				scanIntersection = new List<WorldPos>();
				// Get list of points that have y-value
				//  [ ] - This can be optimized
				foreach (List<WorldPos> edge in edgeList)
				{
					foreach (WorldPos pos in edge)
					{
						if (pos.y == y)
						{
							scanIntersection.Add(pos);
							//Debug.Log("Added: " + pos.x + "," + pos.y + "," + pos.z);
							break; // only need one per edge
						}
					}
				}
				
				// Sort by x
				scanIntersection.Sort(SortByX);
				
				// Fill Pairwise
				for (int i = 0; i < scanIntersection.Count/2; i++)
				{
					int xmin = scanIntersection[2*i].x;
					int xmax = scanIntersection[2*i + 1].x;
					//Debug.Log("xmin: " + xmin);
					//Debug.Log("xmax: " + xmax);
					
					for (int x = xmin + 1; x < xmax; x++)
					{
						// Calculate the z-value
						float z = -A/C*(float)x - B/C*(float)y - D/C;
						// Place block
						Block tempBlock = chunk.world.GetBlock(x, y, Mathf.RoundToInt(z));
						if (tempBlock is BlockAir)
						{
							chunk.world.SetBlock(x, y, Mathf.RoundToInt(z), block);
							filledPosList.Add(new WorldPos(x, y, Mathf.RoundToInt(z)));
							//Debug.Log("Placed: " + x + "," + y + "," + z + "/" + Mathf.RoundToInt(z));
						}
					}
				}
			}
		} else if (Mathf.Abs(B) > Mathf.Abs(A) && Mathf.Abs(B) > Mathf.Abs(C)) // y
		{
			//Debug.Log("Case 2: x-z");
			// Get zmin, zmax
			posList.Sort(SortByZ);
			int zmin = posList[0].z;
			int zmax = posList[posList.Count-1].z;
			//Debug.Log("zmin: " + zmin);
			//Debug.Log("zmax: " + zmax);
			
			// For each z, get all points that intersect scan line
			List<WorldPos> scanIntersection;
			for (int z = zmin + 1; z < zmax; z++)
			{
				//Debug.Log("z = " + z);
				scanIntersection = new List<WorldPos>();
				// Get list of points that have y-value
				//  [ ] - This can be optimized
				foreach (List<WorldPos> edge in edgeList)
				{
					foreach (WorldPos pos in edge)
					{
						if (pos.z == z)
						{
							scanIntersection.Add(pos);
							//Debug.Log("Added: " + pos.x + "," + pos.y + "," + pos.z);
							break; // only need one per edge
						}
					}
				}
				
				// Sort by x
				scanIntersection.Sort(SortByX);
				
				// Fill Pairwise
				for (int i = 0; i < scanIntersection.Count/2; i++)
				{
					int xmin = scanIntersection[2*i].x;
					int xmax = scanIntersection[2*i + 1].x;
					//Debug.Log("xmin: " + xmin);
					//Debug.Log("xmax: " + xmax);
					
					for (int x = xmin + 1; x < xmax; x++)
					{
						// Calculate the y-value
						float y = -A/B*(float)x - C/B*(float)z - D/B;
						// Place block
						Block tempBlock = chunk.world.GetBlock(x, Mathf.RoundToInt(y), z);
						if (tempBlock is BlockAir)
						{
							chunk.world.SetBlock(x, Mathf.RoundToInt(y), z, block);
							filledPosList.Add(new WorldPos(x, Mathf.RoundToInt(y), z));
							//Debug.Log("Placed: " + x + "," + y + "/" + Mathf.RoundToInt(y) + "," + z);
						}
					}
				}
			}
		} else // (A > B && A > C) ie. x
		{
			//Debug.Log("Case 3: y-z");
			// Get zmin, zmax
			posList.Sort(SortByZ);
			int zmin = posList[0].z;
			int zmax = posList[posList.Count-1].z;
			//Debug.Log("zmin: " + zmin);
			//Debug.Log("zmax: " + zmax);
			
			// For each z, get all points that intersect scan line
			List<WorldPos> scanIntersection;
			for (int z = zmin + 1; z < zmax; z++)
			{
				//Debug.Log("z = " + z);
				scanIntersection = new List<WorldPos>();
				// Get list of points that have y-value
				//  [ ] - This can be optimized
				foreach (List<WorldPos> edge in edgeList)
				{
					foreach (WorldPos pos in edge)
					{
						if (pos.z == z)
						{
							scanIntersection.Add(pos);
							//Debug.Log("Added: " + pos.x + "," + pos.y + "," + pos.z);
							break; // only need one per edge
						}
					}
				}
				
				// Sort by y
				scanIntersection.Sort(SortByY);
				
				// Fill Pairwise
				for (int i = 0; i < scanIntersection.Count/2; i++)
				{
					int ymin = scanIntersection[2*i].y;
					int ymax = scanIntersection[2*i + 1].y;
					//Debug.Log("ymin: " + ymin);
					//Debug.Log("ymax: " + ymax);
					
					for (int y = ymin + 1; y < ymax; y++)
					{
						// Calculate the x-value
						float x = -B/A*(float)y - C/A*(float)z - D/A;
						// Place block
						Block tempBlock = chunk.world.GetBlock(Mathf.RoundToInt(x), y, z);
						if (tempBlock is BlockAir)
						{
							chunk.world.SetBlock(Mathf.RoundToInt(x), y, z, block);
							filledPosList.Add(new WorldPos(Mathf.RoundToInt(x), y, z));
							//Debug.Log("Placed: " + Mathf.RoundToInt(x) + "/" + x + "," + y + "," + z);
						}
					}
				}
			}
		}

		// [ ] - Eventually, probably want to use incremental calculation for z, as follows:
		// Shifting along the scan line
		//  z(x+1,y) = z - A/C
		// Shifting the scan line down
		//  z(x+dx,y+1) = z - A/C dx - B/C

		return filledPosList;
	}

	private static int SortByX(WorldPos p1, WorldPos p2)
	{
		return p1.x.CompareTo(p2.x);
	}
	private static int SortByY(WorldPos p1, WorldPos p2)
	{
		return p1.y.CompareTo(p2.y);
	}
	private static int SortByZ(WorldPos p1, WorldPos p2)
	{
		return p1.z.CompareTo(p2.z);
	}

	public static List<WorldPos> LoftAndFillPlanes(List<WorldPos> vertexPosList1, List<List<WorldPos>> edgeList1, 
	                                        List<WorldPos> vertexPosList2, List<List<WorldPos>> edgeList2, 
	                                        RaycastHit lastHit, Block block)
	{
		Debug.Log("Starting Loft and Fill");
		Chunk chunk = lastHit.collider.GetComponent<Chunk>();
		if (chunk == null)
			return new List<WorldPos>();
		
		List<WorldPos> filledPosList = new List<WorldPos>();
		
		// Goal of this function is to join two planes into a 3d object
		// We'll start by drawing the surface
		// Given two planes that have already been drawn
		// 1. Decompose the loft surface into a set of triangles
		List<WorldPos> vertices1;
		List<WorldPos> vertices2;
		List<List<WorldPos>> edges1;
		List<List<WorldPos>> edges2;
		if (vertexPosList1.Count > vertexPosList2.Count)
		{
			// Set list 1 to be polygon with more vertices
			vertices1 = vertexPosList1;
			vertices2 = vertexPosList2;
			edges1 = edgeList1;
			edges2 = edgeList2;
		} else {
			vertices1 = vertexPosList2;
			vertices2 = vertexPosList1;
			edges1 = edgeList2;
			edges2 = edgeList1;
		}

		// Create copies of two faces and set them so they're centered at 0, faces are aligned the same
		List<WorldPos> centeredList1 = centerAndAlignPolygon(vertices1);
		List<WorldPos> centeredList2 = centerAndAlignPolygon(vertices2);

		Debug.Log("Passed center align");

		// Take face with more vertices
		//  For each vertex, find nearest vertex on the other face, draw an edge
		//  If there was an edge previously drawn to that vertex, define the triangle, fill it in
		// We'll store the edges we draw in the format (x,y), where x is the index of the vertex
		//  in polygon 1, and y is the vertex in polygon 2 that defines the edge
		int previousVertex = 100; // some unreasonable number
		List<WorldPos> previousEdge = new List<WorldPos>();

		for (int i = 0; i < centeredList1.Count; i++)
		{
			Debug.Log("i = " + i);
			int j = indexOfClosestVertex(centeredList1[i], centeredList2);
			Debug.Log("j = " + j);
			// Draw edge
			List<WorldPos> currentEdge = SetAllBlocksBetweenPos(vertices1[i], vertices2[j], lastHit, new BlockTemp());
			filledPosList.AddRange(currentEdge);

			if (i > 0) // Don't fill in triangles if it's the first edge
			{
				if (j == previousVertex)
				{
					Debug.Log("j == previousVertex");
					// Draw triangle
					//  Need 3 points, 3 edges in one list
					Vector3 p1 = WorldPos.VectorFromWorldPos(vertices1[i]);
					Vector3 p2 = WorldPos.VectorFromWorldPos(vertices2[j]);
					Vector3 p3 = WorldPos.VectorFromWorldPos(vertices1[i - 1]);
					Plane currentPlane = Plane.newPlaneWithPoints(p1, p2, p3);
					List<WorldPos> currentPosList = new List<WorldPos>();
					currentPosList.Add(vertices1[i]);
					currentPosList.AddRange(edges1[i]);
					currentPosList.Add(vertices2[j]);
					currentPosList.AddRange(currentEdge);
					currentPosList.Add(vertices1[i - 1]);
					currentPosList.AddRange(previousEdge);
					List<List<WorldPos>> currentEdgeList = new List<List<WorldPos>>();
					currentEdgeList.Add(edges1[i]);
					currentEdgeList.Add(currentEdge);
					currentEdgeList.Add(previousEdge);

					List<WorldPos> placedOnPlane = SetAllBlocksInPlane(currentPosList, currentEdgeList, currentPlane, lastHit, block);
					filledPosList.AddRange(placedOnPlane);
				} else {
					// 2 Possible diagonals to draw
					// or more!
					if (j - previousVertex == 1)
					{
						// Here, just have to handle ambiguous case

					} else 
					{
						// Loop over j vertices that we skipped
						List<WorldPos> previousInnerEdge = previousEdge;
						bool ambiguousCaseHandled = false;
						for (int tempj = previousVertex + 1; tempj < j; j++)
						{
							List<WorldPos> currentInnerEdge = new List<WorldPos>();
							// for each of these vertices, check which between vertex i and i-1 is closer, draw edge
							float distance1 = Vector3.Distance(WorldPos.VectorFromWorldPos(vertices2[tempj]), WorldPos.VectorFromWorldPos(vertices1[i-1]));
							float distance2 = Vector3.Distance(WorldPos.VectorFromWorldPos(vertices2[tempj]), WorldPos.VectorFromWorldPos(vertices1[i]));
							if (distance1 < distance2)
							{
								// Set edge tempj -> i-1
								currentInnerEdge = SetAllBlocksBetweenPos(vertices1[i-1], vertices2[tempj], lastHit, new BlockTemp());
								filledPosList.AddRange(currentInnerEdge);
								
								// Draw the triangle tempj-1, tempj, i-1
								Vector3 p1 = WorldPos.VectorFromWorldPos(vertices1[i-1]);
								Vector3 p2 = WorldPos.VectorFromWorldPos(vertices2[tempj]);
								Vector3 p3 = WorldPos.VectorFromWorldPos(vertices2[tempj - 1]);
								Plane currentPlane = Plane.newPlaneWithPoints(p1, p2, p3);
								List<WorldPos> currentPosList = new List<WorldPos>();
								currentPosList.Add(vertices1[i-1]);
								currentPosList.Add(vertices2[tempj]);
								currentPosList.Add(vertices2[tempj - 1]);
								currentPosList.AddRange(edges2[tempj]);
								currentPosList.AddRange(currentInnerEdge);
								currentPosList.AddRange(previousInnerEdge);
								List<List<WorldPos>> currentEdgeList = new List<List<WorldPos>>();
								currentEdgeList.Add(edges2[tempj]);
								currentEdgeList.Add(currentInnerEdge);
								currentEdgeList.Add(previousInnerEdge);
								
								List<WorldPos> placedOnPlane = SetAllBlocksInPlane(currentPosList, currentEdgeList, currentPlane, lastHit, block);
								filledPosList.AddRange(placedOnPlane);
								
							} else if (!ambiguousCaseHandled) {
								// The ambiguous case is caused by both vertices incrementing at the same time
								// This creates a quadrilateral, which we don't know how to shade
								// So we break it into 2 triangles
								// We don't know of a better way to make this decision than to just take the shorter diagonal
								// The four points we have to deal with are tempj, tempj-1, i and i-1
								Vector3 p1 = WorldPos.VectorFromWorldPos(vertices1[i]);
								Vector3 p2 = WorldPos.VectorFromWorldPos(vertices1[i-1]);
								Vector3 p3 = WorldPos.VectorFromWorldPos(vertices2[tempj]);
								Vector3 p4 = WorldPos.VectorFromWorldPos(vertices2[tempj-1]);
								// Compares distances
								if (Vector3.Distance(p2, p3) < Vector3.Distance(p1, p4))
								{
									// Set edge i-1 -> tempj
									List<WorldPos> diagonalEdge = SetAllBlocksBetweenPos(vertices1[i-1], vertices2[tempj], lastHit, new BlockTemp());
									filledPosList.AddRange(diagonalEdge);
									// Draw the triangle i-1, tempj, tempj-1

									// Draw the triangle i-1, tempj, i

								} else
								{
									// Set edge i -> tempj-1
									List<WorldPos> diagonalEdge = SetAllBlocksBetweenPos(vertices1[i], vertices2[tempj-1], lastHit, new BlockTemp());
									filledPosList.AddRange(diagonalEdge);
									// Draw the triangle i, tempj-1, tempj

									// Draw the triangle i, tempj-1, i-1
								}

								ambiguousCaseHandled = true;
							} else {
								// Set edge tempj -> i
								currentInnerEdge = SetAllBlocksBetweenPos(vertices1[i], vertices2[tempj], lastHit, new BlockTemp());
								filledPosList.AddRange(currentInnerEdge);
								
								// Draw the triangle tempj-1, tempj, i
								Vector3 p1 = WorldPos.VectorFromWorldPos(vertices1[i]);
								Vector3 p2 = WorldPos.VectorFromWorldPos(vertices2[tempj]);
								Vector3 p3 = WorldPos.VectorFromWorldPos(vertices2[tempj - 1]);
								Plane currentPlane = Plane.newPlaneWithPoints(p1, p2, p3);
								List<WorldPos> currentPosList = new List<WorldPos>();
								currentPosList.Add(vertices1[i]);
								currentPosList.AddRange(edges2[tempj]);
								currentPosList.Add(vertices2[tempj]);
								currentPosList.AddRange(currentInnerEdge);
								currentPosList.Add(vertices2[tempj - 1]);
								currentPosList.AddRange(previousInnerEdge);
								List<List<WorldPos>> currentEdgeList = new List<List<WorldPos>>();
								currentEdgeList.Add(edges2[tempj]);
								currentEdgeList.Add(currentInnerEdge);
								currentEdgeList.Add(previousInnerEdge);
								
								List<WorldPos> placedOnPlane = SetAllBlocksInPlane(currentPosList, currentEdgeList, currentPlane, lastHit, block);
								filledPosList.AddRange(placedOnPlane);
								
							}
							previousInnerEdge = currentInnerEdge;
						}
					}
				}
			}
			previousVertex = j;
			previousEdge = currentEdge;
		}

		// [ ] - For testing purposes, add a pause after drawing every triangle - will be interesting to see

		// If you switch which vertex you're drawing to, you created a 4sided polygon of some sort
		// Need to split it - choose the shorter diagonal


		// Now we have to fill in the volume

		return filledPosList;
	}

	private static List<WorldPos> centerAndAlignPolygon(List<WorldPos> vertexPosList)
	{
		List<WorldPos> newVertexPosList = new List<WorldPos>();

		if (vertexPosList.Count == 1)
		{
			// Just a point
			// Obvious
			newVertexPosList.Add(new WorldPos(0, 0, 0));
			return newVertexPosList;
		}
		// Calculate center of mass, use as offset to center polygon at 0,0,0
		Vector3 sumPos = new Vector3(0, 0, 0);

		foreach (WorldPos p in vertexPosList)
		{
			Debug.Log("original: " + p.x + "," + p.y + "," + p.z);
			sumPos += new Vector3((float)p.x, (float)p.y, (float)p.z);
		}
		Vector3 offset = sumPos/vertexPosList.Count;

		List<WorldPos> offsetVertexPosList = new List<WorldPos>();
		foreach (WorldPos p in vertexPosList)
		{
			offsetVertexPosList.Add(new WorldPos(p.x - Mathf.RoundToInt(offset.x), p.y - Mathf.RoundToInt(offset.y), p.z - Mathf.RoundToInt(offset.z)));
		}

		if (vertexPosList.Count == 2)
		{
			// A line
			// Need to rotate to the x-z plane, but a specific way - want to rotate along straight line
			// Axis of rotation will be cross product of line and y-axis
			//  Easy way to do this - just take the cross product, set it as a point, then apply the plane case
			Vector3 cross = Vector3.Cross(WorldPos.VectorFromWorldPos(offsetVertexPosList[0]), WorldPos.VectorFromWorldPos(offsetVertexPosList[1]));
			offsetVertexPosList.Add (new WorldPos(Mathf.RoundToInt(cross.x), Mathf.RoundToInt(cross.y), Mathf.RoundToInt(cross.z)));
		} 
		// Rotate polygon to the x-z plane
		// Why the x-z plane? Why not
		// Find normal of plane
		Vector3 p1 = WorldPos.VectorFromWorldPos(offsetVertexPosList[0]);
		Vector3 p2 = WorldPos.VectorFromWorldPos(offsetVertexPosList[1]);
		Vector3 p3 = WorldPos.VectorFromWorldPos(offsetVertexPosList[2]);
		Plane currentPlane = Plane.newPlaneWithPoints(p1, p2, p3);
		Debug.Log("normal: " + currentPlane.normal.x + "," + currentPlane.normal.y + "," + currentPlane.normal.z);
		
		// Problem: the choice of normal vector, inward or outward, will determine the way you rotate
		//  We need to be consistent about which normal we take
		// Note: inward or outward is determined by clockwise or counterclockwise construction of points!
		//  In particular, the 3 points that we take to define the plane
		if (currentPlane.normal.y < 0)
		{
			currentPlane.normal = -currentPlane.normal;
			currentPlane.offset = -currentPlane.offset;
		}
		// [ ] - Few more cases for when y = 0?
		
		// Want new normal of plane to be (0, 1, 0) ie. the y-axis
		Quaternion q = Quaternion.FromToRotation(currentPlane.normal, new Vector3(0, 1, 0));
		
		foreach (WorldPos pos in offsetVertexPosList)
		{
			Debug.Log("offset: " + pos.x + "," + pos.y + "," + pos.z);
			Vector3 p = WorldPos.VectorFromWorldPos(pos);
			p = q * p;
			Debug.Log("rotated: " + Mathf.RoundToInt(p.x) + "," + Mathf.RoundToInt(p.y) + "," + Mathf.RoundToInt(p.z));
			newVertexPosList.Add(new WorldPos(Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.y), Mathf.RoundToInt(p.z)));
		}

		if (vertexPosList.Count == 2)
		{
			// Remove the dummy point that we added
			newVertexPosList.RemoveAt(2);
		}

		return newVertexPosList;
	}

	private static int indexOfClosestVertex(WorldPos pos, List<WorldPos>vertexPosList)
	{
		int indexClosest = 0;
		float distanceClosest = Mathf.Infinity;
		Vector3 p = WorldPos.VectorFromWorldPos(pos);

		for (int i = 0; i < vertexPosList.Count; i++)
		{
			Vector3 currentPos = WorldPos.VectorFromWorldPos(vertexPosList[i]);
			float currentDistance = Vector3.Distance(p, currentPos);
			if (currentDistance < distanceClosest)
			{
				distanceClosest = currentDistance;
				indexClosest = i;
			}
		}
		return indexClosest;
	}
}