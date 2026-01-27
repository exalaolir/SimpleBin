using Microsoft.Win32;
using SimpleBin.Helpers;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using static SimpleBin.Helpers.AppThemeHelper;

namespace SimpleBin
{
    public partial class MainWindow : Form
    {
        private readonly BinHelper _binHelper;
        private readonly IconHelper _iconHelper;
        private bool _isSystemDarkTheme;
        private bool _isRestarting;

        public MainWindow(BinHelper binHelper, IconHelper iconHelper)
        {
            var sysLang = CultureInfo.CurrentUICulture.Name;
            var appLang = "en-001";
            if (sysLang.Contains("ru")) appLang = "ru-Ru";
            if (sysLang.Contains("pl")) appLang = "pl-Pl";
            var culture = new CultureInfo(appLang);
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;

            InitializeComponent();

            InitializeThemeComboBox();

            _iconHelper = iconHelper;
            _isSystemDarkTheme = IsSystemDarkThemeEnabled();
            ThemeChanged += Form1_ThemeChanged;
            TrayMenu.RenderMode = ToolStripRenderMode.System;

            if (StartupHelper.IsInStartup())
            {
                AddToStartupBtn.Enabled = false;
                RemoveFromStartupBtn.Enabled = true;
            }
            else
            {
                AddToStartupBtn.Enabled = true;
                RemoveFromStartupBtn.Enabled = false;
            }

            _binHelper = binHelper;
            UpdateStatsControls();

            _binHelper.Update += (s, e) =>
            {
                if (this.InvokeRequired && !this.IsDisposed)
                    BeginInvoke(UpdateStatsControls);
                else if (!this.IsDisposed)
                    UpdateStatsControls();
            };

            this.Load += (s, e) => RecoverFormState();
        }

        private void Form1_ThemeChanged(bool isDarkTheme)
        {
            _iconHelper.SetTheme(isDarkTheme);
            UpdateStatsControls();
        }

        private delegate void ThemeHandler(bool isDarkTheme);
        private event ThemeHandler ThemeChanged;

        private void TrayIcon_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                Process.Start("explorer.exe", "shell:RecycleBinFolder");
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                HideForm();
            }
        }

        private void HideForm()
        {
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
            this.Hide();
            TrayIcon.Visible = true;
        }

        private void ShowForm()
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.Activate(); // brings the window to the front if it's already open
            this.ShowInTaskbar = true;
        }

        private void UpdateStatsControls()
        {
            var (biteSize, itemCount) = BinHelper.GetBinSize();
            SizeToolStripItem.Text = $"{SizeToolStripItem.Text?.Split()[0]} {ConvertSizeToString(biteSize)}";
            ElementsToolStripItem.Text = $"{ElementsToolStripItem.Text?.Split()[0]} {itemCount}";
            ClearToolStripItem.Enabled = !BinHelper.IsBinEmpty();

            TrayIcon.Icon = itemCount == 0
                ? _iconHelper.GetEmptyIcon()
                : _iconHelper.GetIcon();
        }

        private void SettingsToolStripItem_Click(object sender, EventArgs e) => ShowForm();

        private void ClearToolStripItem_Click(object sender, EventArgs e) => BinHelper.ClearBin();

        private static string ConvertSizeToString(long size) => size switch
        {
            < 1024 => $"{size} B",
            < 1024 * 1024 => $"{size / 1024f:F1} KB",
            < 1024 * 1024 * 1024 => $"{size / (1024f * 1024):F1} MB",
            _ => $"{size / (1024f * 1024 * 1024):F1} GB"
        };

        private void ExitToolStripItem_Click(object sender, EventArgs e)
        {
            this.FormClosing -= Form1_FormClosing!;
            this.Close();
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_SETTINGCHANGE = 0x001A;
            if (m.Msg == WM_SETTINGCHANGE && m.LParam != IntPtr.Zero)
            {
                bool currentTheme = IsSystemDarkThemeEnabled();

                if (_isSystemDarkTheme != currentTheme)
                {
                    _isSystemDarkTheme = currentTheme;
                    ThemeChanged?.Invoke(currentTheme);

                    if (AppThemeHelper.GetAppTheme() == Theme.System && !_isRestarting)
                    {
                        BeginInvoke(new Action(() => SaveFormStateAndRestart(needRecoverFormState: this.ShowInTaskbar)));
                    }
                }
            }
            base.WndProc(ref m);
        }

        public static bool IsSystemDarkThemeEnabled()
        {
            const string keyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
            const string valueName = "SystemUsesLightTheme";

            using var key = Registry.CurrentUser.OpenSubKey(keyPath);

            var keyValue = key?.GetValue(valueName);

            if (keyValue is null) return true; // If application can't open registry dark it will be use dark icons

            return (int)keyValue == 0;
        }

        private void AddToStartupBtn_Click(object sender, EventArgs e)
        {
            StartupHelper.AddToStartup();
            AddToStartupBtn.Enabled = false;
            RemoveFromStartupBtn.Enabled = true;
        }

        private void RemoveFromStartupBtn_Click(object sender, EventArgs e)
        {
            StartupHelper.RemoveFromStartup();
            AddToStartupBtn.Enabled = true;
            RemoveFromStartupBtn.Enabled = false;
        }

        private void InitializeThemeComboBox()
        {
            var resourceManager = new ComponentResourceManager(typeof(MainWindow));

            object[] themeComboBoxItems = [
                new KeyValuePair<Theme, string> (Theme.System, resourceManager.GetString("SystemTheme")!),
                new KeyValuePair<Theme, string> (Theme.Dark,   resourceManager.GetString("DarkTheme")!),
                new KeyValuePair<Theme, string> (Theme.Light,  resourceManager.GetString("LightTheme")!)
            ];

            ThemeComboBox.Items.AddRange(themeComboBoxItems);
            ThemeComboBox.DisplayMember = "Value";
            ThemeComboBox.ValueMember = "Key";

            var currentTheme = AppThemeHelper.GetAppTheme();
            ThemeComboBox.SelectedItem = themeComboBoxItems
                .Cast<KeyValuePair<Theme, string>>()
                .First(i => i.Key == currentTheme);
        }

        private void ThemeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ThemeComboBox.SelectedItem is KeyValuePair<Theme, string> pair)
            {
                AppThemeHelper.SetTheme(pair.Key);
                if (this.Visible) SaveFormStateAndRestart(needRecoverFormState: true);
            }
        }

        private void SaveFormStateAndRestart(bool needRecoverFormState)
        {
            _isRestarting = true;

            SettingsHelper.Save(s =>
            {
                s.Left = Left;
                s.Top = Top;
                s.Width = Width;
                s.Height = Height;
                s.IsNeedRecover = needRecoverFormState;
            });

            FormClosing -= Form1_FormClosing!;

            TrayIcon.Visible = false;
            TrayIcon.Dispose();

            Application.Restart();
            Environment.Exit(0);
        }

        private void RecoverFormState()
        {
            if (SettingsHelper.Get(s => s.IsNeedRecover, false))
            {
                StartPosition = FormStartPosition.Manual;
                this.Left = SettingsHelper.Get(s => s.Left, 0);
                this.Top = SettingsHelper.Get(s => s.Top, 0);
                this.Width = SettingsHelper.Get(s => s.Width, 0);
                this.Height = SettingsHelper.Get(s => s.Height, 0);
                SettingsHelper.Save(s => s.IsNeedRecover = false);
                ShowForm();
            }
            else
            {
                HideForm();
            }
        }

        private void supportLink_Click(object sender, EventArgs e) => Process.Start(new ProcessStartInfo
        {
            FileName = @"https://boosty.to/exalaolir/donate",
            UseShellExecute = true,
        });

        private void repoLink_Click(object sender, EventArgs e) => Process.Start(new ProcessStartInfo
        {
            FileName = @"https://github.com/exalaolir/SimpleBin",
            UseShellExecute = true,
        });
    }
}