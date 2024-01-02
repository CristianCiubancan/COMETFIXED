using System.Buffers;
using System.Diagnostics;
using System.Globalization;
using Comet.Launcher.Configuration;
using Comet.Launcher.Files.Helpers;
using Comet.Launcher.Forms;
using Comet.Launcher.Helpers;
using Comet.Launcher.Managers;
using Comet.Launcher.Threads;
using Comet.Network.Packets.Updater;
using Comet.Shared;

namespace Comet.Launcher
{
    public partial class FrmMain : Form
    {
        public static FrmMain Instance { get; private set; }

        private enum FormStage
        {
            Initializing,
            Running,
            Closing
        }

        public const int CURRENT_VERSION = 10000;
        public static int CurrentGameVersion = 1000;

        public static string DataPath => Path.Combine(Environment.CurrentDirectory, "Data");

        private Image mPlayImage;
        private Image mPlayHoverImage;
        private Image mPlayClickImage;

        private LauncherThread mLauncherThread;
        private AntiCheatThread mAntiCheatThread;
        private FormStage mStage = FormStage.Initializing;

        private readonly Dictionary<int, Process> mClients = new();

        public static string WorkingDirectory => Environment.CurrentDirectory.GetParentDirectory();

        public FrmMain()
        {
            InitializeComponent();

            Instance = this;
        }

        private async void FrmMain_Load(object sender, EventArgs e)
        {
            SuspendLayout();

            LocaleManager.SetLanguage(CultureInfo.CurrentUICulture.Name);

            if (File.Exists(Path.Combine(DataPath, "background.jpg")))
                BackgroundImage = Image.FromFile(Path.Combine(DataPath, "background.jpg"));

            if (File.Exists(Path.Combine(DataPath, "BtnPlay.png")))
            {
                mPlayImage = Image.FromFile(Path.Combine(DataPath, "BtnPlay.png"));
                BtnPlay.BackgroundImage = mPlayImage;
            }

            if (File.Exists(Path.Combine(DataPath, "BtnPlayHover.png")))
                mPlayHoverImage = Image.FromFile(Path.Combine(DataPath, "BtnPlayHover.png"));

            if (File.Exists(Path.Combine(DataPath, "BtnPlayClicked.png")))
                mPlayClickImage = Image.FromFile(Path.Combine(DataPath, "BtnPlayClicked.png"));
            
            DoubleBuffered = true;

            CloseButton.FlatAppearance.MouseOverBackColor = Color.Transparent;
            CloseButton.FlatAppearance.MouseDownBackColor = Color.Transparent;

            MinimizeButton.FlatAppearance.MouseOverBackColor = Color.Transparent;
            MinimizeButton.FlatAppearance.MouseDownBackColor = Color.Transparent;

            SettingsButton.FlatAppearance.MouseOverBackColor = Color.Transparent;
            SettingsButton.FlatAppearance.MouseDownBackColor = Color.Transparent;

            TitleLabel.Text = LocaleManager.GetString("StrTitlePanel");
            LabelProgressStatus.Text = string.Empty;
            LabelReadyProgress.Text = string.Empty;

            CheckVersionFile();

            SetWindowMode(PanelMode.PlayDisabled);

            ResumeLayout(true);

            if (!Kernel.Initialize())
            {
                mStage = FormStage.Closing;
                Close();
                return;
            }

            mStage = FormStage.Running;

            mLauncherThread = new LauncherThread(this);
            await mLauncherThread.StartAsync();

            mAntiCheatThread = new AntiCheatThread(OnIllegalAction);
            await mAntiCheatThread.StartAsync();
        }

        private void OnIllegalAction()
        {
            mStage = FormStage.Closing;
            MessageBox.Show(this,
                            LocaleManager.GetString("StrIllegalActionMessage"),
                            LocaleManager.GetString("StrIllegalActionTitle"),
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error
            );

            Environment.Exit(0xA);
        }

        #region Drag'n Drop

        private Point mMouseLoc;

        private void TitlePanel_MouseDown(object sender, MouseEventArgs e)
        {
            mMouseLoc = e.Location;
        }

        private void TitlePanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                int dx = e.Location.X - mMouseLoc.X;
                int dy = e.Location.Y - mMouseLoc.Y;
                Location = new Point(Location.X + dx, Location.Y + dy);
            }
        }

        private void FrmMain_LocationChanged(object sender, EventArgs e)
        {
            Refresh();
        }

        #endregion

        #region Close Event

        public void ForceClose()
        {
            if (InvokeRequired)
            {
                Invoke(ForceClose);
                return;
            }

            mStage = FormStage.Closing;
            Close();
        }

        private void CloseButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private async void FrmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (mStage == FormStage.Running && e.CloseReason != CloseReason.TaskManagerClosing)
            {
                DialogResult result;
                if (mClients.Count > 0)
                {
                    result = MessageBox.Show(this, LocaleManager.GetString("StrCloseMessageClientOpen"),
                                             LocaleManager.GetString("StrCloseTitle"), MessageBoxButtons.YesNo,
                                             MessageBoxIcon.Warning);
                }
                else
                {
                    result = MessageBox.Show(this, LocaleManager.GetString("StrCloseMessage"),
                                             LocaleManager.GetString("StrCloseTitle"), MessageBoxButtons.YesNo,
                                             MessageBoxIcon.Warning);
                }

                if (result == DialogResult.No)
                {
                    e.Cancel = true;
                    return;
                }
            }

            foreach (Process app in mClients.Values)
            {
                app.Close();
            }

            await mLauncherThread.CloseAsync();
        }

        private void FrmMain_FormClosed(object sender, FormClosedEventArgs e)
        {
            foreach (Process game in mClients.Values)
            {
                try
                {
                    game.Kill();
                    game.Close();
                    game.Dispose();
                }
                catch
                {
                }
            }

            if (!string.IsNullOrEmpty(mOpenOnClose))
            {
                Process.Start(mOpenOnClose);
            }
            else
            {
                Kernel.OpenUrl("https://worldconquer.online");
            }
        }

        #endregion

        #region Play Button Event

        private void BtnPlay_MouseEnter(object sender, EventArgs e)
        {
            if (mPlayHoverImage != null)
                BtnPlay.BackgroundImage = mPlayHoverImage;
        }

        private void BtnPlay_MouseLeave(object sender, EventArgs e)
        {
            if (mPlayImage != null)
                BtnPlay.BackgroundImage = mPlayImage;
        }

        private void BtnPlay_MouseDown(object sender, MouseEventArgs e)
        {
            if (mPlayClickImage != null)
                BtnPlay.BackgroundImage = mPlayClickImage;
        }

        private void BtnPlay_MouseUp(object sender, MouseEventArgs e)
        {
            if (mPlayImage != null)
                BtnPlay.BackgroundImage = mPlayImage;
        }

        private void BtnPlay_Click(object sender, EventArgs e)
        {
            if (mStage != FormStage.Running)
                return;

            Play();
        }

        #endregion

        #region Download Panels Control

        private void SetWindowMode(PanelMode mode)
        {
            if (InvokeRequired)
            {
                Invoke(() => SetWindowMode(mode));
                return;
            }

            if (mode == PanelMode.Download)
            {
                PanelReadyToPlay.Visible = false;
                PanelProgressDoingSomething.Visible = true;
                GeneralProgressBar.Visible = true;

                BtnPlay.Visible = false;
                BtnPlay.Enabled = false;
            }
            else if (mode == PanelMode.NoProgress)
            {
                PanelReadyToPlay.Visible = false;
                PanelProgressDoingSomething.Visible = true;
                GeneralProgressBar.Visible = false;

                BtnPlay.Visible = false;
                BtnPlay.Enabled = false;
            }
            else
            {
                PanelProgressDoingSomething.Visible = false;
                PanelReadyToPlay.Visible = true;
                GeneralProgressBar.Visible = false;

                if (mode == PanelMode.PlayDisabled)
                {
                    BtnPlay.Visible = false;
                    BtnPlay.Enabled = false;
                }
                else
                {
                    BtnPlay.Visible = true;
                    BtnPlay.Enabled = true;
                }
            }
        }

        public void FinishProcess(bool success)
        {
            if (InvokeRequired)
            {
                Invoke(() => FinishProcess(success));
                return;
            }

            SetWindowMode(success ? PanelMode.Play : PanelMode.PlayDisabled);
        }

        #endregion

        #region Query Version

        private void CheckVersionFile()
        {
            if (InvokeRequired)
            {
                Invoke(CheckVersionFile);
                return;
            }

            try
            {
                string versionFilePath = Path.Combine(WorkingDirectory, "Version.dat");
                if (!File.Exists(versionFilePath))
                {
                    MessageBox.Show(this, LocaleManager.GetString("StrVersionNotFound"));
                    return;
                }

                string version = File.ReadAllText(versionFilePath);
                if (string.IsNullOrEmpty(version))
                {
                    MessageBox.Show(this, LocaleManager.GetString("StrVersionNotFound"));
                    return;
                }

                CurrentGameVersion = int.Parse(version);
            }
            finally
            {
                LabelVersion.Text = LocaleManager.GetString("StrVersion", CurrentGameVersion, CURRENT_VERSION);
            }
        }

        #endregion

        #region Progress Label Management

        public void SetProgressLabel(string msg)
        {
            if (InvokeRequired)
            {
                Invoke(() => SetProgressLabel(msg));
                return;
            }

            LabelProgressStatus.Text = msg;
            LabelReadyProgress.Text = msg;
        }

        public void SetProgressMinValue(int value)
        {
            if (InvokeRequired)
            {
                Invoke(() => SetProgressMinValue(value));
                return;
            }

            GeneralProgressBar.Minimum = value;
        }

        public void SetProgressMaxValue(long value)
        {
            if (InvokeRequired)
            {
                Invoke(() => SetProgressMaxValue(value));
                return;
            }

            GeneralProgressBar.Maximum = value;
        }

        public void SetProgressValue(long value)
        {
            if (InvokeRequired)
            {
                Invoke(() => SetProgressValue(value));
                return;
            }

            GeneralProgressBar.Value = value;
        }

        #endregion

        #region Tray Icon Management

        private void MinimizeButton_Click(object sender, EventArgs e)
        {
            Hide();
            NotifyBar.Visible = true;
            if (!Kernel.UserConfiguration.SuppressMinimizeAlert)
                NotifyBar.ShowBalloonTip(5, null, LocaleManager.GetString("StrCometUpdaterBaloonMinimizeMsg"),
                                         ToolTipIcon.Info);
        }

        private void NotifyBar_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            Show();
            NotifyBar.Visible = false;
        }

        private void NotifyBar_BalloonTipClicked(object sender, EventArgs e)
        {
            Kernel.UserConfiguration.SuppressMinimizeAlert = true;
        }

        #endregion

        #region Download Management

        private long mCurrentBytesRead;  // total bytes downloaded
        private long mTotalDownloadSize; // total download size
        private int mCurrentDownload;    // current file download
        private int mTotalFiles;

        private MsgUpdPatchType mCurrentDownloadType; // to check whether the client must be restarted or not
        private string mDomain;
        private readonly Queue<UpdatePatch> mPatches = new();
        private Stopwatch mDownloadSw;

        private string mOpenOnClose;

        public async Task PrepareDownloadingAsync(MsgUpdPatchType type, List<UpdatePatch> patches, string domain)
        {
            SetProgressValue(0);

            mCurrentBytesRead = 0;
            mTotalDownloadSize = 0;
            mCurrentDownload = 0;
            mTotalFiles = 0;

            mCurrentDownloadType = type;
            mDomain = domain;
            mPatches.Clear();
            Process[] procList = Process.GetProcessesByName("Conquer");
            if (procList.Length > 0)
            {
                if (MessageBox.Show(this, LocaleManager.GetString("StrConquerRunning"),
                                    LocaleManager.GetString("StrConquerRunningTitle"), MessageBoxButtons.YesNo) ==
                    DialogResult.Yes)
                {
                    foreach (Process proc in procList)
                    {
                        proc.Kill();
                    }
                }
                else
                {
                    LocaleManager.GetString("StrUnauthorizedConquer");
                    SetWindowMode(PanelMode.PlayDisabled);
                    return;
                }
            }

            SetProgressLabel(LocaleManager.GetString("StrLabelCalculatingDownloadAmount", 0, 0));

            foreach (UpdatePatch file in patches)
            {
                var url = $"{domain}/{file.FileName}";
                if (!await RemoteFileHelper.ExistsAsync(url))
                    continue;

                mTotalDownloadSize += await RemoteFileHelper.FileSizeAsync(url);
                mTotalFiles += 1;
                mPatches.Enqueue(file);
                SetProgressLabel(LocaleManager.GetString("StrLabelCalculatingDownloadAmount", mTotalFiles,
                                                         RemoteFileHelper.ParseFileSize(mTotalDownloadSize)));
            }

            await Task.Delay(1000);
            mDownloadSw = new Stopwatch();
            SetWindowMode(PanelMode.Download);
            await StartDownloadingAsync();
        }

        private async Task StartDownloadingAsync()
        {
            if (mPatches.Count < 1)
            {
                // won't happen, but...
                await mLauncherThread.RequestUpdatesAsync();
                return;
            }

            SetProgressMinValue(0);
            SetProgressMaxValue(mTotalDownloadSize);
            SetProgressValue(mCurrentBytesRead);

            UpdatePatch file = mPatches.Dequeue();
            mCurrentDownload++;
            SetProgressLabel(LocaleManager.GetString("StrLabelDownloading",
                                                     mCurrentDownload,
                                                     mTotalFiles,
                                                     RemoteFileHelper.ParseFileSize(mCurrentBytesRead),
                                                     RemoteFileHelper.ParseFileSize(mTotalDownloadSize),
                                                     RemoteFileHelper.ParseDownloadSpeed(0)));


            using var httpClient = new HttpClient();
            using var message = new HttpRequestMessage(HttpMethod.Get, $"{mDomain}/{file.FileName}");
            using HttpResponseMessage response =
                await httpClient.SendAsync(message, HttpCompletionOption.ResponseHeadersRead);

            try
            {
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, LocaleManager.GetString("StrFailedToFetchFile", file.FileName));
                await Log.WriteLogAsync(ex);
                ForceClose();
                return;
            }

            long totalBytes = response.Content.Headers.ContentLength ?? 0;
            if (totalBytes == 0)
            {
                MessageBox.Show(this, LocaleManager.GetString("StrFailedToFetchFile", file.FileName));
                ForceClose();
                return;
            }

            string tempPath = Path.Combine(TempDirectory(), file.FileName);

            await using Stream contentStream = await response.Content.ReadAsStreamAsync();
            await using var fileStream = new FileStream(tempPath, FileMode.Create);

            byte[] buffer = ArrayPool<byte>.Shared.Rent(8192);
            long bytesRead;
            int tick = Environment.TickCount;
            mDownloadSw.Start();
            while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                mCurrentBytesRead += bytesRead;

                await fileStream.WriteAsync(buffer, 0, (int) bytesRead);

                if (Environment.TickCount - tick > 100)
                {
                    SetProgressLabel(LocaleManager.GetString("StrLabelDownloading",
                                                             mCurrentDownload,
                                                             mTotalFiles,
                                                             RemoteFileHelper.ParseFileSize(mCurrentBytesRead),
                                                             RemoteFileHelper.ParseFileSize(mTotalDownloadSize),
                                                             RemoteFileHelper.ParseDownloadSpeed(
                                                                 (long) (mCurrentBytesRead /
                                                                         mDownloadSw.Elapsed.TotalSeconds))));

                    SetProgressValue(mCurrentBytesRead);
                    tick = Environment.TickCount;
                }
            }

            ArrayPool<byte>.Shared.Return(buffer);
            fileStream.Close();
            mDownloadSw.Stop();

            // start extracting
            if (!tempPath.GetSha256().ToUpperInvariant().Equals(file.Hash.ToUpperInvariant()))
            {
                MessageBox.Show(this, LocaleManager.GetString("StrHashConfirmationFailed"));
                ForceClose();
                return;
            }

            SetProgressLabel(LocaleManager.GetString("StrInstallUpdate", Path.GetFileNameWithoutExtension(tempPath)));

            if (mCurrentDownloadType == MsgUpdPatchType.Client)
            {
                mOpenOnClose = tempPath;
                ForceClose();
                return;
            }

            //#if DEBUG
            //            string extractionPath = Path.Combine(Environment.CurrentDirectory, "AutoPatch", "Test");
            //            if (!Directory.Exists(extractionPath))
            //            {
            //                DirectoryInfo info = Directory.CreateDirectory(extractionPath);
            //                info.Attributes &= ~FileAttributes.ReadOnly;
            //            }
            //#else
            //            string extractionPath = WorkingDirectory;
            //#endif

            string extractionPath = WorkingDirectory;
            bool extractionResult = await UnZipFileHelper.UnZipAsync(tempPath, extractionPath,
                                                                     async (currentFile, files, name) =>
                                                                     {
                                                                         if (currentFile == -1 && files == -1) // Error
                                                                         {
                                                                             MessageBox.Show(
                                                                                 LocaleManager.GetString(
                                                                                     "StrExtractionError"));
                                                                             await Log.WriteLogAsync(
                                                                                 "AutoPatch", LogLevel.Exception, name);
                                                                             return;
                                                                         }

                                                                         SetProgressMaxValue(files);
                                                                         SetProgressValue(currentFile);
                                                                         SetProgressLabel(
                                                                             LocaleManager.GetString(
                                                                                 "StrInstallUpdateEx", file.FileName,
                                                                                 name, currentFile, files));
                                                                     });

            if (!extractionResult)
            {
                ForceClose();
                return;
            }

            CheckVersionFile();
            await StartDownloadingAsync();
        }

        private static string TempDirectory()
        {
            string path = Path.Combine(WorkingDirectory, "AutoPatch");
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            new DirectoryInfo(path).Attributes &= ~FileAttributes.ReadOnly;
            path = Path.Combine(path, "temp");
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            new DirectoryInfo(path).Attributes &= ~FileAttributes.ReadOnly;
            return path;
        }

        #endregion

        #region Ping

        public void UpdatePingMsg(int request, int reply)
        {
            if (InvokeRequired)
            {
                Invoke(() => UpdatePingMsg(request, reply));
                return;
            }

            int delta = reply - request;
            if (delta < 0)
            {
                LblPingDisplay.Text = LocaleManager.GetString("StrDisconnected");
                LblPingDisplay.ForeColor = SystemColors.ButtonHighlight;
            }
            else if (delta == 0)
            {
                LblPingDisplay.Text = LocaleManager.GetString("StrLocalPing");
                LblPingDisplay.ForeColor = Color.Green;
            }
            else if (delta < 50)
            {
                LblPingDisplay.Text = LocaleManager.GetString("StrDisplayPing", delta);
                LblPingDisplay.ForeColor = Color.Green;
            }
            else if (delta < 150)
            {
                LblPingDisplay.Text = LocaleManager.GetString("StrDisplayPing", delta);
                LblPingDisplay.ForeColor = Color.Yellow;
            }
            else if (delta < 300)
            {
                LblPingDisplay.Text = LocaleManager.GetString("StrDisplayPing", delta);
                LblPingDisplay.ForeColor = Color.Orange;
            }
            else
            {
                LblPingDisplay.Text = LocaleManager.GetString("StrDisplayPing", delta);
                LblPingDisplay.ForeColor = Color.Red;
            }
        }

        #endregion

        #region Play

        public void Play()
        {
            string path = Path.Combine(WorkingDirectory, "Dragon.Launch.exe");
            Process game = new Process
            {
                StartInfo =
                {
                    WorkingDirectory = WorkingDirectory,
                    FileName = path,
                    Arguments = $"\"{WorkingDirectory}\" Conquer.exe blacknull"
                }
            };

            if (game.Start())
            {
                game.WaitForExit();
                int processId = game.ExitCode;
                if (processId is <= 0 or 160)
                {
                    MessageBox.Show(this, $@"Something went wrong when injecting. Injector returned {processId} [No process ID]");
                    return;
                }

                //MessageBox.Show(this, $@"Conquer.exe Process ID: {processId}");
                if (mClients.ContainsKey(processId))
                {
                    try
                    {
                        Process p = Process.GetProcessById(processId);
                        p.Close();
                    }
                    catch
                    {
                        // Process is not running
                    }
                }
                mClients.Add(processId, game);
            }
            else
            {
                MessageBox.Show(this, @"Could not start injected Conquer.exe!");
            }
        }

        #endregion

        private enum PanelMode
        {
            Download,
            NoProgress,
            Play,
            PlayDisabled
        }

        private void BtnSettings_Click(object sender, EventArgs e)
        {
            new FrmClientConfig().ShowDialog(this);
        }
    }
}