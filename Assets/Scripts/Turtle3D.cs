using UnityEngine;

// Simple 3D Turtle class for moving around in 3D space
public class Turtle3D
{
    // Turtle's current position in space
    public Vector3 position;
    // Turtle's current rotation from it's starting forward direction
    public Quaternion orientation;
    // Turtle's scale (affects scale of objects placed as well as distance moved)
    public Vector3 scale;

    // default constructor for the Quaternion-challenged :D
    public Turtle3D() 
    {
        this.position = Vector3.zero;
        this.orientation = Quaternion.LookRotation(Vector3.forward);
        this.scale = Vector3.one;
    }
    // for if you want to do your own thing
    public Turtle3D(Vector3 position, Quaternion orientation, Vector3 scale)
    {
        this.position = position;
        this.orientation = orientation;
        this.scale = scale;
    }
    // for if you want to copy somebody else
    public Turtle3D(Turtle3D copy)
    {
        this.position = copy.position;
        this.orientation = copy.orientation;
        this.scale = copy.scale;
    }
    // Change the orientation of the turtle relative to it's forward direction
    public void Turn(Quaternion rotation) 
    {
        this.orientation *= rotation;
    }
    // Move Forward 1 unit in local space
    public void Move()
    {
        this.position += orientation * Vector3.Scale(Vector3.forward, this.scale);
    }
}
