# Procedurally Generated Battlefield
HOW I BUILT A PROCEDURALLY GENERATED  REAL-TIME STRATEGY GAME

This devlog picks up from my previous one where I built a strategy game using heap data structure. I wanted to push the concept further and add some depth and breadth to the idea. In the previous game we only had two factions organized in a priority queue and a soldier is popped at a determined rate during battle. What if we could have more than two factions fighting at the same time? What are they fighting for anyway? Territory..Resources...Honor. Whatever it is, the factions have to be more organized. 
First, I had to figure out how to create the territories and organize them in a robust data-structure. The first thought that came to me was a dungeon generator since I’ve previously built a similar thing with my game Nafas. A dungeon generator works simply by building a Minimum-Spanning-Tree using Prim’s or Kruskal’s algorithm. This creates a kind of a graph structure whereby the vertex can be used as rooms and the connecting edges used as corridoors.

<img width="555" height="500" alt="Screenshot 2025-11-21 062534" src="https://github.com/user-attachments/assets/aa8db45a-e0b1-49cd-9768-948136ff6edc" />

<img width="611" height="483" alt="Screenshot 2025-11-21 062559" src="https://github.com/user-attachments/assets/cd0f2b30-48ab-4cd6-ad05-14b504656570" />


However, a typical dungeon generator did not fit my needs, largely due to the overlapping problem. The more rooms I created the harder it became to connect corridoors resulting in lots of meandering. Even if I got rid of the corridoors, that would restrict the movement of the characters; Unless it were a kind of space game with the territories serving as planets…. No, I’m not doing a space game.
Then I did some searching and came across an interesting topic in computational geometry called Voronoi Diagrams. According to AlgoAcademy, A Voronoi diagram is a partition of a plane (or space) based on the distance to a given set of "sites" (points). For every site, there is a corresponding Voronoi cell that contains all the points in the plane closest to that site. 

After doing some more background research on the topic, I decided to try building a procedurally generated map using Voronoi diagrams and see where how goes.

<img width="1029" height="426" alt="Screenshot 2025-11-19 054306" src="https://github.com/user-attachments/assets/91eb960d-4c46-44ce-b4b0-448a7aa4736c" />
<img width="1023" height="583" alt="Screenshot 2025-11-19 054243" src="https://github.com/user-attachments/assets/9ebe1761-e519-4bb5-b0d7-4125415cf9ab" />



I ran into problems initially due to confusion in orientation. The goal was to generate circular voronoi boundaries around the towers (blue cubes) but my algorithm kept messing up by rendering the boundaries as if on a 2D plane instead of the XZ plane.


<img width="824" height="548" alt="Screenshot 2025-11-19 061430" src="https://github.com/user-attachments/assets/33532f67-b3d5-4b85-997c-974ac5a05cdc" />



Anyway, I solved the orientation problem and successfully built a procedurally generated map with multiple territories.
However, this still looked like a Venn diagram and not a Voronoi diagram. I had to somehow solve the overlapping problem… again. 


<img width="824" height="528" alt="Screenshot 2025-11-19 064932" src="https://github.com/user-attachments/assets/02214e38-5acf-41e0-9e1c-906267971430" />
<img width="813" height="561" alt="Screenshot 2025-11-19 063837" src="https://github.com/user-attachments/assets/16ef3103-4193-41ad-8373-ed6f90338df7" />



To solve the overlapping problem, I created Additively Weighted Voronoi Diagrams where each tower has a "weight" that influences how far its boundary extends. I then wrote CalculateWeightedVoronoiBoundaries(), a method that samples points radially and uses additively weighted distance to determine boundaries. Stronger towers "pull" boundaries closer to weaker neighbors..e.g. tower level, shooting power, etc…

Ok nice, we have territories being procedurally generated, now what? I needed to give it some bit of life. What if I “Procedurally” generate characters into each territory?


<img width="942" height="632" alt="Screenshot 2025-11-19 192633" src="https://github.com/user-attachments/assets/2814c4e3-20d1-41f0-bbd9-d550010b0829" />
<img width="824" height="548" alt="Screenshot 2025-11-19 164027" src="https://github.com/user-attachments/assets/d54a4550-e8b5-45bc-9e6b-87b5863cdc3a" />


I gave each territory native factions and made them form a Voronoi cell structure within their territories whereby the tower acts as a nucleus and the soldiers form a circular voronoi boundary around it to defend it.

It looked good, I felt like I was kinda starting to understand this Procedural content generation thing. However, it didn’t feel right to leave it there; how can I make it more fun? Of course, make them fight.


<img width="775" height="494" alt="Screenshot 2025-11-20 181319" src="https://github.com/user-attachments/assets/7b39660f-d000-44e7-9e6c-4bb84317eaa7" />
<img width="1121" height="607" alt="Screenshot 2025-11-20 181233" src="https://github.com/user-attachments/assets/a0940a57-2b02-4efd-a8d0-99dd13ed0615" />
<img width="1114" height="530" alt="Screenshot 2025-11-20 204458" src="https://github.com/user-attachments/assets/6683c5b8-1922-4b32-ad60-38d82d02e0ba" />




After generating the territories and spawning the characters, I could then easily add more complex battle logic to the gameplay code for instance giving the soldiers ability to form strategies that depend on the state of the battle i.e. a soldier can prioritize either to defend the tower by forming a voronoi boundary around the tower, intercept an invading enemy or to attack neighboring territories.

Turns out Voronoi Diagrams was the best candidate to solve my “organized warfare” problem. Check out the demo video on youtube https://youtu.be/S11xGAMXTBs and the full source code here on github.
