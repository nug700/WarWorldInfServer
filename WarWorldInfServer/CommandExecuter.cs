using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Reflection;

namespace WarWorldInfServer
{
	public class CommandExecuter
	{
		public delegate object CommandFunction(params string[] args);

		public struct CommandDescription{
			public string command;
			public string command_args;
			public string description_small;
			public string description_Long;
			public CommandExecuter.CommandFunction callback;

			public CommandDescription(string _command, string _command_args, string _description_small, string _description_Long, string _callback) {
				command = _command.ToLower();
				command_args = _command_args;
				description_small = _description_small;
				description_Long = _description_Long;
				callback = GetCallbackFromString(_command, _callback);
			}

			public CommandDescription(string _plugin, string _command, string _command_args, string _description_small, string _description_Long, CommandExecuter.CommandFunction _callback) {
				command = _command.ToLower();
				command_args = _command_args;
				description_small = _description_small;
				description_Long = _description_Long;
				callback = _callback;
			}

			public CommandDescription(string _command, string _command_args, string _description_small, string _callback) {
				command = _command.ToLower();
				command_args = _command_args;
				description_small = _description_small;
				description_Long = string.Empty;
				callback = GetCallbackFromString(_command, _callback);
			}

			public CommandDescription(string _command, string _command_args, string _description_small, CommandExecuter.CommandFunction _callback) {
				command = _command.ToLower();
				command_args = _command_args;
				description_small = _description_small;
				description_Long = string.Empty;
				callback = _callback;
			}

			private static CommandExecuter.CommandFunction GetCallbackFromString(string command, string callback){
				CommandExecuter.CommandFunction result = null;
				MethodInfo methodInf = typeof(CommandExecuter).GetMethod (callback, 
					BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
				if (methodInf != null) {
					object typeInstance = GameServer.Instance.CommandExec;
					if (methodInf.IsStatic) {
						result = (CommandExecuter.CommandFunction)Delegate.CreateDelegate (typeof(CommandExecuter.CommandFunction), methodInf);
					} else
						result = (CommandExecuter.CommandFunction)Delegate.CreateDelegate (typeof(CommandExecuter.CommandFunction), typeInstance, methodInf);
				} else
					Logger.LogError ("Could not find function {0}.", callback);
				return result;
			}
		}

		private Dictionary<string, CommandFunction> _cmdTable = new Dictionary<string, CommandFunction>();
		private Dictionary<string, CommandDescription> _cmdDescription = new Dictionary<string, CommandDescription>();

		private string _input = string.Empty;
		private GameServer _server;
		private int padAmount = 0;

		public CommandExecuter ()
		{
			_server = GameServer.Instance;
		}

		public void StartCommandLoop(){
			LoadCommands ();
			Logger.Log ("Command Executer initialized.");

			while (_server.Running) {
				ConsoleKeyInfo key = Console.ReadKey(true);
				if (key.Key == ConsoleKey.Backspace){
					if (_input.Length > 0)
						_input = _input.Remove(_input.Length - 1);
				}
				else if (key.Key == ConsoleKey.Enter){
					string tmpStr = _input;
					_input = string.Empty;
					ExecuteCommand(tmpStr);
				}
				else{
					_input += key.KeyChar;
				}
				Logger.InputStr = _input;
			}
		}

		public void LoadCommands(){
			CommandDescription[] commands = ConfigParser.GetCommands (GameServer.Instance.AppDirectory + "Commands.ini");
			for (int i = 0; i < commands.Length; i++) {
				RegisterCommand(commands[i]);
				if (commands[i].command.Length > padAmount)
					padAmount = commands[i].command.Length;
			}
		}

		public void RegisterCommand(CommandDescription command){
			_cmdTable [command.command.ToLower ()] = command.callback;
			_cmdDescription [command.command.ToLower ()] = command;
		}

		public void UnregisterCommand(string commandString){
			_cmdTable.Remove (commandString.ToLower ());
			_cmdDescription.Remove (commandString.ToString ());
		}

		public string[] Commands(){
			string[] commands = new string[_cmdTable.Keys.Count];
			_cmdTable.Keys.CopyTo (commands, 0);
			return commands;
		}

		private void ExecuteCommand(string command){
			Logger.Print ("> {0}", command);
			command = command.Trim ();
			if (!string.IsNullOrEmpty (command)) {
				string[] args = command.Split (new[]{' '}, System.StringSplitOptions.RemoveEmptyEntries);
				string cmd = args [0].ToLower ();
				if (_cmdTable.ContainsKey (cmd)) {
					string result = _cmdTable [cmd] (args).ToString ();
					if (result != string.Empty)
						Logger.Print (result);
				} else
					Logger.LogError ("Command not found: {0}", args [0]);
			}
		}

		// command functions

		private object Runtime_CMD(params string[] args){
			if (_server.WorldLoaded)
				return _server.GameTime.GetRuntime();
			Logger.LogWarning ("World not loaded.");
			return string.Empty;
		}

		private object Time_CMD(params string[] args){
			Logger.Log (GameTimer.GetDateTime ());
			return string.Empty;
		}

		private object Create_CMD(params string[] args){
			if (args.Length == 2){
				_server.WorldMnger.CreateWorld(args[1]);
			}
			return string.Empty;
		}

		private object Load_CMD(params string[] args){
			if (args.Length == 2){
				_server.WorldMnger.LoadWorld(args[1]);
			}
			return string.Empty;
		}

		private object Save_CMD(params string[] args){
			if (args.Length == 1)
				_server.WorldMnger.SaveCurrentWorld();
			else if (args.Length == 2)
				_server.WorldMnger.SaveWorld(args[1]);
			return string.Empty;
		}

		private object Clear_CMD(params string[] args){
			Logger.Clear ();
			return string.Empty;
		}

		private object Exit_CMD(params string[] args){
			if (_server.WorldLoaded)
				_server.WorldMnger.SaveCurrentWorld ();
			_server.Exit ();
			return "Good bye...";
		}

		private object Help_CMD(params string[] args){
			StringBuilder output = new StringBuilder ();
			if (args.Length == 1) {
				output.AppendLine(string.Format("Commands ({0}):", _cmdTable.Count.ToString()));
				foreach (string key in _cmdTable.Keys) {
					output.AppendLine (string.Format ("  {0} : {1}", key.PadRight(padAmount, ' '), _cmdDescription [key].description_small));
				}
			} else if (args.Length == 2) {
				if (_cmdTable.ContainsKey (args [1])) {
					output.AppendLine(string.Format(" - Command: {0} {1}", _cmdDescription [args [1]].command, _cmdDescription [args [1]].command_args));
					output.AppendLine(string.Format (" - Short description: {0}", _cmdDescription [args [1]].description_small));
					if (!string.IsNullOrEmpty(_cmdDescription [args [1]].description_Long))
						output.AppendLine( string.Format(" - Long description: {0}", _cmdDescription [args [1]].description_Long));
				} else
					Logger.LogError ("Command \"{0}\" not found", args [1]);
			} else {
				Logger.LogError("To many arguments.");
			}
			return output.ToString();
		}
	}
}

