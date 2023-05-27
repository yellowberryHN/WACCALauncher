using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Text;
using System.Timers;
using System.Diagnostics;
using IniParser;
using IniParser.Model;
using SharpDX.DirectInput;
using System.Collections;
using System.Linq;
using IniParser.Exceptions;
using System.Reflection;

namespace WACCALauncher
{
    public partial class MainForm : Form
    {
        private static System.Timers.Timer _delayTimer;
        private readonly System.Windows.Forms.Timer _t = new System.Windows.Forms.Timer();

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        private static extern IntPtr AddFontMemResourceEx(IntPtr pbFont, uint cbFont,
            IntPtr pdv, [System.Runtime.InteropServices.In] ref uint pcFonts);

        private readonly PrivateFontCollection _fonts = new PrivateFontCollection();
        private Label _loadingLabel;
        private Label _versionLabel;

        private Font _menuFont;

        private readonly DirectInput _input = new DirectInput();
        private Joystick _ioBoard;

        public readonly List<Version> Versions = new List<Version>();
        public Version DefaultVer;

        private readonly IniData _config;
        private readonly FileIniDataParser _parser = new FileIniDataParser();

        private readonly Process _gameProcess = new Process();
        private bool _gameRunning = false;

        public MainForm()
        {
            InitializeComponent();

            _t.Tick += Tick;
            _t.Interval = 20;
            _t.Start();

            _delayTimer = new System.Timers.Timer(5000);
            _delayTimer.Elapsed += LaunchDefault;

            _delayTimer.Enabled = true;

            // Load embedded font into memory

            LoadFont();

            try
            {
                _config = _parser.ReadFile("wacca.ini");
            }
            catch (IniParser.Exceptions.ParsingException)
            {
                DisplayError("Config error", "wacca.ini could not be read, check for errors");
            }

            if (!Program.IsCorrectRes()) return;
            var bounds = Program.CurrentScreen.Bounds;
            this.SetBounds(bounds.X, bounds.Y + 362, Width, Height);
        }

        private void LoadFont()
        {
            var fontData = Properties.Resources.menufont;
            var fontPtr = System.Runtime.InteropServices.Marshal.AllocCoTaskMem(fontData.Length);
            System.Runtime.InteropServices.Marshal.Copy(fontData, 0, fontPtr, fontData.Length);
            uint dummy = 0;
            _fonts.AddMemoryFont(fontPtr, Properties.Resources.menufont.Length);
            AddFontMemResourceEx(fontPtr, (uint)Properties.Resources.menufont.Length, IntPtr.Zero, ref dummy);
            System.Runtime.InteropServices.Marshal.FreeCoTaskMem(fontPtr);

            _menuFont = new Font(_fonts.Families[0], 22.5F);
        }

        private bool[] _buttonStates;
        private bool[] _lastButtonStates = new bool[4];
        
        private bool _autoLaunch = true;
        private int _currentMenuItem;
        public ConfigMenu CurrentMenu;
            
        public readonly ConfigMenu MainMenu = new ConfigMenu("Launcher Settings", rootMenu: true) { 
            //new ConfigMenuItem("bruh"), 
            //new ConfigMenuItem("moments"),
            //new ConfigMenuItem("of"), 
            //new ConfigMenuItem("history")
        };

        

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (!_gameRunning)
            {
                if (keyData == Keys.Up) { CursorUp(); return true; }
                else if (keyData == Keys.Down) { CursorDown(); return true; }
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void KeyPressed(object sender, KeyPressEventArgs e)
        {
            if (_gameRunning) return;
            switch ((Keys)e.KeyChar)
            {
                case Keys.Escape:
                    if (_autoLaunch) MenuShow();
                    else MenuBack();
                    e.Handled = true;
                    break;
                case Keys.Enter:
                    MenuSelect();
                    e.Handled = true;
                    break;
            }
        }

        private async void Tick(object sender, EventArgs e)
        {
            var gamepads = _input.GetDevices(DeviceClass.GameControl, DeviceEnumerationFlags.AttachedOnly);

            try
            {
                _gameRunning = _gameProcess.StartTime != null;
            }
            catch (InvalidOperationException) {}

            if (_ioBoard == null && gamepads.Count > 0)
            {
                Console.WriteLine("gamepad detected");
                // it will be the only gamepad on the system
                var guid = gamepads[0].InstanceGuid;
                _ioBoard = new Joystick(_input, guid);
                _ioBoard.Acquire();
            }
            else if (gamepads.Count > 0 && !_gameRunning)
            {
                _ioBoard.Poll();
                _buttonStates = _ioBoard.GetCurrentState().Buttons;

                // vol down
                if (_buttonStates[0] && !_lastButtonStates[0])
                {
                    Console.WriteLine("vol down");
                    CursorDown();
                }

                // vol up
                if (_buttonStates[1] && !_lastButtonStates[1])
                {
                    Console.WriteLine("vol up");
                    CursorUp();
                }

                // service
                if (_buttonStates[6] && !_lastButtonStates[6])
                {
                    Console.WriteLine("service button");
                    CursorDown();
                }

                // test
                if (_buttonStates[9] && !_lastButtonStates[9])
                {
                    Console.WriteLine("test button");
                    if(_autoLaunch)
                    {
                        MenuShow();
                    }
                    else MenuSelect();
                }

                _lastButtonStates = _buttonStates;
            }
        }

        private void CursorUp()
        {
            // move cursor up
            Console.WriteLine("CursorUp");
            CurrentMenu[_currentMenuItem].Deactivate();
            _currentMenuItem = ((_currentMenuItem - 1) + CurrentMenu.Count) % CurrentMenu.Count;
            Console.WriteLine($"Item {_currentMenuItem}");
            RefreshMenu();
        }

        private void CursorDown()
        {
            // move cursor down
            Console.WriteLine("CursorDown");
            CurrentMenu[_currentMenuItem].Deactivate();
            _currentMenuItem = (_currentMenuItem + 1) % CurrentMenu.Count;
            Console.WriteLine($"Item {_currentMenuItem}");
            RefreshMenu();
        }

        public void MenuShow()
        {
            _delayTimer.Stop();
            _loadingLabel.Visible = false;
            menuLabel.Visible = true;
            GenerateMenu(MainMenu);
            _autoLaunch = false;
        }

        public void MenuHide()
        {
            _autoLaunch = true;
            menuLabel.Hide();
            foreach (var item in CurrentMenu)
            {
                item.label.Dispose();
            }
            _loadingLabel.Show();

            _delayTimer = new System.Timers.Timer(5000);
            _delayTimer.Elapsed += LaunchDefault;

            _delayTimer.Enabled = true;
        }

        private void MenuSelect()
        {
            // select menu item
            Console.WriteLine("MenuSelect");
            CurrentMenu[_currentMenuItem].Select(this, CurrentMenu);
        }
        
        private void MenuBack()
        {
            // back from current menu item
            Console.WriteLine("MenuBack");
        }

        private void MenuReturn()
        {
            GenerateMenu(MainMenu);
        }

        public void RefreshMenu()
        {
            this.Controls.Remove(CurrentMenu[_currentMenuItem].label);
            CurrentMenu[_currentMenuItem].Activate();
            this.Controls.Add(CurrentMenu[_currentMenuItem].label);
        }

        private static void vfd_test()
        {
            var vfd = new WaccaVFD();
            vfd.Power(true);
            vfd.Clear();
            vfd.Brightness(WaccaVFD.bright.BRIGHT_50);
            vfd.Cursor(0, 0);
            vfd.CanvasShift(0);
            vfd.Write("Testing VFD!");
            vfd.Cursor(0, 16);
            vfd.ScrollSpeed(2);
            vfd.ScrollText(Math.PI.ToString()+" ");
            vfd.ScrollStart();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            _loadingLabel = new Label();
            SuspendLayout();

            _loadingLabel.Font = _menuFont;
            _loadingLabel.ForeColor = Program.IsCorrectVer() ? Color.White : Color.DarkOrange;
            _loadingLabel.Location = new Point(458, 525);
            _loadingLabel.Name = "loadingLabel";
            _loadingLabel.Size = new Size(164, 30);
            _loadingLabel.TabIndex = 0;
            _loadingLabel.Text = "LOADING...";
            _loadingLabel.TextAlign = ContentAlignment.MiddleCenter;

            this.Controls.Add(_loadingLabel);

            _versionLabel = new Label();

            _versionLabel.Font = _menuFont;
            _versionLabel.ForeColor = Color.FromArgb(50,50,50);
            _versionLabel.Location = new Point(458, 1000);
            _versionLabel.Name = "versionLabel";
            _versionLabel.Size = new Size(164, 30);
            _versionLabel.TabIndex = 0;
            _versionLabel.Text = Assembly.GetEntryAssembly().GetName().Version.ToString();
            _versionLabel.TextAlign = ContentAlignment.MiddleCenter;

            Console.WriteLine(_versionLabel.Text);

            this.Controls.Add(_versionLabel);

            LoadVersionsFromConfig();

            var defVerMenu = new ConfigMenu("default version");

            foreach (var ver in Versions)
            {
                var name = ver.GameVersion == VersionType.Custom ? ver.CustomName : ver.ToString();
                defVerMenu.Add(new ConfigMenuItem($"({(ver == DefaultVer ? 'X' : ' ')}) {name}", ConfigMenuAction.VersionSelect, version: ver));
            }

            defVerMenu.Add(new ConfigMenuItem("Return to settings", ConfigMenuAction.Return));

            MainMenu.Add(new ConfigMenuItem("set default version", ConfigMenuAction.Submenu, submenu: defVerMenu));

            MainMenu.Add(new ConfigMenuItem("test VFD", ConfigMenuAction.Command, method: vfd_test));

            MainMenu.Add(new ConfigMenuItem("exit to windows", ConfigMenuAction.Command, method: Application.Exit));

            MainMenu.Add(new ConfigMenuItem("launch game", ConfigMenuAction.Return));

            _loadingLabel.Font = _menuFont;
            menuLabel.Font = _menuFont;
        }

        public void GenerateMenu(ConfigMenu menu, int selectedIndex = 0)
        {
            if (menu == null || menu == CurrentMenu) return;
            if(CurrentMenu != null)
            {
                menu.ParentMenu = CurrentMenu;
                foreach (var item in CurrentMenu)
                {
                    item.label.Dispose();
                }
            }

            CurrentMenu = menu;
            _currentMenuItem = selectedIndex;

            menuLabel.Text = menu.Name.ToUpper();
            var menuIndex = 0;

            foreach (var item in menu)
            {
                item.label = new Label();
                item.label.Text = item.Name.ToUpper();
                item.label.ForeColor = Color.White;
                item.label.TextAlign = ContentAlignment.MiddleLeft;
                item.label.AutoSize = false;
                item.label.Size = new Size(700, 30);
                item.label.Font = _menuFont;
                item.label.Location = new Point(200, 240 + (40 * menuIndex));
                item.label.Parent = this;
                this.Controls.Add(item.label);
                menuIndex++;
            }

            RefreshMenu();
        }

        private static void KillExplorer()
        {
            Process.Start(@"C:\Windows\System32\taskkill.exe", @"/F /IM explorer.exe");
        }

        private static void OpenExplorer()
        {
            var processes = Process.GetProcessesByName("explorer");
            if (processes.Length == 0) Process.Start("explorer.exe");
        }

        private void LaunchGame(Version version)
        {
            Console.WriteLine("launching game");
            _gameProcess.StartInfo.FileName = version.BatchPath;
            _gameProcess.EnableRaisingEvents = true;

            //this.Hide();
            _gameProcess.Exited += QuitLauncher;
            _gameProcess.Start();
            //Application.Exit();
        }

        public void LaunchGame(VersionType type)
        {
            LaunchGame(Versions.Find(x => x.GameVersion == type));
        }

        public void LaunchGame(string gameId)
        {
            LaunchGame(Versions.Find(x => x.GameId == gameId));
        }

        private void QuitLauncher(Object source, EventArgs e)
        {
            // Only exit after the game has closed, so that the launcher doesn't keep opening when configured as a shell
            Application.Exit();
        }

        private void LaunchDefault(Object source, ElapsedEventArgs e)
        {
            _delayTimer.Stop();
            KillExplorer();
            LaunchGame(DefaultVer);
        }

        private void LoadVersionsFromConfig()
        {
            if (_config == null) return;
            foreach (VersionType item in (VersionType[])Enum.GetValues(typeof(VersionType)))
            {
                var iniPath = _config["versions"][item.ToString().ToLower()];
                if (string.IsNullOrEmpty(iniPath)) continue;
                Console.WriteLine($"Found path for {item.ToString().Replace('_', ' ')}: \"{iniPath}\"");
                try
                {
                    var version = new Version(iniPath, item);
                    if (!version.HasSegatools)
                    {
                        DisplayError("Segatools missing", $"Ensure segatools is present in the bin folder ({version})");
                        return;
                    }
                    Versions.Add(version);
                }
                catch (Exception ex) when (ex is NotSupportedException || ex is DirectoryNotFoundException || ex is ArgumentException)
                {
                    DisplayError($"Invalid path for {item.ToString().Replace('_', ' ')}", "Check the config paths for errors and try again");
                    return;
                }
            }

            int num_customs;
            if(int.TryParse(_config["general"]["num_customs"], out num_customs))
            {
                for (var i = 1; i < num_customs + 1; i++) {
                    var customVer = _config[$"custom_{i}"];

                    var customPath = customVer["path"];
                    var customName = customVer["name"];

                    if (string.IsNullOrEmpty(customPath)) continue;
                    try
                    {
                        var version = new Version(customPath, VersionType.Custom, $"custom_{i}", customName);
                        if (!version.HasSegatools)
                        {
                            DisplayError("Segatools missing", $"Ensure segatools is present in the bin folder ({version})");
                            return;
                        }
                        Versions.Add(version);
                        Console.WriteLine($"Found path for {customName}: \"{customPath}\"");
                    }
                    catch (Exception ex) when (ex is NotSupportedException || ex is DirectoryNotFoundException || ex is ArgumentException)
                    {
                        DisplayError($"Invalid path for {customName}", "Check the config paths for errors and try again");
                        return;
                    }
                }
            }
            

            if (Versions.Count == 0)
            {
                DisplayError("No versions found", "Check the config paths for errors and try again");
            } 
            else
            {
                if (_config["general"]["default_ver"] == null || _config["general"]["default_ver"] == string.Empty)
                {
                    SetDefaultVer(Versions.First());
                }

                DefaultVer = Versions.Find(x => x.GameId == _config["general"]["default_ver"]);
            }
        }

        public void SetDefaultVer(Version version)
        {
            DefaultVer = version;
            _config["general"]["default_ver"] = DefaultVer.GameId;
            _parser.WriteFile("wacca.ini", _config);
        }

        private void DisplayError(string error, string description = "")
        {
            _delayTimer.Stop();
            _loadingLabel?.Hide();

            var errorLabel = new Label();
            SuspendLayout();

            errorLabel.Font = _menuFont;
            errorLabel.ForeColor = Color.Red;
            errorLabel.Location = new Point(90, 495);
            errorLabel.AutoSize = false;
            errorLabel.Name = "errorLabel";
            errorLabel.Size = new Size(900, 90);
            var errorText = new StringBuilder();
            errorText.AppendLine("ERROR: " + error + "\n");
            if (description != string.Empty) errorText.AppendLine(description);
            errorLabel.Text = errorText.ToString().ToUpper();
            errorLabel.TextAlign = ContentAlignment.MiddleCenter;

            Controls.Add(errorLabel);
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            OpenExplorer();
        }
    }

    public enum VersionType
    {
        Unknown = 0,
        WACCA,
        WACCA_S,
        Lily,
        Lily_R,
        Reverse,
        Custom = 10
    }

    public class Version
    {
        private readonly DirectoryInfo _dir;

        public DirectoryInfo GameDirectoryInfo => _dir;
        public readonly VersionType GameVersion;
        public readonly string GameId;
        public readonly string CustomName;
        public readonly bool HasSegatools = false;
        public readonly string BatchPath = string.Empty;

        public Version(string path, VersionType version, string gameId = "", string customName = "")
        {
            this._dir = new DirectoryInfo(path);
            if (!_dir.Exists) throw new DirectoryNotFoundException();
            
            this.GameVersion = version;
            this.GameId = gameId != string.Empty ? gameId : version.ToString().ToLower();
            if (customName != string.Empty) this.CustomName = customName; 
            var binPath = Path.Combine(_dir.FullName, "bin");
            
            if (CheckForSegatools(binPath))
            {
                HasSegatools = true;
                BatchPath = Path.Combine(binPath, "start.bat");
            }
        }

        private bool CheckForSegatools(string path)
        {
            return File.Exists(Path.Combine(path, "segatools.ini")) &&
                   File.Exists(Path.Combine(path, "mercuryhook.dll")) &&
                   File.Exists(Path.Combine(path, "inject.exe"));
        }

        public override string ToString()
        {
            return GameVersion.ToString().Replace('_', ' ');
        }
    }

    public enum ConfigMenuAction
    {
        None = 0,
        Command,
        Submenu,
        VersionSelect,
        ItemSelect,
        Return
    }

    public class ConfigMenu : IList<ConfigMenuItem>
    {
        public readonly string Name;
        public bool RootMenu = false;
        public ConfigMenu ParentMenu = null;
        private readonly List<ConfigMenuItem> _items = new List<ConfigMenuItem>();

        public ConfigMenu(string name, bool rootMenu = false)
        {
            this.Name = name;
            this.RootMenu = rootMenu;
        }

        public ConfigMenuItem this[int index] { get => _items[index]; set => _items[index] = value; }

        public int Count => _items.Count;

        public bool IsReadOnly => false;

        public void Add(ConfigMenuItem item)
        {
            _items.Add(item);
        }

        public void Clear()
        {
            _items.Clear();
        }

        public bool Contains(ConfigMenuItem item)
        {
            return _items.Contains(item);
        }

        public void CopyTo(ConfigMenuItem[] array, int arrayIndex)
        {
            _items.CopyTo(array, arrayIndex);
        }

        public IEnumerator<ConfigMenuItem> GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        public int IndexOf(ConfigMenuItem item)
        {
            return _items.IndexOf(item);
        }

        public void Insert(int index, ConfigMenuItem item)
        {
            _items.Insert(index, item);
        }

        public bool Remove(ConfigMenuItem item)
        {
            return _items.Remove(item);
        }

        public void RemoveAt(int index)
        {
            _items.RemoveAt(index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        public void Destroy()
        {
            Clear();
        }
    }

    public class ConfigMenuItem
    {
        public readonly string Name;
        private readonly ConfigMenuAction _action;
        private readonly Action _method;
        private readonly ConfigMenu _submenu;
        private readonly List<string> _options;
        private readonly Version _version;

        public Label label;

        public ConfigMenuItem(string name, ConfigMenuAction action = ConfigMenuAction.None, Action method = null, ConfigMenu submenu = null, List<string> options = null, Version version = null) { 
            this.Name = name;
            this._action = action;

            if (action == ConfigMenuAction.Command && method == null)
                throw new ArgumentException($"Menu item '{name}' was defined with Command type, but has no method associated.");
            else if (action == ConfigMenuAction.Submenu && submenu == null)
                throw new ArgumentException($"Menu item '{name}' was defined with Submenu type, but has no submenu associated.");
            else if (action == ConfigMenuAction.ItemSelect && options == null)
                throw new ArgumentException($"Menu item '{name}' was defined with ItemSelect type, but has no options associated.");
            else if (action == ConfigMenuAction.VersionSelect && version == null)
                throw new ArgumentException($"Menu item '{name}' was defined with VersionSelect type, but has no version associated.");

            this._method = method;
            this._submenu = submenu;
            this._options = options;
            this._version = version;
        }

        public void Activate()
        {
            label.ForeColor = Color.Red;
        }

        public void Deactivate()
        {
            label.ForeColor = Color.White;
        }

        public void Select(MainForm form, ConfigMenu menu)
        {
            if (_action == ConfigMenuAction.Command)
            {
                // only works with static methods, why
                this._method();
            }
            else if (_action == ConfigMenuAction.Submenu && _submenu != null)
            {
                Console.WriteLine("attempting submenu");
                form.GenerateMenu(_submenu);
            }
            else if (_action == ConfigMenuAction.ItemSelect && _options != null)
            {
                // generate list of options and cycle through them, complicated
                return;
            }
            else if (_action == ConfigMenuAction.VersionSelect && _version != null)
            {
                Console.WriteLine($"setting default version to {_version}");
                form.SetDefaultVer(_version);
                for (int i = 0; i < form.Versions.Count; i++)
                {
                    var name = form.Versions[i].GameVersion == VersionType.Custom ? form.Versions[i].CustomName : form.Versions[i].ToString();
                    menu[i].label.Text = $"({(form.Versions[i] == form.DefaultVer ? 'X' : ' ')}) {name}".ToUpper();
                }
                form.RefreshMenu();
            }
            else if (_action == ConfigMenuAction.Return)
                if(form.CurrentMenu == form.MainMenu)
                {
                    form.MenuHide();
                }
                else form.GenerateMenu(form.CurrentMenu.ParentMenu);
        }
    }
}
