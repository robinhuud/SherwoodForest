using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Data;

// L-System based on the paper http://algorithmicbotany.org/papers/lsfp.pdf by Przemyslaw Prusinkiewicz and James Hanan
// and the book "The Algorithmic Beauty of Plants" http://algorithmicbotany.org/papers/#abop (Prusinkiewicz & Lindenmayer 1990)
// It takes an "axiom" and a set of "rules" and uses them to build a string that represents the branching structure
// of a virtual plant.
// This structure is then interpreted by a 3D "turtle" a-la-LOGO but with a Push/Pop transformation stack
// Rules cane be added to the system using dictionary entries of the form <string, string> with
// rules.Add(rule, result) shown here in the form "A" -> "ABA"
//
// Rules can be of the following types:
// 1) context-sensitive rules                       "AB<C>B" -> "CA"
//    will only substitute "C" for "CA" if it is between "AB" and "B" in that order, eg. "ABCBACA" becomes "ABCABACA"
//    left and right context can have multiple characters, but center item must be a single character
//    when context searching, sub-branches (enclosed in [] brackets) and characters in the "ignored list"
//    are ignored, see comments for StripContext() for more info.
// 2) parameterized rules                           "A(x)" -> "A(x*.65)BC" or "F(a,b)" -> "F(a*.95,b/2)"
//    will substitute the variables and evaluate each result each iteration, eg. "FA(1)F(1,2)A" becomes "FA(.65)F(.95,1)A"
//    order of operations is left-to-right, NO NESTED PARENTHESIS, if parameter count does not match, the rule does not match
// 3) simple single-character replacement           "C" -> "CC"
//    will simply substitute every "C" in the input with an "CC" in the output, eg. "ABCBACA" becomes "ABCCBACCA"
//
// Rules are tested on each character left-to-right in the presidence order above
// If any rule matches, parsing moves forward to the next character (or next clause for parameterized)
//
// TO DO:
//   Stochastic L-Systems
//   allow combination rules (esp. context-sensitive and parameterized)
//     fix single-character limitation of central match strings for contextual
//     fix ignore code to work correctly with parameters

public class LSystem
{
    // The "seed" or axiom of the tree determines the initial shape
    private string axiom;
    private string currentTree;

    // This is a list of characters that the context rule matching algorithm ignores when matching
    // to allow a context-sensitive L-System to ignore drawing commands and focus on the structure
    private string ignoreString = "";

    // Number of iterations that has run on this LSystem
    private int numIterations = 0;

    // The list of rules as a dictionary add rules using the command:
    // rules.Add(predicate, production), eg. rules.Add("A", "AFA");
    private Dictionary<string, string> rules = new Dictionary<string, string>();

    // Global data table to turn strings into math results
    private DataTable calculator = new DataTable();

    // Regex objects for matching the different kinds of rules
    private Regex contextRule = new Regex("(.+)<(.)>(.+)");
    private Regex paramRule = new Regex("(.)\\(([a-z,A-Z]+)\\)");

    // Match objects for each of the Regexes
    private Match contextMatch, paramMatch;

    /// <summary>
    /// Default Constructor
    /// </summary>
    public LSystem()
    {
        this.axiom = "";
    }

    /// <summary>
    /// Constructor with supplied axiom
    /// </summary>
    /// <param name="axiom">Axiom string</param>
    public LSystem(string axiom)
    {
        this.axiom = axiom;
    }

    /// <summary>
    /// Constructor with both axiom and ruleset specified in the constructor
    /// </summary>
    /// <param name="axiom">Axiom string</param>
    /// <param name="rules">Rules dictionary</param>
    public LSystem(string axiom, Dictionary<string, string> rules)
    {
        this.axiom = axiom;
        this.rules = rules;
    }

    /// <summary>
    /// Set the axiom, silently fails if the LSystem has already been grown
    /// </summary>
    /// <param name="newAxiom"></param>
    public void SetAxiom(string newAxiom)
    {
        if(numIterations == 0)
        {
            this.axiom = newAxiom;
        }
    }

    /// <summary>
    /// add a bunch of characters to the ignore list, does not check for duplicates
    /// </summary>
    /// <param name="newIgnores"></param>
    public void AddIgnoreChars(string newIgnores)
    {
        ignoreString += newIgnores;
    }

    /// <summary>
    /// Add a rule to the LSystem, can do this after it's grown ;)
    /// </summary>
    /// <param name="rule">The matching part of the rule</param>
    /// <param name="result">The string to substitute</param>
    public void AddRule(string rule, string result)
    {
        rules.Add(rule, result);
    }

    /// <summary>
    /// Over-ride to the ToString method to retrieve the current tree;
    /// </summary>
    /// <returns>the string representation of the L-System</returns>
    public override string ToString()
    {
        if(numIterations == 0)
        {
            currentTree = axiom;
        }
        return currentTree;
    }

    // This is the heart of the L-System, it's a recersive string-rewriting algorithm
    // that takes a set of rules and applies them in order to the input string
    // see comments above
    public void Grow()
    {
        string newTree = "";
        bool found = false;
        // First time we grow, we must copy the axiom into the root of the tree
        if (numIterations == 0 && currentTree != axiom)
        {
            currentTree = axiom;
        }
        for (int i = 0; i < currentTree.Length; i++)
        {
            string c = currentTree[i].ToString();
            found = false;
            foreach(KeyValuePair<string,string> rule in rules)
            {
                if ((contextMatch = contextRule.Match(rule.Key)).Success) // This is a context-sensitive rule
                {
                    //Debug.Log("Found Context Rule" + contextMatch.Groups.Count);
                    string leftContext, rightContext, leftRule, rightRule, fullContext, matchLiteral;
                    leftRule = contextMatch.Groups[1].Value;
                    rightRule = contextMatch.Groups[3].Value;
                    // If the string is not long enough to grab the contexts then it definitely doesn't match
                    if (i > (leftRule.Length-1) && i < currentTree.Length - rightRule.Length) 
                    {
                        // first strip all ignored characters and bracketed groups out of our context strings
                        leftContext = StripContext(currentTree.Substring(0, i));
                        rightContext = StripContext(currentTree.Substring(i + 1));
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
                    string toMatch = currentTree.Substring(i);
                    // now we know what pattern to look for, and we know it has to be at least 4 characters long
                    if (i <= currentTree.Length - 4 && toMatch.StartsWith(prodName+'(')) 
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
                            i = currentTree.IndexOf(')', i);
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
        //Debug.Log("New Tree:" + newTree);
        currentTree = newTree;
        numIterations++;
    }

    // Strips out unwanted characters (from ignoreList) from the context for context matching
    // also ignores 'out of scope' bracketed items eg. "ABC[DE][SG[HI[JK]L]MNO]" with the rule "BC<S>GM"
    // would match the "S" because the "[DE]" sub-branch is not a strict predecessor to the "S"
    // matches close bracket "]" on the right, but not on the left, (and not open bracket "[")
    // TO DO: modify this to match all subtrees on the right , but only the strict predecessor on the left
    // 
    private string StripContext(string input)
    {
        string output = "";
        // First strip out all the ignore characters (easy)
        for (int i = 0; i < input.Length; i++)
        {
            if (!ignoreString.Contains(input[i].ToString()))
            {
                output += input[i];
            }
        }
        input = output;
        output = "";
        // Then strip out bracketed substrings only with matching pairs
        Stack<int> endOfContext = new Stack<int>();
        for (int i = 0; i < input.Length; i++)
        {
            if (input[i] == '[')
            {
                endOfContext.Push(output.Length);
            }
            else if (input[i] == ']')
            {
                if (endOfContext.Count >= 1) // Don't pop an empty stack
                {
                    output = output.Substring(0, endOfContext.Pop());
                }
                else
                {
                    output += ']';
                }
            }
            else
            {
                output += input[i];
            }
        }
        return output;
    }
}