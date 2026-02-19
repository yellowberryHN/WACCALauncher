using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Timers;
using System.Diagnostics;
using LilyConsole;
using SharpDX.DirectInput;
using Newtonsoft.Json;

namespace WACCALauncher
{
    public enum LauncherState
    {
        Launching,
        Updating,
        GameStarting,
        GameRunning,
        InMenu,
        GameClosed,
        Error
    }

    public partial class MainForm : Form
    {
        private static System.Timers.Timer _delayTimer;
        private readonly System.Windows.Forms.Timer _t = new System.Windows.Forms.Timer();

        public LauncherState _state;

        private static Font _menuFont;

        private static string _loadingText = "LOADING";

        private static Label _loadingLabel = new Label();
        private static Label _versionLabel = new Label();
        private static Label _buttonLabel = new Label();

        // legacy SharpInput IO4
        private readonly DirectInput _input = new DirectInput();
        private Joystick _ioBoard;
        
        // LilyConsole hardware control
        private readonly LightController _consoleLights = new LightController();
        private readonly IO4Controller _io4 = new IO4Controller();

        public static readonly List<Profile> Profiles = new List<Profile>();
        public static Profile DefaultProfile;
        public Profile SelectedProfile;

        private bool _skipUpdater = false;

        private readonly Process _gameProcess = new Process();
        private readonly Process _amdaemonProcess = new Process();

        internal MenuManager _menuManager;

        internal LauncherSettings settings;

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        public MainForm()
        {
            _menuFont = FontLoader.LoadFont();

            InitializeComponent();

            // start input timer
            _t.Tick += Tick;
            _t.Interval = 20;
            _t.Start();

            // if we're running on a vertical monitor, compensate for ring offset
            if (Program.IsCorrectRes())
            {
                var bounds = Program.CurrentScreen.Bounds;
                SetBounds(bounds.X, bounds.Y + 362, Width, Height);
            }

            // bind game exit to be handled properly (ONLY ONCE)
            _gameProcess.Exited += HandleGameClosed;

            // timer started before we check profiles, it gets stopped when errors are thrown
            StartLaunchTimer();
        }

        public void UpdateLoadingText()
        {
            _loadingLabel.Text = string.Join("",_loadingText,_skipUpdater ? "!!!" : "...");
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (_state == LauncherState.Launching || _state == LauncherState.InMenu)
            {
                switch(keyData)
                {
                    case Keys.Up:
                        _menuManager.CursorUp();
                        return true;
                    case Keys.Down:
                        _menuManager.CursorDown();
                        return true;
                    case Keys.Enter:
                        _menuManager.MenuSelect();
                        return true;
                    case Keys.Escape:
                        if (_state == LauncherState.InMenu) _menuManager.MenuBack();
                        else MenuShow();

                        return true;
                }
            }
            else if (_state == LauncherState.Error && keyData == Keys.Escape)
            {
                Application.Exit();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private bool ButtonPressed(JoystickUpdate state, int button)
        {
            return state.Offset == (JoystickOffset.Buttons0 + button) && state.Value > 0;
        }

        private bool ButtonReleased(JoystickUpdate state, int button)
        {
            return state.Offset == (JoystickOffset.Buttons0 + button) && state.Value == 0;
        }

        private bool ButtonHeld(Joystick joystick, int button)
        {
            return joystick.GetCurrentState().Buttons[button];
        }

        private bool ButtonsHeld(Joystick joystick, int[] buttons)
        {
            var state = joystick.GetCurrentState();
            
            foreach (var button in buttons)
            {
                if (!state.Buttons[button]) return false;
            }
            
            return true;
        }

        private void Tick(object sender, EventArgs e)
        {
            UpdateLoadingText();

            // set up IO board controls if enabled
            if (!settings.DisableIO4)
            {
                if (_ioBoard == null)
                {
                   var gamepads = _input.GetDevices(DeviceClass.GameControl, DeviceEnumerationFlags.AttachedOnly);
                    
                   // it will be the only gamepad on the system
                   var guid = gamepads[0].InstanceGuid;
                   _ioBoard = new Joystick(_input, guid);
                   _ioBoard.Properties.BufferSize = 128;
                   _ioBoard.Acquire();
                }
                else
                {
                   // TODO: make this behave safer
                   _ioBoard.Poll();
                   var padStates = _ioBoard.GetBufferedData();

                   foreach (var padState in padStates)
                   {
                       if(padState.Offset >= JoystickOffset.Buttons0
                          && padState.Offset < JoystickOffset.Buttons10)
                       {
                           var pressed = padState.Value > 0;

                           switch (_state)
                           {
                               case LauncherState.InMenu:
                                   // vol down
                                   if (ButtonPressed(padState, 0)) _menuManager.CursorDown();

                                   // vol up
                                   if (ButtonPressed(padState, 1)) _menuManager.CursorUp();

                                   // service
                                   if (ButtonPressed(padState, 6)) _menuManager.CursorDown();

                                   // test
                                   if (ButtonPressed(padState, 9)) _menuManager.MenuSelect();

                                   break;
                               case LauncherState.Launching:
                                   // vol up (held)
                                   _skipUpdater = ButtonHeld(_ioBoard, 1);

                                   // test
                                   if (ButtonPressed(padState, 9)) MenuShow();

                                   break;
                               case LauncherState.Error:
                                   // test
                                   if (ButtonPressed(padState, 9)) Application.Exit();

                                   break;
                               case LauncherState.GameRunning:
                                   if (ButtonsHeld(_ioBoard,new[] {0, 1, 6, 9})) _gameProcess.Kill();

                                   break;
                           }
                       }
                   }
                } 
            }
        }

        private void StartLaunchTimer()
        {
            if (_state == LauncherState.Launching && _delayTimer != null && _delayTimer.Enabled) return;
            _state = LauncherState.Launching;
            _delayTimer = new System.Timers.Timer(5000);
            _delayTimer.Elapsed += LaunchDefault;

            _delayTimer.Enabled = true;
        }

        public void MenuShow()
        {
            _delayTimer.Stop();
            _loadingLabel.Hide();
            menuLabel.Show();
            _buttonLabel.Show();
            menuListBox.Visible = menuListBox.Enabled = true;

            _state = LauncherState.InMenu;
        }

        public void MenuHide()
        {
            menuLabel.Hide();
            _buttonLabel.Hide();
            menuListBox.Visible = menuListBox.Enabled = false;

            _menuManager.ReturnToRoot();

            if (_state == LauncherState.Error) return;

            _loadingLabel.Show();

            StartLaunchTimer();
        }

        public void MenuUpdateLabel(string text)
        {
            menuLabel.Text = text.ToUpper();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            menuListBox.Font = _menuFont;
            
            SuspendLayout();

            _loadingLabel.Font = _menuFont;
            _loadingLabel.ForeColor = Program.IsRecommendedWinVer() ? Color.White : Color.DarkOrange;
            _loadingLabel.BackColor = Color.Transparent;
            _loadingLabel.Location = new Point(0, 525);
            _loadingLabel.Name = "loadingLabel";
            _loadingLabel.Size = new Size(Width, 30);
            _loadingLabel.TabIndex = 0;
            _loadingLabel.Text = "LOADING...";
            _loadingLabel.TextAlign = ContentAlignment.MiddleCenter;

            Controls.Add(_loadingLabel);

            _versionLabel.Font = _menuFont;
            _versionLabel.ForeColor = Color.FromArgb(50,50,50);
            _versionLabel.Location = new Point(458, 1000);
            _versionLabel.Name = "versionLabel";
            _versionLabel.Size = new Size(164, 30);
            _versionLabel.TabIndex = 0;
            _versionLabel.Text = System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString();
            _versionLabel.TextAlign = ContentAlignment.MiddleCenter;

            Controls.Add(_versionLabel);

            _buttonLabel.Font = _menuFont;
            _buttonLabel.ForeColor = Color.White;
            _buttonLabel.BackColor = Color.Transparent;
            _buttonLabel.Location = new Point(0, 900);
            _buttonLabel.Name = "buttonLabel";
            _buttonLabel.Size = new Size(Width, 60);
            _buttonLabel.TabIndex = 0;
            _buttonLabel.Text = "Press SERVICE button to select\nPress TEST button to decide".ToUpper();
            _buttonLabel.TextAlign = ContentAlignment.MiddleCenter;
            _buttonLabel.Visible = false;

            Controls.Add(_buttonLabel);

            menuLabel.Font = _menuFont;

            ReloadConfig();

            var mainMenu = GetMenu();

            #if DEBUG
                mainMenu.Items.Add(GetDebugMenu());
            #endif

            _menuManager = new MenuManager(mainMenu, menuListBox, this);
        }

        private void ReloadConfig()
        {
            DefaultProfile = null;
            Profiles.Clear();

            LoadLauncherSettings();

            LoadProfiles();

            SetDefaultProfile();
        }

        private Menu GetMenu()
        {
            return new Menu("launcher menu", main: true, items: new List<IMenuItem>()
            {
                new ProfileMenu("one-time launch", type: ProfileMenuType.OneTimeLaunch),
                new ProfileMenu("set default profile", type: ProfileMenuType.SetDefault),
                new MenuSeparator(),
                new Menu("cab management", items: new List<IMenuItem>() {
                    new MenuAction("launch file explorer", OpenFileExplorer),
                    new MenuAction("reload configs", ReloadConfig),
                    new MenuAction("exit to windows", Application.Exit),
                    new MenuSeparator(),
                    new MenuAction("reboot cab (!)", RebootCab)
                }),
                new MenuSeparator(),
                new MenuReturn("launch game")
            });
        }

        private Menu GetDebugMenu()
        {
            return new Menu("Debug Menu", items: new List<IMenuItem>() {
                new MenuAction("start desktop", OpenDesktop),
                new Menu("nesting 1", items: new List<IMenuItem>()
                {
                    new Menu("nesting 11", items: new List<IMenuItem>()
                    {
                        new MenuAction("nesting 111"),
                        new MenuAction("nesting 112"),
                        new MenuAction("nesting 113")
                    }),
                    new Menu("nesting 12", items: new List<IMenuItem>()
                    {
                        new MenuAction("nesting 121"),
                        new MenuAction("nesting 122"),
                        new MenuAction("nesting 123")
                    }),
                    new Menu("nesting 13", items: new List<IMenuItem>()
                    {
                        new MenuAction("nesting 131"),
                        new MenuAction("nesting 132"),
                        new MenuAction("nesting 133")
                    })
                }),
                new Menu("nesting 2", items: new List<IMenuItem>()
                {
                    new Menu("nesting 21", items: new List<IMenuItem>()
                    {
                        new MenuAction("nesting 211"),
                        new MenuAction("nesting 212"),
                        new MenuAction("nesting 213")
                    }),
                    new Menu("nesting 22", items: new List<IMenuItem>()
                    {
                        new MenuAction("nesting 221"),
                        new MenuAction("nesting 222"),
                        new MenuAction("nesting 223")
                    }),
                    new Menu("nesting 23", items: new List<IMenuItem>()
                    {
                        new MenuAction("nesting 231"),
                        new MenuAction("nesting 232"),
                        new MenuAction("nesting 233")
                    })
                }),
                new Menu("nesting 3", items: new List<IMenuItem>()
                {
                    new Menu("nesting 31", items: new List<IMenuItem>()
                    {
                        new MenuAction("nesting 311"),
                        new MenuAction("nesting 312"),
                        new MenuAction("nesting 313")
                    }),
                    new Menu("nesting 32", items: new List<IMenuItem>()
                    {
                        new MenuAction("nesting 321"),
                        new MenuAction("nesting 322"),
                        new MenuAction("nesting 323")
                    }),
                    new Menu("nesting 33", items: new List<IMenuItem>()
                    {
                        new MenuAction("nesting 331"),
                        new MenuAction("nesting 332"),
                        new MenuAction("nesting 333")
                    })
                }),
                GenerateLargeMenu("Large Menu", 50)
            });
        }

        private Menu GenerateLargeMenu(string name, int count)
        {
            var menu = new Menu(name);

            menu.Items.Add(GenerateLargeBigTextMenu("really big text", 50));

            for (int i = 0; i < count; i++)
            {
                menu.Items.Add(new MenuAction($"{name} {i}"));
            }

            return menu;
        }

        private Menu GenerateLargeBigTextMenu(string name, int count)
        {
            var menu = new Menu(name);

            for (int i = 0; i < count; i++)
            {
                menu.Items.Add(new MenuAction($"super ultra omega mega epic swag gaming WWWWWW {i}"));
            }

            return menu;
        }

        private static void KillExplorer()
        {
            Process.Start("taskkill.exe", "/F /IM explorer.exe");
        }

        private static void KillAMDaemon()
        {
            Process.Start("taskkill.exe", "/F /IM amdaemon.exe");
        }

        // i love hacks
        private static void OpenDesktop()
        {
            if(FindWindow("Progman", null) == IntPtr.Zero)
            {
                Process.Start("explorer.exe");
            }
        }

        // more hacks, yippee
        private static void OpenFileExplorer()
        {
            var exp = new Process();
            exp.StartInfo.FileName = "explorer.exe";

            // force file explorer to open instead of desktop
            exp.StartInfo.Arguments = "\"\""; 

            exp.Start();
        }

        private static void RebootCab()
        {
            Process.Start("shutdown.exe", "/f /r /t 0");
        }

        private static ProcessStartInfo SetUpAmdaemon(Profile profile)
        {
            var si = new ProcessStartInfo();
            si.WorkingDirectory = Path.Combine(profile.GetBaseDir().FullName, "bin");
            si.WindowStyle = ProcessWindowStyle.Minimized;
            si.FileName = "inject.exe";
            
            si.Arguments = string.Join(" ", "-d", $"-k {profile.InjectDLLs[0]}", "amdaemon.exe", profile.GetAmdaemonArgs());
            return si;
        }

        private void LaunchUpdater(Profile profile)
        {
            if (File.Exists(profile.UpdaterPath))
            {
                Invoke(new Action(() => _state = LauncherState.Updating));

                Invoke(new Action(() => _loadingText = "CHECKING FOR UPDATES"));

                var updater = new Process();
                updater.StartInfo.FileName = profile.UpdaterPath;
                updater.StartInfo.WorkingDirectory = Path.GetDirectoryName(profile.UpdaterPath) ?? "";
                updater.StartInfo.Arguments = profile.UpdaterArgs;

                updater.Start();

                updater.WaitForExit();

                if (settings.StrictMode && updater.ExitCode != 0)
                {
                    DisplayError("Updater error", $"{profile}: updater closed with exit code {updater.ExitCode}");
                }

                Invoke(new Action(() => _loadingText = "LOADING"));
            }
            else if (settings.StrictMode)
            {
                DisplayError("Updater error", $"{profile}: Could not find updater");
            }
        }

        public void LaunchGame(Profile profile)
        {
            if (profile.UpdaterPath != null && !_skipUpdater) LaunchUpdater(profile);

            if (_state == LauncherState.Error) return;

            Invoke(new Action(() => _state = LauncherState.GameStarting));

            Invoke(new Action(() => _loadingText = "STARTING"));

            #if !DEBUG // it's annoying to have my explorer killed all the time
                KillExplorer();
            #endif

            _gameProcess.EnableRaisingEvents = true;
            _gameProcess.StartInfo.FileName = profile.GetGamePath();
            _gameProcess.StartInfo.WorkingDirectory = profile.BasePath;

            switch (profile.Type)
            {
                case ProfileType.WACCA:
                {
                    if(profile.Configs.Count > 0)
                    {
                        _amdaemonProcess.StartInfo = SetUpAmdaemon(profile);
                        _amdaemonProcess.Start();
                        break;
                    }
                    DisplayError("No amdaemon configs specified");
                    return;
                }
                case ProfileType.Generic:
                {
                    break;
                }
                default:
                {
                    DisplayError("Invalid launch type", $"Unknown launch type \"{profile.Type}\"");
                    return;
                }
            }

            if (profile.InjectGame && profile.Type == ProfileType.WACCA)
            {
                var gamePath = _gameProcess.StartInfo.FileName;
                _gameProcess.StartInfo.FileName = "inject.exe";
                _gameProcess.StartInfo.WorkingDirectory = Path.Combine(profile.GetBaseDir().FullName, "bin");
                _gameProcess.StartInfo.Arguments = string.Join(" ", "-d", profile.GetInjectDLLArgs(), $"\"{gamePath}\"");
                _gameProcess.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;
            }
            else _gameProcess.StartInfo.WindowStyle = ProcessWindowStyle.Normal;

            _gameProcess.Start();

            Invoke(new Action(() => _state = LauncherState.GameRunning));

            Invoke(new Action(() => _loadingText = "RUNNING"));
        }

        private void LaunchDefault(object source, ElapsedEventArgs e)
        {
            Invoke(new Action(() => StopTimer()));

            LaunchGame(SelectedProfile ?? DefaultProfile);
        }

        private void HandleGameClosed(object source, EventArgs e)
        {
            Invoke(new Action(() => _state = LauncherState.GameClosed));

            // it will stay open if we don't close it
            try
            {
                _amdaemonProcess.Kill();
            }
            catch
            {
                // amdaemon wasn't running
            }

            if (settings.UseWatchdog)
            {
                Invoke(new Action(() => {
                    _loadingText = "RESTARTING";
                    StartLaunchTimer();
                }));
            }
            else Application.Exit();
        }

        private void LoadLauncherSettings()
        {
            try
            {
                settings = LauncherSettings.Load();
            }
            catch (FileNotFoundException) { /* don't care, we'll just make a new one */ }
            catch (JsonReaderException)
            {
                DisplayError("Invalid Config", "launcher.json could not be parsed, check for errors");
            }
            LauncherSettings.Save(settings);
        }

        private void LoadProfiles()
        {
            // we already loaded them
            if (Profiles.Count > 0) return;

            var curDir = Environment.CurrentDirectory;

            var profileDir = new DirectoryInfo(Path.Combine(curDir, settings.ProfileDir));
            if (profileDir.Exists)
            {
                var verFiles = profileDir.EnumerateFiles("*.json", SearchOption.AllDirectories);

                foreach (var file in verFiles)
                {
                    try
                    {
                        Profiles.Add(Profile.LoadFromJson(file.FullName));
                    }
                    catch (JsonReaderException)
                    {
                        if(settings.StrictMode) DisplayError("Profile Parse Error", string.Format("Unable to parse {0}, fix errors", file.Name));
                    }
                    catch (ProfileLoadException ex)
                    {
                        DisplayError("Profile Load Error", string.Format("{0}: {1}", file.Name, ex.Message));
                    }
                }
            } else profileDir.Create();

            if (Profiles.Count == 0)
            {
                DisplayError("No Profiles Found", "Ensure you have json files in profile folder");
            }
        }

        // select profile for OTL launch
        public void SelectProfile(Profile profile)
        {
            SelectedProfile = profile;
            MenuHide();
            StopTimer();
            
            LaunchGame(SelectedProfile);
        }

        // set configured default, set first profile entry as default if not set
        public void SetDefaultProfile()
        {
            if (DefaultProfile != null || Profiles.Count == 0) return;

            var profile = Profiles.Find(x => x.Name == settings.DefaultProfile && x.Name != string.Empty);

            if (profile == null)
            {
                profile = Profiles[0];

                settings.DefaultProfile = profile.Name;
                LauncherSettings.Save(settings);
            }

            DefaultProfile = profile;
        }

        // show an error and halt execution
        private void DisplayError(string error, string description = "")
        {
            // invoke used to be able to display error even on timer thread
            Invoke(new Action(() => {
                if(_state == LauncherState.InMenu) MenuHide();
                _state = LauncherState.Error;
                StopTimer();
                _loadingLabel?.Hide();
            }));

            var errorLabel = new Label();
            SuspendLayout();

            errorLabel.Font = _menuFont;
            errorLabel.ForeColor = Color.Red;
            errorLabel.Location = new Point(90, 480);
            errorLabel.AutoSize = false;
            errorLabel.Name = "errorLabel";
            errorLabel.Size = new Size(900, 120);
            var errorText = new StringBuilder();
            errorText.AppendLine("ERROR: " + error + "\n");
            if (description != string.Empty) errorText.AppendLine(description);
            errorLabel.Text = errorText.ToString().ToUpper();
            errorLabel.TextAlign = ContentAlignment.MiddleCenter;

            Invoke(new Action(() => Controls.Add(errorLabel)));
        }

        public static void StopTimer()
        {
            _delayTimer.Stop();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            OpenDesktop();
        }

#if DEBUG
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.DrawEllipse(Pens.DeepPink, 10, 10, Width - 20, Height - 20);
        }
#endif
    }
}
