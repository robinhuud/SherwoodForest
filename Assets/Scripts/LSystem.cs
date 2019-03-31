using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Data;
using UnityEngine;

// L-System based on the paper http://algorithmicbotany.org/papers/lsfp.pdf by Przemyslaw Prusinkiewicz and James Hanan
// I wrote a version of this in stereo 3D for my SGI workstation back in the 1990's and I was inspired to
// re-create it in unity and C#
// It takes an "axiom" and a set of "rules" and uses them to build a string that represents the branching structure
// of a virtual plant. This structure is then interpreted by a 3D "turtle" a-la-LOGO but with a Push/Pop transformation stack
// rules can be simple single-character replacement like the rule "C"->"CC" which will substitute every "C" in the input
// with an "CC" in the output, with unmatched characters preserved. so the input string "ABCDE" becomes "ABCCDE"
// This version also supports context-sensitive rules of the form "A<B>C"->"BB" which will only substitute "BB" for "B" if it
// is between "A" and "C" in that order, so the input string "ABCBAB" becomes "ABBCBAB"
// if both of the above rules are applied to the string "ABCDE" it becomes "ABBCCDE"
// by convention context-sensitive rules should always be specified before context-free rules and should always
// have higher priority.
// Future versions will support parameterized L-Systems and stochastic L-Systems

public class LSystem : MonoBehaviour
{
    [Tooltip("Stem object should be pointing in forward direction with length of 1 unit")]
    public GameObject stem;
    [Tooltip("Leaf object should be pointing in forward direction with length of ~1 unit")]
    public GameObject leaf;

    // The "seed" or axiom of the tree determines the initial shape
    private string axiom = "A";

    // This is a list of characters that the context rule matching algorithm ignores when matching
    // to allow a context-sensitive L-System to ignore drawing commands and focus on the structure
    //private string ignoreString = "F+-";
    private string ignoreString = "F-+";

    // Number of iterations to run this before displaying.
    private int numIterations = 6;

    // The list of rules as a dictionary add rules using the command:
    // rules.Add(predicate, production), eg. rules.Add("A", "AFA");
    private Dictionary<string, string> rules = new Dictionary<string, string>();

    // Global data table to turn strings into math results
    private DataTable calculator = new DataTable();
    
    // Start is called before the first frame update
    void Start()
    {
        string tree = axiom;
        rules.Add("A", "[&FA![L]]//'[&FA![L]]///'[&FA![L]]");
        rules.Add("F", "S/!F");
        rules.Add("S", "F[L]");
        rules.Add("L", "^L");
        for (int i = 0; i < numIterations; i++)
        {
            tree = TreeGrower(tree, rules);
        }
        Debug.Log("Final Tree: " + tree);
        TreeBuilder(tree);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // Strips out unwanted characters (from ignoreList) from the context for context matching
    // also ignores 'out of scope' bracketed items eg. "ABC[DE][SG[HI[JK]L]MNO]" with the rule "BC<S>GM"
    // would match the "S" because the "[DE]" sub-branch is not a strict predecessor to the "S"
    // TO DO: modify this to match all subtrees on the right , but only the strict predecessor on the left
    // and to allow it to match brackets on the right? I'm not sure how that's supposed to work.
    string StripContext(string input)
    {
        string output = "";
        // First strip out all the ignore characters (easy)
        for(int i=0; i< input.Length; i++)
        {
            if (!ignoreString.Contains(input[i].ToString()))
            {
                output += input[i];
            }
        }
        input = output;
        output = "";
        // Then strip out bracketed substrings
        // Regex should be able to do this, but in C# (unlike Python) regexes don't support recersive descent
        //output = Regex.Replace(output, "\\[.*?]", ""); // NOPE
        // Instead we use a stack to keep track of where the last open-bracket was
        // and wipe the output back to that point when we find a matching close-bracket
        Stack<int> endOfContext = new Stack<int>();
        for(int i=0; i<input.Length;i++)
        {
            if(input[i] == '[')
            {
                endOfContext.Push(output.Length);
            }
            else if (input[i] == ']')
            {
                if(endOfContext.Count >= 1) // Don't pop an empty stack
                {
                    output = output.Substring(0,endOfContext.Pop());
                }
            }
            else
            {
                output += input[i];
            }
        }
        return output;
    }

    // This is the heart of the L-System, it's a recersive string-rewriting algorithm
    // that takes a set of rules and applies them in order to the input string
    // so a rule might be something like A -> AB so with the axiom
    string TreeGrower(string seed, Dictionary<string, string> ruleDict)
    {
        Regex contextRule = new Regex("(.+)<(.)>(.+)");
        Regex paramRule = new Regex("(.)\\(([a-z,A-Z]+)\\)");
        Match contextMatch, paramMatch;
        string newTree = "";
        bool found;
        for (int i = 0; i < seed.Length; i++)
        {
            string c = seed[i].ToString();
            found = false;
            foreach(KeyValuePair<string,string> rule in ruleDict)
            {
                if ((contextMatch = contextRule.Match(rule.Key)).Success) // This is a context-sensitive rule
                {
                    string leftContext, rightContext, leftRule, rightRule, fullContext, matchLiteral;
                    leftRule = contextMatch.Groups[1].Value;
                    rightRule = contextMatch.Groups[3].Value;
                    // If the string is not long enough to grab the contexts then it definitely doesn't match
                    if (i > (leftRule.Length-1) && i < seed.Length - rightRule.Length) 
                    {
                        // first strip all ignored characters and bracketed groups out of our context strings
                        leftContext = StripContext(seed.Substring(0, i));
                        rightContext = StripContext(seed.Substring(i + 1));
                        if(leftContext.Length >= leftRule.Length && rightContext.Length >= rightRule.Length)
                        {
                            // Only look at the size we actually want to compare
                            leftContext = leftContext.Substring(leftContext.Length - leftRule.Length, leftRule.Length);
                            rightContext = rightContext.Substring(0, rightRule.Length);
                            fullContext = leftContext + c + rightContext;
                            matchLiteral = Regex.Escape(leftRule + contextMatch.Groups[2].Value + rightRule);
                            if (Regex.Match(fullContext, matchLiteral).Success)
                            {
                                newTree += rule.Value;
                                found = true;
                                break; // found our rule, on to the next token
                            }
                        }
                    }
                }
                else if((paramMatch = paramRule.Match(rule.Key)).Success) // This is a parameterized rule
                {
                    string prodName = paramMatch.Groups[1].Value;
                    string[] paramNames = paramMatch.Groups[2].Value.Split(',');
                    string[] matchParams;
                    string toMatch = seed.Substring(i);
                    // now we know what pattern to look for, and we know it has to be at least 4 characters long

                    if (i <= seed.Length - 4 && toMatch.StartsWith(prodName+'(')) 
                    {
                        // This matches the production, but the parameter count also has to match
                        matchParams = toMatch.Substring(2, toMatch.IndexOf(')',2)-2).Split(','); //// DO NOT NEST PARENTHESIS
                        if(matchParams.Length == paramNames.Length)
                        {
                            // now we finally know we're in the right place, first figure out the value of each parameter
                            // and replace it
                            string modifiedRuleValue = rule.Value;
                            for(int j = 0; j < paramNames.Length; j++)
                            {
                                modifiedRuleValue = modifiedRuleValue.Replace(paramNames[j], matchParams[j]);
                            }
                            // now we have to do the math, otherwise the pattern won't match next generation
                            // go thru each pair of parenthesis in the output and evaluate any math functions
                            // inside them using the calculator.Compute() method (slow)
                            // This parenthesis checker does not do recursion, so...
                            // DO NOT NEST PARENTHESIS
                            // rules.Add("A(b,c,d)", "A(b+2,d*2,c/4)FF");
                            string clause, modifiedClause;
                            int closeParenIndex = 0;
                            int startI = 0;
                            while((startI = modifiedRuleValue.IndexOf('(', startIndex: startI+1)) != -1)
                            {
                                closeParenIndex = modifiedRuleValue.IndexOf(')', startI);
                                clause = modifiedRuleValue.Substring(startI + 1, closeParenIndex - (startI + 1));
                                string[] parts = clause.Split(',');
                                modifiedClause = "";
                                for(int j=0; j<parts.Length;j++)
                                {
                                    modifiedClause += calculator.Compute(parts[j], "").ToString();
                                    if(j != parts.Length - 1)
                                    {
                                        modifiedClause += ',';
                                    }
                                }
                                modifiedRuleValue = modifiedRuleValue.Substring(0, startI + 1)
                                    + modifiedClause
                                    + modifiedRuleValue.Substring(closeParenIndex);
                            }
                            newTree += modifiedRuleValue;
                            found = true;
                            i = seed.IndexOf(')', i);
                            break;
                        }
                    }
                }
                else if (c.Equals(rule.Key)) // This is a simple single-character rule
                {
                    newTree += rule.Value;
                    found = true;
                    break; // found our rule, on to the next token
                }
            }
            if(!found) // The default rule is copy unmodified.
            {
                newTree += c;
            }
        }
        Debug.Log("New Tree:" + newTree);
        return newTree;
    }

    
    void TreeBuilder(string lsystem)
    {
        // It's turtles, all the way down.
        Stack<Turtle3D> turtles = new Stack<Turtle3D>();
        // Start my turtle with forward as up and up as back so we make a vertical tree.
        Turtle3D topTurtle = new Turtle3D(Vector3.zero, Quaternion.LookRotation(Vector3.up, Vector3.back), Vector3.one);
        turtles.Push(topTurtle);
        for (int i = 0; i < lsystem.Length; i++)
        {

            switch (lsystem[i])
            {
                case 'F': // Move forward & draw
                    GameObject segment = Instantiate(stem);
                    if(lsystem[i+1] == '(')
                    {
                        string foo = lsystem.Substring(i + 2);
                        foo = foo.Substring(0, foo.IndexOf(')'));
                        //Debug.Log("foo is " + foo);
                        float val = float.Parse(foo);
                        topTurtle.scale = new Vector3(val, val, 1f);
                    }
                    topTurtle.MoveDraw(segment, this.transform);

                    break;
                case 'f': // Just move forward, no drawing
                    topTurtle.Move();
                    break;
                case 'L':
                    topTurtle.Draw(Instantiate(leaf), this.transform);
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
                    turtles.Push(new Turtle3D(topTurtle));
                    topTurtle.scale *= .85f;
                    break;
                case ']': //Pop stack
                    topTurtle = turtles.Pop();
                    break;
                case '!': // Shrink stem size
                    topTurtle.scale.Scale(new Vector3(.95f, .95f, 1f));
                    break;
                case '\'': // Color?
                    break;
            }
        }
    }
}
