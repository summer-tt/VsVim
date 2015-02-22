﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using EnvDTE;
using EditorUtils;
using DteCommand = EnvDTE.Command;

namespace Vim.VisualStudio
{
    /// <summary>
    /// Snapshot of the state of the DTE.Commands and their KeyBindings
    /// </summary>
    public sealed class CommandListSnapshot
    {
        private struct CommandData
        {
            internal DteCommand Command;
            internal ReadOnlyCollection<CommandKeyBinding> CommandKeyBindings;
        }

        private readonly Dictionary<CommandId, CommandData> _commandMap = new Dictionary<CommandId, CommandData>();
        private readonly ReadOnlyCollection<CommandKeyBinding> _commandKeyBindings;
        private readonly ReadOnlyCollection<string> _scopes;

        public ReadOnlyCollection<CommandKeyBinding> CommandKeyBindings
        {
            get { return _commandKeyBindings; }
        }

        public ReadOnlyCollection<string> Scopes
        {
            get { return _scopes; }
        }

        public IEnumerable<KeyBinding> KeyBindings
        {
            get { return _commandKeyBindings.Select(x => x.KeyBinding); }
        }

        public CommandListSnapshot(_DTE dte) : this(dte.Commands.GetCommands())
        {
        }

        public CommandListSnapshot(IEnumerable<DteCommand> commands)
        {
            var list = new List<CommandKeyBinding>();
            var scopes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var command in commands)
            {
                CommandId commandId;
                if (!command.TryGetCommandId(out commandId))
                {
                    continue;
                }

                var commandKeybindings = command.GetCommandKeyBindings().ToReadOnlyCollection();
                var commandData = new CommandData()
                {
                    Command = command,
                    CommandKeyBindings = commandKeybindings
                };

                _commandMap[commandId] = commandData;

                foreach (var commandKeyBinding in commandData.CommandKeyBindings)
                {
                    list.Add(commandKeyBinding);
                    scopes.Add(commandKeyBinding.KeyBinding.Scope);
                }
            }

            _commandKeyBindings = list.ToReadOnlyCollectionShallow();
            _scopes = scopes.ToReadOnlyCollection();
        }

        /// <summary>
        /// Is the specified command active with the given binding
        /// </summary>
        public bool IsActive(CommandKeyBinding commandKeyBinding)
        {
            CommandData commandData;
            if (!_commandMap.TryGetValue(commandKeyBinding.Id, out commandData))
            {
                return false;
            }

            return commandData.CommandKeyBindings.Contains(commandKeyBinding);
        }

        public bool TryGetCommand(CommandId id, out DteCommand command)
        {
            ReadOnlyCollection<CommandKeyBinding> bindings;
            return TryGetCommandData(id, out command, out bindings);
        }

        public bool TryGetCommandKeyBindings(CommandId id, out ReadOnlyCollection<CommandKeyBinding> bindings)
        {
            DteCommand command;
            return TryGetCommandData(id, out command, out bindings);
        }

        public bool TryGetCommandData(CommandId id, out DteCommand command, out ReadOnlyCollection<CommandKeyBinding> bindings)
        {
            CommandData commandData;
            if (!_commandMap.TryGetValue(id, out commandData))
            {
                command = null;
                bindings = null;
                return false;
            }

            command = commandData.Command;
            bindings = commandData.CommandKeyBindings;
            return true;
        }
    }
}
