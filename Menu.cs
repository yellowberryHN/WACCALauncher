using System;
using System.Collections.Generic;

namespace WACCALauncher
{
    internal interface IMenuItem
    {
        string Name { get; }

        void Select(MainForm form);
    }

    internal class MenuSeparator : IMenuItem
    {
        public string Name => "";

        // this should never be called
        public void Select(MainForm form)
        {
            return;
        }
        public override string ToString()
        {
            return "";
        }
    }

    internal class MenuAction : IMenuItem
    {
        public string Name { get; }
        private readonly Action _action;

        public MenuAction(string name, Action action = null)
        {
            Name = name;
            _action = action;
        }

        public void Select(MainForm form)
        {
            _action?.Invoke();
        }

        public override string ToString()
        {
            return Name;
        }
    }

    internal class MenuReturn : IMenuItem
    {
        public string Name { get; }

        public MenuReturn(string name = "Return")
        {
            Name = name;
        }

        public void Select(MainForm form)
        {
            form._menuManager.MenuBack();
        }

        public override string ToString()
        {
            return Name;
        }
    }

    internal class Menu : IMenuItem
    {
        public string Name { get; }
        public string Heading { get; }
        public List<IMenuItem> Items { get; } = new List<IMenuItem>();

        public bool MainMenu { get; }

        public Menu(string name, string heading = null, bool main = false)
        {
            Name = name;
            Heading = heading ?? name;
            MainMenu = main;
        }

        public Menu(string name, List<IMenuItem> items, bool main = false)
        {
            Name = Heading = name;
            Items = items;
            MainMenu = main;
        }

        public List<IMenuItem> GetItems()
        {
            if(MainMenu) return Items;

            var list = new List<IMenuItem>();

            list.AddRange(Items);
            list.Add(new MenuSeparator());
            list.Add(new MenuReturn());

            return list;
        }

        public void Select(MainForm form)
        {
            form._menuManager.NavigateToSubmenu(this);
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public enum ProfileMenuType
    {
        OneTimeLaunch,
        SetDefault,
        Editing
    }

    internal class ProfileMenu : Menu
    {
        public ProfileMenuType MenuType { get; }

        public ProfileMenu(string name, string heading = null, ProfileMenuType type = ProfileMenuType.OneTimeLaunch) : base(name, heading)
        {
            heading = heading ?? "Select Profile";
            MenuType = type;
            FillList();
        }

        internal void FillList()
        {
            if(Items.Count > 0) Items.Clear();

            foreach (var profile in MainForm.Profiles)
            {
                Items.Add(new MenuProfileItem(profile, MenuType));
            }
        }
    }

    internal class MenuProfileItem : IMenuItem
    {
        public string Name { get => ProfileData.Name; }

        public ProfileMenuType MenuType { get; }

        public Profile ProfileData { get; }

        public MenuProfileItem(Profile profile, ProfileMenuType type)
        {
            ProfileData = profile;
            MenuType = type;
        }

        public void Select(MainForm form)
        {
            // TODO: implement profile config toggle editing interface
            switch(MenuType)
            {
                case ProfileMenuType.OneTimeLaunch:
                    form.SelectProfile(ProfileData);
                    break;
                case ProfileMenuType.SetDefault:
                    form.settings.DefaultProfile = ProfileData.Name;
                    LauncherSettings.Save(form.settings);

                    MainForm.DefaultProfile = ProfileData;

                    form._menuManager.RefreshList();
                    break;
            }
        }

        public override string ToString()
        {
            switch (MenuType)
            {
                case ProfileMenuType.SetDefault:
                    return $"[{(MainForm.DefaultProfile == ProfileData ? 'X' : ' ')}] {Name}";
                default:
                    return Name;
            }
        }
    }
}
