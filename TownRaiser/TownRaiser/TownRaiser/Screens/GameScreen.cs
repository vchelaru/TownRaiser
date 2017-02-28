using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

using FlatRedBall;
using FlatRedBall.Input;
using FlatRedBall.Instructions;
using FlatRedBall.AI.Pathfinding;
using FlatRedBall.Graphics.Animation;
using FlatRedBall.Graphics.Particle;

using FlatRedBall.Math.Geometry;
using FlatRedBall.Math.Splines;

using Cursor = FlatRedBall.Gui.Cursor;
using GuiManager = FlatRedBall.Gui.GuiManager;
using FlatRedBall.Localization;

using Keys = Microsoft.Xna.Framework.Input.Keys;
using Vector3 = Microsoft.Xna.Framework.Vector3;
using Texture2D = Microsoft.Xna.Framework.Graphics.Texture2D;
using FlatRedBall.Math;
using FlatRedBall.TileCollisions;
using TownRaiser.DataTypes;
using TownRaiser.Entities;
using TownRaiser.Spawning;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using FlatRedBall.TileEntities;

namespace TownRaiser.Screens
{
    #region Enums

    public enum ActionMode
    {
        Select,
        Train,
        Build
    }

    public enum ResourceType
    {
        Gold,
        Lumber,
        Stone
    }

    public enum MusicMode
    {
        Peace,
        Combat,
    }
    #endregion

    public partial class GameScreen
	{
        public event EventHandler SecondaryClick;
        #region Fields/Properties

        private RaidSpawner raidSpawner;

        private MusicMode m_CurrentMusicModeState;

        public int Lumber { get; set; } = 1200;
        public int Stone { get; set; } = 1000;
        public int Gold { get; set; } = 1000;

        public bool HasTrainingUnits => selectedBuilding != null && selectedBuilding.TrainingQueue.Count > 0;
        
        public int CurrentCapacityUsed
        {
            get
            {
                return currentCapacityUsedButUseProperty + currentTrainingCapacity;
            }
            set
            {
                currentCapacityUsedButUseProperty = value;
            }
        }
        public int MaxCapacity { get; set; }

        TileNodeNetwork tileNodeNetwork;

        TileShapeCollection woodResourceShapeCollection;
        TileShapeCollection stoneResourceShapeCollection;
        TileShapeCollection waterResourceShapeCollection;
        TileShapeCollection goldResourceShapeCollection;

        public const float GridWidth = 16;

        PositionedObjectList<Entities.Unit> selectedUnits = new PositionedObjectList<Entities.Unit>();
        UnitData topSelectedUnit;

        Entities.Building selectedBuilding;

        private float mapXMin;
        private float mapXMax;
        private float mapYMin;
        private float mapYMax;

        private int currentCapacityUsedButUseProperty;
        private int currentTrainingCapacity;

#if DEBUG
        //Debug fields and properties.
        I2DInput cameraControls;
#endif

        #endregion

        #region Initialize Methods

        void CustomInitialize()
        {
            InitializeCamera();

            FlatRedBall.Debugging.Debugger.TextCorner = FlatRedBall.Debugging.Debugger.Corner.BottomLeft;

            InitializeNodeNetwork();

            InitializeResourceTileShapeCollections();

            InitializeUi();

            InitializeRaidSpawner();

            InitializeEncounterPoints();
            
            InitializeSoundTracker();

            InitializeMusic();
        }

        private void InitializeMusic()
        {
            m_CurrentMusicModeState = MusicMode.Peace;
            
            FlatRedBall.Audio.AudioManager.PlaySong(FR_TownSong_Loop, true, false);
        }

        private void InitializeEncounterPoints()
        {
            TileEntityInstantiator.CreateEntitiesFrom(WorldMap);


            // this is temporary code - do we eventually want these set in the TMX? If so, do they reference a CSV? or do they 
            // have their own values....probably CSV so that we can tune difficulty
            var encounterPoint = Factories.EncounterSpawnPointFactory.CreateNew();
            encounterPoint.X = 500;
            encounterPoint.Y = -400;
        }

        private void InitializeSoundTracker()
        {
            //This sets the minimum distance for sounds to play from camera center.
            var mainCamera = Camera.Main;
            var halfWidth = mainCamera.OrthogonalWidth / 2;
            var halfHeight = mainCamera.OrthogonalHeight / 2;
            SoundEffectTracker.Initialize(halfHeight * halfHeight + halfWidth * halfWidth);
        }

        private void InitializeRaidSpawner()
        {
            raidSpawner = new RaidSpawner();
            raidSpawner.Buildings = BuildingList;
            raidSpawner.NodeNetwork = tileNodeNetwork;
            raidSpawner.RequestSpawn += HandleRaidSpawn;
        }

        private void InitializeResourceTileShapeCollections()
        {
            woodResourceShapeCollection = new TileShapeCollection();
            woodResourceShapeCollection.AddMergedCollisionFrom(WorldMap,
            (list) => list.Any(item => item.Name == "ResourceType" && item.Value as string == "Wood"));
#if DEBUG
            woodResourceShapeCollection.Visible = Entities.DebuggingVariables.ShowResourceCollision;
            if (Entities.DebuggingVariables.ShowResourceCollision)
            {
                foreach (var rect in woodResourceShapeCollection.Rectangles)
                {
                    rect.Color = Microsoft.Xna.Framework.Color.Green;
                }
            }
#endif

            stoneResourceShapeCollection = new TileShapeCollection();
            stoneResourceShapeCollection.AddMergedCollisionFrom(WorldMap,
              (list) => list.Any(item => item.Name == "ResourceType" && item.Value as string == "Stone"));
#if DEBUG
            stoneResourceShapeCollection.Visible = Entities.DebuggingVariables.ShowResourceCollision;
            if (Entities.DebuggingVariables.ShowResourceCollision)
            {
                foreach (var rect in stoneResourceShapeCollection.Rectangles)
                {
                    rect.Color = Microsoft.Xna.Framework.Color.Gray;
                }
            }
#endif

            waterResourceShapeCollection = new TileShapeCollection();
            waterResourceShapeCollection.AddMergedCollisionFrom(WorldMap,
              (list) => list.Any(item => item.Name == "ResourceType" && item.Value as string == "Water"));
#if DEBUG
            waterResourceShapeCollection.Visible = Entities.DebuggingVariables.ShowResourceCollision;
            if (Entities.DebuggingVariables.ShowResourceCollision)
            {
                foreach (var rect in waterResourceShapeCollection.Rectangles)
                {
                    rect.Color = Microsoft.Xna.Framework.Color.Blue;
                }
            }
#endif

            goldResourceShapeCollection = new TileShapeCollection();
            goldResourceShapeCollection.AddMergedCollisionFrom(WorldMap,
              (list) => list.Any(item => item.Name == "ResourceType" && item.Value as string == "Gold"));
#if DEBUG
            goldResourceShapeCollection.Visible = Entities.DebuggingVariables.ShowResourceCollision;
            if (Entities.DebuggingVariables.ShowResourceCollision)
            {
                foreach (var rect in goldResourceShapeCollection.Rectangles)
                {
                    rect.Color = Microsoft.Xna.Framework.Color.Yellow;
                }
            }
#endif


            
        }

        private void InitializeCamera()
        {
            SpriteManager.OrderedSortType = FlatRedBall.Graphics.SortType.ZSecondaryParentY;

            //Eventually place the map at the main base spawn point.
            Camera.Main.X = Camera.Main.RelativeXEdgeAt(0) + .2f;
            Camera.Main.Y = -Camera.Main.RelativeYEdgeAt(0) + .2f;
#if DEBUG
            cameraControls = InputManager.Keyboard.Get2DInput(Keys.Left, Keys.Right, Keys.Up, Keys.Down);
#endif
            //Initialize Map bounds
            //World map stars drawing at the upper left corner of the map.
            mapXMin = WorldMap.X;
            mapXMax = mapXMin + WorldMap.Width;
            
            mapYMax = WorldMap.Y;
            mapYMin = mapYMax - WorldMap.Height;

            ClampCameraToMapEdge();
        }

        private void InitializeUi()
        {
            //Set local resource varables.
            

            this.GroupSelectorInstance.VisualRepresentation = GroupSelectorGumInstance;
            this.GroupSelectorInstance.IsInSelectionMode = true;

            this.GroupSelectorInstance.SelectionFinished += HandleGroupSelection;

            ActionToolbarInstance.TrainUnitInvokedFromActionToolbar += (notused, args) =>
            {
                HandlePerfromTrain(args.UnitData);
            };

            // move the UI above all other stuff
            //GameScreenGum.Z = 20;
            SpriteManager.AddToLayer(GameScreenGum, this.UiLayer);

            UpdateResourceDisplay();
        }

        private void HandleGroupSelection(object sender, EventArgs e)
        {
            //Clear selected building and units
            selectedUnits.Clear();
            selectedBuilding?.UpdateRallyPointVisibility(false);
            selectedBuilding = null;
            topSelectedUnit = null;

            // The user is most likely attempting to select units, we should prioritize unit seletction here.

            foreach (var unit in this.UnitList)
            {
                bool canSelect =
                    unit.UnitData.IsEnemy == false && unit.CollideAgainst(GroupSelectorInstance);

#if DEBUG
                if (DebuggingVariables.CanSelectEnemies)
                {
                    // a little inefficient but whatever, it's debug
                    canSelect = unit.CollideAgainst(GroupSelectorInstance);
                }
#endif

                if (canSelect)
                {
                    selectedUnits.Add(unit);
                }
            }

            if (selectedUnits.Count == 0)
            {
                selectedBuilding = BuildingList.FirstOrDefault(item => item.HasCursorOver(GuiManager.Cursor));
            }

            UpdateSelectionMarker();
            HandlePostSelection();
        }

        private void InitializeNodeNetwork()
        {
            TileNodeNetwork.VisibleCoefficient = 3;

            tileNodeNetwork = new TileNodeNetwork(GridWidth / 2f,
                -WorldMap.Height + GridWidth / 2f,
                GridWidth,
                MathFunctions.RoundToInt(WorldMap.Width / WorldMap.WidthPerTile.Value),
                MathFunctions.RoundToInt(WorldMap.Height / WorldMap.HeightPerTile.Value),
                DirectionalType.Eight);

            tileNodeNetwork.FillCompletely();

            var namesToExclude = WorldMap.Properties
                .Where(item => item.Value
                    .Any(customProperty => customProperty.Name == "BlocksPathfinding" && (string)customProperty.Value == "true"));

            foreach(var layer in WorldMap.MapLayers)
            {
                foreach(var name in namesToExclude)
                {
                    var indexes =  layer.NamedTileOrderedIndexes.ContainsKey(name.Key) ? layer.NamedTileOrderedIndexes[name.Key] : null;

                    if(indexes != null)
                    {
                        foreach(var index in indexes)
                        {
                            float x, y;
                            layer.GetBottomLeftWorldCoordinateForOrderedTile(index, out x, out y);

                            var toRemove = tileNodeNetwork.TiledNodeAtWorld(x + GridWidth/2, y + GridWidth/2);

                            if(toRemove != null)
                            {
                                tileNodeNetwork.Remove(toRemove);
                            }
                        }
                    }
                }
            }

#if DEBUG
            tileNodeNetwork.Visible = Entities.DebuggingVariables.ShowNodeNetwork;
#else
            tileNodeNetwork.Visible = false;
#endif
        }

        private void InitializeEvents()
        {
            
        }

        #endregion

        #region Activity Methods

        void CustomActivity(bool firstTimeCalled)
        {
            HotkeyActivity();

            ClickActivity();

            CameraMovementActivity();

            CollisionActivity();

            BuildMarkerActivity();

            CursorChangeActivity();

            RaidSpawningActivity();

            MusicActivity();
        }

        private void MusicActivity()
        {
            var camera = Camera.Main;

            if(CombatTracker.AreUnitsInCombat)
            {
                if (m_CurrentMusicModeState == MusicMode.Peace)
                {
                    m_CurrentMusicModeState = MusicMode.Combat;
                    FlatRedBall.Audio.AudioManager.PlaySong(FR_BattleSong_Loop, true, false);
                }
            }
            else if( CombatTracker.AreUnitsInCombat == false && m_CurrentMusicModeState == MusicMode.Combat)
            {
                m_CurrentMusicModeState = MusicMode.Peace;
                FlatRedBall.Audio.AudioManager.PlaySong(FR_TownSong_Loop, false, false);
                if (CombatTracker.PlayerCount > 0)
                {
                    FlatRedBall.Audio.AudioManager.PlaySongThenResumeCurrent(FR_VictorySong, false);
                    this.Call(HackPlayAfter).After(FR_VictorySong.Duration.TotalSeconds);
                }
                CombatTracker.Clear();

            }
        }

        private void HackPlayAfter()
        {
            FlatRedBall.Audio.AudioManager.PlaySong(FR_TownSong_Loop, false, false);
        }

        private void RaidSpawningActivity()
        {
            bool shouldSpawn = true;
#if DEBUG
            if (DebuggingVariables.NoTimedSpawns)
            {
                shouldSpawn = false;
            }
#endif
            if(shouldSpawn)
            {
                raidSpawner.Activity();
            }
        }

        private void CursorChangeActivity()
        {
            var cursor = GuiManager.Cursor;
            
            //ToDo: Add other conditions to the special cursors.
            if(cursor.MiddleDown)
            {
                CustomCursorGraphicController.CurrentCursorState = CursorState.Move;
            }
            else if(selectedUnits.Count > 0 && cursor.SecondaryDown) 
            {
                CustomCursorGraphicController.CurrentCursorState = CursorState.Attack;
            }
            else if(selectedBuilding != null && cursor.SecondaryDown)
            {
                CustomCursorGraphicController.CurrentCursorState = CursorState.Target;
            }
            else
            {
                CustomCursorGraphicController.CurrentCursorState = CursorState.Select;
            }

        }

        private void BuildMarkerActivity()
        {
            if(ActionToolbarInstance.GetActionStateBaseOnUi() == ActionMode.Build && GetIfCanClickInWorld())
            {
                //We don't want to show the group selector if we are placing a building.
                GroupSelectorInstance.IsInSelectionMode = false;

                BuildingMarkerInstance.Visible = true;
                BuildingMarkerInstance.BuildingData = ActionToolbarInstance.SelectedBuildingData;
                float x, y;

                GetBuildLocationFromCursor(out x, out y);


                bool isInvalid = BuildingList.Any(item => item.Collision.CollideAgainst(BuildingMarkerInstance.AxisAlignedRectangleInstance))
                    || woodResourceShapeCollection.Rectangles.Any(rect => rect.CollideAgainst(BuildingMarkerInstance.AxisAlignedRectangleInstance))
                    || stoneResourceShapeCollection.Rectangles.Any(rect => rect.CollideAgainst(BuildingMarkerInstance.AxisAlignedRectangleInstance))
                    || waterResourceShapeCollection.Rectangles.Any(rect => rect.CollideAgainst(BuildingMarkerInstance.AxisAlignedRectangleInstance));

                if (isInvalid )
                {
                    BuildingMarkerInstance.CurrentState = Entities.BuildingMarker.VariableState.Invalid;
                }
                else
                {
                    BuildingMarkerInstance.CurrentState = Entities.BuildingMarker.VariableState.Normal;

                }

                BuildingMarkerInstance.X = x;
                BuildingMarkerInstance.Y = y;
                // put it above other stuff
                BuildingMarkerInstance.Z = 3;
            }
            else
            {
                // Reset BuildingMarkerInstance state.
                BuildingMarkerInstance.Visible = false;
                if (BuildingMarkerInstance.CurrentState != Entities.BuildingMarker.VariableState.Invalid) {
                    BuildingMarkerInstance.CurrentState = Entities.BuildingMarker.VariableState.Invalid;
                }
                GroupSelectorInstance.IsInSelectionMode = true;
            }
        }

        private void CollisionActivity()
        {
            PerformUnitsVsTerrainCollision();

            PerformUnitsVsUnitsCollision();

            PerformUnitVsEncounterPointCollision();
        }

        private void PerformUnitVsEncounterPointCollision()
        {
            // brute force it for now:
            for (int i = 0; i < EncounterSpawnPointList.Count; i++)
            {
                var encounterPoint = EncounterSpawnPointList[i];

                if (encounterPoint.CurrentLogicState == EncounterSpawnPoint.LogicState.Dormant)
                {
                    // don't do anything with this encounter point, it can't spawn until it 
                    // regenerates all dead units:
                    continue;
                }
                else if (encounterPoint.CurrentLogicState == EncounterSpawnPoint.LogicState.Spawned)
                {
                    // if no units are touching this, then it will go back to Active mode, waiting for the next spawn:
                    bool areAnyUnitsTouching = UnitList.Any(unit => unit.UnitData.IsEnemy == false && unit.CollideAgainst(encounterPoint));

                    if (!areAnyUnitsTouching)
                    {
                        encounterPoint.ReturnSpawnedUnits();
                    }
                }
                else if (encounterPoint.CurrentLogicState == EncounterSpawnPoint.LogicState.ReturningUnits ||
                    encounterPoint.CurrentLogicState == EncounterSpawnPoint.LogicState.ActiveWaiting)
                {
                    var playerUnit = UnitList.FirstOrDefault(unit => unit.UnitData.IsEnemy == false && unit.CollideAgainst(encounterPoint));

                    if (playerUnit != null)
                    {
                        encounterPoint.Attack(playerUnit, SpawnNewUnit);
                    }

                }

            }
        }

        private void PerformUnitsVsTerrainCollision()
        {
            for (int i = 0; i < UnitList.Count; i++)
            {
                stoneResourceShapeCollection.CollideAgainstSolid(UnitList[i].CircleInstance);
                woodResourceShapeCollection.CollideAgainstSolid(UnitList[i].CircleInstance);
            }
        }

        private void PerformUnitsVsUnitsCollision()
        {
            // larger value keeps them from overlapping...
            const float separationCoefficient = 3;

            for(int i = 0; i < UnitList.Count -1; i++)
            {
                var first = UnitList[i];
                for(int j = i+1; j < UnitList.Count; j++)
                {
                    var second = UnitList[j];
                    if(first.CircleInstance.CollideAgainstMove(second.CircleInstance, 1, 1))
                    {
                        var firstRepositionVector = new Vector3(
                            first.CircleInstance.LastMoveCollisionReposition.X,
                            first.CircleInstance.LastMoveCollisionReposition.Y, 0);

                        var secondRepositionVector = new Vector3(
                            second.CircleInstance.LastMoveCollisionReposition.X,
                            second.CircleInstance.LastMoveCollisionReposition.Y, 0);

                        first.Position -= firstRepositionVector;
                        second.Position -= secondRepositionVector;

                        first.Position += separationCoefficient * firstRepositionVector * TimeManager.SecondDifference;
                        second.Position += separationCoefficient * secondRepositionVector * TimeManager.SecondDifference;
                    }
                }
            }
        }

        private void HotkeyActivity()
        {
            var completedBuildings = BuildingList.Where(item => item.IsConstructionComplete).ToList();
            ActionToolbarInstance.UpdateButtonEnabledStates(Lumber, Stone, Gold, CurrentCapacityUsed, MaxCapacity, completedBuildings);
            if(InputManager.Keyboard.AnyKeyPushed())
            {
                ActionToolbarInstance.ReactToKeyPress();
                if(ActionToolbarInstance.CurrentVariableState == GumRuntimes.ActionToolbarRuntime.VariableState.SelectModeView)
                {
                    selectedBuilding?.UpdateRallyPointVisibility(false);
                }
            }

        }

        private void CameraMovementActivity()
        {
#if DEBUG

            const float cameraMovementSpeed = 200;
            Camera.Main.XVelocity = cameraMovementSpeed * cameraControls.X;
            Camera.Main.YVelocity = cameraMovementSpeed * cameraControls.Y;
#endif

            var cursor = GuiManager.Cursor;
            if(cursor.MiddleDown)
            {
                //Minusequals - we want to pull the map in the direction of the cursor.
                Camera.Main.X -= cursor.WorldXChangeAt(0);
                Camera.Main.Y -= cursor.WorldYChangeAt(0);

                //Clamp to map bounds.
                ClampCameraToMapEdge();
            }
        }

        private void ClampCameraToMapEdge()
        {
            var camera = Camera.Main;

            if (camera.AbsoluteLeftXEdgeAt(0) < mapXMin)
            {
                camera.X = mapXMin + camera.OrthogonalWidth / 2;
            }
            else if (camera.AbsoluteRightXEdgeAt(0) > mapXMax)
            {
                camera.X = mapXMax - camera.OrthogonalWidth / 2;
            }

            if (camera.AbsoluteBottomYEdgeAt(0) < mapYMin)
            {
                camera.Y = mapYMin + camera.OrthogonalHeight / 2;
            }
            else if (camera.AbsoluteTopYEdgeAt(0) > mapYMax)
            {
                camera.Y = mapYMax - camera.OrthogonalHeight / 2;
            }
        }

        private void ClickActivity()
        {
            var cursor = GuiManager.Cursor;
            FlatRedBall.Debugging.Debugger.Write(GuiManager.Cursor.WindowOver);

            if(cursor.PrimaryClick && !GroupSelectorInstance.WasReleasedThisFrame)
            {
                if (GetIfCanClickInWorld())
                {
                    //Update: February 11, 2017
                    //Rick Blaylock
                    //After implementing hotkeys and proper unit/building data I ran into issues where the action mode would not update on a double click.
                    //For now, we will check the toggle state on clicks.
                    switch(ActionToolbarInstance.GetActionStateBaseOnUi())
                    {
                        case ActionMode.Build:
                            if (BuildingMarkerInstance.CurrentState == Entities.BuildingMarker.VariableState.Normal)
                            {
                                HandlePerformBuildingClick();
                                HandlePostClick();
                            }
                            break;
                        case ActionMode.Select:
                            HandlePerformSelection();
                            break;
                    }

#if DEBUG
                    DebugClickActivity();
#endif
                }
            }

            if(cursor.SecondaryClick)
            {
                if (GetIfCanClickInWorld())
                {
                    HandleSecondaryClick();
                }
                else
                {
                    //Raise the SecondaryClick event for any unit buttons which can cancel training.
                    SecondaryClick?.Invoke(null, null);
                }
            }
        }

        private bool GetIfCanClickInWorld()
        {
            var cursor = GuiManager.Cursor;

            return cursor.WindowOver == null || cursor.WindowOver == this.ResourceDisplayInstance;
        }

        private void DebugClickActivity()
        {
            var keyboard = InputManager.Keyboard;
            if(keyboard.KeyDown(Keys.D1) || GuiManager.Cursor.PrimaryDoubleClick)
            {
                DebugAddUnit(GlobalContent.UnitData[UnitData.Snake]);
            }
            if (keyboard.KeyDown(Keys.D2))
            {
                DebugAddUnit(GlobalContent.UnitData[UnitData.Bat]);
            }
            if (keyboard.KeyDown(Keys.D3))
            {
                DebugAddUnit(GlobalContent.UnitData[UnitData.Cyclops]);
            }
            if (keyboard.KeyDown(Keys.D4))
            {
                DebugAddUnit(GlobalContent.UnitData[UnitData.Dragon]);
            }
            if (keyboard.KeyDown(Keys.D5))
            {
                DebugAddUnit(GlobalContent.UnitData[UnitData.KingSkeleton]);
            }
            if (keyboard.KeyDown(Keys.D6))
            {
                DebugAddUnit(GlobalContent.UnitData[UnitData.Octopus]);
            }
            if (keyboard.KeyDown(Keys.D7))
            {
                DebugAddUnit(GlobalContent.UnitData[UnitData.Skeleton]);
            }
            if (keyboard.KeyDown(Keys.D8) )
            {
                DebugAddUnit(GlobalContent.UnitData[UnitData.Slime]);
            }
            if (keyboard.KeyDown(Keys.D9))
            {
                DebugAddUnit(GlobalContent.UnitData[UnitData.Goblin]);
            }

            var cursor = GuiManager.Cursor;
            var worldX = cursor.WorldXAt(0);
            var worldY = cursor.WorldYAt(0);

            const int amountToAddPerClick = 8;

            if (goldResourceShapeCollection.GetTileAt(worldX, worldY) != null)
            {
                Gold += amountToAddPerClick;
                UpdateResourceDisplay();
            }
            if (woodResourceShapeCollection.GetTileAt(worldX, worldY) != null)
            {
                Lumber += amountToAddPerClick;
                UpdateResourceDisplay();
            }
            if (stoneResourceShapeCollection.GetTileAt(worldX, worldY) != null)
            {
                Stone += amountToAddPerClick;
                UpdateResourceDisplay();
            }
        }

        private void DebugAddUnit(UnitData unitData)
        {
            var newUnit = Factories.UnitFactory.CreateNew();
            newUnit.NodeNetwork = this.tileNodeNetwork;
            newUnit.AllUnits = UnitList;
            newUnit.AllBuildings = BuildingList;
            newUnit.X = GuiManager.Cursor.WorldXAt(0);
            newUnit.Y = GuiManager.Cursor.WorldYAt(0);
            newUnit.Z = 1;
            newUnit.UnitData = unitData;

        }

        private void HandlePostClick()
        {
            if(InputManager.Keyboard.KeyDown(Keys.LeftShift) == false && InputManager.Keyboard.KeyDown(Keys.RightShift) == false)
            {
                ActionToolbarInstance.SetActionMode(ActionMode.Select);
            }
        }

        private void HandleSecondaryClick()
        {
            HandleSelectedBuilding();
            HandleSelectedUnitsRightClick();
        }

        private void HandleSelectedBuilding()
        {
            //Create a rally point for units that are created.
            //If the selected building can train units.
            Cursor cursor = GuiManager.Cursor;
            if (selectedBuilding != null)
            {
                
                if(selectedBuilding.IsConstructionComplete == false && selectedBuilding.IsCursorOverSprite(cursor))
                {
                    CancelBuildingConstruction();
                }
                else if (selectedBuilding.HasTrainableUnits)
                {
                    var worldX = cursor.WorldXAt(0);
                    var worldY = cursor.WorldYAt(0);

                    selectedBuilding.RallyPoint = new Vector3() { X = worldX, Y = worldY, Z = selectedBuilding.Z };
                }
            }
            else if(selectedUnits.Count == 0)
            {
                selectedBuilding = BuildingList.FirstOrDefault(x => x.IsCursorOverSprite(cursor));
                if (selectedBuilding != null)
                {
                    CancelBuildingConstruction();
                }
            }
        }

        private void CancelBuildingConstruction()
        {
            selectedBuilding.TryCancelBuilding();
            selectedBuilding = null;
            UpdateResourceDisplay();
            HandlePostSelection();
        }

        private void HandleSelectedUnitsRightClick()
        {
            //Only do this work if we have selected units
            if (selectedUnits.Count > 0)
            {
                //Play obey order sound effect
                Unit.TryPlayObeySound(topSelectedUnit);

                Cursor cursor = GuiManager.Cursor;

                var worldX = cursor.WorldXAt(0);
                var worldY = cursor.WorldYAt(0);

                // Are we right-clicking a resource?
                var woodResourceOver = woodResourceShapeCollection.GetTileAt(worldX, worldY);
                var stoneResourceOver = stoneResourceShapeCollection.GetTileAt(worldX, worldY);
                var goldResourceOver = goldResourceShapeCollection.GetTileAt(worldX, worldY);
                var resourceOver = woodResourceOver ?? stoneResourceOver ?? goldResourceOver;
                ResourceType? resourceType = null;
                if (woodResourceOver != null) { resourceType = ResourceType.Lumber; }
                else if (stoneResourceOver != null) { resourceType = ResourceType.Stone; }
                else if (goldResourceOver != null) { resourceType = ResourceType.Gold; }

                // Are we right-clicking on a building?
                var buildingOver = BuildingList.FirstOrDefault(item => item.IsCursorOverSprite(cursor));

                // Are we right-clicking an enemy?
                var enemyOver = UnitList.FirstOrDefault(item =>
                    item.UnitData.IsEnemy && item.HasCursorOver(cursor));

                foreach (var selectedUnit in selectedUnits)
                {
                    if (enemyOver != null)
                    {
                        selectedUnit.AssignAttackGoal(enemyOver);
                    }
                    // If unit has resource to return and we are right-clicking a town hall to return it to...
                    else if (selectedUnit.HasResourceToReturn && buildingOver?.BuildingData?.Name == BuildingData.TownHall)
                    {
                        var clickLocation = new Vector3(worldX, worldY, 0);
                        selectedUnit.AssignResourceReturnGoal(clickLocation, buildingOver, selectedUnit.ResourceTypeToReturn.Value);
                    }
                    // If a unit initiates battle, then it cannot gather resources
                    else if (resourceOver != null && selectedUnit.UnitData.InitiatesBattle == false)
                    {
                        var clickLocation = new Vector3(worldX, worldY, 0);
                        selectedUnit.AssignResourceCollectGoal(clickLocation, resourceOver, resourceType.Value);
                    }
                    else
                    {
                        // todo: do we want to differentiate between move and move+attack?
                        const bool forceWalk = false;

                        if (selectedUnit.UnitData.InitiatesBattle == false || forceWalk)
                        {
                            selectedUnit.AssignMoveGoal(worldX, worldY);
                        }
                        else
                        {
                            // todo: this eventually will get removed, but it's here for testing:
                            if (selectedUnit.UnitData.IsEnemy)
                            {
                                selectedUnit.AssignAttackThenRetreat(worldX, worldY);
                            }
                            else
                            {
                                selectedUnit.AssignMoveAttackGoal(worldX, worldY);
                            }
                        }
                    }
                }
            }
        }

        private void HandlePerformSelection()
        {
            //Clear selected building and units
            selectedUnits.Clear();
            selectedBuilding?.UpdateRallyPointVisibility(false);
            selectedBuilding = null;
            topSelectedUnit = null;

            

            var cursor = GuiManager.Cursor;

            selectedBuilding = BuildingList.FirstOrDefault(item => item.IsCursorOverSprite(cursor));

            if (selectedBuilding == null)
            {
                foreach (var unit in this.UnitList)
                {
                    bool canSelect =
                        unit.UnitData.IsEnemy == false && unit.HasCursorOver(cursor);

    #if DEBUG
                    if (DebuggingVariables.CanSelectEnemies)
                    {
                        // a little inefficient but whatever, it's debug
                        canSelect = unit.CollideAgainst(GroupSelectorInstance);
                    }
    #endif

                    if (canSelect)
                    {
                        selectedUnits.Add(unit);
                        break; //Break out since we have found a unit from a click. Only drag and select should select should select many.
                    }
                }
            }

            UpdateSelectionMarker();
            HandlePostSelection();
        }

        private void HandlePostSelection()
        {
            if(selectedBuilding == null && selectedUnits.Count == 0)
            {
                this.ActionToolbarInstance.SetViewFromEntity(null);                
            }
            else if (selectedBuilding != null)
            {
                this.ActionToolbarInstance.SetViewFromEntity(selectedBuilding);
                selectedBuilding.UpdateRallyPointVisibility(true);
                
            }
            else 
            {
                topSelectedUnit = GetTopSelectedUnit();
                Unit.TryPlaySelectSound(topSelectedUnit);
                this.ActionToolbarInstance.SetViewFromEntity(null);
            }
        }

        private UnitData GetTopSelectedUnit()
        {
            var distinctList = selectedUnits.Select(x => x.UnitData).Distinct();
            UnitData topData = null;
            int topDataCount = 0;

            foreach (var unit in distinctList)
            {
                var currentCount = selectedUnits.Count(x => x.UnitData == unit);
                if (topData == null || topDataCount < currentCount)
                {
                    topData = unit;
                    topDataCount = currentCount; ;
                }
            }

            return topData;
        }

        private void UpdateSelectionMarker()
        {
            while(SelectionMarkerList.Count > selectedUnits.Count)
            {
                SelectionMarkerList.Last().Destroy();
            }
            while(SelectionMarkerList.Count < selectedUnits.Count)
            {
                var selectionMarker = new Entities.SelectionMarker();
                selectionMarker.RelativeZ = -.01f;
                SelectionMarkerList.Add(selectionMarker);
            }

            for(int i = 0; i < SelectionMarkerList.Count; i++)
            {
                SelectionMarkerList[i].AttachTo(selectedUnits[i], false);
            }
        }
        
        private void HandlePerfromTrain(DataTypes.UnitData unitData)
        {
            if (unitData == null)
            {
                throw new Exception("Tried to train a null unit.");
            }
            
            //Only check gold first.
            bool hasEnoughGold = unitData.GoldCost <= this.Gold;

#if DEBUG
            if (Entities.DebuggingVariables.HasInfiniteResources)
            {
                hasEnoughGold = true;
            }
#endif

            //If we have enough gold, we will add the unit to the end of the queue.
            //The training will not occur until there is enough capacity, but units can be queued as long as there is gold.
            bool wasAddeToQueue = hasEnoughGold ? selectedBuilding.TryAddUnitToTrain(unitData.Name) : false;

            if (wasAddeToQueue)
            {
                bool shouldUpdateResources = true;
#if DEBUG

                shouldUpdateResources = Entities.DebuggingVariables.HasInfiniteResources == false;
#endif
                if (shouldUpdateResources)
                {
                    this.Gold -= unitData.GoldCost;
                }

                UpdateResourceDisplay();
            }
            else
            {
                //tell them?
            }

        } 

        private void HandlePerformBuildingClick()
        {

            DataTypes.BuildingData buildingType = ActionToolbarInstance.SelectedBuildingData;

            if(buildingType == null)
            {
                throw new Exception("Building Data is null.");
            }

            bool hasEnoughResources = this.Lumber >= buildingType.LumberCost && this.Stone >= buildingType.StoneCost;

            bool isOverOtherBuilding = false;
            if(hasEnoughResources)
            {
                var cursor = GuiManager.Cursor;
                isOverOtherBuilding = BuildingList.Any(item => item.HasCursorOver(cursor));
            }

            bool canBuild = hasEnoughResources && !isOverOtherBuilding;

            if (canBuild)
            {
                PerformBuildAtCursor(buildingType);
            }
        }

        private void PerformBuildAtCursor(BuildingData buildingType)
        {
                var building = Factories.BuildingFactory.CreateNew();
            building.BuildingData = buildingType;

            building.BuildingConstructionCompleted += () =>
            {
                UpdateCapacityValue();
                UpdateResourceDisplay();
            };
            building.OnDestroy += (not, used) =>
            {
                UpdateCapacityValue();
                UpdateResourceDisplay();

                // re-add a node here
                tileNodeNetwork.AddAndLinkTiledNodeWorld(building.X, building.Y);

            };

                float x, y;
                GetBuildLocationFromCursor(out x, out y);

                building.X = x;
                building.Y = y;
                building.Z = 1;

                building.StartBuilding();

            tileNodeNetwork.RemoveAndUnlinkNode(ref building.Position);


                bool shouldUpdateResources = true;
#if DEBUG

                shouldUpdateResources = Entities.DebuggingVariables.HasInfiniteResources == false;
#endif
                if (shouldUpdateResources)
                {
                    this.Lumber -= buildingType.LumberCost;
                    this.Stone -= buildingType.StoneCost;

                UpdateCapacityValue();

                UpdateResourceDisplay();
            }
        }

        private void UpdateCapacityValue()
        {

            CurrentCapacityUsed = UnitList
                .Select(x => x.UnitData)
                .Where(x => x.IsEnemy == false)
                .Sum(x => x.Capacity); //makes sure we have the correct capacity when units are trained.

            this.MaxCapacity = BuildingList.Sum(item =>
            {
                if (item.IsConstructionComplete)
                {
                    return item.BuildingData.Capacity;
                }
            else
            {
                    return 0;
            }
            });
        }

        private static void GetBuildLocationFromCursor(out float x, out float y)
        {
            var cursor = GuiManager.Cursor;

            x = cursor.WorldXAt(0);
            y = cursor.WorldYAt(0);
            const float tilesWide = 3;

            x = MathFunctions.RoundFloat(x, GridWidth * tilesWide, GridWidth * tilesWide / 2);
            y = MathFunctions.RoundFloat(y, GridWidth * tilesWide, GridWidth * tilesWide / 2);
        }

        public bool CheckIfCanStartTraining(string unit)
        {
            var unitCapacity = GlobalContent.UnitData[unit].Capacity;

            var potentialCapacityUsed = CurrentCapacityUsed + unitCapacity;

            bool canTrain = false;
            if (potentialCapacityUsed <= MaxCapacity)
            {
                canTrain = true;
                currentTrainingCapacity += unitCapacity;

                UpdateResourceDisplay();
            }

#if DEBUG
            if(DebuggingVariables.HasInfiniteResources)
            {
                canTrain = true;
            }
#endif

            return canTrain;
        }

        public void UpdateResourceDisplay()
        {
            this.ResourceDisplayInstance.CapacityText = $"{CurrentCapacityUsed}/{this.MaxCapacity.ToString()}";
            this.ResourceDisplayInstance.LumberText = this.Lumber.ToString();
            this.ResourceDisplayInstance.StoneText = this.Stone.ToString();
            this.ResourceDisplayInstance.GoldText = this.Gold.ToString();

            if (selectedBuilding != null)
            {
                ActionToolbarInstance.UpdateCostUiFromLastRollOver();
            }
        }

        

        private void HandleRaidSpawn(IEnumerable<UnitData> unitDatas, Vector3 spawnPoint)
        {
            List<Unit> newEnemies = new List<Entities.Unit>();
            foreach(var unitData in unitDatas)
            {
                var newEnemy = SpawnNewUnit(unitData.Name, spawnPoint);

                newEnemies.Add(newEnemy);
            }


        }

        public Entities.Unit SpawnNewUnit(string unitDataKey, Vector3 spawnPoint)
        {
            var newUnit = Factories.UnitFactory.CreateNew();
            newUnit.Died += () =>
            {
                UpdateCapacityValue();
                UpdateResourceDisplay();
                UpdateSelectionMarker();
            };

            newUnit.Position = spawnPoint;
            // make it sit above the ground
            newUnit.Z = 1.5f;
            var unitData = GlobalContent.UnitData[unitDataKey];
            newUnit.UnitData = unitData;
            newUnit.AllUnits = this.UnitList;
            newUnit.AllBuildings = this.BuildingList;
            newUnit.NodeNetwork = this.tileNodeNetwork;

            //Capacity is, current units in training + the units which have been trained.
            //Do not count enemy units.
            if (unitData.IsEnemy == false)
            {
                currentTrainingCapacity -= unitData.Capacity;
                CurrentCapacityUsed = UnitList.Select(x => x.UnitData).Where(x => x.IsEnemy == false).Sum(x => x.Capacity); //makes sure we have the correct capacity when units are trained.
                UpdateResourceDisplay();
            }

            if(selectedBuilding != null)
            {
                this.ActionToolbarInstance.UpdateCostUiFromLastRollOver();
            }

            Unit.TryPlaySpawnSound(unitData);

            return newUnit;
        }

        public void CancelLastTrainingInstanceOfUnit(string unitToCancel)
        {
            if (selectedBuilding != null)
            {
                //If the unit was canceled we know we need to upate the gold count.
                //However, we may not have to update the training capacity. So we will pass it in as a ref which has smarter logic to 
                //update it appropriately.
                var wasCancelled = selectedBuilding.CancelLastTrainingInstance(unitToCancel, ref currentTrainingCapacity);
                if (wasCancelled)
                {
                    var unitData = GlobalContent.UnitData[unitToCancel];
                    Gold += unitData.Gold;
                    UpdateResourceDisplay();
                }
            }
        }
        #endregion

        public void TryPlayResourceCollectSound(ResourceType resource, Vector3 soundOrigin)
        {
            SoundEffect soundEffect;
            string soundEffectName;
            switch(resource)
            {
                case ResourceType.Gold:
                    soundEffect = ui_collect_gold_1;
                    soundEffectName = "ui_collect_gold_1";
                    break;
                case ResourceType.Lumber:
                    soundEffect = ui_collect_lumber_1;
                    soundEffectName = "ui_collect_lumber_1";
                    break;
                case ResourceType.Stone:
                    soundEffect = ui_collect_stone_1;
                    soundEffectName = "ui_collect_stone_1";
                    break;
                default:
                    throw new Exception($"Resource type does not have a sound: {resource.ToString()}");
            }
            SoundEffectTracker.TryPlayCameraRestrictedSoundEffect(soundEffect, soundEffectName, Camera.Main.Position, soundOrigin);
        }

        void CustomDestroy()
		{


		}

        static void CustomLoadStaticContent(string contentManagerName)
        {


        }

	}
}
