# EridineConflict
## A Bachelor's Engineering Thesis project
This project is a part of my Bachelor's Engineering Thesis in Computer Science at Wrocław University of Technology.
## Description
It is a single player real time strategy game (RTS), embedded in a science fiction universe. The game was made with Unity engine. The player is commanding his units in a form of a starship. His goal is to destroy all enemy's units. Units can be ordered to act individually or to work together in formations.   
In this project a special emphasis was placed on the design and implementation of units’ and computer enemy’s artificial intelligence, together with a graphical user interface.

Units' AI is divided into two layers. The first layer is responsible for action planning and decision making. 
Second layer is responsible for units' movement and is using steering behavior model. It provides target seeking and collision avoidance. It also enables units to work together in formations, by keeping them in strict separations from each other and by averaging their movement vectors to avoid chaos and collisions.

Enemy's AI is using my original solution, which is a combination of Lanchester Law and unbalanced assingment solution problem.
