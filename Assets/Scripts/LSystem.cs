using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// L-System from

public class LSystem : MonoBehaviour
{
    public string axiom = "A";
    public GameObject stem;
    public GameObject leaf;

    private Dictionary<string, string> rules = new Dictionary<string, string>();
    private string tree = "";
    
    // Start is called before the first frame update
    void Start()
    {
        tree = axiom;
        rules.Add("X", "F+[[X]-X]-F[^FXL]^X");
        rules.Add("F", "FF");
        for (int i = 0; i < 2; i++)
        {
            tree = grow(tree, rules);
        }
        Debug.Log("Final Tree: " + tree);
        NewBuilder(tree);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    string grow(string seed, Dictionary<string, string> ruleDict)
    {
        string newTree = "";
        bool found;
        for(int i = 0; i < seed.Length; i++)
        {
            string c = "" + seed[i];
            found = false;
            foreach(KeyValuePair<string,string> rule in ruleDict)
            {
                if(c.Equals(rule.Key))
                {
                    newTree += rule.Value;
                    found = true;
                    break; // found our rule, on to the next token
                }
            }
            if(!found)
            {
                newTree += c;
            }
        }
        return newTree;
    }

    // It's stacking turtles, all the way down.
    void NewBuilder(string lsystem)
    {
        Stack<Turtle3D> turtles = new Stack<Turtle3D>();
        // Start my turtle with forward as up and up as back so we make a vertical tree.
        Turtle3D topTurtle = new Turtle3D(Vector3.zero, Quaternion.LookRotation(Vector3.up, Vector3.back), Vector3.one);
        turtles.Push(topTurtle);
        for (int i = 0; i < lsystem.Length; i++)
        {

            switch (lsystem[i])
            {
                case 'F': // Move forward & draw
                    topTurtle.DrawStem(Instantiate(stem), this.transform);
                    topTurtle.scale *= .95f;
                    break;
                case 'f': // Just move forward, no drawing
                    topTurtle.Move();
                    break;
                case 'L':
                    topTurtle.DrawLeaf(Instantiate(leaf), this.transform);
                    break;
                case '+': // Rotate Right
                    topTurtle.Turn(Quaternion.AngleAxis(25f, Vector3.up));
                    break;
                case '-': //Rotate Left
                    topTurtle.Turn(Quaternion.AngleAxis(-25f, Vector3.up));
                    break;
                case '&': //Pitch down
                    topTurtle.Turn(Quaternion.AngleAxis(25f, Vector3.right));
                    break;
                case '^': // Pitch up
                    topTurtle.Turn(Quaternion.AngleAxis(-25f, Vector3.right));
                    break;
                case '\\': //Roll left
                    topTurtle.Turn(Quaternion.AngleAxis(-25f, Vector3.forward));
                    break;
                case '/': // Roll right
                    topTurtle.Turn(Quaternion.AngleAxis(25f, Vector3.forward));
                    break;
                case '[': //Push stack
                    turtles.Push(new Turtle3D(topTurtle.position, topTurtle.orientation, topTurtle.scale));
                    topTurtle.scale *= .85f;
                    break;
                case ']': //Pop stack
                    topTurtle = turtles.Pop();
                    break;
            }
        }
    }
}
