using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// L-System from

public class LSystem : MonoBehaviour
{
    public string axiom = "X";
    public GameObject stem;
    //public GameObject leaf;

    private Dictionary<string, string> rules = new Dictionary<string, string>();
    private string tree = "";
    
    // Start is called before the first frame update
    void Start()
    {
        rules.Add("X", "F+[[X]-X]-F[-FX]+X");
        rules.Add("F", "FF");
        tree = axiom;
        for(int i = 0; i < 3; i++)
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
        Turtle3D topTurtle = new Turtle3D(Vector3.zero, Quaternion.LookRotation(Vector3.up, Vector3.back));
        turtles.Push(topTurtle);
        for (int i = 0; i < lsystem.Length; i++)
        {

            switch (lsystem[i])
            {
                case 'F': // Move forward & draw
                    topTurtle.Draw(Instantiate(stem), this.transform);
                    break;
                case '+': // Rotate Right
                    topTurtle.Turn(Quaternion.AngleAxis(25f, Vector3.up));
                    break;
                case '-': //Rotate Left
                    topTurtle.Turn(Quaternion.AngleAxis(-25f, Vector3.up));
                    break;
                case '[': //Push stack
                    turtles.Push(new Turtle3D(topTurtle.position, topTurtle.orientation));
                    break;
                case ']': //Pop stack
                    topTurtle = turtles.Pop();
                    break;
            }
        }
    }
}
