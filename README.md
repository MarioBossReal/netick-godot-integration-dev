Development repo for improving Netick's Godot integration. Netick created by Karrar Rahim. 

[17/12/23]
This version of the integration cleans up a lot of the editor plugin code, and makes it so that prefabs and levels are registered and referred to by their names rather than their resource path, among other things.
Prefabs and levels are automatically added to the netick config whenever they are opened / switched to in the editor. Invalid prefabs and levels are removed from the netick config automatically before the game is run.
Some changes have been made to the bomberman demo, however the prefab structure remains the same for now until a good solution can be implemented.
Most files have been renamed to use snake_case as this is the proper case to use within Godot. The actual addon files haven't been renamed as this caused everything to break.
