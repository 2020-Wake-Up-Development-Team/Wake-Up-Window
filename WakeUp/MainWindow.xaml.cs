using System;
using System.Windows;
using System.Windows.Forms;

namespace WakeUp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private NotifyIcon notifyIcon = new NotifyIcon();

        public MainWindow()
        {
            InitializeComponent();

            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            DataContext = App.programViewModel;
            Visibility = Visibility.Collapsed;

            SetDisplayArea();
            SetTraySystem();
        }

        private void SetDisplayArea()
        {
            var area = SystemParameters.WorkArea;

            this.Top = area.Top + 10;
            this.Left = area.Right - this.Width - 10;
        }

        private void SetTraySystem()
        {
            notifyIcon.Icon = Properties.Resources.logo;
            notifyIcon.Text = "WakeUp";
            notifyIcon.ContextMenu = GetContextMenu();
            notifyIcon.Visible = true;
        }

        private ContextMenu GetContextMenu()
        {
            ContextMenu menu = new ContextMenu();

            MenuItem settingItem = new MenuItem("Setting");
            MenuItem exitItem = new MenuItem("Exit");

            settingItem.Click += SettingItem_Click;
            exitItem.Click += ExitItem_Click;

            menu.MenuItems.Add(settingItem);
            menu.MenuItems.Add(exitItem);

            return menu;
        }

        private void SettingItem_Click(object sender, EventArgs e)
        {
            CheckPasswordWindow checkPasswordWindow = new CheckPasswordWindow();

            if (checkPasswordWindow.ShowDialog() == true)
            {
                Visibility = Visibility.Visible;
            }
        }

        private void ExitItem_Click(object sender, EventArgs e)
        {
            App.Current.Shutdown();
        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            Visibility = Visibility.Collapsed;
        }
    }
}
