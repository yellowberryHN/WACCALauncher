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
using System.Linq;
using System.Reflection;
using WACCA;

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

        public MenuManager _menuManager;
        private VFD _vfd;

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
        
        public bool _autoLaunch = true;
        private int _currentMenuItem;

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (!_gameRunning)
            {
                if (keyData == Keys.Up) { _menuManager.CursorUp(); return true; }
                else if (keyData == Keys.Down) { _menuManager.CursorDown(); return true; }
                else if (keyData == Keys.Enter) { _menuManager.MenuSelect(); return true; }
                else if (keyData == Keys.Escape)
                {
                    if (_autoLaunch) MenuShow();
                    else _menuManager.MenuBack();
                    return true;
                }
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        /*
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
        }*/

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
                    _menuManager.CursorDown();
                }

                // vol up
                if (_buttonStates[1] && !_lastButtonStates[1])
                {
                    Console.WriteLine("vol up");
                    _menuManager.CursorUp();
                }

                // service
                if (_buttonStates[6] && !_lastButtonStates[6])
                {
                    Console.WriteLine("service button");
                    _menuManager.CursorDown();
                }

                // test
                if (_buttonStates[9] && !_lastButtonStates[9])
                {
                    Console.WriteLine("test button");
                    if(_autoLaunch)
                    {
                        MenuShow();
                    }
                    else _menuManager.MenuSelect();
                }

                _lastButtonStates = _buttonStates;
            }
        }

        public void MenuShow()
        {
            _delayTimer.Stop();
            _loadingLabel.Hide();
            menuLabel.Show();
            waccaListTest.Visible = waccaListTest.Enabled = true;
            _autoLaunch = false;
        }

        public void MenuHide()
        {
            _autoLaunch = true;
            menuLabel.Hide();
            waccaListTest.Visible = waccaListTest.Enabled = false;
            _loadingLabel.Show();

            _delayTimer = new System.Timers.Timer(5000);
            _delayTimer.Elapsed += LaunchDefault;

            _delayTimer.Enabled = true;
        }

        public void MenuUpdateLabel(string text)
        {
            menuLabel.Text = text.ToUpper();
        }

        private static void vfd_test()
        {
            var vfd = new VFD();
            vfd.Reset();
            vfd.PowerOn();
            vfd.Brightness(VFD.Bright._50);
            vfd.CanvasShift(0);
            vfd.Cursor(0, 0);
            vfd.FontSize(VFD.Font._16_16);
            vfd.Write("Testing VFD!");
            vfd.Cursor(0, 2);
            vfd.ScrollSpeed(15);
            vfd.ScrollText(Math.PI.ToString() + " ");
            vfd.ScrollStart();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            _loadingLabel = new Label();
            waccaListTest.Font = _menuFont;
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
            
            var mainMenu = new ConfigMenu("Launcher Settings", items: new List<ConfigMenu>() {
                new ConfigMenu("one-time launch", ConfigMenuAction.Menu, items: GetOTLMenu()),
                new ConfigMenu("set default version", ConfigMenuAction.Menu, items: GetDefaultVersionMenu()),
                new ConfigMenu("exit to windows", ConfigMenuAction.Command, method: Application.Exit),
                new ConfigMenu("launch game", ConfigMenuAction.Return)
            });

            _menuManager = new MenuManager(mainMenu, waccaListTest, this);

            _loadingLabel.Font = _menuFont;
            menuLabel.Font = _menuFont;
        }

        public List<ConfigMenu> GetDefaultVersionMenu()
        {
            var defVerMenu = new List<ConfigMenu>();

            foreach (var ver in Versions)
            {
                var name = ver.GameVersion == VersionType.Custom ? ver.CustomName : ver.ToString();
                defVerMenu.Add(new ConfigMenu($"({(ver == DefaultVer ? 'X' : ' ')}) {name}", ConfigMenuAction.VersionSelect, version: ver, defVer: true));
            }

            defVerMenu.Add(new ConfigMenu("Return to settings", ConfigMenuAction.Return));

            return defVerMenu;
        }

        public List<ConfigMenu> GetOTLMenu()
        {
            var otlMenu = new List<ConfigMenu>();

            foreach (var ver in Versions)
            {
                var name = ver.GameVersion == VersionType.Custom ? ver.CustomName : ver.ToString();
                otlMenu.Add(new ConfigMenu(name, ConfigMenuAction.VersionSelect, version: ver, defVer: false));
            }

            otlMenu.Add(new ConfigMenu("Return to settings", ConfigMenuAction.Return));

            return otlMenu;
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

        public void LaunchGame(Version version)
        {
            Console.WriteLine("launching game");
            _gameProcess.StartInfo.FileName = version.BatchPath;
            _gameProcess.EnableRaisingEvents = true;

            //this.Hide();
            _gameProcess.Exited += QuitLauncher;
            _gameProcess.Start();
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

        public void StopTimer()
        {
            _delayTimer.Stop();
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
        Offline,
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
        Menu,
        Command,
        VersionSelect,
        ItemSelect,
        Return
    }

    public class ConfigMenu
    {
        public string Name;
        public List<ConfigMenu> Items;
        public ConfigMenu ParentMenu;

        private readonly ConfigMenuAction _action;
        private readonly Action _method;
        private readonly List<string> _options;
        private readonly Version _version;
        private readonly bool _defVer;

        public void Select(MainForm form)
        {
            if (_action == ConfigMenuAction.Command)
            {
                // only works with static methods, why
                this._method();
            }
            else if (_action == ConfigMenuAction.Menu && Items != null)
            {
                form._menuManager.NavigateToSubmenu(this);
            }
            else if (_action == ConfigMenuAction.ItemSelect && _options != null)
            {
                // generate list of options and cycle through them, complicated
                return;
            }
            else if (_action == ConfigMenuAction.VersionSelect && _version != null)
            {
                if(_defVer)
                {
                    Console.WriteLine($"setting default version to {_version}");
                    form.SetDefaultVer(_version);
                    // TODO: this is kinda jank, fix this
                    form._menuManager.UpdateCurrentMenuItems(form.GetDefaultVersionMenu());
                }
                else
                {
                    Console.WriteLine($"one-time launch for {_version}");
                    form.MenuHide();
                    form.StopTimer();
                    form.LaunchGame(_version);
                }
                
            }
            else if (_action == ConfigMenuAction.Return) { form._menuManager.MenuBack(); }
        }

        public ConfigMenu(string name, ConfigMenuAction action = ConfigMenuAction.None, Action method = null, List<ConfigMenu> items = null, List<string> options = null, Version version = null, bool defVer = false)
        {
            this.Name = name;
            this._action = action;

            if (action == ConfigMenuAction.Menu && items == null)
                throw new ArgumentException($"Menu item '{name}' was defined with Menu type, but has no menu associated.");
            else if (action == ConfigMenuAction.Command && method == null)
                throw new ArgumentException($"Menu item '{name}' was defined with Command type, but has no method associated.");
            else if (action == ConfigMenuAction.ItemSelect && options == null)
                throw new ArgumentException($"Menu item '{name}' was defined with ItemSelect type, but has no options associated.");
            else if (action == ConfigMenuAction.VersionSelect && version == null)
                throw new ArgumentException($"Menu item '{name}' was defined with VersionSelect type, but has no version associated.");

            this.Items = items;
            this._method = method;
            this._options = options;
            this._version = version;
            this._defVer = defVer;
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public class MenuManager
    {
        private ConfigMenu _rootMenu;
        private ConfigMenu _currentMenu;
        private WaccaList _list;
        private MainForm _form;

        public MenuManager(ConfigMenu root, WaccaList list, MainForm form)
        {
            _rootMenu = root;
            _currentMenu = _rootMenu;
            _list = list;
            _list.AssignMenuManager(this);
            _form = form;
            UpdateList();      
        }

        public void CursorUp()
        {
            // move cursor up
            if (_form._autoLaunch) return;

            var idx = ((_list.SelectedIndex - 1) + _list.Items.Count) % _list.Items.Count;
            _list.SelectedIndex = idx;
        }

        public void CursorDown()
        {
            // move cursor down
            if (_form._autoLaunch) return;

            var idx = (_list.SelectedIndex + 1) % _list.Items.Count;
            _list.SelectedIndex = idx;
        }

        public void MenuBack()
        {
            // back from current menu item
            if (_form._autoLaunch) return;

            Console.WriteLine("MenuBack");
            if (_form._menuManager.GetCurrentMenu().ParentMenu == null)
                _form.MenuHide();
            else _form._menuManager.NavigateBack();
        }

        public void MenuSelect()
        {
            // select menu item
            if (_form._autoLaunch) return;

            Console.WriteLine("MenuSelect");
            (_list.SelectedItem as ConfigMenu).Select(_form);
        }

        public ConfigMenu GetCurrentMenu()
        {
            return _currentMenu;
        }

        public void NavigateToSubmenu(ConfigMenu menu)
        {
            menu.ParentMenu = _currentMenu;
            _currentMenu = menu;
            UpdateList();
        }

        public void NavigateBack()
        {
            _currentMenu = _currentMenu.ParentMenu;
            UpdateList();
        }

        public void UpdateCurrentMenuItems(List<ConfigMenu> items)
        {
            _currentMenu.Items = items;
            UpdateList(preserveIndex: true);
        }

        private void UpdateList(bool preserveIndex = false)
        {
            var oldIndex = _list.SelectedIndex;
            _list.Items.Clear();
            _list.Items.AddRange(_currentMenu.Items.ToArray());
            _form.MenuUpdateLabel(_currentMenu.Name);
            if (_list.Items.Count > 0) _list.SelectedIndex = preserveIndex ? oldIndex : 0;
        }
    }
}
