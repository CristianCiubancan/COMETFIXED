using System.Globalization;
using Comet.Launcher.Helpers;
using Comet.Launcher.Managers;
using Comet.Launcher.Windows;
using IniParser;
using IniParser.Model;

namespace Comet.Launcher.Forms
{
    public partial class FrmClientConfig : Form
    {
        private const string ScreenMode = "GameScreenSettings";
        private const string ScreenWidth = "ScrWidth";
        private const string ScreenHeight = "ScrHeight";
        private const string FramesPerSecond = "Fps";

        public FrmClientConfig()
        {
            InitializeComponent();

            NumScreenWidth.Maximum = Screen.PrimaryScreen.Bounds.Width;
            NumScreenHeight.Maximum = Screen.PrimaryScreen.Bounds.Height;

            foreach (Control ctrl in Controls)
            {
                ctrl.Text = LocaleManager.GetString(ctrl.Text);
            }

            foreach (string screen in ScreenResolutionHelper.Query()
                                                            .OrderBy(x => x.dmPelsWidth)
                                                            .ThenBy(x => x.dmPelsHeight)
                                                            .Select(x => $"{x.dmPelsWidth}x{x.dmPelsHeight}")
                                                            .Distinct())
            {
                CmbResolution.Items.Add(screen);
            }
        }

        private void FrmClientConfig_Load(object sender, EventArgs e)
        {
            var parser = new FileIniDataParser();
            parser.Parser.Configuration.AssigmentSpacer = null;
            parser.Parser.Configuration.AllowDuplicateSections = true;
            parser.Parser.Configuration.OverrideDuplicateKeys = true;
            string path = Path.Combine(Environment.CurrentDirectory, "GameSetUp.ini");
            IniData data = parser.ReadFile(path);

            if (data.Sections.ContainsSection(ScreenMode))
            {
                if (data[ScreenMode].ContainsKey(ScreenWidth) && int.TryParse(data[ScreenMode][ScreenWidth], out int w))
                {
                    NumScreenWidth.Value = w;
                }

                if (data[ScreenMode].ContainsKey(ScreenHeight) &&
                    int.TryParse(data[ScreenMode][ScreenHeight], out int h))
                {
                    NumScreenHeight.Value = h;
                }

                if (data[ScreenMode].ContainsKey(FramesPerSecond) &&
                    int.TryParse(data[ScreenMode][FramesPerSecond], out int fps))
                {
                    NumFramesPerSecond.Value = fps;
                }
            }

            for (int i = 0; i < CmbResolution.Items.Count; i++)
            {
                string item = CmbResolution.Items[i].ToString();
                if ($"{NumScreenWidth.Value}x{NumScreenHeight.Value}".Equals(item))
                {
                    ChkChooseScreenSize.Checked = true;
                    CmbResolution.SelectedIndex = i;
                    break;
                }
            }
        }

        private void BtnAccept_Click(object sender, EventArgs e)
        {
            var parser = new FileIniDataParser();
            parser.Parser.Configuration.AssigmentSpacer = null;
            parser.Parser.Configuration.AllowDuplicateSections = true;
            parser.Parser.Configuration.OverrideDuplicateKeys = true;
            string path = Path.Combine(Environment.CurrentDirectory, "GameSetUp.ini");
            IniData data = parser.ReadFile(path);

            int w = 1024;
            int h = 768;
            if (!ChkChooseScreenSize.Checked || CmbResolution.SelectedIndex < 0 || CmbResolution.SelectedIndex >= CmbResolution.Items.Count)
            {
                w = (int) NumScreenWidth.Value;
                h = (int) NumScreenHeight.Value;
            }
            else
            {
                string[] resParsed = CmbResolution.SelectedItem.ToString().Split('x');
                if (resParsed.Length <= 1 || !int.TryParse(resParsed[0], out w) || !int.TryParse(resParsed[1], out h))
                {
                    w = 1024;
                    h = 768;
                }
            }

            if (data[ScreenMode].ContainsKey(ScreenWidth))
            {
                data[ScreenMode][ScreenWidth] = w.ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                data.Sections[ScreenMode]
                    .AddKey(ScreenWidth, w.ToString(CultureInfo.InvariantCulture));
            }

            if (data[ScreenMode].ContainsKey(ScreenHeight))
            {
                data[ScreenMode][ScreenHeight] = h.ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                data.Sections[ScreenMode]
                    .AddKey(ScreenHeight, h.ToString(CultureInfo.InvariantCulture));
            }

            if (data[ScreenMode].ContainsKey(FramesPerSecond))
            {
                data[ScreenMode][FramesPerSecond] = NumFramesPerSecond.Value.ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                data.Sections[ScreenMode].AddKey(FramesPerSecond,
                                                 NumFramesPerSecond.Value.ToString(CultureInfo.InvariantCulture));
            }

            parser.WriteFile(path, data);
            DialogResult = DialogResult.OK;
            Close();
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void ChkChooseScreenSize_CheckedChanged(object sender, EventArgs e)
        {
            var s = (CheckBox) sender;
            NumScreenWidth.Enabled = !s.Checked;
            NumScreenHeight.Enabled = !s.Checked;
            CmbResolution.Enabled = s.Checked;
        }
    }
}