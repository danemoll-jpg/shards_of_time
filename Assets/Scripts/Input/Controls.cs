using UnityEngine;
using UnityEngine.InputSystem;

namespace ShardsOfTime
{
    // All input goes through one Input Actions asset (Resources/Input/Controls.inputactions): map "Player" for play, map "UI" for menus.
    // LastDevice follows the device the player touched last, so prompts name only the buttons of the device in use (Btn).
    public static class Controls
    {
        public enum Device { Gamepad, Keyboard }
        public enum Act { Move, Look, Jump, Attack, Dive, SwitchPrev, SwitchNext, PickDan, PickAbi, PickKira, Recenter, Pause, Skip, Submit, Cancel, Navigate }

        public static InputActionAsset Asset { get; private set; }
        public static InputAction Move, Look, Jump, Attack, Dive, SwitchPrev, SwitchNext, PickDan, PickAbi, PickKira, Recenter, Pause, Skip;
        public static InputAction Navigate, Submit, Cancel;
        public static Device LastDevice { get; private set; } = Device.Gamepad;

        public static void Init()
        {
            if (Asset) return;
            Asset = Object.Instantiate(Resources.Load<InputActionAsset>("Input/Controls"));
            var p = Asset.FindActionMap("Player"); var u = Asset.FindActionMap("UI");
            Move = p["Move"]; Look = p["Look"]; Jump = p["Jump"]; Attack = p["Attack"]; Dive = p["Dive"];
            SwitchPrev = p["SwitchPrev"]; SwitchNext = p["SwitchNext"]; PickDan = p["PickDan"]; PickAbi = p["PickAbi"]; PickKira = p["PickKira"];
            Recenter = p["Recenter"]; Pause = p["Pause"]; Skip = p["Skip"];
            Navigate = u["Navigate"]; Submit = u["Submit"]; Cancel = u["Cancel"];
            Asset.Enable();
            LastDevice = Gamepad.all.Count > 0 ? Device.Gamepad : Device.Keyboard;
            InputSystem.onActionChange += (o, change) =>
            {
                if (change != InputActionChange.ActionPerformed || !(o is InputAction a) || a.activeControl == null) return;
                var d = a.activeControl.device;
                if (d is Gamepad) LastDevice = Device.Gamepad;
                else if (d is Keyboard || (d is Mouse && a != Look)) LastDevice = Device.Keyboard;
                else if (d is Mouse && a == Look && a.ReadValue<Vector2>().sqrMagnitude > 25) LastDevice = Device.Keyboard;
            };
        }

        // The button name for the device in use (Xbox layout on a pad: the Steam Deck's buttons have the same names).
        public static string Btn(Act a) => Btn(a, LastDevice);
        public static string Btn(Act a, Device d)
        {
            bool pad = d == Device.Gamepad;
            switch (a)
            {
                case Act.Move: return pad ? "Left stick" : "W A S D";
                case Act.Look: return pad ? "Right stick" : "Mouse";
                case Act.Jump: return pad ? "A" : "Space";
                case Act.Attack: return pad ? "X" : "Left click / F";
                case Act.Dive: return pad ? "B" : "Ctrl / C";
                case Act.SwitchPrev: return pad ? "LB" : "Q";
                case Act.SwitchNext: return pad ? "RB" : "E";
                case Act.PickDan: return pad ? "LB / RB" : "1";
                case Act.PickAbi: return pad ? "LB / RB" : "2";
                case Act.PickKira: return pad ? "LB / RB" : "3";
                case Act.Recenter: return pad ? "Right stick click" : "R / middle click";
                case Act.Pause: return pad ? "Menu" : "Esc";
                case Act.Skip: return pad ? "A" : "Space";
                case Act.Submit: return pad ? "A" : "Enter";
                case Act.Cancel: return pad ? "B" : "Esc";
                case Act.Navigate: return pad ? "D-pad / left stick" : "Arrow keys / W S";
            }
            return a.ToString();
        }
    }
}
