# Procedurally Generated Battlefield
HOW I BUILT A PROCEDURALLY GENERATED  REAL-TIME STRATEGY GAME

This devlog picks up from my previous one where I built a strategy game using heap data structure. I wanted to push the concept further and add some depth and breadth to the idea. In the previous game we only had two factions organized in a priority queue and a soldier is popped at a determined rate during battle. What if we could have more than two factions fighting at the same time? What are they fighting for anyway? Territory..Resources...Honor. Whatever it is, the factions have to be more organized. 
First, I had to figure out how to create the territories and organize them in a robust data-structure. The first thought that came to me was a dungeon generator since I’ve previously built a similar thing with my game Nafas. A dungeon generator works simply by building a Minimum-Spanning-Tree using Prim’s or Kruskal’s algorithm. This creates a kind of a graph structure whereby the vertex can be used as rooms and the connecting edges used as corridoors.


However, a typical dungeon generator did not fit my needs, largely due to the overlapping problem. The more rooms I created the harder it became to connect corridoors resulting in lots of meandering. Even if I got rid of the corridoors, that would restrict the movement of the characters; Unless it were a kind of space game with the territories serving as planets…. No, I’m not doing a space game.
Then I did some searching and came across an interesting topic in computational geometry called Voronoi Diagrams. According to AlgoAcademy, A Voronoi diagram is a partition of a plane (or space) based on the distance to a given set of "sites" (points). For every site, there is a corresponding Voronoi cell that contains all the points in the plane closest to that site. 

After doing some more background research on the topic, I decided to try building a procedurally generated map using Voronoi diagrams and see where how goes.




I rand into problems initially due to confusion in orientation. The goal was to generate circular voronoi boundaries around the towers (blue cubes) but my algorithm kept messing up by rendering the boundaries as if on a 2D plane instead of the XZ plane.





Anyway, I solved the orientation problem and successfully built a procedurally generated map with multiple territories.
However, this still looked like a Venn diagram and not a Voronoi diagram. I had to somehow solve the overlapping problem… again. 





To solve the overlapping problem, I created Additively Weighted Voronoi Diagrams where each tower has a "weight" that influences how far its boundary extends. I then wrote CalculateWeightedVoronoiBoundaries(), a method that samples points radially and uses additively weighted distance to determine boundaries. Stronger towers "pull" boundaries closer to weaker neighbors..e.g. tower level, shooting power, etc…

Ok nice, we have territories being procedurally generated, now what? I needed to give it some bit of life. What if I “Procedurally” generate characters into each territory?




I gave each territory native factions and made them form a Voronoi cell structure within their territories whereby the tower acts as a nucleus and the soldiers form a circular voronoi boundary around it to defend it.

It looked good, I felt like I was kinda starting to understand this Procedural content generation thing. However, it didn’t feel right to leave it there; how can I make it more fun? Of course, make them fight.






After generating the territories and spawning the characters, I could then easily add more complex battle logic to the gameplay code for instance giving the soldiers ability to form strategies that depend on the state of the battle i.e. a soldier can prioritize either to defend the tower by forming a voronoi boundary around the tower, intercept an invading enemy or to attack neighboring territories.

Turns out Voronoi Diagrams was the best candidate to solve my “organized warfare” problem. Check out the demo video on youtube https://youtu.be/S11xGAMXTBs and the full source code here on github.
