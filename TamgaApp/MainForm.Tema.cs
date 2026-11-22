using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace TamgaApp
{
    /// <summary>
    /// Ana formun modern (web tarzı) iskeleti: sol menü, üst başlık çubuğu, Ana Panel kontrol
    /// merkezi ve alt sekmeler için hap seçiciler. Sekmelerin kendisi (TabControl) yerinde kalır;
    /// yalnızca başlık şeritleri gizlenir ve sayfa geçişleri bu menülerden yapılır. Böylece yetki
    /// filtreleri, Selecting kilidi ve sekme adına bakan bütün eski kodlar aynen çalışır.
    /// </summary>
    public partial class MainForm
    {
        #region 🧭 MENÜ TANIMLARI (GRUP, İKON, AÇIKLAMA)

        private class MenuTanimi
        {
            public string Grup;
            public string Ikon;
            public string Aciklama;
            public MenuTanimi(string grup, string ikon, string aciklama) { Grup = grup; Ikon = ikon; Aciklama = aciklama; }
        }

        private static readonly string[] MenuGrupSirasi = { "GENEL", "DEPO", "SEVKİYAT", "YAZDIRMA", "FİRMALAR", "SİSTEM", "DİĞER" };

        private static MenuTanimi MenuTanimiBul(string temizAd)
        {
            string a = temizAd.ToLowerInvariant();
            if (a.Contains("ana panel")) return new MenuTanimi("GENEL", "", "Genel bakış ve hızlı erişim");
            if (a.Contains("stok")) return new MenuTanimi("DEPO", "", "Canlı stok ve pivot raporları");
            if (a.Contains("depo kabul")) return new MenuTanimi("DEPO", "", "Üretimden gelen ürünleri kabul et");
            if (a.Contains("sayım")) return new MenuTanimi("DEPO", "", "Depo sayımı ve envanter kontrolü");
            if (a.Contains("sevkiyat plan")) return new MenuTanimi("SEVKİYAT", "", "Siparişleri paletle ve sevk et");
            if (a.Contains("sevkiyat")) return new MenuTanimi("SEVKİYAT", "", "Palet etiketleri ve sevk arşivi");
            if (a.Contains("normal zarf")) return new MenuTanimi("YAZDIRMA", "", "Tek firma için zarf yazdır");
            if (a.Contains("çoklu zarf")) return new MenuTanimi("YAZDIRMA", "", "Birden çok firmaya toplu zarf");
            if (a.Contains("etiket")) return new MenuTanimi("YAZDIRMA", "", "Manuel ve seri etiket yazdır");
            if (a.Contains("yeni firma")) return new MenuTanimi("FİRMALAR", "", "Firma kartı oluştur");
            if (a.Contains("firma")) return new MenuTanimi("FİRMALAR", "", "Firma bilgilerini düzenle");
            if (a.Contains("ayar")) return new MenuTanimi("SİSTEM", "", "Yazıcı, şablon, kullanıcı ve SQL");
            if (a.Contains("yardım")) return new MenuTanimi("SİSTEM", "", "Kullanım kılavuzu");
            return new MenuTanimi("DİĞER", "", "");
        }

        private static IEnumerable<Control> TumKontroller(Control kok)
        {
            foreach (Control c in kok.Controls)
            {
                yield return c;
                foreach (Control alt in TumKontroller(c)) yield return alt;
            }
        }

        /// <summary>Sekme adının başındaki emoji/simge karakterlerini atar ("📦 Stok" → "Stok").</summary>
        private static string SekmeAdiTemizle(string ad)
        {
            if (string.IsNullOrEmpty(ad)) return "";
            int i = 0;
            while (i < ad.Length && !char.IsLetterOrDigit(ad[i])) i++;
            return ad.Substring(i).Trim();
        }

        #endregion

        #region 🏗️ İSKELET KURULUMU

        private Panel solMenu;
        private Panel solMenuListe;
        private Panel solMenuAlt;
        private Panel ustCubuk;
        private Label lblSayfaBaslik;
        private Label lblSayfaGrup;
        private Label lblUstSaat;
        private readonly List<MenuOgesi> menuOgeleri = new List<MenuOgesi>();
        private bool menuDaraltildi;
        private const int MenuGenisligi = 232;
        private const int MenuDarGenislik = 68;
        private bool modernArayuzKuruldu;

        /// <summary>
        /// MainForm_Load'un en sonunda (yetki filtreleri uygulandıktan sonra) çağrılır.
        /// </summary>
        private void ModernArayuzuKur()
        {
            if (modernArayuzKuruldu) return;
            modernArayuzKuruldu = true;

            this.SuspendLayout();
            this.BackColor = Tema.Zemin; // Not: Form.Font değiştirilmez (AutoScale tüm sayfaları büyütür)

            // Sol menü eklenince içerik alanı daralır. Dikey bölücülerde (SplitContainer) dar olan araç
            // panelinin genişliği sabitlenir; aksi halde WinForms iki paneli orantılı küçültür ve
            // soldaki buton/kutu panelleri kesilir.
            foreach (var sc in TumKontroller(this).OfType<SplitContainer>())
            {
                if (sc.Orientation != Orientation.Vertical || sc.FixedPanel != FixedPanel.None) continue;
                sc.FixedPanel = sc.Panel1.Width <= sc.Panel2.Width ? FixedPanel.Panel1 : FixedPanel.Panel2;
            }

            tabControl1.DrawItem -= TabControl1_DrawItem; // gizli sekme şeridi artık çizilmez
            Tema.BasliklariGizle(tabControl1);
            tabControl1.Margin = new Padding(0);

            SolMenuyuOlustur();
            UstCubuguOlustur();
            UstCubugaEylemleriEkle();
            RevizyonOzellikleriniKur();

            this.Controls.Add(solMenu);
            this.Controls.Add(ustCubuk);
            this.Controls.SetChildIndex(tabControl1, 0);
            this.Controls.SetChildIndex(ustCubuk, 1);
            this.Controls.SetChildIndex(solMenu, 2);

            AnaPaneliYenidenTasarla();
            AnaPanelDurumKartlariniEkle();
            AnaPanelYerlesimi();
            IslemGunluguSekmesiniEkle();
            AltSekmeleriHapaCevir(tabPrintSettings, 54);
            AltSekmeleriHapaCevir(tabControl2, 44);

            tabControl1.SelectedIndexChanged += (s, e) => MenuSeciminiGuncelle();
            tabControl1.ControlAdded += (s, e) => MenuyuDoldur();
            tabControl1.ControlRemoved += (s, e) => BeginInvoke(new Action(MenuyuDoldur));

            // Küçük ekranlarda (ör. 1366 px dizüstü/tablet) içerik alanı daralmasın diye menü kendiliğinden daralır.
            if (Screen.FromControl(this).WorkingArea.Width < 1600) MenuyuDaralt(true);

            MenuyuDoldur();
            this.ResumeLayout(true);
            Tema.Uygula(this);
            SablonTasarimEkraniniDuzenle();
            SevkiyatPlanPaneliniDuzenle();
            NormalZarfPaneliniDuzenle();
            Tema.BasligiBoya(this);
            this.Text = "TamgaApp  ·  " + AktifKullaniciAdi;

            // Form ekrana gelip gerçek (tam ekran) boyutlarını aldıktan sonra taşan satırları sıkıştır.
            // Not: WinForms gizli sekmelere gerçek boyutunu sayfa seçilene kadar vermez; bu yüzden kontrol
            // her sayfa (ve alt sayfa) açıldığında, o sayfa için yapılır.
            this.Shown += (s, e) => BeginInvoke(new Action(() => TasanSatirlariSikistir(tabControl1.SelectedTab)));
            tabControl1.SelectedIndexChanged += (s, e) => BeginInvoke(new Action(() => TasanSatirlariSikistir(tabControl1.SelectedTab)));
            foreach (var ic in TumKontroller(tabControl1).OfType<TabControl>().ToList())
            {
                var icSekme = ic;
                icSekme.SelectedIndexChanged += (s, e) => BeginInvoke(new Action(() => TasanSatirlariSikistir(icSekme.SelectedTab)));
            }
        }

        /// <summary>
        /// Bir kapsayıcının içindeki nesnelerin gerçekte ihtiyaç duyduğu genişliği hesaplar
        /// (yerleşik/dock'lu iç paneller için onların içeriğine bakar; tablolar esnek kabul edilir).
        /// </summary>
        private static int IcerikGenisligi(Control kap)
        {
            int enSag = 0;
            foreach (Control c in kap.Controls)
            {
                if (c is DataGridView || c is SplitContainer || c is TabControl) continue;
                int sag;
                if (c.Dock == DockStyle.None) sag = c.Right;
                else if (c.Controls.Count > 0 && !(c is ListBox)) sag = c.Left + IcerikGenisligi(c);
                else continue;
                if (sag > enSag) enSag = sag;
            }
            return enSag;
        }

        /// <summary>
        /// Dikey bölücülerde dar kalan paneli (ör. Firma Düzenleme sağ paneli) butonları ve kutuları
        /// tam görünecek kadar genişletir. Geniş paneldeki tablonun en az %40'ı her zaman korunur.
        /// </summary>
        private static void BolucuyuIcerigeSigdir(SplitContainer sc)
        {
            if (sc.Orientation != Orientation.Vertical || sc.Width < 400) return;
            try
            {
                int ihtiyac2 = IcerikGenisligi(sc.Panel2) + 20;
                int ihtiyac1 = IcerikGenisligi(sc.Panel1) + 20;
                int asgari = (int)(sc.Width * 0.4);
                if (ihtiyac2 > 40 && sc.Panel2.Width < ihtiyac2)
                {
                    int yeni = Math.Max(asgari, sc.Width - ihtiyac2 - sc.SplitterWidth);
                    if (yeni < sc.SplitterDistance) sc.SplitterDistance = yeni;
                }
                else if (ihtiyac1 > 40 && sc.Panel1.Width < ihtiyac1)
                {
                    int yeni = Math.Min(sc.Width - asgari, ihtiyac1);
                    if (yeni > sc.SplitterDistance) sc.SplitterDistance = yeni;
                }
            }
            catch { /* Panel1MinSize/Panel2MinSize sınırına takılırsa mevcut düzen korunur */ }
        }

        /// <summary>
        /// Sevkiyat Plan sol paneli (panel7): eski yazıya göre piksel piksel sıkıştırılmış 28 nesne,
        /// aynı yükseklikte satırlar ve eşit aralıklarla yeniden dizilir. Panel genişliği değişirse
        /// (menü daraltma, ekran boyutu) yerleşim kendini yeniler. Panel yüksekliği korunur; böylece
        /// alttaki SEVKİYAT/YÜKLEME/AMBAR bölümü daralmaz.
        /// </summary>
        private void SevkiyatPlanPaneliniDuzenle()
        {
            if (panel7 == null) return;
            Action dizle = () =>
            {
                try
                {
                    panel7.SuspendLayout();
                    int x0 = 12, W = Math.Max(360, panel7.ClientSize.Width - 24), etiketG = 104;
                    Font etiketYazi = Tema.Yazi(9f, true);
                    Action<Label, string, int> etiket = (l, metin, y) =>
                    {
                        if (l == null) return;
                        l.AutoSize = false; l.Text = metin; l.Font = etiketYazi; l.ForeColor = Tema.Metin;
                        l.TextAlign = ContentAlignment.MiddleLeft; l.Bounds = new Rectangle(x0, y, etiketG - 6, 24);
                    };
                    Action<Control, Rectangle> yer = (c, r) => { if (c != null) c.Bounds = r; };

                    // Müşteri bilgileri (etiket solda, kutu sağda)
                    etiket(label49, "Müşteri adı", 6);
                    yer(txtMusteriAdi, new Rectangle(x0 + etiketG, 6, W - etiketG, 24));
                    etiket(label51, "Sevk müşterisi", 34);
                    yer(txtSevkMusteri, new Rectangle(x0 + etiketG, 34, W - etiketG, 24));
                    etiket(label58, "Müşteri seç", 62);
                    yer(cmbMusteri, new Rectangle(x0 + etiketG, 62, W - etiketG, 24));

                    // Belge listesi (solda) + belge/palet işlemleri (sağda)
                    int x1 = x0 + 148, w1 = W - 148, yarim = (w1 - 8) / 2;
                    yer(clbBelgeNo, new Rectangle(x0, 94, 140, 178));
                    yer(btnSiparisYenile, new Rectangle(x1, 92, yarim, 34));
                    yer(btnSiparisEkle, new Rectangle(x1 + yarim + 8, 92, w1 - yarim - 8, 34));
                    yer(btnSevkAra, new Rectangle(x1, 128, w1, 34));
                    yer(btnTumBelgeleriSec, new Rectangle(x1, 164, w1, 34));
                    if (label59 != null) { label59.AutoSize = false; label59.Font = etiketYazi; label59.Text = "Palet sayısı"; label59.TextAlign = ContentAlignment.MiddleLeft; label59.Bounds = new Rectangle(x1, 200, yarim, 18); }
                    if (label60 != null) { label60.AutoSize = false; label60.Font = etiketYazi; label60.Text = "Aktif palet"; label60.TextAlign = ContentAlignment.MiddleLeft; label60.Bounds = new Rectangle(x1 + yarim + 8, 200, w1 - yarim - 8, 18); }
                    yer(cmbSevkPaletSayisi, new Rectangle(x1, 218, yarim, 24));
                    yer(cmbAktifPalet, new Rectangle(x1 + yarim + 8, 218, w1 - yarim - 8, 24));
                    yer(btnOncekiPalet, new Rectangle(x1, 246, yarim, 30));
                    yer(btnSonrakiPalet, new Rectangle(x1 + yarim + 8, 246, w1 - yarim - 8, 30));
                    if (btnOncekiPalet != null) btnOncekiPalet.Text = "◀  Önceki";
                    if (btnSonrakiPalet != null) btnSonrakiPalet.Text = "Sonraki  ▶";

                    // Barkod okutma
                    etiket(label56, "Ürün barkodu", 284);
                    yer(txtBarkod, new Rectangle(x0 + etiketG, 284, W - etiketG, 24));
                    yer(btnBarkodKilidi, new Rectangle(x0, 314, W, 34));

                    // Manuel düzeltmeler
                    int adetG = 70, ikili = (W - adetG - 16) / 2;
                    yer(btnManuelEksilt, new Rectangle(x0, 352, ikili, 38));
                    yer(btnManuelEkle, new Rectangle(x0 + ikili + 8, 352, ikili, 38));
                    if (numManuelAdet != null)
                    {
                        numManuelAdet.Font = Tema.Yazi(14f, true);
                        numManuelAdet.TextAlign = HorizontalAlignment.Center;
                        numManuelAdet.Bounds = new Rectangle(x0 + W - adetG, 354, adetG, numManuelAdet.Height);
                    }
                    yer(btnPalettenSil, new Rectangle(x0, 394, (W - 8) / 2, 36));
                    yer(btnSevkTemizle, new Rectangle(x0 + (W - 8) / 2 + 8, 394, W - (W - 8) / 2 - 8, 36));

                    if (panel7.Height < 436) panel7.Height = 436;
                }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Sevkiyat paneli: " + ex.Message); }
                finally { panel7.ResumeLayout(true); }
            };
            dizle();
            int sonGenislik = panel7.Width;
            panel7.Resize += (s, e) => { if (panel7.Width != sonGenislik) { sonGenislik = panel7.Width; dizle(); } };
        }

        /// <summary>
        /// Normal Zarf Yazdırma sağ paneli: tek sütun, etiket üstte, eşit aralıklı. Tablonun üstünde havada
        /// duran "Şablon" etiketi (eski tasarım hatası) şablon seçicisinin üstüne taşınır.
        /// </summary>
        private void NormalZarfPaneliniDuzenle()
        {
            if (panel1 == null || pnlNormal == null) return;
            try
            {
                panel1.Width = 236;
                panel1.AutoScroll = true;
                if (label11 != null && label11.Parent == pnlNormal)
                {
                    label11.Parent = panel1;
                    label11.Text = "Zarf şablonu";
                }

                var nesneler = panel1.Controls.Cast<Control>().Where(c => c != label11).OrderBy(c => c.Top).ToList();
                var ilkCombo = nesneler.OfType<ComboBox>().FirstOrDefault();
                if (label11 != null && ilkCombo != null) nesneler.Insert(nesneler.IndexOf(ilkCombo), label11);

                int x = 14, w = 236 - 28, y = 14;
                Control onceki = null;
                foreach (var c in nesneler)
                {
                    if (c is Label l)
                    {
                        l.AutoSize = false;
                        l.TextAlign = ContentAlignment.MiddleLeft;
                        l.Font = Tema.Yazi(9f, true);
                        l.ForeColor = Tema.Metin;
                        if (onceki != null) y += 4;
                        l.Bounds = new Rectangle(x, y, w, 20);
                        y += 22;
                    }
                    else if (c is Button)
                    {
                        if (onceki is TextBoxBase || onceki is ComboBox) y += 2;
                        c.Bounds = new Rectangle(x - 2, y, w + 4, c.Height >= 50 ? 52 : 40);
                        y += c.Height + 6;
                    }
                    else if (c is ListBox)
                    {
                        y += 6;
                        c.Bounds = new Rectangle(x, y, w, Math.Max(160, c.Height));
                        y += c.Height + 10;
                    }
                    else
                    {
                        c.Location = new Point(x, y);
                        c.Width = w;
                        y += c.Height + 8;
                    }
                    onceki = c;
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Normal zarf paneli: " + ex.Message); }
        }

        /// <summary>
        /// Şablon Tasarım ekranı 8 pt eski yazıya göre çok dar (105-116 px) panellerle tasarlanmıştı.
        /// Araç kutusu, özellikler paneli ve üst çubuk Segoe UI ile rahat okunacak şekilde yeniden dizilir.
        /// Tasarım masasına (pnlDesignSurface) ve orada çizilen baskı nesnelerine dokunulmaz.
        /// </summary>
        private void SablonTasarimEkraniniDuzenle()
        {
            try
            {
                // --- Sol araç kutusu ---
                if (pnlToolbox != null)
                {
                    pnlToolbox.Width = 176;
                    pnlToolbox.Padding = new Padding(0);
                    var butonlar = pnlToolbox.Controls.OfType<Button>().OrderBy(b => b.Top).ToList();
                    int y = 10;
                    Button onceki = null;
                    foreach (var b in butonlar)
                    {
                        // Gruplar arasında (ör. "Seçili öğeyi sil" ile "TEMİZLE", "Yazdır" ile "Dışarıdan Yükle") boşluk bırak
                        if (onceki != null && b.Top - onceki.Bottom > 12) y += 14;
                        b.Bounds = new Rectangle(12, y, 152, 36);
                        b.Font = Tema.Yazi(9.5f, b.Font != null && b.Font.Name.Contains("Semibold"));
                        y += 40;
                        onceki = b;
                    }
                    if (label22 != null) { label22.Text = "Kayıtlı şablonlar"; label22.AutoSize = true; label22.Location = new Point(14, y + 10); }
                    if (lstTemplates != null) lstTemplates.Bounds = new Rectangle(12, y + 32, 152, Math.Max(120, pnlToolbox.Height - (y + 44)));
                    if (lstTemplates != null) lstTemplates.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom;
                }

                // --- Sağ özellikler paneli: etiket üstte, kutu altta, tek sütun ---
                if (pnlProperties != null)
                {
                    pnlProperties.Width = 200;
                    pnlProperties.AutoScroll = true;
                    var nesneler = pnlProperties.Controls.Cast<Control>().OrderBy(c => c.Top).ThenBy(c => c is Label ? 0 : 1).ToList();
                    int y = 10;
                    foreach (var c in nesneler)
                    {
                        if (c is Label l)
                        {
                            l.AutoSize = true;
                            l.TextAlign = ContentAlignment.MiddleLeft;
                            l.Location = new Point(14, y);
                            y += 20;
                        }
                        else
                        {
                            bool buton = c is Button;
                            if (buton && y > 14) y += 6;
                            c.Location = new Point(12, y);
                            c.Width = 172;
                            if (buton) c.Height = 36;
                            y += c.Height + (buton ? 8 : 12);
                        }
                    }
                }

                // --- Üst çubuk: etiket üstte, kutu altta gruplar ---
                if (panel2 != null)
                {
                    panel2.Height = 66;
                    if (label20 != null) label20.Text = "Izgara aralığı (mm)"; // eski tasarımda yanlışlıkla "Sayfa yüksekliği" yazıyordu
                    if (label18 != null) label18.Text = "Sayfa genişliği (mm)";
                    if (label19 != null) label19.Text = "Sayfa yüksekliği (mm)";

                    int x = 16;
                    Action<Label, Control, int> grup = (etiket, kutu, genislik) =>
                    {
                        if (etiket != null) { etiket.AutoSize = true; etiket.Location = new Point(x, 8); }
                        if (kutu != null) { kutu.Location = new Point(x, 30); kutu.Width = genislik; }
                        int g = Math.Max(genislik, etiket != null ? etiket.PreferredWidth : 0);
                        x += g + 22;
                    };
                    grup(label18, txtPageWidthMm, 120);
                    grup(label19, txtPageHeightMm, 120);
                    if (rbPortrait != null && rbLandscape != null)
                    {
                        rbPortrait.AutoSize = true; rbLandscape.AutoSize = true;
                        rbPortrait.Location = new Point(x, 10);
                        rbLandscape.Location = new Point(x, 34);
                        x += Math.Max(rbPortrait.PreferredSize.Width, rbLandscape.PreferredSize.Width) + 22;
                    }
                    if (chkSnapToGrid != null) { chkSnapToGrid.AutoSize = true; chkSnapToGrid.Location = new Point(x, 32); x += chkSnapToGrid.PreferredSize.Width + 22; }
                    grup(label20, numGridMm, 110);

                    // Kağıt biçimi seçicisi ve etiketi kodla ekleniyor; kalan nesneleri sırayla sağa diz
                    var bilinen = new HashSet<Control> { label18, label19, label20, txtPageWidthMm, txtPageHeightMm, rbPortrait, rbLandscape, chkSnapToGrid, numGridMm };
                    var digerleri = panel2.Controls.Cast<Control>().Where(c => !bilinen.Contains(c)).OrderBy(c => c.Left).ToList();
                    var etiketler = digerleri.OfType<Label>().ToList();
                    var kutular = digerleri.Where(c => !(c is Label)).ToList();
                    for (int i = 0; i < kutular.Count; i++)
                        grup(i < etiketler.Count ? etiketler[i] : null, kutular[i], Math.Max(200, kutular[i].Width));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Şablon ekranı düzenlenemedi: " + ex.Message);
            }
        }

        /// <summary>
        /// Sol menü eklendiği için sağa taşan buton satırlarını bulur. Satırdaki butonları metinlerinin
        /// gerektirdiği genişliğe indirir ve sonraki nesneleri sola kaydırır. Yalnızca gerçekten taşan
        /// satırlara dokunur; zaten sığan sayfaların yerleşimi aynen korunur.
        /// </summary>
        private void TasanSatirlariSikistir(Control kap)
        {
            if (kap == null || kap.Name == "pnlDesignSurface" || kap is DataGridView) return;
            if (kap is SplitContainer sc) BolucuyuIcerigeSigdir(sc);
            foreach (Control c in kap.Controls) TasanSatirlariSikistir(c);

            if (!(kap is TabPage || kap is SplitterPanel || kap.GetType() == typeof(Panel))) return;

            // Alta yapışık (Dock=Bottom) bir panel, serbest yerleşimli nesnelerin üstünü örtüyorsa
            // (ör. Yönetim sekmesindeki yedekleme paneli) paneli içeriğin hemen altına taşı.
            if (kap is TabPage)
            {
                foreach (var alt in kap.Controls.Cast<Control>().Where(c => c.Dock == DockStyle.Bottom && c is Panel).ToList())
                {
                    int enAlt = kap.Controls.Cast<Control>().Where(c => c != alt && c.Visible && c.Dock == DockStyle.None).Select(c => c.Bottom).DefaultIfEmpty(0).Max();
                    if (enAlt <= alt.Top) continue;
                    int yukseklik = alt.Height;
                    alt.Dock = DockStyle.None;
                    alt.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                    alt.Bounds = new Rectangle(16, enAlt + 20, Math.Max(400, kap.ClientSize.Width - 32), yukseklik);
                    ((TabPage)kap).AutoScroll = true;
                }
            }
            // Görünür sınır: kapsayıcı kendi sayfasından geniş olabilir (MinimumSize/AutoScroll),
            // bu yüzden sayfanın görünen genişliği esas alınır.
            int sinir = kap.ClientSize.Width;
            int ofset = 0;
            Control x = kap;
            while (x != null && !(x is TabPage)) { ofset += x.Left; x = x.Parent; }
            if (x != null && x != kap) sinir = Math.Min(sinir, x.ClientSize.Width - ofset);
            sinir -= 12;
            if (sinir < 300) return;

            var cocuklar = kap.Controls.Cast<Control>().Where(c => c.Visible && c.Dock == DockStyle.None).ToList();
            var tasan = cocuklar.Where(c => c.Right > sinir && (c.Anchor & AnchorStyles.Right) == 0).OrderBy(c => c.Top).FirstOrDefault();
            int koruma = 0;
            while (tasan != null && koruma++ < 6)
            {
                var serit = cocuklar.Where(c => c.Top < tasan.Bottom && c.Bottom > tasan.Top && (c.Anchor & AnchorStyles.Right) == 0)
                                    .OrderBy(c => c.Left).ToList();
                int kazanc = 0;
                foreach (var c in serit)
                {
                    c.Left -= kazanc;
                    if (c is Button b && !string.IsNullOrEmpty(b.Text))
                    {
                        int gereken = Math.Max(92, TextRenderer.MeasureText(b.Text, b.Font).Width + 36);
                        if (gereken < b.Width) { kazanc += b.Width - gereken; b.Width = gereken; }
                    }
                }
                var yeniTasan = cocuklar.Where(c => c.Right > sinir && (c.Anchor & AnchorStyles.Right) == 0 && c.Top > tasan.Top).OrderBy(c => c.Top).FirstOrDefault();
                tasan = yeniTasan;
            }
        }

        #endregion

        #region 📚 SOL MENÜ

        private void SolMenuyuOlustur()
        {
            solMenu = new CiftTamponluPanel { Dock = DockStyle.Left, Width = MenuGenisligi, BackColor = Tema.Grafit };
            Tema.HaricTut(solMenu);

            // Marka alanı
            var marka = new CiftTamponluPanel { Dock = DockStyle.Top, Height = 76, BackColor = Tema.Grafit };
            Tema.HaricTut(marka);
            marka.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                var logoAlani = new Rectangle(menuDaraltildi ? 16 : 20, 18, 36, 36);
                Image logo = pictureBox1 == null ? null : (pictureBox1.Image ?? pictureBox1.BackgroundImage);
                if (logo != null)
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.DrawImage(logo, logoAlani);
                }
                else
                {
                    using (var yol = Tema.YuvarlakDikdortgen(logoAlani, 9))
                    using (var b = new SolidBrush(Tema.Vurgu)) g.FillPath(b, yol);
                }
                if (!menuDaraltildi)
                {
                    TextRenderer.DrawText(g, "TamgaApp", Tema.Yazi(14f, true), new Point(64, 16), Color.White);
                    TextRenderer.DrawText(g, "Depo & Sevkiyat Otomasyonu", Tema.Yazi(8.5f), new Point(66, 41), Color.FromArgb(148, 163, 184), TextFormatFlags.NoPrefix);
                }
                using (var p = new Pen(Color.FromArgb(31, 41, 55))) g.DrawLine(p, 16, marka.Height - 1, marka.Width - 16, marka.Height - 1);
            };

            solMenuListe = new CiftTamponluPanel { Dock = DockStyle.Fill, BackColor = Tema.Grafit, AutoScroll = true, Padding = new Padding(0, 8, 0, 8) };
            Tema.HaricTut(solMenuListe);

            solMenuAlt = new CiftTamponluPanel { Dock = DockStyle.Bottom, Height = 158, BackColor = Tema.Grafit };
            Tema.HaricTut(solMenuAlt);
            solMenuAlt.Paint += SolMenuAlt_Paint;
            solMenuAlt.Resize += (s, e) => SolMenuAltYerlesimi();

            // Mevcut oturum butonları menünün altına taşınır (tıklama olayları tasarımcıda bağlı kalır).
            if (btnLoginDon != null)
            {
                btnLoginDon.Parent = solMenuAlt;
                btnLoginDon.BackColor = SystemColors.Control; // ikincil (çerçeveli) görünüm
                btnLoginDon.Tag = "|Oturumu Kapat";
                Tema.ButonStil(btnLoginDon);
                btnLoginDon.Font = Tema.Yazi(9.5f, true);
                Tema.HaricTut(btnLoginDon);
            }
            if (btnCikisYap != null)
            {
                btnCikisYap.Parent = solMenuAlt;
                btnCikisYap.BackColor = Color.Red; // tehlike rengi
                btnCikisYap.Tag = "|Çıkış";
                Tema.ButonStil(btnCikisYap);
                btnCikisYap.Font = Tema.Yazi(9.5f, true);
                Tema.HaricTut(btnCikisYap);
            }

            solMenu.Controls.Add(solMenuListe);
            solMenu.Controls.Add(marka);
            solMenu.Controls.Add(solMenuAlt);
            solMenu.Paint += (s, e) => { using (var p = new Pen(Color.FromArgb(31, 41, 55))) e.Graphics.DrawLine(p, solMenu.Width - 1, 0, solMenu.Width - 1, solMenu.Height); };
        }

        private void SolMenuAlt_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var p = new Pen(Color.FromArgb(31, 41, 55))) g.DrawLine(p, 16, 0, solMenuAlt.Width - 16, 0);

            string ad = string.IsNullOrWhiteSpace(AktifKullaniciAdi) ? "Kullanıcı" : AktifKullaniciAdi;
            string bas = ad.Substring(0, 1).ToUpper(new System.Globalization.CultureInfo("tr-TR"));
            var avatar = new Rectangle(menuDaraltildi ? 16 : 18, 14, 36, 36);
            using (var b = new SolidBrush(Tema.Vurgu)) g.FillEllipse(b, avatar);
            TextRenderer.DrawText(g, bas, Tema.Yazi(12f, true), avatar, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            if (!menuDaraltildi)
            {
                var adAlani = new Rectangle(62, 14, solMenuAlt.Width - 74, 20);
                TextRenderer.DrawText(g, ad, Tema.Yazi(10f, true), adAlani, Color.White, TextFormatFlags.EndEllipsis | TextFormatFlags.Left);
                string rol = AktifOturumTamYetkiliMi() ? "Tam yetkili yönetici" : "Kullanıcı";
                TextRenderer.DrawText(g, rol, Tema.Yazi(8.5f), new Point(62, 34), Color.FromArgb(148, 163, 184));
            }
        }

        private void SolMenuAltYerlesimi()
        {
            int w = solMenuAlt.Width;
            Button[] butonlar = { btnLoginDon, btnCikisYap };
            int y = 62;
            foreach (var b in butonlar)
            {
                if (b == null) continue;
                string[] parca = (b.Tag as string ?? "|").Split('|');
                if (menuDaraltildi)
                {
                    b.Bounds = new Rectangle(14, y, w - 28, 38);
                    b.Text = parca[0];
                    b.Font = Tema.Ikon(12f);
                }
                else
                {
                    b.Bounds = new Rectangle(16, y, w - 32, 38);
                    b.Text = parca.Length > 1 ? parca[1] : b.Text;
                    b.Font = Tema.Yazi(9.5f, true);
                }
                y += 44;
            }
            solMenuAlt.Invalidate();
        }

        private void MenuyuDoldur()
        {
            if (solMenuListe == null) return;
            solMenuListe.SuspendLayout();
            foreach (Control c in solMenuListe.Controls.Cast<Control>().ToList()) { solMenuListe.Controls.Remove(c); c.Dispose(); }
            menuOgeleri.Clear();

            var sayfalar = tabControl1.TabPages.Cast<TabPage>()
                .Select(p => new { Sayfa = p, Ad = SekmeAdiTemizle(p.Text), Tanim = MenuTanimiBul(SekmeAdiTemizle(p.Text)) })
                .ToList();

            int y = 8;
            foreach (string grup in MenuGrupSirasi)
            {
                var gruptakiler = sayfalar.Where(x => x.Tanim.Grup == grup).ToList();
                if (gruptakiler.Count == 0) continue;

                if (!menuDaraltildi)
                {
                    var baslik = new Label
                    {
                        Text = grup,
                        Font = Tema.Yazi(8f, true),
                        ForeColor = Color.FromArgb(100, 116, 139),
                        BackColor = Tema.Grafit,
                        AutoSize = false,
                        Bounds = new Rectangle(26, y + 6, MenuGenisligi - 40, 20),
                        Tag = Tema.TemaYok
                    };
                    solMenuListe.Controls.Add(baslik);
                    y += 28;
                }
                else y += 10;

                foreach (var x in gruptakiler)
                {
                    var oge = new MenuOgesi
                    {
                        Ikon = x.Tanim.Ikon,
                        Metin = x.Ad,
                        Sayfa = x.Sayfa,
                        Daraltilmis = menuDaraltildi,
                        Bounds = new Rectangle(12, y, solMenu.Width - 24, 40)
                    };
                    var hedef = x.Sayfa;
                    oge.Click += (s, e) => SayfayaGit(hedef);
                    solMenuListe.Controls.Add(oge);
                    menuOgeleri.Add(oge);
                    if (menuDaraltildi) MenuIpucu.SetToolTip(oge, x.Ad);
                    y += 42;
                }
            }
            solMenuListe.ResumeLayout(true);
            MenuSeciminiGuncelle();
            SolMenuAltYerlesimi();
            AnaPanelKutucuklariniDoldur();
        }

        private readonly ToolTip MenuIpucu = new ToolTip { InitialDelay = 200 };

        private void SayfayaGit(TabPage sayfa)
        {
            if (sayfa == null || !tabControl1.TabPages.Contains(sayfa)) return;
            tabControl1.SelectedTab = sayfa; // Selecting kilidi (askıdaki işlem) burada da devrededir
        }

        private void MenuSeciminiGuncelle()
        {
            TabPage secili = tabControl1.SelectedTab;
            foreach (var o in menuOgeleri) o.Secili = o.Sayfa == secili;
            if (secili != null && lblSayfaBaslik != null)
            {
                string ad = SekmeAdiTemizle(secili.Text);
                lblSayfaBaslik.Text = ad;
                var tanim = MenuTanimiBul(ad);
                lblSayfaGrup.Text = "TamgaApp  ›  " + (tanim.Grup == "DİĞER" ? "Modüller" : tanim.Grup.Substring(0, 1) + tanim.Grup.Substring(1).ToLower(new System.Globalization.CultureInfo("tr-TR"))) + (string.IsNullOrEmpty(tanim.Aciklama) ? "" : "  ·  " + tanim.Aciklama);
            }
        }

        private void MenuyuDaralt(bool daralt)
        {
            menuDaraltildi = daralt;
            solMenu.Width = daralt ? MenuDarGenislik : MenuGenisligi;
            foreach (Control c in solMenu.Controls) c.Invalidate();
            MenuyuDoldur();
        }

        #endregion

        #region 🔝 ÜST BAŞLIK ÇUBUĞU

        private void UstCubuguOlustur()
        {
            ustCubuk = new CiftTamponluPanel { Dock = DockStyle.Top, Height = 68, BackColor = Tema.Kart };
            Tema.HaricTut(ustCubuk);
            ustCubuk.Paint += (s, e) => { using (var p = new Pen(Tema.Cizgi)) e.Graphics.DrawLine(p, 0, ustCubuk.Height - 1, ustCubuk.Width, ustCubuk.Height - 1); };

            var btnMenu = new Button { Text = "", Font = Tema.Ikon(13f), Bounds = new Rectangle(16, 15, 38, 38), BackColor = SystemColors.Control };
            Tema.ButonStil(btnMenu);
            btnMenu.Font = Tema.Ikon(13f);
            Tema.HaricTut(btnMenu);
            btnMenu.Click += (s, e) => MenuyuDaralt(!menuDaraltildi);
            MenuIpucu.SetToolTip(btnMenu, "Menüyü daralt / genişlet");

            lblSayfaBaslik = new Label { AutoSize = true, Location = new Point(68, 10), Font = Tema.Yazi(15f, true), ForeColor = Tema.Metin, BackColor = Color.Transparent, Tag = Tema.TemaYok };
            lblSayfaGrup = new Label { AutoSize = true, Location = new Point(70, 40), Font = Tema.Yazi(9f), ForeColor = Tema.SolukMetin, BackColor = Color.Transparent, Tag = Tema.TemaYok };

            lblUstSaat = new Label { AutoSize = false, Size = new Size(320, 40), Font = Tema.Yazi(10f), ForeColor = Tema.SolukMetin, BackColor = Color.Transparent, TextAlign = ContentAlignment.MiddleRight, Anchor = AnchorStyles.Top | AnchorStyles.Right, Tag = Tema.TemaYok };
            ustCubuk.Resize += (s, e) => lblUstSaat.Location = new Point(ustCubuk.Width - lblUstSaat.Width - 24, 14);

            ustCubuk.Controls.Add(btnMenu);
            ustCubuk.Controls.Add(lblSayfaBaslik);
            ustCubuk.Controls.Add(lblSayfaGrup);
            ustCubuk.Controls.Add(lblUstSaat);

            UstSaatiGuncelle();
            timerSaat.Tick += (s, e) => UstSaatiGuncelle();
        }

        private void UstSaatiGuncelle()
        {
            if (lblUstSaat == null) return;
            DateTime simdi = DateTime.Now;
            lblUstSaat.Text = simdi.ToString("dd MMMM yyyy, dddd") + "    " + simdi.ToString("HH:mm");
        }

        #endregion

        #region 🏠 ANA PANEL (KONTROL MERKEZİ)

        private KartPanel anaKahraman;
        private FlowLayoutPanel anaKutucuklar;
        private Label lblHizliErisim;

        private void AnaPaneliYenidenTasarla()
        {
            if (tabPage1 == null) return;
            tabPage1.BackColor = Tema.Zemin;
            tabPage1.AutoScroll = true;
            tabPage1.Padding = new Padding(0);

            anaKahraman = new KartPanel
            {
                KartRengi = Tema.Grafit,
                GecisRengi = Color.FromArgb(15, 94, 84),
                KenarRengi = Color.Empty,
                Yaricap = 18,
                Location = new Point(32, 28),
                Size = new Size(Math.Max(600, tabPage1.ClientSize.Width - 64), 196)
            };
            Tema.HaricTut(anaKahraman);

            if (pictureBox1 != null)
            {
                pictureBox1.Parent = anaKahraman;
                pictureBox1.BackColor = Color.Transparent;
                pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
                pictureBox1.Bounds = new Rectangle(28, 28, 140, 140);
                pictureBox1.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            }
            if (lblKarsilama != null)
            {
                lblKarsilama.Parent = anaKahraman;
                lblKarsilama.AutoSize = true;
                lblKarsilama.Font = Tema.Yazi(24f, true);
                lblKarsilama.ForeColor = Color.White;
                lblKarsilama.BackColor = Color.Transparent;
                lblKarsilama.Location = new Point(192, 46);
                lblKarsilama.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                lblKarsilama.Text = "Hoş geldin, " + (string.IsNullOrWhiteSpace(AktifKullaniciAdi) ? "" : AktifKullaniciAdi);
            }
            var lblAlt = new Label
            {
                AutoSize = true,
                Text = "Bugün hangi işlemle başlamak istersiniz? Aşağıdaki kısayollardan ya da soldaki menüden ilerleyebilirsiniz.",
                Font = Tema.Yazi(10.5f),
                ForeColor = Color.FromArgb(203, 213, 225),
                BackColor = Color.Transparent,
                Location = new Point(196, 98),
                Tag = Tema.TemaYok
            };
            anaKahraman.Controls.Add(lblAlt);
            if (lblTakvim != null)
            {
                lblTakvim.Parent = anaKahraman;
                lblTakvim.AutoSize = true;
                lblTakvim.Font = Tema.Yazi(11f, true);
                lblTakvim.ForeColor = Color.FromArgb(153, 246, 228);
                lblTakvim.BackColor = Color.Transparent;
                lblTakvim.Location = new Point(196, 132);
                lblTakvim.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            }
            if (lblSaat != null)
            {
                lblSaat.Parent = anaKahraman;
                lblSaat.AutoSize = false;
                lblSaat.Size = new Size(300, 80);
                lblSaat.TextAlign = ContentAlignment.MiddleRight;
                lblSaat.Font = new Font("Segoe UI Light", 40f);
                lblSaat.ForeColor = Color.White;
                lblSaat.BackColor = Color.Transparent;
                lblSaat.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                lblSaat.Text = DateTime.Now.ToString("HH:mm:ss");
            }

            lblHizliErisim = new Label { Text = "Hızlı Erişim", AutoSize = true, Font = Tema.Yazi(13f, true), ForeColor = Tema.Metin, BackColor = Color.Transparent, Location = new Point(34, 248), Tag = Tema.TemaYok };

            anaKutucuklar = new FlowLayoutPanel
            {
                Location = new Point(26, 284),
                Size = new Size(Math.Max(600, tabPage1.ClientSize.Width - 52), Math.Max(200, tabPage1.ClientSize.Height - 300)),
                BackColor = Tema.Zemin,
                AutoScroll = true,
                WrapContents = true,
                Padding = new Padding(0)
            };
            Tema.HaricTut(anaKutucuklar);

            tabPage1.Controls.Add(anaKahraman);
            tabPage1.Controls.Add(lblHizliErisim);
            tabPage1.Controls.Add(anaKutucuklar);

            tabPage1.Resize += (s, e) => AnaPanelYerlesimi();
            AnaPanelYerlesimi();
        }

        private void AnaPanelYerlesimi()
        {
            if (anaKahraman == null) return;
            int w = Math.Max(600, tabPage1.ClientSize.Width - 64);
            anaKahraman.Width = w;
            if (lblSaat != null) lblSaat.Location = new Point(w - lblSaat.Width - 36, 58);
            int kutuUst = 284;
            if (anaDurumKartlari != null)
            {
                anaDurumKartlari.Width = Math.Max(600, tabPage1.ClientSize.Width - 52);
                lblHizliErisim.Top = 414;
                kutuUst = 448;
            }
            anaKutucuklar.Location = new Point(26, kutuUst);
            anaKutucuklar.Size = new Size(Math.Max(600, tabPage1.ClientSize.Width - 52), Math.Max(200, tabPage1.ClientSize.Height - kutuUst - 16));
        }

        private void AnaPanelKutucuklariniDoldur()
        {
            if (anaKutucuklar == null) return;
            anaKutucuklar.SuspendLayout();
            foreach (Control c in anaKutucuklar.Controls.Cast<Control>().ToList()) { anaKutucuklar.Controls.Remove(c); c.Dispose(); }

            foreach (TabPage p in tabControl1.TabPages)
            {
                if (p == tabPage1) continue;
                string ad = SekmeAdiTemizle(p.Text);
                var tanim = MenuTanimiBul(ad);
                var hedef = p;
                var kutu = new HizliErisimKutusu { Ikon = tanim.Ikon, Baslik = ad, Aciklama = tanim.Aciklama, Margin = new Padding(6) };
                kutu.Click += (s, e) => SayfayaGit(hedef);
                anaKutucuklar.Controls.Add(kutu);
            }
            anaKutucuklar.ResumeLayout(true);
        }

        #endregion

        #region 💊 ALT SEKMELER → HAP SEÇİCİ

        /// <summary>
        /// İç içe TabControl'lerin (Ayarlar, Sevkiyat Plan alt sekmeleri) gri başlık şeridini gizler ve
        /// yerine web tarzı "hap" butonlardan oluşan bir seçici koyar.
        /// </summary>
        private void AltSekmeleriHapaCevir(TabControl tc, int yukseklik)
        {
            if (tc == null || tc.Parent == null) return;
            Tema.BasliklariGizle(tc);

            var serit = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = yukseklik,
                BackColor = Tema.Zemin,
                WrapContents = false,
                AutoScroll = false,
                Padding = new Padding(yukseklik > 48 ? 20 : 8, yukseklik > 48 ? 12 : 8, 8, 4)
            };
            Tema.HaricTut(serit);

            Control ebeveyn = tc.Parent;
            ebeveyn.Controls.Add(serit);
            // Dock sırası: şerit, TabControl'den hemen önce yerleşsin (diğer üst bileşenlerin altında kalsın).
            ebeveyn.Controls.SetChildIndex(serit, ebeveyn.Controls.GetChildIndex(tc) + 1);

            Action doldur = null;
            doldur = () =>
            {
                serit.SuspendLayout();
                foreach (Control c in serit.Controls.Cast<Control>().ToList()) { serit.Controls.Remove(c); c.Dispose(); }
                bool kucuk = yukseklik <= 48;
                foreach (TabPage p in tc.TabPages)
                {
                    var hedef = p;
                    var hap = new HapButon { Metin = SekmeAdiTemizle(p.Text), Secili = tc.SelectedTab == p, Kucuk = kucuk, Margin = new Padding(0, 0, 6, 0) };
                    hap.Click += (s, e) => { tc.SelectedTab = hedef; };
                    serit.Controls.Add(hap);
                }
                serit.ResumeLayout(true);
            };
            tc.SelectedIndexChanged += (s, e) =>
            {
                foreach (HapButon h in serit.Controls.OfType<HapButon>())
                    h.Secili = tc.SelectedTab != null && h.Metin == SekmeAdiTemizle(tc.SelectedTab.Text);
            };
            tc.ControlAdded += (s, e) => doldur();
            tc.ControlRemoved += (s, e) => BeginInvoke(doldur);
            doldur();
        }

        #endregion

        #region 🧩 ÖZEL ÇİZİMLİ KONTROLLER

        private class CiftTamponluPanel : Panel
        {
            public CiftTamponluPanel()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            }
        }

        /// <summary>Sol menüdeki tek bir satır: ikon + metin; seçiliyken sol vurgu çizgisi.</summary>
        private class MenuOgesi : Control
        {
            public string Ikon;
            public string Metin;
            public TabPage Sayfa;
            public bool Daraltilmis;
            private bool secili, ustunde;
            public bool Secili { get { return secili; } set { if (secili != value) { secili = value; Invalidate(); } } }

            public MenuOgesi()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
                Cursor = Cursors.Hand;
                BackColor = Tema.Grafit;
                Tag = Tema.TemaYok;
            }

            protected override void OnMouseEnter(EventArgs e) { ustunde = true; Invalidate(); base.OnMouseEnter(e); }
            protected override void OnMouseLeave(EventArgs e) { ustunde = false; Invalidate(); base.OnMouseLeave(e); }

            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.Clear(Tema.Grafit);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                if (secili || ustunde)
                {
                    Color dolgu = secili ? Color.FromArgb(19, 78, 74) : Color.FromArgb(31, 41, 55);
                    using (var yol = Tema.YuvarlakDikdortgen(new RectangleF(0, 0, Width - 1, Height - 1), 9))
                    using (var b = new SolidBrush(dolgu)) g.FillPath(b, yol);
                }
                if (secili)
                {
                    using (var yol = Tema.YuvarlakDikdortgen(new RectangleF(0, 9, 4, Height - 18), 2))
                    using (var b = new SolidBrush(Color.FromArgb(45, 212, 191))) g.FillPath(b, yol);
                }
                Color yazi = secili ? Color.White : (ustunde ? Color.FromArgb(226, 232, 240) : Color.FromArgb(148, 163, 184));
                Color ikonRengi = secili ? Color.FromArgb(94, 234, 212) : yazi;
                if (Daraltilmis)
                {
                    TextRenderer.DrawText(g, Ikon, Tema.Ikon(13f), new Rectangle(0, 0, Width, Height), ikonRengi, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                    return;
                }
                TextRenderer.DrawText(g, Ikon, Tema.Ikon(12f), new Rectangle(14, 0, 24, Height), ikonRengi, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                TextRenderer.DrawText(g, Metin, Tema.Yazi(10f, secili), new Rectangle(46, 0, Width - 52, Height), yazi, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }
        }

        /// <summary>Alt sekme seçicisindeki hap şeklinde buton.</summary>
        private class HapButon : Control
        {
            public string Metin;
            public bool Kucuk;
            private bool secili, ustunde;
            public bool Secili { get { return secili; } set { if (secili != value) { secili = value; Invalidate(); } } }

            public HapButon()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
                Cursor = Cursors.Hand;
                Tag = Tema.TemaYok;
            }

            protected override void OnCreateControl()
            {
                base.OnCreateControl();
                Font f = Tema.Yazi(Kucuk ? 9f : 10f, true);
                Size olcu = TextRenderer.MeasureText(Metin ?? "", f);
                Size = new Size(olcu.Width + (Kucuk ? 22 : 32), Kucuk ? 28 : 34);
            }

            protected override void OnMouseEnter(EventArgs e) { ustunde = true; Invalidate(); base.OnMouseEnter(e); }
            protected override void OnMouseLeave(EventArgs e) { ustunde = false; Invalidate(); base.OnMouseLeave(e); }

            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.Clear(Tema.GercekArkaPlan(Parent));
                g.SmoothingMode = SmoothingMode.AntiAlias;
                Color dolgu = secili ? Tema.Vurgu : (ustunde ? Color.White : Color.Transparent);
                Color yazi = secili ? Color.White : (ustunde ? Tema.VurguKoyu : Tema.SolukMetin);
                using (var yol = Tema.YuvarlakDikdortgen(new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f), Height / 2f))
                {
                    if (dolgu.A > 0) using (var b = new SolidBrush(dolgu)) g.FillPath(b, yol);
                    if (ustunde && !secili) using (var p = new Pen(Tema.Cizgi)) g.DrawPath(p, yol);
                }
                TextRenderer.DrawText(g, Metin, Tema.Yazi(Kucuk ? 9f : 10f, true), new Rectangle(0, 0, Width, Height), yazi, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }

        /// <summary>Ana Panel'deki hızlı erişim kartı.</summary>
        private class HizliErisimKutusu : Control
        {
            public string Ikon, Baslik, Aciklama;
            private bool ustunde;

            public HizliErisimKutusu()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
                Size = new Size(264, 128);
                Cursor = Cursors.Hand;
                Tag = Tema.TemaYok;
            }

            protected override void OnMouseEnter(EventArgs e) { ustunde = true; Invalidate(); base.OnMouseEnter(e); }
            protected override void OnMouseLeave(EventArgs e) { ustunde = false; Invalidate(); base.OnMouseLeave(e); }

            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.Clear(Tema.Zemin);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                var kart = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
                if (ustunde)
                {
                    using (var golge = Tema.YuvarlakDikdortgen(new RectangleF(2, 4, Width - 4, Height - 4), 14))
                    using (var b = new SolidBrush(Color.FromArgb(18, 15, 23, 42))) g.FillPath(b, golge);
                }
                using (var yol = Tema.YuvarlakDikdortgen(kart, 14))
                {
                    using (var b = new SolidBrush(Tema.Kart)) g.FillPath(b, yol);
                    using (var p = new Pen(ustunde ? Tema.Vurgu : Tema.Cizgi, ustunde ? 1.5f : 1f)) g.DrawPath(p, yol);
                }
                var ikonKutusu = new RectangleF(20, 20, 42, 42);
                using (var yol = Tema.YuvarlakDikdortgen(ikonKutusu, 11))
                using (var b = new SolidBrush(ustunde ? Tema.Vurgu : Tema.VurguAcik)) g.FillPath(b, yol);
                TextRenderer.DrawText(g, Ikon, Tema.Ikon(15f), Rectangle.Round(ikonKutusu), ustunde ? Color.White : Tema.VurguKoyu, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                TextRenderer.DrawText(g, "", Tema.Ikon(9f), new Rectangle(Width - 40, 20, 20, 20), ustunde ? Tema.Vurgu : Tema.Cizgi, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                TextRenderer.DrawText(g, Baslik, Tema.Yazi(11f, true), new Rectangle(20, 72, Width - 36, 22), Tema.Metin, TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
                TextRenderer.DrawText(g, Aciklama, Tema.Yazi(9f), new Rectangle(20, 96, Width - 36, 20), Tema.SolukMetin, TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
            }
        }

        #endregion
    }
}
