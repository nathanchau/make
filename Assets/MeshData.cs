﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MeshData
{
    public List<Vector3> vertices = new List<Vector3>();
    public List<int> triangles = new List<int>();
    // Used for temp blocks
    public List<int> tempTriangles = new List<int>();
    public List<int> tempVertexTriangles = new List<int>();
    public List<Vector2> uv = new List<Vector2>();

    public List<Vector3> colVertices = new List<Vector3>();
    public List<int> colTriangles = new List<int>();

    public bool useRenderDataForCol;

    public MeshData() { }

    public void AddQuadTriangles()
    {
        triangles.Add(vertices.Count - 4);
        triangles.Add(vertices.Count - 3);
        triangles.Add(vertices.Count - 2);

        triangles.Add(vertices.Count - 4);
        triangles.Add(vertices.Count - 2);
        triangles.Add(vertices.Count - 1);

        if (useRenderDataForCol)
        {
            colTriangles.Add(colVertices.Count - 4);
            colTriangles.Add(colVertices.Count - 3);
            colTriangles.Add(colVertices.Count - 2);
            colTriangles.Add(colVertices.Count - 4);
            colTriangles.Add(colVertices.Count - 2);
            colTriangles.Add(colVertices.Count - 1);
        }
    }

    public void AddQuadTempTriangles()
    {
        tempTriangles.Add(vertices.Count - 4);
        tempTriangles.Add(vertices.Count - 3);
        tempTriangles.Add(vertices.Count - 2);

        tempTriangles.Add(vertices.Count - 4);
        tempTriangles.Add(vertices.Count - 2);
        tempTriangles.Add(vertices.Count - 1);

        if (useRenderDataForCol)
        {
            colTriangles.Add(colVertices.Count - 4);
            colTriangles.Add(colVertices.Count - 3);
            colTriangles.Add(colVertices.Count - 2);
            colTriangles.Add(colVertices.Count - 4);
            colTriangles.Add(colVertices.Count - 2);
            colTriangles.Add(colVertices.Count - 1);
        }
    }

    public void AddQuadTempVertexTriangles()
    {
        tempVertexTriangles.Add(vertices.Count - 4);
        tempVertexTriangles.Add(vertices.Count - 3);
        tempVertexTriangles.Add(vertices.Count - 2);

        tempVertexTriangles.Add(vertices.Count - 4);
        tempVertexTriangles.Add(vertices.Count - 2);
        tempVertexTriangles.Add(vertices.Count - 1);

        if (useRenderDataForCol)
        {
            colTriangles.Add(colVertices.Count - 4);
            colTriangles.Add(colVertices.Count - 3);
            colTriangles.Add(colVertices.Count - 2);
            colTriangles.Add(colVertices.Count - 4);
            colTriangles.Add(colVertices.Count - 2);
            colTriangles.Add(colVertices.Count - 1);
        }
    }

    public void AddVertex(Vector3 vertex)
    {
        vertices.Add(vertex);

        if (useRenderDataForCol)
        {
            colVertices.Add(vertex);
        }

    }

    public void AddTriangle(int tri)
    {
        triangles.Add(tri);

        if (useRenderDataForCol)
        {
            colTriangles.Add(tri - (vertices.Count - colVertices.Count));
        }
    }
}