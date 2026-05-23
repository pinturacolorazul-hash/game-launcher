using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using System.Text;
using System.Web.Script.Serialization;
using System.Runtime.InteropServices;

namespace GameLauncher
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    internal static class NativeMethods
    {
        [DllImport("dwmapi.dll")]
        internal static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
        internal const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        [DllImport("user32.dll")]
        internal static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")]
        internal static extern bool ReleaseCapture();
        internal const int WM_NCHITTEST = 0x84;
        internal const int WM_NCLBUTTONDOWN = 0xA1;
        internal const int HTCLIENT = 1;
        internal const int HTCAPTION = 2;
        internal const int HTLEFT = 10;
        internal const int HTRIGHT = 11;
        internal const int HTTOP = 12;
        internal const int HTTOPLEFT = 13;
        internal const int HTTOPRIGHT = 14;
        internal const int HTBOTTOM = 15;
        internal const int HTBOTTOMLEFT = 16;
        internal const int HTBOTTOMRIGHT = 17;
    }

    public class GameConfig
    {
        public string nombre { get; set; }
        public string portada { get; set; }
        public List<LaunchOption> opciones { get; set; }
    }

    public class LaunchOption
    {
        public string etiqueta { get; set; }
        public string ruta { get; set; }
        public string argumentos { get; set; }
        public string directorio { get; set; }
    }

    public class MainForm : Form
    {
        private List<GameConfig> games;
        private string configPath;
        private string coversDir;
        private ContextMenuStrip contextMenu;
        private GameConfig selectedGame;
        private Panel scrollPanel;
        private int titleBarHeight = 34;
        private int resizeBorder = 5;

        private enum ViewMode { GridXL, GridL, GridM, GridS, GridXS, List }
        private ViewMode currentView = ViewMode.GridL;
        private Button btnGridXL, btnGridL, btnGridM, btnGridS, btnGridXS, btnList;
        private int cardWidth = 240;
        private int cardHeight = 230;
        private int cardMargin = 5;
        private Button closeBtn, maxBtn, minBtn;

        public MainForm()
        {
            configPath = Path.Combine(Application.StartupPath, "juegos.json");
            coversDir = Path.Combine(Application.StartupPath, "covers");
            if (!Directory.Exists(coversDir))
                Directory.CreateDirectory(coversDir);

            this.Text = "Game Launcher";
            this.Size = new Size(1100, 720);
            this.MinimumSize = new Size(450, 300);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(24, 24, 24);
            this.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
            this.DoubleBuffered = true;
            this.FormBorderStyle = FormBorderStyle.None;

            this.WindowState = FormWindowState.Maximized;
            LoadGames();
            InitializeUI();
        }

        private void LoadGames()
        {
            if (File.Exists(configPath))
            {
                try
                {
                    var json = File.ReadAllText(configPath, Encoding.UTF8);
                    var serializer = new JavaScriptSerializer();
                    games = serializer.Deserialize<List<GameConfig>>(json);
                }
                catch { games = new List<GameConfig>(); }
            }
            else
            {
                games = new List<GameConfig>();
                var serializer = new JavaScriptSerializer();
                File.WriteAllText(configPath, serializer.Serialize(games), Encoding.UTF8);
            }
        }

        private void InitializeUI()
        {
            Panel titleBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = titleBarHeight,
                BackColor = Color.FromArgb(18, 18, 18)
            };
            titleBar.MouseDown += (s, e) => { if (e.Clicks == 1) DoDragMove(); };
            titleBar.MouseDoubleClick += (s, e) => ToggleMaximize();

            Label title = new Label
            {
                Text = "Game Launcher",
                ForeColor = Color.FromArgb(150, 150, 150),
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                Location = new Point(10, 0),
                Size = new Size(120, titleBarHeight),
                TextAlign = ContentAlignment.MiddleLeft
            };
            title.MouseDown += (s, e) => DoDragMove();
            titleBar.Controls.Add(title);

            btnGridXL = CreateViewButton("\u25c7", 140, 0, ViewMode.GridXL);
            btnGridL = CreateViewButton("\u25a6", 166, 0, ViewMode.GridL);
            btnGridM = CreateViewButton("\u25a3", 192, 0, ViewMode.GridM);
            btnGridS = CreateViewButton("\u25a2", 218, 0, ViewMode.GridS);
            btnGridXS = CreateViewButton("\u25a1", 244, 0, ViewMode.GridXS);
            btnList = CreateViewButton("\u2630", 270, 0, ViewMode.List);

            titleBar.Controls.Add(btnGridXL);
            titleBar.Controls.Add(btnGridL);
            titleBar.Controls.Add(btnGridM);
            titleBar.Controls.Add(btnGridS);
            titleBar.Controls.Add(btnGridXS);
            titleBar.Controls.Add(btnList);

            Button addTitleBtn = new Button
            {
                Text = "+",
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                BackColor = Color.FromArgb(18, 18, 18),
                ForeColor = Color.FromArgb(170, 170, 170),
                Font = new Font("Segoe UI", 16F, FontStyle.Regular),
                Location = new Point(308, 0),
                Size = new Size(28, titleBarHeight),
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter
            };
            addTitleBtn.Click += (s, e) => ShowAddDialog();
            titleBar.Controls.Add(addTitleBtn);

            int cx = this.ClientSize.Width;

            minBtn = CreateWinButton("\u2014", cx - 105, titleBarHeight, true);
            minBtn.Click += (s, e) => this.WindowState = FormWindowState.Minimized;
            titleBar.Controls.Add(minBtn);

            maxBtn = CreateWinButton("\u25a1", cx - 72, titleBarHeight, true);
            maxBtn.Click += (s, e) => ToggleMaximize();
            titleBar.Controls.Add(maxBtn);

            closeBtn = CreateWinButton("\u2715", cx - 39, titleBarHeight, false);
            closeBtn.Click += (s, e) => this.Close();
            titleBar.Controls.Add(closeBtn);

            this.Controls.Add(titleBar);

            this.Resize += (s, e) =>
            {
                cx = this.ClientSize.Width;
                minBtn.Location = new Point(cx - 105, 0);
                maxBtn.Location = new Point(cx - 72, 0);
                closeBtn.Location = new Point(cx - 39, 0);
            };

            scrollPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.FromArgb(24, 24, 24)
            };
            scrollPanel.MouseWheel += (s, e) =>
            {
                if (Control.ModifierKeys == Keys.Control)
                {
                    ((HandledMouseEventArgs)e).Handled = true;
                    if (e.Delta > 0) ZoomIn();
                    else ZoomOut();
                }
            };

            this.Controls.Add(scrollPanel);

            contextMenu = new ContextMenuStrip();
            contextMenu.BackColor = Color.FromArgb(40, 40, 40);
            contextMenu.ForeColor = Color.FromArgb(200, 200, 200);
            contextMenu.Font = new Font("Segoe UI", 9F);
            contextMenu.Renderer = new CustomRenderer();

            UpdateViewButtons();
            RenderView();
        }

        private Button CreateViewButton(string text, int x, int h, ViewMode mode)
        {
            Button btn = new Button
            {
                Text = text,
                Location = new Point(x, 0),
                Size = new Size(26, h),
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 12F),
                TextAlign = ContentAlignment.MiddleCenter
            };
            btn.Tag = mode;
            btn.Click += (s, e) =>
            {
                currentView = mode;
                UpdateViewButtons();
                RenderView();
            };
            return btn;
        }

        private Button CreateWinButton(string text, int x, int h, bool normal)
        {
            return new Button
            {
                Text = text,
                Location = new Point(x, 0),
                Size = new Size(33, h),
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(160, 160, 160),
                BackColor = Color.FromArgb(18, 18, 18),
                TextAlign = ContentAlignment.MiddleCenter
            };
        }

        private void UpdateViewButtons()
        {
            Color active = Color.FromArgb(60, 60, 80);
            Color inactive = Color.FromArgb(18, 18, 18);
            Color activeFg = Color.FromArgb(220, 220, 220);
            Color inactiveFg = Color.FromArgb(130, 130, 130);

            btnGridXL.BackColor = currentView == ViewMode.GridXL ? active : inactive;
            btnGridL.BackColor = currentView == ViewMode.GridL ? active : inactive;
            btnGridM.BackColor = currentView == ViewMode.GridM ? active : inactive;
            btnGridS.BackColor = currentView == ViewMode.GridS ? active : inactive;
            btnGridXS.BackColor = currentView == ViewMode.GridXS ? active : inactive;
            btnList.BackColor = currentView == ViewMode.List ? active : inactive;
            btnGridXL.ForeColor = currentView == ViewMode.GridXL ? activeFg : inactiveFg;
            btnGridL.ForeColor = currentView == ViewMode.GridL ? activeFg : inactiveFg;
            btnGridM.ForeColor = currentView == ViewMode.GridM ? activeFg : inactiveFg;
            btnGridS.ForeColor = currentView == ViewMode.GridS ? activeFg : inactiveFg;
            btnGridXS.ForeColor = currentView == ViewMode.GridXS ? activeFg : inactiveFg;
            btnList.ForeColor = currentView == ViewMode.List ? activeFg : inactiveFg;
        }

        private void ToggleMaximize()
        {
            if (this.WindowState == FormWindowState.Maximized)
                this.WindowState = FormWindowState.Normal;
            else
                this.WindowState = FormWindowState.Maximized;
        }

        private void DoDragMove()
        {
            NativeMethods.ReleaseCapture();
            NativeMethods.SendMessage(this.Handle, NativeMethods.WM_NCLBUTTONDOWN, (IntPtr)NativeMethods.HTCAPTION, IntPtr.Zero);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            if (Control.ModifierKeys == Keys.Control)
            {
                if (e.Delta > 0) ZoomIn();
                else ZoomOut();
            }
            else
            {
                base.OnMouseWheel(e);
            }
        }

        private void ZoomIn()
        {
            if (currentView == ViewMode.List) currentView = ViewMode.GridXS;
            else if (currentView == ViewMode.GridXS) currentView = ViewMode.GridS;
            else if (currentView == ViewMode.GridS) currentView = ViewMode.GridM;
            else if (currentView == ViewMode.GridM) currentView = ViewMode.GridL;
            else if (currentView == ViewMode.GridL) currentView = ViewMode.GridXL;
            UpdateViewButtons();
            RenderView();
        }

        private void ZoomOut()
        {
            if (currentView == ViewMode.GridXL) currentView = ViewMode.GridL;
            else if (currentView == ViewMode.GridL) currentView = ViewMode.GridM;
            else if (currentView == ViewMode.GridM) currentView = ViewMode.GridS;
            else if (currentView == ViewMode.GridS) currentView = ViewMode.GridXS;
            else if (currentView == ViewMode.GridXS) currentView = ViewMode.List;
            UpdateViewButtons();
            RenderView();
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == NativeMethods.WM_NCHITTEST && this.WindowState != FormWindowState.Maximized)
            {
                int x = m.LParam.ToInt32() & 0xFFFF;
                int y = (m.LParam.ToInt32() >> 16) & 0xFFFF;
                Point pt = this.PointToClient(new Point(x, y));
                int b = resizeBorder;
                int w = this.ClientSize.Width;
                int h = this.ClientSize.Height;
                IntPtr ht = IntPtr.Zero;
                if (pt.X < b && pt.Y < b) ht = (IntPtr)NativeMethods.HTTOPLEFT;
                else if (pt.X >= w - b && pt.Y < b) ht = (IntPtr)NativeMethods.HTTOPRIGHT;
                else if (pt.X < b && pt.Y >= h - b) ht = (IntPtr)NativeMethods.HTBOTTOMLEFT;
                else if (pt.X >= w - b && pt.Y >= h - b) ht = (IntPtr)NativeMethods.HTBOTTOMRIGHT;
                else if (pt.X < b) ht = (IntPtr)NativeMethods.HTLEFT;
                else if (pt.X >= w - b) ht = (IntPtr)NativeMethods.HTRIGHT;
                else if (pt.Y < b) ht = (IntPtr)NativeMethods.HTTOP;
                else if (pt.Y >= h - b) ht = (IntPtr)NativeMethods.HTBOTTOM;
                else if (pt.Y < titleBarHeight && pt.X < w - 105 && (pt.X < 140 || pt.X > 308))
                    ht = (IntPtr)NativeMethods.HTCAPTION;
                if (ht != IntPtr.Zero)
                {
                    m.Result = ht;
                    return;
                }
            }
            base.WndProc(ref m);
        }

        private void RenderView()
        {
            scrollPanel.Controls.Clear();
            if (games == null || games.Count == 0)
            {
                ShowEmptyState();
                return;
            }
            if (currentView == ViewMode.List)
                RenderListView();
            else
                RenderGridView();
        }

        private void ShowEmptyState()
        {
            Panel ep = new Panel
            {
                Size = new Size(260, 120),
                Location = new Point(
                    Math.Max(0, (scrollPanel.ClientSize.Width - 260) / 2),
                    Math.Max(0, (scrollPanel.ClientSize.Height - 160) / 2)),
                BackColor = Color.FromArgb(24, 24, 24)
            };
            Label msg = new Label
            {
                Text = "No hay juegos a\xfan",
                ForeColor = Color.FromArgb(100, 100, 100),
                Font = new Font("Segoe UI", 14F),
                Location = new Point(0, 0),
                Width = 260,
                Height = 30,
                TextAlign = ContentAlignment.MiddleCenter
            };
            Button addBtn = new Button
            {
                Text = "+  A\xf1adir juego",
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(45, 45, 50),
                ForeColor = Color.FromArgb(180, 180, 180),
                FlatAppearance = { BorderSize = 0 },
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 14F),
                Size = new Size(200, 50),
                Location = new Point(30, 40),
                TextAlign = ContentAlignment.MiddleCenter
            };
            addBtn.Click += (s, e) => ShowAddDialog();
            ep.Controls.Add(msg);
            ep.Controls.Add(addBtn);
            this.Resize += (s2, e2) =>
            {
                ep.Location = new Point(
                    Math.Max(0, (scrollPanel.ClientSize.Width - 260) / 2),
                    Math.Max(0, (scrollPanel.ClientSize.Height - 160) / 2 + 24));
            };
            scrollPanel.Controls.Add(ep);
        }

        private void RenderGridView()
        {
            switch (currentView)
            {
                case ViewMode.GridXL: cardWidth = 300; cardHeight = 420; cardMargin = 10; break;
                case ViewMode.GridL: cardWidth = 240; cardHeight = 330; cardMargin = 8; break;
                case ViewMode.GridM: cardWidth = 190; cardHeight = 260; cardMargin = 7; break;
                case ViewMode.GridS: cardWidth = 150; cardHeight = 200; cardMargin = 6; break;
                case ViewMode.GridXS: cardWidth = 120; cardHeight = 160; cardMargin = 5; break;
            }

            int pad = 12;
            int gapX = cardMargin * 2;
            int gapY = 8;
            int totalW = cardWidth + gapX;
            int availW = scrollPanel.ClientSize.Width - pad * 2;
            int cols = Math.Max(1, (availW + gapX) / (totalW));
            int rowW = cols * totalW - gapX;
            int startX = pad + (availW - rowW) / 2;

            Panel gridPanel = new Panel
            {
                Location = new Point(0, 24),
                Width = scrollPanel.ClientSize.Width,
                BackColor = Color.FromArgb(24, 24, 24)
            };

            int x = startX, y = 4;
            int count = 0;

            foreach (var game in games)
            {
                Panel card = CreateGridCard(game);
                card.Location = new Point(x, y);
                gridPanel.Controls.Add(card);
                count++;
                if (count % cols == 0)
                {
                    x = startX;
                    y += cardHeight + gapY;
                }
                else
                {
                    x += totalW;
                }
            }

            Button addCard = CreateAddButton(cardWidth);
            addCard.Location = new Point(x, y);
            gridPanel.Controls.Add(addCard);
            gridPanel.Height = y + cardHeight + gapY + pad;

            scrollPanel.Controls.Add(gridPanel);
            scrollPanel.Resize += (s2, e2) =>
            {
                int nw = scrollPanel.ClientSize.Width - pad * 2;
                int nc = Math.Max(1, (nw + gapX) / (totalW));
                int rw = nc * totalW - gapX;
                int sx = pad + (nw - rw) / 2;
                int cx = sx, cy = 4;
                int idx = 0;
                foreach (Control c in gridPanel.Controls)
                {
                    c.Location = new Point(cx, cy);
                    idx++;
                    if (idx % nc == 0) { cx = sx; cy += cardHeight + gapY; }
                    else cx += totalW;
                }
                gridPanel.Height = cy + cardHeight + gapY + pad;
            };
        }

        private void RenderListView()
        {
            Panel list = new Panel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(6, 0, 6, 4),
                Location = new Point(0, 24),
                BackColor = Color.FromArgb(24, 24, 24)
            };

            foreach (var game in games)
                list.Controls.Add(CreateListItem(game));

            Button addBtn = new Button
            {
                Text = "+ A\xf1adir juego",
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.FromArgb(150, 150, 150),
                FlatAppearance = { BorderSize = 0 },
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 10F),
                Size = new Size(160, 36),
                Location = new Point(8, 4 + games.Count * 58),
                TextAlign = ContentAlignment.MiddleCenter
            };
            addBtn.Click += (s, e) => ShowAddDialog();
            list.Controls.Add(addBtn);

            scrollPanel.Controls.Add(list);
        }

        private Panel CreateGridCard(GameConfig game)
        {
            int cw = cardWidth;
            int ch = cardHeight;
            int coverH = ch - 30;

            Panel card = new Panel
            {
                Width = cw,
                Height = ch,
                Margin = new Padding(cardMargin, 4, cardMargin, 4),
                BackColor = Color.FromArgb(30, 30, 30),
                Cursor = Cursors.Hand
            };

            card.Paint += (sender, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var path = GetRoundedRect(new Rectangle(0, 0, cw - 1, ch - 1), 6))
                using (var brush = new SolidBrush(Color.FromArgb(30, 30, 30)))
                using (var pen = new Pen(Color.FromArgb(45, 45, 45), 1))
                {
                    e.Graphics.FillPath(brush, path);
                    e.Graphics.DrawPath(pen, path);
                }
            };

            PictureBox cover = new PictureBox
            {
                Width = cw,
                Height = coverH,
                Location = new Point(0, 0),
                SizeMode = PictureBoxSizeMode.Normal,
                BackColor = Color.FromArgb(30, 30, 30),
                Cursor = Cursors.Hand
            };

            Image coverImg = null;
            string coverPath = "";
            if (!string.IsNullOrEmpty(game.portada))
            {
                coverPath = Path.Combine(Application.StartupPath, game.portada);
                if (!File.Exists(coverPath))
                    coverPath = Path.Combine(coversDir, game.portada);
            }
            if (!string.IsNullOrEmpty(coverPath) && File.Exists(coverPath))
            {
                try { coverImg = Image.FromFile(coverPath); }
                catch { }
            }

            cover.Paint += (sender, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                var rect = new Rectangle(0, 0, cw, coverH);
                if (coverImg != null)
                {
                    float imgR = (float)coverImg.Width / coverImg.Height;
                    float boxR = (float)cw / coverH;
                    int dx, dy, dw, dh;
                    if (imgR > boxR)
                    {
                        dh = coverH; dw = (int)(coverH * imgR);
                        dx = (cw - dw) / 2; dy = 0;
                    }
                    else
                    {
                        dw = cw; dh = (int)(cw / imgR);
                        dx = 0; dy = (coverH - dh) / 2;
                    }
                    e.Graphics.DrawImage(coverImg, new Rectangle(dx, dy, dw, dh));
                }
                else
                {
                    using (var brush = new LinearGradientBrush(rect,
                        Color.FromArgb(55, 55, 70), Color.FromArgb(35, 35, 50), 45f))
                    {
                        e.Graphics.FillRectangle(brush, rect);
                    }
                    if (!string.IsNullOrEmpty(game.nombre))
                    {
                        string initials = GetInitials(game.nombre);
                        float fontSize = Math.Min(cw, coverH) * 0.18f;
                        using (var font = new Font("Segoe UI", fontSize, FontStyle.Bold))
                        using (var brush = new SolidBrush(Color.FromArgb(80, 80, 105)))
                        {
                            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                            e.Graphics.DrawString(initials, font, brush, rect, sf);
                        }
                    }
                }
            };

            cover.Click += (s, e) => LaunchDefault(game);

            Panel overlay = new Panel
            {
                Width = cw,
                Height = 30,
                Location = new Point(0, ch - 30),
                BackColor = Color.FromArgb(180, 0, 0, 0)
            };

            Label nameLabel = new Label
            {
                Text = game.nombre,
                ForeColor = Color.FromArgb(210, 210, 210),
                Font = new Font("Segoe UI", 9F),
                TextAlign = ContentAlignment.MiddleLeft,
                Width = cw - 32,
                Height = 30,
                Location = new Point(6, 0),
                AutoEllipsis = true
            };
            overlay.Controls.Add(nameLabel);

            Button moreBtn = new Button
            {
                Text = "\u2022\u2022\u2022",
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(140, 140, 140),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Size = new Size(24, 20),
                Location = new Point(cw - 26, 5),
                FlatAppearance = { BorderSize = 0 },
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter
            };
            moreBtn.Click += (s, e) => ShowGameMenu(game, moreBtn);
            overlay.Controls.Add(moreBtn);

            card.Controls.Add(cover);
            card.Controls.Add(overlay);
            return card;
        }

        private Panel CreateListItem(GameConfig game)
        {
            int h = 54;
            Panel row = new Panel
            {
                Width = this.ClientSize.Width - 20,
                Height = h,
                Location = new Point(4, 4 + games.IndexOf(game) * 58),
                BackColor = Color.FromArgb(34, 34, 34),
                Cursor = Cursors.Hand
            };

            row.Paint += (sender, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var path = GetRoundedRect(new Rectangle(0, 0, row.Width - 1, h - 1), 4))
                using (var brush = new SolidBrush(Color.FromArgb(34, 34, 34)))
                using (var pen = new Pen(Color.FromArgb(50, 50, 50), 1))
                {
                    e.Graphics.FillPath(brush, path);
                    e.Graphics.DrawPath(pen, path);
                }
            };

            PictureBox thumb = new PictureBox
            {
                Size = new Size(46, 46),
                Location = new Point(6, 4),
                SizeMode = PictureBoxSizeMode.Normal,
                BackColor = Color.FromArgb(34, 34, 34),
                Cursor = Cursors.Hand
            };

            Image thumbImg = null;
            string coverPath = "";
            if (!string.IsNullOrEmpty(game.portada))
            {
                coverPath = Path.Combine(Application.StartupPath, game.portada);
                if (!File.Exists(coverPath))
                    coverPath = Path.Combine(coversDir, game.portada);
            }
            if (!string.IsNullOrEmpty(coverPath) && File.Exists(coverPath))
            {
                try { thumbImg = Image.FromFile(coverPath); }
                catch { }
            }

            thumb.Paint += (sender, e) =>
            {
                int ts = 46;
                var rect = new Rectangle(0, 0, ts, ts);
                if (thumbImg != null)
                {
                    float imgR = (float)thumbImg.Width / thumbImg.Height;
                    int dx, dy, dw, dh;
                    if (imgR > 1f) { dh = ts; dw = (int)(ts * imgR); dx = (ts - dw) / 2; dy = 0; }
                    else { dw = ts; dh = (int)(ts / imgR); dx = 0; dy = (ts - dh) / 2; }
                    using (var path = GetRoundedRect(rect, 4))
                    {
                        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                        e.Graphics.SetClip(path);
                        e.Graphics.DrawImage(thumbImg, new Rectangle(dx, dy, dw, dh));
                    }
                }
                else
                {
                    using (var brush = new LinearGradientBrush(rect,
                        Color.FromArgb(55, 55, 70), Color.FromArgb(35, 35, 50), 45f))
                    using (var path = GetRoundedRect(rect, 4))
                    {
                        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                        e.Graphics.FillPath(brush, path);
                    }
                    if (!string.IsNullOrEmpty(game.nombre))
                    {
                        string initials = GetInitials(game.nombre);
                        if (initials.Length > 2) initials = initials.Substring(0, 2);
                        using (var font = new Font("Segoe UI", 14F, FontStyle.Bold))
                        using (var brush = new SolidBrush(Color.FromArgb(80, 80, 105)))
                        {
                            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                            e.Graphics.DrawString(initials, font, brush, rect, sf);
                        }
                    }
                }
            };

            thumb.Click += (s, e) => LaunchDefault(game);
            row.Click += (s, e) => LaunchDefault(game);

            Label nameLabel = new Label
            {
                Text = game.nombre,
                ForeColor = Color.FromArgb(200, 200, 200),
                Font = new Font("Segoe UI", 11F, FontStyle.Regular),
                Location = new Point(60, 14),
                AutoSize = true,
                Cursor = Cursors.Hand
            };
            nameLabel.Click += (s, e) => LaunchDefault(game);

            Button playBtn = new Button
            {
                Text = "\u25b6",
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(50, 75, 50),
                ForeColor = Color.FromArgb(140, 200, 140),
                Font = new Font("Segoe UI", 10F),
                Size = new Size(28, 28),
                Location = new Point(row.Width - 70, 13),
                FlatAppearance = { BorderSize = 0 },
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter
            };
            playBtn.Click += (s, e) => LaunchDefault(game);

            Button moreBtn = new Button
            {
                Text = "\u2022\u2022\u2022",
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(140, 140, 140),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Size = new Size(28, 28),
                Location = new Point(row.Width - 38, 13),
                FlatAppearance = { BorderSize = 0 },
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter
            };
            moreBtn.Click += (s, e) => ShowGameMenu(game, moreBtn);

            row.Resize += (s, e) =>
            {
                playBtn.Location = new Point(row.Width - 70, 13);
                moreBtn.Location = new Point(row.Width - 38, 13);
            };

            row.Controls.Add(thumb);
            row.Controls.Add(nameLabel);
            row.Controls.Add(playBtn);
            row.Controls.Add(moreBtn);
            return row;
        }

        private Button CreateAddButton(int sz)
        {
            int s = Math.Max(100, sz);
            Button btn = new Button
            {
                Text = "+\nA\xf1adir",
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.FromArgb(100, 100, 100),
                FlatAppearance = { BorderSize = 0 },
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 16F, FontStyle.Regular),
                Size = new Size(s, s + 60),
                Margin = new Padding(cardMargin, 4, cardMargin, 4),
                TextAlign = ContentAlignment.MiddleCenter
            };
            btn.Paint += (sender, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                var rect = new Rectangle(0, 0, s - 1, s + 59);
                using (var path = GetRoundedRect(rect, 6))
                using (var pen = new Pen(Color.FromArgb(60, 60, 60), 2) { DashStyle = DashStyle.Dash })
                {
                    e.Graphics.DrawPath(pen, path);
                }
            };
            btn.Click += (sender, e) => ShowAddDialog();
            return btn;
        }

        private void ShowGameMenu(GameConfig game, Button target)
        {
            selectedGame = game;
            contextMenu.Items.Clear();
            bool hasMulti = game.opciones != null && game.opciones.Count > 1;
            if (hasMulti)
            {
                foreach (var opt in game.opciones)
                {
                    var item = new ToolStripMenuItem(opt.etiqueta);
                    item.ForeColor = Color.FromArgb(200, 200, 200);
                    item.Click += (ev, args) => LaunchGame(game, opt);
                    contextMenu.Items.Add(item);
                }
                contextMenu.Items.Add(new ToolStripSeparator());
            }
            var editItem = new ToolStripMenuItem("Editar");
            editItem.ForeColor = Color.FromArgb(200, 200, 200);
            editItem.Click += (ev, args) => ShowEditDialog(game);
            contextMenu.Items.Add(editItem);
            var delItem = new ToolStripMenuItem("Eliminar");
            delItem.ForeColor = Color.FromArgb(255, 120, 120);
            delItem.Click += (ev, args) => DeleteGame(game);
            contextMenu.Items.Add(delItem);
            Point screenPt = target.PointToScreen(new Point(0, target.Height));
            contextMenu.Show(screenPt);
        }

        private void LaunchDefault(GameConfig game)
        {
            if (game.opciones != null && game.opciones.Count > 0)
                LaunchGame(game, game.opciones[0]);
        }

        private void LaunchGame(GameConfig game, LaunchOption opt)
        {
            if (string.IsNullOrEmpty(opt.ruta))
            {
                MessageBox.Show("No has configurado la ruta para: " + opt.etiqueta,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!File.Exists(opt.ruta))
            {
                MessageBox.Show("No se encuentra:\n" + opt.ruta,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = opt.ruta,
                    Arguments = opt.argumentos ?? "",
                    WorkingDirectory = !string.IsNullOrEmpty(opt.directorio)
                        ? opt.directorio : Path.GetDirectoryName(opt.ruta)
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowAddDialog() { ShowEditDialog(null); }

        private void ShowEditDialog(GameConfig game)
        {
            bool isNew = game == null;
            int formW = 560;
            Form f = new Form
            {
                Text = isNew ? "Agregar juego" : "Editar juego",
                Size = new Size(formW, 500),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false, MinimizeBox = false,
                BackColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.FromArgb(200, 200, 200),
                Font = new Font("Segoe UI", 10F)
            };

            int dark = 1;
            NativeMethods.DwmSetWindowAttribute(f.Handle, NativeMethods.DWMWA_USE_IMMERSIVE_DARK_MODE, ref dark, sizeof(int));

            Label lblName = new Label { Text = "Nombre del juego:", Location = new Point(16, 18), AutoSize = true, ForeColor = Color.FromArgb(200, 200, 200) };
            TextBox txtName = new TextBox
            {
                Location = new Point(140, 15), Width = 380,
                Text = game != null ? game.nombre : "",
                BackColor = Color.FromArgb(60, 60, 60), ForeColor = Color.FromArgb(220, 220, 220),
                BorderStyle = BorderStyle.FixedSingle
            };

            Label lblCover = new Label { Text = "Portada:", Location = new Point(16, 50), AutoSize = true, ForeColor = Color.FromArgb(200, 200, 200) };
            TextBox txtCover = new TextBox
            {
                Location = new Point(140, 47), Width = 320,
                Text = game != null ? game.portada : "",
                BackColor = Color.FromArgb(60, 60, 60), ForeColor = Color.FromArgb(220, 220, 220),
                BorderStyle = BorderStyle.FixedSingle
            };
            Button btnBrowseCover = new Button
            {
                Text = "Examinar", Location = new Point(466, 46), Size = new Size(54, 24),
                FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.FromArgb(200, 200, 200), FlatAppearance = { BorderSize = 0 }, Cursor = Cursors.Hand
            };
            btnBrowseCover.Click += (s, e) =>
            {
                using (var dlg = new OpenFileDialog())
                {
                    dlg.Filter = "Imagenes (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp";
                    dlg.InitialDirectory = coversDir;
                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        string dest = Path.Combine(coversDir, Path.GetFileName(dlg.FileName));
                        if (dlg.FileName != dest) File.Copy(dlg.FileName, dest, true);
                        txtCover.Text = "covers\\" + Path.GetFileName(dlg.FileName);
                    }
                }
            };

            Label lblOptHeader = new Label
            {
                Text = "Formas de abrir el juego:",
                Location = new Point(16, 82), AutoSize = true,
                ForeColor = Color.FromArgb(200, 200, 200), Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };

            Panel optsPanel = new Panel
            {
                Location = new Point(16, 105), Width = formW - 40, Height = 270,
                AutoScroll = true, BackColor = Color.FromArgb(30, 30, 30),
                BorderStyle = BorderStyle.None
            };

            List<Control[]> optionRows = new List<Control[]>();
            Action<string, string, string> AddOptionRow = (optLabel, optPath, optArgs) =>
            {
                int idx = optionRows.Count;
                int y = idx * 80 + 4;

                Panel rowBg = new Panel
                {
                    Location = new Point(4, y),
                    Width = optsPanel.Width - 16,
                    Height = 72,
                    BackColor = Color.FromArgb(38, 38, 42)
                };
                optsPanel.Controls.Add(rowBg);

                Label lblNum = new Label
                {
                    Text = "#" + (idx + 1),
                    Location = new Point(6, 6),
                    AutoSize = true,
                    ForeColor = Color.FromArgb(100, 100, 100),
                    Font = new Font("Segoe UI", 8F)
                };
                rowBg.Controls.Add(lblNum);

                Label lblEtiqueta = new Label
                {
                    Text = "Etiqueta:", Location = new Point(6, 24),
                    AutoSize = true, ForeColor = Color.FromArgb(160, 160, 160),
                    Font = new Font("Segoe UI", 8F)
                };
                rowBg.Controls.Add(lblEtiqueta);
                TextBox txtEtiqueta = new TextBox
                {
                    Text = optLabel, Location = new Point(66, 22), Width = 180,
                    BackColor = Color.FromArgb(55, 55, 55), ForeColor = Color.FromArgb(220, 220, 220),
                    BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 9F)
                };
                rowBg.Controls.Add(txtEtiqueta);

                Label lblRuta = new Label
                {
                    Text = "Ruta:", Location = new Point(6, 48),
                    AutoSize = true, ForeColor = Color.FromArgb(160, 160, 160),
                    Font = new Font("Segoe UI", 8F)
                };
                rowBg.Controls.Add(lblRuta);
                TextBox txtRuta = new TextBox
                {
                    Text = optPath, Location = new Point(66, 46), Width = 300,
                    BackColor = Color.FromArgb(55, 55, 55), ForeColor = Color.FromArgb(220, 220, 220),
                    BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 9F)
                };
                rowBg.Controls.Add(txtRuta);

                Button btnBrowse = new Button
                {
                    Text = "...", Location = new Point(370, 46), Size = new Size(28, 22),
                    FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(55, 55, 55),
                    ForeColor = Color.FromArgb(200, 200, 200), FlatAppearance = { BorderSize = 0 },
                    Cursor = Cursors.Hand, Font = new Font("Segoe UI", 9F, FontStyle.Bold)
                };
                btnBrowse.Click += (s2, e2) =>
                {
                    using (var dlg = new OpenFileDialog())
                    {
                        dlg.Title = "Seleccionar ejecutable";
                        dlg.Filter = "Ejecutables (*.exe;*.bat;*.cmd;*.lnk)|*.exe;*.bat;*.cmd;*.lnk|Todos los archivos (*.*)|*.*";
                        if (dlg.ShowDialog() == DialogResult.OK)
                            txtRuta.Text = dlg.FileName;
                    }
                };
                rowBg.Controls.Add(btnBrowse);

                Button btnRemove = new Button
                {
                    Text = "X", Location = new Point(rowBg.Width - 32, 4), Size = new Size(26, 22),
                    FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(55, 30, 30),
                    ForeColor = Color.FromArgb(200, 100, 100), FlatAppearance = { BorderSize = 0 },
                    Cursor = Cursors.Hand, Font = new Font("Segoe UI", 8F, FontStyle.Bold)
                };
                Panel rowBgCopy = rowBg;
                btnRemove.Click += (s2, e2) =>
                {
                    int found = -1;
                    for (int i = 0; i < optionRows.Count; i++)
                    {
                        if (optionRows[i][0] == rowBgCopy) { found = i; break; }
                    }
                    if (found >= 0)
                    {
                        foreach (var c in optionRows[found])
                            optsPanel.Controls.Remove(c);
                        optionRows.RemoveAt(found);
                    }
                };
                rowBg.Controls.Add(btnRemove);

                optionRows.Add(new Control[] { rowBg, txtEtiqueta, txtRuta });
            };

            if (game != null && game.opciones != null)
            {
                foreach (var o in game.opciones)
                    AddOptionRow(o.etiqueta, o.ruta, o.argumentos ?? "");
            }
            if (optionRows.Count == 0)
                AddOptionRow("Abrir normal", "", "");

            Button btnAddOption = new Button
            {
                Text = "+ Agregar otra forma de abrir",
                Location = new Point(16, 378), Size = new Size(200, 28),
                FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(50, 50, 55),
                ForeColor = Color.FromArgb(170, 170, 170), FlatAppearance = { BorderSize = 0 },
                Cursor = Cursors.Hand, TextAlign = ContentAlignment.MiddleLeft
            };
            btnAddOption.Click += (s, e) => AddOptionRow("", "", "");

            Button btnSave = new Button
            {
                Text = "Guardar", Location = new Point(formW - 190, 422), Size = new Size(80, 30),
                FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(70, 100, 180),
                ForeColor = Color.White, FlatAppearance = { BorderSize = 0 }, Cursor = Cursors.Hand
            };
            Button btnCancel = new Button
            {
                Text = "Cancelar", Location = new Point(formW - 100, 422), Size = new Size(80, 30),
                FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(55, 55, 55),
                ForeColor = Color.FromArgb(180, 180, 180), FlatAppearance = { BorderSize = 0 }, Cursor = Cursors.Hand
            };

            btnSave.Click += (s, e) =>
            {
                var newOptions = new List<LaunchOption>();
                foreach (var r in optionRows)
                {
                    var txtEti = r[1] as TextBox;
                    var txtRut = r[2] as TextBox;
                    if (!string.IsNullOrWhiteSpace(txtRut.Text))
                    {
                        newOptions.Add(new LaunchOption
                        {
                            etiqueta = string.IsNullOrWhiteSpace(txtEti.Text) ? "Opcion" : txtEti.Text,
                            ruta = txtRut.Text,
                            argumentos = ""
                        });
                    }
                }
                if (newOptions.Count == 0)
                {
                    MessageBox.Show("Debes agregar al menos una opcion con ruta valida.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (string.IsNullOrWhiteSpace(txtName.Text))
                {
                    MessageBox.Show("El nombre del juego es obligatorio.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (isNew)
                    games.Add(new GameConfig { nombre = txtName.Text, portada = txtCover.Text, opciones = newOptions });
                else
                {
                    game.nombre = txtName.Text;
                    game.portada = txtCover.Text;
                    game.opciones = newOptions;
                }
                SaveGames();
                RenderView();
                f.Close();
            };
            btnCancel.Click += (s, e) => f.Close();

            f.Controls.Add(lblName);
            f.Controls.Add(txtName);
            f.Controls.Add(lblCover);
            f.Controls.Add(txtCover);
            f.Controls.Add(btnBrowseCover);
            f.Controls.Add(lblOptHeader);
            f.Controls.Add(optsPanel);
            f.Controls.Add(btnAddOption);
            f.Controls.Add(btnSave);
            f.Controls.Add(btnCancel);
            f.ShowDialog(this);
        }

        private void DeleteGame(GameConfig game)
        {
            if (MessageBox.Show("\xbfEliminar \"" + game.nombre + "\" de la lista?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            { games.Remove(game); SaveGames(); RenderView(); }
        }

        private void SaveGames()
        {
            var s = new JavaScriptSerializer();
            File.WriteAllText(configPath, s.Serialize(games), Encoding.UTF8);
        }

        private string GetInitials(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "?";
            var p = name.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (p.Length == 1) return p[0][0].ToString().ToUpper();
            return (p[0][0].ToString() + p[p.Length - 1][0].ToString()).ToUpper();
        }

        private GraphicsPath GetRoundedRect(Rectangle rect, int r)
        {
            var path = new GraphicsPath();
            int d = r * 2;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    public class CustomRenderer : ToolStripProfessionalRenderer
    {
        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            using (var b = new SolidBrush(e.Item.Selected ? Color.FromArgb(60, 60, 80) : Color.FromArgb(40, 40, 40)))
                e.Graphics.FillRectangle(b, new Rectangle(0, 0, e.Item.Width, e.Item.Height));
        }
        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e) { }
    }
}
