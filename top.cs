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
                // Layered shadow – drawn slightly offset, below the card rect
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
