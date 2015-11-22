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
        Debug.Log("In Function");
        Chunk chunk = lastHit.collider.GetComponent<Chunk>();
        if (chunk == null)
            return false;
        Debug.Log("Passed null chunk test");
        Debug.Log(posList.Count);

        foreach (WorldPos pos in posList)
        {
            Debug.Log(pos);
            chunk.world.SetBlock(pos.x, pos.y, pos.z, block);
        }
        return true;
    }
}