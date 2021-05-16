# SE-Target-Lead-Indicator
I did a lot of math in my free time
SPACE ENGINEERS
-Uses turrets and/or cameras to find target
-calculates target lead based on muzzle velocity,target velocity,your velocity
-projects these locations onto LCD screen
YOUS's Target Lead Indicator v1 for Large Grids
========COMPONENTS:=========================================================================
ship controller
turret or camera placed in front of where you intend to view the screen you can have both
transparent LCD
put the word Indicator(not case sensitive) in front of the name of these blocks ex.(Indicator - Transparant LCD)
any Terminal block named "weapon"(case sensitive) we need this for the location from which the projectile originates
this can be anything from a gatling gun to the welder in the center of your gun
=====================IMPORTANT:==============================================================
input the muzzle velocity for your intended weapon as the value for muzzleVelocity (gatling guns 400, rockets 350, etc.)
make sure you look at the config variables, there you set your weapons muzzle velocity (how fast the bullet goes)
along with other quality of life adjustments
if you are viewing the indicator through a camera name that camera "viewing camera"(case sentitive)
if you are viewing through a cockpit and your crosshair doesn't line up with the reticle, you will have to 
play around with the center offset
====================Raycasting:============================================================
if you choose to use a camera over a turret:
make sure the camera is in front of your ship
make sure the raycasting camera has the "Indicator" tag
input the argument "scan" to start looking for targets in front of you
input scan again to stop scanning (you should do this to preserve power)
============================================================================================
