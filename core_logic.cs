// ======== CORE LOGIC ========

        private string GetAppFolder()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), APP_NAME);
        }

        private void Log(string msg)
        {
            if (logBox == null) return;
            logBox.AppendText("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + msg + "\n");
            logBox.ScrollToCaret();
            Application.DoEvents();
        }

        private void RunFixRegistry()
        {
            Log("Memulai perbaikan sistem...");
            RunFullInstall();
        }

        private void RunFullInstall()
        {
            try
            {
                string apd = GetAppFolder();
                if (!Directory.Exists(apd)) Directory.CreateDirectory(apd);
                string manifestPath = Path.Combine(apd, "manifest.xml");
                File.WriteAllText(manifestPath, GetManifestXml());

                string batPath = Path.Combine(Path.GetTempPath(), "pasang_gandi.bat");
                string wef16 = @"HKCU\Software\Microsoft\Office\16.0\Wef";
                string wordWef = @"HKCU\Software\Microsoft\Office\16.0\Word\Wef";
                string privacy = @"HKCU\Software\Microsoft\Office\16.0\Common\Privacy";
                string trustLoc = @"HKCU\Software\Microsoft\Office\16.0\Word\Security\Trusted Locations\ParafraseGandi";
                string trustCent = @"HKCU\Software\Microsoft\Office\16.0\WEF\TrustCenter";

                // Convert path to file:// URL for developer sideload
                string fileUrl = "file:///" + manifestPath.Replace("\\", "/");

                string batCode = "@echo off\n" +
                    "color 0B\n" +
                    "echo ==================================================\n" +
                    "echo MEMASANG PARAFRASE GANDI v5.2 (NUCLEAR BYPASS)\n" +
                    "echo ==================================================\n" +
                    "echo.\n" +
                    "echo Menutup Microsoft Word...\n" +
                    "taskkill /f /im winword.exe >nul 2>&1\n" +
                    "timeout /t 1 >nul\n" +
                    "echo.\n" +
                    "echo 1. Membuka Blokir Keamanan Office (Trust Center)...\n" +
                    "reg add \"" + privacy + "\" /v ConnectedExperiencesAllowed /t REG_DWORD /d 1 /f >nul 2>&1\n" +
                    "reg add \"" + privacy + "\" /v OptionalConnectedExperiencesAllowed /t REG_DWORD /d 1 /f >nul 2>&1\n" +
                    "reg add \"" + trustCent + "\" /v DisableAllWefAddins /t REG_DWORD /d 0 /f >nul 2>&1\n" +
                    "reg add \"" + trustCent + "\" /v BlockWebAddins /t REG_DWORD /d 0 /f >nul 2>&1\n" +

                    "echo 2. Membuat Network Share Lokal (IP-Link)...\n" +
                    "net share GandiAddin /delete /y >nul 2>&1\n" +
                    "net share GandiAddin=\"" + apd + "\" /grant:Everyone,READ >nul 2>&1\n" +

                    "echo 3. Mendaftarkan Lokasi Terpercaya...\n" +
                    "reg add \"" + trustLoc + "\" /v Path /t REG_SZ /d \"" + apd + "\" /f >nul 2>&1\n" +
                    "reg add \"" + trustLoc + "\" /v AllowSubfolders /t REG_DWORD /d 1 /f >nul 2>&1\n" +

                    "echo 4. Injeksi Pendaftaran Add-in (Multimode)...\n" +
                    "reg delete \"" + wef16 + "\\Developer\\" + AppId + "\" /f >nul 2>&1\n" +
                    "reg delete \"" + wef16 + "\\TrustedCatalogs\\" + CATALOG_ID + "\" /f >nul 2>&1\n" +

                    "reg add \"" + wef16 + "\\TrustedCatalogs\\" + CATALOG_ID + "\" /v Id /t REG_SZ /d \"" + CATALOG_ID + "\" /f >nul 2>&1\n" +
                    "reg add \"" + wef16 + "\\TrustedCatalogs\\" + CATALOG_ID + "\" /v Url /t REG_SZ /d \"\\\\127.0.0.1\\GandiAddin\" /f >nul 2>&1\n" +
                    "reg add \"" + wef16 + "\\TrustedCatalogs\\" + CATALOG_ID + "\" /v Flags /t REG_DWORD /d 1 /f >nul 2>&1\n" +
                    "reg add \"" + wef16 + "\\TrustedCatalogs\\" + CATALOG_ID + "\" /v ShowInMenu /t REG_DWORD /d 1 /f >nul 2>&1\n" +

                    "reg add \"" + wordWef + "\\TrustedCatalogs\\" + CATALOG_ID + "\" /v Id /t REG_SZ /d \"" + CATALOG_ID + "\" /f >nul 2>&1\n" +
                    "reg add \"" + wordWef + "\\TrustedCatalogs\\" + CATALOG_ID + "\" /v Url /t REG_SZ /d \"\\\\127.0.0.1\\GandiAddin\" /f >nul 2>&1\n" +
                    "reg add \"" + wordWef + "\\TrustedCatalogs\\" + CATALOG_ID + "\" /v Flags /t REG_DWORD /d 1 /f >nul 2>&1\n" +

                    "reg add \"" + wef16 + "\\Developer\\" + AppId + "\" /v Url /t REG_SZ /d \"" + fileUrl + "\" /f >nul 2>&1\n" +
                    "reg add \"" + wordWef + "\\Developer\\" + AppId + "\" /v Url /t REG_SZ /d \"" + fileUrl + "\" /f >nul 2>&1\n" +

                    "echo 5. Membersihkan Cache Office...\n" +
                    "rmdir /s /q \"%LOCALAPPDATA%\\Microsoft\\Office\\16.0\\Wef\" >nul 2>&1\n" +
                    "echo ==================================================\n" +
                    "echo SUKSES! Silakan cek tab 'Shared Folder' di My Add-ins.\n" +
                    "echo Membuka Microsoft Word...\n" +
                    "echo ==================================================\n" +
                    "start winword.exe\n" +
                    "timeout /t 5 >nul\n" +
                    "del \"%~f0\"\n";

                File.WriteAllText(batPath, batCode);

                ProcessStartInfo psi = new ProcessStartInfo(batPath) { UseShellExecute = true };
                Process.Start(psi);

                Log("Menjalankan script instalasi...");
                Log("Lihat jendela hitam (CMD) yang muncul.");
                Log("Selesai. Cek Word Anda!");
            }
            catch (Exception ex)
            {
                Log("ERROR: " + ex.Message);
                MessageBox.Show("Terjadi kesalahan:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RunUninstall()
        {
            if (MessageBox.Show("Hapus Add-in dari Word?", "Konfirmasi", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
            try
            {
                string batPath = Path.Combine(Path.GetTempPath(), "hapus_gandi.bat");
                string wef16 = @"HKCU\Software\Microsoft\Office\16.0\Wef";

                string batCode = "@echo off\n" +
                    "color 0C\n" +
                    "echo MENGHAPUS PARAFRASE GANDI...\n" +
                    "taskkill /f /im winword.exe >nul 2>&1\n" +
                    "reg delete \"" + wef16 + "\\Developer\\" + AppId + "\" /f >nul 2>&1\n" +
                    "reg delete \"" + wef16 + "\\TrustedCatalogs\\" + CATALOG_ID + "\" /f >nul 2>&1\n" +
                    "reg delete \"" + wef16 + "\\TrustedCatalogs\\" + AppId + "\" /f >nul 2>&1\n" +
                    "rmdir /s /q \"%LOCALAPPDATA%\\Microsoft\\Office\\16.0\\Wef\" >nul 2>&1\n" +
                    "echo Selesai dihapus.\n" +
                    "timeout /t 3 >nul\n" +
                    "del \"%~f0\"\n";

                File.WriteAllText(batPath, batCode);
                ProcessStartInfo psi = new ProcessStartInfo(batPath) { UseShellExecute = true };
                Process.Start(psi);

                MessageBox.Show("Terminal akan memproses penghapusan.", "Selesai", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ShowTab(0);
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }

        private void RunCheckUpdate()
        {
            try
            {
                using (WebClient wc = new WebClient())
                {
                    wc.Headers.Add("User-Agent", "GandiManager");
                    string json = wc.DownloadString(REMOTE_BASE + "/version.json");
                    if (json.Contains(VERSION))
                    {
                        MessageBox.Show("Anda sudah menggunakan versi terbaru.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        if (MessageBox.Show("Versi baru tersedia. Perbarui manifest sekarang?", "Update", MessageBoxButtons.YesNo) == DialogResult.Yes)
                        {
                            string apd = GetAppFolder();
                            wc.DownloadFile(REMOTE_BASE + "/manifest.xml", Path.Combine(apd, "manifest.xml"));
                            MessageBox.Show("Manifest diperbarui!", "Sukses", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Gagal cek update:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private string DetectWordVersion()
        {
            string[] paths = {
                @"Software\Microsoft\Office\16.0\Word\InstallRoot",
                @"Software\Microsoft\Office\15.0\Word\InstallRoot"
            };
            string[] names = { "Word 2016/2019/2021/365", "Word 2013" };
            for (int i = 0; i < paths.Length; i++)
            {
                try
                {
                    using (RegistryKey rk = Registry.LocalMachine.OpenSubKey(paths[i]))
                    {
                        if (rk != null && rk.GetValue("Path") != null) return names[i];
                    }
                    using (RegistryKey rk = Registry.CurrentUser.OpenSubKey(paths[i]))
                    {
                        if (rk != null && rk.GetValue("Path") != null) return names[i];
                    }
                }
                catch { }
            }
            try
            {
                using (RegistryKey rk = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Office\ClickToRun\Configuration"))
                {
                    if (rk != null)
                    {
                        string ver = rk.GetValue("VersionToReport", "").ToString();
                        if (ver.StartsWith("16.")) return "Word 365/2021 (v" + ver + ")";
                    }
                }
            }
            catch { }
            return "Tidak Ditemukan";
        }

        private void RefreshStatusLabel(Label statusValue)
        {
            bool installed = false;
            try
            {
                using (RegistryKey rk = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Office\16.0\Wef\Developer\" + AppId))
                {
                    installed = (rk != null);
                }
            }
            catch { }

            if (installed)
            {
                statusValue.Text = "Siap Digunakan";
                statusValue.ForeColor = successGreen;
            }
            else
            {
                statusValue.Text = "Belum Terpasang";
                statusValue.ForeColor = dangerRed;
            }
        }

        private string GetManifestXml()
        {
            return @"<?xml version=""1.0"" encoding=""UTF-8""?>
<OfficeApp xmlns=""http://schemas.microsoft.com/office/appforoffice/1.1"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:type=""TaskPaneApp"">
  <Id>" + RAW_ID + @"</Id>
  <Version>1.0.0.0</Version>
  <ProviderName>Parafrase Gandi</ProviderName>
  <DefaultLocale>id-ID</DefaultLocale>
  <DisplayName DefaultValue=""Parafrase Gandi""/>
  <Description DefaultValue=""AI Writing Assistant for Microsoft Word""/>
  <IconUrl DefaultValue=""" + REMOTE_BASE + @"/assets/icon-32.png""/>
  <HighResolutionIconUrl DefaultValue=""" + REMOTE_BASE + @"/assets/icon-64.png""/>
  <SupportUrl DefaultValue=""" + REMOTE_BASE + @"""/>
  <Hosts>
    <Host Name=""Document""/>
  </Hosts>
  <DefaultSettings>
    <SourceLocation DefaultValue=""" + REMOTE_BASE + @"/taskpane.html""/>
  </DefaultSettings>
  <Permissions>ReadWriteDocument</Permissions>
</OfficeApp>";
        }
    }

    class Program
    {
        static bool IsAdmin()
        {
            try
            {
                WindowsIdentity id = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(id);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch { return false; }
        }

        [STAThread]
        static void Main()
        {
            if (!IsAdmin())
            {
                try
                {
                    ProcessStartInfo proc = new ProcessStartInfo();
                    proc.UseShellExecute = true;
                    proc.WorkingDirectory = Environment.CurrentDirectory;
                    proc.FileName = Application.ExecutablePath;
                    proc.Verb = "runas";
                    Process.Start(proc);
                }
                catch { } // User refused UAC
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new ManagerForm());
        }
    }
}
