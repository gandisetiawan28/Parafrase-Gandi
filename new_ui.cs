// --- DESIGN TOKENS ---
        private static Color primary     = ColorTranslator.FromHtml("#0058bc");
        private static Color primaryContainer = ColorTranslator.FromHtml("#0070eb");
        private static Color surface     = ColorTranslator.FromHtml("#f4f6fb");
        private static Color onSurface   = ColorTranslator.FromHtml("#1a1c1f");
        private static Color onSurfaceVariant = ColorTranslator.FromHtml("#5a5e6e");
        private static Color surfaceContainerLow  = ColorTranslator.FromHtml("#eef0f6");
        private static Color surfaceContainerHighest = ColorTranslator.FromHtml("#dde0ea");
        private static Color surfaceContainerLowest  = ColorTranslator.FromHtml("#ffffff");
        private static Color primaryFixed = ColorTranslator.FromHtml("#d8e2ff");
        private static Color successGreen  = ColorTranslator.FromHtml("#15803d");
        private static Color successGreenBg = ColorTranslator.FromHtml("#dcfce7");
        private static Color dangerRed  = ColorTranslator.FromHtml("#ba1a1a");
        private static Color infoBlue   = ColorTranslator.FromHtml("#2563eb");
        private static Color infoBlueBg = ColorTranslator.FromHtml("#eff6ff");

        private static string VERSION = "2.4.0";
        private string AppId { get { return "{" + RAW_ID + "}"; } }

        private Panel mainContent;
        private int activeTab = 0;
        private Panel[] navItems;
        private RichTextBox logBox;

        // -------- FORM SETUP --------
        public ManagerForm()
        {
            this.Text = "Parafrase Gandi Manager";
            try { this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }
            this.Size = new Size(500, 820);
            this.MinimumSize = new Size(440, 700);
            this.BackColor = surface;
            this.Font = new Font("Segoe UI", 10F);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.DoubleBuffered = true;
            try { ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072 | (SecurityProtocolType)768; } catch { }
            BuildUI();
            ShowTab(0);
        }

        // -------- CHROME BUILD --------
        private void BuildUI()
        {
            this.BackColor = surface;

            // --- HEADER ---
            Panel header = new Panel
            {
                Dock = DockStyle.Top, Height = 62,
                BackColor = Color.FromArgb(240, 244, 246, 251),
                Padding = new Padding(20, 0, 20, 0)
            };
            this.Controls.Add(header);

            FlowLayoutPanel logoRow = new FlowLayoutPanel
            {
                Dock = DockStyle.Left, Width = 260, WrapContents = false,
                BackColor = Color.Transparent, Padding = new Padding(0, 0, 0, 0)
            };
            Label icoLbl = new Label
            {
                Text = "\uE8D7", Font = new Font("Segoe MDL2 Assets", 16),
                ForeColor = primary, AutoSize = true,
                Margin = new Padding(0, 20, 8, 0)
            };
            Label appLbl = new Label
            {
                Text = "Parafrase Gandi",
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = onSurface, AutoSize = true,
                Margin = new Padding(0, 18, 0, 0)
            };
            logoRow.Controls.AddRange(new Control[] { icoLbl, appLbl });
            header.Controls.Add(logoRow);

            Label verLabel = new Label
            {
                Text = "v" + VERSION,
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = onSurfaceVariant,
                Dock = DockStyle.Right, Width = 55,
                TextAlign = ContentAlignment.MiddleRight
            };
            header.Controls.Add(verLabel);

            // --- BOTTOM NAV DOCK ---
            Panel navDock = new Panel
            {
                Dock = DockStyle.Bottom, Height = 88,
                BackColor = Color.Transparent,
                Padding = new Padding(32, 0, 32, 20)
            };
            this.Controls.Add(navDock);

            RoundedCard dockCard = new RoundedCard
            {
                Dock = DockStyle.Fill, Radius = 36,
                BackColor = Color.FromArgb(252, 255, 255, 255),
                ShowShadow = true
            };
            navDock.Controls.Add(dockCard);

            TableLayoutPanel navLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 4, BackColor = Color.Transparent
            };
            for (int i = 0; i < 4; i++)
                navLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            dockCard.Controls.Add(navLayout);

            string[] labels = { "Home", "Install", "Update", "Diagnose" };
            string[] icons  = { "\uE80F","\uE896","\uE72C","\uE9D5" };
            navItems = new Panel[4];

            for (int i = 0; i < 4; i++)
            {
                int idx = i;
                Panel tab = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Cursor = Cursors.Hand };
                Label ic = new Label
                {
                    Text = icons[i], Font = new Font("Segoe MDL2 Assets", 17),
                    Dock = DockStyle.Top, Height = 38,
                    TextAlign = ContentAlignment.BottomCenter,
                    ForeColor = onSurfaceVariant, BackColor = Color.Transparent
                };
                Label tx = new Label
                {
                    Text = labels[i], Font = new Font("Segoe UI Semibold", 8F),
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.TopCenter,
                    ForeColor = onSurfaceVariant, BackColor = Color.Transparent
                };
                EventHandler click = (s, e) => ShowTab(idx);
                ic.Click += click; tx.Click += click; tab.Click += click;
                tab.MouseEnter += (s, e) => { if (activeTab != idx) ic.ForeColor = primary; };
                tab.MouseLeave += (s, e) => { if (activeTab != idx) ic.ForeColor = onSurfaceVariant; };
                tab.Tag = new Control[] { ic, tx };
                tab.Controls.Add(tx); tab.Controls.Add(ic);
                navItems[idx] = tab;
                navLayout.Controls.Add(tab, i, 0);
            }

            // --- SCROLL CONTENT ---
            mainContent = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.Transparent,
                Padding = new Padding(0)
            };
            this.Controls.Add(mainContent);
            mainContent.SendToBack();
            header.BringToFront();
            navDock.BringToFront();
        }

        private void ShowTab(int index)
        {
            activeTab = index;
            for (int i = 0; i < navItems.Length; i++)
            {
                var c = (Control[])navItems[i].Tag;
                bool active = (i == index);
                c[0].ForeColor = active ? primary : onSurfaceVariant;
                c[1].ForeColor = active ? primary : onSurfaceVariant;
                c[1].Font = new Font(active ? "Segoe UI Bold" : "Segoe UI Semibold", 8F);
            }
            mainContent.Controls.Clear();
            mainContent.VerticalScroll.Value = 0;
            mainContent.AutoScrollPosition = new Point(0, 0);
            switch (index)
            {
                case 0: BuildHomePage(); break;
                case 1: BuildInstallPage(); break;
                case 2: BuildUpdatePage(); break;
                case 3: BuildDiagnosaPage(); break;
            }
        }

        // -------- HELPER: add card to scroll panel --------
        private RoundedCard AddCard(int pad, ref int y, int w, int h, Color bg, bool shadow = true)
        {
            RoundedCard c = new RoundedCard
            {
                Location = new Point(pad, y),
                Size = new Size(w, h),
                BackColor = bg,
                ShowShadow = shadow,
                Radius = 20
            };
            mainContent.Controls.Add(c);
            y = c.Bottom + 16;
            return c;
        }

        private void AddSpacer(ref int y)
        {
            mainContent.Controls.Add(new Panel
            {
                Location = new Point(0, y),
                Size = new Size(10, 120),
                BackColor = Color.Transparent
            });
        }

        private Label Lbl(string text, Font font, Color color, Rectangle bounds, bool bg = false)
        {
            return new Label
            {
                Text = text, Font = font, ForeColor = color,
                Location = bounds.Location, Size = bounds.Size,
                BackColor = bg ? Color.FromArgb(20, color) : Color.Transparent,
                TextAlign = ContentAlignment.TopLeft
            };
        }

        private PillButton Btn(string text, Font font, Color fg, Color bg, bool grad, Color grad2, int x, int y2, int w, int h)
        {
            return new PillButton
            {
                Text = text, Font = font, ForeColor = fg,
                BackColor = bg, UseGradient = grad, GradientColor2 = grad2,
                Location = new Point(x, y2), Size = new Size(w, h)
            };
        }

        // -------- PAGES --------
        private void BuildHomePage()
        {
            int pad = 18, cw = mainContent.ClientSize.Width - pad * 2, y = 12;

            // Hero gradient card
            RoundedCard hero = AddCard(pad, ref y, cw, 185, primary, false);
            hero.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (LinearGradientBrush lb = new LinearGradientBrush(
                    hero.ClientRectangle, primary, primaryContainer, 45f))
                    e.Graphics.FillRectangle(lb, hero.ClientRectangle);
            };
            hero.Controls.Add(Lbl("Modern Paraphrasing", new Font("Segoe UI", 18, FontStyle.Bold), Color.White, new Rectangle(24, 24, cw - 48, 34)));
            hero.Controls.Add(Lbl("AI-powered semantic engine for professional writing.", new Font("Segoe UI", 9.5f), Color.FromArgb(210, Color.White), new Rectangle(24, 64, cw - 80, 40)));
            PillButton hBtn = Btn("\uE896  Get Started", new Font("Segoe UI Bold", 9.5f), primary, Color.White, false, Color.White, 24, 120, 130, 38);
            hero.Controls.Add(hBtn);
            hBtn.Click += (s, e) => ShowTab(1);

            // Two column row
            int col1 = (int)(cw * 0.6) - 8, col2 = cw - col1 - 16, yRow = y;

            RoundedCard sCard = new RoundedCard { Location = new Point(pad, yRow), Size = new Size(col1, 170), BackColor = surfaceContainerLowest, Radius = 20, ShowShadow = true };
            mainContent.Controls.Add(sCard);
            sCard.Controls.Add(Lbl("\uE713", new Font("Segoe MDL2 Assets", 16), primary, new Rectangle(20, 20, 36, 36)));
            sCard.Controls.Add(Lbl("System Status", new Font("Segoe UI Bold", 13), onSurface, new Rectangle(20, 60, col1 - 40, 26)));
            sCard.Controls.Add(Lbl("All services synchronized.\nPerformance optimized for v" + VERSION + ".", new Font("Segoe UI", 9f), onSurfaceVariant, new Rectangle(20, 90, col1 - 40, 50)));
            sCard.Controls.Add(Lbl("\uE73E  Active", new Font("Segoe UI Bold", 9f), successGreen, new Rectangle(20, 143, 120, 20)));

            RoundedCard rCard = new RoundedCard { Location = new Point(pad + col1 + 16, yRow), Size = new Size(col2, 170), BackColor = surfaceContainerLow, Radius = 20, ShowShadow = true };
            mainContent.Controls.Add(rCard);
            rCard.Controls.Add(Lbl("\uE825", new Font("Segoe MDL2 Assets", 28), primary, new Rectangle(0, 28, col2, 40)) );
            ((Label)rCard.Controls[rCard.Controls.Count - 1]).TextAlign = ContentAlignment.MiddleCenter;
            rCard.Controls.Add(Lbl("Cloud Sync", new Font("Segoe UI Bold", 10), onSurface, new Rectangle(0, 80, col2, 20)));
            ((Label)rCard.Controls[rCard.Controls.Count - 1]).TextAlign = ContentAlignment.MiddleCenter;
            rCard.Controls.Add(Lbl("Secure\nConnection", new Font("Segoe UI", 8.5f), onSurfaceVariant, new Rectangle(0, 104, col2, 36)));
            ((Label)rCard.Controls[rCard.Controls.Count - 1]).TextAlign = ContentAlignment.MiddleCenter;

            y = Math.Max(sCard.Bottom, rCard.Bottom) + 16;

            // Word detection card
            RoundedCard wCard = AddCard(pad, ref y, cw, 82, surfaceContainerLowest);
            Label wIco = new Label { Text = "\uE8A5", Font = new Font("Segoe MDL2 Assets", 22), ForeColor = infoBlue, BackColor = infoBlueBg, Location = new Point(16, 14), Size = new Size(54, 54), TextAlign = ContentAlignment.MiddleCenter };
            wCard.Controls.Add(wIco);
            wCard.Controls.Add(Lbl("Add-in Environment", new Font("Segoe UI Bold", 11), onSurface, new Rectangle(82, 18, cw - 120, 22)));
            wCard.Controls.Add(Lbl("Plugin detected and ready for integration.", new Font("Segoe UI", 9f), onSurfaceVariant, new Rectangle(82, 44, cw - 120, 20)));

            // Diagnostics card
            RoundedCard dCard = AddCard(pad, ref y, cw, 140, surfaceContainerHighest);
            dCard.Controls.Add(Lbl("System Health", new Font("Segoe UI Bold", 11), onSurface, new Rectangle(20, 20, cw - 60, 22)));
            dCard.Controls.Add(Lbl("Run deep diagnostics to ensure the highest fidelity of the Parafrase Gandi engine.", new Font("Segoe UI", 9f), onSurfaceVariant, new Rectangle(20, 46, cw - 40, 50)));
            PillButton dBtn = Btn("Initialize Diagnostics", new Font("Segoe UI Bold", 9.5f), primary, Color.White, false, Color.White, 20, 96, 175, 34);
            dCard.Controls.Add(dBtn);
            dBtn.Click += (s, e) => ShowTab(3);

            AddSpacer(ref y);
        }

        private void BuildInstallPage()
        {
            int pad = 18, cw = mainContent.ClientSize.Width - pad * 2, y = 12;

            // Title card
            RoundedCard hCard = AddCard(pad, ref y, cw, 110, surfaceContainerLow, false);
            hCard.Controls.Add(Lbl("Deployment", new Font("Segoe UI", 22, FontStyle.Bold), onSurface, new Rectangle(24, 20, cw - 48, 40)));
            hCard.Controls.Add(Lbl("Integrate the AI engine into your Word environment.", new Font("Segoe UI", 9.5f), onSurfaceVariant, new Rectangle(24, 66, cw - 48, 36)));

            // Two columns
            int col1 = (int)(cw * 0.58) - 8, col2 = cw - col1 - 16;
            int yRow = y;

            // Activation card
            RoundedCard aCard = new RoundedCard { Location = new Point(pad, yRow), Size = new Size(col1, 340), BackColor = surfaceContainerLowest, Radius = 20, ShowShadow = true };
            mainContent.Controls.Add(aCard);
            Label aIco = new Label { Text = "\uE81E", Font = new Font("Segoe MDL2 Assets", 16), ForeColor = primary, BackColor = primaryFixed, Location = new Point(20, 20), Size = new Size(36, 36), TextAlign = ContentAlignment.MiddleCenter };
            aCard.Controls.Add(aIco);
            aCard.Controls.Add(Lbl("Activation Manager", new Font("Segoe UI Bold", 12), onSurface, new Rectangle(65, 24, col1 - 80, 28)));
            aCard.Controls.Add(Lbl("1. Close Microsoft Word before proceeding.", new Font("Segoe UI", 9f), onSurfaceVariant, new Rectangle(20, 70, col1 - 40, 35)));
            aCard.Controls.Add(Lbl("2. Click 'Deploy Engine' to install.", new Font("Segoe UI", 9f), onSurfaceVariant, new Rectangle(20, 108, col1 - 40, 35)));
            aCard.Controls.Add(Lbl("3. Authorize if a UAC prompt appears.", new Font("Segoe UI", 9f), onSurfaceVariant, new Rectangle(20, 146, col1 - 40, 35)));

            PillButton instBtn = Btn("DEPLOY ENGINE", new Font("Segoe UI Bold", 10), Color.White, primary, true, primaryContainer, 20, 210, col1 - 40, 46);
            instBtn.Click += (s, e) => RunFullInstall();
            aCard.Controls.Add(instBtn);

            PillButton rmvBtn = Btn("Undeploy Engine", new Font("Segoe UI Bold", 9f), dangerRed, Color.Transparent, false, Color.Transparent, 20, 268, col1 - 40, 36);
            rmvBtn.Click += (s, e) => RunUninstall();
            aCard.Controls.Add(rmvBtn);

            // Log card
            RoundedCard lCard = new RoundedCard { Location = new Point(pad + col1 + 16, yRow), Size = new Size(col2, 340), BackColor = Color.FromArgb(22, 24, 28), Radius = 20, ShowShadow = true };
            mainContent.Controls.Add(lCard);
            Label lTitle = new Label { Text = "● PROCESS LOG", Font = new Font("Consolas", 7.5f), ForeColor = Color.FromArgb(120, 180, 120), BackColor = Color.Transparent, Location = new Point(12, 10), Size = new Size(col2 - 24, 18) };
            lCard.Controls.Add(lTitle);
            logBox = new RichTextBox
            {
                Location = new Point(10, 32),
                Size = new Size(col2 - 20, 296),
                BackColor = Color.FromArgb(22, 24, 28),
                ForeColor = primaryFixedDim,
                Font = new Font("Consolas", 8.5F),
                BorderStyle = BorderStyle.None,
                ReadOnly = true, WordWrap = true
            };
            logBox.Text = "> Ready...\n";
            lCard.Controls.Add(logBox);

            y = Math.Max(aCard.Bottom, lCard.Bottom) + 16;
            AddSpacer(ref y);
        }

        private void BuildUpdatePage()
        {
            int pad = 18, cw = mainContent.ClientSize.Width - pad * 2, y = 12;

            // Main update card
            RoundedCard mfCard = AddCard(pad, ref y, cw, 260, surfaceContainerLowest);
            mfCard.Controls.Add(Lbl("\uE896", new Font("Segoe MDL2 Assets", 32), primary, new Rectangle(24, 24, 50, 50)));
            mfCard.Controls.Add(Lbl("AI Version Control", new Font("Segoe UI Bold", 17), onSurface, new Rectangle(24, 82, cw - 48, 34)));
            mfCard.Controls.Add(Lbl("Updating keeps your paraphrasing engine synchronized with the latest linguistic and bypass logic.", new Font("Segoe UI", 9.5f), onSurfaceVariant, new Rectangle(24, 122, cw - 48, 55)));
            PillButton upBtn = Btn("\uE72C  Check for Updates", new Font("Segoe UI Bold", 10), Color.White, primary, true, primaryContainer, 24, 194, 200, 44);
            upBtn.Click += (s, e) => RunCheckUpdate();
            mfCard.Controls.Add(upBtn);
            Label badge = new Label { Text = "\uE73E  v" + VERSION + " STABLE", Font = new Font("Segoe UI Bold", 8f), ForeColor = successGreen, BackColor = successGreenBg, Location = new Point(240, 202), Size = new Size(120, 28), TextAlign = ContentAlignment.MiddleCenter };
            mfCard.Controls.Add(badge);

            // Status row
            RoundedCard sysCard = AddCard(pad, ref y, cw, 72, surfaceContainerLow);
            Label sIco = new Label { Text = "\uE753", Font = new Font("Segoe MDL2 Assets", 18), ForeColor = primary, BackColor = surfaceContainerLowest, Location = new Point(16, 17), Size = new Size(38, 38), TextAlign = ContentAlignment.MiddleCenter };
            sysCard.Controls.Add(sIco);
            sysCard.Controls.Add(Lbl("Status System", new Font("Segoe UI Bold", 10), onSurface, new Rectangle(66, 15, 180, 20)));
            sysCard.Controls.Add(Lbl("Last checked: just now", new Font("Segoe UI", 8.5f), onSurfaceVariant, new Rectangle(66, 38, 180, 18)));
            Label sRight = new Label { Text = "You're on the latest", Font = new Font("Segoe UI Semibold", 9), ForeColor = primary, TextAlign = ContentAlignment.MiddleRight, Location = new Point(cw - 165, 20), Size = new Size(150, 30), BackColor = Color.Transparent };
            sysCard.Controls.Add(sRight);

            AddSpacer(ref y);
        }

        private void BuildDiagnosaPage()
        {
            int pad = 18, cw = mainContent.ClientSize.Width - pad * 2, y = 12;

            // Title card
            RoundedCard hCard = AddCard(pad, ref y, cw, 110, surfaceContainerLow, false);
            hCard.Controls.Add(Lbl("Optimization", new Font("Segoe UI", 24, FontStyle.Bold), onSurface, new Rectangle(24, 18, cw - 48, 45)));
            hCard.Controls.Add(Lbl("Registry health and stability tracking.", new Font("Segoe UI", 9.5f), onSurfaceVariant, new Rectangle(24, 68, cw - 48, 32)));

            // Repair button
            PillButton fixBtn = Btn("\uE81E  REPAIR & OPTIMIZE", new Font("Segoe UI Bold", 11.5f), Color.White, primary, true, primaryContainer, pad, y, cw, 54);
            fixBtn.Click += (s, e) => RunFixRegistry();
            mainContent.Controls.Add(fixBtn);
            y = fixBtn.Bottom + 14;

            // Server check
            PillButton srvBtn = Btn("\uE753  Check Server Connection", new Font("Segoe UI Bold", 9.5f), primary, surfaceContainerHighest, false, Color.White, pad, y, cw, 44);
            srvBtn.Click += (s, e) => { try { Dns.GetHostEntry("gandisetiawan28.github.io"); MessageBox.Show("Koneksi berhasil!"); } catch { MessageBox.Show("Gagal terhubung."); } };
            mainContent.Controls.Add(srvBtn);
            y = srvBtn.Bottom + 24;

            // Status cards
            string[] items   = { "Manifest File", "Word Registry", "Server" };
            string[] subs    = { "SYSTEM INTEGRITY", "LINGUISTIC DATA", "CLOUD SYNC" };
            string[] icns    = { "\uE8A5", "\uE71D", "\uE753" };
            string[] badges  = { "Installed", "Connected", "Stable" };

            foreach (var i2 in new int[]{0,1,2})
            {
                RoundedCard sc = new RoundedCard { Location = new Point(pad, y), Size = new Size(cw, 76), BackColor = surfaceContainerLowest, Radius = 18, ShowShadow = true };
                Label ico2 = new Label { Text = icns[i2], Font = new Font("Segoe MDL2 Assets", 16), ForeColor = successGreen, BackColor = successGreenBg, Location = new Point(16, 18), Size = new Size(40, 40), TextAlign = ContentAlignment.MiddleCenter };
                Label ttl = new Label { Text = items[i2], Font = new Font("Segoe UI Bold", 10f), ForeColor = onSurface, BackColor = Color.Transparent, Location = new Point(68, 16), Size = new Size(200, 20) };
                Label sub2 = new Label { Text = subs[i2], Font = new Font("Segoe UI", 8f, FontStyle.Bold), ForeColor = onSurfaceVariant, BackColor = Color.Transparent, Location = new Point(68, 38), Size = new Size(200, 18) };
                Label bdg = new Label { Text = "\uE73E " + badges[i2], Font = new Font("Segoe UI Bold", 8f), ForeColor = successGreen, BackColor = successGreenBg, Location = new Point(cw - 102, 24), Size = new Size(88, 26), TextAlign = ContentAlignment.MiddleCenter };
                sc.Controls.AddRange(new Control[] { ico2, ttl, sub2, bdg });
                mainContent.Controls.Add(sc);
                y = sc.Bottom + 12;
            }

            AddSpacer(ref y);
        }
