using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using System.Text;

public class LSystemController : MonoBehaviour {

	public float initial_length = 2;
	public float initial_radius = 1.0f;
	StringBuilder start = new StringBuilder("");
	StringBuilder lang = new StringBuilder("");
	StringBuilder[,] rules = new StringBuilder[2,2];
	GameObject contents;
	GameObject parent;
	List<GameObject> list = new List<GameObject>();
	float angleToUse = 45f;
	public int iterations = 1;
	
	// for drawing lines
	public Material lineMaterial; 
	public float lineWidth = 1.0f;

	void Start () {
	
		
	    //variables : 0, 1
    	//constants: [, ]
    	//axiom  : 0
    	//rules  : (1 → 11), (0 → 1[0]0)
		// Second example LSystem from 
		// http://en.wikipedia.org/wiki/L-system
		start = new StringBuilder("0");
		rules[0,0] = new StringBuilder("1");
		rules[0,1] = new StringBuilder("11");
		rules[1,0] = new StringBuilder("0");
		rules[1,1] = new StringBuilder("1[0]0");
		angleToUse = 45f;
		run(iterations);
		print (lang);
		display2 ();
		
		// Weed type plant example from: 
		// http://en.wikipedia.org/wiki/L-system
//		start = new StringBuilder("X");
//		rules[0,0] = new StringBuilder("X");
//		rules[0,1] = new StringBuilder("F-[[X]+X]+F[+FX]-X");
//		rules[1,0] = new StringBuilder("F");
//		rules[1,1] = new StringBuilder("FF");
//		angleToUse = 25f;
//		run(iterations);
//		print (lang);
//		display3 ();

	}
	
	// Get a rule from a given letter that's in our array
	// Assume that there's a 2d array of [letters, rules]
	string getRule( string input) {
		for(int i=0; i<rules.GetLength(0); i++) {
			//print (rules[i,0]);
			if (rules[i,0].ToString().Equals( input )) {
				return rules[i,1].ToString();
			}
		}
		
		return input;
	}
	
	// Run the lsystem iterations number of times on the start axiom.
	void run(int iterations) {
    	StringBuilder curr = start;
		
    	for (int i = 0; i < iterations; i++) {
        	for (int j = 0; j < curr.Length; j++) {
            	string buff = getRule(curr[j].ToString() );
                curr = curr.Replace(curr[j].ToString(), buff, j, 1);
                j += buff.Length - 1;
        	}
    	}

    	lang = curr;
	}
	
	// The display routine for the weed type plant above
	void display3() {
		
		// to push and pop location and angles
		Stack<float> positions = new Stack<float>();
		Stack<float> angles = new Stack<float>();
		
		// current location and angle
		float angle = 0f;
		Vector3 position = new Vector3(0,0,0);
		float posy = 0.0f;
		float posx = 0.0f;
		
		// location and rotation to draw towards
		Vector3 newPosition;
		Vector2 rotated;
		
		// start at 0,0,0
		// create a new object for every line segment
		// Yes, this is hokey, but it's the easiest way to do things
		contents = new GameObject();
		GameObject tmp = contents;
		LineRenderer line = tmp.AddComponent<LineRenderer>(); 
		line.SetColors(Color.green,Color.green);
		line.material = lineMaterial;

		// Apply all the drawing rules to the lsystem string
		for(int i=0; i<lang.Length; i++) {
			string buff = lang[i].ToString();
			switch (buff) {
			case "-" : 
				// Turn left 25
				angle -= angleToUse;
				break;
			case "+" : 
				// Turn right 25
				angle += angleToUse;
				break;
			case "F" : 
				// draw a line 
				posy += initial_length;
				newPosition = new Vector3(position.x, posy, 0);
				rotated = rotate (position, new Vector3(position.x,posy,0), angle);
				newPosition = new Vector3(rotated.x,rotated.y,0);
				drawLSystemLine(position, newPosition, line, Color.green);
				// set up for the next draw
				position = newPosition;
				posx = newPosition.x;
				posy = newPosition.y;
				tmp = new GameObject();
				line = tmp.AddComponent<LineRenderer>(); 
				break;
			case "[" :
				//[: push position and angle
				positions.Push (posy);
				positions.Push (posx);
				float currentAngle = angle;
				angles.Push(currentAngle);
				break;
			case "]" : 
				//]: pop position and angle
				posx = positions.Pop();
				posy = positions.Pop();
				position = new Vector3(posx, posy, 0);
				angle = angles.Pop();								
				break;
			default : break;
			}
			
			
		}
	}
	
	// Display routine for 2nd example on the Wikipedia page
	void display2() {
		
		// to push and pop location and angle
		Stack<float> positions = new Stack<float>();
		Stack<float> angles = new Stack<float>();
		
		// current angle and position
		float angle = 0f;
		Vector3 position = new Vector3(0,0,0);
		float posy = 0.0f;
		float posx = 0.0f;

		// positions to draw towards
		Vector3 newPosition;
		Vector2 rotated;
		
		// start at 0,0,0
		// create a new object for every line segment
		contents = new GameObject();
		GameObject tmp = contents;
		LineRenderer line = tmp.AddComponent<LineRenderer>(); 

		// Apply the drawing rules to the string given to us
		for(int i=0; i<lang.Length; i++) {
			string buff = lang[i].ToString();
			switch (buff) {
			case "0" : 
				// draw a line ending in a leaf
				posy += initial_length;
				newPosition = new Vector3(position.x, posy, 0);
				rotated = rotate (position, new Vector3(position.x,posy,0), angle);
				newPosition = new Vector3(rotated.x,rotated.y,0);
				drawLSystemLine(position, new Vector3(rotated.x,rotated.y,0), line, Color.red);
				// set up for the next draw
				position = newPosition;
				posx = newPosition.x;
				posy = newPosition.y;
				tmp = new GameObject();
				line = tmp.AddComponent<LineRenderer>(); 
				drawCircle(0.05f, 0.05f, position, line, Color.blue);
				tmp = new GameObject();
				line = tmp.AddComponent<LineRenderer>(); 
				break;
			case "1" : 
				// draw a line 
				posy += initial_length;
				newPosition = new Vector3(position.x, posy, 0);
				rotated = rotate (position, new Vector3(position.x,posy,0), angle);
				newPosition = new Vector3(rotated.x,rotated.y,0);
				drawLSystemLine(position, newPosition, line, Color.green);
				// set up for the next draw
				position = newPosition;
				posx = newPosition.x;
				posy = newPosition.y;
				tmp = new GameObject();
				line = tmp.AddComponent<LineRenderer>(); 
				break;
			case "[" :
				//[: push position and angle, turn left 45 degrees
				positions.Push (posy);
				positions.Push (posx);
				float currentAngle = angle;
				angles.Push(currentAngle);
				angle -= 45;
				break;
			case "]" : 
				//]: pop position and angle, turn right 45 degrees
				posx = positions.Pop();
				posy = positions.Pop();
				position = new Vector3(posx, posy, 0);
				angle = angles.Pop();
				angle += 45;		
				break;
			default : break;
			}
			
			
		}
	}
	
	// Draw a line with the given parameters
	void drawLSystemLine (Vector3 from, Vector3 to, LineRenderer line, Color color){
		line.SetColors(color,color);
		line.material = lineMaterial;
		float wide = lineWidth;
		List<Vector3> drawArray = new List<Vector3>();
		drawArray.Add(from);
		drawArray.Add(to);
		
		line.SetWidth(wide, wide);
		line.SetVertexCount(drawArray.Count);
		
		for (int i = 0; i < drawArray.Count; i++){
			for (int j = i; j < drawArray.Count; j++){
					line.SetPosition(j, drawArray[i]); //set the position of all unused points to the current point (to avoid weird stretching thing)
			}
				
			line.SetPosition(i, drawArray[i]);
		}
	}
	
	// rotate a line and return the position after rotation
	// Assumes rotation around the Z axis
	Vector2 rotate(Vector3 pivotPoint, Vector3 pointToRotate, float angle) {
   		Vector2 result;
   		float Nx = (pointToRotate.x - pivotPoint.x);
   		float Ny = (pointToRotate.y - pivotPoint.y);
   		angle = -angle * Mathf.PI/180f;
   		result = new Vector2(Mathf.Cos(angle) * Nx - Mathf.Sin(angle) * Ny + pivotPoint.x, Mathf.Sin(angle) * Nx + Mathf.Cos(angle) * Ny + pivotPoint.y);
   		return result;
	}
   

	// Draw a circle with the given parameters
	// Should probably use different stuff than the default
    void drawCircle(float radiusX, float radiusY, Vector3 center, LineRenderer line, Color color) {

        float x;
        float y;
        float z = 0f;
		int segments = 15;
        float angle = (360f / segments);
			
		line.SetVertexCount (segments + 1);       
		line.material = lineMaterial;
		line.SetColors(color,color);

        for (int i = 0; i < (segments + 1); i++) {

            x = Mathf.Sin (Mathf.Deg2Rad * angle) * radiusX + center.x;
            y = Mathf.Cos (Mathf.Deg2Rad * angle) * radiusY + center.y;

            line.SetPosition (i,new Vector3(x,y,0) );

            angle += (360f / segments);

        }

    }
		
	
	// Update is called once per frame
	void Update () {
	
	}
	
}
