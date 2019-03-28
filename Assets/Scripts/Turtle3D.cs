using System;
using UnityEngine;

// Simple 3D Turtle class for moving around and placing objects in 3D space
public class Turtle3D
{
    // Turtle's current position in space
    public Vector3 position;
    // Turtle's current rotation from it's starting forward direction
    public Quaternion orientation;
    // Turtle's scale (affects scale of objects placed as well as distance moved)
    public Vector3 scale;

    public Turtle3D(Vector3 position, Quaternion orientation, Vector3 scale)
    {
        this.position = position;
        this.orientation = orientation;
        this.scale = scale;
    }
    public void Turn(Quaternion rotation)
    {
        this.orientation *= rotation;
    }
    public void DrawStem(GameObject renderObject, Transform parent)
    {
        DrawLeaf(renderObject, parent);
        Move();
    }
    public void DrawLeaf(GameObject renderObject, Transform parent)
    {
        renderObject.transform.SetPositionAndRotation(position, orientation);
        renderObject.transform.localScale = scale;
        renderObject.transform.parent = parent;
    }
    public void Move()
    {
        this.position += orientation * Vector3.Scale(Vector3.forward, this.scale);
    }
}
