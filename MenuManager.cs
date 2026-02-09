using System;
using System.Collections.Generic;

namespace WACCALauncher
{
    internal class MenuManager
    {
        private Menu _currentMenu;
        private Stack<Tuple<Menu, int>> _menuLevels = new Stack<Tuple<Menu, int>>();
        private WaccaList _list;
        private MainForm _form;

        public MenuManager(Menu root, WaccaList list, MainForm form)
        {
            _currentMenu = root;
            _list = list;
            _list.AssignMenuManager(this);
            _form = form;
            UpdateList();
        }

        public void CursorUp()
        {
            // move cursor up
            var idx = ((_list.SelectedIndex - 1) + _list.Items.Count) % _list.Items.Count;
            _list.SelectedIndex = idx;
            if (_list.Items[idx] is MenuSeparator) CursorUp();
        }

        public void CursorDown()
        {
            // move cursor down
            var idx = (_list.SelectedIndex + 1) % _list.Items.Count;
            _list.SelectedIndex = idx;
            if (_list.Items[idx] is MenuSeparator) CursorDown();
        }

        public void MenuBack()
        {
            // back from current menu item
            if (_menuLevels.Count < 1)
            {
                _form._state = LauncherState.Launching;
                _form.MenuHide();
                if (MainForm.DefaultProfile == null) _form.SetDefaultProfile();
            }
            else NavigateBack();
        }

        public void MenuSelect()
        {
            // select menu item
            (_list.SelectedItem as IMenuItem).Select(_form);
        }

        public Menu GetCurrentMenu()
        {
            return _currentMenu;
        }

        public void NavigateToSubmenu(Menu menu)
        {
            _menuLevels.Push(new Tuple<Menu, int>(_currentMenu, _list.SelectedIndex));
            _currentMenu = menu;
            if (menu is ProfileMenu profileMenu) profileMenu.FillList();
            UpdateList();
        }

        public void NavigateBack()
        {
            var back = _menuLevels.Pop();
            _currentMenu = back.Item1;
            UpdateList(back.Item2);
        }

        public void ReturnToRoot()
        {
            while(_menuLevels.Count > 0) NavigateBack();
        }

        private void UpdateList(int idx = 0)
        {
            _list.Items.Clear();
            _list.Items.AddRange(_currentMenu.GetItems().ToArray());
            _form.MenuUpdateLabel(_currentMenu.Heading);
            if (_list.Items.Count > 0) _list.SelectedIndex = idx;
        }

        internal void RefreshList()
        {
            _list.Invalidate();
        }
    }
}
