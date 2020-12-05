using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlantRenderer : MonoBehaviour
{
    [Tooltip("Stem object should be pointing in forward direction with length of 1 unit")]
    public GameObject stem;
    [Tooltip("Leaf object should be pointing in forward direction with length of ~1 unit")]
    public GameObject leaf;

    // It's turtles, all the way down.
    private Stack<Turtle3D> turtles = new Stack<Turtle3D>();

    // Start my turtle with forward as up and up as back so we make a vertical tree.
    Turtle3D topTurtle = new Turtle3D(Vector3.zero, Quaternion.LookRotation(Vector3.up, Vector3.back), Vector3.one);

    // Start is called before the first frame update
    void Start()
    {
        LSystem seed = new LSystem("FA");
        seed.AddRule("A", "![&FA!![^L]]/'[&FA!![^L]]/'[&FA!![^L]]");
        seed.AddRule("F(x)", "F(x*1.2)");
        seed.AddRule("F", "F(.95)/S");
        seed.AddRule("^^^<L>]", "q");
        seed.AddRule("S", "F[^L]");
        seed.AddRule("L", "^^L");
        for(int i = 0; i < 5; i++)
        {
            seed.Grow();
            TreeBuilder(seed.ToString());
            topTurtle.Set(Vector3.zero, Quaternion.LookRotation(Vector3.up, Vector3.back), Vector3.one);
            this.transform.position = new Vector3(0, 0, 5.0f*(i+1));
            //Debug.Log(seed.ToString());
        }
        //TreeBuilder(seed.ToString());
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /// <summary>
    /// converts an L-System in to a collection of instantiations of objects using a stack of Turtle3D objects
    /// Renderable characters are:
    /// F - Draw Stem at current scale
    /// F(x) - Draw stem of width (x)
    /// L - Draw Leaf at current scale
    /// </summary>
    /// <param name="lsystem">string containing a parameterized and/or bracketed L-System</param>

    void TreeBuilder(string lsystem)
    {
        turtles.Push(topTurtle);
        for (int i = 0; i < lsystem.Length; i++)
        {
            //
            float angle = 25f;
            // Process the commands, many of them just set values on the top turtle of the stack
            switch (lsystem[i])
            {
                case 'F': // Move forward & draw
                    GameObject segment = Instantiate(stem);
                    // If this is parameterized, we draw the stem with a scale
                    // factor in the non-Z directions: aka thickness of the stem
                    if (lsystem[i + 1] == '(')
                    {
                        string foo = lsystem.Substring(i + 2);
                        foo = foo.Substring(0, foo.IndexOf(')'));
                        float thickness = float.Parse(foo);
                        i += foo.Length;
                        DrawObject(segment, this.transform, new Vector3(thickness * topTurtle.scale.x, thickness * topTurtle.scale.y, topTurtle.scale.z));
                        topTurtle.Move();
                    }
                    else
                    {
                        MoveDraw(segment, this.transform);
                    }
                    break;
                case 'L':
                    DrawObject(Instantiate(leaf), this.transform);
                    break;
                case 'f': // Just move forward, no drawing
                    topTurtle.Move();
                    break;
                case '+': // Rotate Right
                    if(lsystem[i+1] == '(')
                    {
                        string foo = lsystem.Substring(i + 2);
                        foo = foo.Substring(0, foo.IndexOf(')'));
                        angle = float.Parse(foo);
                        i += foo.Length;
                    }
                    topTurtle.Turn(Quaternion.AngleAxis(angle, Vector3.up));
                    break;
                case '-': //Rotate Left
                    if (lsystem[i + 1] == '(')
                    {
                        string foo = lsystem.Substring(i + 2);
                        foo = foo.Substring(0, foo.IndexOf(')'));
                        angle = float.Parse(foo);
                        i += foo.Length;
                    }
                    topTurtle.Turn(Quaternion.AngleAxis(-angle, Vector3.up));
                    break;
                case '&': //Pitch down
                    if (lsystem[i + 1] == '(')
                    {
                        string foo = lsystem.Substring(i + 2);
                        foo = foo.Substring(0, foo.IndexOf(')'));
                        angle = float.Parse(foo);
                        i += foo.Length;
                    }
                    topTurtle.Turn(Quaternion.AngleAxis(angle, Vector3.right));
                    break;
                case '^': // Pitch up
                    if (lsystem[i + 1] == '(')
                    {
                        string foo = lsystem.Substring(i + 2);
                        foo = foo.Substring(0, foo.IndexOf(')'));
                        angle = float.Parse(foo);
                        i += foo.Length;
                    }
                    topTurtle.Turn(Quaternion.AngleAxis(-angle, Vector3.right));
                    break;
                case '\\': //Roll left
                    if (lsystem[i + 1] == '(')
                    {
                        string foo = lsystem.Substring(i + 2);
                        foo = foo.Substring(0, foo.IndexOf(')'));
                        angle = float.Parse(foo);
                        i += foo.Length;
                    }
                    topTurtle.Turn(Quaternion.AngleAxis(-137.5f, Vector3.forward));
                    break;
                case '/': // Roll right
                    if (lsystem[i + 1] == '(')
                    {
                        string foo = lsystem.Substring(i + 2);
                        foo = foo.Substring(0, foo.IndexOf(')'));
                        angle = float.Parse(foo);
                        i += foo.Length;
                    }
                    topTurtle.Turn(Quaternion.AngleAxis(137.5f, Vector3.forward));
                    break;
                case '[': //Push stack
                    turtles.Push(new Turtle3D(topTurtle));
                    break;
                case ']': //Pop stack
                    topTurtle = turtles.Pop();
                    break;
                case '!': // Shrink scale of turtle and all children
                    topTurtle.scale *= .85f;
                    break;
                case '\'': // Color?
                    break;
            }
        }
        topTurtle = turtles.Pop();
        Debug.Log(turtles.Count);
    }
    // stolen from turtle
    public void MoveDraw(GameObject renderObject, Transform parent)
    {
        DrawObject(renderObject, parent);
        topTurtle.Move();
    }
    // Place the object into the parent with the transformation from the turtle including scale
    public void DrawObject(GameObject renderObject, Transform parent)
    {
        DrawObject(renderObject, parent, topTurtle.scale);
    }
    // Draw Object accpets an optional scale factor which ignores the parent's scale when drawing (but still retains the value for movement)
    public void DrawObject(GameObject renderObject, Transform parent, Vector3 atScale)
    {
        renderObject.transform.parent = parent;
        renderObject.transform.SetPositionAndRotation(topTurtle.position, topTurtle.orientation);
        renderObject.transform.localScale = atScale;
    }
}
