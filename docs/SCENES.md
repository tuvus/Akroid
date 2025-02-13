## Scenes
Akroid has three scenes that the player will see and interact with.

* Start Scene: Houses the start menu and setting up the game.
The player then selects if they want to start a simulation or the campaing and loads into the loading scene.
* Loading Scene: Allows us to set up the battle scene asyncronously and to display the progress bar.
* Battle Scene: A simple template to structure the gameobjects for any game the player will play.
Instead of putting premade ships and objects in a specific scene as a "level" we change a script instead that determines the setup.
The battle can be set up through random generation or through a campaign script.
  * Random generation allows us to specify how many objects of what type we want in the battle and generates reasonable positions for them.
  * A campaign script allows for more control over how the battle is set up and supports dynamic events and rules.

Note: Pretty much all of the game development can be done in the start screen.
The battle gameobject structure and player UI structure are both held in prefabs so you can edit them without ever switching to the game scene.
This allows for easier testing through the start menu UI and a quicker development cycle.

