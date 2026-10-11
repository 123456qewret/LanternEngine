using System.Runtime.InteropServices;
using System.Windows.Input;

namespace BloodborneLauncher.Core
{
    /// <summary>
    /// One game action the engine binds (bbport.ini bind.&lt;name&gt;): one keyboard key and one mouse
    /// input at most. Pad is the controller button that does it (shown for reference).
    /// </summary>
    public sealed record ControlDef(string Name, string Label, string Group, string Hint, string Default, string? Pad);

    /// <summary>
    /// The engine's PC controls (bbhost input, gpu/shim/input/bindings.cpp): 29 actions, keys by SDL
    /// name ("Left Shift", "Return"), mouse as Mouse1..Mouse5, WheelUp, WheelDown; "none" unbinds.
    /// </summary>
    public static class InputMap
    {
        public static readonly ControlDef[] Controls =
        {
            new("move_forward", "Move forward", "Movement", "", "W", "Left stick"),
            new("move_back", "Move back", "Movement", "", "S", "Left stick"),
            new("move_left", "Move left", "Movement", "", "A", "Left stick"),
            new("move_right", "Move right", "Movement", "", "D", "Left stick"),
            new("walk", "Walk", "Movement", "Hold with the movement keys", "Left Alt", null),
            new("roll", "Dodge / sprint", "Movement", "Tap to dodge, hold to sprint", "Space", "Circle"),
            new("l3", "Left stick click", "Movement", "", "Left Ctrl", "L3"),
            new("attack", "Attack", "Combat", "With the strong modifier held: strong attack", "Mouse1", "R1"),
            new("strong_attack", "Strong attack", "Combat", "Hold to charge", "V", "R2"),
            new("strong_modifier", "Strong attack modifier", "Combat", "Held with Attack", "Left Shift", null),
            new("transform", "Transform weapon", "Combat", "", "C, Mouse3", "L1"),
            new("firearm", "Firearm / parry", "Combat", "", "X, Mouse2", "L2"),
            new("lock_on", "Lock on", "Combat", "Or reset the camera", "Q", "R3"),
            new("blood_vial", "Blood Vial", "Items", "", "R", "Triangle"),
            new("use_item", "Use item", "Items", "The selected quick item", "F", "Square"),
            new("interact", "Interact", "Items", "Talk, examine, pick up, open", "E", "Cross"),
            new("switch_item", "Switch item", "Items", "Cycle the quick items", "Down", "D-pad down"),
            new("switch_right", "Switch right weapon", "Items", "", "Right", "D-pad right"),
            new("switch_left", "Switch left weapon", "Items", "", "Left", "D-pad left"),
            new("dpad_up", "D-pad up", "Items", "", "Up", "D-pad up"),
            new("gestures", "Gestures", "Menus", "", "G", "Touchpad (left)"),
            new("personal_effects", "Personal effects", "Menus", "", "T", "Touchpad (right)"),
            new("menu", "Menu", "Menus", "", "Escape", "Options"),
            new("confirm", "Confirm in menus", "Menus", "", "Return", null),
            new("back", "Back in menus", "Menus", "", "Backspace", null),
            new("look_up", "Look up", "Camera keys", "The camera without the mouse", "I", "Right stick"),
            new("look_down", "Look down", "Camera keys", "", "K", "Right stick"),
            new("look_left", "Look left", "Camera keys", "", "J", "Right stick"),
            new("look_right", "Look right", "Camera keys", "", "L", "Right stick"),
        };

        /// <summary>Keys the port itself uses while the game runs.</summary>
        public static readonly Dictionary<string, string> Reserved = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Insert"] = "Insert opens the port's settings menu",
        };

        public static readonly string[] MouseTokens = { "Mouse1", "Mouse2", "Mouse3", "Mouse4", "Mouse5", "WheelUp", "WheelDown" };

        public static bool IsMouse(string name) => MouseTokens.Contains(name, StringComparer.OrdinalIgnoreCase);

        /// <summary>A friendly name for a chip.</summary>
        public static string Display(string name) => name.ToLowerInvariant() switch
        {
            "mouse1" => "Left click",
            // SDL numbering, as the engine reads it (input.cpp): 2 is the middle button, 3 the right.
            "mouse2" => "Middle click",
            "mouse3" => "Right click",
            "mouse4" => "Mouse 4",
            "mouse5" => "Mouse 5",
            "wheelup" => "Wheel up",
            "wheeldown" => "Wheel down",
            _ => name,
        };

        public static List<string> Split(string value) =>
            value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(n => !n.Equals("none", StringComparison.OrdinalIgnoreCase)).ToList();

        public static string Join(IEnumerable<string> names) => string.Join(", ", names);

        /// <summary>A WPF key as SDL names it (SDL_GetScancodeFromName), or null if SDL has no name for it.</summary>
        public static string? SdlKeyName(Key key)
        {
            if (key >= Key.A && key <= Key.Z) return key.ToString();
            if (key >= Key.D0 && key <= Key.D9) return ((char)('0' + (key - Key.D0))).ToString();
            if (key >= Key.NumPad0 && key <= Key.NumPad9) return "Keypad " + (char)('0' + (key - Key.NumPad0));
            if (key >= Key.F1 && key <= Key.F24) return "F" + (key - Key.F1 + 1);

            return key switch
            {
                Key.Space => "Space",
                Key.Return => "Return",
                Key.Escape => "Escape",
                Key.Back => "Backspace",
                Key.Tab => "Tab",
                Key.CapsLock => "CapsLock",
                Key.LeftShift => "Left Shift",
                Key.RightShift => "Right Shift",
                Key.LeftCtrl => "Left Ctrl",
                Key.RightCtrl => "Right Ctrl",
                Key.LeftAlt => "Left Alt",
                Key.RightAlt => "Right Alt",
                Key.LWin => "Left GUI",
                Key.RWin => "Right GUI",
                Key.Apps => "Application",
                Key.Up => "Up",
                Key.Down => "Down",
                Key.Left => "Left",
                Key.Right => "Right",
                Key.Insert => "Insert",
                Key.Delete => "Delete",
                Key.Home => "Home",
                Key.End => "End",
                Key.PageUp => "PageUp",
                Key.PageDown => "PageDown",
                Key.PrintScreen => "PrintScreen",
                Key.Scroll => "ScrollLock",
                Key.Pause => "Pause",
                Key.NumLock => "Numlock",
                Key.Divide => "Keypad /",
                Key.Multiply => "Keypad *",
                Key.Subtract => "Keypad -",
                Key.Add => "Keypad +",
                Key.Decimal => "Keypad .",
                Key.OemMinus => "-",
                Key.OemPlus => "=",
                Key.OemOpenBrackets => "[",
                Key.OemCloseBrackets => "]",
                Key.OemPipe => "\\",
                Key.OemSemicolon => ";",
                Key.OemQuotes => "'",
                Key.OemTilde => "`",
                Key.OemComma => ",",
                Key.OemPeriod => ".",
                Key.OemQuestion => "/",
                Key.OemBackslash => "NonUSBackslash",
                _ => null,
            };
        }

        public static string? MouseToken(MouseButton button) => button switch
        {
            MouseButton.Left => "Mouse1",
            MouseButton.Middle => "Mouse2",
            MouseButton.Right => "Mouse3",
            MouseButton.XButton1 => "Mouse4",
            MouseButton.XButton2 => "Mouse5",
            _ => null,
        };

        private static readonly Lazy<HashSet<string>> KeyNames = new(() =>
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (Key key in Enum.GetValues(typeof(Key)))
                if (SdlKeyName(key) is { } name) names.Add(name);
            return names;
        });

        /// <summary>A key or mouse name in the engine's spelling, or null if it is not one.</summary>
        public static string? Canonical(string name)
        {
            name = name.Trim();
            if (MouseTokens.FirstOrDefault(t => t.Equals(name, StringComparison.OrdinalIgnoreCase)) is { } mouse) return mouse;
            return KeyNames.Value.FirstOrDefault(k => k.Equals(name, StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <summary>The bindings in bbport.ini (bind.&lt;action&gt;), as the engine and its own menu keep them.</summary>
    public static class Bindings
    {
        public static string IniKey(ControlDef c) => "bind." + c.Name;

        public static List<string> Get(IniFile ini, ControlDef c) => InputMap.Split(ini.Raw(IniKey(c)) ?? c.Default);

        public static bool IsDefault(IniFile ini, ControlDef c) =>
            Same(Get(ini, c), InputMap.Split(c.Default));

        private static bool Same(List<string> a, List<string> b) =>
            a.Count == b.Count && a.Zip(b).All(p => p.First.Equals(p.Second, StringComparison.OrdinalIgnoreCase));

        /// <summary>One key and one mouse input at most, key first; written out explicitly ("none" unbinds).</summary>
        public static void Set(IniFile ini, ControlDef c, IEnumerable<string> names)
        {
            var list = names.ToList();
            var key = list.LastOrDefault(n => !InputMap.IsMouse(n));
            var mouse = list.LastOrDefault(InputMap.IsMouse);
            var value = InputMap.Join(new[] { key, mouse }.Where(n => n != null)!);
            ini.Set(IniKey(c), value.Length == 0 ? "none" : value);
        }

        /// <summary>A new input for an action: replaces the one of the same kind (key or mouse), or all.</summary>
        public static void Assign(IniFile ini, ControlDef c, string name, bool keepOtherKind)
        {
            var current = keepOtherKind ? Get(ini, c).Where(n => InputMap.IsMouse(n) != InputMap.IsMouse(name)) : Enumerable.Empty<string>();
            Set(ini, c, current.Append(name));
        }

        public static void ResetAll(IniFile ini)
        {
            foreach (var c in InputMap.Controls)
                ini.Set(IniKey(c), c.Default);
        }

        public static Dictionary<string, string> Current(IniFile ini) =>
            InputMap.Controls.ToDictionary(c => c.Name, c => InputMap.Join(Get(ini, c)));
    }

    /// <summary>XInput: enough to show which Xbox-compatible pads are connected.</summary>
    public static class XInput
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct State
        {
            public uint PacketNumber;
            public ushort Buttons;
            public byte LeftTrigger, RightTrigger;
            public short ThumbLX, ThumbLY, ThumbRX, ThumbRY;
        }

        [DllImport("xinput1_4.dll", EntryPoint = "XInputGetState")]
        private static extern uint GetState(uint user, out State state);

        /// <summary>Slots 1-4 with a pad connected.</summary>
        public static List<int> Connected()
        {
            var slots = new List<int>();
            try
            {
                for (uint i = 0; i < 4; i++)
                    if (GetState(i, out _) == 0)
                        slots.Add((int)i + 1);
            }
            catch
            {
                // No XInput on this system.
            }
            return slots;
        }
    }
}
