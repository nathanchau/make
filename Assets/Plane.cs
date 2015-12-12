using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Plane
{
    // We'll use the form ax + by + cz + d = 0
    public Vector3 normal;
    public float offset;
    
	public List<WorldPos> vertexPosList = new List<WorldPos>(); // List of vertices used for current polygon
	public List<List<WorldPos>> edgeList = new List<List<WorldPos>>(); // List of edges used for current polygon
	public List<WorldPos> fillPosList = new List<WorldPos>(); // List of positions that have been added for polygon fill

	// Loft from this plane to previous
	public List<WorldPos> loftFillPosList = new List<WorldPos>(); // List of positions that have been added for all polygon fills for loft

    public Plane()
    {

    }

    public static Plane newPlaneWithPoints(Vector3 p1, Vector3 p2, Vector3 p3)
    {
        Plane newPlane = new Plane();
        // Create 2 vectors
        Vector3 v1 = p2 - p1;
        Vector3 v2 = p3 - p1;
        // Take the cross product
        Vector3 crossProduct = Vector3.Cross(v1, v2).normalized;
        newPlane.normal = crossProduct;
        // Find the offset by plugging p1 in
        float newOffset = -(crossProduct.x * p1.x + crossProduct.y * p1.y + crossProduct.z * p1.z);
        newPlane.offset = newOffset;
        return newPlane;
    }

	public void calculatePlaneVariables()
	{
		Vector3 p1 = WorldPos.VectorFromWorldPos(vertexPosList[0]);
		Vector3 p2 = WorldPos.VectorFromWorldPos(vertexPosList[1]);
		Vector3 p3 = WorldPos.VectorFromWorldPos(vertexPosList[2]);
		// Create 2 vectors
		Vector3 v1 = p2 - p1;
		Vector3 v2 = p3 - p1;
		// Take the cross product
		Vector3 crossProduct = Vector3.Cross(v1, v2).normalized;
		normal = crossProduct;
		// Find the offset by plugging p1 in
		offset = -(crossProduct.x * p1.x + crossProduct.y * p1.y + crossProduct.z * p1.z);
	}

    public static bool isCoplanar(Plane plane, Vector3 point)
    {
        // Note, I know that this implementation won't return exactly coplanar results, but it seems pretty 
        //  reasonable all things considered
        Vector3 normal = plane.normal;
        float numerator = Mathf.Abs(normal.x * point.x + normal.y * point.y + normal.z * point.z + plane.offset);
        float denominator = Mathf.Sqrt(Mathf.Pow(normal.x, 2) + Mathf.Pow(normal.y, 2) + Mathf.Pow(normal.z, 2));

        float distance = numerator / denominator;

        // Check if distance is less than maximum distance from center of cube = 0.866
        if (distance <= 0.866)
            return true;

        return false;
    }
}
