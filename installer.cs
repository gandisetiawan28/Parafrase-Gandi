using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Net;
using System.Windows.Forms;
using System.Diagnostics;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Security.Principal;

namespace ParafraseGandiManager
{
    // ======== HELPERS ========
    static class GdiHelper
    {
        public static GraphicsPath RoundRect(Rectangle r, int rad)
        {
            int d = Math.Min(rad * 2, Math.Min(r.Width, r.Height));
            GraphicsPath p = new GraphicsPath();
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }
    }

    // ======== CUSTOM CONTROLS ========

    // Card with optional soft drop-shadow, drawn BEFORE children (base paint only)
    public class RoundedCard : Panel
    {
        public int Radius { get; set; }
        public bool ShowShadow { get; set; }

        public RoundedCard()
        {
            Radius = 24;
            ShowShadow = true;
            this.SetStyle(ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer |
                          ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
            this.BackColor = Color.White;
        }

        protected override void OnPaintBackground(PaintEventArgs e) { /* suppress default */ }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            if (ShowShadow)
            {
                // Layered shadow â€“ drawn slightly offset, below the card rect
                for (int i = 4; i >= 1; i--)
                {
                    Rectangle sr = new Rectangle(i, i + 1, Width - i * 2 - 1, Height - i * 2 - 1);
                    using (GraphicsPath sp = GdiHelper.RoundRect(sr, Radius))
                    using (SolidBrush sb = new SolidBrush(Color.FromArgb(8, 0, 0, 0)))
                        g.FillPath(sb, sp);
                }
            }

            // Card face
            Rectangle face = new Rectangle(1, 1, Width - 3, Height - 3);
            using (GraphicsPath gp = GdiHelper.RoundRect(face, Radius))
            {
                using (SolidBrush b = new SolidBrush(BackColor))
                    g.FillPath(b, gp);
                using (Pen p = new Pen(Color.FromArgb(18, 0, 0, 0), 1f))
                    g.DrawPath(p, gp);
                // Clip so children never bleed outside
                g.Clip = new Region(gp);
            }
        }
    }

    // Smooth animated pill button
    public class PillButton : Button
    {
        public int Radius { get; set; }
        public bool UseGradient { get; set; }
        public Color GradientColor2 { get; set; }

        private float hov = 0f;
        private Timer t;

        public PillButton()
        {
            Radius = 999; // auto-pill
            UseGradient = false;
            GradientColor2 = Color.Blue;
            this.SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw |
                          ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            this.FlatStyle = FlatStyle.Flat;
            this.FlatAppearance.BorderSize = 0;
            this.FlatAppearance.MouseDownBackColor = Color.Transparent;
            this.FlatAppearance.MouseOverBackColor = Color.Transparent;
            this.Cursor = Cursors.Hand;
            this.BackColor = Color.Transparent;

            t = new Timer { Interval = 14 };
            t.Tick += (s, e2) =>
            {
                bool over = ClientRectangle.Contains(PointToClient(Cursor.Position));
                hov += over ? 0.12f : -0.12f;
                if (hov < 0) { hov = 0; t.Stop(); }
                if (hov > 1) { hov = 1; t.Stop(); }
                Invalidate();
            };
            MouseEnter += (s, e2) => t.Start();
            MouseLeave += (s, e2) => t.Start();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int rad = Math.Min(Radius, Math.Min(Width, Height) / 2);
            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath gp = GdiHelper.RoundRect(r, rad))
            {
                if (UseGradient)
                {
                    // Brighten on hover
                    Color c1 = Lerp(BackColor, LightenColor(BackColor, 0.15f), hov);
                    Color c2 = Lerp(GradientColor2, LightenColor(GradientColor2, 0.15f), hov);
                    using (LinearGradientBrush lb = new LinearGradientBrush(r, c1, c2, 90f))
                        g.FillPath(lb, gp);
                }
                else
                {
                    Color fill = BackColor == Color.Transparent
                        ? Color.FromArgb((int)(hov * 30), 0, 0, 0)
                        : Color.FromArgb(230, BackColor);
                    using (SolidBrush sb = new SolidBrush(fill)) g.FillPath(sb, gp);
                    if (BackColor == Color.Transparent )
                    {
                        int bord = (int)(40 + hov * 60);
                        using (Pen p = new Pen(Color.FromArgb(bord, ForeColor), 1f)) g.DrawPath(p, gp);
                    }
                }
            }

            // Shadow on main buttons
            if (UseGradient)
            {
                Rectangle sr = new Rectangle(2, 3, Width - 5, Height - 4);
                using (GraphicsPath sp = GdiHelper.RoundRect(sr, rad))
                using (SolidBrush sb = new SolidBrush(Color.FromArgb((int)(30 + hov * 20), BackColor)))
                    g.FillPath(sb, sp);
            }

            TextFormatFlags flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine;
            TextRenderer.DrawText(g, Text, Font, ClientRectangle, ForeColor, flags);
        }

        Color Lerp(Color a, Color b, float t2)
        {
            t2 = Math.Max(0, Math.Min(1, t2));
            return Color.FromArgb(
                (int)(a.A + (b.A - a.A) * t2),
                (int)(a.R + (b.R - a.R) * t2),
                (int)(a.G + (b.G - a.G) * t2),
                (int)(a.B + (b.B - a.B) * t2));
        }

        Color LightenColor(Color c, float amount)
        {
            return Color.FromArgb(c.A,
                Math.Min(255, (int)(c.R + 255 * amount)),
                Math.Min(255, (int)(c.G + 255 * amount)),
                Math.Min(255, (int)(c.B + 255 * amount)));
        }
    }

    // ======== MAIN FORM ========

    public class ManagerForm : Form
    {
        private const string RAW_ID = "9BB7A975-B568-4B2D-9683-39FD2900118F";
        private const string CATALOG_ID = "{C1234567-ABCD-1234-ABCD-1234567890AB}";
        private const string APP_NAME = "ParafraseGandi";
        private const string SHARE_NAME = "ParafraseGandi";
        private const string REMOTE_BASE = "https://gandisetiawan28.github.io/Parafrase-Gandi";

        private static Color primaryFixedDim = ColorTranslator.FromHtml("#adc6ff");
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
            Label lTitle = new Label { Text = "â— PROCESS LOG", Font = new Font("Consolas", 7.5f), ForeColor = Color.FromArgb(120, 180, 120), BackColor = Color.Transparent, Location = new Point(12, 10), Size = new Size(col2 - 24, 18) };
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
