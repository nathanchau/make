﻿using UnityEngine;
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

    public static WorldPos SetBlock(RaycastHit hit, World world, Block block, bool adjacent = false)
    {
        Chunk chunk = hit.collider.GetComponent<Chunk>();
        if (chunk == null && hit.collider.tag != "guide")
		{
			return new WorldPos();
		}

        WorldPos pos = GetBlockPos(hit, adjacent);
        world.SetBlock(pos.x, pos.y, pos.z, block);

        return pos;
    }

    public static Block GetBlock(RaycastHit hit, World world, bool adjacent = false)
    {
        Chunk chunk = hit.collider.GetComponent<Chunk>();
        if (chunk == null)
		{
			return null;
		}

        WorldPos pos = GetBlockPos(hit, adjacent);

        Block block = chunk.world.GetBlock(pos.x, pos.y, pos.z);

        return block;
    }

    public static bool IsAdjacentBlockGrass(RaycastHit hit, World world, bool adjacent = false)
    {
		// If you're hitting the guide plane, then you're going to set
		if (hit.collider.tag == "guide")
			return true;
		
		Chunk chunk = hit.collider.GetComponent<Chunk>();
        if (chunk == null)
		{
			return false;
		}

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

    // Sets all blocks between hit1 and hit2, inclusive hit1, inclusive hit2
    public static List<WorldPos> SetAllBlocksBetween(RaycastHit hit1, RaycastHit hit2, World world, Block block, bool adjacent = false)
    {
        // If previous hit was defaulted out, then just setblock
        if (hit1.point == default(RaycastHit).point)
        {
            List<WorldPos> posList1 = new List<WorldPos>();
            WorldPos tempPos1 = SetBlock(hit2, world, block, adjacent);
            posList1.Add(tempPos1);
            return posList1;
        }

        // 3D extension of Bresenham's Line Drawing Algorithm
        //  adapted to use floating point numbers instead of error values

        // Note: Very unlikely that you somehow traverse an entire chunk between update calls, but if it somehow happens,
        //  it's not handled - ie. if there's an unset chunk halfway between two hits, and you call this function
        //  then you'll try to set a block on an empty chunk - nothing bad will happen I don't think, it just won't work
        Chunk chunk = hit2.collider.GetComponent<Chunk>();
        if (chunk == null && hit2.collider.tag != "guide")
            return new List<WorldPos>();

        // Get endpoints
        WorldPos pos1 = GetBlockPos(hit1, adjacent);
        WorldPos pos2 = GetBlockPos(hit2, adjacent);
        //Debug.Log("pos1: " + pos1.x + "," + pos1.y + "," + pos1.z);
        //Debug.Log("pos2: " + pos2.x + "," + pos2.y + "," + pos2.z);
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

        // Set first block
        // Note: Have to check if replacing blockgrass or block air in this function - kind of inconsistent
        //Block tempFirstBlock = chunk.world.GetBlock(Mathf.RoundToInt(tempPos.x), Mathf.RoundToInt(tempPos.y), Mathf.RoundToInt(tempPos.z));
        //if (tempFirstBlock is BlockAir)
        //{
        world.SetBlock(Mathf.RoundToInt(tempPos.x), Mathf.RoundToInt(tempPos.y), Mathf.RoundToInt(tempPos.z), block);
        posList.Add(new WorldPos(Mathf.RoundToInt(tempPos.x), Mathf.RoundToInt(tempPos.y), Mathf.RoundToInt(tempPos.z)));
        //}

        for (int i = 0; i < N; i++)
        {
            tempPos = tempPos + step;
            posList.Add(new WorldPos(Mathf.RoundToInt(tempPos.x), Mathf.RoundToInt(tempPos.y), Mathf.RoundToInt(tempPos.z)));

            // Note: Have to check if replacing blockgrass or block air in this function - kind of inconsistent
            Block tempBlock = world.GetBlock(Mathf.RoundToInt(tempPos.x), Mathf.RoundToInt(tempPos.y), Mathf.RoundToInt(tempPos.z));
            if (tempBlock is BlockAir)
            {
                world.SetBlock(Mathf.RoundToInt(tempPos.x), Mathf.RoundToInt(tempPos.y), Mathf.RoundToInt(tempPos.z), block);
            }
        }

        // Note: this might not be ideal for "shading in" parts of solids, because it doesn't fill every point it intersects, 
        //  but rather a single line between the two points

        return posList;
    }

	// Same as above, just using worldpos varibles for endpoints instead
	public static List<WorldPos> SetAllBlocksBetweenPos(WorldPos pos1, WorldPos pos2, World world, RaycastHit hit, Block block)
	{
		Chunk chunk = hit.collider.GetComponent<Chunk>();
		if (chunk == null && hit.collider.tag != "guide") // [ ] - not completely confident in this
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

        // Set first block
        // Note: Have to check if replacing blockgrass or block air in this function - kind of inconsistent
        //Block tempFirstBlock = chunk.world.GetBlock(Mathf.RoundToInt(tempPos.x), Mathf.RoundToInt(tempPos.y), Mathf.RoundToInt(tempPos.z));
        //if (tempFirstBlock is BlockAir)
        //{
        world.SetBlock(Mathf.RoundToInt(tempPos.x), Mathf.RoundToInt(tempPos.y), Mathf.RoundToInt(tempPos.z), block);
        posList.Add(new WorldPos(Mathf.RoundToInt(tempPos.x), Mathf.RoundToInt(tempPos.y), Mathf.RoundToInt(tempPos.z)));
        //}

        for (int i = 0; i < N; i++)
        {
            tempPos = tempPos + step;
            posList.Add(new WorldPos(Mathf.RoundToInt(tempPos.x), Mathf.RoundToInt(tempPos.y), Mathf.RoundToInt(tempPos.z)));

            // Note: Have to check if replacing blockgrass or block air in this function - kind of inconsistent
            Block tempBlock = world.GetBlock(Mathf.RoundToInt(tempPos.x), Mathf.RoundToInt(tempPos.y), Mathf.RoundToInt(tempPos.z));
            if (tempBlock is BlockAir)
            {
                world.SetBlock(Mathf.RoundToInt(tempPos.x), Mathf.RoundToInt(tempPos.y), Mathf.RoundToInt(tempPos.z), block);
            }
        }

        // Note: this might not be ideal for "shading in" parts of solids, because it doesn't fill every point it intersects, 
        //  but rather a single line between the two points

        return posList;
	}
	
	public static bool SetAllBlocksGivenPos(World world, List<WorldPos> posList, RaycastHit lastHit, Block block, bool checkEnvironment = false, WorldState worldState = null)
    {
        Chunk chunk = lastHit.collider.GetComponent<Chunk>();
        if (chunk == null && lastHit.collider.tag != "guide")
            return false;

        foreach (WorldPos pos in posList)
        {
            if (checkEnvironment)
            {
                // Check against the worldState
                List<Shape> shapes = worldState.shapesAtPos(pos);
                if (shapes.Count == 0)
                    world.SetBlock(pos.x, pos.y, pos.z, block);
            }
            else
                world.SetBlock(pos.x, pos.y, pos.z, block);
        }
        return true;
    }

	public static List<WorldPos> SetAllBlocksInPlane(World world, List<WorldPos> posList, List<WorldPos> vertexPosList, List<List<WorldPos>> edgeList, Plane plane, RaycastHit lastHit, Block block)
	{
		Chunk chunk = lastHit.collider.GetComponent<Chunk>();
		if (chunk == null && lastHit.collider.tag != "guide")
			return new List<WorldPos>();

		List<WorldPos> filledPosList = new List<WorldPos>();

		// Using equation z = -A/C x - B/C y - D/C where C(z) has largest coefficient
		// [ ] - How do you make sure it generalizes like this?
		float A = plane.normal.x;
		float B = plane.normal.y;
		float C = plane.normal.z;
		float D = plane.offset;
		//Debug.Log("A: " + A + ", B: " + B + ", C: " + C + ", D: " + D);

        //foreach (WorldPos pos in vertexPosList)
        //    Debug.Log(pos.x + "," + pos.y + "," + pos.z);
        //foreach (List<WorldPos> edge in edgeList)
        //{
        //    foreach (WorldPos pos in edge)
        //    {
        //        Debug.Log(edgeList.IndexOf(edge) + ": " + pos.x + "," + pos.y + "," + pos.z);
        //    }
        //}

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

                // List of edge indices to ignore
                List<int> ignoreEdgeIndices = new List<int>();

                // First, handle the special case
                // If scan line is on a vertex, you either want to take one point, which means you fill beside it
                //  or take 2 points, which means you don't fill 
                // We'll take two points if it's a local minimum/maximum, otherwise only take one point
                // First, detect if we're on a vertex
                for (int i = 0; i < vertexPosList.Count; i++)
                {
                    if (vertexPosList[i].y == y)
                    {
                        // If we're on a vertex, then get the edges that connect to it
                        int indexEdge1 = i - 1;
                        int indexEdge2 = i + 1;
                        // Handle wraparound cases
                        if (indexEdge1 > vertexPosList.Count - 1)
                            indexEdge1 -= vertexPosList.Count;
                        else if (indexEdge1 < 0)
                            indexEdge1 += vertexPosList.Count;
                        if (indexEdge2 > vertexPosList.Count - 1)
                            indexEdge2 -= vertexPosList.Count;
                        else if (indexEdge2 < 0)
                            indexEdge2 += vertexPosList.Count;
                        List<WorldPos> edge1 = edgeList[indexEdge1];
                        List<WorldPos> edge2 = edgeList[indexEdge2];

                        // Now we have to test whether vertex is local minimum/maximum
                        //  To do this, we'll just look at the endpoints
                        //  Get the endpoints of edges that aren't at the vertex
                        WorldPos p1 = edge1[0];
                        WorldPos p2 = edge1[edge1.Count - 1];
                        WorldPos p3 = edge2[0];
                        WorldPos p4 = edge2[edge2.Count - 1];
                        Vector3 end1;
                        Vector3 end2;
                        if ((WorldPos.VectorFromWorldPos(p1) - WorldPos.VectorFromWorldPos(vertexPosList[i])).magnitude > (WorldPos.VectorFromWorldPos(p1) - WorldPos.VectorFromWorldPos(vertexPosList[i])).magnitude)
                            end1 = WorldPos.VectorFromWorldPos(p1);
                        else
                            end1 = WorldPos.VectorFromWorldPos(p2);
                        if ((WorldPos.VectorFromWorldPos(p3) - WorldPos.VectorFromWorldPos(vertexPosList[i])).magnitude > (WorldPos.VectorFromWorldPos(p4) - WorldPos.VectorFromWorldPos(vertexPosList[i])).magnitude)
                            end2 = WorldPos.VectorFromWorldPos(p3);
                        else
                            end2 = WorldPos.VectorFromWorldPos(p4);

                        // If signs of z values are the same, then we keep both
                        // Otherwise, we keep only one
                        if (((end1 - WorldPos.VectorFromWorldPos(vertexPosList[i])).y < 0 && (end2 - WorldPos.VectorFromWorldPos(vertexPosList[i])).y < 0)
                            || ((end1 - WorldPos.VectorFromWorldPos(vertexPosList[i])).y > 0 && (end2 - WorldPos.VectorFromWorldPos(vertexPosList[i])).y > 0))
                        {
                            // Keep both points
                        }
                        else
                        {
                            // Keep only one
                            //Debug.Log("KEPT ONLY ONE");
                            ignoreEdgeIndices.Add(indexEdge1);
                        }
                    }
                }

                // Get list of points that have y-value
                //  [ ] - This can be optimized
                foreach (List<WorldPos> edge in edgeList)
				{
                    if (!ignoreEdgeIndices.Contains(edgeList.IndexOf(edge)))
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
						Block tempBlock = world.GetBlock(x, y, Mathf.RoundToInt(z));
                        filledPosList.Add(new WorldPos(x, y, Mathf.RoundToInt(z)));
                        if (tempBlock is BlockAir)
						{
							world.SetBlock(x, y, Mathf.RoundToInt(z), block);
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

                // List of edge indices to ignore
                List<int> ignoreEdgeIndices = new List<int>();

                // First, handle the special case
                // If scan line is on a vertex, you either want to take one point, which means you fill beside it
                //  or take 2 points, which means you don't fill 
                // We'll take two points if it's a local minimum/maximum, otherwise only take one point
                // First, detect if we're on a vertex
                for (int i = 0; i < vertexPosList.Count; i++)
                {
                    //Debug.Log(vertexPosList[i].z);
                    if (vertexPosList[i].z == z)
                    {
                        //Debug.Log("checking");
                        // If we're on a vertex, then get the edges that connect to it
                        int indexEdge1 = i - 1;
                        int indexEdge2 = i + 1;
                        // Handle wraparound cases
                        if (indexEdge1 > vertexPosList.Count - 1)
                            indexEdge1 -= vertexPosList.Count;
                        else if (indexEdge1 < 0)
                            indexEdge1 += vertexPosList.Count;
                        if (indexEdge2 > vertexPosList.Count - 1)
                            indexEdge2 -= vertexPosList.Count;
                        else if (indexEdge2 < 0)
                            indexEdge2 += vertexPosList.Count;
                        List<WorldPos> edge1 = edgeList[indexEdge1];
                        List<WorldPos> edge2 = edgeList[indexEdge2];

                        // Now we have to test whether vertex is local minimum/maximum
                        //  To do this, we'll just look at the endpoints
                        //  Get the endpoints of edges that aren't at the vertex
                        WorldPos p1 = edge1[0];
                        WorldPos p2 = edge1[edge1.Count - 1];
                        WorldPos p3 = edge2[0];
                        WorldPos p4 = edge2[edge2.Count - 1];
                        Vector3 end1;
                        Vector3 end2;
                        if ((WorldPos.VectorFromWorldPos(p1) - WorldPos.VectorFromWorldPos(vertexPosList[i])).magnitude > (WorldPos.VectorFromWorldPos(p1) - WorldPos.VectorFromWorldPos(vertexPosList[i])).magnitude)
                            end1 = WorldPos.VectorFromWorldPos(p1);
                        else
                            end1 = WorldPos.VectorFromWorldPos(p2);
                        if ((WorldPos.VectorFromWorldPos(p3) - WorldPos.VectorFromWorldPos(vertexPosList[i])).magnitude > (WorldPos.VectorFromWorldPos(p4) - WorldPos.VectorFromWorldPos(vertexPosList[i])).magnitude)
                            end2 = WorldPos.VectorFromWorldPos(p3);
                        else
                            end2 = WorldPos.VectorFromWorldPos(p4);

                        // If signs of z values are the same, then we keep both
                        // Otherwise, we keep only one
                        if (((end1 - WorldPos.VectorFromWorldPos(vertexPosList[i])).z < 0 && (end2 - WorldPos.VectorFromWorldPos(vertexPosList[i])).z < 0) 
                            || ((end1 - WorldPos.VectorFromWorldPos(vertexPosList[i])).z > 0 && (end2 - WorldPos.VectorFromWorldPos(vertexPosList[i])).z > 0))
                        {
                            // Keep both points
                        }
                        else
                        {
                            // Keep only one
                            //Debug.Log("KEPT ONLY ONE");
                            ignoreEdgeIndices.Add(indexEdge1);
                        }
                    }
                }

				// Get list of points that have y-value
				//  [ ] - This can be optimized
				foreach (List<WorldPos> edge in edgeList)
				{
                    if (!ignoreEdgeIndices.Contains(edgeList.IndexOf(edge)))
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
						Block tempBlock = world.GetBlock(x, Mathf.RoundToInt(y), z);
                        filledPosList.Add(new WorldPos(x, Mathf.RoundToInt(y), z));
                        if (tempBlock is BlockAir)
						{
							world.SetBlock(x, Mathf.RoundToInt(y), z, block);
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

                // List of edge indices to ignore
                List<int> ignoreEdgeIndices = new List<int>();

                // First, handle the special case
                // If scan line is on a vertex, you either want to take one point, which means you fill beside it
                //  or take 2 points, which means you don't fill 
                // We'll take two points if it's a local minimum/maximum, otherwise only take one point
                // First, detect if we're on a vertex
                for (int i = 0; i < vertexPosList.Count; i++)
                {
                    if (vertexPosList[i].z == z)
                    {
                        // If we're on a vertex, then get the edges that connect to it
                        int indexEdge1 = i - 1;
                        int indexEdge2 = i + 1;
                        // Handle wraparound cases
                        if (indexEdge1 > vertexPosList.Count - 1)
                            indexEdge1 -= vertexPosList.Count;
                        else if (indexEdge1 < 0)
                            indexEdge1 += vertexPosList.Count;
                        if (indexEdge2 > vertexPosList.Count - 1)
                            indexEdge2 -= vertexPosList.Count;
                        else if (indexEdge2 < 0)
                            indexEdge2 += vertexPosList.Count;
                        List<WorldPos> edge1 = edgeList[indexEdge1];
                        List<WorldPos> edge2 = edgeList[indexEdge2];

                        // Now we have to test whether vertex is local minimum/maximum
                        //  To do this, we'll just look at the endpoints
                        //  Get the endpoints of edges that aren't at the vertex
                        WorldPos p1 = edge1[0];
                        WorldPos p2 = edge1[edge1.Count - 1];
                        WorldPos p3 = edge2[0];
                        WorldPos p4 = edge2[edge2.Count - 1];
                        Vector3 end1;
                        Vector3 end2;
                        if ((WorldPos.VectorFromWorldPos(p1) - WorldPos.VectorFromWorldPos(vertexPosList[i])).magnitude > (WorldPos.VectorFromWorldPos(p1) - WorldPos.VectorFromWorldPos(vertexPosList[i])).magnitude)
                            end1 = WorldPos.VectorFromWorldPos(p1);
                        else
                            end1 = WorldPos.VectorFromWorldPos(p2);
                        if ((WorldPos.VectorFromWorldPos(p3) - WorldPos.VectorFromWorldPos(vertexPosList[i])).magnitude > (WorldPos.VectorFromWorldPos(p4) - WorldPos.VectorFromWorldPos(vertexPosList[i])).magnitude)
                            end2 = WorldPos.VectorFromWorldPos(p3);
                        else
                            end2 = WorldPos.VectorFromWorldPos(p4);

                        // If signs of z values are the same, then we keep both
                        // Otherwise, we keep only one
                        if (((end1 - WorldPos.VectorFromWorldPos(vertexPosList[i])).z < 0 && (end2 - WorldPos.VectorFromWorldPos(vertexPosList[i])).z < 0)
                            || ((end1 - WorldPos.VectorFromWorldPos(vertexPosList[i])).z > 0 && (end2 - WorldPos.VectorFromWorldPos(vertexPosList[i])).z > 0))
                        {
                            // Keep both points
                        }
                        else
                        {
                            // Keep only one
                            //Debug.Log("KEPT ONLY ONE");
                            ignoreEdgeIndices.Add(indexEdge1);
                        }
                    }
                }

                // Get list of points that have y-value
                //  [ ] - This can be optimized
                foreach (List<WorldPos> edge in edgeList)
				{
                    if (!ignoreEdgeIndices.Contains(edgeList.IndexOf(edge)))
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
						Block tempBlock = world.GetBlock(Mathf.RoundToInt(x), y, z);
                        filledPosList.Add(new WorldPos(Mathf.RoundToInt(x), y, z));
                        if (tempBlock is BlockAir)
						{
							world.SetBlock(Mathf.RoundToInt(x), y, z, block);
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
	                                        RaycastHit lastHit, World world, Block block)
	{
		//Debug.Log("STARTING LOFT AND FILL");
		Chunk chunk = lastHit.collider.GetComponent<Chunk>();
		if (chunk == null && lastHit.collider.tag != "guide")
			return new List<WorldPos>();
		
		List<WorldPos> filledPosList = new List<WorldPos>();
		
		// Goal of this function is to join two planes into a 3d object
		// We'll start by drawing the surface
		// Given two planes that have already been drawn
		// 1. Decompose the loft surface into a set of triangles
		List<WorldPos> vertices1; // = new List<WorldPos>(vertexPosList1);
		List<WorldPos> vertices2; // = new List<WorldPos>(vertexPosList2);
		List<List<WorldPos>> edges1; // = edgeList1;
		List<List<WorldPos>> edges2; // = edgeList2;

		if (vertexPosList1.Count > vertexPosList2.Count)
		{
			// Set list 1 to be polygon with more vertices
			vertices1 = new List<WorldPos>(vertexPosList1);
			vertices2 = new List<WorldPos>(vertexPosList2);
			edges1 = new List<List<WorldPos>>(edgeList1);
			edges2 = new List<List<WorldPos>>(edgeList2);
		} else {
			vertices1 = new List<WorldPos>(vertexPosList2);
			vertices2 = new List<WorldPos>(vertexPosList1);
			edges1 = new List<List<WorldPos>>(edgeList2);
			edges2 = new List<List<WorldPos>>(edgeList1);
		}

        // Add the first one at the end so we can wraparound and set last plane
        vertices1.Add(vertices1[0]);
		edges1.Add(edges1[0]);

		// Create copies of two faces and set them so they're centered at 0, faces are aligned the same
		List<WorldPos> centeredList1 = centerAndAlignPolygon(vertices1);
		List<WorldPos> centeredList2 = centerAndAlignPolygon(vertices2);

		// Reorder list 2 so that it's 0th element matches 0th of list 1
		//  Have to do this for centeredlist2, vertices2 and edges2
		int firstIndex = indexOfClosestVertex(centeredList1[0], centeredList2);
		List<WorldPos> reorderPostList = centeredList2.GetRange(0, firstIndex);
		centeredList2.RemoveRange(0, firstIndex);
		centeredList2.AddRange(reorderPostList);

		reorderPostList = vertices2.GetRange(0, firstIndex);
		vertices2.RemoveRange(0, firstIndex);
		vertices2.AddRange(reorderPostList);

		List<List<WorldPos>> reorderEdgeList = edges2.GetRange(0, firstIndex);
		edges2.RemoveRange(0, firstIndex);
		edges2.AddRange(reorderEdgeList);

		// We also have to reorder the list 2 so that it's going in the same order, ie. closest element of list 1 is 
		//  whichever way is closer - seems like a reasonable heuristic
		int secondIndex = indexOfClosestVertex(centeredList1[1], centeredList2);
		if (secondIndex > centeredList2.Count - secondIndex)
		{
			// We need to reverse order
			WorldPos firstPos = centeredList2[0];
			centeredList2.RemoveAt(0);
			centeredList2.Add(firstPos);
			centeredList2.Reverse();

			firstPos = vertices2[0];
			vertices2.RemoveAt(0);
			vertices2.Add(firstPos);
			vertices2.Reverse();

            // The way that edges are reversed is different than vertices
			edges2.Reverse();
		}

		// Take face with more vertices
		//  For each vertex, find nearest vertex on the other face, draw an edge
		//  If there was an edge previously drawn to that vertex, define the triangle, fill it in
		// We'll store the edges we draw in the format (x,y), where x is the index of the vertex
		//  in polygon 1, and y is the vertex in polygon 2 that defines the edge
		int previousVertex = 100; // some unreasonable number
		List<WorldPos> previousEdge = new List<WorldPos>();

		for (int i = 0; i < centeredList1.Count; i++)
		{
			//Debug.Log("Vertex i = " + i);
			//Debug.Log("with position: " + vertices1[i].x + "," + vertices1[i].y + "," + vertices1[i].z + ",");
			int j = indexOfClosestVertex(centeredList1[i], centeredList2);
			//Debug.Log("j = " + j);
			//Debug.Log("with position: " + vertices2[j].x + "," + vertices2[j].y + "," + vertices2[j].z + ",");
				// Draw edge
			List<WorldPos> currentEdge = SetAllBlocksBetweenPos(vertices1[i], vertices2[j], world, lastHit, new BlockTemp());
			filledPosList.AddRange(currentEdge);

			if (i > 0) // Don't fill in triangles if it's the first edge
			{
				if (j == previousVertex)
				{
					//Debug.Log("j == previousVertex");
					// Draw triangle
					//  Need 3 points, 3 edges in one list
					Vector3 p1 = WorldPos.VectorFromWorldPos(vertices1[i]);
					Vector3 p2 = WorldPos.VectorFromWorldPos(vertices2[j]);
					Vector3 p3 = WorldPos.VectorFromWorldPos(vertices1[i - 1]);
					Plane currentPlane = Plane.newPlaneWithPoints(p1, p2, p3);
					List<WorldPos> currentPosList = new List<WorldPos>();
					currentPosList.Add(vertices1[i]);
					currentPosList.Add(vertices2[j]);
					currentPosList.Add(vertices1[i - 1]);
                    currentPosList.AddRange(edges1[i - 1]);
                    currentPosList.AddRange(currentEdge);
                    currentPosList.AddRange(previousEdge);
                    List<WorldPos> currentVertexPosList = new List<WorldPos>();
                    currentVertexPosList.Add(vertices1[i]);
                    currentVertexPosList.Add(vertices2[j]);
                    currentVertexPosList.Add(vertices1[i - 1]);
                    List<List<WorldPos>> currentEdgeList = new List<List<WorldPos>>();
					currentEdgeList.Add(currentEdge);
					currentEdgeList.Add(previousEdge);
                    currentEdgeList.Add(edges1[i - 1]);
                    //Debug.Log("i: " + vertices1[i].x + "," + vertices1[i].y + "," + vertices1[i].z);
                    //Debug.Log("j: " + vertices2[j].x + "," + vertices2[j].y + "," + vertices2[j].z);
                    //Debug.Log("i-1: " + vertices1[i-1].x + "," + vertices1[i - 1].y + "," + vertices1[i - 1].z);
                    //Debug.Log("currentEdge. first:" + currentEdge[0].x + "," + currentEdge[0].y + "," + currentEdge[0].z + ", last: " +
                    //    currentEdge[currentEdge.Count - 1].x + "," + currentEdge[currentEdge.Count - 1].y + "," + currentEdge[currentEdge.Count - 1].z);
                    //Debug.Log("previousEdge. first:" + previousEdge[0].x + "," + previousEdge[0].y + "," + previousEdge[0].z + ", last: " +
                    //    previousEdge[previousEdge.Count - 1].x + "," + previousEdge[previousEdge.Count - 1].y + "," + previousEdge[previousEdge.Count - 1].z);
                    //Debug.Log("edges1[i - 1]. first:" + edges1[i - 1][0].x + "," + edges1[i - 1][0].y + "," + edges1[i - 1][0].z + ", last: " +
                    //    edges1[i - 1][edges1[i - 1].Count - 1].x + "," + edges1[i - 1][edges1[i - 1].Count - 1].y + "," + edges1[i - 1][edges1[i - 1].Count - 1].z);

                    List<WorldPos> placedOnPlane = SetAllBlocksInPlane(world, currentPosList, currentVertexPosList, currentEdgeList, currentPlane, lastHit, block);
					filledPosList.AddRange(placedOnPlane);
				} else {
					// Might be ambiguous case, or might be switching back to the beginning
					// We'll handle wrapping around back to the beginning first
					if (j == 0)
					{
						//Debug.Log("In Wraparound case");
						// Handle it the same as the ambiguous case
						// Just take previousVertex to 0
						Vector3 p1 = WorldPos.VectorFromWorldPos(vertices1[i]);
						Vector3 p2 = WorldPos.VectorFromWorldPos(vertices1[i-1]);
						Vector3 p3 = WorldPos.VectorFromWorldPos(vertices2[j]);
						Vector3 p4 = WorldPos.VectorFromWorldPos(vertices2[previousVertex]);
						// Compares distances
						if (Vector3.Distance(p2, p3) < Vector3.Distance(p1, p4))
						{
							//Debug.Log("0 Setting edge i-1=" + (i-1) + "->j=" + j);
							// Set edge i-1 -> j
							List<WorldPos> diagonalEdge = SetAllBlocksBetweenPos(vertices1[i-1], vertices2[j], world, lastHit, new BlockTemp());
							filledPosList.AddRange(diagonalEdge);
							
							//Debug.Log("1 Drawing triangle i-1=" + (i-1) + ", j=" + j +", previousVertex=" + (previousVertex));
							// Draw the triangle i-1, j, j-1
							Plane currentPlane = Plane.newPlaneWithPoints(p2, p3, p4);
							List<WorldPos> currentPosList = new List<WorldPos>();
							currentPosList.Add(vertices1[i-1]);
							currentPosList.Add(vertices2[j]);
							currentPosList.Add(vertices2[previousVertex]);
							currentPosList.AddRange(edges2[previousVertex]);
							currentPosList.AddRange(diagonalEdge);
							currentPosList.AddRange(previousEdge);
                            List<WorldPos> currentVertexPosList = new List<WorldPos>();
                            currentVertexPosList.Add(vertices1[i - 1]);
                            currentVertexPosList.Add(vertices2[j]);
                            currentVertexPosList.Add(vertices2[previousVertex]);
                            List<List<WorldPos>> currentEdgeList = new List<List<WorldPos>>();
							currentEdgeList.Add(diagonalEdge);
                            currentEdgeList.Add(edges2[previousVertex]);
                            currentEdgeList.Add(previousEdge);
                            //Debug.Log("i-1: " + vertices1[i-1].x + "," + vertices1[i-1].y + "," + vertices1[i-1].z);
                            //Debug.Log("j: " + vertices2[j].x + "," + vertices2[j].y + "," + vertices2[j].z);
                            //Debug.Log("previousVertex: " + vertices2[previousVertex].x + "," + vertices2[previousVertex].y + "," + vertices2[previousVertex].z);
                            //Debug.Log("diagonalEdge. first:" + diagonalEdge[0].x + "," + diagonalEdge[0].y + "," + diagonalEdge[0].z + ", last: " +
                            //    diagonalEdge[diagonalEdge.Count - 1].x + "," + diagonalEdge[diagonalEdge.Count - 1].y + "," + diagonalEdge[diagonalEdge.Count - 1].z);
                            //Debug.Log("previousEdge. first:" + previousEdge[0].x + "," + previousEdge[0].y + "," + previousEdge[0].z + ", last: " +
                            //    previousEdge[previousEdge.Count - 1].x + "," + previousEdge[previousEdge.Count - 1].y + "," + previousEdge[previousEdge.Count - 1].z);
                            //Debug.Log("edges2[previousVertex]. first:" + edges2[previousVertex][0].x + "," + edges2[previousVertex][0].y + "," + edges2[previousVertex][0].z + ", last: " +
                            //    edges2[previousVertex][edges2[previousVertex].Count - 1].x + "," + edges2[previousVertex][edges2[previousVertex].Count - 1].y + "," + edges2[previousVertex][edges2[previousVertex].Count - 1].z);

                            List<WorldPos> placedOnPlane = SetAllBlocksInPlane(world, currentPosList, currentVertexPosList, currentEdgeList, currentPlane, lastHit, block);
							filledPosList.AddRange(placedOnPlane);
							
							//Debug.Log("2 Drawing triangle i-1=" + (i-1) + ", j=" + j +", i=" + (i));
							// Draw the triangle i-1, j, i
							currentPlane = Plane.newPlaneWithPoints(p1, p2, p3);
							currentPosList = new List<WorldPos>();
							currentPosList.Add(vertices1[i-1]);
							currentPosList.Add(vertices2[j]);
							currentPosList.Add(vertices1[i]);
							currentPosList.AddRange(edges1[i-1]);
							currentPosList.AddRange(currentEdge);
							currentPosList.AddRange(diagonalEdge);
                            currentVertexPosList = new List<WorldPos>();
                            currentVertexPosList.Add(vertices1[i - 1]);
                            currentVertexPosList.Add(vertices2[j]);
                            currentVertexPosList.Add(vertices1[i]);
                            currentEdgeList = new List<List<WorldPos>>();
							currentEdgeList.Add(diagonalEdge);
                            currentEdgeList.Add(currentEdge);
                            currentEdgeList.Add(edges1[i - 1]);
                            //Debug.Log("i-1: " + vertices1[i - 1].x + "," + vertices1[i - 1].y + "," + vertices1[i - 1].z);
                            //Debug.Log("j: " + vertices2[j].x + "," + vertices2[j].y + "," + vertices2[j].z);
                            //Debug.Log("i: " + vertices1[i].x + "," + vertices1[i].y + "," + vertices1[i].z);
                            //Debug.Log("diagonalEdge. first:" + diagonalEdge[0].x + "," + diagonalEdge[0].y + "," + diagonalEdge[0].z + ", last: " +
                            //    diagonalEdge[diagonalEdge.Count - 1].x + "," + diagonalEdge[diagonalEdge.Count - 1].y + "," + diagonalEdge[diagonalEdge.Count - 1].z);
                            //Debug.Log("currentEdge. first:" + currentEdge[0].x + "," + currentEdge[0].y + "," + currentEdge[0].z + ", last: " +
                            //    currentEdge[currentEdge.Count - 1].x + "," + currentEdge[currentEdge.Count - 1].y + "," + currentEdge[currentEdge.Count - 1].z);
                            //Debug.Log("edges1[i-1]. first:" + edges1[i - 1][0].x + "," + edges1[i - 1][0].y + "," + edges1[i - 1][0].z + ", last: " +
                            //    edges1[i - 1][edges1[i - 1].Count - 1].x + "," + edges1[i - 1][edges1[i - 1].Count - 1].y + "," + edges1[i - 1][edges1[i - 1].Count - 1].z);

                            placedOnPlane = SetAllBlocksInPlane(world, currentPosList, currentVertexPosList, currentEdgeList, currentPlane, lastHit, block);
							filledPosList.AddRange(placedOnPlane);
							
						} else
						{
							//Debug.Log("3 Setting edge i=" + (i) + "->j-1=" + (j-1));
							// Set edge i -> j-1
							List<WorldPos> diagonalEdge = SetAllBlocksBetweenPos(vertices1[i], vertices2[previousVertex], world, lastHit, new BlockTemp());
							filledPosList.AddRange(diagonalEdge);
							
							//Debug.Log("4 Drawing triangle i=" + (i) + ", previousVertex=" + (previousVertex) +", j=" + (j));
							// Draw the triangle i, j-1, j
							Plane currentPlane = Plane.newPlaneWithPoints(p1, p3, p4);
							List<WorldPos> currentPosList = new List<WorldPos>();
							currentPosList.Add(vertices1[i]);
							currentPosList.Add(vertices2[j]);
							currentPosList.Add(vertices2[previousVertex]);
							currentPosList.AddRange(edges2[j]);
							currentPosList.AddRange(diagonalEdge);
							currentPosList.AddRange(currentEdge);
                            List<WorldPos> currentVertexPosList = new List<WorldPos>();
                            currentVertexPosList.Add(vertices1[i]);
                            currentVertexPosList.Add(vertices2[j]);
                            currentVertexPosList.Add(vertices2[previousVertex]);
                            List<List<WorldPos>> currentEdgeList = new List<List<WorldPos>>();
							currentEdgeList.Add(currentEdge);
                            currentEdgeList.Add(edges2[previousVertex]);
                            currentEdgeList.Add(diagonalEdge);
                            //Debug.Log("i: " + vertices1[i].x + "," + vertices1[i].y + "," + vertices1[i].z);
                            //Debug.Log("j: " + vertices2[j].x + "," + vertices2[j].y + "," + vertices2[j].z);
                            //Debug.Log("previousVertex: " + vertices2[previousVertex].x + "," + vertices2[previousVertex].y + "," + vertices2[previousVertex].z);
                            //Debug.Log("diagonalEdge. first:" + diagonalEdge[0].x + "," + diagonalEdge[0].y + "," + diagonalEdge[0].z + ", last: " +
                            //    diagonalEdge[diagonalEdge.Count - 1].x + "," + diagonalEdge[diagonalEdge.Count - 1].y + "," + diagonalEdge[diagonalEdge.Count - 1].z);
                            //Debug.Log("currentEdge. first:" + currentEdge[0].x + "," + currentEdge[0].y + "," + currentEdge[0].z + ", last: " +
                            //    currentEdge[currentEdge.Count - 1].x + "," + currentEdge[currentEdge.Count - 1].y + "," + currentEdge[currentEdge.Count - 1].z);
                            //Debug.Log("edges2[previousVertex]. first:" + edges2[previousVertex][0].x + "," + edges2[previousVertex][0].y + "," + edges2[previousVertex][0].z + ", last: " +
                            //    edges2[previousVertex][edges2[previousVertex].Count - 1].x + "," + edges2[previousVertex][edges2[previousVertex].Count - 1].y + "," + edges2[previousVertex][edges2[previousVertex].Count - 1].z);

                            List<WorldPos> placedOnPlane = SetAllBlocksInPlane(world, currentPosList, currentVertexPosList, currentEdgeList, currentPlane, lastHit, block);
							filledPosList.AddRange(placedOnPlane);
							
							//Debug.Log("5 Drawing triangle i=" + (i) + ", previousVertex=" + (previousVertex) +", i-1=" + (i-1));
							// Draw the triangle i, j-1, i-1
							currentPlane = Plane.newPlaneWithPoints(p1, p2, p4);
							currentPosList = new List<WorldPos>();
							currentPosList.Add(vertices1[i-1]);
							currentPosList.Add(vertices2[previousVertex]);
							currentPosList.Add(vertices1[i]);
							currentPosList.AddRange(edges1[i-1]);
							currentPosList.AddRange(previousEdge);
							currentPosList.AddRange(diagonalEdge);
                            currentVertexPosList = new List<WorldPos>();
                            currentVertexPosList.Add(vertices1[i - 1]);
                            currentVertexPosList.Add(vertices2[previousVertex]);
                            currentVertexPosList.Add(vertices1[i]);
                            currentEdgeList = new List<List<WorldPos>>();
							currentEdgeList.Add(previousEdge);
							currentEdgeList.Add(diagonalEdge);
                            currentEdgeList.Add(edges1[i - 1]);
                            //Debug.Log("i-1: " + vertices1[i - 1].x + "," + vertices1[i - 1].y + "," + vertices1[i - 1].z);
                            //Debug.Log("previousVertex: " + vertices2[previousVertex].x + "," + vertices2[previousVertex].y + "," + vertices2[previousVertex].z);
                            //Debug.Log("i: " + vertices1[i].x + "," + vertices1[i].y + "," + vertices1[i].z);
                            //Debug.Log("diagonalEdge. first:" + diagonalEdge[0].x + "," + diagonalEdge[0].y + "," + diagonalEdge[0].z + ", last: " +
                            //    diagonalEdge[diagonalEdge.Count - 1].x + "," + diagonalEdge[diagonalEdge.Count - 1].y + "," + diagonalEdge[diagonalEdge.Count - 1].z);
                            //Debug.Log("previousEdge. first:" + previousEdge[0].x + "," + previousEdge[0].y + "," + previousEdge[0].z + ", last: " +
                            //    previousEdge[previousEdge.Count - 1].x + "," + previousEdge[previousEdge.Count - 1].y + "," + previousEdge[previousEdge.Count - 1].z);
                            //Debug.Log("edges1[i-1]. first:" + edges1[i - 1][0].x + "," + edges1[i - 1][0].y + "," + edges1[i - 1][0].z + ", last: " +
                            //    edges1[i - 1][edges1[i - 1].Count - 1].x + "," + edges1[i - 1][edges1[i - 1].Count - 1].y + "," + edges1[i - 1][edges1[i - 1].Count - 1].z);

                            placedOnPlane = SetAllBlocksInPlane(world, currentPosList, currentVertexPosList, currentEdgeList, currentPlane, lastHit, block);
							filledPosList.AddRange(placedOnPlane);
						}
					}
					// 2 Possible diagonals to draw
					// or more!
					else if (Mathf.Abs(j - previousVertex) == 1)
					{
						//Debug.Log("Entering the matrix");
						int jOffset = j - previousVertex; // Not always j-1 - because of =/- case
						//Debug.Log("jOffset = " + jOffset);
						// Here, just have to handle ambiguous case
						// The ambiguous case is caused by both vertices incrementing at the same time
						// This creates a quadrilateral, which we don't know how to shade
						// So we break it into 2 triangles
						// We don't know of a better way to make this decision than to just take the shorter diagonal
						// The four points we have to deal with are tempj, tempj-1, i and i-1
						Vector3 p1 = WorldPos.VectorFromWorldPos(vertices1[i]);
						Vector3 p2 = WorldPos.VectorFromWorldPos(vertices1[i-1]);
						Vector3 p3 = WorldPos.VectorFromWorldPos(vertices2[j]);
						Vector3 p4 = WorldPos.VectorFromWorldPos(vertices2[j-jOffset]);
						// Compares distances
						if (Vector3.Distance(p2, p3) < Vector3.Distance(p1, p4))
						{
							//Debug.Log("6 Setting edge i-1=" + (i-1) + "->j=" + (j));
							// Set edge i-1 -> j
							List<WorldPos> diagonalEdge = SetAllBlocksBetweenPos(vertices1[i-1], vertices2[j], world, lastHit, new BlockTemp());
							filledPosList.AddRange(diagonalEdge);

							//Debug.Log("7 Drawing triangle i-1=" + (i-1) + ", j=" + (j) +", j-jOffset=" + (j-jOffset));
							// Draw the triangle i-1, j, j-1
							Plane currentPlane = Plane.newPlaneWithPoints(p2, p3, p4);
							List<WorldPos> currentPosList = new List<WorldPos>();
							currentPosList.Add(vertices1[i-1]);
							currentPosList.Add(vertices2[j]);
							currentPosList.Add(vertices2[j - jOffset]);
							currentPosList.AddRange(edges2[j - jOffset]);
							currentPosList.AddRange(diagonalEdge);
							currentPosList.AddRange(previousEdge);
                            List<WorldPos> currentVertexPosList = new List<WorldPos>();
                            currentVertexPosList.Add(vertices1[i - 1]);
                            currentVertexPosList.Add(vertices2[j]);
                            currentVertexPosList.Add(vertices2[j - jOffset]);
                            List<List<WorldPos>> currentEdgeList = new List<List<WorldPos>>();
							currentEdgeList.Add(diagonalEdge);
                            currentEdgeList.Add(edges2[j - jOffset]);
                            currentEdgeList.Add(previousEdge);
                            //Debug.Log("i-1: " + vertices1[i - 1].x + "," + vertices1[i - 1].y + "," + vertices1[i - 1].z);
                            //Debug.Log("j: " + vertices2[j].x + "," + vertices2[j].y + "," + vertices2[j].z);
                            //Debug.Log("j - jOffset: " + vertices2[j - jOffset].x + "," + vertices2[j - jOffset].y + "," + vertices2[j - jOffset].z);
                            //Debug.Log("diagonalEdge. first:" + diagonalEdge[0].x + "," + diagonalEdge[0].y + "," + diagonalEdge[0].z + ", last: " +
                            //    diagonalEdge[diagonalEdge.Count - 1].x + "," + diagonalEdge[diagonalEdge.Count - 1].y + "," + diagonalEdge[diagonalEdge.Count - 1].z);
                            //Debug.Log("previousEdge. first:" + previousEdge[0].x + "," + previousEdge[0].y + "," + previousEdge[0].z + ", last: " +
                            //    previousEdge[previousEdge.Count - 1].x + "," + previousEdge[previousEdge.Count - 1].y + "," + previousEdge[previousEdge.Count - 1].z);
                            //Debug.Log("edges2[j - jOffset]. first:" + edges2[j - jOffset][0].x + "," + edges2[j - jOffset][0].y + "," + edges2[j - jOffset][0].z + ", last: " +
                            //    edges2[j - jOffset][edges2[j - jOffset].Count - 1].x + "," + edges2[j - jOffset][edges2[j - jOffset].Count - 1].y + "," + edges2[j - jOffset][edges2[j - jOffset].Count - 1].z);

                            List<WorldPos> placedOnPlane = SetAllBlocksInPlane(world, currentPosList, currentVertexPosList, currentEdgeList, currentPlane, lastHit, block);
							filledPosList.AddRange(placedOnPlane);
							
							//Debug.Log("8 Drawing triangle i-1=" + (i-1) + ", j=" + (j) +", i=" + (i));
							// Draw the triangle i-1, j, i
							currentPlane = Plane.newPlaneWithPoints(p1, p2, p3);
							currentPosList = new List<WorldPos>();
							currentPosList.Add(vertices1[i-1]);
							currentPosList.Add(vertices2[j]);
							currentPosList.Add(vertices1[i]);
							currentPosList.AddRange(edges1[i-1]);
							currentPosList.AddRange(currentEdge);
							currentPosList.AddRange(diagonalEdge);
                            currentVertexPosList = new List<WorldPos>();
                            currentVertexPosList.Add(vertices1[i - 1]);
                            currentVertexPosList.Add(vertices2[j]);
                            currentVertexPosList.Add(vertices1[i]);
                            currentEdgeList = new List<List<WorldPos>>();
                            currentEdgeList.Add(diagonalEdge);
							currentEdgeList.Add(currentEdge);
                            currentEdgeList.Add(edges1[i - 1]);
                            //Debug.Log("i-1: " + vertices1[i - 1].x + "," + vertices1[i - 1].y + "," + vertices1[i - 1].z);
                            //Debug.Log("j: " + vertices2[j].x + "," + vertices2[j].y + "," + vertices2[j].z);
                            //Debug.Log("i: " + vertices1[i].x + "," + vertices1[i].y + "," + vertices1[i].z);
                            //Debug.Log("diagonalEdge. first:" + diagonalEdge[0].x + "," + diagonalEdge[0].y + "," + diagonalEdge[0].z + ", last: " +
                            //    diagonalEdge[diagonalEdge.Count - 1].x + "," + diagonalEdge[diagonalEdge.Count - 1].y + "," + diagonalEdge[diagonalEdge.Count - 1].z);
                            //Debug.Log("currentEdge. first:" + currentEdge[0].x + "," + currentEdge[0].y + "," + currentEdge[0].z + ", last: " +
                            //    currentEdge[currentEdge.Count - 1].x + "," + currentEdge[currentEdge.Count - 1].y + "," + currentEdge[currentEdge.Count - 1].z);
                            //Debug.Log("edges1[i-1]. first:" + edges1[i - 1][0].x + "," + edges1[i - 1][0].y + "," + edges1[i - 1][0].z + ", last: " +
                            //    edges1[i - 1][edges1[i - 1].Count - 1].x + "," + edges1[i - 1][edges1[i - 1].Count - 1].y + "," + edges1[i - 1][edges1[i - 1].Count - 1].z);

                            placedOnPlane = SetAllBlocksInPlane(world, currentPosList, currentVertexPosList, currentEdgeList, currentPlane, lastHit, block);
							filledPosList.AddRange(placedOnPlane);
							
						} else
						{
							//Debug.Log("9 Setting edge i=" + (i) + "->j-jOffset=" + (j-jOffset));
							// Set edge i -> j-1
							List<WorldPos> diagonalEdge = SetAllBlocksBetweenPos(vertices1[i], vertices2[j-jOffset], world, lastHit, new BlockTemp());
							filledPosList.AddRange(diagonalEdge);

							//Debug.Log("10 Drawing triangle i=" + (i) + ", j-jOffset=" + (j-jOffset) +", j=" + (j));
							// Draw the triangle i, j-1, j
							Plane currentPlane = Plane.newPlaneWithPoints(p1, p3, p4);
							List<WorldPos> currentPosList = new List<WorldPos>();
							currentPosList.Add(vertices1[i]);
							currentPosList.Add(vertices2[j]);
							currentPosList.Add(vertices2[j - jOffset]);
							currentPosList.AddRange(edges2[j - jOffset]);
							currentPosList.AddRange(diagonalEdge);
							currentPosList.AddRange(currentEdge);
                            List<WorldPos> currentVertexPosList = new List<WorldPos>();
                            currentVertexPosList.Add(vertices1[i]);
                            currentVertexPosList.Add(vertices2[j]);
                            currentVertexPosList.Add(vertices2[j - jOffset]);
                            List<List<WorldPos>> currentEdgeList = new List<List<WorldPos>>();
							currentEdgeList.Add(currentEdge);
                            currentEdgeList.Add(edges2[j - jOffset]);
                            currentEdgeList.Add(diagonalEdge);
                            //Debug.Log("i: " + vertices1[i].x + "," + vertices1[i].y + "," + vertices1[i].z);
                            //Debug.Log("j: " + vertices2[j].x + "," + vertices2[j].y + "," + vertices2[j].z);
                            //Debug.Log("j - jOffset: " + vertices2[j - jOffset].x + "," + vertices2[j - jOffset].y + "," + vertices2[j - jOffset].z);
                            //Debug.Log("diagonalEdge. first:" + diagonalEdge[0].x + "," + diagonalEdge[0].y + "," + diagonalEdge[0].z + ", last: " +
                            //    diagonalEdge[diagonalEdge.Count - 1].x + "," + diagonalEdge[diagonalEdge.Count - 1].y + "," + diagonalEdge[diagonalEdge.Count - 1].z);
                            //Debug.Log("currentEdge. first:" + currentEdge[0].x + "," + currentEdge[0].y + "," + currentEdge[0].z + ", last: " +
                            //    currentEdge[currentEdge.Count - 1].x + "," + currentEdge[currentEdge.Count - 1].y + "," + currentEdge[currentEdge.Count - 1].z);
                            //Debug.Log("edges2[j - jOffset]. first:" + edges2[j - jOffset][0].x + "," + edges2[j - jOffset][0].y + "," + edges2[j - jOffset][0].z + ", last: " +
                            //    edges2[j - jOffset][edges2[j - jOffset].Count - 1].x + "," + edges2[j - jOffset][edges2[j - jOffset].Count - 1].y + "," + edges2[j - jOffset][edges2[j - jOffset].Count - 1].z);

                            List<WorldPos> placedOnPlane = SetAllBlocksInPlane(world, currentPosList, currentVertexPosList, currentEdgeList, currentPlane, lastHit, block);
							filledPosList.AddRange(placedOnPlane);
							
							//Debug.Log("11 Drawing triangle i=" + (i) + ", j-jOffset=" + (j-jOffset) +", i-1=" + (i-1));
							// Draw the triangle i, j-1, i-1
							currentPlane = Plane.newPlaneWithPoints(p1, p2, p4);
							currentPosList = new List<WorldPos>();
							currentPosList.Add(vertices1[i-1]);
							currentPosList.Add(vertices2[j-jOffset]);
							currentPosList.Add(vertices1[i]);
							currentPosList.AddRange(edges1[i-1]);
							currentPosList.AddRange(previousEdge);
							currentPosList.AddRange(diagonalEdge);
                            currentVertexPosList = new List<WorldPos>();
                            currentVertexPosList.Add(vertices1[i - 1]);
                            currentVertexPosList.Add(vertices2[j - jOffset]);
                            currentVertexPosList.Add(vertices1[i]);
                            currentEdgeList = new List<List<WorldPos>>();
							currentEdgeList.Add(previousEdge);
							currentEdgeList.Add(diagonalEdge);
                            currentEdgeList.Add(edges1[i - 1]);
                            //Debug.Log("i-1: " + vertices1[i - 1].x + "," + vertices1[i - 1].y + "," + vertices1[i - 1].z);
                            //Debug.Log("j-jOffset: " + vertices2[j - jOffset].x + "," + vertices2[j - jOffset].y + "," + vertices2[j - jOffset].z);
                            //Debug.Log("i: " + vertices1[i].x + "," + vertices1[i].y + "," + vertices1[i].z);
                            //Debug.Log("diagonalEdge. first:" + diagonalEdge[0].x + "," + diagonalEdge[0].y + "," + diagonalEdge[0].z + ", last: " +
                            //    diagonalEdge[diagonalEdge.Count - 1].x + "," + diagonalEdge[diagonalEdge.Count - 1].y + "," + diagonalEdge[diagonalEdge.Count - 1].z);
                            //Debug.Log("previousEdge. first:" + previousEdge[0].x + "," + previousEdge[0].y + "," + previousEdge[0].z + ", last: " +
                            //    previousEdge[previousEdge.Count - 1].x + "," + previousEdge[previousEdge.Count - 1].y + "," + previousEdge[previousEdge.Count - 1].z);
                            //Debug.Log("edges1[i-1]. first:" + edges1[i - 1][0].x + "," + edges1[i - 1][0].y + "," + edges1[i - 1][0].z + ", last: " +
                            //    edges1[i - 1][edges1[i - 1].Count - 1].x + "," + edges1[i - 1][edges1[i - 1].Count - 1].y + "," + edges1[i - 1][edges1[i - 1].Count - 1].z);

                            placedOnPlane = SetAllBlocksInPlane(world, currentPosList, currentVertexPosList, currentEdgeList, currentPlane, lastHit, block);
							filledPosList.AddRange(placedOnPlane);
						}
					} else 
					{
						//Debug.Log("THE TOUGH CASE");

						// Gives the sign of the j movement - not necessarily j-1
						int jSign = (j - previousVertex)/Mathf.Abs(j - previousVertex);

						// Loop over j vertices that we skipped
						List<WorldPos> previousInnerEdge = previousEdge;
						bool ambiguousCaseHandled = false;
						for (int tempj = previousVertex + jSign; tempj != j; tempj = tempj + jSign)
						{
							//Debug.Log("tempj = " + tempj);
							List<WorldPos> currentInnerEdge = new List<WorldPos>();
							// for each of these vertices, check which between vertex i and i-1 is closer, draw edge
							float distance1 = Vector3.Distance(WorldPos.VectorFromWorldPos(vertices2[tempj]), WorldPos.VectorFromWorldPos(vertices1[i-1]));
							float distance2 = Vector3.Distance(WorldPos.VectorFromWorldPos(vertices2[tempj]), WorldPos.VectorFromWorldPos(vertices1[i]));
							if (distance1 < distance2)
							{
								// Set edge tempj -> i-1
								currentInnerEdge = SetAllBlocksBetweenPos(vertices1[i-1], vertices2[tempj], world, lastHit, new BlockTemp());
								filledPosList.AddRange(currentInnerEdge);
								
								// Draw the triangle tempj-1, tempj, i-1
								Vector3 p1 = WorldPos.VectorFromWorldPos(vertices1[i-1]);
								Vector3 p2 = WorldPos.VectorFromWorldPos(vertices2[tempj]);
								Vector3 p3 = WorldPos.VectorFromWorldPos(vertices2[tempj - jSign]);
								Plane currentPlane = Plane.newPlaneWithPoints(p1, p2, p3);
								List<WorldPos> currentPosList = new List<WorldPos>();
								currentPosList.Add(vertices1[i-1]);
								currentPosList.Add(vertices2[tempj]);
								currentPosList.Add(vertices2[tempj - jSign]);
								currentPosList.AddRange(edges2[tempj - jSign]);
								currentPosList.AddRange(currentInnerEdge);
								currentPosList.AddRange(previousInnerEdge);
                                List<WorldPos> currentVertexPosList = new List<WorldPos>();
                                currentVertexPosList.Add(vertices1[i - 1]);
                                currentVertexPosList.Add(vertices2[tempj]);
                                currentVertexPosList.Add(vertices2[tempj - jSign]);
                                List<List<WorldPos>> currentEdgeList = new List<List<WorldPos>>();
                                currentEdgeList.Add(currentInnerEdge);
                                currentEdgeList.Add(edges2[tempj - jSign]);
								currentEdgeList.Add(previousInnerEdge);
                                //Debug.Log("i-1: " + vertices1[i - 1].x + "," + vertices1[i - 1].y + "," + vertices1[i - 1].z);
                                //Debug.Log("tempj: " + vertices2[tempj].x + "," + vertices2[tempj].y + "," + vertices2[tempj].z);
                                //Debug.Log("tempj - jSign: " + vertices2[tempj - jSign].x + "," + vertices2[tempj - jSign].y + "," + vertices2[tempj - jSign].z);
                                //Debug.Log("currentInnerEdge. first:" + currentInnerEdge[0].x + "," + currentInnerEdge[0].y + "," + currentInnerEdge[0].z + ", last: " +
                                //    currentInnerEdge[currentInnerEdge.Count - 1].x + "," + currentInnerEdge[currentInnerEdge.Count - 1].y + "," + currentInnerEdge[currentInnerEdge.Count - 1].z);
                                //Debug.Log("previousInnerEdge. first:" + previousInnerEdge[0].x + "," + previousInnerEdge[0].y + "," + previousInnerEdge[0].z + ", last: " +
                                //    previousInnerEdge[previousInnerEdge.Count - 1].x + "," + previousInnerEdge[previousInnerEdge.Count - 1].y + "," + previousInnerEdge[previousInnerEdge.Count - 1].z);
                                //Debug.Log("edges2[tempj - jSign]. first:" + edges2[tempj - jSign][0].x + "," + edges2[tempj - jSign][0].y + "," + edges2[tempj - jSign][0].z + ", last: " +
                                //    edges2[tempj - jSign][edges2[tempj - jSign].Count - 1].x + "," + edges2[tempj - jSign][edges2[tempj - jSign].Count - 1].y + "," + edges2[tempj - jSign][edges2[tempj - jSign].Count - 1].z);

                                List<WorldPos> placedOnPlane = SetAllBlocksInPlane(world, currentPosList, currentVertexPosList, currentEdgeList, currentPlane, lastHit, block);
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
								Vector3 p4 = WorldPos.VectorFromWorldPos(vertices2[tempj-jSign]);
								// Compares distances
								if (Vector3.Distance(p2, p3) < Vector3.Distance(p1, p4))
								{
									// Set edge i-1 -> tempj
									List<WorldPos> diagonalEdge = SetAllBlocksBetweenPos(vertices1[i-1], vertices2[tempj], world, lastHit, new BlockTemp());
									filledPosList.AddRange(diagonalEdge);

									// Draw the triangle i-1, tempj, tempj-1
									Plane currentPlane = Plane.newPlaneWithPoints(p2, p3, p4);
									List<WorldPos> currentPosList = new List<WorldPos>();
									currentPosList.Add(vertices1[i-1]);
									currentPosList.Add(vertices2[tempj]);
									currentPosList.Add(vertices2[tempj - jSign]);
									currentPosList.AddRange(edges2[tempj - jSign]);
									currentPosList.AddRange(diagonalEdge);
									currentPosList.AddRange(previousInnerEdge);
                                    List<WorldPos> currentVertexPosList = new List<WorldPos>();
                                    currentVertexPosList.Add(vertices1[i - 1]);
                                    currentVertexPosList.Add(vertices2[tempj]);
                                    currentVertexPosList.Add(vertices2[tempj - jSign]);
                                    List<List<WorldPos>> currentEdgeList = new List<List<WorldPos>>();
									currentEdgeList.Add(diagonalEdge);
                                    currentEdgeList.Add(edges2[tempj - jSign]);
                                    currentEdgeList.Add(previousInnerEdge);
                                    //Debug.Log("i-1: " + vertices1[i - 1].x + "," + vertices1[i - 1].y + "," + vertices1[i - 1].z);
                                    //Debug.Log("tempj: " + vertices2[tempj].x + "," + vertices2[tempj].y + "," + vertices2[tempj].z);
                                    //Debug.Log("tempj - jSign: " + vertices2[tempj - jSign].x + "," + vertices2[tempj - jSign].y + "," + vertices2[tempj - jSign].z);
                                    //Debug.Log("currentInnerEdge. first:" + currentInnerEdge[0].x + "," + currentInnerEdge[0].y + "," + currentInnerEdge[0].z + ", last: " +
                                    //    currentInnerEdge[currentInnerEdge.Count - 1].x + "," + currentInnerEdge[currentInnerEdge.Count - 1].y + "," + currentInnerEdge[currentInnerEdge.Count - 1].z);
                                    //Debug.Log("previousInnerEdge. first:" + previousInnerEdge[0].x + "," + previousInnerEdge[0].y + "," + previousInnerEdge[0].z + ", last: " +
                                    //    previousInnerEdge[previousInnerEdge.Count - 1].x + "," + previousInnerEdge[previousInnerEdge.Count - 1].y + "," + previousInnerEdge[previousInnerEdge.Count - 1].z);
                                    //Debug.Log("edges2[tempj - jSign]. first:" + edges2[tempj - jSign][0].x + "," + edges2[tempj - jSign][0].y + "," + edges2[tempj - jSign][0].z + ", last: " +
                                    //    edges2[tempj - jSign][edges2[tempj - jSign].Count - 1].x + "," + edges2[tempj - jSign][edges2[tempj - jSign].Count - 1].y + "," + edges2[tempj - jSign][edges2[tempj - jSign].Count - 1].z);

                                    List<WorldPos> placedOnPlane = SetAllBlocksInPlane(world, currentPosList, currentVertexPosList, currentEdgeList, currentPlane, lastHit, block);
									filledPosList.AddRange(placedOnPlane);

									// Draw the triangle i-1, tempj, i
									currentPlane = Plane.newPlaneWithPoints(p1, p2, p3);
									currentPosList = new List<WorldPos>();
									currentPosList.Add(vertices1[i-1]);
									currentPosList.Add(vertices2[tempj]);
									currentPosList.Add(vertices1[i]);
									currentPosList.AddRange(edges1[i-1]);
									currentPosList.AddRange(currentInnerEdge);
									currentPosList.AddRange(diagonalEdge);
                                    currentVertexPosList = new List<WorldPos>();
                                    currentVertexPosList.Add(vertices1[i - 1]);
                                    currentVertexPosList.Add(vertices2[tempj]);
                                    currentVertexPosList.Add(vertices1[i]);
                                    currentEdgeList = new List<List<WorldPos>>();
                                    currentEdgeList.Add(diagonalEdge);
									currentEdgeList.Add(currentInnerEdge);
                                    currentEdgeList.Add(edges1[i - 1]);
                                    //Debug.Log("i-1: " + vertices1[i - 1].x + "," + vertices1[i - 1].y + "," + vertices1[i - 1].z);
                                    //Debug.Log("tempj: " + vertices2[tempj].x + "," + vertices2[tempj].y + "," + vertices2[tempj].z);
                                    //Debug.Log("i: " + vertices1[i].x + "," + vertices1[i].y + "," + vertices1[i].z);
                                    //Debug.Log("currentInnerEdge. first:" + currentInnerEdge[0].x + "," + currentInnerEdge[0].y + "," + currentInnerEdge[0].z + ", last: " +
                                    //    currentInnerEdge[currentInnerEdge.Count - 1].x + "," + currentInnerEdge[currentInnerEdge.Count - 1].y + "," + currentInnerEdge[currentInnerEdge.Count - 1].z);
                                    //Debug.Log("diagonalEdge. first:" + diagonalEdge[0].x + "," + diagonalEdge[0].y + "," + diagonalEdge[0].z + ", last: " +
                                    //    diagonalEdge[diagonalEdge.Count - 1].x + "," + diagonalEdge[diagonalEdge.Count - 1].y + "," + diagonalEdge[diagonalEdge.Count - 1].z);
                                    //Debug.Log("edges1[i-1]. first:" + edges1[i - 1][0].x + "," + edges1[i - 1][0].y + "," + edges1[i - 1][0].z + ", last: " +
                                    //    edges1[i - 1][edges1[i - 1].Count - 1].x + "," + edges1[i - 1][edges1[i - 1].Count - 1].y + "," + edges1[i - 1][edges1[i - 1].Count - 1].z);

                                    placedOnPlane = SetAllBlocksInPlane(world, currentPosList, currentVertexPosList, currentEdgeList, currentPlane, lastHit, block);
									filledPosList.AddRange(placedOnPlane);

								} else
								{
									// Set edge i -> tempj-1
									List<WorldPos> diagonalEdge = SetAllBlocksBetweenPos(vertices1[i], vertices2[tempj-jSign], world, lastHit, new BlockTemp());
									filledPosList.AddRange(diagonalEdge);

									// Draw the triangle i, tempj-1, tempj
									Plane currentPlane = Plane.newPlaneWithPoints(p1, p3, p4);
									List<WorldPos> currentPosList = new List<WorldPos>();
									currentPosList.Add(vertices1[i]);
									currentPosList.Add(vertices2[tempj]);
									currentPosList.Add(vertices2[tempj - jSign]);
									currentPosList.AddRange(edges2[tempj - jSign]);
									currentPosList.AddRange(diagonalEdge);
									currentPosList.AddRange(currentInnerEdge);
                                    List<WorldPos> currentVertexPosList = new List<WorldPos>();
                                    currentVertexPosList.Add(vertices1[i]);
                                    currentVertexPosList.Add(vertices2[tempj]);
                                    currentVertexPosList.Add(vertices2[tempj - jSign]);
                                    List<List<WorldPos>> currentEdgeList = new List<List<WorldPos>>();
                                    currentEdgeList.Add(currentInnerEdge);
                                    currentEdgeList.Add(edges2[tempj - jSign]);
									currentEdgeList.Add(diagonalEdge);
                                    //Debug.Log("i: " + vertices1[i].x + "," + vertices1[i].y + "," + vertices1[i].z);
                                    //Debug.Log("tempj: " + vertices2[tempj].x + "," + vertices2[tempj].y + "," + vertices2[tempj].z);
                                    //Debug.Log("tempj - jSign: " + vertices2[tempj - jSign].x + "," + vertices2[tempj - jSign].y + "," + vertices2[tempj - jSign].z);
                                    //Debug.Log("currentInnerEdge. first:" + currentInnerEdge[0].x + "," + currentInnerEdge[0].y + "," + currentInnerEdge[0].z + ", last: " +
                                    //    currentInnerEdge[currentInnerEdge.Count - 1].x + "," + currentInnerEdge[currentInnerEdge.Count - 1].y + "," + currentInnerEdge[currentInnerEdge.Count - 1].z);
                                    //Debug.Log("diagonalEdge. first:" + diagonalEdge[0].x + "," + diagonalEdge[0].y + "," + diagonalEdge[0].z + ", last: " +
                                    //    diagonalEdge[diagonalEdge.Count - 1].x + "," + diagonalEdge[diagonalEdge.Count - 1].y + "," + diagonalEdge[diagonalEdge.Count - 1].z);
                                    //Debug.Log("edges2[tempj - jSign]. first:" + edges2[tempj - jSign][0].x + "," + edges2[tempj - jSign][0].y + "," + edges2[tempj - jSign][0].z + ", last: " +
                                    //    edges2[tempj - jSign][edges2[tempj - jSign].Count - 1].x + "," + edges2[tempj - jSign][edges2[tempj - jSign].Count - 1].y + "," + edges2[tempj - jSign][edges2[tempj - jSign].Count - 1].z);

                                    List<WorldPos> placedOnPlane = SetAllBlocksInPlane(world, currentPosList, currentVertexPosList, currentEdgeList, currentPlane, lastHit, block);
									filledPosList.AddRange(placedOnPlane);

									// Draw the triangle i, tempj-1, i-1
									currentPlane = Plane.newPlaneWithPoints(p1, p2, p4);
									currentPosList = new List<WorldPos>();
									currentPosList.Add(vertices1[i-1]);
									currentPosList.Add(vertices2[tempj-jSign]);
									currentPosList.Add(vertices1[i]);
									currentPosList.AddRange(edges1[i-1]);
									currentPosList.AddRange(previousInnerEdge);
									currentPosList.AddRange(diagonalEdge);
                                    currentVertexPosList = new List<WorldPos>();
                                    currentVertexPosList.Add(vertices1[i - 1]);
                                    currentVertexPosList.Add(vertices2[tempj - jSign]);
                                    currentVertexPosList.Add(vertices1[i]);
                                    currentEdgeList = new List<List<WorldPos>>();
									currentEdgeList.Add(previousInnerEdge);
									currentEdgeList.Add(diagonalEdge);
                                    currentEdgeList.Add(edges1[i - 1]);
                                    //Debug.Log("i-1: " + vertices1[i - 1].x + "," + vertices1[i - 1].y + "," + vertices1[i - 1].z);
                                    //Debug.Log("tempj - jSign: " + vertices2[tempj - jSign].x + "," + vertices2[tempj - jSign].y + "," + vertices2[tempj - jSign].z);
                                    //Debug.Log("i: " + vertices1[i].x + "," + vertices1[i].y + "," + vertices1[i].z);
                                    //Debug.Log("previousInnerEdge. first:" + previousInnerEdge[0].x + "," + previousInnerEdge[0].y + "," + previousInnerEdge[0].z + ", last: " +
                                    //    previousInnerEdge[previousInnerEdge.Count - 1].x + "," + previousInnerEdge[previousInnerEdge.Count - 1].y + "," + previousInnerEdge[previousInnerEdge.Count - 1].z);
                                    //Debug.Log("diagonalEdge. first:" + diagonalEdge[0].x + "," + diagonalEdge[0].y + "," + diagonalEdge[0].z + ", last: " +
                                    //    diagonalEdge[diagonalEdge.Count - 1].x + "," + diagonalEdge[diagonalEdge.Count - 1].y + "," + diagonalEdge[diagonalEdge.Count - 1].z);
                                    //Debug.Log("edges1[i-1]. first:" + edges1[i - 1][0].x + "," + edges1[i - 1][0].y + "," + edges1[i - 1][0].z + ", last: " +
                                    //    edges1[i - 1][edges1[i - 1].Count - 1].x + "," + edges1[i - 1][edges1[i - 1].Count - 1].y + "," + edges1[i - 1][edges1[i - 1].Count - 1].z);

                                    placedOnPlane = SetAllBlocksInPlane(world, currentPosList, currentVertexPosList, currentEdgeList, currentPlane, lastHit, block);
									filledPosList.AddRange(placedOnPlane);
								}

								ambiguousCaseHandled = true;
							} else {
								// Set edge tempj -> i
								currentInnerEdge = SetAllBlocksBetweenPos(vertices1[i], vertices2[tempj], world, lastHit, new BlockTemp());
								filledPosList.AddRange(currentInnerEdge);
								
								// Draw the triangle tempj-1, tempj, i
								Vector3 p1 = WorldPos.VectorFromWorldPos(vertices1[i]);
								Vector3 p2 = WorldPos.VectorFromWorldPos(vertices2[tempj]);
								Vector3 p3 = WorldPos.VectorFromWorldPos(vertices2[tempj - jSign]);
								Plane currentPlane = Plane.newPlaneWithPoints(p1, p2, p3);
								List<WorldPos> currentPosList = new List<WorldPos>();
								currentPosList.Add(vertices1[i]);
								currentPosList.Add(vertices2[tempj]);
								currentPosList.Add(vertices2[tempj - jSign]);
                                currentPosList.AddRange(edges2[tempj - jSign]);
                                currentPosList.AddRange(currentInnerEdge);
                                currentPosList.AddRange(previousInnerEdge);
                                List<WorldPos> currentVertexPosList = new List<WorldPos>();
                                currentVertexPosList.Add(vertices1[i]);
                                currentVertexPosList.Add(vertices2[tempj]);
                                currentVertexPosList.Add(vertices2[tempj - jSign]);
                                List<List<WorldPos>> currentEdgeList = new List<List<WorldPos>>();
                                currentEdgeList.Add(currentInnerEdge);
                                currentEdgeList.Add(edges2[tempj - jSign]);
								currentEdgeList.Add(previousInnerEdge);
                                //Debug.Log("i: " + vertices1[i].x + "," + vertices1[i].y + "," + vertices1[i].z);
                                //Debug.Log("tempj: " + vertices2[tempj].x + "," + vertices2[tempj].y + "," + vertices2[tempj].z);
                                //Debug.Log("tempj - jSign: " + vertices2[tempj - jSign].x + "," + vertices2[tempj - jSign].y + "," + vertices2[tempj - jSign].z);
                                //Debug.Log("currentInnerEdge. first:" + currentInnerEdge[0].x + "," + currentInnerEdge[0].y + "," + currentInnerEdge[0].z + ", last: " +
                                //    currentInnerEdge[currentInnerEdge.Count - 1].x + "," + currentInnerEdge[currentInnerEdge.Count - 1].y + "," + currentInnerEdge[currentInnerEdge.Count - 1].z);
                                //Debug.Log("previousInnerEdge. first:" + previousInnerEdge[0].x + "," + previousInnerEdge[0].y + "," + previousInnerEdge[0].z + ", last: " +
                                //    previousInnerEdge[previousInnerEdge.Count - 1].x + "," + previousInnerEdge[previousInnerEdge.Count - 1].y + "," + previousInnerEdge[previousInnerEdge.Count - 1].z);
                                //Debug.Log("edges2[tempj - jSign]. first:" + edges2[tempj - jSign][0].x + "," + edges2[tempj - jSign][0].y + "," + edges2[tempj - jSign][0].z + ", last: " +
                                //    edges2[tempj - jSign][edges2[tempj - jSign].Count - 1].x + "," + edges2[tempj - jSign][edges2[tempj - jSign].Count - 1].y + "," + edges2[tempj - jSign][edges2[tempj - jSign].Count - 1].z);

                                List<WorldPos> placedOnPlane = SetAllBlocksInPlane(world, currentPosList, currentVertexPosList, currentEdgeList, currentPlane, lastHit, block);
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
			Vector3 p = WorldPos.VectorFromWorldPos(pos);
			p = q * p;
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