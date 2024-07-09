﻿using HandheldCompanion.Actions;
using HandheldCompanion.Misc;
using HandheldCompanion.Utils;
using HandheldCompanion.Views;
using HandheldCompanion.Views.Windows;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace HandheldCompanion.Commands.Functions.Windows
{
    [Serializable]
    public class OnScreenKeyboardCommands : FunctionCommands
    {
        public OnScreenKeyboardCommands()
        {
            Name = Properties.Resources.Hotkey_Keyboard;
            Description = Properties.Resources.Hotkey_KeyboardDesc;
            Glyph = "\uE765";
            OnKeyUp = true;
        }

        public override void Execute(bool IsKeyDown, bool IsKeyUp)
        {
            OnScreenKeyboard.ToggleVisibility();

            base.Execute(IsKeyDown, IsKeyUp);
        }

        public override object Clone()
        {
            OnScreenKeyboardCommands commands = new();
            commands.commandType = commandType;
            commands.Name = Name;
            commands.Description = Description;
            commands.Glyph = Glyph;
            commands.OnKeyUp = OnKeyUp;
            commands.OnKeyDown = OnKeyDown;

            return commands;
        }
    }
}
