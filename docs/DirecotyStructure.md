## Directory Structure
The source files can be found in the `/Assets` directory.
The source code is split into three different assemblies.
Here are some of the more important files:

* Game Logic `/Assets/Battle`: Contains the game logic to simulate the game but not run the UI.
The game logic not depend on the UI so that they aren't closely coupled.
Players or a server might want to simulate the game without running any UI.
The game is like a simulation the will run without player input.
  * `BattleManager`: The most important class in the game. It handles the creation of objects, setup of the game and updating objects.
  * `Faction`: Sets up and controls units owned by the faction. Holds a FactionAI and FactionCommManager.
  * `SimulationFactionAI`: The top level AI that gathers resources, creates fleets, commands ships and fleets.
  * `IObject`: Most basic object with a position and size.
    * `BattleObject`: Anything object that will be rendered. Has a rotation, sprite, scale and can be spawned.
      * `Unit`: Objects with health, ownership, weapons, components and can be targeted and destroyed.
        * `Ship`: A moving object with a ShipAI, can dock to stations and be a part of a fleet.
          * `ShipAI`: Stores a queue of commands for the ship and the logic for them. Tells the simpler ship logic where to move and turn.
      * `Station`: Stationary objects with a hangar
      * `Star/Asteroid/GasCloud`: Non-Unit objects that are rendered
      * `Planet`: Holds planet factions which can control territory on the planet.
      * `Projectile`
      * `Missile`
    * `ObjectGroup`: Holds a set of BattleObjects and calculates the center position and size.
      * `Fleet`: Holds a set of Ships and a FleetAI
        * `FleetAI`: Stores a queue of commands for the Fleet and gives commands to Ships in the fleet.
      * `AsteroidField`: Holds multiple asteroids.
  * `EventManager`: Handles creating and checking event conditions.
    * `EventChainBuilder`: Handles long chains of event conditions for the campaign.
  * `FactionCommManager`: Handles communications between factions.


* User Interface `/Assets/UI`: Contains sprites and player UI.
The UI does two things, it renders objects in the correct positions based on the game logic state and hanles player input.
  * `UIManger`: Top level class handles updating all UI components and settings.
  * `UnitSpriteManager`: Handles rendering objects in the game logic and updating them.
  * `PlayerUI`: Handles displaying the player HUD, various UI menus and panels.
  * `LocalPlayerInput`: Handles player unit selection, camera movement and player issued commands.
  * `UIEventManager`: Handles processing UI event conditions.


* Tests `/Assets/Tests`: Contains tests for both the game logic and the UI.


* Resources `/Assets/Resources`: Contains unit data, images and campaign files.
  * `UnitScriptableObject`: Template for a unit, holds specific values that will be used to set up a unit or won't be changed at runtime.
  * Prefabs are used by the UI to render the unit and tell the UnitScriptable object what systems, modules and component the unit can hold.


* Campaign `/Assets/Resources/Campaign`: Contains the campaign logic and any special units for the campaign.
* Start Menu `/Scences/StartMenu:` Contains the source files and data for the start screen.
