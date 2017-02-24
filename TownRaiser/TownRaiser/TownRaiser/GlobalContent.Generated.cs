#if ANDROID || IOS
// Android doesn't allow background loading. iOS doesn't allow background rendering (which is used by converting textures to use premult alpha)
#define REQUIRES_PRIMARY_THREAD_LOADING
#endif
using System.Collections.Generic;
using System.Threading;
using FlatRedBall;
using FlatRedBall.Math.Geometry;
using FlatRedBall.ManagedSpriteGroups;
using FlatRedBall.Graphics.Animation;
using FlatRedBall.Graphics.Particle;
using FlatRedBall.AI.Pathfinding;
using FlatRedBall.Utilities;
using BitmapFont = FlatRedBall.Graphics.BitmapFont;
using FlatRedBall.Localization;
using TownRaiser.DataTypes;
using FlatRedBall.IO.Csv;

namespace TownRaiser
{
	public static partial class GlobalContent
	{
		
		public static FlatRedBall.Gum.GumIdb GumProject { get; set; }
		public static Microsoft.Xna.Framework.Graphics.Texture2D MainTileset { get; set; }
		public static System.Collections.Generic.Dictionary<string, TownRaiser.DataTypes.BuildingData> BuildingData { get; set; }
		public static System.Collections.Generic.Dictionary<string, TownRaiser.DataTypes.UnitData> UnitData { get; set; }
		public static Microsoft.Xna.Framework.Graphics.Texture2D CharactersSheet { get; set; }
		public static System.Collections.Generic.List<TownRaiser.DataTypes.TimedSpawnData> TimedSpawnData { get; set; }
		public static FlatRedBall.Graphics.Animation.AnimationChainList GumAnimationChains { get; set; }
		public static System.Collections.Generic.Dictionary<string, TownRaiser.DataTypes.SoundEffectData> SoundEffectData { get; set; }
		[System.Obsolete("Use GetFile instead")]
		public static object GetStaticMember (string memberName)
		{
			switch(memberName)
			{
				case  "GumProject":
					return GumProject;
				case  "MainTileset":
					return MainTileset;
				case  "BuildingData":
					return BuildingData;
				case  "UnitData":
					return UnitData;
				case  "CharactersSheet":
					return CharactersSheet;
				case  "TimedSpawnData":
					return TimedSpawnData;
				case  "GumAnimationChains":
					return GumAnimationChains;
				case  "SoundEffectData":
					return SoundEffectData;
			}
			return null;
		}
		public static object GetFile (string memberName)
		{
			switch(memberName)
			{
				case  "GumProject":
					return GumProject;
				case  "MainTileset":
					return MainTileset;
				case  "BuildingData":
					return BuildingData;
				case  "UnitData":
					return UnitData;
				case  "CharactersSheet":
					return CharactersSheet;
				case  "TimedSpawnData":
					return TimedSpawnData;
				case  "GumAnimationChains":
					return GumAnimationChains;
				case  "SoundEffectData":
					return SoundEffectData;
			}
			return null;
		}
		public static bool IsInitialized { get; private set; }
		public static bool ShouldStopLoading { get; set; }
		const string ContentManagerName = "Global";
		public static void Initialize ()
		{
			
			FlatRedBall.Gum.GumIdb.StaticInitialize("content/gumproject/gumproject.gumx"); FlatRedBall.Gum.GumIdb.RegisterTypes();  FlatRedBall.Gui.GuiManager.BringsClickedWindowsToFront = false;Gum.Wireframe.GraphicalUiElement.ShowLineRectangles = false;
			MainTileset = FlatRedBall.FlatRedBallServices.Load<Microsoft.Xna.Framework.Graphics.Texture2D>(@"content/maintileset.png", ContentManagerName);
			if (BuildingData == null)
			{
				{
					// We put the { and } to limit the scope of oldDelimiter
					char oldDelimiter = FlatRedBall.IO.Csv.CsvFileManager.Delimiter;
					FlatRedBall.IO.Csv.CsvFileManager.Delimiter = ',';
					System.Collections.Generic.Dictionary<string, TownRaiser.DataTypes.BuildingData> temporaryCsvObject = new System.Collections.Generic.Dictionary<string, TownRaiser.DataTypes.BuildingData>();
					FlatRedBall.IO.Csv.CsvFileManager.CsvDeserializeDictionary<string, TownRaiser.DataTypes.BuildingData>("content/globalcontent/buildingdata.csv", temporaryCsvObject);
					FlatRedBall.IO.Csv.CsvFileManager.Delimiter = oldDelimiter;
					BuildingData = temporaryCsvObject;
				}
			}
			if (UnitData == null)
			{
				{
					// We put the { and } to limit the scope of oldDelimiter
					char oldDelimiter = FlatRedBall.IO.Csv.CsvFileManager.Delimiter;
					FlatRedBall.IO.Csv.CsvFileManager.Delimiter = ',';
					System.Collections.Generic.Dictionary<string, TownRaiser.DataTypes.UnitData> temporaryCsvObject = new System.Collections.Generic.Dictionary<string, TownRaiser.DataTypes.UnitData>();
					FlatRedBall.IO.Csv.CsvFileManager.CsvDeserializeDictionary<string, TownRaiser.DataTypes.UnitData>("content/globalcontent/unitdata.csv", temporaryCsvObject);
					FlatRedBall.IO.Csv.CsvFileManager.Delimiter = oldDelimiter;
					UnitData = temporaryCsvObject;
				}
			}
			CharactersSheet = FlatRedBall.FlatRedBallServices.Load<Microsoft.Xna.Framework.Graphics.Texture2D>(@"content/characterssheet.png", ContentManagerName);
			if (TimedSpawnData == null)
			{
				{
					// We put the { and } to limit the scope of oldDelimiter
					char oldDelimiter = FlatRedBall.IO.Csv.CsvFileManager.Delimiter;
					FlatRedBall.IO.Csv.CsvFileManager.Delimiter = ',';
					System.Collections.Generic.List<TownRaiser.DataTypes.TimedSpawnData> temporaryCsvObject = new System.Collections.Generic.List<TownRaiser.DataTypes.TimedSpawnData>();
					FlatRedBall.IO.Csv.CsvFileManager.CsvDeserializeList(typeof(TownRaiser.DataTypes.TimedSpawnData), "content/globalcontent/timedspawndata.csv", temporaryCsvObject);
					FlatRedBall.IO.Csv.CsvFileManager.Delimiter = oldDelimiter;
					TimedSpawnData = temporaryCsvObject;
				}
			}
			GumAnimationChains = FlatRedBall.FlatRedBallServices.Load<FlatRedBall.Graphics.Animation.AnimationChainList>(@"content/globalcontent/gumanimationchains.achx", ContentManagerName);
			if (SoundEffectData == null)
			{
				{
					// We put the { and } to limit the scope of oldDelimiter
					char oldDelimiter = FlatRedBall.IO.Csv.CsvFileManager.Delimiter;
					FlatRedBall.IO.Csv.CsvFileManager.Delimiter = ',';
					System.Collections.Generic.Dictionary<string, TownRaiser.DataTypes.SoundEffectData> temporaryCsvObject = new System.Collections.Generic.Dictionary<string, TownRaiser.DataTypes.SoundEffectData>();
					FlatRedBall.IO.Csv.CsvFileManager.CsvDeserializeDictionary<string, TownRaiser.DataTypes.SoundEffectData>("content/globalcontent/soundeffectdata.csv", temporaryCsvObject);
					FlatRedBall.IO.Csv.CsvFileManager.Delimiter = oldDelimiter;
					SoundEffectData = temporaryCsvObject;
				}
			}
						IsInitialized = true;
			#if DEBUG && WINDOWS
			InitializeFileWatch();
			#endif
		}
		public static void Reload (object whatToReload)
		{
			if (whatToReload == BuildingData)
			{
				FlatRedBall.IO.Csv.CsvFileManager.UpdateDictionaryValuesFromCsv(BuildingData, "content/globalcontent/buildingdata.csv");
			}
			if (whatToReload == UnitData)
			{
				FlatRedBall.IO.Csv.CsvFileManager.UpdateDictionaryValuesFromCsv(UnitData, "content/globalcontent/unitdata.csv");
			}
			if (whatToReload == TimedSpawnData)
			{
				{
					// We put the { and } to limit the scope of oldDelimiter
					char oldDelimiter = FlatRedBall.IO.Csv.CsvFileManager.Delimiter;
					FlatRedBall.IO.Csv.CsvFileManager.Delimiter = ',';
					TimedSpawnData.Clear();
					FlatRedBall.IO.Csv.CsvFileManager.CsvDeserializeList(typeof(TownRaiser.DataTypes.TimedSpawnData), "content/globalcontent/timedspawndata.csv", TimedSpawnData);
					FlatRedBall.IO.Csv.CsvFileManager.Delimiter = oldDelimiter;
				}
			}
			if (whatToReload == SoundEffectData)
			{
				FlatRedBall.IO.Csv.CsvFileManager.UpdateDictionaryValuesFromCsv(SoundEffectData, "content/globalcontent/soundeffectdata.csv");
			}
		}
		#if DEBUG && WINDOWS
		static System.IO.FileSystemWatcher watcher;
		private static void InitializeFileWatch ()
		{
			string globalContent = FlatRedBall.IO.FileManager.RelativeDirectory + "content/globalcontent/";
			if (System.IO.Directory.Exists(globalContent))
			{
				watcher = new System.IO.FileSystemWatcher();
				watcher.Path = globalContent;
				watcher.NotifyFilter = System.IO.NotifyFilters.LastWrite;
				watcher.Filter = "*.*";
				watcher.Changed += HandleFileChanged;
				watcher.EnableRaisingEvents = true;
			}
		}
		private static void HandleFileChanged (object sender, System.IO.FileSystemEventArgs e)
		{
			try
			{
				System.Threading.Thread.Sleep(500);
				var fullFileName = e.FullPath;
				var relativeFileName = FlatRedBall.IO.FileManager.MakeRelative(FlatRedBall.IO.FileManager.Standardize(fullFileName));
				if (relativeFileName == "content/gumproject/gumproject.gumx")
				{
					Reload(GumProject);
				}
				if (relativeFileName == "content/maintileset.png")
				{
					Reload(MainTileset);
				}
				if (relativeFileName == "content/globalcontent/buildingdata.csv")
				{
					Reload(BuildingData);
				}
				if (relativeFileName == "content/globalcontent/unitdata.csv")
				{
					Reload(UnitData);
				}
				if (relativeFileName == "content/characterssheet.png")
				{
					Reload(CharactersSheet);
				}
				if (relativeFileName == "content/globalcontent/timedspawndata.csv")
				{
					Reload(TimedSpawnData);
				}
				if (relativeFileName == "content/globalcontent/gumanimationchains.achx")
				{
					Reload(GumAnimationChains);
				}
				if (relativeFileName == "content/globalcontent/soundeffectdata.csv")
				{
					Reload(SoundEffectData);
				}
			}
			catch{}
		}
		#endif
		
		
	}
}
