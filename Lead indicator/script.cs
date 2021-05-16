/*
 * R e a d m e
 * -----------
 * YOUS's Target Lead Indicator v1 for Large Grids
 * ========COMPONENTS:=========================================================================
 * ship controller
 * turret or camera placed in front of where you intend to view the screen you can have both
 * transparent LCD
 * put the word Indicator(not case sensitive) in front of the name of these blocks ex.(Indicator - Transparant LCD)
 * any Terminal block named "weapon"(case sensitive) we need this for the location from which the projectile originates
 * this can be anything from a gatling gun to the welder in the center of your gun
 * =====================IMPORTANT:==============================================================
 * input the muzzle velocity for your intended weapon as the value for muzzleVelocity (gatling guns 400, rockets 350, etc.)
 * make sure you look at the config variables, there you set your weapons muzzle velocity (how fast the bullet goes)
 * along with other quality of life adjustments
 * if you are viewing the indicator through a camera name that camera "viewing camera"(case sentitive)
 * if you are viewing through a cockpit and your crosshair doesn't line up with the reticle, you will have to 
 * play around with the center offset
 * ====================Raycasting:============================================================
 * if you choose to use a camera over a turret:
 * make sure the camera is in front of your ship
 * make sure the raycasting camera has the "Indicator" tag
 * input the argument "scan" to start looking for targets in front of you
 * input scan again to stop scanning (you should do this to preserve power)
 * ============================================================================================
 */

//==============================================================================================
//config vars, RECOMPILE AFTER EDITING
readonly String tag = "Indicator";

//400m/s for gatling gun, 350m/s for rockets, if you are using a custom weapon set this value to whatever your weapon's projectile speed is
readonly float muzzleVelocity = 400f;

//don't worry about this if you are using a camera
// Since cockpits vary, if the center is lower than the default crosshair make this number a little higher,
// for example, 0.23 works well for the default cockpit, and 0.03 works for the industrial cockpit
//make sure you keep the f there, you might have to do a bit of trial and error here
float centerOffset = 0.23f;

//set this value to your weapons effective range in meters, all default weapons cap out at 800m
readonly float effectiveRange = 800;

//set this to false if you find the rotating animation annoying
readonly bool rotating = true;

//set this to false if you dont want lines
readonly bool DrawLines = true;
//=============================================================================================
//Dont touch these
bool isSetup = false;
IMyTextSurface drawingSurface;
IMyShipController ship;
IMyLargeTurretBase turret;
IMyTerminalBlock gun;
IMyTerminalBlock view;
float viewOffset = 0;
IMyCameraBlock caster;
MyDetectedEntityInfo target;
String notify;
RectangleF viewport;
Vector3D offsetGun;
Color reticleColor = Color.Red;
Color noti;
Color targt;
double counter = 0;
bool startScan = false;
float metersToPx;
double screenWidthInMeters;
public Program()
{
    if (!isSetup)
    {
        isSetup = Setup();
        if (isSetup)
        {
            Echo("Setup Complete!\n");

            PrepareTextSurfaceForSprites(drawingSurface);
        }
        else
        {
            Echo("Setup Failed(lol)\n");
            return;
        }
    }
    viewport = new RectangleF((drawingSurface.TextureSize - drawingSurface.SurfaceSize) / 2f,
    drawingSurface.SurfaceSize);
    // ASSUMPTION:
    // The screen is square
    screenWidthInMeters = (drawingSurface as IMyTextPanel).CubeGrid.GridSize * 0.855f; // Whip's magic number for large grid
    metersToPx = (float)(drawingSurface.TextureSize.X / screenWidthInMeters);
    Runtime.UpdateFrequency = UpdateFrequency.Update1;
}

public void Main(string argument, UpdateType updateSource)
{
    if (isSetup)
    {
        //set up the frame
        var frame = drawingSurface.DrawFrame();
        //draw the crosshair
        Vector2 center = new Vector2(viewport.Width / 2, (viewport.Height / 2) - (viewport.Height * centerOffset));
        DrawSprite(ref frame, "AH_BoreSight", center, reticleColor, (float)(3*Math.PI / 2), new Vector2(viewport.Width / 17, viewport.Height / 17));
        if (turret != null)
        {
            if (turret.HasTarget)
            {
                target = turret.GetTargetedEntity();
            }
        }

        if (caster != null)
        {
            if (argument.Equals("scan"))
            {
                startScan = !startScan;
            }
            if (startScan)
            {
                notify = "Scanning...";
                noti = Color.Red;
            }
            if (FindTarget(caster))
            {
                notify = "Target Spotted";
                noti = Color.Green;
            }
            if (!startScan)
            {
                caster.EnableRaycast = false;
                notify = "Scanning off";
                noti = Color.Yellow;
            }

            DrawText(ref frame, notify, new Vector2(viewport.Width / 7, (viewport.Height)-(viewport.Height / 5)),noti,0.5f);
        }
        if (!target.IsEmpty())
        {
            Vector2 targetScreen;
            float targetDistance = (float)(view.GetPosition() - target.Position).Length();
            if (WorldPositionToScreenPosition(target.Position, view, drawingSurface as IMyTextPanel, out targetScreen))
            {
                targt = targetDistance <= effectiveRange ? Color.Red : Color.Gray;
                DrawSprite(ref frame, "SquareHollow", targetScreen, targt, (float)((Math.PI / 3) + counter), new Vector2(viewport.Width / 20, viewport.Height / 20));
                DrawText(ref frame,targetDistance.ToString("0.00")+" m",new Vector2(targetScreen.X + (viewport.Width*0.05f),targetScreen.Y + (viewport.Height * 0.05f)),targt,0.5f);
                //iterate 1 radian every tick
                counter = rotating ? counter + Math.PI / 180: 0 ;
                offsetGun = (gun.GetPosition() - view.GetPosition());
                Vector3D WorldAimpoint = GetPredictedTargetPosition2(gun.GetPosition(), Convert3(ship.GetShipVelocities().LinearVelocity), target, muzzleVelocity);
                // account for gun's offeset
                WorldAimpoint -= offsetGun;
                Vector2 leadScreen;
                if (targetDistance <= effectiveRange)
                {
                    if (WorldPositionToScreenPosition(WorldAimpoint, view, drawingSurface as IMyTextPanel, out leadScreen))
                    {
                        DrawSprite(ref frame, "CircleHollow", leadScreen, Color.Orange, 0, new Vector2(viewport.Width / 20, viewport.Height / 20));
                        DrawSprite(ref frame, "Cross", leadScreen, Color.Red, (float)((Math.PI / 4) - counter), new Vector2(viewport.Width / 50, viewport.Height / 50));
                        if (DrawLines)
                        {
                            WorldAimpoint += offsetGun;
                            float distance = (float)(WorldAimpoint - target.Position).Length();
                            DrawLineBetweenPoints(targetScreen, leadScreen, ref frame,distance);
                        }

                        //Find out if reticle is aiming at targets predicted position
                        Vector2 distanceFromCenter = leadScreen - center;
                        if (distanceFromCenter.Length() <= viewport.Width / 20)
                        {
                            reticleColor = Color.Green;
                        }
                        else
                        {
                            reticleColor = Color.Red;
                        }
                    }
                }

            }
        }
        else
        {
            reticleColor = Color.Red;
            DrawText(ref frame, "no target", new Vector2(center.X, (viewport.Height) - (viewport.Height / 3)), Color.Red, 0.5f);
        }
        frame.Dispose();
    }
    else
    {
        isSetup = Setup();
    }

}

//function Derived from Whiplash141, I slightly modified it
//projects World position vector onto LCD screen
bool WorldPositionToScreenPosition(Vector3D worldPosition, IMyTerminalBlock cockpit, IMyTextPanel screen, out Vector2 screenPositionPx)
{
    screenPositionPx = Vector2.Zero;

    Vector3D cockpitPos = cockpit.GetPosition() + cockpit.WorldMatrix.Forward * viewOffset;
    Vector3D screenPosition = screen.GetPosition() + screen.WorldMatrix.Forward * 0.5 * screen.CubeGrid.GridSize;
    Vector3D normal = screen.WorldMatrix.Forward;
    Vector3D cockpitToScreen = screenPosition - cockpitPos;
    double distanceToScreen = Math.Abs(Vector3D.Dot(cockpitToScreen, normal));

    Vector3D viewCenterWorld = distanceToScreen * cockpit.WorldMatrix.Forward;

    // Project direction onto the screen plane (world coords)
    Vector3D direction = worldPosition - cockpitPos;
    Vector3D directionParallel = direction.Dot(normal) * normal;
    double distanceRatio = distanceToScreen / directionParallel.Length();

    Vector3D directionOnScreenWorld = distanceRatio * direction;

    // If we are pointing backwards, ignore
    if (directionOnScreenWorld.Dot(screen.WorldMatrix.Forward) < 0)
    {
        return false;
    }

    Vector3D planarcockpitToScreen = cockpitToScreen - Vector3D.Dot(cockpitToScreen, normal) * normal;
    directionOnScreenWorld -= planarcockpitToScreen;

    // Convert location to be screen local (world coords)
    Vector2 directionOnScreenLocal = new Vector2(
        (float)directionOnScreenWorld.Dot(screen.WorldMatrix.Right),
        (float)directionOnScreenWorld.Dot(screen.WorldMatrix.Down));



    // Convert direction to be screen local (pixel coords)
    directionOnScreenLocal *= metersToPx;

    // Get final location on screen
    Vector2 screenCenterPx = new Vector2(screen.TextureSize.X / 2, (screen.TextureSize.Y / 2) - (screen.TextureSize.Y * centerOffset));
    screenPositionPx = screenCenterPx + directionOnScreenLocal;
    return true;
}
//Draws a sprite with given parameters
public MySprite DrawSprite(ref MySpriteDrawFrame frame, String spriteData, Vector2 position, Color color, float rotationAngle, Vector2 size)
{
    var sprite = new MySprite()
    {
        Type = SpriteType.TEXTURE,
        Data = spriteData,
        Position = position,
        Size = size,
        Color = color,
        Alignment = TextAlignment.CENTER,
        RotationOrScale = rotationAngle
    };
    frame.Add(sprite);
    return sprite;
}
//Draws a text sprite with the given parameters
public MySprite DrawText(ref MySpriteDrawFrame frame, String text, Vector2 position, Color color, float scale)
{
    var sprite = new MySprite()
    {
        Type = SpriteType.TEXT,
        Data = text,
        Color = color,
        Position = position,
        Alignment = TextAlignment.CENTER,
        RotationOrScale = scale,
        FontId = "White"
    };
    frame.Add(sprite);
    return sprite;
}
//gets all the necessary blocks
public bool Setup()
{
    Echo("--====Running Setup====--\n");
    List<IMyTerminalBlock> cockpits = new List<IMyTerminalBlock>();
    List<IMyTerminalBlock> turrets = new List<IMyTerminalBlock>();
    List<IMyTerminalBlock> screens = new List<IMyTerminalBlock>();
    List<IMyTerminalBlock> cameras = new List<IMyTerminalBlock>();
    Echo("Screen Found\n");
    GridTerminalSystem.SearchBlocksOfName(tag, cockpits, cockpit => cockpit is IMyShipController);
    if (cockpits.Count < 1)
    {
        Echo("Error: no ShipController with tag: " + tag + " found\n");
        return false;
    }
    Echo("ShipController Found\n");
    GridTerminalSystem.SearchBlocksOfName(tag, turrets, turret => turret is IMyLargeTurretBase);
    GridTerminalSystem.SearchBlocksOfName(tag, cameras, camera => camera is IMyCameraBlock);
    if (turrets.Count < 1)
    {
        Echo("no Turret with tag: " + tag + " found, hope you have a camera\n");
        if (cameras.Count < 1)
        {
            Echo("Error: no Camera with tag: " + tag + " found\n");
            return false;
        }
    }
    if(!(cameras.Count < 1))
    {
        Echo("Camera found\n");
        caster = cameras[0] as IMyCameraBlock;
    }
    if (!(turrets.Count < 1))
    {
        Echo("Turret Found\n");
        turret = turrets[0] as IMyLargeTurretBase;
    }
    GridTerminalSystem.SearchBlocksOfName(tag, screens, screen => screen is IMyTextSurface);
    if (screens.Count < 1)
    {
        Echo("Error: no TextSurface with tag: " + tag + " found\n");
        return false;
    }
    ship = cockpits[0] as IMyShipController;
    drawingSurface = screens[0] as IMyTextSurface;
    gun = GridTerminalSystem.GetBlockWithName("weapon");
    if (gun == null)
    {
        Echo("Error: no block with name weapon found\n");
        return false;
    }
    view = GridTerminalSystem.GetBlockWithName("viewing camera");
    if(view == null)
    {
        view = ship;
        Echo("Viewing camera not found, using cockpit check config variables\n");
    }
    else
    {
        Echo("Viewing camera found, using camera\n");
        viewOffset = 0.25f;
        centerOffset = 0f;
    }
    Echo("Gun Found\n");

    return true;
}
public void PrepareTextSurfaceForSprites(IMyTextSurface textSurface)
{
    textSurface.ContentType = ContentType.SCRIPT;
    textSurface.Script = "";
    textSurface.ScriptBackgroundColor = Color.Black;
}

//Converts Vector3D to Vector3
public Vector3 Convert3(Vector3D v)
{
    return new Vector3(v.X, v.Y, v.Z); ;
}
// Calculate the time when we can hit a target with a bullet
//Derived from Keen's turret system
public Vector3D GetPredictedTargetPosition2(Vector3D shooterPos, Vector3 ShipVel, MyDetectedEntityInfo target, float shotSpeed)
{
    Vector3D predictedPosition = target.Position;
    Vector3D dirToTarget = Vector3D.Normalize(predictedPosition - shooterPos);

    //Run Setup Calculations
    Vector3 targetVelocity = target.Velocity;
    targetVelocity -= ShipVel;
    Vector3 targetVelOrth = Vector3.Dot(targetVelocity, dirToTarget) * dirToTarget;
    Vector3 targetVelTang = targetVelocity - targetVelOrth;
    Vector3 shotVelTang = targetVelTang;
    float shotVelSpeed = shotVelTang.Length();

    if (shotVelSpeed > shotSpeed)
    {
        // Shot is too slow
        return Vector3.Normalize(target.Velocity) * shotSpeed;
    }
    else
    {
        // Run Calculations
        float shotSpeedOrth = (float)Math.Sqrt(shotSpeed * shotSpeed - shotVelSpeed * shotVelSpeed);
        Vector3 shotVelOrth = dirToTarget * shotSpeedOrth;
        float timeDiff = shotVelOrth.Length() - targetVelOrth.Length();
        var timeToCollision = timeDiff != 0 ? ((shooterPos - target.Position).Length()) / timeDiff : 0;
        Vector3 shotVel = shotVelOrth + shotVelTang;
        //if the time to collision is small we just return the target's current position
        predictedPosition = timeToCollision > 0.01f ? shooterPos + (Vector3D)shotVel * timeToCollision : predictedPosition;
        return predictedPosition;
    }
}

public bool FindTarget(IMyCameraBlock camera)
{
    MyDetectedEntityInfo targetInfo = new MyDetectedEntityInfo();
    if(!camera.EnableRaycast && startScan)
        camera.EnableRaycast = true;
    if(startScan && camera.CanScan(effectiveRange))
    {
        targetInfo = camera.Raycast(camera.AvailableScanRange);
        Echo("Scan Complete\n");
    }
    if (!targetInfo.IsEmpty())
    {
        if (targetInfo.Type.ToString().Equals("Planet"))
        {
            return false;
        }
        target = targetInfo;
        return true;
    }
    return false;

}
//Draws dotted line from position1 to position2
public void DrawLineBetweenPoints(Vector2 position1, Vector2 position2, ref MySpriteDrawFrame frame, float displayedDistance)
{
    //yeah, I know my y=mx+b B^)
    //find line's equation
    float slope = (position2.Y - position1.Y) / (position2.X - position1.X);
    float intercept = position1.Y - (slope * position1.X);
    float start = Math.Min(position1.X,position2.X);
    float end = Math.Max(position1.X, position2.X);
    float distance = (float)(position1 - position2).Length();
    float centerDot = (float)Math.Floor(distance / 20);
    //Display the line
    Vector2 offset = viewport.Size * 0.03f;
    int count = 0;
    for (float i = start; i<end; i += 10)
    {
        float y = (slope * i) + intercept;
        Vector2 point = new Vector2(i, y-offset.Y);
        DrawText(ref frame, ".", point, Color.White, 0.6f);
        if (count == centerDot && centerDot>=3)
        {
            DrawText(ref frame, displayedDistance.ToString("0.00") + " m", new Vector2(i,y+(viewport.Height*0.02f)), Color.White, 0.5f);
        }
        count++;
    }
}