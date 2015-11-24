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
		Debug.Log("A: " + A + ", B: " + B + ", C: " + C + ", D: " + D);

		// Pretty easy actually - in theory, all you do is the regular scan line algorithm over two coordinates
		//  Then, over last coordinate, which ideally you have the least variation over, you evaluate the point
		//  based on the other two, say x and y, and then fill at that point

		// 3 Cases - if Least variation is in x, y or z

		if (Mathf.Abs(C) > Mathf.Abs(A) && Mathf.Abs(C) > Mathf.Abs(B)) // z
		{
			Debug.Log("Case 1: x-y");
			// Get ymin, ymax
			posList.Sort(SortByY);
			int ymin = posList[0].y;
			int ymax = posList[posList.Count-1].y;
			Debug.Log("ymin: " + ymin);
			Debug.Log("ymax: " + ymax);
			
			// For each y, get all points that intersect scan line
			List<WorldPos> scanIntersection;
			for (int y = ymin + 1; y < ymax; y++)
			{
				Debug.Log("y = " + y);
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
							Debug.Log("Added: " + pos.x + "," + pos.y + "," + pos.z);
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
					Debug.Log("xmin: " + xmin);
					Debug.Log("xmax: " + xmax);
					
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
							Debug.Log("Placed: " + x + "," + y + "," + z + "/" + Mathf.RoundToInt(z));
						}
					}
				}
			}
		} else if (Mathf.Abs(B) > Mathf.Abs(A) && Mathf.Abs(B) > Mathf.Abs(C)) // y
		{
			Debug.Log("Case 2: x-z");
			// Get zmin, zmax
			posList.Sort(SortByZ);
			int zmin = posList[0].z;
			int zmax = posList[posList.Count-1].z;
			Debug.Log("zmin: " + zmin);
			Debug.Log("zmax: " + zmax);
			
			// For each z, get all points that intersect scan line
			List<WorldPos> scanIntersection;
			for (int z = zmin + 1; z < zmax; z++)
			{
				Debug.Log("z = " + z);
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
							Debug.Log("Added: " + pos.x + "," + pos.y + "," + pos.z);
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
					Debug.Log("xmin: " + xmin);
					Debug.Log("xmax: " + xmax);
					
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
							Debug.Log("Placed: " + x + "," + y + "/" + Mathf.RoundToInt(y) + "," + z);
						}
					}
				}
			}
		} else // (A > B && A > C) ie. x
		{
			Debug.Log("Case 3: y-z");
			// Get zmin, zmax
			posList.Sort(SortByZ);
			int zmin = posList[0].z;
			int zmax = posList[posList.Count-1].z;
			Debug.Log("zmin: " + zmin);
			Debug.Log("zmax: " + zmax);
			
			// For each z, get all points that intersect scan line
			List<WorldPos> scanIntersection;
			for (int z = zmin + 1; z < zmax; z++)
			{
				Debug.Log("z = " + z);
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
							Debug.Log("Added: " + pos.x + "," + pos.y + "," + pos.z);
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
					Debug.Log("ymin: " + ymin);
					Debug.Log("ymax: " + ymax);
					
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
							Debug.Log("Placed: " + Mathf.RoundToInt(x) + "/" + x + "," + y + "," + z);
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
}