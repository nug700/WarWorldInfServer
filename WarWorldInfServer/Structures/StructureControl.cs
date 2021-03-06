﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WarWorldInfinity.Shared;
using WarWorldInfinity.Units;

namespace WarWorldInfinity.Structures {
    public class StructureControl {
        private Dictionary<Vector2Int, Structure> _structures;
        private Dictionary<Vector2Int, Structure> _changedLastTick;
        private Dictionary<Vector2Int, Structure> _changedThisTick;
        private Dictionary<string, List<string>> _structureCommands;

        public StructureControl() {
            _structures = new Dictionary<Vector2Int, Structure>();
            _changedLastTick = new Dictionary<Vector2Int, Structure>();
            _changedThisTick = new Dictionary<Vector2Int, Structure>();
            _structureCommands = new Dictionary<string, List<string>>();
            _structureCommands.Add(Structure.StructureType.None.ToString(), new List<string>());
            _structureCommands.Add(Structure.StructureType.Outpost.ToString(), new List<string>());
            _structureCommands.Add(Structure.StructureType.City.ToString(), new List<string>());
            _structureCommands.Add(Structure.StructureType.Radar.ToString(), new List<string>());

            _structureCommands[Structure.StructureType.Outpost.ToString()].Add("Test");
        }

        public void TickUpdate() {
            _changedLastTick.Clear();
            foreach (Structure op in new List<Structure>(_changedThisTick.Values)) {
                _changedLastTick.Add(op.Location, op);
            }
            _changedThisTick.Clear();
            foreach (Structure op in new List<Structure>(_structures.Values)) {
                op.TickUpdate();
            }
        }

        public void AddStructure(Structure OP, bool updateImmediately) {
            if (!OpExists(OP.Location)) {
                _structures.Add(OP.Location, OP);
                if (!_changedThisTick.ContainsKey(OP.Location))
                    _changedThisTick.Add(OP.Location, OP);
                else {
                    _changedThisTick[OP.Location] = OP;
                }
            }
        }

        public void SetStructure(Structure OP) {
            if (OpExists(OP.Location)) {
                _structures[OP.Location] = OP;
                if (!_changedThisTick.ContainsKey(OP.Location))
                    _changedThisTick.Add(OP.Location, OP);
                else {
                    _changedThisTick[OP.Location] = OP;
                }
                Logger.Log("structure upgraded to " + OP.Type);
            }
        }

        public void RemoveOutpost(Vector2Int position) {
            if (OpExists(position)) {
                Squad[] units = _structures[position].Squads.ToArray();
                User user = _structures[position].Owner;
                _structures[position].Destroy();
                _structures.Remove(position);
                if (units.Length > 0)
                    user.CreateStructure(position, Structure.StructureType.None, true).SetSquads(units);
                if (_changedLastTick.ContainsKey(position))
                    _changedLastTick.Remove(position);
                if (_changedThisTick.ContainsKey(position))
                    _changedThisTick.Remove(position);
            }
        }

        public Dictionary<Vector2Int, Structure> GetStructures(Vector2Int[] locations) {
            Dictionary<Vector2Int, Structure> result = new Dictionary<Vector2Int, Structure>();
            for (int i = 0; i < locations.Length; i++) {
                if (OpExists(locations[i])) {
                    result.Add(locations[i], _structures[locations[i]]);
                }
            }
            return result;
        }

        public bool CanCreateStructure(Vector2Int location) {
            Vector2Int topLeft = new Vector2Int(location.x - AppSettings.MinOpDistance, location.y + AppSettings.MinOpDistance);
            Vector2Int bottomRight = new Vector2Int(location.x + AppSettings.MinOpDistance, location.y - AppSettings.MinOpDistance);
            Vector2Int[] nearOps = GetStructureLocations(topLeft, bottomRight);
            int opsInCircle = RadarUtility.GetVisibleObjects(new RadarUtility.RadarData(location, AppSettings.MinOpDistance), nearOps).Length;
            return opsInCircle == 0;
        }

        public Vector2Int[] GetStructureLocations(Vector2Int topLeft, Vector2Int bottomRight) {
            List<Vector2Int> result = new List<Vector2Int>();
            foreach (Vector2Int position in _structures.Keys) {
                if (position.x >= topLeft.x && position.x <= bottomRight.x &&
                    position.y <= topLeft.y && position.y >= bottomRight.y) {
                    result.Add(position);
                }
            }
            return result.ToArray();
        }

        public Structure[] GetStructures(Vector2Int topLeft, Vector2Int bottomRight) {
            List<Structure> result = new List<Structure>();
            foreach (Vector2Int position in _structures.Keys) {
                if (position.x >= topLeft.x && position.x <= bottomRight.x &&
                    position.y <= topLeft.y && position.y >= bottomRight.y) {
                    result.Add(_structures[position]);
                }
            }
            return result.ToArray();
        }

        public Vector2Int[] GetStructureLocation() {
            return _structures.Keys.ToArray();
        }

        public Structure[] GetStructures() {
            return _structures.Values.ToArray();
        }

        public Structure GetStructure(Vector2Int location) {
            if (_structures.ContainsKey(location))
                return _structures[location];
            return null;
        }

        public Vector2Int[] GetLastTickStructureLocations(Vector2Int topLeft, Vector2Int bottomRight) {
            List<Vector2Int> result = new List<Vector2Int>();
            foreach (Vector2Int position in _changedLastTick.Keys) {
                if (position.x >= topLeft.x && position.x <= bottomRight.x &&
                    position.y <= topLeft.y && position.y >= bottomRight.y) {
                    result.Add(position);
                }
            }
            return result.ToArray();
        }

        public Structure[] GetLastTickStructures(Vector2Int topLeft, Vector2Int bottomRight) {
            List<Structure> result = new List<Structure>();
            foreach (Vector2Int position in _changedLastTick.Keys) {
                if (position.x >= topLeft.x && position.x <= bottomRight.x &&
                    position.y <= topLeft.y && position.y >= bottomRight.y) {
                    result.Add(_changedLastTick[position]);
                }
            }
            return result.ToArray();
        }

        public Vector2Int[] GetLastTickStructureLocation() {
            return _changedLastTick.Keys.ToArray();
        }

        public Structure[] GetLastTickStructures() {
            return _changedLastTick.Values.ToArray();
        }

        public Vector2Int[] GetThisTickStructureLocations(Vector2Int topLeft, Vector2Int bottomRight) {
            List<Vector2Int> result = new List<Vector2Int>();
            foreach (Vector2Int position in _changedThisTick.Keys) {
                if (position.x >= topLeft.x && position.x <= bottomRight.x &&
                    position.y <= topLeft.y && position.y >= bottomRight.y) {
                    result.Add(position);
                }
            }
            return result.ToArray();
        }

        public Structure[] GetThisTickStructures(Vector2Int topLeft, Vector2Int bottomRight) {
            List<Structure> result = new List<Structure>();
            foreach (Vector2Int position in _changedThisTick.Keys) {
                if (position.x >= topLeft.x && position.x <= bottomRight.x &&
                    position.y <= topLeft.y && position.y >= bottomRight.y) {
                    result.Add(_changedThisTick[position]);
                }
            }
            return result.ToArray();
        }

        public Vector2Int[] GetThisTickStructureLocation() {
            return _changedThisTick.Keys.ToArray();
        }

        public Structure[] GetThisTickStructures() {
            return _changedThisTick.Values.ToArray();
        }

        public CommandList[] GetCommands() {
            List<CommandList> commands = new List<CommandList>();
            foreach(string type in _structureCommands.Keys) {
                commands.Add(new CommandList(type, _structureCommands[type].ToArray()));
            }
            return commands.ToArray();
        }

       public bool ChangedLastTick(Vector2Int location) {
            return _changedLastTick.ContainsKey(location);
        }

        public bool ChangedThisTick(Vector2Int location) {
            return _changedThisTick.ContainsKey(location);
        }

        public bool OpExists(Vector2Int position) {
            return _structures.ContainsKey(position);
        }

        public void Load() {
            foreach (Structure str in _structures.Values) {
                str.PostLoad();
            }
        }
    }
}
