using UnityEngine;
using System.Collections;
using System;

[Serializable]
public struct WorldPos
{
    public int x, y, z;

    public WorldPos(int x, int y, int z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 47;

            hash = hash * 227 + x.GetHashCode();
            hash = hash * 227 + y.GetHashCode();
            hash = hash * 227 + z.GetHashCode();

            return hash;
        }
    }

    public override bool Equals(object obj)
    {
        if (GetHashCode() == obj.GetHashCode())
            return true;
        return false;
    }

    public WorldPos Add(WorldPos pos)
    {
        return new WorldPos(x + pos.x, y + pos.y, z + pos.z);
    }

    public WorldPos Subtract(WorldPos pos)
    {
        return new WorldPos(x - pos.x, y - pos.y, z - pos.z);
    }

    public static Vector3 VectorFromWorldPos(WorldPos pos)
    {
		return new Vector3((float)pos.x, (float)pos.y, (float)pos.z);
    }

    public static WorldPos WorldPosFromVector(Vector3 vector)
    {
        return new WorldPos(Mathf.RoundToInt(vector.x), Mathf.RoundToInt(vector.y), Mathf.RoundToInt(vector.z));
    }
}