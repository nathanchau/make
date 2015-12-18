using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class WorldState {
    // Data structure to contain the state of world at one point in time
    // World is made up of Shapes at different WorldPos

    // Variables
    // Hash - Keys are WorldPos, Values are list of Shapes that lie at that point
    private Dictionary<WorldPos, List<Shape>> posDictionary = new Dictionary<WorldPos, List<Shape>>();

    // List of all shapes
    private List<Shape> shapes = new List<Shape>();

    // Links to previous and next state
    // Both are allowed to be null - if so, that just indicates end of branch
    public WorldState previousWorldState;
    public WorldState nextWorldState;

    public WorldState()
    {

    }

    // Takes a shape and stores in world state
    public void storeShape(Shape shape)
    {
        // Take shape and add it to list of shapes
        shapes.Add(shape);

        // Add every WorldPos as either new entry or add to List
        List<WorldPos> posList = new List<WorldPos>();
        foreach (List<WorldPos> tempPosList in shape.posList)
        {
            posList.AddRange(tempPosList);
        }
        foreach (Plane plane in shape.planes)
        {
            posList.AddRange(plane.fillPosList);
            posList.AddRange(plane.loftFillPosList);
        }
        foreach(WorldPos pos in posList)
        {
            if (posDictionary.ContainsKey(pos))
            {
                posDictionary[pos].Add(shape);
            }
            else
            {
                posDictionary.Add(pos, new List<Shape>());
                posDictionary[pos].Add(shape);
            }
        }
    }

    public void removeShape(Shape shape)
    {
        // Remove shape from list of shapes
        shapes.Remove(shape);

        // Remove every reference in dictionary
        List<WorldPos> posList = new List<WorldPos>();
        foreach (List<WorldPos> tempPosList in shape.posList)
        {
            posList.AddRange(tempPosList);
        }
        foreach (Plane plane in shape.planes)
        {
            posList.AddRange(plane.fillPosList);
            posList.AddRange(plane.loftFillPosList);
        }
        foreach (WorldPos pos in posList)
        {
            if (posDictionary.ContainsKey(pos))
            {
                posDictionary[pos].Remove(shape);
            }
        }
    }

    // Takes a WorldPos and answers with list of shapes at that position
    public List<Shape> shapesAtPos(WorldPos pos)
    {
        if (posDictionary.ContainsKey(pos))
        {
            return posDictionary[pos];
        }
        return new List<Shape>();
    }
}
