using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace TamgaApp
{
    /// <summary>
    /// v2.1 revizyonu ile gelen özellikler: komut paleti (Ctrl+K) ve kısayollar, Ana Panel canlı durum
    /// kartları, işlem günlüğü sayfası, Hakkında/Sistem bilgisi penceresi, güncelleme rozeti,
    /// yedek saklama politikası ve küçük yardımcılar.
    /// </summary>
    public partial class MainForm
    {
        #region 🧰 YARDIMCILAR

        /// <summary>Login ekranından gelen karşılama mesajı (açılışta bildirim olarak gösterilir).</summary>
        public static string AcilisKarsilamasi;

        private static DateTime? sonYedekZamani;

        /// <summary>HTML çıktılarına yazılan verileri kaçışlar (firma adındaki &lt; &gt; &amp; çıktıyı bozmasın).</summary>
        internal static string Html(object deger)
        {
            return System.Net.WebUtility.HtmlEncode(Convert.ToString(deger) ?? "");
        }

        /// <summary>
        /// Üretim raporu kayıt yeri ayarı artık AppData'da tutulur (program yönetici izni olmadan çalışabilsin).
        /// Eski sürümün program klasörüne yazdığı dosya varsa bir kez taşınır.
        /// </summary>
        private static string KayitYeriAyarDosyasi()
        {
            string klasor = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TamgaApp");
            Directory.CreateDirectory(klasor);
            string yeni = Path.Combine(klasor, "KayitYeri.txt");
            try
            {
                string eski = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "KayitYeri.txt");
                if (!File.Exists(yeni) && File.Exists(eski)) File.Copy(eski, yeni);
            }
            catch { }
            return yeni;
        }

        /// <summary>
        /// Otomatik yedekler 4 saatte bir alınır ve eskiden hiç silinmiyordu (masaüstü/OneDrive dolabiliyordu).
        /// En yeni <paramref name="saklanacak"/> tamamlanmış yedek tutulur, gerisi silinir.
        /// </summary>
        private void EskiYedekleriTemizle(int saklanacak)
        {
            try
            {
                if (!Directory.Exists(yedeklerKlasoru)) return;
                var yedekler = new DirectoryInfo(yedeklerKlasoru).GetDirectories("Yedek_*")
                    .Where(d => !d.Name.EndsWith(".incomplete", StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(d => d.Name, StringComparer.Ordinal)
                    .ToList();
                foreach (var eski in yedekler.Skip(saklanacak))
                {
                    try { eski.Delete(true); } catch { }
                }
            }
            catch { }
        }

        private void SonYedekZamaniniBul()
        {
            try
            {
                if (!Directory.Exists(yedeklerKlasoru)) return;
                var son = new DirectoryInfo(yedeklerKlasoru).GetDirectories("Yedek_*")
                    .Where(d => !d.Name.EndsWith(".incomplete", StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(d => d.CreationTime).FirstOrDefault();
                if (son != null) sonYedekZamani = son.CreationTime;
            }
            catch { }
        }

        private static string GoreceZaman(DateTime? t)
        {
            if (!t.HasValue) return "Henüz yok";
            var f = DateTime.Now - t.Value;
            if (f.TotalMinutes < 1) return "Az önce";
            if (f.TotalMinutes < 60) return (int)f.TotalMinutes + " dk önce";
            if (f.TotalHours < 24) return (int)f.TotalHours + " saat önce";
            return (int)f.TotalDays + " gün önce";
        }

        private static long TabloSayisi(string tablo)
        {
            try
            {
                using (var conn = DbHelper.GetConnection())
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT COUNT(*) FROM " + tablo + ";";
                        return Convert.ToInt64(cmd.ExecuteScalar());
                    }
                }
            }
            catch { return -1; }
        }

        private static long BugunkuIslemSayisi()
        {
            try
            {
                using (var conn = DbHelper.GetConnection())
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT COUNT(*) FROM IslemGunlugu WHERE Tarih >= @t;";
                        cmd.Parameters.AddWithValue("@t", DateTime.Today.ToString("yyyy-MM-dd 00:00:00"));
                        return Convert.ToInt64(cmd.ExecuteScalar());
                    }
                }
            }
            catch { return -1; }
        }

        #endregion

        #region 🚀 REVİZYON KURULUMU (ModernArayuzuKur içinden çağrılır)

        private Timer guncellemeZamanlayici;

        private void RevizyonOzellikleriniKur()
        {
            SonYedekZamaniniBul();
            EskiYedekleriTemizle(30);
            Gunluk.EskileriTemizle(180);

            // Güncelleme: login'de denetlendi; çalışırken her 3 saatte bir sessizce tekrar denetle.
            Guncelleme.DurumDegisti += GuncellemeRozetiniYenile;
            this.FormClosed += (s, e) => Guncelleme.DurumDegisti -= GuncellemeRozetiniYenile;
            guncellemeZamanlayici = new Timer { Interval = 3 * 60 * 60 * 1000 };
            guncellemeZamanlayici.Tick += (s, e) => Guncelleme.Denetle(true);
            guncellemeZamanlayici.Start();

            this.Shown += (s, e) =>
            {
                GuncellemeRozetiniYenile();
                if (!string.IsNullOrEmpty(AcilisKarsilamasi))
                    Bildirim.Goster("Hoş geldiniz, " + AktifKullaniciAdi, AcilisKarsilamasi, BildirimTuru.Basari, 6000);
                else
                    Bildirim.Goster("Hoş geldiniz, " + AktifKullaniciAdi, "Komut paleti için Ctrl+K, sayfalar arası geçiş için Ctrl+1…9.", BildirimTuru.Bilgi, 6000);
                if (!Guncelleme.SonDenetim.HasValue && !Guncelleme.Denetleniyor) Guncelleme.Denetle(true);
            };
        }

        #endregion

        #region ⌨️ KISAYOLLAR VE KOMUT PALETİ

        private bool GenelKisayoluIsle(Keys tus)
        {
            if (!modernArayuzKuruldu) return false;
            if (tus == (Keys.Control | Keys.K)) { KomutPaletiniAc(); return true; }
            if (tus == (Keys.Control | Keys.B)) { MenuyuDaralt(!menuDaraltildi); return true; }
            if (tus == (Keys.Control | Keys.U)) { Guncelleme.Denetle(false); return true; }
            if (tus == Keys.F1)
            {
                var yardim = tabControl1.TabPages.Cast<TabPage>().FirstOrDefault(p => SekmeAdiTemizle(p.Text).StartsWith("Yardım"));
                if (yardim != null) { SayfayaGit(yardim); return true; }
            }
            Keys kod = tus & Keys.KeyCode;
            if ((tus & Keys.Modifiers) == Keys.Control && kod >= Keys.D1 && kod <= Keys.D9)
            {
                int sira = kod - Keys.D1;
                var sayfalar = MenuSirasiylaSayfalar();
                if (sira < sayfalar.Count) { SayfayaGit(sayfalar[sira]); return true; }
            }
            return false;
        }

        private List<TabPage> MenuSirasiylaSayfalar()
        {
            return menuOgeleri.Select(o => o.Sayfa).Where(p => p != null).ToList();
        }

        private class PaletOgesi
        {
            public string Ikon, Baslik, Aciklama, Kisayol;
            public Action Calistir;
        }

        private List<PaletOgesi> PaletOgeleriniHazirla()
        {
            var liste = new List<PaletOgesi>();
            int i = 0;
            foreach (var p in MenuSirasiylaSayfalar())
            {
                string ad = SekmeAdiTemizle(p.Text);
                var t = MenuTanimiBul(ad);
                var hedef = p;
                i++;
                liste.Add(new PaletOgesi { Ikon = t.Ikon, Baslik = ad, Aciklama = t.Aciklama, Kisayol = i <= 9 ? "Ctrl+" + i : "", Calistir = () => SayfayaGit(hedef) });
            }
            liste.Add(new PaletOgesi { Ikon = "", Baslik = "Güncellemeleri denetle", Aciklama = "Yeni sürüm var mı kontrol et", Kisayol = "Ctrl+U", Calistir = () => Guncelleme.Denetle(false) });
            liste.Add(new PaletOgesi { Ikon = "", Baslik = "Hakkında ve sistem bilgisi", Aciklama = "Sürüm, veri klasörü, veritabanı ve yedek durumu", Calistir = HakkindaGoster });
            liste.Add(new PaletOgesi { Ikon = "", Baslik = "Menüyü daralt / genişlet", Aciklama = "Sol menüyü simgelere indir", Kisayol = "Ctrl+B", Calistir = () => MenuyuDaralt(!menuDaraltildi) });
            if (AktifOturumTamYetkiliMi())
            {
                liste.Add(new PaletOgesi { Ikon = "", Baslik = "İşlem günlüğü", Aciklama = "Kim, ne zaman, ne yaptı?", Calistir = IslemGunlugunuAc });
                liste.Add(new PaletOgesi { Ikon = "", Baslik = "Şimdi yedek al", Aciklama = "Tüm verilerin doğrulanmış yedeğini oluştur", Calistir = ElleYedekAl });
            }
            liste.Add(new PaletOgesi { Ikon = "", Baslik = "Oturumu kapat", Aciklama = "Giriş ekranına dön", Calistir = () => btnLoginDon?.PerformClick() });
            return liste;
        }

        private async void ElleYedekAl()
        {
            try
            {
                Bildirim.Goster("Yedek alınıyor…", "Veriler kopyalanıp doğrulanıyor.", BildirimTuru.Bilgi, 2500);
                string yol = await YedekAlAsync();
                Bildirim.Goster("Yedek tamamlandı", Path.GetFileName(yol), BildirimTuru.Basari, 5000, () =>
                {
                    try { System.Diagnostics.Process.Start("explorer.exe", yedeklerKlasoru); } catch { }
                });
                DurumKartlariniYenile();
            }
            catch (Exception ex)
            {
                YedeklemeHatasiniKaydet(ex);
                Bildirim.Goster("Yedek alınamadı", ex.Message, BildirimTuru.Hata, 8000);
            }
        }

        private void KomutPaletiniAc()
        {
            var ogeler = PaletOgeleriniHazirla();
            using (var f = new KomutPaleti(ogeler))
            {
                var alan = RectangleToScreen(ClientRectangle);
                f.StartPosition = FormStartPosition.Manual;
                f.Location = new Point(alan.X + (alan.Width - f.Width) / 2, alan.Y + 90);
                if (f.ShowDialog(this) == DialogResult.OK && f.Secilen != null) f.Secilen.Calistir();
            }
        }

        /// <summary>Ctrl+K komut paleti: yazdıkça filtrelenen sayfa ve işlem listesi.</summary>
        private class KomutPaleti : Form
        {
            public PaletOgesi Secilen;
            private readonly List<PaletOgesi> tumu;
            private List<PaletOgesi> gorunen;
            private readonly TextBox ara;
            private readonly ListBox liste;

            public KomutPaleti(List<PaletOgesi> ogeler)
            {
                tumu = ogeler; gorunen = ogeler;
                FormBorderStyle = FormBorderStyle.None;
                ShowInTaskbar = false;
                Size = new Size(640, 470);
                BackColor = Tema.Kart;
                KeyPreview = true;
                Tema.HaricTut(this);
                Region = Region.FromHrgn(YuvarlakBolgeOlustur(0, 0, Width + 1, Height + 1, 20, 20));

                ara = new TextBox { BorderStyle = BorderStyle.None, Font = Tema.Yazi(14f), ForeColor = Tema.Metin, BackColor = Tema.Kart, Bounds = new Rectangle(58, 22, Width - 140, 32), Tag = Tema.TemaYok };
                liste = new ListBox { BorderStyle = BorderStyle.None, DrawMode = DrawMode.OwnerDrawFixed, ItemHeight = 52, Bounds = new Rectangle(10, 72, Width - 20, Height - 112), BackColor = Tema.Kart, IntegralHeight = false, Tag = Tema.TemaYok };
                Controls.Add(ara);
                Controls.Add(liste);
                ara.HandleCreated += (s, e) => IpucuMetniAyarla(ara.Handle, 0x1501, (IntPtr)1, "Sayfa veya işlem ara…");
                Doldur();

                ara.TextChanged += (s, e) => Doldur();
                liste.DrawItem += CizOge;
                liste.DoubleClick += (s, e) => Sec();
                liste.MouseClick += (s, e) => Sec();
                Deactivate += (s, e) => { if (DialogResult == DialogResult.None) Close(); };
                KeyDown += (s, e) =>
                {
                    if (e.KeyCode == Keys.Escape) { Close(); e.Handled = true; }
                    else if (e.KeyCode == Keys.Enter) { Sec(); e.Handled = true; e.SuppressKeyPress = true; }
                    else if (e.KeyCode == Keys.Down && liste.Items.Count > 0) { liste.SelectedIndex = Math.Min(liste.Items.Count - 1, liste.SelectedIndex + 1); e.Handled = true; }
                    else if (e.KeyCode == Keys.Up && liste.Items.Count > 0) { liste.SelectedIndex = Math.Max(0, liste.SelectedIndex - 1); e.Handled = true; }
                };
            }

            [System.Runtime.InteropServices.DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
            private static extern IntPtr YuvarlakBolgeOlustur(int l, int t, int r, int b, int w, int h);

            [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "SendMessageW", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
            private static extern IntPtr IpucuMetniAyarla(IntPtr h, int msg, IntPtr w, string l);

            protected override CreateParams CreateParams { get { var cp = base.CreateParams; cp.ClassStyle |= 0x20000; return cp; } }

            private void Doldur()
            {
                var tr = new System.Globalization.CultureInfo("tr-TR");
                string q = ara.Text.Trim().ToLower(tr);
                gorunen = string.IsNullOrEmpty(q) ? tumu : tumu.Where(o => (o.Baslik + " " + o.Aciklama).ToLower(tr).Contains(q)).ToList();
                liste.BeginUpdate();
                liste.Items.Clear();
                foreach (var o in gorunen) liste.Items.Add(o.Baslik);
                liste.EndUpdate();
                if (liste.Items.Count > 0) liste.SelectedIndex = 0;
                Invalidate();
            }

            private void Sec()
            {
                if (liste.SelectedIndex < 0 || liste.SelectedIndex >= gorunen.Count) return;
                Secilen = gorunen[liste.SelectedIndex];
                DialogResult = DialogResult.OK;
                Close();
            }

            private void CizOge(object sender, DrawItemEventArgs e)
            {
                if (e.Index < 0 || e.Index >= gorunen.Count) return;
                var o = gorunen[e.Index];
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                bool secili = (e.State & DrawItemState.Selected) != 0;
                using (var b = new SolidBrush(Tema.Kart)) g.FillRectangle(b, e.Bounds);
                var r = new RectangleF(e.Bounds.X + 4, e.Bounds.Y + 3, e.Bounds.Width - 8, e.Bounds.Height - 6);
                if (secili) using (var yol = Tema.YuvarlakDikdortgen(r, 10)) using (var b = new SolidBrush(Tema.VurguCokAcik)) g.FillPath(b, yol);
                var ikon = new Rectangle(e.Bounds.X + 14, e.Bounds.Y + 9, 34, 34);
                using (var yol = Tema.YuvarlakDikdortgen(ikon, 9)) using (var b = new SolidBrush(secili ? Tema.Vurgu : Color.FromArgb(241, 245, 249))) g.FillPath(b, yol);
                TextRenderer.DrawText(g, o.Ikon, Tema.Ikon(12f), ikon, secili ? Color.White : Tema.SolukMetin, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                TextRenderer.DrawText(g, o.Baslik, Tema.Yazi(10.5f, true), new Point(e.Bounds.X + 60, e.Bounds.Y + 8), Tema.Metin, TextFormatFlags.NoPrefix);
                TextRenderer.DrawText(g, o.Aciklama ?? "", Tema.Yazi(9f), new Point(e.Bounds.X + 60, e.Bounds.Y + 28), Tema.SolukMetin, TextFormatFlags.NoPrefix);
                if (!string.IsNullOrEmpty(o.Kisayol))
                {
                    Size ks = TextRenderer.MeasureText(o.Kisayol, Tema.Yazi(8.5f, true));
                    var kr = new Rectangle(e.Bounds.Right - ks.Width - 34, e.Bounds.Y + 15, ks.Width + 16, 22);
                    using (var yol = Tema.YuvarlakDikdortgen(kr, 6))
                    {
                        using (var b = new SolidBrush(Color.FromArgb(248, 250, 252))) g.FillPath(b, yol);
                        using (var p = new Pen(Tema.Cizgi)) g.DrawPath(p, yol);
                    }
                    TextRenderer.DrawText(g, o.Kisayol, Tema.Yazi(8.5f, true), kr, Tema.SolukMetin, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.Clear(Tema.Kart);
                using (var p = new Pen(Tema.Cizgi)) { g.DrawRectangle(p, 0, 0, Width - 1, Height - 1); g.DrawLine(p, 0, 66, Width, 66); g.DrawLine(p, 0, Height - 38, Width, Height - 38); }
                TextRenderer.DrawText(g, "", Tema.Ikon(14f), new Rectangle(18, 20, 32, 36), Tema.SolukMetin, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                TextRenderer.DrawText(g, "Esc", Tema.Yazi(8.5f, true), new Rectangle(Width - 66, 26, 40, 22), Tema.SolukMetin, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                TextRenderer.DrawText(g, "↑↓ gezin   ·   Enter aç   ·   Esc kapat", Tema.Yazi(8.5f), new Rectangle(0, Height - 36, Width, 34), Tema.SolukMetin, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                if (gorunen.Count == 0)
                    TextRenderer.DrawText(g, "Sonuç bulunamadı", Tema.Yazi(10.5f), new Rectangle(0, 180, Width, 30), Tema.SolukMetin, TextFormatFlags.HorizontalCenter);
            }

            protected override void OnShown(EventArgs e) { base.OnShown(e); ara.Focus(); }
        }

        #endregion

        #region 🔝 ÜST ÇUBUK EYLEMLERİ (ARAMA, GÜNCELLEME ROZETİ, HAKKINDA)

        private Button btnGuncellemeRozeti;

        private void UstCubugaEylemleriEkle()
        {
            if (ustCubuk == null) return;

            var btnAra = new Button { Text = "\U0001F50D  Ara veya git…      Ctrl+K", BackColor = SystemColors.Control, Size = new Size(250, 38), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            Tema.ButonStil(btnAra); btnAra.Font = Tema.Yazi(9.5f); Tema.HaricTut(btnAra);
            btnAra.Click += (s, e) => KomutPaletiniAc();
            MenuIpucu.SetToolTip(btnAra, "Komut paleti (Ctrl+K)");

            var btnBilgi = new Button { Text = "", BackColor = SystemColors.Control, Size = new Size(38, 38), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            Tema.ButonStil(btnBilgi); btnBilgi.Font = Tema.Ikon(12f); Tema.HaricTut(btnBilgi);
            btnBilgi.Click += (s, e) => HakkindaGoster();
            MenuIpucu.SetToolTip(btnBilgi, "Hakkında ve sistem bilgisi");

            btnGuncellemeRozeti = new Button { Text = "⬆  Yeni sürüm", BackColor = Color.SeaGreen, Size = new Size(160, 38), Anchor = AnchorStyles.Top | AnchorStyles.Right, Visible = false };
            Tema.ButonStil(btnGuncellemeRozeti); btnGuncellemeRozeti.Font = Tema.Yazi(9.5f, true); Tema.HaricTut(btnGuncellemeRozeti);
            btnGuncellemeRozeti.Click += (s, e) => Guncelleme.PencereyiGoster();

            ustCubuk.Controls.Add(btnAra);
            ustCubuk.Controls.Add(btnBilgi);
            ustCubuk.Controls.Add(btnGuncellemeRozeti);

            Action yerlestir = () =>
            {
                int sag = ustCubuk.Width - 24;
                lblUstSaat.Location = new Point(sag - lblUstSaat.Width, 14);
                sag = lblUstSaat.Left - 4;
                btnBilgi.Location = new Point(sag - btnBilgi.Width, 15); sag = btnBilgi.Left - 8;
                btnAra.Location = new Point(sag - btnAra.Width, 15); sag = btnAra.Left - 8;
                btnGuncellemeRozeti.Location = new Point(sag - btnGuncellemeRozeti.Width, 15);
            };
            lblUstSaat.Width = 230;
            ustCubuk.Resize += (s, e) => yerlestir();
            yerlestir();
        }

        private void GuncellemeRozetiniYenile()
        {
            if (btnGuncellemeRozeti == null || btnGuncellemeRozeti.IsDisposed) return;
            btnGuncellemeRozeti.Visible = Guncelleme.YeniSurumVar;
            if (Guncelleme.YeniSurumVar) btnGuncellemeRozeti.Text = "⬆  Sürüm " + Guncelleme.SonBilgi.CurrentVersion;
            DurumKartlariniYenile();
        }

        #endregion

        #region 📊 ANA PANEL — CANLI DURUM KARTLARI

        private FlowLayoutPanel anaDurumKartlari;
        private Label lblSistemDurumu;
        private readonly Dictionary<string, DurumKarti> durumKartlari = new Dictionary<string, DurumKarti>();
        private Timer durumZamanlayici;

        private void AnaPanelDurumKartlariniEkle()
        {
            if (tabPage1 == null) return;
            lblSistemDurumu = new Label { Text = "Sistem Durumu", AutoSize = true, Font = Tema.Yazi(13f, true), ForeColor = Tema.Metin, BackColor = Color.Transparent, Location = new Point(34, 248), Tag = Tema.TemaYok };
            anaDurumKartlari = new FlowLayoutPanel { Location = new Point(26, 282), Size = new Size(1200, 112), BackColor = Tema.Zemin, WrapContents = false, AutoScroll = false };
            Tema.HaricTut(anaDurumKartlari);

            KartEkle("firma", "", "Kayıtlı firma", Tema.Vurgu, () => SayfayaGit(tabControl1.TabPages.Cast<TabPage>().FirstOrDefault(p => SekmeAdiTemizle(p.Text).StartsWith("Firma"))));
            KartEkle("urun", "", "Kayıtlı ürün", Tema.Bilgi, null);
            KartEkle("belge", "", "Sevk edilen belge", Tema.Basari, null);
            KartEkle("islem", "", "Bugünkü işlem", Tema.Mor, AktifOturumTamYetkiliMi() ? (Action)IslemGunlugunuAc : null);
            KartEkle("yedek", "", "Son yedek", Tema.Uyari, AktifOturumTamYetkiliMi() ? (Action)ElleYedekAl : null);
            KartEkle("okuyucu", "", "Barkod okuyucu", Tema.Cyan, null);

            tabPage1.Controls.Add(lblSistemDurumu);
            tabPage1.Controls.Add(anaDurumKartlari);

            durumZamanlayici = new Timer { Interval = 60000 };
            durumZamanlayici.Tick += (s, e) => { if (tabControl1.SelectedTab == tabPage1) DurumKartlariniYenile(); };
            durumZamanlayici.Start();
            tabControl1.SelectedIndexChanged += (s, e) => { if (tabControl1.SelectedTab == tabPage1) DurumKartlariniYenile(); };
            DurumKartlariniYenile();
        }

        private void KartEkle(string anahtar, string ikon, string baslik, Color renk, Action tik)
        {
            var k = new DurumKarti { Ikon = ikon, Baslik = baslik, Renk = renk, Margin = new Padding(6), Cursor = tik != null ? Cursors.Hand : Cursors.Default };
            if (tik != null) k.Click += (s, e) => tik();
            durumKartlari[anahtar] = k;
            anaDurumKartlari.Controls.Add(k);
        }

        private void DurumKartlariniYenile()
        {
            if (durumKartlari.Count == 0) return;
            Func<long, string> say = n => n < 0 ? "—" : n.ToString("N0", new System.Globalization.CultureInfo("tr-TR"));
            KartGuncelle("firma", say(TabloSayisi("Firmalar")), "Zarf ve etiket için");
            KartGuncelle("urun", say(TabloSayisi("Urunler")), "Barkod veritabanında");
            KartGuncelle("belge", say(TamamlananBelgeNolar.Count), "Kapatılan sipariş belgesi");
            KartGuncelle("islem", say(BugunkuIslemSayisi()), "İşlem günlüğüne kaydedildi");
            KartGuncelle("yedek", GoreceZaman(sonYedekZamani), sonYedekZamani.HasValue ? sonYedekZamani.Value.ToString("dd.MM.yyyy HH:mm") : "Otomatik yedek 4 saatte bir");
            bool bagli = barkodPort != null && barkodPort.IsOpen;
            KartGuncelle("okuyucu", bagli ? "Bağlı" : "Bağlı değil", bagli ? barkodPort.PortName + " dinleniyor" : "Ayarlar › Port İşlemleri");
        }

        private void KartGuncelle(string anahtar, string deger, string alt)
        {
            DurumKarti k;
            if (!durumKartlari.TryGetValue(anahtar, out k)) return;
            k.Deger = deger; k.Alt = alt; k.Invalidate();
        }

        private class DurumKarti : Control
        {
            public string Ikon, Baslik, Deger = "—", Alt = "";
            public Color Renk = Tema.Vurgu;
            private bool ustunde;

            public DurumKarti()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
                Size = new Size(212, 100);
                Tag = Tema.TemaYok;
            }
            protected override void OnMouseEnter(EventArgs e) { ustunde = Cursor == Cursors.Hand; Invalidate(); base.OnMouseEnter(e); }
            protected override void OnMouseLeave(EventArgs e) { ustunde = false; Invalidate(); base.OnMouseLeave(e); }

            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Tema.Zemin);
                using (var yol = Tema.YuvarlakDikdortgen(new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f), 14))
                {
                    using (var b = new SolidBrush(Tema.Kart)) g.FillPath(b, yol);
                    using (var p = new Pen(ustunde ? Renk : Tema.Cizgi, ustunde ? 1.5f : 1f)) g.DrawPath(p, yol);
                }
                var ik = new Rectangle(Width - 50, 16, 34, 34);
                using (var yol = Tema.YuvarlakDikdortgen(ik, 9)) using (var b = new SolidBrush(Tema.Karistir(Renk, Color.White, 0.86f))) g.FillPath(b, yol);
                TextRenderer.DrawText(g, Ikon, Tema.Ikon(12f), ik, Renk, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                TextRenderer.DrawText(g, Baslik, Tema.Yazi(9f, true), new Point(16, 16), Tema.SolukMetin);
                TextRenderer.DrawText(g, Deger, Tema.Yazi(17f, true), new Rectangle(14, 36, Width - 70, 34), Tema.Metin, TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
                TextRenderer.DrawText(g, Alt, Tema.Yazi(8.5f), new Rectangle(16, 72, Width - 26, 18), Tema.SolukMetin, TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            }
        }

        #endregion

        #region 📜 İŞLEM GÜNLÜĞÜ SAYFASI (YALNIZCA TAM YETKİLİ)

        private TabPage sayfaGunluk;
        private DataGridView dgvGunluk;
        private DateTimePicker dtpGunlukBas, dtpGunlukBit;
        private TextBox txtGunlukAra;
        private Label lblGunlukSayi;

        private void IslemGunluguSekmesiniEkle()
        {
            if (!AktifOturumTamYetkiliMi() || tabPrintSettings == null) return;

            sayfaGunluk = new TabPage("İşlem Günlüğü") { BackColor = Tema.Zemin };
            var ust = new Panel { Dock = DockStyle.Top, Height = 72, BackColor = Tema.Zemin, Padding = new Padding(20, 16, 20, 8) };

            ust.Controls.Add(new Label { Text = "Başlangıç", AutoSize = true, Location = new Point(20, 6) });
            dtpGunlukBas = new DateTimePicker { Format = DateTimePickerFormat.Short, Location = new Point(20, 28), Width = 130, Value = DateTime.Today.AddDays(-7) };
            ust.Controls.Add(new Label { Text = "Bitiş", AutoSize = true, Location = new Point(162, 6) });
            dtpGunlukBit = new DateTimePicker { Format = DateTimePickerFormat.Short, Location = new Point(162, 28), Width = 130, Value = DateTime.Today };
            ust.Controls.Add(new Label { Text = "Ara (kullanıcı, modül, işlem, detay)", AutoSize = true, Location = new Point(304, 6) });
            txtGunlukAra = new TextBox { Location = new Point(304, 28), Width = 300 };
            var btnListele = new Button { Text = "Listele", Location = new Point(616, 24), Size = new Size(110, 36), BackColor = Color.SeaGreen };
            var btnCsv = new Button { Text = "CSV'ye Aktar", Location = new Point(734, 24), Size = new Size(130, 36), BackColor = SystemColors.Control };
            lblGunlukSayi = new Label { AutoSize = true, Location = new Point(880, 34), ForeColor = Tema.SolukMetin };
            ust.Controls.AddRange(new Control[] { dtpGunlukBas, dtpGunlukBit, txtGunlukAra, btnListele, btnCsv, lblGunlukSayi });

            if (KurucuOturumuMu())
            {
                var btnOzet = new Button { Text = "\U0001F511 Sistem hesabı şifre özeti", Location = new Point(1060, 24), Size = new Size(230, 36), BackColor = SystemColors.Control };
                btnOzet.Click += (s, e) => SistemHesabiOzetiUret();
                ust.Controls.Add(btnOzet);
            }

            dgvGunluk = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            var govde = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20, 0, 20, 20), BackColor = Tema.Zemin };
            govde.Controls.Add(dgvGunluk);
            sayfaGunluk.Controls.Add(govde);
            sayfaGunluk.Controls.Add(ust);

            btnListele.Click += (s, e) => GunluguListele();
            txtGunlukAra.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; GunluguListele(); } };
            btnCsv.Click += (s, e) => GunluguCsvAktar();
            tabPrintSettings.SelectedIndexChanged += (s, e) => { if (tabPrintSettings.SelectedTab == sayfaGunluk) GunluguListele(); };

            tabPrintSettings.TabPages.Add(sayfaGunluk);
        }

        private void IslemGunlugunuAc()
        {
            var ayarlar = tabControl1.TabPages.Cast<TabPage>().FirstOrDefault(p => p.Controls.Contains(tabPrintSettings));
            if (ayarlar == null || sayfaGunluk == null) return;
            SayfayaGit(ayarlar);
            tabPrintSettings.SelectedTab = sayfaGunluk;
        }

        private void GunluguListele()
        {
            try
            {
                var dt = Gunluk.Getir(dtpGunlukBas.Value, dtpGunlukBit.Value, txtGunlukAra.Text.Trim());
                dgvGunluk.DataSource = dt;
                if (dgvGunluk.Columns.Contains("Detay")) dgvGunluk.Columns["Detay"].FillWeight = 260;
                if (dgvGunluk.Columns.Contains("Tarih")) dgvGunluk.Columns["Tarih"].FillWeight = 110;
                lblGunlukSayi.Text = dt.Rows.Count.ToString("N0") + " kayıt";
            }
            catch (Exception ex)
            {
                Bildirim.Goster("Günlük okunamadı", ex.Message, BildirimTuru.Hata);
            }
        }

        private void GunluguCsvAktar()
        {
            var dt = dgvGunluk.DataSource as DataTable;
            if (dt == null || dt.Rows.Count == 0) { GunluguListele(); dt = dgvGunluk.DataSource as DataTable; }
            if (dt == null || dt.Rows.Count == 0) { Bildirim.Goster("Aktarılacak kayıt yok", "Önce tarih aralığını seçip listeleyin.", BildirimTuru.Uyari); return; }
            using (var sfd = new SaveFileDialog { Filter = "CSV dosyası (*.csv)|*.csv", FileName = "TamgaApp_IslemGunlugu_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".csv" })
            {
                if (sfd.ShowDialog(this) != DialogResult.OK) return;
                var sb = new StringBuilder();
                Func<object, string> hucre = v => "\"" + (Convert.ToString(v) ?? "").Replace("\"", "\"\"") + "\"";
                sb.AppendLine(string.Join(";", dt.Columns.Cast<DataColumn>().Select(c => hucre(c.ColumnName))));
                foreach (DataRow r in dt.Rows) sb.AppendLine(string.Join(";", r.ItemArray.Select(hucre)));
                File.WriteAllText(sfd.FileName, sb.ToString(), new UTF8Encoding(true)); // BOM: Excel Türkçe karakterleri doğru açsın
                Gunluk.Yaz("İşlem Günlüğü", "Günlük dışa aktarıldı", sfd.FileName);
                Bildirim.Goster("Günlük dışa aktarıldı", Path.GetFileName(sfd.FileName), BildirimTuru.Basari, 5000);
            }
        }

        /// <summary>Kurucu/Yönetici şifresini değiştirmek için Guvenlik.cs'ye yapıştırılacak özeti üretir.</summary>
        private void SistemHesabiOzetiUret()
        {
            string s1 = BasitGirdiAl("Yeni sistem hesabı şifresi:", true);
            if (string.IsNullOrEmpty(s1)) return;
            string kural = Guvenlik.SifreKuraliHatasi(s1);
            if (kural != null || s1.Length < 10) { MessageBox.Show("Sistem hesabı şifresi en az 10 karakter, harf ve rakam içermelidir.", "Zayıf Şifre", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (BasitGirdiAl("Şifreyi tekrar girin:", true) != s1) { MessageBox.Show("Şifreler uyuşmuyor.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
            string ozet = Guvenlik.OzetUret(s1);
            try { Clipboard.SetText(ozet); } catch { }
            MessageBox.Show("Özet panoya kopyalandı:\n\n" + ozet + "\n\nGuvenlik.cs içindeki KurucuOzeti (veya YoneticiOzeti) sabitine yapıştırıp yeni sürüm yayınlayın. Şifrenin kendisi hiçbir yerde saklanmaz.", "Sistem Hesabı Şifre Özeti", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private string BasitGirdiAl(string soru, bool gizli)
        {
            using (var f = new Form { Text = "TamgaApp", Width = 380, Height = 170, FormBorderStyle = FormBorderStyle.FixedDialog, StartPosition = FormStartPosition.CenterParent, MaximizeBox = false, MinimizeBox = false })
            {
                var l = new Label { Text = soru, Left = 16, Top = 14, Width = 330 };
                var t = new TextBox { Left = 16, Top = 40, Width = 330, UseSystemPasswordChar = gizli };
                var b = new Button { Text = "Tamam", Left = 236, Top = 76, Width = 110, Height = 34, DialogResult = DialogResult.OK, BackColor = Color.SeaGreen };
                f.Controls.AddRange(new Control[] { l, t, b });
                f.AcceptButton = b;
                return f.ShowDialog(this) == DialogResult.OK ? t.Text : null;
            }
        }

        #endregion

        #region ℹ️ HAKKINDA VE SİSTEM BİLGİSİ

        private void HakkindaGoster()
        {
            string veriKlasoru = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TamgaApp");
            string dbBoyut = "—";
            try { if (File.Exists(DbHelper.DbPath)) dbBoyut = (new FileInfo(DbHelper.DbPath).Length / 1024.0 / 1024.0).ToString("0.00") + " MB"; } catch { }

            var satirlar = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("Sürüm", Guncelleme.KuruluSurum + (Guncelleme.YeniSurumVar ? "   (yeni sürüm: " + Guncelleme.SonBilgi.CurrentVersion + ")" : Guncelleme.SonDenetim.HasValue ? "   (güncel)" : "")),
                new KeyValuePair<string, string>("Lisans sahibi", "YalçınSoft — TamgaApp Otomasyon Sistemi"),
                new KeyValuePair<string, string>("Oturum", AktifKullaniciAdi + (AktifOturumTamYetkiliMi() ? "  · Tam yetkili" : "")),
                new KeyValuePair<string, string>("Bilgisayar", Environment.MachineName + "  ·  " + Environment.UserName),
                new KeyValuePair<string, string>("Veri klasörü", veriKlasoru),
                new KeyValuePair<string, string>("Veritabanı", dbBoyut + "  ·  " + TabloSayisi("Firmalar") + " firma, " + TabloSayisi("Urunler") + " ürün"),
                new KeyValuePair<string, string>("Yedekler", yedeklerKlasoru + "  ·  son: " + GoreceZaman(sonYedekZamani)),
                new KeyValuePair<string, string>(".NET / Windows", Environment.Version + "  ·  " + Environment.OSVersion.VersionString),
            };

            using (var f = new HakkindaPenceresi(satirlar))
            {
                f.VeriKlasoruAc = () => { try { System.Diagnostics.Process.Start("explorer.exe", veriKlasoru); } catch { } };
                f.YedekKlasoruAc = () => { try { Directory.CreateDirectory(yedeklerKlasoru); System.Diagnostics.Process.Start("explorer.exe", yedeklerKlasoru); } catch { } };
                f.ShowDialog(this);
            }
        }

        private class HakkindaPenceresi : Form
        {
            public Action VeriKlasoruAc, YedekKlasoruAc;
            private readonly List<KeyValuePair<string, string>> satirlar;

            public HakkindaPenceresi(List<KeyValuePair<string, string>> satirlar)
            {
                this.satirlar = satirlar;
                Text = "TamgaApp Hakkında";
                FormBorderStyle = FormBorderStyle.None;
                StartPosition = FormStartPosition.CenterParent;
                ClientSize = new Size(640, 200 + satirlar.Count * 34 + 80);
                BackColor = Tema.Kart;
                DoubleBuffered = true;
                KeyPreview = true;
                Tema.HaricTut(this);
                Region = Region.FromHrgn(YuvarlakBolgeOlustur(0, 0, Width + 1, Height + 1, 22, 22));
                KeyDown += (s, e) => { if (e.KeyCode == Keys.Escape) Close(); };

                int y = Height - 64;
                var b1 = Buton("Veri klasörü", new Rectangle(28, y, 140, 40), () => VeriKlasoruAc?.Invoke());
                var b2 = Buton("Yedek klasörü", new Rectangle(176, y, 140, 40), () => YedekKlasoruAc?.Invoke());
                var b3 = Buton("Güncellemeleri denetle", new Rectangle(324, y, 180, 40), () => Guncelleme.Denetle(false));
                var b4 = Buton("Kapat", new Rectangle(512, y, 100, 40), Close, Color.SeaGreen);
                Controls.AddRange(new Control[] { b1, b2, b3, b4 });
            }

            private Button Buton(string metin, Rectangle r, Action a, Color? renk = null)
            {
                var b = new Button { Text = metin, Bounds = r, BackColor = renk ?? SystemColors.Control };
                Tema.ButonStil(b); b.Font = Tema.Yazi(9.5f, true); Tema.HaricTut(b);
                b.Click += (s, e) => a();
                return b;
            }

            [System.Runtime.InteropServices.DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
            private static extern IntPtr YuvarlakBolgeOlustur(int l, int t, int r, int b, int w, int h);

            protected override CreateParams CreateParams { get { var cp = base.CreateParams; cp.ClassStyle |= 0x20000; return cp; } }

            protected override void OnMouseDown(MouseEventArgs e)
            {
                base.OnMouseDown(e);
                if (e.Button == MouseButtons.Left) { LoginForm.ReleaseCapture(); LoginForm.SendMessage(Handle, 0xA1, 0x2, 0); }
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Tema.Kart);
                var bant = new Rectangle(0, 0, Width, 150);
                using (var b = new LinearGradientBrush(bant, Tema.Grafit, Color.FromArgb(15, 94, 84), 20f)) g.FillRectangle(b, bant);
                var logo = new Rectangle(32, 36, 76, 76);
                using (var b = new SolidBrush(Color.FromArgb(36, 255, 255, 255))) using (var yol = Tema.YuvarlakDikdortgen(logo, 18)) g.FillPath(b, yol);
                TextRenderer.DrawText(g, "T", Tema.Yazi(34f, true), logo, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                TextRenderer.DrawText(g, "TamgaApp", Tema.Yazi(22f, true), new Point(126, 40), Color.White);
                TextRenderer.DrawText(g, "Depo · Sevkiyat · Etiket Otomasyon Sistemi", Tema.Yazi(10.5f), new Point(129, 82), Color.FromArgb(203, 213, 225));
                TextRenderer.DrawText(g, "© " + DateTime.Now.Year + " YalçınSoft. Tüm hakları saklıdır.", Tema.Yazi(8.5f), new Point(129, 108), Color.FromArgb(148, 163, 184));

                int y = 176;
                foreach (var s in satirlar)
                {
                    TextRenderer.DrawText(g, s.Key, Tema.Yazi(9.5f, true), new Rectangle(32, y, 140, 24), Tema.SolukMetin, TextFormatFlags.VerticalCenter);
                    TextRenderer.DrawText(g, s.Value, Tema.Yazi(9.5f), new Rectangle(176, y, Width - 206, 24), Tema.Metin, TextFormatFlags.VerticalCenter | TextFormatFlags.PathEllipsis | TextFormatFlags.NoPrefix);
                    using (var p = new Pen(Color.FromArgb(241, 245, 249))) g.DrawLine(p, 32, y + 30, Width - 32, y + 30);
                    y += 34;
                }
            }
        }

        #endregion
    }
}
