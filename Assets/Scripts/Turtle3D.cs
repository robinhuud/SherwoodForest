using System;
using UnityEngine;

public class Turtle3D
{
    // Turtle's position in space
    public Vector3 position;
    // Turtle's rotation from it's starting forward direction
    public Quaternion orientation;

    public Turtle3D(Vector3 position, Quaternion orientation)
    {
        this.position = position;
        this.orientation = orientation;
    }
    public void Turn(Quaternion rotation)
    {
        this.orientation *= rotation;
    }
    public void Draw(GameObject renderObject, Transform parent)
    {
        renderObject.transform.SetPositionAndRotation(position, orientation);
        renderObject.transform.parent = parent;
        this.position += orientation * Vector3.forward;
    }
}
