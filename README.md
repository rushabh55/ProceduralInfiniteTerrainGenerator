TerrainGeneration
=================

My Artificial Intelligence project of procedural terrain generation using Unity3D. Uses the Unity3D's terrain engine, with calculated height maps to generate a truly procedural terrain in a grid of 3x3. The terrain rengerates when you move in a particular direction and uses the noise functions described below. 
Uses:
1. Perlin Noise
2. Vernoi Noise
3. Simplex Noise

This is implemented in C#. The unity web player runtime version of the prototype can be found on the following link http://rushg.me/UnityWebPlay.html?path=demos/webplayer/terrain
Please give it a min to load ;) 

Improvements Needed:
Optimization. Currently a lot of calculations are per frame bases, we can cache them. Some calculations involve iterating over the whole hight map twice. Some of this can be done in a single iteration
Artficats. The current version generates some notable artifacts, which can be cleared using height normalization. 


For any comments or suggestions, Please contact me at rushabh.techie@gmail.com 
