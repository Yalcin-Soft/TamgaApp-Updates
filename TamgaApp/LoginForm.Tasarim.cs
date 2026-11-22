using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace TamgaApp
{
    /// <summary>
    /// Giriş ekranının görsel tasarımı: animasyonlu marka paneli (sol), modern giriş kartı (sağ),
    /// ikonlu giriş alanları, şifre göster/gizle, Caps Lock uyarısı, satır içi hata kutusu ve
    /// hatalı girişte "sallanma" animasyonu.
    /// </summary>
    public partial class LoginForm
    {
        private const int MarkaGenisligi = 480;
        private static readonly Size FormBoyutu = new Size(1040, 620);

        private Panel pnlSag;
        private GirisAlani alanKullanici, alanSifre;
        private Label lblCaps, lblGuncelleme, lblBaslik, lblAltBaslik;
        private HataKutusu kutuHata;
        private readonly Timer animasyon = new Timer { Interval = 33 };
        private readonly Timer kilitSayaci = new Timer { Interval = 1000 };
        private float animasyonZamani;
        private Bitmap arkaPlanOnbellek, onPlanOnbellek;

        private void ModernTasarimiKur()
        {
            Tema.HaricTut(this); // Bu form tamamen elle tasarlanır; otomatik tema dokunmaz
            this.SuspendLayout();
            this.ClientSize = FormBoyutu;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.White;
            this.Text = "TamgaApp — Giriş";
            this.Opacity = 0;
            this.KeyPreview = true;

            // Görselleri biz çizeceğiz (animasyonlu zemin üzerinde saydamlık sorunsuz olsun diye)
            pictureBox1.Visible = false;
            pictureBox2.Visible = false;

            pnlSag = new Panel { Bounds = new Rectangle(MarkaGenisligi, 0, FormBoyutu.Width - MarkaGenisligi, FormBoyutu.Height), BackColor = Color.White };
            Tema.HaricTut(pnlSag);
            pnlSag.MouseDown += LoginForm_MouseDown;
            this.Controls.Add(pnlSag);

            int x = 80, w = 400;

            // Pencere butonları
            var btnKucult = new PencereButonu { Ikon = "", Bounds = new Rectangle(pnlSag.Width - 96, 14, 38, 38) };
            btnKucult.Click += (s, e) => this.WindowState = FormWindowState.Minimized;
            pnlSag.Controls.Add(btnKucult);
            btnExit.Visible = false;
            var btnKapat = new PencereButonu { Ikon = "", Tehlikeli = true, Bounds = new Rectangle(pnlSag.Width - 52, 14, 38, 38) };
            btnKapat.Click += (s, e) => btnExit_Click(btnKapat, EventArgs.Empty);
            pnlSag.Controls.Add(btnKapat);

            var rozet = new Rozet { Metin = "GÜVENLİ GİRİŞ", Location = new Point(x, 104) };
            pnlSag.Controls.Add(rozet);

            lblBaslik = YeniEtiket("Tekrar hoş geldiniz", Tema.Yazi(24f, true), Tema.Metin, new Point(x - 3, 136));
            lblAltBaslik = YeniEtiket("Devam etmek için TamgaApp hesabınızla giriş yapın.", Tema.Yazi(10.5f), Tema.SolukMetin, new Point(x, 182));

            EtiketiDuzenle(label36, "Kullanıcı adı", new Point(x, 230));
            alanKullanici = new GirisAlani(txtKullaniciAdi, "", false) { Bounds = new Rectangle(x, 254, w, 50) };
            pnlSag.Controls.Add(alanKullanici);

            EtiketiDuzenle(label1, "Şifre", new Point(x, 318));
            alanSifre = new GirisAlani(txtSifre, "", true) { Bounds = new Rectangle(x, 342, w, 50) };
            pnlSag.Controls.Add(alanSifre);

            lblCaps = YeniEtiket("⚠  Caps Lock açık", Tema.Yazi(9f, true), Tema.Uyari, new Point(x + w - 130, 318));
            lblCaps.AutoSize = false; lblCaps.Size = new Size(130, 20); lblCaps.TextAlign = ContentAlignment.MiddleRight; lblCaps.Visible = false;

            chkBeniHatirla.Parent = pnlSag;
            chkBeniHatirla.Text = "Beni hatırla";
            chkBeniHatirla.Font = Tema.Yazi(9.5f);
            chkBeniHatirla.ForeColor = Tema.Metin;
            chkBeniHatirla.BackColor = Color.White;
            chkBeniHatirla.AutoSize = true;
            chkBeniHatirla.Location = new Point(x, 406);
            chkBeniHatirla.Cursor = Cursors.Hand;

            var lnkUnuttum = new LinkLabel { Text = "Şifremi unuttum", AutoSize = true, Font = Tema.Yazi(9.5f, true), LinkColor = Tema.Vurgu, ActiveLinkColor = Tema.VurguKoyu, LinkBehavior = LinkBehavior.HoverUnderline, BackColor = Color.White };
            lnkUnuttum.Location = new Point(x + w - TextRenderer.MeasureText(lnkUnuttum.Text, lnkUnuttum.Font).Width - 2, 407);
            lnkUnuttum.LinkClicked += (s, e) => MessageBox.Show(this, "Güvenliğiniz için şifreler sistemde geri döndürülemez biçimde saklanır.\n\nYöneticiniz şifrenizi Ayarlar › Yönetim › \"Seçili Kullanıcının Şifresini Sıfırla\" ile yenileyebilir.", "Şifremi Unuttum", MessageBoxButtons.OK, MessageBoxIcon.Information);
            pnlSag.Controls.Add(lnkUnuttum);

            kutuHata = new HataKutusu { Bounds = new Rectangle(x, 440, w, 0), Visible = false };
            pnlSag.Controls.Add(kutuHata);

            btnGiris.Parent = pnlSag;
            btnGiris.Bounds = new Rectangle(x, 446, w, 52);
            btnGiris.Text = "Giriş Yap";
            btnGiris.BackColor = Color.SeaGreen;
            Tema.ButonStil(btnGiris);
            btnGiris.Font = Tema.Yazi(11.5f, true);
            Tema.HaricTut(btnGiris);
            this.AcceptButton = btnGiris;

            lblVersiyon.Parent = pnlSag;
            lblVersiyon.Font = Tema.Yazi(9f);
            lblVersiyon.ForeColor = Tema.SolukMetin;
            lblVersiyon.BackColor = Color.White;
            lblVersiyon.AutoSize = true;
            lblVersiyon.Location = new Point(x, 578);

            lblGuncelleme = YeniEtiket("", Tema.Yazi(9f, true), Tema.SolukMetin, new Point(x + 160, 578));
            lblGuncelleme.AutoSize = false;
            lblGuncelleme.Size = new Size(w - 160, 20);
            lblGuncelleme.TextAlign = ContentAlignment.MiddleRight;
            lblGuncelleme.Cursor = Cursors.Hand;
            lblGuncelleme.Click += (s, e) =>
            {
                if (Guncelleme.YeniSurumVar) Guncelleme.PencereyiGoster();
                else Guncelleme.Denetle(false);
            };

            // Caps Lock uyarısı
            txtSifre.KeyUp += (s, e) => CapsKontrol();
            txtSifre.Enter += (s, e) => CapsKontrol();
            txtSifre.Leave += (s, e) => lblCaps.Visible = false;
            txtKullaniciAdi.TextChanged += (s, e) => HatayiGizle();
            txtSifre.TextChanged += (s, e) => HatayiGizle();
            this.KeyDown += (s, e) => { if (e.KeyCode == Keys.Escape) btnExit_Click(this, EventArgs.Empty); };

            // Animasyonlar
            animasyon.Tick += (s, e) =>
            {
                animasyonZamani += 0.033f;
                if (Opacity < 1) Opacity = Math.Min(1, Opacity + 0.08);
                Invalidate(new Rectangle(0, 0, MarkaGenisligi, FormBoyutu.Height));
            };
            this.Shown += (s, e) => animasyon.Start();
            this.FormClosed += (s, e) => { animasyon.Stop(); kilitSayaci.Stop(); arkaPlanOnbellek?.Dispose(); onPlanOnbellek?.Dispose(); };
            kilitSayaci.Tick += (s, e) => KilitDurumunuGuncelle();

            this.ResumeLayout(true);
        }

        protected override CreateParams CreateParams
        {
            get { var cp = base.CreateParams; cp.ClassStyle |= 0x20000; return cp; } // CS_DROPSHADOW
        }

        private Label YeniEtiket(string metin, Font yazi, Color renk, Point konum)
        {
            var l = new Label { Text = metin, Font = yazi, ForeColor = renk, BackColor = Color.White, AutoSize = true, Location = konum };
            pnlSag.Controls.Add(l);
            return l;
        }

        private void EtiketiDuzenle(Label l, string metin, Point konum)
        {
            l.Parent = pnlSag;
            l.Text = metin;
            l.Font = Tema.Yazi(9.5f, true);
            l.ForeColor = Tema.Metin;
            l.BackColor = Color.White;
            l.AutoSize = true;
            l.Location = konum;
        }

        private void CapsKontrol()
        {
            lblCaps.Visible = txtSifre.Focused && Control.IsKeyLocked(Keys.CapsLock);
        }

        #region ❗ HATA, KİLİT VE GÜNCELLEME DURUMU

        private void HataGoster(string mesaj, bool bilgi = false)
        {
            kutuHata.Mesaj = mesaj;
            kutuHata.Bilgi = bilgi;
            kutuHata.Height = 44;
            kutuHata.Top = 440;
            kutuHata.Visible = true;
            btnGiris.Top = 496;
            kutuHata.Invalidate();
        }

        private void HatayiGizle()
        {
            if (kutuHata == null || !kutuHata.Visible || Guvenlik.KalanKilitSuresi > TimeSpan.Zero) return;
            kutuHata.Visible = false;
            btnGiris.Top = 446;
        }

        private void KilitDurumunuGuncelle()
        {
            TimeSpan kalan = Guvenlik.KalanKilitSuresi;
            if (kalan > TimeSpan.Zero)
            {
                btnGiris.Enabled = false;
                HataGoster($"Çok fazla hatalı deneme. Güvenliğiniz için {Math.Ceiling(kalan.TotalSeconds)} sn sonra tekrar deneyin.");
                if (!kilitSayaci.Enabled) kilitSayaci.Start();
            }
            else
            {
                kilitSayaci.Stop();
                if (!btnGiris.Enabled)
                {
                    btnGiris.Enabled = true;
                    kutuHata.Visible = false;
                    btnGiris.Top = 446;
                }
            }
        }

        /// <summary>Hatalı girişte giriş kartını kısa süre sağa sola sallar.</summary>
        private void Salla()
        {
            int[] adimlar = { 12, -10, 8, -6, 4, -2, 0 };
            int i = 0;
            var t = new Timer { Interval = 28 };
            t.Tick += (s, e) =>
            {
                if (i >= adimlar.Length) { t.Stop(); t.Dispose(); pnlSag.Left = MarkaGenisligi; return; }
                pnlSag.Left = MarkaGenisligi + adimlar[i++];
            };
            t.Start();
        }

        private void GuncellemeDurumunuGoster()
        {
            if (lblGuncelleme == null || lblGuncelleme.IsDisposed) return;
            if (Guncelleme.Denetleniyor) { lblGuncelleme.Text = "Güncellemeler denetleniyor…"; lblGuncelleme.ForeColor = Tema.SolukMetin; }
            else if (Guncelleme.YeniSurumVar) { lblGuncelleme.Text = "⬆  Yeni sürüm hazır: " + Guncelleme.SonBilgi.CurrentVersion; lblGuncelleme.ForeColor = Tema.Vurgu; }
            else if (!string.IsNullOrEmpty(Guncelleme.SonHata)) { lblGuncelleme.Text = "Güncelleme sunucusuna ulaşılamadı · Tekrar dene"; lblGuncelleme.ForeColor = Tema.SolukMetin; }
            else if (Guncelleme.SonDenetim.HasValue) { lblGuncelleme.Text = "✓  Güncel"; lblGuncelleme.ForeColor = Tema.Basari; }
            else { lblGuncelleme.Text = "Güncellemeleri denetle"; lblGuncelleme.ForeColor = Tema.SolukMetin; }
        }

        #endregion

        #region 🎨 MARKA PANELİ ÇİZİMİ (ANİMASYONLU)

        protected override void OnPaintBackground(PaintEventArgs e) { /* tüm çizim OnPaint'te (titreşimsiz) */ }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            if (arkaPlanOnbellek == null) OnbellekleriOlustur();

            g.DrawImageUnscaled(arkaPlanOnbellek, 0, 0);

            // Yavaşça süzülen iki yumuşak ışık
            g.SmoothingMode = SmoothingMode.AntiAlias;
            IsikCiz(g, 110 + 50 * (float)Math.Sin(animasyonZamani * 0.6), 150 + 40 * (float)Math.Cos(animasyonZamani * 0.5), 260, Color.FromArgb(70, 45, 212, 191));
            IsikCiz(g, 390 + 40 * (float)Math.Cos(animasyonZamani * 0.4), 520 + 50 * (float)Math.Sin(animasyonZamani * 0.45), 300, Color.FromArgb(55, 16, 185, 129));

            g.DrawImageUnscaled(onPlanOnbellek, 0, 0);
            g.FillRectangle(Brushes.White, MarkaGenisligi, 0, Width - MarkaGenisligi, Height);
        }

        private static void IsikCiz(Graphics g, float cx, float cy, float r, Color renk)
        {
            using (var yol = new GraphicsPath())
            {
                yol.AddEllipse(cx - r, cy - r, r * 2, r * 2);
                using (var b = new PathGradientBrush(yol) { CenterColor = renk, SurroundColors = new[] { Color.FromArgb(0, renk) } })
                {
                    var eski = g.Clip;
                    g.SetClip(new Rectangle(0, 0, MarkaGenisligi, FormBoyutu.Height));
                    g.FillPath(b, yol);
                    g.Clip = eski;
                }
            }
        }

        private void OnbellekleriOlustur()
        {
            int w = MarkaGenisligi, h = FormBoyutu.Height;

            // Arka plan: koyu grafit → zümrüt geçişi + ince altıgen deseni
            arkaPlanOnbellek = new Bitmap(Width, Height);
            using (var g = Graphics.FromImage(arkaPlanOnbellek))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.White);
                using (var b = new LinearGradientBrush(new Rectangle(0, 0, w, h), Color.FromArgb(8, 13, 22), Color.FromArgb(12, 74, 66), 65f))
                    g.FillRectangle(b, 0, 0, w, h);

                using (var p = new Pen(Color.FromArgb(16, 255, 255, 255), 1f))
                {
                    float r = 26f, dx = r * 1.732f, dy = r * 1.5f;
                    for (int satir = -1; satir * dy < h + r; satir++)
                        for (int sutun = -1; sutun * dx < w + r; sutun++)
                        {
                            float cx = sutun * dx + (satir % 2 == 0 ? 0 : dx / 2), cy = satir * dy;
                            var noktalar = new PointF[6];
                            for (int k = 0; k < 6; k++)
                            {
                                double a = Math.PI / 180 * (60 * k - 30);
                                noktalar[k] = new PointF(cx + r * (float)Math.Cos(a), cy + r * (float)Math.Sin(a));
                            }
                            g.DrawPolygon(p, noktalar);
                        }
                }
                // sağ kenarda yumuşak bir ayrım çizgisi
                using (var b = new LinearGradientBrush(new Rectangle(w - 60, 0, 60, h), Color.FromArgb(0, 0, 0, 0), Color.FromArgb(60, 0, 0, 0), 0f))
                    g.FillRectangle(b, w - 60, 0, 60, h);
            }

            // Ön plan: logo, yazı ve özellik listesi (saydam katman)
            onPlanOnbellek = new Bitmap(Width, Height);
            using (var g = Graphics.FromImage(onPlanOnbellek))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

                Image logo = pictureBox2.BackgroundImage ?? pictureBox2.Image;
                if (logo != null)
                {
                    int lw = 240, lh = (int)(lw * (logo.Height / (float)logo.Width));
                    g.DrawImage(logo, new Rectangle((w - lw) / 2, 70, lw, lh));
                }
                Image yazi = pictureBox1.BackgroundImage ?? pictureBox1.Image;
                if (yazi != null)
                {
                    int yw = 270, yh = (int)(yw * (yazi.Height / (float)yazi.Width));
                    g.DrawImage(yazi, new Rectangle((w - yw) / 2, 262, yw, yh));
                }

                using (var sf = new StringFormat { Alignment = StringAlignment.Center })
                using (var b = new SolidBrush(Color.White))
                    g.DrawString("Depo, sevkiyat ve etiket yönetimi — tek ekranda.", Tema.Yazi(12.5f, true), b, new RectangleF(30, 340, w - 60, 30), sf);

                string[] ozellikler = { "Barkodlu sevkiyat, paletleme ve kamyon yükleme", "ERP entegrasyonu ve canlı stok raporları", "Otomatik yedekleme ve işlem günlüğü" };
                int y = 396;
                foreach (string o in ozellikler)
                {
                    var tik = new Rectangle(86, y, 24, 24);
                    using (var b = new SolidBrush(Color.FromArgb(45, 45, 212, 191))) g.FillEllipse(b, tik);
                    TextRenderer.DrawText(g, "", Tema.Ikon(9f), tik, Color.FromArgb(94, 234, 212), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                    using (var b = new SolidBrush(Color.FromArgb(226, 232, 240)))
                        g.DrawString(o, Tema.Yazi(10.5f), b, 122, y + 2);
                    y += 38;
                }

                using (var sf = new StringFormat { Alignment = StringAlignment.Center })
                using (var b = new SolidBrush(Color.FromArgb(120, 148, 163, 184)))
                    g.DrawString("© " + DateTime.Now.Year + " YalçınSoft · Tüm hakları saklıdır", Tema.Yazi(8.5f), b, new RectangleF(0, h - 40, w, 20), sf);
            }
        }

        #endregion

        #region 🧩 GİRİŞ EKRANI KONTROLLERİ

        /// <summary>İkonlu, yuvarlak kenarlı, odaklanınca vurgulanan giriş kutusu (içinde gerçek TextBox).</summary>
        private class GirisAlani : Panel
        {
            private readonly TextBox kutu;
            private readonly string ikon;
            private readonly Label goz;

            public GirisAlani(TextBox kutu, string ikon, bool sifreAlani)
            {
                this.kutu = kutu; this.ikon = ikon;
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
                BackColor = Color.White;
                Tag = Tema.TemaYok;
                Cursor = Cursors.IBeam;

                kutu.Parent = this;
                kutu.BorderStyle = BorderStyle.None;
                kutu.Font = Tema.Yazi(12f);
                kutu.ForeColor = Tema.Metin;
                kutu.BackColor = Color.White;
                if (sifreAlani) { kutu.PasswordChar = '\0'; kutu.UseSystemPasswordChar = true; }
                kutu.Enter += (s, e) => Invalidate();
                kutu.Leave += (s, e) => Invalidate();
                Click += (s, e) => kutu.Focus();

                if (sifreAlani)
                {
                    goz = new Label { Text = "", Font = Tema.Ikon(12f), ForeColor = Tema.SolukMetin, BackColor = Color.White, Size = new Size(40, 40), TextAlign = ContentAlignment.MiddleCenter, Cursor = Cursors.Hand, Tag = Tema.TemaYok };
                    goz.Click += (s, e) =>
                    {
                        kutu.UseSystemPasswordChar = !kutu.UseSystemPasswordChar;
                        goz.ForeColor = kutu.UseSystemPasswordChar ? Tema.SolukMetin : Tema.Vurgu;
                        kutu.Focus();
                        kutu.SelectionStart = kutu.TextLength;
                    };
                    new ToolTip().SetToolTip(goz, "Şifreyi göster / gizle");
                    Controls.Add(goz);
                }
            }

            protected override void OnLayout(LayoutEventArgs e)
            {
                base.OnLayout(e);
                int sag = goz != null ? 46 : 14;
                kutu.Location = new Point(46, (Height - kutu.Height) / 2);
                kutu.Width = Width - 46 - sag;
                if (goz != null) goz.Location = new Point(Width - 44, (Height - 40) / 2);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.White);
                bool odak = kutu.Focused;
                if (odak)
                {
                    using (var yol = Tema.YuvarlakDikdortgen(new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f), 12))
                    using (var b = new SolidBrush(Tema.VurguCokAcik)) g.FillPath(b, yol);
                }
                using (var yol = Tema.YuvarlakDikdortgen(new RectangleF(1.5f, 1.5f, Width - 3.5f, Height - 3.5f), 11))
                {
                    using (var b = new SolidBrush(Color.White)) g.FillPath(b, yol);
                    using (var p = new Pen(odak ? Tema.Vurgu : Color.FromArgb(203, 213, 225), odak ? 1.6f : 1f)) g.DrawPath(p, yol);
                }
                TextRenderer.DrawText(g, ikon, Tema.Ikon(12f), new Rectangle(8, 0, 36, Height), odak ? Tema.Vurgu : Tema.SolukMetin, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }

        private class HataKutusu : Control
        {
            public string Mesaj;
            public bool Bilgi;
            public HataKutusu()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
                Tag = Tema.TemaYok;
            }
            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.White);
                Color ana = Bilgi ? Tema.Basari : Tema.Tehlike;
                using (var yol = Tema.YuvarlakDikdortgen(new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f), 10))
                {
                    using (var b = new SolidBrush(Tema.Karistir(ana, Color.White, 0.9f))) g.FillPath(b, yol);
                    using (var p = new Pen(Tema.Karistir(ana, Color.White, 0.6f))) g.DrawPath(p, yol);
                }
                TextRenderer.DrawText(g, Bilgi ? "" : "", Tema.Ikon(11f), new Rectangle(8, 0, 32, Height), ana, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                TextRenderer.DrawText(g, Mesaj, Tema.Yazi(9.5f, true), new Rectangle(42, 0, Width - 50, Height), Tema.Karistir(ana, Color.Black, 0.25f), TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            }
        }

        private class Rozet : Control
        {
            public string Metin;
            public Rozet()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
                Size = new Size(132, 24);
                Tag = Tema.TemaYok;
            }
            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.White);
                using (var yol = Tema.YuvarlakDikdortgen(new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f), 12))
                using (var b = new SolidBrush(Tema.VurguCokAcik)) g.FillPath(b, yol);
                using (var b = new SolidBrush(Tema.Vurgu)) g.FillEllipse(b, 10, 8, 8, 8);
                TextRenderer.DrawText(g, Metin, Tema.Yazi(8f, true), new Rectangle(24, 0, Width - 28, Height), Tema.VurguKoyu, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
            }
        }

        private class PencereButonu : Control
        {
            public string Ikon;
            public bool Tehlikeli;
            private bool ustunde;
            public PencereButonu()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
                Cursor = Cursors.Hand;
                Tag = Tema.TemaYok;
            }
            protected override void OnMouseEnter(EventArgs e) { ustunde = true; Invalidate(); base.OnMouseEnter(e); }
            protected override void OnMouseLeave(EventArgs e) { ustunde = false; Invalidate(); base.OnMouseLeave(e); }
            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.White);
                if (ustunde)
                    using (var yol = Tema.YuvarlakDikdortgen(new RectangleF(0, 0, Width - 1, Height - 1), 10))
                    using (var b = new SolidBrush(Tehlikeli ? Tema.Tehlike : Color.FromArgb(241, 245, 249))) g.FillPath(b, yol);
                TextRenderer.DrawText(g, Ikon, Tema.Ikon(10f), ClientRectangle, ustunde && Tehlikeli ? Color.White : Tema.SolukMetin, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }

        #endregion
    }
}
