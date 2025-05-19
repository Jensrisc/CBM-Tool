using System;
using System.Windows.Forms;
using System.Linq;
using System.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DualPageApp
{
    
    public class SplashScreen : Form
{
    private Label lblStatus;
    private ProgressBar progressBar;

    public SplashScreen()
    {
        Width = 400;
        Height = 200;
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;

        this.BackColor = System.Drawing.Color.White;

        lblStatus = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = 40,
            TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
            Text = "Prüfe auf Updates..."
        };

        progressBar = new ProgressBar
        {
            Dock = DockStyle.Top,
            Height = 30,
            Minimum = 0,
            Maximum = 100,
            Value = 0
        };

        Controls.Add(progressBar);
        Controls.Add(lblStatus);
    }

    protected override async void OnShown(EventArgs e)
    {
        base.OnShown(e);
        await CheckForUpdatesAsync();
    }

    private async Task CheckForUpdatesAsync()
    {
        try
        {
            string currentVersion = Application.ProductVersion; // z.B. "1.0.0"
            string repoOwner = "Jensrisc";
            string repoName = "CBM-Tool";
            string token = "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";

            using HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("CBMImportToolUpdater");

            string apiUrl = $"https://api.github.com/repos/{repoOwner}/{repoName}/releases/latest";
            var response = await client.GetAsync(apiUrl);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            dynamic release = Newtonsoft.Json.JsonConvert.DeserializeObject(json);
            string latestVersion = release.tag_name;

            if (IsNewVersionAvailable(currentVersion, latestVersion))
            {
                lblStatus.Text = $"Neue Version {latestVersion} gefunden. Lade herunter...";
                
                // Beispiel: Nimm ersten Asset-Link (du kannst anpassen)
                string assetUrl = release.assets[0].browser_download_url;

                string tempFile = Path.Combine(Path.GetTempPath(), "update.exe");
                await DownloadFileWithProgressAsync(assetUrl, tempFile);

                lblStatus.Text = "Update heruntergeladen. Starte Installation...";
                await Task.Delay(1000);

                // Hier Update ausführen, z.B. Prozess starten und aktuelles Programm schließen
                System.Diagnostics.Process.Start(tempFile);
                Application.Exit();
            }
            else
            {
                lblStatus.Text = "Keine Updates gefunden.";
                await Task.Delay(1000);
                OpenMainForm();
            }
        }
        catch (Exception ex)
        {
            lblStatus.Text = $"Fehler bei Update-Prüfung: {ex.Message}";
            await Task.Delay(2000);
            OpenMainForm();
        }
    }

    private bool IsNewVersionAvailable(string current, string latest)
    {
        Version vCurrent = new Version(current);
        Version vLatest = new Version(latest.TrimStart('v'));
        return vLatest > vCurrent;
    }

    private async Task DownloadFileWithProgressAsync(string url, string destinationPath)
    {
        using HttpClient client = new HttpClient();
        using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? -1L;
        var canReportProgress = totalBytes != -1;

        using var contentStream = await response.Content.ReadAsStreamAsync();
        using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

        var buffer = new byte[8192];
        long totalRead = 0;
        int bytesRead;

        while ((bytesRead = await contentStream.ReadAsync(buffer.AsMemory(0, buffer.Length))) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
            totalRead += bytesRead;
            if (canReportProgress)
            {
                int progress = (int)((totalRead * 100) / totalBytes);
                progressBar.Value = progress > 100 ? 100 : progress;
                lblStatus.Text = $"Lade herunter... {progress}%";
                Application.DoEvents(); // UI aktualisieren
            }
        }

        progressBar.Value = 100;
        lblStatus.Text = "Download abgeschlossen.";
    }

    private void OpenMainForm()
    {
        var mainForm = new DualPageApp.MainForm();
        mainForm.Show();
        this.Close();
    }
}
    public class MainForm : Form
    {
        private Button btnRanger;
        private Button btnThermo;
        private Panel panelContent;
        private TextBox consoleOutput;

        public MainForm()
        {
            Text = "CBM Import Tool";
            Width = 500;
            BackColor = System.Drawing.Color.White;
            Height = 350;

            btnRanger = new Button { Text = "Ranger", Width = 100, Left = 50, Top = 20 };
            btnThermo = new Button { Text = "Thermo", Width = 100, Left = 200, Top = 20 };

            panelContent = new Panel { Left = 20, Top = 70, Width = 440, Height = 180, BorderStyle = BorderStyle.FixedSingle };
            consoleOutput = new TextBox { Left = 20, Top = 260, Width = 440, Height = 50, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical };

            btnRanger.Click += (s, e) => ShowRangerPage();
            btnThermo.Click += (s, e) => ShowThermoPage();

            Controls.Add(btnRanger);
            Controls.Add(btnThermo);
            Controls.Add(panelContent);
            Controls.Add(consoleOutput);
        }

        private void ShowStatus(string message)
        {
            if (consoleOutput.InvokeRequired)
            {
                consoleOutput.Invoke(new Action(() => AddStatusToConsole(message)));
            }
            else
            {
                AddStatusToConsole(message);
            }
        }

        private void AddStatusToConsole(string message)
        {
            consoleOutput.AppendText($"{message}\r\n");
        }

        private void ShowRangerPage()
        {
            panelContent.Controls.Clear();

            Label lbl = new Label { Text = "Willkommen auf der Ranger-Seite", Left = 30, Top = 10, Width = 300 };
            Button btnUploadData = new Button { Text = "Upload Data", Width = 120, Left = 30, Top = 50 };
            Button btnUploadFeedback = new Button { Text = "Upload Feedback", Width = 120, Left = 160, Top = 50 };

            ComboBox comboBox = new ComboBox { Left = 30, Top = 100, Width = 250 };
            string[] options = { "DUS4", "DUS2", "PAD1", "PAD2", "SCN2", "STR1", "FRA7", "CGN1" };
            comboBox.Items.AddRange(options);

            string machineName = Environment.MachineName;
            int index = Array.FindIndex(options, option => machineName.IndexOf(option, StringComparison.OrdinalIgnoreCase) >= 0);
            comboBox.SelectedIndex = index >= 0 ? index : 0;

            btnUploadData.Click += (s, e) =>
            {
                RenameFiles(comboBox.SelectedItem?.ToString() ?? "");
                OpenWebsite();
            };

            btnUploadFeedback.Click += (s, e) =>
            {
                OpenWebsiteFeedback();
            };

            panelContent.Controls.Add(lbl);
            panelContent.Controls.Add(btnUploadData);
            panelContent.Controls.Add(btnUploadFeedback);
            panelContent.Controls.Add(comboBox);
        }

        private void ShowThermoPage()
        {
            panelContent.Controls.Clear();
            Label lbl = new Label { Text = "Willkommen auf der Thermo-Seite", Left = 30, Top = 30, Width = 300 };
            panelContent.Controls.Add(lbl);
        }

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

        private const int SW_MINIMIZE = 6;



        private void OpenWebsite()
        {
            try
            {
                string url = "https://conduit.security.a2z.com/accounts/aws/089910738259/attributes";
                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
                ShowStatus("Anmeldeseite geöffnet.");
                Thread.Sleep(3000);
               
            }
            catch (Exception ex)
            {
                ShowStatus($"Fehler beim Öffnen der Website: {ex.Message}");
            }

            // Zweite URL: mit SendKeys
            SendKeys.SendWait("%d");  // Fokus auf die Adressleiste
            string secondUrl = "https://conduit.security.a2z.com/console?awsAccountId=089910738259&awsPartition=aws&accountName=EURME-PredictiveAnalytics&sessionDuration=43200&iamRole=arn:aws:iam::089910738259:role/S3_Ranger_Tech";
            Clipboard.SetText(secondUrl);
            SendKeys.SendWait("^v");  // Einfügen
            SendKeys.SendWait("{ENTER}");
            ShowStatus("Konsole geöffnet.");
            Thread.Sleep(6000);

               // Dritte URL öffnen mit Prüfung ob Chrome läuft
        string thirdUrl = "eu-west-1.console.aws.amazon.com/s3/buckets/ranger-production?region=eu-west-1&bucketType=general&prefix=data/&showversions=false";

        bool chromeRunning = Process.GetProcessesByName("chrome").Any();

        if (chromeRunning)
        {
            // Wenn Chrome läuft, SendKeys-Methode benutzen
            SendKeys.SendWait("%d"); // Fokus auf die Adressleiste
            SendKeys.Send("{LEFT}"); // Cursor ans Ende bewegen

            // Cursor 30-mal nach rechts bewegen
            for (int i = 0; i < 30; i++)
            {
                SendKeys.Send("{RIGHT}");
            }

            // Alles nach der aktuellen Position markieren und löschen
            SendKeys.Send("+{END}");
            SendKeys.Send("{BACKSPACE}");

            // Neue URL einfügen
            Clipboard.SetText(thirdUrl);
            SendKeys.SendWait("^v");
            SendKeys.SendWait("{ENTER}");
            ShowStatus("S3-Data geöffnet.");
        }
        else
        {
            // Wenn Chrome nicht läuft, URL mit Process.Start öffnen
            string fullUrl = "https://" + thirdUrl;
            Process.Start(new ProcessStartInfo { FileName = fullUrl, UseShellExecute = true });
            ShowStatus("S3-Data per Process.Start geöffnet.");
        }
    }

   private void OpenWebsiteFeedback()
{
    // Prüfen, ob Chrome läuft
    bool isChromeRunning = Process.GetProcessesByName("chrome").Length > 0;

    if (isChromeRunning)
    {
        try
        {
            string url1 = "https://conduit.security.a2z.com/accounts/aws/089910738259/attributes";
            Process.Start(new ProcessStartInfo { FileName = url1, UseShellExecute = true });
            ShowStatus("Anmeldeseite geöffnet.");
            Thread.Sleep(3000);

            // Zweite URL per SendKeys einfügen
            SendKeys.SendWait("%d");  // Fokus auf Adressleiste
            string url2 = "https://conduit.security.a2z.com/console?awsAccountId=089910738259&awsPartition=aws&accountName=EURME-PredictiveAnalytics&sessionDuration=43200&iamRole=arn:aws:iam::089910738259:role/S3_Ranger_Tech";
            Clipboard.SetText(url2);
            SendKeys.SendWait("^v");
            SendKeys.SendWait("{ENTER}");
            ShowStatus("Konsole geöffnet.");
            Thread.Sleep(6000);

            // Dritte URL per SendKeys
            SendKeys.SendWait("%d");  // Fokus auf Adressleiste
            SendKeys.Send("{LEFT}");

            for (int i = 0; i < 29; i++)
                SendKeys.Send("{RIGHT}");

            SendKeys.Send("+{END}");
            SendKeys.Send("{BACKSPACE}");

            string url3 = ".eu-west-1.console.aws.amazon.com/s3/buckets/ranger-production?region=eu-west-1&bucketType=general&prefix=feedback/&showversions=false";
            Clipboard.SetText(url3);
            SendKeys.SendWait("^v");
            SendKeys.SendWait("{ENTER}");
            ShowStatus("S3-Feedback geöffnet.");
        }
        catch (Exception ex)
        {
            ShowStatus($"Fehler beim Öffnen der Website: {ex.Message}");
        }
    }
    else
    {
        // Chrome läuft nicht, alternative Seite öffnen
        string alternativeUrl = "https://eu-west-1.console.aws.amazon.com/s3/buckets/ranger-production?region=eu-west-1&bucketType=general&prefix=feedback/&showversions=false";
        try
        {
            Process.Start(new ProcessStartInfo { FileName = alternativeUrl, UseShellExecute = true });
            ShowStatus("Alternative Website geöffnet, da Chrome nicht läuft.");
        }
        catch (Exception ex)
        {
            ShowStatus($"Fehler beim Öffnen der alternativen Website: {ex.Message}");
        }
    }
}



private void RenameFiles(string location)
{
    foreach (var drive in System.IO.DriveInfo.GetDrives())
    {
        if (drive.DriveType == System.IO.DriveType.Removable && drive.IsReady)
        {
            var files = System.IO.Directory.GetFiles(drive.Name, "*.bag")
                .Concat(System.IO.Directory.GetFiles(drive.Name, "*.mp4"));
            var renamedFiles = new HashSet<string>();

            foreach (var file in files)
            {
                try
                {
                    string directoryName = System.IO.Path.GetDirectoryName(file);
                    if (directoryName != null)
                    {
                        string currentName = System.IO.Path.GetFileNameWithoutExtension(file);
                        foreach (var opt in new string[] {"DUS4", "DUS2", "PAD1", "PAD2", "SCN2", "STR1", "FRA7", "CGN1"})
                        {
                            if (currentName.EndsWith("_" + opt, StringComparison.OrdinalIgnoreCase))
                            {
                                currentName = currentName.Substring(0, currentName.LastIndexOf("_" + opt, StringComparison.OrdinalIgnoreCase));
                                break;
                            }
                        }
                        string newFileName = $"{currentName}_{location}{System.IO.Path.GetExtension(file)}";
                        string newFilePath = System.IO.Path.Combine(directoryName, newFileName);
                        System.IO.File.Move(file, newFilePath);
                        renamedFiles.Add(newFilePath);
                        ShowStatus($"Datei umbenannt: {file} -> {newFilePath}");
                    }
                }
                catch (Exception ex)
                {
                    ShowStatus($"Fehler beim Umbenennen der Datei: {ex.Message}");
                }
            }

            try
            {
                var allFiles = System.IO.Directory.GetFiles(drive.Name);
                foreach (var file in allFiles)
                {
                    if (!renamedFiles.Contains(file) && !file.EndsWith(".bag", StringComparison.OrdinalIgnoreCase) && !file.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase))
                    {
                        System.IO.File.Delete(file);
                        ShowStatus($"Datei gelöscht: {file}");
                    }
                }
            }
            catch (Exception ex)
            {
                ShowStatus($"Fehler beim Löschen von Dateien: {ex.Message}");
            }
        }
    }
}

    static class Program
{
    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        // Erst SplashScreen zeigen (wichtig: Application.Run blockiert bis SplashScreen schließt)
        using (var splash = new SplashScreen())
        {
            Application.Run(splash);
        }

        // Nach dem Schließen des SplashScreens dann MainForm starten
        Application.Run(new MainForm());
    }
}

}}
