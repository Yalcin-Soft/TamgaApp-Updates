using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace TamgaApp
{
    public enum BildirimTuru { Bilgi, Basari, Uyari, Hata }

    /// <summary>
    /// Ekranın sağ altında beliren, işi bölmeyen (MessageBox gibi tıklama beklemeyen) modern bildirim.
    /// Birden fazla bildirim üst üste dizilir; tıklanınca (varsa) eylem çalışır ve kapanır.
    /// </summary>
    public class Bildirim : Form
    {
        private static readonly List<Bildirim> acikBildirimler = new List<Bildirim>();

        private readonly string baslik, mesaj;
        private readonly BildirimTuru tur;
        private readonly Action eylem;
        private readonly Timer zamanlayici = new Timer { Interval = 15 };
        private int kalanMs;
        private bool kapaniyor;

        public static void Goster(string baslik, string mesaj, BildirimTuru tur = BildirimTuru.Bilgi, int sureMs = 4500, Action tiklaninca = null)
        {
            try
            {
                var b = new Bildirim(baslik, mesaj, tur, sureMs, tiklaninca);
                b.Konumlandir();
                b.Show();
            }
            catch { }
        }

        private Bildirim(string baslik, string mesaj, BildirimTuru tur, int sureMs, Action eylem)
        {
            this.baslik = baslik; this.mesaj = mesaj; this.tur = tur; this.eylem = eylem; kalanMs = sureMs;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            BackColor = Tema.Kart;
            Size = new Size(380, 88);
            Opacity = 0;
            Cursor = eylem != null ? Cursors.Hand : Cursors.Default;
            DoubleBuffered = true;
            Tema.HaricTut(this);
            Region = Region.FromHrgn(YuvarlakBolge(0, 0, Width + 1, Height + 1, 18, 18));

            zamanlayici.Tick += (s, e) =>
            {
                if (kapaniyor)
                {
                    Opacity -= 0.08;
                    if (Opacity <= 0.02) { zamanlayici.Stop(); Close(); }
                    return;
                }
                if (Opacity < 1) Opacity = Math.Min(1, Opacity + 0.1);
                kalanMs -= zamanlayici.Interval;
                if (kalanMs <= 0 && !RectangleToScreen(ClientRectangle).Contains(Cursor.Position)) kapaniyor = true;
            };
            Click += (s, e) => { kapaniyor = true; eylem?.Invoke(); };
            FormClosed += (s, e) => { acikBildirimler.Remove(this); TumunuDiz(); zamanlayici.Dispose(); };
            Shown += (s, e) => zamanlayici.Start();
        }

        [System.Runtime.InteropServices.DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr YuvarlakBolge(int l, int t, int r, int b, int w, int h);

        protected override bool ShowWithoutActivation { get { return true; } }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= 0x08000000 | 0x80; // WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW
                cp.ClassStyle |= 0x20000;        // CS_DROPSHADOW
                return cp;
            }
        }

        private void Konumlandir()
        {
            acikBildirimler.Add(this);
            TumunuDiz();
        }

        private static void TumunuDiz()
        {
            Rectangle alan = Screen.PrimaryScreen.WorkingArea;
            Form ana = Application.OpenForms.Cast<Form>().FirstOrDefault(f => f is MainForm || f is LoginForm);
            if (ana != null) alan = Screen.FromControl(ana).WorkingArea;
            int y = alan.Bottom - 16;
            foreach (var b in acikBildirimler.ToList())
            {
                y -= b.Height;
                b.Location = new Point(alan.Right - b.Width - 16, y);
                y -= 10;
            }
        }

        private Color TurRengi()
        {
            switch (tur)
            {
                case BildirimTuru.Basari: return Tema.Basari;
                case BildirimTuru.Uyari: return Tema.Uyari;
                case BildirimTuru.Hata: return Tema.Tehlike;
                default: return Tema.Vurgu;
            }
        }

        private string TurIkonu()
        {
            switch (tur)
            {
                case BildirimTuru.Basari: return "";
                case BildirimTuru.Uyari: return "";
                case BildirimTuru.Hata: return "";
                default: return "";
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Tema.Kart);
            Color renk = TurRengi();
            using (var p = new Pen(Tema.Cizgi)) g.DrawRectangle(p, 0, 0, Width - 1, Height - 1);
            using (var b = new SolidBrush(renk)) g.FillRectangle(b, 0, 0, 5, Height);
            var ikon = new Rectangle(20, (Height - 36) / 2, 36, 36);
            using (var b = new SolidBrush(Tema.Karistir(renk, Color.White, 0.85f))) g.FillEllipse(b, ikon);
            TextRenderer.DrawText(g, TurIkonu(), Tema.Ikon(13f), ikon, renk, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            TextRenderer.DrawText(g, baslik, Tema.Yazi(10.5f, true), new Rectangle(68, 16, Width - 84, 22), Tema.Metin, TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            TextRenderer.DrawText(g, mesaj, Tema.Yazi(9.5f), new Rectangle(68, 40, Width - 84, Height - 46), Tema.SolukMetin, TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }
    }
}
