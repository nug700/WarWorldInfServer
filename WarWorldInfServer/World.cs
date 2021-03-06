using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using WarWorldInfinity.LibNoise;

namespace WarWorldInfinity
{
	public class World
	{
		public struct WorldConfigSave
		{
			public string version;
			public Time time;
			public TerrainBuilder.TerrainSettings terrain;
            public Alliance.AllianceSave[] alliances;
		}

		public struct Time
		{
			public int seconds;
			public int minutes;
			public int hours;
			public int days;
			public int tick;
			public int secondsInTicks;
			public int maxSecondsInTicks;

			public Time(int seconds, int minutes, int hours, int days, int tick, int secondsInTicks, int maxSecondsInTicks){
				this.seconds = seconds;
				this.minutes = minutes;
				this.hours = hours;
				this.days = days;
				this.tick = tick;
				this.secondsInTicks = secondsInTicks;
				this.maxSecondsInTicks = maxSecondsInTicks;
			}

			public Time(SaveVersions.Version_Current.Time time){
				this.seconds = time.seconds;
				this.minutes = time.minutes;
				this.hours = time.hours;
				this.days = time.days;
				this.tick = time.tick;
				this.secondsInTicks = time.secondsInTicks;
				this.maxSecondsInTicks = time.maxSecondsInTicks;
			}
		}

		//private string _worldName;
		//private string _worldDirectory;
		private WorldManager _worldManager;
		//private Time _time;
		//private TerrainBuilder _terrain;

		public string WorldName { get; private set; }
		public string WorldDirectory { get; private set; }
		public Time WorldStartTime{ get; private set; }
		public TerrainBuilder Terrain { get; private set; }

		public World (WorldManager worldManager)
		{
			_worldManager = worldManager;
        }

		public World CreateNewWorld(string worldName){
			string stage = "stage1";
			try {
                Logger.Log("Creating new world '{0}'.", worldName);
				WorldDirectory = _worldManager.MainWorldDirectory + worldName + GameServer.sepChar;
				if (!Directory.Exists (WorldDirectory))
					Directory.CreateDirectory (WorldDirectory);
				_worldManager.AddWorldDirectory (worldName, WorldDirectory);
                _worldManager.CurrentWorldDirectory = WorldDirectory;
				
				IModule module = new Perlin ();
				((Perlin)module).OctaveCount = 16;
				((Perlin)module).Seed = AppSettings.TerrainSeed;
				Terrain = new TerrainBuilder (AppSettings.TerrainWidth, AppSettings.TerrainHeight, AppSettings.TerrainSeed);
				Terrain.Generate (module, AppSettings.TerrainPreset);
				Terrain.Save(WorldDirectory + AppSettings.TerrainImageFile);
				Terrain.SaveModule(WorldDirectory + AppSettings.TerrainModuleFile);
                WorldName = worldName;
				WorldConfigSave worldSave = new WorldConfigSave ();
				WorldStartTime = new Time (0, 0, 0, 0, 0, 0, AppSettings.SecondsInTicks);
				worldSave.version = GameServer.Instance.Version;
				worldSave.time = WorldStartTime;
				worldSave.terrain = Terrain.Settings;
                worldSave.alliances = GameServer.Instance.Alliances.Save();
				FileManager.SaveConfigFile(WorldDirectory + AppSettings.WorldSaveFile, worldSave, false);
				GameServer.Instance.Users.Save(WorldDirectory + "Users" + GameServer.sepChar);
				
				Logger.Log ("World \"{0}\" created.", worldName);
				GameServer.Instance.StartWorld (this);
			}
			catch (JsonSerializationException e){
				Logger.LogError("json serialization failed: {0}", stage);
				Logger.LogError(e.StackTrace);
			}
			catch (Exception e){
				Logger.LogError("{0}: {1}\n{2}", e.GetType(), e.Message, e.StackTrace);
			}
			return this;
		}

		public World LoadWorld(string worldName){
			if (_worldManager.WorldExists (worldName)) {
                WorldDirectory = _worldManager.GetWorldDirectory(worldName);
                GameServer.Instance.Worlds.CurrentWorldDirectory = WorldDirectory;
                WorldName = worldName;
                WorldConfigSave worldSave = FileManager.LoadObject<WorldConfigSave>(WorldDirectory + AppSettings.WorldSaveFile, false);
                WorldStartTime = worldSave.time;
                Terrain = new TerrainBuilder(worldSave.terrain);
                GameServer.Instance.Users.LoadUsers(WorldDirectory + "Users" + GameServer.sepChar);
                GameServer.Instance.Alliances.Load(worldSave.alliances);
                GameServer.Instance.Structures.Load();

                Logger.Log("World \"{0}\" loaded.", worldName);
                GameServer.Instance.StartWorld(this);

            } else {
				Logger.LogWarning ("Created world \"{0}\" as it does not exists.", worldName);
				CreateNewWorld(worldName);
			}

			return this;
		}

		public void Save(string worldName){
			WorldDirectory = _worldManager.MainWorldDirectory + worldName + "/";

			WorldConfigSave worldSave = new WorldConfigSave ();
			GameTimer timer = GameServer.Instance.GameTime;
			worldSave.version = GameServer.Instance.Version;
			worldSave.time = new Time (timer.TotalSeconds, timer.Minutes, timer.Hours, timer.Days, timer.Tick, timer.SecondsInTick, timer.MaxSecondsInTick );
			worldSave.terrain = Terrain.Settings;
            worldSave.alliances = GameServer.Instance.Alliances.Save();
            FileManager.SaveConfigFile (WorldDirectory + AppSettings.WorldSaveFile, worldSave, false);

			if (!File.Exists(WorldDirectory + AppSettings.TerrainImageFile))
				Terrain.Save (WorldDirectory + AppSettings.TerrainImageFile);
			GameServer.Instance.Users.Save (WorldDirectory + "Users" + GameServer.sepChar);
			
			Logger.Log ("World \"{0}\" saved.", worldName);
		}
	}
}

