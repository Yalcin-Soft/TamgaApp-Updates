using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Net;
using System.Windows.Forms;
using AutoUpdaterDotNET;

namespace TamgaApp
{
    /// <summary>
    /// Modern güncelleme merkezi. AutoUpdater.NET'in indirme/kurulum altyapısını kullanır ama kendi
    /// eski tip penceresi yerine TamgaApp tasarımında bir pencere ve sessiz arka plan denetimi sunar.
    /// - Açılışta ve çalışırken her 3 saatte bir sessizce denetler.
    /// - Yeni sürüm varsa üst çubukta "Yeni sürüm" rozeti çıkar; zorunlu sürümde pencere hemen açılır.
    /// - update.xml'de &lt;checksum algorithm="SHA256"&gt; varsa indirilen paket doğrulanır (CI otomatik yazar).
    /// </summary>
    public static class Guncelleme
    {
        public const string BildirimAdresi = "https://raw.githubusercontent.com/Yalcin-Soft/TamgaApp-Updates/main/TamgaApp/update.xml";
        public const string SurumNotlariAdresi = "https://github.com/Yalcin-Soft/TamgaApp-Updates/releases";

        public static UpdateInfoEventArgs SonBilgi { get; private set; }
        public static DateTime? SonDenetim { get; private set; }
        public static string SonHata { get; private set; }
        public static bool Denetleniyor { get; private set; }
        public static bool YeniSurumVar { get { return SonBilgi != null && SonBilgi.IsUpdateAvailable; } }

        /// <summary>Durum her değiştiğinde (denetim başladı/bitti, sürüm bulundu) tetiklenir. UI iş parçacığında çalışır.</summary>
        public static event Action DurumDegisti;

        private static bool hazir;
        private static bool sessiz = true;
        private static bool pencereAcik;

        private static void Hazirla()
        {
            if (hazir) return;
            hazir = true;
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            AutoUpdater.HttpUserAgent = "TamgaApp-Guncelleyici";
            AutoUpdater.RunUpdateAsAdmin = true;   // Program Files'a kurulum için yalnızca kurulum adımı yükseltilir
            AutoUpdater.ReportErrors = false;
            AutoUpdater.Synchronous = false;
            AutoUpdater.AppTitle = "TamgaApp";
            AutoUpdater.CheckForUpdateEvent += DenetimSonucu;
        }

        public static string KuruluSurum
        {
            get { return typeof(Guncelleme).Assembly.GetName().Version.ToString(); }
        }

        /// <summary>Güncelleme denetimi başlatır. sessiz=false ise "güncelsiniz"/hata bildirimleri de gösterilir.</summary>
        public static void Denetle(bool sessizDenetim)
        {
            Hazirla();
            if (Denetleniyor) return;
            Denetleniyor = true;
            sessiz = sessizDenetim;
            SonHata = null;
            DurumDegisti?.Invoke();
            try { AutoUpdater.Start(BildirimAdresi, typeof(Guncelleme).Assembly); }
            catch (Exception ex) { Denetleniyor = false; SonHata = ex.Message; DurumDegisti?.Invoke(); }
        }

        private static void DenetimSonucu(UpdateInfoEventArgs a)
        {
            Denetleniyor = false;
            SonDenetim = DateTime.Now;

            if (a == null || a.Error != null)
            {
                SonHata = a?.Error is WebException ? "Güncelleme sunucusuna ulaşılamadı (internet bağlantısını kontrol edin)." : (a?.Error?.Message ?? "Bilinmeyen hata");
                DurumDegisti?.Invoke();
                if (!sessiz) Bildirim.Goster("Güncelleme denetlenemedi", SonHata, BildirimTuru.Uyari);
                return;
            }

            SonBilgi = a;
            DurumDegisti?.Invoke();

            if (a.IsUpdateAvailable)
            {
                if (!sessiz || Zorunlu(a)) PencereyiGoster(a);
                else Bildirim.Goster("Yeni sürüm hazır: " + a.CurrentVersion, "Güncellemek için tıklayın. Değişiklikler birkaç dakika sürer.", BildirimTuru.Bilgi, 8000, () => PencereyiGoster(a));
            }
            else if (!sessiz)
            {
                Bildirim.Goster("TamgaApp güncel", "En son sürümü kullanıyorsunuz (" + KuruluSurum + ").", BildirimTuru.Basari);
            }
        }

        public static bool Zorunlu(UpdateInfoEventArgs a)
        {
            return a != null && a.Mandatory != null && a.Mandatory.Value;
        }

        public static void PencereyiGoster(UpdateInfoEventArgs a = null)
        {
            a = a ?? SonBilgi;
            if (a == null || !a.IsUpdateAvailable || pencereAcik) return;
            pencereAcik = true;
            try
            {
                using (var f = new GuncellemePenceresi(a)) f.ShowDialog();
            }
            finally { pencereAcik = false; }
        }

        /// <summary>Paketi indirir, doğrular ve kurulumu başlatır. Başarılıysa uygulama kapanır.</summary>
        public static void SimdiKur(UpdateInfoEventArgs a)
        {
            try
            {
                Gunluk.Yaz("Güncelleme", "Güncelleme başlatıldı", KuruluSurum + " → " + a.CurrentVersion);
                if (AutoUpdater.DownloadUpdate(a))
                {
                    Application.Exit();
                    Environment.Exit(0);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Güncelleme indirilemedi veya kurulamadı:\n\n" + ex.Message, "Güncelleme Hatası", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    /// <summary>TamgaApp tasarımında "Yeni sürüm hazır" penceresi.</summary>
    public class GuncellemePenceresi : Form
    {
        private readonly UpdateInfoEventArgs bilgi;
        private bool kuruldu;

        public GuncellemePenceresi(UpdateInfoEventArgs a)
        {
            bilgi = a;
            Text = "TamgaApp Güncelleme";
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(520, 360);
            BackColor = Tema.Kart;
            ShowInTaskbar = true;
            TopMost = true;
            DoubleBuffered = true;
            KeyPreview = true;
            Tema.HaricTut(this);
            Region = Region.FromHrgn(YuvarlakBolge(0, 0, Width + 1, Height + 1, 22, 22));

            bool zorunlu = Guncelleme.Zorunlu(a);

            var btnKur = new Button { Text = "Şimdi Güncelle", BackColor = Color.SeaGreen, Bounds = new Rectangle(32, 288, zorunlu ? 456 : 280, 46) };
            Tema.ButonStil(btnKur); btnKur.Font = Tema.Yazi(11f, true); Tema.HaricTut(btnKur);
            btnKur.Click += (s, e) => { kuruldu = true; btnKur.Enabled = false; btnKur.Text = "İndiriliyor..."; Guncelleme.SimdiKur(bilgi); btnKur.Enabled = true; btnKur.Text = "Şimdi Güncelle"; };
            Controls.Add(btnKur);

            if (!zorunlu)
            {
                var btnSonra = new Button { Text = "Daha Sonra", BackColor = SystemColors.Control, Bounds = new Rectangle(324, 288, 164, 46) };
                Tema.ButonStil(btnSonra); btnSonra.Font = Tema.Yazi(10.5f, true); Tema.HaricTut(btnSonra);
                btnSonra.Click += (s, e) => Close();
                Controls.Add(btnSonra);
            }

            var lnk = new LinkLabel { Text = "Bu sürümde neler var? Sürüm notlarını görüntüle →", AutoSize = true, Location = new Point(32, 248), Font = Tema.Yazi(9.5f), LinkColor = Tema.Vurgu, ActiveLinkColor = Tema.VurguKoyu, BackColor = Tema.Kart, LinkBehavior = LinkBehavior.HoverUnderline };
            Tema.HaricTut(lnk);
            lnk.LinkClicked += (s, e) =>
            {
                string adres = string.IsNullOrEmpty(bilgi.ChangelogURL) ? Guncelleme.SurumNotlariAdresi : bilgi.ChangelogURL;
                try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(adres) { UseShellExecute = true }); } catch { }
            };
            Controls.Add(lnk);

            KeyDown += (s, e) => { if (e.KeyCode == Keys.Escape && !zorunlu) Close(); };
            FormClosing += (s, e) =>
            {
                // Zorunlu güncellemede güncellemeden devam edilemez (eski sürüm sunucu/veri uyumsuzluğu yaratabilir).
                if (zorunlu && !kuruldu && e.CloseReason == CloseReason.UserClosing)
                {
                    var c = MessageBox.Show("Bu güncelleme zorunludur. Güncellemeden devam edilemez.\n\nProgram kapatılsın mı?", "Zorunlu Güncelleme", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (c == DialogResult.Yes) Environment.Exit(0);
                    e.Cancel = true;
                }
            };
        }

        [System.Runtime.InteropServices.DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr YuvarlakBolge(int l, int t, int r, int b, int w, int h);

        protected override CreateParams CreateParams
        {
            get { var cp = base.CreateParams; cp.ClassStyle |= 0x20000; return cp; }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left) { YakalamayiBirak(); SendMessage(Handle, 0xA1, 0x2, 0); }
        }

        [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "ReleaseCapture")] private static extern bool YakalamayiBirak();
        [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr h, int m, int w, int l);

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Tema.Kart);

            // Üst bant
            var bant = new Rectangle(0, 0, Width, 132);
            using (var b = new LinearGradientBrush(bant, Tema.Grafit, Color.FromArgb(15, 94, 84), 20f)) g.FillRectangle(b, bant);
            var ikon = new Rectangle(32, 34, 64, 64);
            using (var b = new SolidBrush(Color.FromArgb(40, 255, 255, 255))) g.FillEllipse(b, ikon);
            TextRenderer.DrawText(g, "", Tema.Ikon(24f), ikon, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            TextRenderer.DrawText(g, "Yeni sürüm hazır", Tema.Yazi(18f, true), new Point(112, 36), Color.White);
            TextRenderer.DrawText(g, "TamgaApp'in daha yeni bir sürümü yayınlandı.", Tema.Yazi(10f), new Point(114, 72), Color.FromArgb(203, 213, 225));

            // Sürüm karşılaştırması
            KutuCiz(g, new Rectangle(32, 156, 210, 72), "Kurulu sürüm", Guncelleme.KuruluSurum, Tema.SolukMetin);
            TextRenderer.DrawText(g, "", Tema.Ikon(14f), new Rectangle(242, 156, 36, 72), Tema.SolukMetin, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            KutuCiz(g, new Rectangle(278, 156, 210, 72), "Yeni sürüm", bilgi.CurrentVersion, Tema.Vurgu);

            if (Guncelleme.Zorunlu(bilgi))
            {
                var rozet = new Rectangle(Width - 120, 18, 100, 26);
                using (var yol = Tema.YuvarlakDikdortgen(rozet, 13)) using (var b = new SolidBrush(Tema.Uyari)) g.FillPath(b, yol);
                TextRenderer.DrawText(g, "ZORUNLU", Tema.Yazi(8.5f, true), rozet, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }

        private static void KutuCiz(Graphics g, Rectangle r, string etiket, string deger, Color renk)
        {
            using (var yol = Tema.YuvarlakDikdortgen(r, 12))
            {
                using (var b = new SolidBrush(Color.FromArgb(248, 250, 252))) g.FillPath(b, yol);
                using (var p = new Pen(Tema.Cizgi)) g.DrawPath(p, yol);
            }
            TextRenderer.DrawText(g, etiket, Tema.Yazi(9f), new Point(r.X + 16, r.Y + 12), Tema.SolukMetin);
            TextRenderer.DrawText(g, deger ?? "-", Tema.Yazi(15f, true), new Point(r.X + 14, r.Y + 32), renk);
        }
    }
}
