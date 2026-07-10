using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using static TamgaApp.DataAccess;
using AutoUpdaterDotNET;
using System.Data;
using System.Net.Http;

namespace TamgaApp
{
    public partial class MainForm : Form
    {
        #region 🌐 01. GLOBAL HAFIZA VE ÇEKİRDEK DEĞİŞKENLER

        #region 🏠 01.1 EV MODU (GELİŞTİRİCİ ALANI)
        // ------------------------------------------------------------------------
        // DİKKAT: İş yerinde SQL'e bağlanamadığınız zamanlarda sistemi sahte
        // verilerle test etmek için bu şalteri 'true' yapın. 
        // Gerçek SQL veritabanına bağlanmak için mutlaka 'false' yapmalısınız!
        // ------------------------------------------------------------------------
        public static bool EvModuAktif = false; // TODO: Canlı ortamda FALSE kalmalı!
        #endregion

        #region 🔐 01.2 KULLANICI VE YETKİLENDİRME
        // Sisteme başarılı şekilde giriş yapan aktif kullanıcının bilgilerini ve
        // erişebileceği sekme/modül yetkilerini tutan global güvenlik değişkenleri.
        public static string AktifKullaniciAdi = "";
        public static string AktifYetkiler = "";
        #endregion

        #region 🎨 01.3 TASARIM MOTORU (SÜRÜKLE-BIRAK) DEĞİŞKENLERİ
        // Kağıt/Zarf üzerindeki görsel nesneleri (etiket, dinamik alan, resim) yöneten motor
        private List<DesignItem> designItems = new List<DesignItem>(); // Tasarım ekranındaki tüm görsel nesnelerin RAM'deki listesi
        private List<Control> selectedControls = new List<Control>();  // Klavyeden CTRL tuşu ile seçilen çoklu nesneleri hafızada tutar

        // Dinamik Fare Hareketleri ve Boyutlandırma Takibi
        private bool isDragging = false;                               // Seçili nesne sürükleniyor mu?
        private bool isResizing = false;                               // Seçili nesne kenarından çekilip büyütülüyor/küçültülüyor mu?
        private string resizeDir = "";                                 // Boyutlandırmanın yönü (Örn: "WE" yatay sündürme, "NS" dikey)
        private Point dragStart;                                       // Sürükleme veya sündürme işleminin başladığı X,Y koordinatı
        private Control draggingControl;                               // O an farenin ucunda tutulan aktif kutucuk
        private DesignItem selectedDesignItem;                         // Sağ taraftaki "Özellikler" panelinde ayarları gösterilen aktif nesne

        // Tasarım Arayüzü (Masa ve Kağıt)
        private Panel pnlWorkspace;                                    // Arka plandaki devasa gri çalışma masası (Nesnelerin dışarı taşmasını önler)
        private ComboBox cmbPaperSize;                                 // Üst menüdeki kağıt/zarf boyutu seçici (DL Zarf, A4, Özel Boyut vb.)
        #endregion

        #region 🖨️ 01.4 YAZDIRMA, PDF VE YAZICI SPOOLER YÖNETİMİ
        // Tekli ve çoklu yazdırma işlemlerini sıraya koyan, yazıcı eşleştirmelerini tutan değişkenler
        private Firma currentPreviewFirma;                             // Ekranda tekli önizlemesi yapılan (aktif) firma verisi
        private List<Firma> batchFirms;                                // Çoklu zarf yazdırma işlemine sokulan firmaların toplu sırası
        private int batchIndex;                                        // Çoklu yazdırmada anlık olarak kaçıncı kağıdın/firmanın yazdırıldığını tutar

        // Yazıcı Hafızası
        private Dictionary<string, string> printerMappings = new Dictionary<string, string>(); // Hangi ekranın hangi yazıcıyı varsayılan kullanacağını tutar
        private const string PrinterSettingsFile = "printer_settings.json";                    // Yazıcı eşleştirmelerinin diske kaydedildiği JSON dosyasının adı
        private PrintDocument pdUretim;                                // Üretim listesini (A4 kağıda) döken yazdırma motoru
        #endregion

        #region 📦 01.5 SEVKİYAT SİSTEMİ VE KALICI HAFIZA (GHOST MODU)
        // WMS ve Sipariş takip ekranının veritabanı değişkenleri
        private DataTable dtTumSiparisler = new DataTable();           // SQL'den çekilen tüm siparişlerin geçici olarak tutulduğu RAM deposu

        // 👻 GHOST MODU KARA LİSTESİ (Kalıcı Hafıza)
        // Tamamı veya bir kısmı sevk edilip işlemleri biten benzersiz belge (sipariş) numaralarını tutar.
        // Program açıldığında arka plandaki '.txt' dosyasından okunarak doldurulur.
        // Böylece program kapansa bile, kapanmış bir sipariş SQL'den gelse dahi ekranda gösterilmez.
        public static List<string> TamamlananBelgeNolar = new List<string>();
        #endregion

        #region 🧩 01.6 VERİ MODELLERİ (JSON SERİLEŞTİRME SINIFLARI)
        // Tasarım şablonlarının ve içindeki görsel nesnelerin diske kaydedilebilmesi 
        // (.json dosyası olabilmesi) için gereken kalıp sınıflar.

        public class YarimSevkiyatHafizasi
        {
            public string MusteriAdi { get; set; }
            public string BelgeNo { get; set; }
            public string SevkMusteri { get; set; }
            public int PaletSayisi { get; set; }
            public DateTime KayitTarihi { get; set; }
            public Dictionary<string, int> AnaOkutulanlar { get; set; } = new Dictionary<string, int>();
            public Dictionary<int, Dictionary<int, string>> PaletMatrisiDurumu { get; set; } = new Dictionary<int, Dictionary<int, string>>();
        }

        [Serializable]
        public class DesignItem
        {
            public string Id { get; set; } = Guid.NewGuid().ToString(); // Nesnenin benzersiz kimliği
            public string Type { get; set; } // Kutunun türü: "Label" (Sabit), "Field" (Dinamik Veri), "Frame" (Çerçeve), "Image" (Resim)
            public string Text { get; set; } // İçindeki sabit metin (Resim ise dosya yolu tutulur)
            public string PlaceholderKey { get; set; } // Dinamik bir alansa veritabanı anahtarı (Örn: FirmaAdi, Adres, Telefon1)

            // Milimetrik Koordinatlar ve Boyutlar (Yazıcıda milimetrik, hatasız çıkması için Px yerine Mm kullanıyoruz)
            public float Xmm { get; set; }
            public float Ymm { get; set; }
            public float Wmm { get; set; }
            public float Hmm { get; set; }

            // Tipografi ve Görsel Stil Ayarları
            public string FontName { get; set; } = "Times New Roman";
            public float FontSizePt { get; set; } = 12f;
            public FontStyle FontStyle { get; set; } = FontStyle.Regular;
            public string ColorName { get; set; } = "#000000";    // HEX renk kodu (Siyah)
            public string Alignment { get; set; } = "Center";     // Metin Hizalama: Left, Center, Right
            public int Rotation { get; set; } = 0;                // Metnin kağıt üzerindeki dönüş açısı (Dikey metinler için 90, 270 vb.)
        }

        [Serializable]
        private class TemplateFile
        {
            public string TemplateName { get; set; }          // JSON Şablonunun adı
            public float PageWidthMm { get; set; }            // Kağıdın milimetrik genişliği
            public float PageHeightMm { get; set; }           // Kağıdın milimetrik yüksekliği
            public string Orientation { get; set; }           // Kağıt yönü (Portrait: Dikey, Landscape: Yatay)
            public int Version { get; set; }                  // Şablon versiyonu (Gelecekte eski şablonları uyarlamak için)
            public DateTime CreatedAt { get; set; }           // Şablonun tasarlanma tarihi
            public List<DesignItem> DesignItems { get; set; } // Şablonun üzerindeki nesnelerin (DesignItem) barındırdığı liste
        }
        #endregion

        #endregion

        // =========================================================================================

        #region ⚙️ 02. BAŞLANGIÇ AYARLARI VE FORM YÜKLEME (INIT)

        public MainForm()
        {
            InitializeComponent();
            this.Load += MainForm_Load;
            this.FormClosing += MainForm_FormClosing; // 🚀 Çıkışta yumuşak kapanış (fade-out) animasyonunu tetikler
            WireUiEvents();                           // Tüm butonların ve kutuların tıklanma olaylarını (event) sisteme bağlar
        }

        #region 🚀 02.1 FORM LOAD (AÇILIŞ MOTORU)
        private void MainForm_Load(object sender, EventArgs e)
        {

            // 🛡️ SÜRÜM GÜNCELLEME ZIRHI: Eski sürümdeki SQL ve Yazıcı ayarlarını yeni sürüme göç ettirir!
            if (!Properties.Settings.Default.AyarlarTasindi)
            {
                Properties.Settings.Default.Upgrade(); // Eski sürümdeki ayarları bul ve buraya kopyala
                Properties.Settings.Default.AyarlarTasindi = true; // Taşıma işlemi bitti, şalteri kapat
                Properties.Settings.Default.Save(); // Yeni durumu kaydet
            }

            // Giriş yapan aktif kullanıcının adını programın üst başlığına yazdırır
            this.Text = $"TamgaApp Otomasyon - Aktif Kullanıcı: {AktifKullaniciAdi}";

            // --- YETKİ KUTUSUNU OTOMATİK DOLDURAN MOTOR ---
            // Programdaki tüm sekmeleri tarar ve "Yönetim" dışındaki sekmeleri Kullanıcı Ekleme sayfasındaki listeye doldurur.
            clbYetkiler.Items.Clear();
            foreach (TabPage sekme in tabControl1.TabPages)
            {
                clbYetkiler.Items.Add(sekme.Text);

                foreach (Control ctrl in sekme.Controls)
                {
                    if (ctrl is TabControl altTabControl)
                    {
                        foreach (TabPage altSekme in altTabControl.TabPages)
                        {
                            if (altSekme.Text != "Yönetim")
                            {
                                clbYetkiler.Items.Add(altSekme.Text);
                            }
                        }
                    }
                }
            }


            // 🛡️ ULTRA GÜVENLİ GOD MODE DESTEKLİ YETKİ KALKANI
            // Giriş yapan kullanıcı "TamgaApp" (Ana Admin) değilse ve yetkisi "Sınırsız" değilse çalışır.
            // Kullanıcının yetkisi olmayan sekmeleri program açılırken fiziksel olarak siler/gizler.
            if (AktifKullaniciAdi != "TamgaApp" && AktifYetkiler != "Sınırsız")
            {
                var yetkiListesi = AktifYetkiler.Split(',').Select(y => y.Trim()).ToList();

                foreach (TabPage sekme in tabControl1.TabPages.Cast<TabPage>().ToList())
                {
                    if (!yetkiListesi.Contains(sekme.Text))
                    {
                        tabControl1.TabPages.Remove(sekme);
                    }
                    else
                    {
                        foreach (Control ctrl in sekme.Controls)
                        {
                            if (ctrl is TabControl altTabControl)
                            {
                                foreach (TabPage altSekme in altTabControl.TabPages.Cast<TabPage>().ToList())
                                {
                                    // "Yönetim" sekmesi her halükarda normal kullanıcılara kapalıdır.
                                    if (altSekme.Text == "Yönetim" || !yetkiListesi.Contains(altSekme.Text))
                                    {
                                        altTabControl.TabPages.Remove(altSekme);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // 👻 GHOST MODU: KALICI HAFIZAYI YÜKLE
            // Daha önceden 'Tam Sevk' veya 'Kısmi Sevk' ile tamamen kapatılmış belgeleri 
            // yerel dosyadan (txt) okur ve SQL'den gelse bile bir daha ekrana yansıtmaz.
            KaraListeyiYukle();

            // Diğer başlangıç yüklemeleri
            LoadFirmalar();
            LoadTemplateList();
            InitializePrinterSettingsTab();
            AmbarSisteminiHazirla();
            SayimSisteminiHazirla();
            YaziciAyarlariniYukle();

            // Tasarım Ekranı Özellikler Paneli (Properties) Varsayılan Ayarları
            numPropFontSize.Minimum = 6;
            numPropFontSize.Value = 12;

            cmbPropFont.Items.Clear();
            foreach (FontFamily f in FontFamily.Families) cmbPropFont.Items.Add(f.Name);
            if (cmbPropFont.Items.Count > 0) cmbPropFont.SelectedItem = "Arial";

            cmbPropPlaceholder.Items.Clear();
            cmbPropPlaceholder.Items.AddRange(new string[] { "FirmaAdi", "Adres", "Il", "Telefon1", "Telefon2" });
            cmbPropPlaceholder.SelectedIndex = 0;

            cmbPropRotation.Items.Clear();
            cmbPropRotation.Items.AddRange(new object[] { "0", "90", "180", "270" });
            cmbPropRotation.SelectedIndex = 0;

            cmbPropAlignment.Items.Clear();
            cmbPropAlignment.Items.AddRange(new string[] { "Left", "Center", "Right" });
            cmbPropAlignment.SelectedIndex = 1;

            // Tasarım Masası Izgara (Grid) Ayarları
            numGridMm.Minimum = 1;
            numGridMm.Maximum = 50;
            numGridMm.Value = 5;
            chkSnapToGrid.Checked = true;

            // Varsayılan Kağıt Boyutu: DL Zarf
            txtPageWidthMm.Text = "220";
            txtPageHeightMm.Text = "110";
            rbPortrait.Checked = true;

            if (cmbAdet != null) cmbAdet.SelectedIndex = 0;

            SetupPaperSizes();
            SetupResponsiveLayout();

            // --- ÇOKLU ZARF SEKMESİ YAZICI DOLDURMA ---
            if (cmbCokluPrinter != null)
            {
                cmbCokluPrinter.Items.Clear();
                foreach (string printer in System.Drawing.Printing.PrinterSettings.InstalledPrinters)
                {
                    cmbCokluPrinter.Items.Add(printer);
                }

                // 🌟 Önce ayarlara bak, özel atanmış yazıcı var mı?
                string ozelYazici = Properties.Settings.Default.YaziciCokluZarf;

                if (!string.IsNullOrEmpty(ozelYazici) && cmbCokluPrinter.Items.Contains(ozelYazici))
                {
                    cmbCokluPrinter.SelectedItem = ozelYazici;
                }
                else
                {
                    System.Drawing.Printing.PrintDocument pd = new System.Drawing.Printing.PrintDocument();
                    string defaultPrinter = pd.PrinterSettings.PrinterName;
                    if (cmbCokluPrinter.Items.Contains(defaultPrinter)) cmbCokluPrinter.SelectedItem = defaultPrinter;
                    else if (cmbCokluPrinter.Items.Count > 0) cmbCokluPrinter.SelectedIndex = 0;
                }
            }

            // --- MANUEL ETİKET SEKMESİ YAZICI DOLDURMA ---
            if (cmbManuelPrinter != null)
            {
                cmbManuelPrinter.Items.Clear();
                foreach (string printer in System.Drawing.Printing.PrinterSettings.InstalledPrinters)
                {
                    cmbManuelPrinter.Items.Add(printer);
                }

                // 🌟 Önce ayarlara bak, özel atanmış yazıcı var mı?
                string ozelYazici = Properties.Settings.Default.YaziciManuelEtiket;

                if (!string.IsNullOrEmpty(ozelYazici) && cmbManuelPrinter.Items.Contains(ozelYazici))
                {
                    cmbManuelPrinter.SelectedItem = ozelYazici;
                }
                else
                {
                    System.Drawing.Printing.PrintDocument pd = new System.Drawing.Printing.PrintDocument();
                    string defaultPrinter = pd.PrinterSettings.PrinterName;
                    if (cmbManuelPrinter.Items.Contains(defaultPrinter)) cmbManuelPrinter.SelectedItem = defaultPrinter;
                    else if (cmbManuelPrinter.Items.Count > 0) cmbManuelPrinter.SelectedIndex = 0;
                }
            }

            // Tasarım Masası Olaylarını Bağla
            pnlDesignSurface.Paint += PnlDesignSurface_Paint;
            pnlDesignSurface.MouseDown += PnlDesignSurface_MouseDown;

            // Üretim Takip Raporları Kayıt Yeri Hatırlatıcısı
            string ayarDosyasi = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "KayitYeri.txt");
            if (System.IO.File.Exists(ayarDosyasi))
            {
                if (txtKayitYeri != null) txtKayitYeri.Text = System.IO.File.ReadAllText(ayarDosyasi);
            }

            // Üretim Takip (A4) DataGridView Ayarları
            dgvUretim.ReadOnly = false;
            dgvUretim.Columns["ÜrünKodu"].ReadOnly = true;
            dgvUretim.Columns["ÜrünAçıklaması"].ReadOnly = true;
            dgvUretim.Columns["ÜrünBarkodu"].ReadOnly = true;
            dgvUretim.Columns["ÜrünAdeti"].ReadOnly = false;
            dgvUretim.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvBarkodVerileri.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            // Saat ve Takvim Başlatıcı
            lblKarsilama.Text = $"Hoş Geldin {AktifKullaniciAdi}";
            timerSaat.Tick += timerSaat_Tick;
            timerSaat.Interval = 1000;
            timerSaat.Start();

            // Dinamik Ekran Boyutlandırma Tetikleyicisi
            tabControl1.SelectedIndexChanged += tabControl1_SelectedIndexChanged;

            // Program Açılış Boyutu ve Konumu
            this.Size = new Size(950, 600);
            this.CenterToScreen();

            // Görsel Temayı (Renkler ve Yazı Tipleri) Uygula
            ElitTasarimiUygula();
        }
        #endregion

        #region 🖥️ 02.2 DİNAMİK EKRAN YÖNETİMİ
        // Kullanıcının geçtiği sekmeye göre pencere boyutunu dinamik olarak genişletip daraltır.
        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            string seciliSekme = tabControl1.SelectedTab.Text;

            if (seciliSekme == "Ana Panel")
            {
                this.Size = new Size(950, 600);
                this.CenterToScreen();
            }
            else if (seciliSekme == "Üretim Takip")
            {
                this.Size = new Size(1400, 850);
                this.CenterToScreen();

                cmbUretimYazici.Items.Clear();
                foreach (string printer in System.Drawing.Printing.PrinterSettings.InstalledPrinters)
                {
                    cmbUretimYazici.Items.Add(printer);
                }
                if (cmbUretimYazici.Items.Count > 0) cmbUretimYazici.SelectedIndex = 0;

                txtBarkodOkut.Focus();
            }
            else if (seciliSekme == "Çoklu Zarf Yazdırma" || seciliSekme == "Sevkiyat")
            {
                this.Size = new Size(1400, 850);
                this.CenterToScreen();
            }
            else
            {
                this.Size = new Size(1100, 700);
                this.CenterToScreen();
            }
        }
        #endregion

        #region ⏰ 02.3 SİSTEM SAATİ
        // Ana ekrandaki saat ve tarihi her saniye günceller
        private void timerSaat_Tick(object sender, EventArgs e)
        {
            lblSaat.Text = DateTime.Now.ToString("HH:mm:ss");
            lblTakvim.Text = DateTime.Now.ToString("dd MMMM yyyy, dddd");
        }
        #endregion

        #region 🔗 02.4 OLAY BAĞLAYICI (WIRE UI EVENTS)
        // Tasarım arayüzündeki tüm butonların tıklama olaylarını ilgili metotlara bağlar.
        private void WireUiEvents()
        {
            // Tasarım Araçları
            if (btnAddLabel != null) btnAddLabel.Click += BtnAddLabel_Click;
            if (btnAddField != null) btnAddField.Click += BtnAddField_Click;
            if (btnAddFrame != null) btnAddFrame.Click += BtnAddFrame_Click;
            if (btnAddImage != null) btnAddImage.Click += BtnAddImage_Click;
            if (btnDeleteItem != null) btnDeleteItem.Click += BtnDeleteItem_Click;
            if (btnApplyProp != null) btnApplyProp.Click += BtnApplyProp_Click;
            if (btnPropColor != null) btnPropColor.Click += BtnPropColor_Click;

            // Şablon ve Yazdırma Komutları
            if (btnSaveTemplate != null) btnSaveTemplate.Click += BtnSaveTemplate_Click;
            if (btnLoadTemplate != null) btnLoadTemplate.Click += BtnLoadTemplate_Click;
            if (btnPreview != null) btnPreview.Click += BtnPreview_Click;
            if (btnPrint != null) btnPrint.Click += BtnPrint_Click;
            if (btnManuelOnizle != null) btnManuelOnizle.Click += (s, e) => RunManualPrint(true);
            if (btnManuelYazdir != null) btnManuelYazdir.Click += (s, e) => RunManualPrint(false);
            if (btnTemizleTasarm != null) btnTemizleTasarm.Click += BtnTemizleTasarm_Click;
            if (btnDisSablonYukle != null) btnDisSablonYukle.Click += btnDisSablonYukle_Click;

            // Firma/Zarf Yönetimi
            if (btnZarfYenile != null) { btnZarfYenile.Click -= btnZarfYenile_Click; btnZarfYenile.Click += btnZarfYenile_Click; }
            if (btnAra != null) { btnAra.Click -= btnAra_Click; btnAra.Click += btnAra_Click; }
            if (btnCikar != null) { btnCikar.Click -= btnCikar_Click; btnCikar.Click += btnCikar_Click; }
            if (btnTemizle != null) { btnTemizle.Click -= btnTemizle_Click; btnTemizle.Click += btnTemizle_Click; }
            if (btnCokluZarfYazdir != null) { btnCokluZarfYazdir.Click -= btnCokluZarfYazdir_Click; btnCokluZarfYazdir.Click += btnCokluZarfYazdir_Click; }
            if (dgvZarfFirmalar != null) { dgvZarfFirmalar.CellDoubleClick -= dgvZarfFirmalar_CellDoubleClick; dgvZarfFirmalar.CellDoubleClick += dgvZarfFirmalar_CellDoubleClick; }
            if (btnTumFirmalariSil != null) { btnTumFirmalariSil.Click -= btnTumFirmalariSil_Click; btnTumFirmalariSil.Click += btnTumFirmalariSil_Click; }
            if (lstFirmalar != null) { lstFirmalar.SelectedIndexChanged -= lstFirmalar_SelectedIndexChanged; lstFirmalar.SelectedIndexChanged += lstFirmalar_SelectedIndexChanged; }
            if (lstFirmalar != null) { lstFirmalar.DoubleClick -= lstFirmalar_DoubleClick; lstFirmalar.DoubleClick += lstFirmalar_DoubleClick; }

            // Ambar/Desi Yönetimi
            if (btnAmbarAra != null) { btnAmbarAra.Click -= btnAmbarAra_Click; btnAmbarAra.Click += btnAmbarAra_Click; }
            if (dgvAmbarTumFirmalar != null) { dgvAmbarTumFirmalar.CellDoubleClick -= dgvAmbarTumFirmalar_CellDoubleClick; dgvAmbarTumFirmalar.CellDoubleClick += dgvAmbarTumFirmalar_CellDoubleClick; }
            if (dgvAmbarSecilenFirmalar != null) { dgvAmbarSecilenFirmalar.CellDoubleClick -= dgvAmbarSecilenFirmalar_CellDoubleClick; dgvAmbarSecilenFirmalar.CellDoubleClick += dgvAmbarSecilenFirmalar_CellDoubleClick; }
            if (cmbPaletSayisi != null) { cmbPaletSayisi.SelectedIndexChanged -= cmbPaletSayisi_SelectedIndexChanged; cmbPaletSayisi.SelectedIndexChanged += cmbPaletSayisi_SelectedIndexChanged; }
            if (dgvPaletler != null) { dgvPaletler.CellValueChanged -= dgvPaletler_CellValueChanged; dgvPaletler.CellValueChanged += dgvPaletler_CellValueChanged; }
            if (btnAmbarYenile != null) { btnAmbarYenile.Click -= btnAmbarYenile_Click; btnAmbarYenile.Click += btnAmbarYenile_Click; }
            if (btnAmbarListeyeEkle != null) { btnAmbarListeyeEkle.Click -= btnAmbarListeyeEkle_Click; btnAmbarListeyeEkle.Click += btnAmbarListeyeEkle_Click; }
            if (btnAmbarSil != null) { btnAmbarSil.Click -= btnAmbarSil_Click; btnAmbarSil.Click += btnAmbarSil_Click; }
            if (btnAmbarYazdir != null) { btnAmbarYazdir.Click -= btnAmbarYazdir_Click; btnAmbarYazdir.Click += btnAmbarYazdir_Click; }

            // Depo Sayım Sistemi
            if (txtSayimBarkod != null) { txtSayimBarkod.KeyDown -= TxtSayimBarkod_KeyDown; txtSayimBarkod.KeyDown += TxtSayimBarkod_KeyDown; }
            if (btnSayimBitir != null) { btnSayimBitir.Click -= BtnSayimBitir_Click; btnSayimBitir.Click += BtnSayimBitir_Click; }
            if (btnSayimYenile != null) { btnSayimYenile.Click -= BtnSayimYenile_Click; btnSayimYenile.Click += BtnSayimYenile_Click; }
            if (btnSayimAc != null) { btnSayimAc.Click -= BtnSayimAc_Click; btnSayimAc.Click += BtnSayimAc_Click; }

            // ⚡ KRİTİK HATA ÇÖZÜMÜ: Müşteri Seçildiğinde Belge No Kutusunu Doldurur
            if (cmbMusteri != null)
            {
                cmbMusteri.SelectedIndexChanged -= cmbMusteri_SelectedIndexChanged;
                cmbMusteri.SelectedIndexChanged += cmbMusteri_SelectedIndexChanged;
            }

            // Sevkiyat ve Yarım Kalanlar (Askı) Sistemi
            if (btnYarimGetir != null) { btnYarimGetir.Click -= btnYarimGetir_Click; btnYarimGetir.Click += btnYarimGetir_Click; }
            if (btnYarimAc != null) { btnYarimAc.Click -= btnYarimAc_Click; btnYarimAc.Click += btnYarimAc_Click; }
            if (btnGecmisSevkleriListele != null) { btnGecmisSevkleriListele.Click -= btnGecmisSevkleriListele_Click; btnGecmisSevkleriListele.Click += btnGecmisSevkleriListele_Click; }

            // 🌟 SEVKİYAT SAYFASI MOTORU BAĞLANTI KÖPRÜLERİ
            if (cmbSevkPaletSayisi != null) { cmbSevkPaletSayisi.SelectedIndexChanged -= cmbSevkPaletSayisi_SelectedIndexChanged; cmbSevkPaletSayisi.SelectedIndexChanged += cmbSevkPaletSayisi_SelectedIndexChanged; }
            if (btnSevkAra != null) { btnSevkAra.Click -= btnSevkAra_Click; btnSevkAra.Click += btnSevkAra_Click; }
            if (btnTamSevk != null) { btnTamSevk.Click -= btnTamSevk_Click; btnTamSevk.Click += btnTamSevk_Click; }
            if (btnKismiSevk != null) { btnKismiSevk.Click -= btnKismiSevk_Click; btnKismiSevk.Click += btnKismiSevk_Click; }
            if (btnSevkAskayaAl != null) { btnSevkAskayaAl.Click -= btnSevkAskayaAl_Click; btnSevkAskayaAl.Click += btnSevkAskayaAl_Click; }
            if (txtBarkod != null) { txtBarkod.KeyDown -= txtBarkod_KeyDown; txtBarkod.KeyDown += txtBarkod_KeyDown; }
        }
        #endregion

        #region 🎨 02.5 GÖRSEL TEMA (ELİT TASARIM)
        // Ana sayfadaki karşılama yazıları ve çıkış butonlarının elit (şık) renk paletine geçirilmesi
        private void ElitTasarimiUygula()
        {
            try
            {
                lblKarsilama.Font = new Font("Segoe UI Semibold", 22, FontStyle.Italic);
                lblKarsilama.ForeColor = Color.FromArgb(15, 76, 58); // Koyu elit yeşil

                lblSaat.Font = new Font("Segoe UI", 36, FontStyle.Bold);
                lblSaat.ForeColor = Color.FromArgb(45, 52, 54); // Soft antrasit siyah

                lblTakvim.Font = new Font("Segoe UI", 14, FontStyle.Regular);
                lblTakvim.ForeColor = Color.DimGray;

                // Programdan Çıkış Butonu Stili
                Control[] butonlar = this.Controls.Find("btnCikisYap", true);
                if (butonlar.Length > 0 && butonlar[0] is Button btnCikis)
                {
                    btnCikis.FlatStyle = FlatStyle.Flat;
                    btnCikis.FlatAppearance.BorderSize = 0;
                    btnCikis.BackColor = Color.FromArgb(15, 76, 58);
                    btnCikis.ForeColor = Color.White;
                    btnCikis.Font = new Font("Segoe UI", 12, FontStyle.Bold);
                    btnCikis.Cursor = Cursors.Hand;
                }

                // Oturumu Kapat (Giriş Ekranına Dön) Butonu Stili
                Control[] btnOturumKapat = this.Controls.Find("btnLoginDon", true);
                if (btnOturumKapat.Length > 0 && btnOturumKapat[0] is Button btnKapat)
                {
                    btnKapat.FlatStyle = FlatStyle.Flat;
                    btnKapat.FlatAppearance.BorderSize = 2;
                    btnKapat.FlatAppearance.BorderColor = Color.FromArgb(15, 76, 58);
                    btnKapat.BackColor = Color.White;
                    btnKapat.ForeColor = Color.FromArgb(15, 76, 58);
                    btnKapat.Font = new Font("Segoe UI", 11, FontStyle.Bold);
                    btnKapat.Cursor = Cursors.Hand;
                }
            }
            catch { }
        }

        private void btnLoginDon_Click(object sender, EventArgs e)
        {
            DialogResult onay = MessageBox.Show("Mevcut oturumu kapatıp Giriş Ekranına dönmek istediğinize emin misiniz?", "Oturumu Kapat", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (onay == DialogResult.Yes)
            {
                kapanisBasladi = true;
                AktifKullaniciAdi = "";
                AktifYetkiler = "";
                Application.Restart(); // Programı yeniden başlatarak LoginForm'a döndürür
            }
        }
        #endregion

        #endregion

        // =========================================================================================

        #region 📐 03. TASARIM MASASI VE IZGARA SİSTEMİ (UI)

        #region 🖥️ 03.1 ÇALIŞMA ALANI VE YERLEŞİM (WORKSPACE)
        // Tasarım arayüzündeki panellerin ekrana doğru şekilde yerleşmesini sağlar.
        // Ortada devasa gri bir masa (Workspace) ve onun içinde beyaz bir kağıt (DesignSurface) oluşturur.
        private void SetupResponsiveLayout()
        {
            // Üst, sol (araçlar) ve sağ (özellikler) panellerini form kenarlarına yapıştır
            if (panel2 != null) { panel2.Dock = DockStyle.Top; panel2.BringToFront(); }
            if (pnlToolbox != null) { pnlToolbox.Dock = DockStyle.Left; pnlToolbox.BringToFront(); }
            if (pnlProperties != null) { pnlProperties.Dock = DockStyle.Right; pnlProperties.BringToFront(); }

            // Eğer gri çalışma masası henüz oluşturulmadıysa sıfırdan oluştur
            if (pnlWorkspace == null)
            {
                pnlWorkspace = new Panel();
                pnlWorkspace.Dock = DockStyle.Fill; // Kalan tüm boşluğu kapla
                pnlWorkspace.BackColor = Color.Gray; // Masanın rengi
                pnlWorkspace.AutoScroll = true; // Kağıt ekrana sığmazsa kaydırma çubukları çıksın

                // Tasarım sekmesine gri masayı ekle
                tabPage11.Controls.Add(pnlWorkspace);
                pnlWorkspace.BringToFront();

                // Beyaz kağıdı (DesignSurface) serbest bırak ve rengini ayarla
                pnlDesignSurface.Dock = DockStyle.None;
                pnlDesignSurface.BackColor = Color.White;

                // Beyaz kağıdı gri masanın içine koy
                pnlWorkspace.Controls.Add(pnlDesignSurface);

                // Ekran boyutu her değiştiğinde kağıdı masanın tam ortasına sabitle
                pnlWorkspace.Resize += (s, e) => CenterDesignSurface();

                // Cetvellerin çizilmesi için masanın Paint olayını bağla
                pnlWorkspace.Paint += PnlWorkspace_Paint;
            }
        }

        // Beyaz kağıdı, gri masanın tam merkezine konumlandıran matematiksel motor
        private void CenterDesignSurface()
        {
            if (pnlWorkspace != null && pnlDesignSurface != null)
            {
                // Masanın genişliğinden kağıdın genişliğini çıkarıp 2'ye bölerek tam ortayı buluyoruz
                int x = (pnlWorkspace.Width - pnlDesignSurface.Width) / 2;
                int y = (pnlWorkspace.Height - pnlDesignSurface.Height) / 2;

                // Kağıdın en az 20 piksel boşluk bırakacak şekilde yerleşmesini sağla (ekran çok küçülse bile köşeye yapışmasın)
                pnlDesignSurface.Location = new Point(Math.Max(20, x), Math.Max(20, y));
                pnlWorkspace.Invalidate(); // Ekranı tazele
            }
        }
        #endregion

        #region 📄 03.2 KAĞIT BOYUTU VE YÖNETİMİ (PAPER SIZES)
        // Üst taraftaki "Kağıt Biçimi" açılır kutusunu oluşturur ve varsayılan boyutları yükler
        private void SetupPaperSizes()
        {
            cmbPaperSize = new ComboBox();
            cmbPaperSize.DropDownStyle = ComboBoxStyle.DropDownList; // Elle yazılmasını engelle, sadece seçime izin ver
            cmbPaperSize.Items.AddRange(new string[] { "DL Zarf (220x110 mm)", "A4 Kağıt (210x297 mm)", "10x15 Etiket (100x150 mm)", "Özel Boyut" });
            cmbPaperSize.Width = 170;

            Label lblPaper = new Label() { Text = "Kağıt Biçimi:", Width = 75, AutoSize = true };

            // Oluşturulan menüyü üst panele (panel2) yerleştir
            if (panel2 != null)
            {
                panel2.Controls.Add(lblPaper);
                panel2.Controls.Add(cmbPaperSize);
                lblPaper.Left = 450; lblPaper.Top = 10;
                cmbPaperSize.Left = 530; cmbPaperSize.Top = 8;
            }

            // Seçim değiştiğinde boyutları hesaplayacak eventi bağla
            cmbPaperSize.SelectedIndexChanged += CmbPaperSize_SelectedIndexChanged;

            // Yatay/Dikey radyo butonlarının olaylarını bağla
            if (rbLandscape != null) rbLandscape.CheckedChanged += (s, e) => { if (rbLandscape.Checked) ApplyDesignSurfaceSize(); };
            if (rbPortrait != null) rbPortrait.CheckedChanged += (s, e) => { if (rbPortrait.Checked) ApplyDesignSurfaceSize(); };

            // Varsayılan olarak ilk sıradaki DL Zarf'ı seçili getir
            cmbPaperSize.SelectedIndex = 0;
        }

        // Kullanıcı listeden yeni bir kağıt boyutu seçtiğinde en-boy kutularını otomatik doldurur
        private void CmbPaperSize_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbPaperSize.SelectedIndex == 0) { txtPageWidthMm.Text = "220"; txtPageHeightMm.Text = "110"; rbLandscape.Checked = true; }
            if (cmbPaperSize.SelectedIndex == 1) { txtPageWidthMm.Text = "210"; txtPageHeightMm.Text = "297"; rbPortrait.Checked = true; }
            if (cmbPaperSize.SelectedIndex == 2) { txtPageWidthMm.Text = "100"; txtPageHeightMm.Text = "150"; rbPortrait.Checked = true; }

            // Yeni değerlere göre kağıdı yeniden çiz
            ApplyDesignSurfaceSize();
        }

        // Girilen milimetrik ölçüleri ekrandaki piksel (PX) karşılığına çevirip beyaz kağıdı boyutlandırır
        private void ApplyDesignSurfaceSize(float w = 0, float h = 0, bool isLand = false)
        {
            // Kutulardaki değeri okumaya çalış, okuyamazsa varsayılan zarf boyutunu baz al
            if (!float.TryParse(txtPageWidthMm.Text, out w)) w = 220f;
            if (!float.TryParse(txtPageHeightMm.Text, out h)) h = 110f;

            // Eğer Yatay (Landscape) seçiliyse ve yükseklik genişlikten büyükse değerleri yer değiştir
            if (rbLandscape != null && rbLandscape.Checked && h > w) { float temp = w; w = h; h = temp; txtPageWidthMm.Text = w.ToString(); txtPageHeightMm.Text = h.ToString(); }

            // Eğer Dikey (Portrait) seçiliyse ve genişlik yükseklikten büyükse değerleri yer değiştir
            if (rbPortrait != null && rbPortrait.Checked && w > h) { float temp = w; w = h; h = temp; txtPageWidthMm.Text = w.ToString(); txtPageHeightMm.Text = h.ToString(); }

            // Ekranın DPI (Piksel Yoğunluğu) değerini al
            float screenDpiX = GetScreenDpiX();

            // Milimetreyi, mevcut monitörün pikseline çevir (1 inç = 25.4 mm mantığı ile)
            float widthPx = MmToPx(w, screenDpiX);
            float heightPx = MmToPx(h, screenDpiX);

            // Beyaz kağıdın (DesignSurface) yeni fiziksel piksellerini uygula
            if (pnlDesignSurface != null)
            {
                pnlDesignSurface.Width = Math.Max(100, (int)Math.Round(widthPx)); // Çok küçülmesini engellemek için minimum 100px koruması
                pnlDesignSurface.Height = Math.Max(100, (int)Math.Round(heightPx));
                pnlDesignSurface.Invalidate(); // Çizimleri tazele
            }

            // Boyut değiştiği için kağıdı tekrar masanın ortasına çek
            CenterDesignSurface();
        }
        #endregion

        #region 📏 03.3 CETVEL VE ÇİZİM MOTORU (RULERS)
        // Gri masanın (Workspace) üzerine beyaz kağıdın etrafını saracak şekilde milimetrik cetvelleri çizer
        private void PnlWorkspace_Paint(object sender, PaintEventArgs e)
        {
            if (pnlDesignSurface == null) return;
            Graphics g = e.Graphics;
            Pen pen = Pens.Black;
            Brush brush = Brushes.Black;
            Font font = new Font("Arial", 7);

            int rulerSize = 20; // Cetvelin kalınlığı (piksel)
            int paperX = pnlDesignSurface.Left;
            int paperY = pnlDesignSurface.Top;
            int paperW = pnlDesignSurface.Width;
            int paperH = pnlDesignSurface.Height;

            // Üstteki yatay cetvelin gri arkaplanını ve çerçevesini çiz
            g.FillRectangle(Brushes.WhiteSmoke, paperX, paperY - rulerSize, paperW, rulerSize);
            g.DrawRectangle(pen, paperX, paperY - rulerSize, paperW, rulerSize);

            // Soldaki dikey cetvelin gri arkaplanını ve çerçevesini çiz
            g.FillRectangle(Brushes.WhiteSmoke, paperX - rulerSize, paperY, rulerSize, paperH);
            g.DrawRectangle(pen, paperX - rulerSize, paperY, rulerSize, paperH);

            // Ekranda 1 milimetrenin kaç piksele denk geldiğini hesapla
            float mmToPx = g.DpiX / 25.4f;

            // ÜST YATAY CETVEL ÇİZGİLERİ VE RAKAMLARI
            for (int mm = 0; mm * mmToPx <= paperW; mm += 5)
            {
                float px = paperX + (mm * mmToPx);
                if (mm % 10 == 0)
                {
                    // 10, 20, 30 gibi tam sayılarda uzun çizgi çek ve rakamı yaz
                    g.DrawLine(pen, px, paperY - rulerSize, px, paperY);
                    g.DrawString(mm.ToString(), font, brush, px + 2, paperY - rulerSize + 2);
                }
                else
                {
                    // 5, 15, 25 gibi buçuklu sayılarda sadece kısa ara çizgi çek
                    g.DrawLine(pen, px, paperY - rulerSize / 2, px, paperY);
                }
            }

            // SOL DİKEY CETVEL ÇİZGİLERİ VE RAKAMLARI
            for (int mm = 0; mm * mmToPx <= paperH; mm += 5)
            {
                float px = paperY + (mm * mmToPx);
                if (mm % 10 == 0)
                {
                    g.DrawLine(pen, paperX - rulerSize, px, paperX, px);
                    g.DrawString(mm.ToString(), font, brush, paperX - rulerSize + 2, px + 2);
                }
                else
                {
                    g.DrawLine(pen, paperX - rulerSize / 2, px, paperX, px);
                }
            }
        }
        #endregion

        #endregion

        // =========================================================================================

        #region 🎨 04. GÖRSEL TASARIM EDİTÖRÜ (SÜRÜKLE-BIRAK)

        #region 📐 04.1 IZGARA (GRID) ÇİZİMİ
        // Kağıdın üzerine (DesignSurface) kullanıcının hizalama yapmasını kolaylaştıran ızgara çizgilerini çizer.
        private void PnlDesignSurface_Paint(object sender, PaintEventArgs e)
        {
            // Eğer "Izgaraya Hizala" tiki kapalıysa çizgileri hiç çizme
            if (!chkSnapToGrid.Checked) return;

            // Milimetrik ızgara aralığını piksele çevir (örn: 5mm = ? px)
            float gridPx = e.Graphics.DpiX * (float)numGridMm.Value / 25.4f;

            using (var pen = new Pen(Color.LightGray, 1f))
            {
                // Dikey çizgileri çiz
                for (float x = 0; x < pnlDesignSurface.Width; x += gridPx)
                    e.Graphics.DrawLine(pen, x, 0, x, pnlDesignSurface.Height);

                // Yatay çizgileri çiz
                for (float y = 0; y < pnlDesignSurface.Height; y += gridPx)
                    e.Graphics.DrawLine(pen, 0, y, pnlDesignSurface.Width, y);
            }
        }
        #endregion

        #region 📦 04.2 NESNE ÜRETİM VE YERLEŞTİRME MOTORU
        // Hafızadaki bir DesignItem (JSON) modelini okuyup, onu ekranda fiziksel bir kutuya (Control) dönüştürür.
        private void CreateControlForDesignItem(DesignItem item)
        {
            if (item == null) return;
            if (item.FontSizePt <= 0) item.FontSizePt = 12f; // Font boyutu sıfırlanmışsa varsayılan 12 yap

            Control ctrl;

            // Etiket (Sabit Yazı) veya Alan (Veritabanından Gelen Değişken) İse
            if (item.Type == "Label" || item.Type == "Field")
            {
                var lbl = new Label { AutoSize = false };

                // Eğer bu bir 'Field' ise tasarım ekranında süslü parantez içinde alan adını göster (Örn: {FirmaAdi})
                lbl.Text = item.Type == "Field" ? $"{{{item.PlaceholderKey}}}" : (item.Text ?? "");

                // Font Zırhı: Eğer sistemde olmayan bir font seçildiyse, program çökmesin diye Arial'e geri dön
                try { lbl.Font = new Font(item.FontName ?? "Arial", item.FontSizePt, item.FontStyle); }
                catch { lbl.Font = new Font("Arial", item.FontSizePt, item.FontStyle); }

                // Renk Zırhı: Renk kodu (HEX) bozuksa siyah renk kullan
                try { lbl.ForeColor = ColorTranslator.FromHtml(item.ColorName); }
                catch { lbl.ForeColor = Color.Black; }

                lbl.TextAlign = item.Alignment == "Center" ? ContentAlignment.MiddleCenter : item.Alignment == "Right" ? ContentAlignment.MiddleRight : ContentAlignment.MiddleLeft;
                lbl.BackColor = Color.Transparent;
                lbl.BorderStyle = BorderStyle.FixedSingle;
                ctrl = lbl;
            }
            // Sadece Çerçeve (Kare/Dikdörtgen) İse
            else if (item.Type == "Frame")
            {
                ctrl = new Panel { BorderStyle = BorderStyle.FixedSingle, BackColor = Color.Transparent };
            }
            // Resim (Logo vb.) İse
            else
            {
                var pb = new PictureBox { SizeMode = PictureBoxSizeMode.StretchImage, BackColor = Color.LightGray };
                if (item.Type == "Image" && !string.IsNullOrWhiteSpace(item.Text) && File.Exists(item.Text))
                {
                    try { pb.Image = Image.FromFile(item.Text); } catch { pb.Image = null; } // Resim silinmişse veya bozuksa boş göster
                }
                ctrl = pb;
            }

            // Fiziksel kutunun içine, tüm ayarlarını içeren hafıza modelini (Tag) gizle
            ctrl.Tag = item;

            // Kutuyu beyaz kağıdın üzerine ekle
            pnlDesignSurface.Controls.Add(ctrl);
            PlaceControlOnDesignSurface(ctrl, item);

            // Çift tıklanınca özellikleri yanda açma olayını bağla
            ctrl.DoubleClick -= DesignControl_DoubleClick;
            ctrl.DoubleClick += DesignControl_DoubleClick;
        }

        // Oluşturulan fiziksel kutunun, modeldeki milimetrik koordinatlara göre kağıt üzerindeki tam konumunu (X,Y) belirler
        private void PlaceControlOnDesignSurface(Control ctrl, DesignItem item)
        {
            if (ctrl == null || item == null) return;

            float screenDpi = GetScreenDpiX();
            float mmToPx = screenDpi / 25.4f;

            // Milimetreyi piksele çevirerek X ve Y eksenlerine oturt
            ctrl.Left = (int)Math.Round(item.Xmm * mmToPx);
            ctrl.Top = (int)Math.Round(item.Ymm * mmToPx);
            ctrl.Width = Math.Max(1, (int)Math.Round(item.Wmm * mmToPx));
            ctrl.Height = Math.Max(1, (int)Math.Round(item.Hmm * mmToPx));

            ctrl.Tag = item;

            // Fare ile tut-sürükle-bırak olaylarını bağla
            ctrl.MouseDown -= DesignControl_MouseDown;
            ctrl.MouseMove -= DesignControl_MouseMove;
            ctrl.MouseUp -= DesignControl_MouseUp;

            ctrl.MouseDown += DesignControl_MouseDown;
            ctrl.MouseMove += DesignControl_MouseMove;
            ctrl.MouseUp += DesignControl_MouseUp;
        }
        #endregion

        #region ➕ 04.3 YENİ NESNE EKLEME BUTONLARI
        // Sabit Metin (Label) Ekle
        private void BtnAddLabel_Click(object sender, EventArgs e)
        {
            var item = new DesignItem
            {
                Type = "Label",
                Text = "Yeni Label",
                Xmm = 10,
                Ymm = 10,
                Wmm = 60,
                Hmm = 10, // Varsayılan Başlangıç Konumu
                FontName = cmbPropFont.SelectedItem?.ToString() ?? "Arial",
                FontSizePt = (float)numPropFontSize.Value
            };

            designItems.Add(item);
            CreateControlForDesignItem(item);
        }

        // Dinamik Veri Alanı ({FirmaAdi} gibi) Ekle
        private void BtnAddField_Click(object sender, EventArgs e)
        {
            string secilenAlan = cmbPropPlaceholder.SelectedItem?.ToString() ?? "FirmaAdi";

            // 🛡️ KOTA KONTROLÜ: Kullanıcının belirlediği limitin (Örn: Adres alanı sadece 1 tane olabilir) aşılmasını engelle
            int izinVerilenMaxAdet = 1;
            if (cmbAdet != null && cmbAdet.SelectedItem != null)
            {
                int.TryParse(cmbAdet.SelectedItem.ToString(), out izinVerilenMaxAdet);
            }
            if (izinVerilenMaxAdet <= 0) izinVerilenMaxAdet = 1;

            // Kağıt üzerinde bu alandan şu an kaç tane var say
            int kagitUzerindekiAdet = designItems.Count(x => x.Type == "Field" && x.PlaceholderKey == secilenAlan);

            // Limit aşıldıysa uyarı ver ve eklemeyi durdur
            if (kagitUzerindekiAdet >= izinVerilenMaxAdet)
            {
                MessageBox.Show($"DİKKAT: Kağıda en fazla {izinVerilenMaxAdet} adet '{{{secilenAlan}}}' eklemenize izin verilmiştir!\n\nLütfen limiti artırın veya kağıttakilerden birini silin.", "Kota Doldu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var yeniAlan = new DesignItem
            {
                Type = "Field",
                PlaceholderKey = secilenAlan,
                Text = "", // Dinamik olduğu için metin boştur
                Xmm = 10 + (kagitUzerindekiAdet * 10), // Üst üste binmemeleri için X-Y koordinatını hafif kaydır
                Ymm = 25 + (kagitUzerindekiAdet * 10),
                Wmm = 100,
                Hmm = 12,
                FontName = cmbPropFont.SelectedItem?.ToString() ?? "Arial",
                FontSizePt = (float)numPropFontSize.Value
            };

            designItems.Add(yeniAlan);
            CreateControlForDesignItem(yeniAlan);
        }

        // Çerçeve Ekle
        private void BtnAddFrame_Click(object sender, EventArgs e)
        {
            var item = new DesignItem { Type = "Frame", Text = "", Xmm = 5, Ymm = 5, Wmm = 200, Hmm = 50 };
            designItems.Add(item);
            CreateControlForDesignItem(item);
        }

        // Resim / Logo Ekle
        private void BtnAddImage_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog { Filter = "Image files|*.png;*.jpg;*.jpeg;*.bmp|All files|*.*" })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    var item = new DesignItem { Type = "Image", Text = ofd.FileName, Xmm = 5, Ymm = 5, Wmm = 30, Hmm = 30 };
                    designItems.Add(item);
                    CreateControlForDesignItem(item);
                }
            }
        }

        // Seçili Nesneyi Sil (Delete)
        private void BtnDeleteItem_Click(object sender, EventArgs e)
        {
            if (selectedDesignItem == null) return;

            // Ekranda seçili olan modelin fiziksel kutusunu bul
            var ctrl = pnlDesignSurface.Controls.Cast<Control>().FirstOrDefault(c => c.Tag == selectedDesignItem);

            // Kutuyu kağıttan sil
            if (ctrl != null) pnlDesignSurface.Controls.Remove(ctrl);

            // Nesneyi RAM'deki listeden de tamamen çıkar
            designItems.Remove(selectedDesignItem);
            selectedDesignItem = null;
        }
        #endregion

        #region 🎛️ 04.4 ÖZELLİKLER PANELİ (PROPERTIES) YÖNETİMİ
        // Kağıt üzerindeki bir nesneye çift tıklandığında, o nesnenin ayarlarını yandaki özellikler paneline yansıtır.
        private void DesignControl_DoubleClick(object sender, EventArgs e)
        {
            var ctrl = sender as Control;
            var item = ctrl?.Tag as DesignItem;
            if (item == null) return;

            selectedDesignItem = item;

            // Nesnedeki verileri, sağdaki panel kutularına güvenlice yazdır (Hata çıkarsa yut)
            try { if (txtPropText != null) txtPropText.Text = item.Text; } catch { }
            try { if (cmbPropPlaceholder != null) cmbPropPlaceholder.SelectedItem = item.PlaceholderKey; } catch { }
            try { if (cmbPropFont != null) cmbPropFont.SelectedItem = item.FontName; } catch { }
            try { if (cmbPropRotation != null) cmbPropRotation.SelectedItem = item.Rotation.ToString(); } catch { }
            try { if (cmbPropAlignment != null) cmbPropAlignment.SelectedItem = string.IsNullOrEmpty(item.Alignment) ? "Center" : item.Alignment; } catch { }
            try { if (numPropFontSize != null) numPropFontSize.Value = Math.Max(numPropFontSize.Minimum, Math.Min(numPropFontSize.Maximum, (decimal)item.FontSizePt)); } catch { }
            try { if (numPropXmm != null) numPropXmm.Value = Math.Max(numPropXmm.Minimum, Math.Min(numPropXmm.Maximum, (decimal)item.Xmm)); } catch { }
            try { if (numPropYmm != null) numPropYmm.Value = Math.Max(numPropYmm.Minimum, Math.Min(numPropYmm.Maximum, (decimal)item.Ymm)); } catch { }
            try { if (numPropWmm != null) numPropWmm.Value = Math.Max(numPropWmm.Minimum, Math.Min(numPropWmm.Maximum, (decimal)item.Wmm)); } catch { }
            try { if (numPropHmm != null) numPropHmm.Value = Math.Max(numPropHmm.Minimum, Math.Min(numPropHmm.Maximum, (decimal)item.Hmm)); } catch { }
            try { if (btnPropColor != null) btnPropColor.BackColor = ColorTranslator.FromHtml(item.ColorName); } catch { }
        }

        // Sağ panelde (Properties) yapılan değişiklikleri seçili nesneye/nesnelere "Uygula" butonuna basınca aktarır.
        private void BtnApplyProp_Click(object sender, EventArgs e)
        {
            foreach (var ctrl in selectedControls)
            {
                var item = ctrl.Tag as DesignItem;
                if (item == null) continue;

                // Metin ve Veri Alanı güncellemelerini sadece TEK BİR nesne seçiliyse yap (Çoklu seçimde isimler birbirine karışmasın)
                if (selectedControls.Count == 1)
                {
                    item.Text = txtPropText.Text;
                    item.PlaceholderKey = cmbPropPlaceholder.SelectedItem?.ToString();
                }

                // Font, boyut ve renk ayarlarını tüm seçili nesnelere topluca uygula
                item.FontName = cmbPropFont.SelectedItem?.ToString() ?? "Arial";
                item.FontSizePt = (float)numPropFontSize.Value;
                item.Alignment = cmbPropAlignment.SelectedItem?.ToString() ?? "Left";
                item.ColorName = ColorTranslator.ToHtml(btnPropColor.BackColor);
                item.Rotation = int.Parse(cmbPropRotation.SelectedItem?.ToString() ?? "0");

                // Fiziksel kutunun görüntüsünü (UI) hemen yeni ayarlara göre güncelle
                if (ctrl is Label lbl)
                {
                    lbl.Text = item.Type == "Field" ? $"{{{item.PlaceholderKey}}}" : item.Text;
                    lbl.Font = new Font(item.FontName, item.FontSizePt, item.FontStyle);
                    try { lbl.ForeColor = ColorTranslator.FromHtml(item.ColorName); } catch { lbl.ForeColor = Color.Black; }
                    lbl.TextAlign = item.Alignment == "Center" ? ContentAlignment.MiddleCenter : item.Alignment == "Right" ? ContentAlignment.MiddleRight : ContentAlignment.MiddleLeft;
                }
            }
        }

        // Renk Seçici Paletini Açar
        private void BtnPropColor_Click(object sender, EventArgs e)
        {
            using (var cd = new ColorDialog())
            {
                if (cd.ShowDialog() == DialogResult.OK) btnPropColor.BackColor = cd.Color;
            }
        }
        #endregion

        #endregion

        // =========================================================================================

        #region ⌨️ 05. KLAVYE / MOUSE KONTROLLERİ VE MATEMATİK

        #region 🖱️ 05.1 FARE HAREKETLERİ (SEÇİM VE SÜRÜKLEME)
        // Kullanıcı kağıt üzerindeki bir nesneye (etiket, resim) sol tıklandığında çalışır.
        private void DesignControl_MouseDown(object sender, MouseEventArgs e)
        {
            Control ctrl = sender as Control;
            if (ctrl == null) return;

            // CTRL Tuşuna basılı tutuluyorsa Çoklu Seçim Modu aktiftir
            if (Control.ModifierKeys == Keys.Control)
            {
                if (selectedControls.Contains(ctrl))
                {
                    // Zaten seçiliyse listeden çıkar ve rengini sıfırla
                    selectedControls.Remove(ctrl);
                    ctrl.BackColor = (ctrl is PictureBox) ? Color.LightGray : Color.Transparent;
                }
                else
                {
                    // Seçili değilse listeye ekle ve arka planını mavi yap
                    selectedControls.Add(ctrl);
                    ctrl.BackColor = Color.LightBlue;
                }
            }
            else
            {
                // Normal tıklamaysa diğer tüm seçimleri temizle, sadece bunu seç
                if (!selectedControls.Contains(ctrl))
                {
                    ClearSelection();
                    selectedControls.Add(ctrl);
                    ctrl.BackColor = Color.LightBlue;
                }
            }

            // Sürükleme veya yeniden boyutlandırma işlemini başlatmak için başlangıç değerlerini al
            draggingControl = ctrl;
            dragStart = e.Location;
            selectedDesignItem = ctrl.Tag as DesignItem;

            // Tıklanan nesneyi kağıtta en üst katmana getir (diğerlerinin üstüne çıksın)
            ctrl.BringToFront();

            // Farenin şekline bakarak kullanıcının nesneyi sürüklemek mi yoksa büyütmek mi istediğini anla
            if (ctrl.Cursor == Cursors.SizeNWSE) { isResizing = true; resizeDir = "NWSE"; } // Çapraz Sündürme
            else if (ctrl.Cursor == Cursors.SizeWE) { isResizing = true; resizeDir = "WE"; } // Yatay Sündürme
            else if (ctrl.Cursor == Cursors.SizeNS) { isResizing = true; resizeDir = "NS"; } // Dikey Sündürme
            else { isDragging = true; } // Normal taşıma
        }

        // Fare nesne üzerindeyken veya tıklı şekilde hareket ederken çalışır
        private void DesignControl_MouseMove(object sender, MouseEventArgs e)
        {
            Control ctrl = sender as Control;
            if (ctrl == null) return;

            // Herhangi bir tıklama/sürükleme yoksa sadece farenin şeklini ayarla
            if (!isDragging && !isResizing)
            {
                int edge = 10; // Köşelere/kenarlara 10 piksel yaklaşıldığında ok şeklini değiştir
                if (e.X > ctrl.Width - edge && e.Y > ctrl.Height - edge) ctrl.Cursor = Cursors.SizeNWSE;
                else if (e.X > ctrl.Width - edge) ctrl.Cursor = Cursors.SizeWE;
                else if (e.Y > ctrl.Height - edge) ctrl.Cursor = Cursors.SizeNS;
                else ctrl.Cursor = Cursors.SizeAll;
            }

            // EĞER BOYUTLANDIRMA (SÜNDÜRME) İŞLEMİ YAPILIYORSA
            if (isResizing && draggingControl != null)
            {
                if (resizeDir == "WE" || resizeDir == "NWSE") draggingControl.Width = Math.Max(10, e.X);
                if (resizeDir == "NS" || resizeDir == "NWSE") draggingControl.Height = Math.Max(10, e.Y);

                // Boyut değiştikçe sağdaki Özellikler (Properties) paneline anlık yansıt
                UpdateItemPositionFromControl(draggingControl, selectedDesignItem);
            }
            // EĞER SÜRÜKLEME (TAŞIMA) İŞLEMİ YAPILIYORSA
            else if (isDragging && draggingControl != null)
            {
                int nx = draggingControl.Left + (e.X - dragStart.X);
                int ny = draggingControl.Top + (e.Y - dragStart.Y);

                // Izgaraya Hizalama (Snap to Grid) açıksa X, Y koordinatlarını ona göre yuvarla
                if (chkSnapToGrid != null && chkSnapToGrid.Checked) { nx = SnapToGrid(nx); ny = SnapToGrid(ny); }

                // Kağıdın sol veya üst sınırından dışarı çıkmasını engelle (0'dan küçük olamaz)
                draggingControl.Left = Math.Max(0, nx);
                draggingControl.Top = Math.Max(0, ny);
            }
        }

        // Fare tıklaması bırakıldığında çalışır
        private void DesignControl_MouseUp(object sender, MouseEventArgs e)
        {
            // İşlem bittiğinde nesnenin son (X,Y,W,H) piksellerini milimetreye çevirip JSON modele (Tag) kalıcı olarak kaydet
            if (draggingControl != null && draggingControl.Tag is DesignItem item)
            {
                UpdateItemPositionFromControl(draggingControl, item);
            }

            isDragging = false;
            isResizing = false;

            if (draggingControl != null) draggingControl.Cursor = Cursors.SizeAll;
            draggingControl = null;
        }

        // Seçili nesnelerin arkasındaki mavi rengi kaldırıp listeyi boşaltır
        private void ClearSelection()
        {
            foreach (var ctrl in selectedControls)
            {
                if (ctrl is PictureBox) ctrl.BackColor = Color.LightGray;
                else ctrl.BackColor = Color.Transparent;
            }
            selectedControls.Clear();
        }

        // Tasarım kağıdındaki (DesignSurface) boş bir yere tıklanırsa seçimi temizle
        private void PnlDesignSurface_MouseDown(object sender, MouseEventArgs e)
        {
            ClearSelection();
            selectedDesignItem = null;
        }
        #endregion

        #region ⌨️ 05.2 KLAVYE KISAYOLLARI VE YÖN TUŞLARI
        // Form üzerindeki klavye basımlarını yakalayıp özel komutları çalıştırır
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // Sadece Yazdırma Ayarları sekmesindeysek ve bir nesne seçiliyse klavye komutlarını aktif et
            if (tabPrintSettings != null && tabPrintSettings.SelectedTab == tabPage11 && selectedControls.Count > 0)
            {
                // [CTRL + L] : Seçili Tüm Nesneleri Sola Hizala
                if (keyData == (Keys.Control | Keys.L))
                {
                    int minLeft = selectedControls.Min(c => c.Left); // En soldaki nesneyi bul
                    foreach (var c in selectedControls) { c.Left = minLeft; UpdateItemPositionFromControl(c, c.Tag as DesignItem); }
                    return true;
                }

                // [CTRL + T] : Seçili Tüm Nesneleri Üste Hizala
                if (keyData == (Keys.Control | Keys.T))
                {
                    int minTop = selectedControls.Min(c => c.Top); // En üstteki nesneyi bul
                    foreach (var c in selectedControls) { c.Top = minTop; UpdateItemPositionFromControl(c, c.Tag as DesignItem); }
                    return true;
                }

                // [DELETE] : Seçili Nesneleri Sil
                if (keyData == Keys.Delete)
                {
                    foreach (var c in selectedControls.ToList()) { selectedDesignItem = c.Tag as DesignItem; BtnDeleteItem_Click(null, null); }
                    ClearSelection();
                    return true;
                }

                // KLAVYE YÖN TUŞLARI İLE MİLİMETRİK TAŞIMA VE BOYUTLANDIRMA
                int dx = 0, dy = 0;
                bool isShift = (Control.ModifierKeys & Keys.Shift) == Keys.Shift; // Shift tuşuna basılıyor mu?

                // Basılan yöne göre X veya Y eksenindeki değişimi ayarla (+1 veya -1 piksel)
                if ((keyData & Keys.KeyCode) == Keys.Up) dy = -1;
                else if ((keyData & Keys.KeyCode) == Keys.Down) dy = 1;
                else if ((keyData & Keys.KeyCode) == Keys.Left) dx = -1;
                else if ((keyData & Keys.KeyCode) == Keys.Right) dx = 1;

                if (dx != 0 || dy != 0)
                {
                    foreach (var c in selectedControls)
                    {
                        // Eğer SHIFT ile birlikte yön tuşuna basılırsa NESNEYİ SÜNDÜR (Boyutlandır)
                        if (isShift) { c.Width = Math.Max(10, c.Width + dx); c.Height = Math.Max(10, c.Height + dy); }
                        // Sadece yön tuşuna basılırsa NESNEYİ TAŞI
                        else { c.Left += dx; c.Top += dy; }

                        UpdateItemPositionFromControl(c, c.Tag as DesignItem);
                    }
                    return true; // Komut başarıyla işlendi
                }
            }

            // İşlem yapılmadıysa diğer varsayılan tuş kombinasyonlarının (TAB, Enter) çalışmasına izin ver
            return base.ProcessCmdKey(ref msg, keyData);
        }
        #endregion

        #region 📐 05.3 MATEMATİKSEL DÖNÜŞÜM MOTORU (PX / MM)
        // Ekranda piksellerle hareket ettirilen fiziksel kutunun değerlerini,
        // JSON modelindeki kalıcı milimetrik değerlere (Xmm, Ymm vb.) çevirip Özellikler paneline aktarır.
        private void UpdateItemPositionFromControl(Control ctrl, DesignItem item)
        {
            using (var g = pnlDesignSurface.CreateGraphics())
            {
                item.Xmm = PxToMm(ctrl.Left, g);
                item.Ymm = PxToMm(ctrl.Top, g);
                item.Wmm = PxToMm(ctrl.Width, g);
                item.Hmm = PxToMm(ctrl.Height, g);

                // Değerleri yandaki numaratör kutularına (NumericUpDown) limitleri aşmayacak şekilde yazdır
                try
                {
                    if (numPropXmm != null) numPropXmm.Value = Math.Max(numPropXmm.Minimum, Math.Min(numPropXmm.Maximum, (decimal)item.Xmm));
                    if (numPropYmm != null) numPropYmm.Value = Math.Max(numPropYmm.Minimum, Math.Min(numPropYmm.Maximum, (decimal)item.Ymm));
                    if (numPropWmm != null) numPropWmm.Value = Math.Max(numPropWmm.Minimum, Math.Min(numPropWmm.Maximum, (decimal)item.Wmm));
                    if (numPropHmm != null) numPropHmm.Value = Math.Max(numPropHmm.Minimum, Math.Min(numPropHmm.Maximum, (decimal)item.Hmm));
                }
                catch { } // Limit hatalarını sessizce yut
            }
        }

        // Kullanılmayan yedek metot
        private void PointToMm(int px, out float mmX, out float mmY) { mmX = 0; mmY = 0; }

        // Farenin serbest hareketini, ızgara noktalarına (mıknatıs gibi) yapıştırır
        private int SnapToGrid(int px)
        {
            using (var g = pnlDesignSurface.CreateGraphics())
            {
                float gridPx = MmToPx((float)numGridMm.Value, g);
                return gridPx <= 0 ? px : (int)(Math.Round(px / gridPx) * gridPx);
            }
        }

        // Milimetreyi, donanımın mevcut DPI çözünürlüğüne göre piksele dönüştürür (1 İnç = 25.4 Mm)
        private int MmToPx(float mm, Graphics g) { return (int)Math.Round(mm * g.DpiX / 25.4f); }

        // Pikseli, milimetreye geri çevirir
        private float PxToMm(int px, Graphics g) { return px * 25.4f / g.DpiX; }

        // Ekranın anlık yatay DPI değerini alır
        private float GetScreenDpiX() { using (var g = pnlDesignSurface.CreateGraphics()) return g.DpiX; }

        // Grafik nesnesi olmadan DPI üzerinden manuel hesaplama metodu
        private float MmToPx(float mm, float dpi) => dpi / 25.4f * mm;
        #endregion

        #endregion

        // =========================================================================================

        #region 🖨️ 06. YAZICI SEÇİMİ VE SPOOLER MOTORU

        #region ⚙️ 06.1 YAZICI EŞLEŞTİRME (MAPPING) SİSTEMİ
        // Kullanıcının "Tekli Zarf" veya "Çoklu Zarf" gibi farklı ekranlar için varsayılan
        // yazıcıları seçmesini ve bu seçimlerin JSON dosyasına kaydedilmesini sağlar.
        private void InitializePrinterSettingsTab()
        {
            try
            {
                if (cmbPrinters == null || cmbPrintingPages == null) return;

                // Bilgisayara kurulu olan tüm donanımsal ve sanal yazıcıları listeye çek
                cmbPrinters.Items.Clear();
                foreach (string printer in PrinterSettings.InstalledPrinters)
                {
                    cmbPrinters.Items.Add(printer);
                }

                // Yazıcı atanabilecek sayfaları (modülleri) tanımla
                cmbPrintingPages.Items.Clear();
                cmbPrintingPages.Items.AddRange(new string[] { "Tekli Zarf Yazdırma", "Çoklu Zarf Yazdırma" });

                // Daha önce diske kaydedilmiş yazıcı ayarlarını (JSON) belleğe al
                LoadPrinterMappings();

                // Varsayılan olarak ilk sayfayı seç ve eventi bağla
                if (cmbPrintingPages.Items.Count > 0) cmbPrintingPages.SelectedIndex = 0;
                cmbPrintingPages.SelectedIndexChanged += CmbPrintingPages_SelectedIndexChanged;

                if (btnSavePrinterMapping != null) btnSavePrinterMapping.Click += BtnSavePrinterMapping_Click;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Yazıcı listesi yükleme hatası: " + ex.Message);
            }
        }

        // "Hangi sayfa için yazıcı seçeceksin?" kutusu değiştiğinde, eğer o sayfa için
        // önceden kaydedilmiş bir yazıcı varsa, sağ taraftaki yazıcı kutusunda onu otomatik seçer.
        private void CmbPrintingPages_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbPrintingPages.SelectedItem == null) return;

            string selectedPage = cmbPrintingPages.SelectedItem.ToString();
            if (printerMappings != null && printerMappings.ContainsKey(selectedPage))
            {
                cmbPrinters.SelectedItem = printerMappings[selectedPage];
            }
            else
            {
                cmbPrinters.SelectedIndex = -1;
            }
        }

        // Seçilen sayfa ve yazıcı eşleştirmesini kalıcı olması için JSON dosyasına kaydeder
        private void BtnSavePrinterMapping_Click(object sender, EventArgs e)
        {
            if (cmbPrintingPages.SelectedItem == null || cmbPrinters.SelectedItem == null)
            {
                MessageBox.Show("Lütfen önce bir sayfa ve bir yazıcı seçin!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string selectedPage = cmbPrintingPages.SelectedItem.ToString();
            string selectedPrinter = cmbPrinters.SelectedItem.ToString();

            // RAM'deki sözlüğü (Dictionary) güncelle
            printerMappings[selectedPage] = selectedPrinter;

            try
            {
                // Sözlüğü JSON formatına çevirip diske yaz
                string path = Path.Combine(GetTemplatesDirectory(), PrinterSettingsFile);
                string json = JsonConvert.SerializeObject(printerMappings, Formatting.Indented);
                File.WriteAllText(path, json);

                MessageBox.Show($"'{selectedPage}' için varsayılan yazıcı başarıyla atandı:\n{selectedPrinter}", "Ayar Kaydedildi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Yazıcı ayarı kaydedilirken hata oluştu: " + ex.Message);
            }
        }

        // JSON dosyasından yazıcı eşleştirmelerini okur
        private void LoadPrinterMappings()
        {
            try
            {
                string path = Path.Combine(GetTemplatesDirectory(), PrinterSettingsFile);
                if (File.Exists(path))
                {
                    printerMappings = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(path)) ?? new Dictionary<string, string>();
                }
            }
            catch
            {
                printerMappings = new Dictionary<string, string>();
            }
        }

        // Yazdırma işlemi başlamadan hemen önce, o sayfa için atanmış yazıcıyı PrintDocument nesnesine zorlar
        private void ApplyPrinterMapping(PrintDocument doc, string sayfaAdi)
        {
            if (printerMappings != null && printerMappings.ContainsKey(sayfaAdi))
            {
                string atananYazici = printerMappings[sayfaAdi];

                // Atanan yazıcı bilgisayardan silinmiş/kaldırılmış olabilir, kontrol et
                if (PrinterSettings.InstalledPrinters.Cast<string>().Contains(atananYazici))
                {
                    doc.PrinterSettings.PrinterName = atananYazici;
                }
            }
        }
        #endregion

        #region 📄 06.2 TEKLİ YAZDIRMA VE ÖNİZLEME (SINGLE PRINT)
        // Ekranda tasarlanan zarfın/etiketin kağıt üzerinde nasıl duracağını gösteren önizleme penceresini açar
        private void BtnPreview_Click(object sender, EventArgs e)
        {
            batchFirms = null; // Tekli yazdırma olduğu için çoklu listeyi sıfırla
            if (printDocument1 != null) { printDocument1.Dispose(); }
            printDocument1 = new PrintDocument();

            // Kağıt boyutlarını (Milimetre) al
            if (!float.TryParse(txtPageWidthMm.Text, out float pageW)) pageW = 220f;
            if (!float.TryParse(txtPageHeightMm.Text, out float pageH)) pageH = 110f;

            // YAZICI MATEMATİĞİ: Windows yazdırma sistemi inç'in yüzde biri (1/100 inch) ile çalışır.
            // Milimetreyi inçe çevirmek için 25.4'e bölüyoruz, sonra yazıcı için 100 ile çarpıyoruz.
            int printW = (int)(pageW * 100f / 25.4f);
            int printH = (int)(pageH * 100f / 25.4f);

            // Kağıt yatay mı dikey mi ayarla
            if (rbLandscape != null && rbLandscape.Checked)
            {
                // Yatayda yazıcıya her zaman Kısa Kenar x Uzun Kenar verilir, Landscape=true yapılarak kağıt sanal olarak döndürülür
                printDocument1.DefaultPageSettings.PaperSize = new PaperSize("OzelBoyut", Math.Min(printW, printH), Math.Max(printW, printH));
                printDocument1.DefaultPageSettings.Landscape = true;
            }
            else
            {
                printDocument1.DefaultPageSettings.PaperSize = new PaperSize("OzelBoyut", printW, printH);
                printDocument1.DefaultPageSettings.Landscape = false;
            }

            // Yazdırma olaylarını bağla
            printDocument1.PrintPage += PrintDocument1_PrintPage;
            printDocument1.BeginPrint += PrintDocument1_BeginPrint;

            // Arkaplandaki beyaz kağıdın görünümünü de güncelle
            ApplyDesignSurfaceSize(pageW, pageH, rbLandscape != null && rbLandscape.Checked);

            // Nesneleri milimetrik koordinatlarına göre kağıda yerleştir
            foreach (var item in designItems)
            {
                Control ctrl = pnlDesignSurface.Controls.Cast<Control>().FirstOrDefault(c => object.ReferenceEquals(c.Tag, item));
                if (ctrl != null) PlaceControlOnDesignSurface(ctrl, item);
            }

            // Windows'un standart önizleme penceresini aç
            printPreviewDialog1.Document = printDocument1;
            try { printPreviewDialog1.ShowDialog(); } catch { }
        }

        // Tasarlanan tek zarfı/etiketi, seçilen veya varsayılan yazıcıya direkt gönderir
        private void BtnPrint_Click(object sender, EventArgs e)
        {
            batchFirms = null; // Tekli yazdırma

            // Tasarımda dinamik veri alanı ({FirmaAdi}) varsa, örnek olarak kullanılacak firmayı seç
            var firma = GetSelectedFirmaForPreview();
            if (firma == null) { MessageBox.Show("Yazdırılacak firma seçin."); return; }
            currentPreviewFirma = firma;

            if (printDocument1 != null) { printDocument1.Dispose(); }
            printDocument1 = new PrintDocument();

            // Bu sekme için JSON'dan kaydedilmiş bir yazıcı varsa otomatik onu seç
            ApplyPrinterMapping(printDocument1, "Tekli Zarf Yazdırma");

            // Kağıt boyutlandırma işlemleri (Yukarıdaki önizleme metoduyla aynı mantık)
            if (!float.TryParse(txtPageWidthMm.Text, out float pageW)) pageW = 220f;
            if (!float.TryParse(txtPageHeightMm.Text, out float pageH)) pageH = 110f;

            int printW = (int)(pageW * 100f / 25.4f);
            int printH = (int)(pageH * 100f / 25.4f);

            if (rbLandscape != null && rbLandscape.Checked)
            {
                printDocument1.DefaultPageSettings.PaperSize = new PaperSize("OzelBoyut", Math.Min(printW, printH), Math.Max(printW, printH));
                printDocument1.DefaultPageSettings.Landscape = true;
            }
            else
            {
                printDocument1.DefaultPageSettings.PaperSize = new PaperSize("OzelBoyut", printW, printH);
                printDocument1.DefaultPageSettings.Landscape = false;
            }

            printDocument1.PrintPage += PrintDocument1_PrintPage;
            printDocument1.BeginPrint += PrintDocument1_BeginPrint;

            // İşlemi yazıcıya yolla!
            printDocument1.Print();
        }
        #endregion

        #region 📂 06.3 ÇOKLU YAZDIRMA (BATCH PRINTING)
        // Seçilen bir şablonu, listeden işaretlenen N tane firma için arka arkaya (loop) yazdırır
        private void btnCokluZarfYazdir_Click(object sender, EventArgs e)
        {
            if (lstSecilenFirmalar.CheckedItems.Count == 0) { MessageBox.Show("Lütfen firmaları işaretleyin."); return; }
            if (cmbPrintStyle.SelectedItem == null) { MessageBox.Show("Şablon seçin."); return; }

            // Seçilen şablon dosyasını bul ve JSON olarak oku
            string path = Path.Combine(GetTemplatesDirectory(), cmbPrintStyle.SelectedItem.ToString());
            if (!File.Exists(path)) return;

            var loadedTemplate = JsonConvert.DeserializeObject<TemplateFile>(File.ReadAllText(path));
            if (loadedTemplate == null) return;

            if (printDocument1 != null) { printDocument1.Dispose(); }
            printDocument1 = new PrintDocument();

            // Çoklu yazdırma ekranındaki ComboBox'tan yazıcı seçildiyse onu kullan, yoksa JSON'dan çek
            if (cmbCokluPrinter != null && cmbCokluPrinter.SelectedItem != null)
            {
                printDocument1.PrinterSettings.PrinterName = cmbCokluPrinter.SelectedItem.ToString();
            }
            else
            {
                ApplyPrinterMapping(printDocument1, "Çoklu Zarf Yazdırma");
            }

            // Şablonun içindeki milimetrik ayarları inç yüzdesine çevir
            int printW = (int)(loadedTemplate.PageWidthMm * 100f / 25.4f);
            int printH = (int)(loadedTemplate.PageHeightMm * 100f / 25.4f);
            bool isLandscape = (loadedTemplate.Orientation == "Landscape");

            if (isLandscape)
            {
                printDocument1.DefaultPageSettings.PaperSize = new PaperSize("OzelBoyut", Math.Min(printW, printH), Math.Max(printW, printH));
                printDocument1.DefaultPageSettings.Landscape = true;
            }
            else
            {
                printDocument1.DefaultPageSettings.PaperSize = new PaperSize("OzelBoyut", printW, printH);
                printDocument1.DefaultPageSettings.Landscape = false;
            }

            printDocument1.PrintPage += PrintDocument1_PrintPage;
            printDocument1.BeginPrint += PrintDocument1_BeginPrint;

            // Şablonun içindeki görsel nesneleri RAM'e al
            designItems = loadedTemplate.DesignItems ?? new List<DesignItem>();
            batchFirms = new List<Firma>();

            // Sağdaki listede (CheckedListBox) işaretlenen her bir firmanın ID'sini bulup veritabanından çek ve sıraya ekle
            foreach (var item in lstSecilenFirmalar.CheckedItems)
            {
                int id = int.Parse(item.ToString().Split('-')[0].Trim());
                var f = DataAccess.GetFirmaById(id);
                if (f != null) batchFirms.Add(f);
            }

            // Yazdırma indeksini sıfırla (0. firmadan başla)
            batchIndex = 0;

            // Güvenlik amacıyla direkt yazdırmak yerine çoklu önizleme aç
            printPreviewDialog1.Document = printDocument1;
            try { printPreviewDialog1.ShowDialog(); } catch { }
        }
        #endregion

        #region 🖨️ 06.4 ANA YAZDIRMA ÇEKİRDEĞİ (SPOOLER ENGINE)
        // Yazdırma işlemi başladığında indeksi sıfırlar
        private void PrintDocument1_BeginPrint(object sender, PrintEventArgs e) { batchIndex = 0; }

        // Bu metot, yazıcıya gönderilecek her bir kağıt için tekrar tekrar çalışır.
        // Çoklu yazdırmada 50 firma varsa, bu metot arkada 50 kez tetiklenir (HasMorePages mantığı ile)
        private void PrintDocument1_PrintPage(object sender, PrintPageEventArgs e)
        {
            try
            {
                float printerMmToPx = 100f / 25.4f;
                Firma firma;

                // EĞER ÇOKLU YAZDIRMAYSA: Sıradaki firmayı getir
                if (batchFirms != null && batchFirms.Count > 0)
                {
                    if (batchIndex < 0 || batchIndex >= batchFirms.Count) batchIndex = 0;
                    firma = batchFirms[batchIndex];
                }
                // EĞER TEKLİ YAZDIRMAYSA: Önizlemedeki tek firmayı kullan
                else
                {
                    firma = currentPreviewFirma ?? GetSelectedFirmaForPreview();
                }

                // Çizilecek hiçbir nesne yoksa işlemi bitir
                if (designItems == null || designItems.Count == 0) { e.HasMorePages = false; return; }

                // Kağıt üzerindeki tüm nesneleri (Etiket, Çerçeve, Resim vb.) tek tek çiz
                foreach (var item in designItems)
                {
                    float x = item.Xmm * printerMmToPx, y = item.Ymm * printerMmToPx, w = item.Wmm * printerMmToPx, h = item.Hmm * printerMmToPx;
                    RectangleF rect = new RectangleF(x, y, w, h);

                    // Çerçeve (Kare) Çizimi
                    if (item.Type == "Frame") { e.Graphics.DrawRectangle(Pens.Black, Rectangle.Round(rect)); continue; }

                    // Resim (Logo) Çizimi
                    if (item.Type == "Image")
                    {
                        if (File.Exists(item.Text))
                        {
                            try { using (var img = Image.FromFile(item.Text)) e.Graphics.DrawImage(img, rect); } catch { }
                        }
                        continue;
                    }

                    // Metin veya Dinamik Alan (Field) Çizimi
                    string text = "";
                    if (item.Type == "Field")
                    {
                        // Süslü parantezli değişkenleri ({FirmaAdi}), sıradaki firmanın gerçek bilgileriyle değiştir
                        if (item.PlaceholderKey == "FirmaAdi") text = firma.FirmaAdi;
                        else if (item.PlaceholderKey == "Adres") text = firma.Adres;
                        else if (item.PlaceholderKey == "Il") text = firma.Il;
                        else if (item.PlaceholderKey == "Telefon1") text = firma.Telefon1;
                        else if (item.PlaceholderKey == "Telefon2") text = firma.Telefon2;

                        if (string.IsNullOrWhiteSpace(text)) text = "";
                    }
                    else
                    {
                        // Normal sabit metin
                        text = item.Text;
                    }

                    // Eğer basılacak metin boşsa hiç font oluşturma, pas geç
                    if (string.IsNullOrWhiteSpace(text)) continue;

                    // Metni Font, Hizalama ve Renk ayarlarına göre kağıda bas (Graphics)
                    using (Font f = new Font(string.IsNullOrWhiteSpace(item.FontName) ? "Arial" : item.FontName, Math.Max(6f, item.FontSizePt), item.FontStyle))
                    {
                        StringFormat sf = new StringFormat
                        {
                            Alignment = item.Alignment == "Center" ? StringAlignment.Center : item.Alignment == "Right" ? StringAlignment.Far : StringAlignment.Near,
                            LineAlignment = StringAlignment.Near
                        };
                        sf.FormatFlags |= StringFormatFlags.NoClip;

                        Brush drawBrush = Brushes.Black;
                        try
                        {
                            Color c = ColorTranslator.FromHtml(item.ColorName);
                            if (c.ToKnownColor() != KnownColor.Black) drawBrush = new SolidBrush(c);
                        }
                        catch { }

                        // Eğer metin dikeyse (90 derece vb.) kağıt eksenini sanal olarak döndürüp öyle yazdır
                        if (item.Rotation != 0)
                        {
                            var state = e.Graphics.Save();
                            e.Graphics.TranslateTransform(rect.Left + rect.Width / 2f, rect.Top + rect.Height / 2f);
                            e.Graphics.RotateTransform(item.Rotation);
                            e.Graphics.DrawString(text, f, drawBrush, new RectangleF(-rect.Width / 2f, -rect.Height / 2f, rect.Width, rect.Height), sf);
                            e.Graphics.Restore(state);
                        }
                        else
                        {
                            // Düz (0 derece) metin
                            e.Graphics.DrawString(text, f, drawBrush, rect, sf);
                        }

                        if (drawBrush != Brushes.Black) drawBrush.Dispose();
                    }
                }

                // Çoklu yazdırmada (Batch) sırada başka firma var mı kontrolü
                if (batchFirms != null && batchFirms.Count > 0)
                {
                    batchIndex++;
                    // Eğer indeks firma sayısından küçükse "true" döner ve bu metot YENİ BİR SAYFA İÇİN baştan başlar
                    e.HasMorePages = (batchIndex < batchFirms.Count);
                }
                else
                {
                    e.HasMorePages = false; // Tekli yazdırmaysa işlemi bitir
                }
            }
            catch
            {
                e.HasMorePages = false; // Herhangi bir çökme olursa kağıt kusmasını engellemek için işlemi iptal et
            }
        }
        #endregion

        #endregion

        // =========================================================================================

        #region 🗃️ 07. FİRMA VE VERİTABANI YÖNETİMİ (CRUD / EXCEL)

        #region 📝 07.1 TEKİL FİRMA İŞLEMLERİ (EKLE, GÜNCELLE, SİL)
        // Yeni bir firma kaydı oluşturur ve veritabanına yazar
        private void btnKaydet_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtFirmaAdi.Text))
            {
                MessageBox.Show("Firma adı boş olamaz.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var f = new Firma
            {
                FirmaAdi = txtFirmaAdi.Text.Trim(),
                Adres = txtAdres.Text.Trim(),
                Il = txtIl.Text.Trim(),
                Telefon1 = txtTel1.Text.Trim(),
                Telefon2 = txtTel2.Text.Trim()
            };

            try
            {
                DataAccess.InsertFirma(f);
                MessageBox.Show("Firma başarıyla kaydedildi.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Ekranı Temizle ve Listeleri Yenile
                txtFirmaAdi.Clear();
                txtAdres.Clear();
                txtIl.Clear();
                txtTel1.Clear();
                txtTel2.Clear();
                LoadFirmalar();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Kayıt sırasında hata oluştu: " + ex.Message, "Kritik Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Seçili firmanın bilgilerini günceller
        private void btnGuncelle_Click(object sender, EventArgs e)
        {
            if (lstFirmalar.SelectedItem != null)
            {
                // "15 - Ahmet Yılmaz" şeklindeki veriden ID kısmını (15) çekip alıyoruz
                int id = int.Parse(lstFirmalar.SelectedItem.ToString().Split('-')[0].Trim());
                var f = new Firma
                {
                    Id = id,
                    FirmaAdi = txtEditFirmaAdi.Text.Trim(),
                    Adres = txtEditAdres.Text.Trim(),
                    Il = txtEditIl.Text.Trim(),
                    Telefon1 = txtEditTel1.Text.Trim(),
                    Telefon2 = txtEditTel2.Text.Trim()
                };

                DataAccess.UpdateFirma(f);
                MessageBox.Show("Firma başarıyla güncellendi.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadFirmalar();
            }
        }

        // Seçili firmayı veritabanından kalıcı olarak siler
        private void btnSil_Click(object sender, EventArgs e)
        {
            if (lstFirmalar.SelectedItem != null)
            {
                if (MessageBox.Show("Seçilen firmayı tamamen silmek istediğinize emin misiniz?", "Firma Sil", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    int id = int.Parse(lstFirmalar.SelectedItem.ToString().Split('-')[0].Trim());
                    DataAccess.DeleteFirma(id);
                    MessageBox.Show("Firma başarıyla silindi.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    txtEditFirmaAdi.Clear();
                    txtEditAdres.Clear();
                    txtEditIl.Clear();
                    txtEditTel1.Clear();
                    txtEditTel2.Clear();
                    LoadFirmalar();
                }
            }
            else
            {
                MessageBox.Show("Lütfen silinecek firmayı sağdaki listeden seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        #endregion

        #region 📋 07.2 LİSTELEME VE ARAMA MOTORU
        // Veritabanındaki tüm firmaları form üzerindeki basit ListBox'a VE DGV Tablosuna doldurur
        private void LoadFirmalar()
        {
            // 1. Önce hem sağdaki listeyi hem de soldaki koca tabloyu temizle
            if (lstFirmalar != null) lstFirmalar.Items.Clear();
            if (dgvFirmalar != null) dgvFirmalar.Rows.Clear();

            // 2. Veritabanından tüm firmaları çek
            var firmalar = DataAccess.GetAllFirmalar();

            // 3. Çekilen her bir firma için döngüye gir
            foreach (var f in firmalar)
            {
                // Sağ taraftaki ListBox'a ekleme (Eski Kod)
                if (lstFirmalar != null)
                {
                    lstFirmalar.Items.Add($"{f.Id} - {f.FirmaAdi}");
                }

                // 🌟 YENİ: Sol taraftaki devasa DataGridView tablosuna satır satır ekleme
                if (dgvFirmalar != null)
                {
                    // Not: Tablonda ID, Firma Adı, Adres, İl, Telefon1, Telefon2 şeklinde 6 sütun olmalı!
                    dgvFirmalar.Rows.Add(f.Id, f.FirmaAdi, f.Adres, f.Il, f.Telefon1, f.Telefon2);
                }
            }
        }

        // ListBox'tan bir firma seçildiğinde, bilgilerini düzenleme (Edit) kutularına doldurur
        private void lstFirmalar_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstFirmalar.SelectedItem != null)
            {
                int id = int.Parse(lstFirmalar.SelectedItem.ToString().Split('-')[0].Trim());
                var f = DataAccess.GetFirmaById(id);

                if (f != null)
                {
                    txtEditFirmaAdi.Text = f.FirmaAdi;
                    txtEditAdres.Text = f.Adres;
                    txtEditIl.Text = f.Il;
                    txtEditTel1.Text = f.Telefon1;
                    txtEditTel2.Text = f.Telefon2;
                }
            }
        }

        // Çift tıklama olayında da aynı seçme işlemini tetikle
        private void lstFirmalar_DoubleClick(object sender, EventArgs e)
        {
            lstFirmalar_SelectedIndexChanged(sender, e);
        }

        // Veritabanındaki tüm firmaları DataGridView tablosuna (Zarf yazdırma ekranı için) yansıtır
        private void LoadZarfFirmalar()
        {
            if (dgvZarfFirmalar == null) return;
            dgvZarfFirmalar.Rows.Clear();
            var firmalar = DataAccess.GetAllFirmalar();
            foreach (var f in firmalar)
            {
                dgvZarfFirmalar.Rows.Add(f.Id, f.FirmaAdi, f.Adres, f.Il, f.Telefon1, f.Telefon2);
            }
        }

        private void btnZarfYenile_Click(object sender, EventArgs e) { LoadZarfFirmalar(); }
        private void btnListele_Click(object sender, EventArgs e) { LoadZarfFirmalar(); } // Aynı amaca hizmet ediyor

        // Zarf sekmesindeki arama kutusuna göre DataGridView'i dinamik olarak filtreler
        private void btnAra_Click(object sender, EventArgs e)
        {
            if (dgvZarfFirmalar == null) return;

            string aranan = txtAramaFirmaAdi.Text.Trim().ToLower();
            dgvZarfFirmalar.Rows.Clear();
            var firmalar = DataAccess.GetAllFirmalar();

            foreach (var f in firmalar)
            {
                if (f.FirmaAdi.ToLower().Contains(aranan))
                {
                    dgvZarfFirmalar.Rows.Add(f.Id, f.FirmaAdi, f.Adres, f.Il, f.Telefon1, f.Telefon2);
                }
            }

            if (dgvZarfFirmalar.Rows.Count == 0) MessageBox.Show("Aramanıza uygun firma bulunamadı.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        #endregion

        #region 🗃️ 07.3 ÇOKLU SEÇİM VE TOPLU AKTARIM (BATCH & EXCEL)
        // Ana tablodan çift tıklanan firmayı, "Çoklu Yazdırılacaklar" sağ listesine atar
        private void dgvZarfFirmalar_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                var row = dgvZarfFirmalar.Rows[e.RowIndex];
                string item = $"{row.Cells[0].Value} - {row.Cells[1].Value}";
                if (!lstSecilenFirmalar.Items.Contains(item)) lstSecilenFirmalar.Items.Add(item, true);
            }
        }

        // "Çoklu Yazdırılacaklar" listesinden seçili öğeyi çıkarır
        private void btnCikar_Click(object sender, EventArgs e)
        {
            if (lstSecilenFirmalar.SelectedItem != null)
                lstSecilenFirmalar.Items.Remove(lstSecilenFirmalar.SelectedItem);
            else
                MessageBox.Show("Lütfen listeden çıkarmak istediğiniz firmayı seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        // "Çoklu Yazdırılacaklar" listesini tamamen temizler
        private void btnTemizle_Click(object sender, EventArgs e)
        {
            lstSecilenFirmalar.Items.Clear();
        }

        // Dışarıdan bir CSV veya Metin belgesindeki yüzlerce firmayı tek tıkla veritabanına ekler
        private void btnTopluAktar_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "CSV Dosyaları (*.csv)|*.csv|Metin Dosyaları (*.txt)|*.txt|Tüm Dosyalar (*.*)|*.*";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        // Dosyayı Türkçe karakter uyumlu (Default Encoding) şekilde satır satır oku
                        string[] satirlar = File.ReadAllLines(ofd.FileName, System.Text.Encoding.Default);
                        int eklenenSayisi = 0;

                        foreach (string satir in satirlar)
                        {
                            if (string.IsNullOrWhiteSpace(satir)) continue;

                            // Excel'den gelen gizli tırnak işaretlerini (") temizle
                            string temizSatir = satir.Replace("\"", "");

                            // Satırı noktalı virgüllere göre hücrelere (sütunlara) ayır
                            string[] hucreler = temizSatir.Split(';');

                            // ZIRH: Eğer ilk sütunun içinde "firma" kelimesi geçiyorsa bu başlık satırıdır, pas geç!
                            if (hucreler.Length > 0 && hucreler[0].Trim().ToLower().Contains("firma")) continue;

                            if (hucreler.Length > 0 && !string.IsNullOrWhiteSpace(hucreler[0]))
                            {
                                var f = new Firma();
                                f.FirmaAdi = hucreler[0].Trim();
                                f.Adres = hucreler.Length > 1 ? hucreler[1].Trim() : "";
                                f.Il = hucreler.Length > 2 ? hucreler[2].Trim() : "";
                                f.Telefon1 = hucreler.Length > 3 ? hucreler[3].Trim() : "";
                                f.Telefon2 = hucreler.Length > 4 ? hucreler[4].Trim() : "";

                                DataAccess.InsertFirma(f);
                                eklenenSayisi++;
                            }
                        }

                        LoadFirmalar();
                        MessageBox.Show($"{eklenenSayisi} adet firma başarıyla veritabanına işlendi!", "İşlem Tamam", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Dosya okunurken bir hata oluştu. Lütfen formatın (FirmaAdı;Adres;İl...) şeklinde olduğundan emin olun.\n\nHata Detayı: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        // Geliştirici/Test ortamında boş yazdırmaları engellemek için kullanılan sanal (dummy) firma
        private Firma GetSelectedFirmaForPreview()
        {
            return new Firma
            {
                FirmaAdi = "Merko Mobilya",
                Adres = "Turgutreis Mh. Tarakçı Cd. No:19",
                Il = "Ümraniye / İstanbul",
                Telefon1 = "0535 821 7164",
                Telefon2 = ""
            };
        }

        // ⚠️ TEHLİKELİ İŞLEM: Veritabanındaki tüm firma kayıtlarını sıfırlar (DROP/DELETE)
        private void btnTumFirmalariSil_Click(object sender, EventArgs e)
        {
            DialogResult ilkCevap = MessageBox.Show(
                "DİKKAT: Veritabanındaki KAYITLI TÜM FİRMALAR kalıcı olarak silinecektir!\n\nBu işlemi geri alamazsınız. Devam etmek istediğinize emin misiniz?",
                "Kritik Toplu Silme Onayı",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2); // Varsayılan olarak 'Hayır' butonunu seçili getir ki yanlışlıkla Enter'a basılmasın

            if (ilkCevap == DialogResult.Yes)
            {
                try
                {
                    DataAccess.DeleteAllFirmalar();

                    // Sadece veritabanını değil, arayüzdeki (UI) tüm listeleri de temizle
                    if (lstFirmalar != null) lstFirmalar.Items.Clear();
                    if (dgvAmbarTumFirmalar != null) dgvAmbarTumFirmalar.Rows.Clear();
                    if (dgvZarfFirmalar != null) dgvZarfFirmalar.Rows.Clear();
                    if (lstSecilenFirmalar != null) lstSecilenFirmalar.Items.Clear();
                    if (dgvAmbarSecilenFirmalar != null) dgvAmbarSecilenFirmalar.Rows.Clear();
                    if (dgvAmbarSonListe != null) dgvAmbarSonListe.Rows.Clear();

                    txtEditFirmaAdi.Clear(); txtEditAdres.Clear(); txtEditIl.Clear(); txtEditTel1.Clear(); txtEditTel2.Clear();

                    MessageBox.Show("Veritabanındaki tüm firma kayıtları başarıyla sıfırlandı!", "İşlem Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Silme işlemi sırasında veritabanı kaynaklı bir hata oluştu:\n\n" + ex.Message, "Kritik Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        #endregion

        #endregion

        // =========================================================================================

        #region 📂 08. ŞABLON DOSYA YÖNETİMİ (JSON I/O)

        #region 📁 08.1 KLASÖR VE LİSTE YÖNETİMİ
        // Kullanıcının tasarladığı şablonların güvenli bir şekilde saklanacağı klasör yolunu döndürür.
        // AppData klasörünü kullanıyoruz ki program güncellense veya yeri değişse bile şablonlar silinmesin.
        private string GetTemplatesDirectory()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string templatesDir = Path.Combine(appData, "TamgaApp", "Templates");

            // Eğer böyle bir klasör bilgisayarda henüz yoksa (program ilk kez açılıyorsa) oluştur
            if (!Directory.Exists(templatesDir)) Directory.CreateDirectory(templatesDir);

            return templatesDir;
        }

        // Güvenli klasördeki (AppData) tüm ".json" uzantılı şablon dosyalarını bulur ve arayüzdeki listelere doldurur
        private void LoadTemplateList()
        {
            string templatesDir = GetTemplatesDirectory();

            // Klasördeki tüm geçerli şablon dosyalarını oku
            var files = Directory.GetFiles(templatesDir, "*.json")
                                 .Select(Path.GetFileName)
                                 .Where(f => f != PrinterSettingsFile)
                                 .OrderBy(n => n)
                                 .ToArray();

            // Tüm ilgili arayüz elemanlarını sıfırla
            lstTemplates.Items.Clear();
            cmbPrintStyle.Items.Clear();

            // 🌟 TAM SENKRONİZASYON ZIRHI: Manuel şablon kutusunu da buraya bağlıyoruz
            if (cmbManualTemplate != null) cmbManualTemplate.Items.Clear();

            if (files.Length > 0)
            {
                // Verileri tüm listelere tek tıkla topluca dağıtıyoruz
                lstTemplates.Items.AddRange(files);
                cmbPrintStyle.Items.AddRange(files);

                if (cmbManualTemplate != null)
                {
                    cmbManualTemplate.Items.AddRange(files);
                    // Eğer hiçbir şey seçili değilse otomatik olarak ilk şablonu seçili getir
                    if (cmbManualTemplate.SelectedIndex == -1) cmbManualTemplate.SelectedIndex = 0;
                }

                if (cmbPrintStyle.Items.Count > 0 && cmbPrintStyle.SelectedIndex == -1)
                    cmbPrintStyle.SelectedIndex = 0;
            }
        }
        #endregion

        #region 💾 08.2 ŞABLON KAYDETME VE SİLME (SAVE / DELETE)
        // Kağıt üzerindeki tüm tasarımı (koordinatlar, renkler, boyutlar) JSON formatına çevirip diske kaydeder
        private void BtnSaveTemplate_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog sfd = new SaveFileDialog { InitialDirectory = GetTemplatesDirectory(), Title = "Şablonu Hangi İsimle Kaydetmek İstersiniz?", Filter = "Şablon Dosyası (*.json)|*.json", DefaultExt = "json", FileName = "Yeni_Zarf_Sablonu" })
            {
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string fileName = Path.GetFileName(sfd.FileName);

                        // Kaydedilecek veriyi oluştur (Kağıt ölçüleri, yönü ve üzerindeki nesneler)
                        var tf = new TemplateFile
                        {
                            TemplateName = Path.GetFileNameWithoutExtension(fileName),
                            PageWidthMm = float.TryParse(txtPageWidthMm.Text, out float w) ? w : 220,
                            PageHeightMm = float.TryParse(txtPageHeightMm.Text, out float h) ? h : 110,
                            Orientation = rbLandscape.Checked ? "Landscape" : "Portrait",
                            CreatedAt = DateTime.Now,
                            DesignItems = designItems // Ekrandaki tüm nesneleri RAM'den JSON listesine aktar
                        };

                        // JSON'a dönüştür ve dosyayı yaz (Formatting.Indented = Okunabilir, düzenli JSON formatı)
                        File.WriteAllText(Path.Combine(GetTemplatesDirectory(), fileName), JsonConvert.SerializeObject(tf, Formatting.Indented));

                        LoadTemplateList(); // Listeyi güncelle ki yeni kaydedilen şablon ekranda görünsün
                        MessageBox.Show($"Şablon başarıyla kaydedildi:\n{fileName}", "Kayıt Tamamlandı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Şablon kaydedilirken bir hata oluştu: " + ex.Message, "Kritik Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        // Seçili şablon dosyasını bilgisayardan kalıcı olarak siler
        private void btnDeleteTemplate_Click(object sender, EventArgs e)
        {
            if (lstTemplates.SelectedItem == null)
            {
                MessageBox.Show("Lütfen silmek istediğiniz şablonu listeden seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string selectedTemplate = lstTemplates.SelectedItem.ToString();

            // Silmeden önce kullanıcıdan son bir onay al
            if (MessageBox.Show($"'{selectedTemplate}' adlı şablonu kalıcı olarak silmek istediğinize emin misiniz?", "Şablon Sil", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                try
                {
                    string fullPath = Path.Combine(GetTemplatesDirectory(), selectedTemplate);
                    if (File.Exists(fullPath)) File.Delete(fullPath); // Dosyayı diskten uçur

                    LoadTemplateList(); // Listeyi güncelle
                    MessageBox.Show("Şablon başarıyla silindi.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Şablon silinirken bir hata oluştu: " + ex.Message, "Kritik Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        #endregion

        #region 🔄 08.3 ŞABLON YÜKLEME (İÇE AKTARMA)
        // Listeden seçilen hazır şablonu (JSON dosyasını) okuyup tasarım ekranına (Kağıda) yerleştirir
        private void BtnLoadTemplate_Click(object sender, EventArgs e)
        {
            if (lstTemplates.SelectedItem == null)
            {
                MessageBox.Show("Lütfen açmak için bir şablon seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string path = Path.Combine(GetTemplatesDirectory(), lstTemplates.SelectedItem.ToString());
            if (!File.Exists(path))
            {
                MessageBox.Show("Şablon dosyası diskte bulunamadı! Silinmiş veya yeri değiştirilmiş olabilir.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // JSON'u oku ve C# modeline (TemplateFile) çevir
            var loaded = JsonConvert.DeserializeObject<TemplateFile>(File.ReadAllText(path));
            if (loaded == null)
            {
                MessageBox.Show("Şablon dosyası bozuk veya yüklenemedi.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Kağıt boyutlarını ve yönünü şablondaki ayarlara göre değiştir
            txtPageWidthMm.Text = loaded.PageWidthMm.ToString();
            txtPageHeightMm.Text = loaded.PageHeightMm.ToString();

            if (loaded.Orientation == "Landscape") rbLandscape.Checked = true;
            else rbPortrait.Checked = true;

            // RAM'deki mevcut nesne listesini boşalt ve şablondan gelenleri yükle
            designItems = loaded.DesignItems ?? new List<DesignItem>();

            // Kağıdın üzerindeki eski fiziksel çizimleri temizle
            pnlDesignSurface.Controls.Clear();

            // Şablondaki her bir nesne için yeni bir fiziksel kutucuk (Control) oluştur
            foreach (var item in designItems) CreateControlForDesignItem(item);
        }

        // Flash bellekten, e-postadan veya başka bir bilgisayardan gelen harici bir .json şablonunu sisteme aktarır
        private void btnDisSablonYukle_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog { InitialDirectory = GetTemplatesDirectory(), Filter = "Şablon Dosyası (*.json)|*.json", Title = "Yüklenecek Harici Şablon Dosyasını Seçin" })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        // Dışarıdan gelen dosyayı test et (Bozuk mu, geçerli mi?)
                        var loaded = JsonConvert.DeserializeObject<TemplateFile>(File.ReadAllText(ofd.FileName));
                        if (loaded == null)
                        {
                            MessageBox.Show("Seçilen şablon dosyası geçersiz veya okunamadı.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }

                        // Dışarıdan gelen şablonu ekrana yansıt
                        txtPageWidthMm.Text = loaded.PageWidthMm.ToString();
                        txtPageHeightMm.Text = loaded.PageHeightMm.ToString();

                        if (loaded.Orientation == "Landscape") rbLandscape.Checked = true;
                        else rbPortrait.Checked = true;

                        designItems = loaded.DesignItems ?? new List<DesignItem>();
                        pnlDesignSurface.Controls.Clear();

                        foreach (var item in designItems) CreateControlForDesignItem(item);

                        MessageBox.Show($"'{Path.GetFileName(ofd.FileName)}' adlı şablon dışarıdan başarıyla yüklendi! Lütfen listeye kalıcı olarak eklemek için 'Farklı Kaydet' işlemini yapın.", "Şablon Hazır", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Şablon yüklenirken sistemsel hata oluştu: " + ex.Message, "Kritik Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
        #endregion

        #endregion

        // =========================================================================================

        #region 🚀 09. MANUEL ETİKET VE ÖZEL YAZDIRMA

        #region 🖨️ 09.1 MANUEL YAZDIRMA MOTORU (MANUAL PRINT)
        // Veritabanına bağlı kalmadan, kullanıcının ekrandaki kutucuklara elle girdiği bilgileri
        // (Firma, Adres vb.) anında bir şablonla birleştirip yazdırmaya veya önizlemeye yarar.
        private void RunManualPrint(bool isPreview)
        {
            // Şablon seçilmemişse işlemi durdur
            if (cmbManualTemplate.SelectedItem == null)
            {
                MessageBox.Show("Lütfen önce bir şablon seçin!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Seçilen şablonun dosya yolunu bul ve varlığını doğrula
            string path = Path.Combine(GetTemplatesDirectory(), cmbManualTemplate.SelectedItem.ToString());
            if (!File.Exists(path)) return;

            // JSON dosyasından şablon tasarımını RAM'e yükle
            var loadedTemplate = JsonConvert.DeserializeObject<TemplateFile>(File.ReadAllText(path));

            // Ekrandaki TextBox'lardan geçici (sanal) bir firma oluştur
            Firma manualFirma = new Firma
            {
                FirmaAdi = txtManFirma.Text.Trim(),
                Adres = txtManAdres.Text.Trim(),
                Il = txtManIl.Text.Trim(),
                Telefon1 = txtManTel1.Text.Trim(),
                Telefon2 = txtManTel2.Text.Trim()
            };

            // Çoklu yazdırma listesini sadece bu geçici firmayla doldur (Tek sayfa basılacak)
            batchFirms = new List<Firma> { manualFirma };
            batchIndex = 0;

            // Yazıcı motorunu sıfırla ve yeniden oluştur
            if (printDocument1 != null) printDocument1.Dispose();
            printDocument1 = new PrintDocument();

            // Manuel yazdırma sekmesindeki ComboBox'tan yazıcı seçildiyse onu kullan,
            // yoksa genel ayarlardan (JSON) varsayılanı çek.
            if (cmbManuelPrinter != null && cmbManuelPrinter.SelectedItem != null)
            {
                printDocument1.PrinterSettings.PrinterName = cmbManuelPrinter.SelectedItem.ToString();
            }
            else
            {
                ApplyPrinterMapping(printDocument1, "Tekli Zarf Yazdırma");
            }

            // Şablonun milimetrik kağıt boyutlarını inç yüzdesine çevir
            int printW = (int)(loadedTemplate.PageWidthMm * 100f / 25.4f);
            int printH = (int)(loadedTemplate.PageHeightMm * 100f / 25.4f);
            bool isLandscape = (loadedTemplate.Orientation == "Landscape");

            // Kağıt yatay/dikey ayarlarını yazıcıya bildir
            if (isLandscape)
            {
                printDocument1.DefaultPageSettings.PaperSize = new PaperSize("Ozel", Math.Min(printW, printH), Math.Max(printW, printH));
                printDocument1.DefaultPageSettings.Landscape = true;
            }
            else
            {
                printDocument1.DefaultPageSettings.PaperSize = new PaperSize("Ozel", printW, printH);
                printDocument1.DefaultPageSettings.Landscape = false;
            }

            // Şablondaki çizilecek nesneleri (DesignItems) motorun kullanması için değişkenlere aktar
            designItems = loadedTemplate.DesignItems ?? new List<DesignItem>();

            // Yazdırma görevlerini arka plandaki spooler olaylarına bağla
            printDocument1.PrintPage += PrintDocument1_PrintPage;
            printDocument1.BeginPrint += PrintDocument1_BeginPrint;

            // Parametreye göre Önizleme penceresini aç veya doğrudan yazıcıya gönder
            if (isPreview)
            {
                printPreviewDialog1.Document = printDocument1;
                try { printPreviewDialog1.ShowDialog(); } catch { }
            }
            else
            {
                printDocument1.Print();
            }
        }
        #endregion

        #region 🧹 09.2 TASARIM TEMİZLİĞİ (CLEAR DESIGN)
        // Tasarım ekranındaki beyaz kağıdın üzerindeki her şeyi tamamen siler ve sıfırlar
        private void BtnTemizleTasarm_Click(object sender, EventArgs e)
        {
            // Yanlışlıkla basılmalara karşı uyarı zırhı
            if (MessageBox.Show("Tüm tasarım nesnelerini silmek istediğinize emin misiniz? Bu işlem geri alınamaz!",
                "Tasarımı Sıfırla", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                designItems.Clear();                  // RAM'deki nesne listesini (JSON Kalıbını) boşalt
                pnlDesignSurface.Controls.Clear();    // Fiziksel kutuları kağıttan (UI) sil
                selectedControls.Clear();             // Fareyle seçili kalan nesneleri unut
                selectedDesignItem = null;            // Özellikler panelindeki aktif seçimi sıfırla

                pnlDesignSurface.Invalidate();        // Kağıdın görüntüsünü tazele

                MessageBox.Show("Tasarım ekranı başarıyla temizlendi!", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        #endregion

        #endregion

        // =========================================================================================

        #region 🏭 10. ÜRETİM VE BARKOD TAKİP SİSTEMİ

        #region 📦 10.1 BARKOD OKUTMA VE ÜRETİM LİSTESİ
        // El terminali (Barkod Okuyucu) ile okutulan ürünün veritabanında olup olmadığını
        // kontrol eder ve üretim listesine (DataGridView) yansıtır.
        private void txtBarkodOkut_KeyDown(object sender, KeyEventArgs e)
        {
            // Cihaz barkodu okuduktan sonra otomatik ENTER tuşuna basar
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true; // Sistemin "ding" diye hata sesi vermesini engelle
                string okunanBarkod = txtBarkodOkut.Text.Trim();

                if (!string.IsNullOrEmpty(okunanBarkod))
                {
                    // Barkodu veritabanında ara
                    Urun bulunanUrun = DataAccess.GetUrunByBarkod(okunanBarkod);

                    // Ürün veritabanında (Excel aktarımında) yoksa "Kayıtsız" olarak işaretle
                    if (bulunanUrun == null)
                    {
                        bulunanUrun = new Urun { UrunKodu = "KAYITSIZ", Aciklama = "SİSTEMDE BULUNAMADI!", Barkod = okunanBarkod };
                    }

                    bool varMi = false;
                    // Okutulan ürün halihazırda listeye eklenmiş mi kontrol et
                    foreach (DataGridViewRow row in dgvUretim.Rows)
                    {
                        if (row.Cells[3].Value != null && row.Cells[3].Value.ToString() == okunanBarkod)
                        {
                            // Ürün listede varsa, sadece adet miktarını 1 arttır
                            int mevcutAdet = Convert.ToInt32(row.Cells[2].Value);
                            row.Cells[2].Value = mevcutAdet + 1;
                            varMi = true;
                            break;
                        }
                    }

                    // Ürün listede ilk kez okutuluyorsa, yeni bir satır olarak ekle
                    if (!varMi)
                    {
                        dgvUretim.Rows.Add(bulunanUrun.UrunKodu, bulunanUrun.Aciklama, 1, bulunanUrun.Barkod);
                    }
                }

                // Barkod kutusunu yeni okutma için temizle ve odakla
                txtBarkodOkut.Clear();
                txtBarkodOkut.Focus();
            }
        }

        // Listeden seçilen veya üstüne tıklanan hatalı/fazla okutulmuş ürünü siler
        private void btnSecileniSil_Click(object sender, EventArgs e)
        {
            if (dgvUretim.Rows.Count == 0) return;

            // Satırın tamamı seçiliyse
            if (dgvUretim.SelectedRows.Count > 0)
            {
                foreach (DataGridViewRow row in dgvUretim.SelectedRows)
                {
                    if (!row.IsNewRow) dgvUretim.Rows.Remove(row);
                }
            }
            // Sadece bir hücrenin içine tıklandıysa (Satır seçili değilse bile o satırı sil)
            else if (dgvUretim.CurrentCell != null)
            {
                int seciliSatirIndeksi = dgvUretim.CurrentCell.RowIndex;
                if (!dgvUretim.Rows[seciliSatirIndeksi].IsNewRow)
                {
                    dgvUretim.Rows.RemoveAt(seciliSatirIndeksi);
                }
            }
            else
            {
                MessageBox.Show("Lütfen silmek istediğiniz kalemi tablodan seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            txtBarkodOkut.Focus(); // Silme işleminden sonra hemen barkod okutmaya devam edilebilmesi için
        }
        #endregion

        #region 💾 10.2 ÜRETİM RAPORUNU KAYDETME VE YAZDIRMA
        // Ekrandaki üretilmiş/okutulmuş ürün listesini bir CSV (Excel destekli) dosyasına kaydeder
        private void btnUretimKaydet_Click(object sender, EventArgs e)
        {
            if (dgvUretim.Rows.Count == 0) { MessageBox.Show("Tabloda okutulmuş ürün yok!"); return; }

            DateTime secilenTarih = dtpUretimTarihi.Value;

            // Kayıt yerini belirle (Kullanıcı seçmemişse Masaüstüne klasör aç)
            string anaYol = string.IsNullOrWhiteSpace(txtKayitYeri.Text)
                ? System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Günlük Üretim Takip")
                : txtKayitYeri.Text;

            // Düzenli arşivleme için Yıl/Ay/Gün şeklinde içiçe klasörler oluştur
            string yil = secilenTarih.ToString("yyyy");
            string ay = secilenTarih.ToString("MMMM");
            string gun = secilenTarih.ToString("dd-MM-yyyy");

            string tamHedefKlasor = Path.Combine(anaYol, yil, ay, gun);
            if (!Directory.Exists(tamHedefKlasor)) Directory.CreateDirectory(tamHedefKlasor);

            // Aynı günde birden fazla vardiya kaydı olabileceği için ismine saat-dakika-saniye ekle
            string dosyaAdi = $"Uretim_{DateTime.Now.ToString("HHmmss")}.csv";
            string dosyaYolu = Path.Combine(tamHedefKlasor, dosyaAdi);

            // Dosyayı Türkçe karakter destekli (UTF8) oluştur ve satırları yazdır
            using (StreamWriter sw = new StreamWriter(dosyaYolu, false, System.Text.Encoding.UTF8))
            {
                sw.WriteLine("Ürün Kodu;Ürün Açıklaması;Ürün Adeti;Ürün Barkodu"); // Excel Sütun Başlıkları
                foreach (DataGridViewRow row in dgvUretim.Rows)
                {
                    if (row.Cells[0].Value != null)
                    {
                        sw.WriteLine($"{row.Cells[0].Value};{row.Cells[1].Value};{row.Cells[2].Value};{row.Cells[3].Value}");
                    }
                }
            }

            // Kullanıcı "Kaydederken A4 Kağıda Yazdır" tikini işaretlediyse yazdırma motoruna gönder
            if (chkYazdir != null && chkYazdir.Checked)
            {
                UretimListesiYazdir(secilenTarih);
            }

            MessageBox.Show($"Üretim verileri başarıyla kaydedildi!\nYol: {dosyaYolu}", "Kayıt Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);

            dgvUretim.Rows.Clear(); // Bir sonraki vardiya için ekranı temizle
        }

        // Üretim raporunu (A4 boyutunda standart yazıcıdan) kağıda döken Spooler Motoru
        private void UretimListesiYazdir(DateTime secilenTarih)
        {
            pdUretim = new System.Drawing.Printing.PrintDocument();

            // Üretim sayfasındaki özel listeden seçilen yazıcıyı kullan
            if (cmbUretimYazici != null && cmbUretimYazici.SelectedItem != null)
            {
                pdUretim.PrinterSettings.PrinterName = cmbUretimYazici.SelectedItem.ToString();
            }

            pdUretim.PrintPage += PdUretim_PrintPage;
            pdUretim.Print(); // Gönder!
        }

        // Yazıcının kağıda atacağı siyah mürekkepleri ve koordinatlarını çizen metot
        private void PdUretim_PrintPage(object sender, System.Drawing.Printing.PrintPageEventArgs e)
        {
            Graphics g = e.Graphics;
            Font baslikFont = new Font("Arial", 16, FontStyle.Bold);
            Font icerikFont = new Font("Arial", 10);
            Font barkodFont = new Font("Arial", 12, FontStyle.Bold);

            int y = 50; // Kağıdın en üstünden 50px boşluk bırak
            string baslik = $"{dtpUretimTarihi.Value.ToString("dd.MM.yyyy")} Mamul Depo Kabul Listesi";

            g.DrawString(baslik, baslikFont, Brushes.Black, 50, y);
            y += 40;

            // Sütun Başlıkları
            g.DrawString("KOD", icerikFont, Brushes.Black, 50, y);
            g.DrawString("AÇIKLAMA", icerikFont, Brushes.Black, 150, y);
            g.DrawString("ADET", icerikFont, Brushes.Black, 450, y);
            g.DrawString("BARKOD", icerikFont, Brushes.Black, 520, y);
            y += 20;

            // Başlığın altına yatay ayırıcı bir çizgi çek
            g.DrawLine(Pens.Black, 50, y, 750, y);
            y += 10;

            // Listeyi döngüye al ve her satırı kağıda yazdır
            foreach (DataGridViewRow row in dgvUretim.Rows)
            {
                if (row.IsNewRow) continue;

                string kod = row.Cells[0].Value?.ToString() ?? "";
                string aciklama = row.Cells[1].Value?.ToString() ?? "";
                string adet = row.Cells[2].Value?.ToString() ?? "";
                string barkod = row.Cells[3].Value?.ToString() ?? "";

                // Açıklama çok uzunsa ve diğer sütunlara taşacaksa sonunu "..." yaparak kes
                if (aciklama.Length > 35) aciklama = aciklama.Substring(0, 35) + "...";

                g.DrawString(kod, icerikFont, Brushes.Black, 50, y);
                g.DrawString(aciklama, icerikFont, Brushes.Black, 150, y);
                g.DrawString(adet, icerikFont, Brushes.Black, 450, y);
                g.DrawString(barkod, barkodFont, Brushes.Black, 520, y);

                y += 30; // Bir sonraki satır için 30px aşağı kay

                // Kağıdın en altına (MarginBounds) gelindiyse, yeni bir kağıt (sayfa) talep et
                if (y > e.MarginBounds.Bottom)
                {
                    e.HasMorePages = true;
                    return;
                }
            }

            // Tüm satırlar bittiyse işlemi kapat
            e.HasMorePages = false;
        }
        #endregion

        #region 📂 10.3 GEÇMİŞ RAPORLARI GÖRÜNTÜLEME
        // Kaydedilmiş klasörü tarar ve daha önce oluşturulan üretim raporlarını ekrana getirir
        private void btnRaporYenile_Click(object sender, EventArgs e)
        {
            string anaYol = string.IsNullOrWhiteSpace(txtKayitYeri.Text)
                ? System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Günlük Üretim Takip")
                : txtKayitYeri.Text;

            if (!System.IO.Directory.Exists(anaYol))
            {
                MessageBox.Show("Henüz kaydedilmiş hiçbir üretim raporu bulunamadı.", "Arşiv Boş", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(anaYol);
            // İç içe geçmiş (Yıl/Ay/Gün) tüm klasörlerdeki CSV'leri bul
            System.IO.FileInfo[] raporDosyalari = di.GetFiles("*.csv", System.IO.SearchOption.AllDirectories);

            if (raporDosyalari.Length == 0)
            {
                MessageBox.Show("Arşiv klasöründe hiç CSV dosyası yok.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            lstRaporlar.DataSource = null;
            lstRaporlar.DataSource = raporDosyalari;
            lstRaporlar.DisplayMember = "Name"; // Sadece dosya ismini göster
        }

        // Seçilen CSV dosyasını okuyup, program içinde yeni bir pencerede (rapor okuyucu) açar
        private void btnRaporAc_Click(object sender, EventArgs e)
        {
            if (lstRaporlar.SelectedItem == null)
            {
                MessageBox.Show("Lütfen açmak istediğiniz raporu listeden seçin!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            System.IO.FileInfo secilenDosya = (System.IO.FileInfo)lstRaporlar.SelectedItem;
            string dosyaYolu = secilenDosya.FullName;

            if (!System.IO.File.Exists(dosyaYolu)) return;

            // Yeni, boş bir popup (Form) penceresi oluştur
            Form raporPenceresi = new Form();
            raporPenceresi.Text = "TamgaApp Rapor Detayı: " + secilenDosya.Name;
            raporPenceresi.Size = new Size(900, 600);
            raporPenceresi.StartPosition = FormStartPosition.CenterScreen;
            raporPenceresi.Icon = this.Icon;

            // Pencerenin içine sadece okunabilir (ReadOnly) bir tablo göm
            DataGridView dgvRapor = new DataGridView();
            dgvRapor.Dock = DockStyle.Fill;
            dgvRapor.AllowUserToAddRows = false;
            dgvRapor.ReadOnly = true;
            dgvRapor.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvRapor.BackgroundColor = Color.WhiteSmoke;
            dgvRapor.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            raporPenceresi.Controls.Add(dgvRapor);

            try
            {
                // Dosyayı oku ve hücrelere böl
                string[] satirlar = System.IO.File.ReadAllLines(dosyaYolu, System.Text.Encoding.UTF8);
                if (satirlar.Length > 0)
                {
                    string[] basliklar = satirlar[0].Split(';'); // İlk satır Sütun Başlıklarıdır
                    foreach (string baslik in basliklar)
                    {
                        dgvRapor.Columns.Add(baslik, baslik);
                    }

                    for (int i = 1; i < satirlar.Length; i++) // 1'den başla ki verileri çeksin (başlık haricinde)
                    {
                        if (string.IsNullOrWhiteSpace(satirlar[i])) continue;
                        dgvRapor.Rows.Add(satirlar[i].Split(';'));
                    }
                }

                // Tablo dolduğunda pencereyi kullanıcının önüne fırlat
                raporPenceresi.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Rapor okunurken bir hata oluştu: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Üretim raporlarının ana (root) olarak kaydedileceği klasörü değiştirmeyi sağlar
        private void btnKayitYeriSec_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                fbd.Description = "Üretim raporlarının kaydedileceği ana klasörü seçin";

                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    txtKayitYeri.Text = fbd.SelectedPath;

                    // Seçilen yolu, program her açıldığında hatırlaması için txt dosyasına yaz
                    string ayarDosyasi = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "KayitYeri.txt");
                    System.IO.File.WriteAllText(ayarDosyasi, fbd.SelectedPath);

                    MessageBox.Show("Rapor kayıt yeri başarıyla güncellendi!", "Ayarlar Kaydedildi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
        #endregion

        #region 📊 10.4 EXCEL'DEN VERİTABANINA ÜRÜN AKTARIMI VE SİLME
        // Dışarıdan (Muhasebeden / ERP'den) gelen güncel barkodlu ürün listesini (Excel)
        // tek tıkla veritabanına almayı sağlar.
        private void btnExcelAktar_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog { Filter = "Excel Dosyası|*.xlsx;*.xls", Title = "Ürün Listesi Seçin" })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        // Microsoft'un ACE OLEDB sürücüsünü kullanarak Excel dosyasına bir veritabanı gibi bağlan
                        string connString = $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={ofd.FileName};Extended Properties=\"Excel 12.0 Xml;HDR=YES;IMEX=1\";";

                        using (System.Data.OleDb.OleDbConnection conn = new System.Data.OleDb.OleDbConnection(connString))
                        {
                            conn.Open();

                            // Excel'in içindeki sayfaların (Sheet1, Sayfa2 vb.) isimlerini bul
                            System.Data.DataTable dtExcelSchema = conn.GetOleDbSchemaTable(System.Data.OleDb.OleDbSchemaGuid.Tables, null);

                            string sheetName = "";
                            foreach (System.Data.DataRow schemaRow in dtExcelSchema.Rows)
                            {
                                string tempName = schemaRow["TABLE_NAME"].ToString();
                                // 🛡️ ZIRH: Sayfa isimleri her zaman $ işareti ile bitmelidir, 
                                // yazıcı bölgesi (Print_Area) gibi sahte sayfaları dışlamak için.
                                if (tempName.EndsWith("$") || tempName.EndsWith("$'"))
                                {
                                    sheetName = tempName;
                                    break;
                                }
                            }

                            // Şayet $ işareti ile biten bir sayfa bulamadıysa, ilk bulduğu tabloyu al
                            if (string.IsNullOrEmpty(sheetName)) sheetName = dtExcelSchema.Rows[0]["TABLE_NAME"].ToString();

                            // Bulunan ilk sayfadaki tüm verileri (SELECT *) DataTable'a aktar
                            System.Data.OleDb.OleDbDataAdapter da = new System.Data.OleDb.OleDbDataAdapter($"SELECT * FROM [{sheetName}]", conn);
                            System.Data.DataTable dt = new System.Data.DataTable();
                            da.Fill(dt);

                            int eklenenSayisi = 0;
                            int atlananSayisi = 0;

                            // Tablodaki her bir satırı veritabanına işle
                            foreach (System.Data.DataRow row in dt.Rows)
                            {
                                // 1. Sütun (Ürün Kodu) boş ise o satırı geç
                                if (row[0] == DBNull.Value || string.IsNullOrWhiteSpace(row[0].ToString()))
                                {
                                    atlananSayisi++;
                                    continue;
                                }

                                Urun yeniUrun = new Urun
                                {
                                    UrunKodu = row[0].ToString().Trim(),
                                    Aciklama = row.ItemArray.Length > 1 ? row[1].ToString().Trim() : "",
                                    IngilizceAciklama = row.ItemArray.Length > 2 ? row[2].ToString().Trim() : "",
                                    Barkod = row.ItemArray.Length > 3 ? row[3].ToString().Trim() : "",
                                    Renk = row.ItemArray.Length > 4 ? row[4].ToString().Trim() : "" // 🌟 5. Sütun (Renk) Eklendi!
                                };

                                DataAccess.InsertUrun(yeniUrun);
                                eklenenSayisi++;
                            }

                            MessageBox.Show($"İşlem Tamamlandı!\n\nExcel'de Bulunan Toplam Satır: {dt.Rows.Count}\nVeritabanına Başarıyla Eklenen: {eklenenSayisi}\nBoş Olduğu İçin Atlanan: {atlananSayisi}", "Excel Aktarım Raporu", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Not: 64-bit ve 32-bit ofis uyumsuzluğundan dolayı hata fırlatabilir.
                        MessageBox.Show("Excel okunurken veya veritabanına kaydedilirken hata oluştu!\n(Not: Bilgisayarınızda Microsoft Access Database Engine 2010 yüklü olmayabilir)\n\nHata Detayı: " + ex.Message, "Kritik Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        // Veritabanındaki kayıtlı tüm ürünleri basitçe DataGridView'de gösterir
        private void btnBarkodVerileri_Click(object sender, EventArgs e)
        {
            var urunler = DataAccess.GetAllUrunler();

            if (urunler.Count == 0)
            {
                MessageBox.Show("Veritabanı şu an BOMBOŞ! Excel'den henüz hiçbir ürün aktarılamamış.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            dgvBarkodVerileri.DataSource = null;
            dgvBarkodVerileri.DataSource = urunler;

            // Arayüz Sütun Ayarları
            if (dgvBarkodVerileri.Columns["Id"] != null) dgvBarkodVerileri.Columns["Id"].Visible = false;
            if (dgvBarkodVerileri.Columns["UrunKodu"] != null) dgvBarkodVerileri.Columns["UrunKodu"].HeaderText = "Ürün Kodu";
            if (dgvBarkodVerileri.Columns["Aciklama"] != null) dgvBarkodVerileri.Columns["Aciklama"].HeaderText = "Açıklama";
            if (dgvBarkodVerileri.Columns["IngilizceAciklama"] != null) dgvBarkodVerileri.Columns["IngilizceAciklama"].HeaderText = "İngilizce Açıklama";
            if (dgvBarkodVerileri.Columns["Barkod"] != null) dgvBarkodVerileri.Columns["Barkod"].HeaderText = "Barkod (EAN)";
            if (dgvBarkodVerileri.Columns["Renk"] != null) dgvBarkodVerileri.Columns["Renk"].HeaderText = "Renk"; // 🌟 Rengi Ekranda Göster
        }

        // ⚠️ TEHLİKELİ İŞLEM: Veritabanındaki on binlerce ürünü tek tuşla tamamen siler
        private void btnTumVerileriSil_Click(object sender, EventArgs e)
        {
            DialogResult cevap = MessageBox.Show("Veritabanındaki TÜM ürünler kalıcı olarak silinecek!\nBu işlemi geri alamazsınız. Emin misiniz?", "Kritik Uyarı", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (cevap == DialogResult.Yes)
            {
                DataAccess.DeleteAllUrunler();

                // Ekranda açık liste varsa onu da silip tertemiz yap
                if (dgvBarkodVerileri != null) dgvBarkodVerileri.DataSource = null;

                MessageBox.Show("Veritabanı başarıyla tertemiz yapıldı!", "İşlem Tamam", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        #endregion

        #endregion

        // =========================================================================================

        #region 👤 11. KULLANICI VE GÜVENLİK YÖNETİMİ

        #region ➕ 11.1 KULLANICI EKLEME VE YETKİLENDİRME
        // Sisteme yeni bir kullanıcı ekler, şifresini kriptolar ve erişebileceği sayfaları kaydeder.
        private void btnKullaniciEkle_Click(object sender, EventArgs e)
        {
            string kAdi = txtYeniKullanici.Text.Trim();
            string sifre = txtYeniSifre.Text.Trim();

            // Boş alan kontrolü zırhı
            if (string.IsNullOrEmpty(kAdi) || string.IsNullOrEmpty(sifre) || string.IsNullOrWhiteSpace(cmbHesapSuresi.Text) || string.IsNullOrWhiteSpace(cmbSifreYenileme.Text))
            {
                MessageBox.Show("Lütfen Kullanıcı Adı, Şifre, Hesap Süresi ve Şifre Yenileme alanlarının TAMAMINI doldurun!", "Eksik Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Kullanıcının listbox'ta işaretlediği yetkileri (sekmeleri) topla ve aralarına virgül koyarak tek bir metne dönüştür
            System.Collections.Generic.List<string> seciliYetkiler = new System.Collections.Generic.List<string>();
            foreach (var item in clbYetkiler.CheckedItems) seciliYetkiler.Add(item.ToString());
            string yetkiString = string.Join(",", seciliYetkiler);

            // Hesabın otomatik kilitleneceği / süresinin dolacağı tarihi hesapla
            DateTime? bitis = null;
            if (cmbHesapSuresi.Text == "1 Saat") bitis = DateTime.Now.AddHours(1);
            else if (cmbHesapSuresi.Text == "1 Ay") bitis = DateTime.Now.AddMonths(1);
            else if (cmbHesapSuresi.Text == "3 Ay") bitis = DateTime.Now.AddMonths(3);
            else if (cmbHesapSuresi.Text == "6 Ay") bitis = DateTime.Now.AddMonths(6);

            // Şifre değiştirme zorunluluğu döngüsünü ayarla
            int sifreSure = 0;
            if (cmbSifreYenileme.Text == "1 Ayda Bir") sifreSure = 1;
            else if (cmbSifreYenileme.Text == "3 Ayda Bir") sifreSure = 3;

            // Veritabanı modelini oluştur
            Kullanici yeniKullanici = new Kullanici
            {
                KullaniciAdi = kAdi,
                SifreHash = SecurityHelper.HashPassword(sifre), // Şifreyi asla düz metin kaydetme, kriptola (Hash)
                Yetkiler = yetkiString,
                BitisTarihi = bitis,
                SonSifreDegistirme = DateTime.Now,
                SifreGecerlilikAyi = sifreSure
            };

            try
            {
                DataAccess.InsertKullanici(yeniKullanici);
                MessageBox.Show($"'{kAdi}' sisteme eklendi!\nYetkileri: {yetkiString}", "İşlem Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // İşlem bitince kutuları temizle
                txtYeniKullanici.Clear();
                txtYeniSifre.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Kayıt hatası! Bu isimde bir kullanıcı zaten olabilir.\n\nSistem Mesajı: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        #endregion

        #region 📋 11.2 KULLANICI LİSTELEME VE SİLME
        // Veritabanındaki tüm kullanıcıları çekip arayüzdeki tabloya (DataGridView) güzel bir formatta yansıtır.
        private void btnKullaniciListele_Click(object sender, EventArgs e)
        {
            var kullanicilar = DataAccess.GetAllKullanicilar();

            // Veritabanı isimleri yerine UI için Türkçe ve anlaşılır sütun başlıkları oluştur (LINQ)
            var tabloVerisi = kullanicilar.Select(k => new {
                Kullanıcı_Adı = k.KullaniciAdi,
                Yetkileri = k.Yetkiler,
                Bitiş_Tarihi = k.BitisTarihi.HasValue ? k.BitisTarihi.Value.ToString("dd.MM.yyyy HH:mm") : "Süresiz",
                Şifre_Zorunluluğu = k.SifreGecerlilikAyi == 0 ? "Hiçbir Zaman" : k.SifreGecerlilikAyi + " Ayda Bir"
            }).ToList();

            dgvKullanicilar.DataSource = null;
            dgvKullanicilar.DataSource = tabloVerisi;
            dgvKullanicilar.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvKullanicilar.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        }

        // Seçili kullanıcıyı sistemden tamamen siler
        private void btnKullaniciSil_Click(object sender, EventArgs e)
        {
            if (dgvKullanicilar.SelectedRows.Count == 0)
            {
                MessageBox.Show("Lütfen tablodan silmek istediğiniz kullanıcıyı (satırı) seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Seçili satırdan kullanıcı adını al
            string seciliKullanici = dgvKullanicilar.SelectedRows[0].Cells["Kullanıcı_Adı"].Value.ToString();

            DialogResult cevap = MessageBox.Show($"'{seciliKullanici}' isimli kullanıcıyı SİLMEK istediğinize emin misiniz?", "Kritik Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Error);
            if (cevap == DialogResult.Yes)
            {
                DataAccess.DeleteKullanici(seciliKullanici);
                MessageBox.Show("Kullanıcı başarıyla silindi!", "İşlem Tamam", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Listeyi yenilemek için listele butonuna sanal olarak tıkla
                btnKullaniciListele.PerformClick();
            }
        }
        #endregion

        #region 🔐 11.3 ŞİFRE SIFIRLAMA (ADMİN)
        // Admin yetkisiyle, kullanıcının şifresini bilmeye gerek kalmadan yeni bir şifre atanmasını sağlar.
        private void btnSifreYenileAdmin_Click(object sender, EventArgs e)
        {
            if (dgvKullanicilar.SelectedRows.Count == 0)
            {
                MessageBox.Show("Lütfen tablodan şifresini sıfırlamak istediğiniz kullanıcıyı seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string seciliKullanici = dgvKullanicilar.SelectedRows[0].Cells["Kullanıcı_Adı"].Value.ToString();

            // Özel şifre sıfırlama formunu/metodunu çağır
            if (LoginForm.KullaniciKendiSifresiniDegistir(seciliKullanici))
            {
                MessageBox.Show($"'{seciliKullanici}' isimli kullanıcının şifresi başarıyla yenilendi!", "İşlem Tamam", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        #endregion

        #region 🚪 11.4 PROGRAM ÇIKIŞI VE ANİMASYON
        // Programın iki kere kapanma sinyali göndermesini engellemek için kullanılan kilit
        private bool kapanisBasladi = false;

        // Sağ üstteki kırmızı (X) çarpı tuşuna basıldığında tetiklenir
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!kapanisBasladi)
            {
                // Standart, çirkin ve anlık kapanmayı iptal et
                e.Cancel = true;

                // Bizim şık kapanış animasyonumuzu çalıştır
                CikisAnimasyonuVeKapat();
            }
        }

        // Sol menüdeki "Güvenli Çıkış" butonuna basıldığında tetiklenir
        private void btnCikisYap_Click(object sender, EventArgs e)
        {
            CikisAnimasyonuVeKapat();
        }

        // Ana formu gizleyip ekrana Splash (Veda) ekranını getiren ve 1.5 saniye sonra sistemi tamamen öldüren motor
        private void CikisAnimasyonuVeKapat()
        {
            kapanisBasladi = true; // Kilit mekanizmasını aç (çifte kapanmayı önle)
            this.Hide(); // Ana program penceresini gizle

            // Veda animasyonlu SplashForm'u ekrana getir
            SplashForm vedaEkrani = new SplashForm();
            vedaEkrani.StartPosition = FormStartPosition.CenterScreen;
            vedaEkrani.Show();

            // 1.5 Saniyelik (1500ms) bir saatli bomba kur
            Timer t = new Timer();
            t.Interval = 1500;
            t.Tick += (s, ev) =>
            {
                t.Stop();
                Environment.Exit(0); // Bütün arka plan işlemlerini ve programı kökten sonlandır
            };
            t.Start();
        }
        #endregion

        #endregion

        // =========================================================================================

        #region 🚛 12. AMBAR ZARFI VE DESİ HESAPLAMA MOTORU

        #region 🧮 12.1 DESİ HESAPLAMA ÇEKİRDEĞİ
        // Kargo ve ambar standartlarına göre (En x Boy x Yükseklik / 3000) hacimsel ağırlık (Desi) hesaplar.
        public double DesiHesapla(string ebatMetni)
        {
            try
            {
                string[] carpanlar = ebatMetni.Split('*');
                if (carpanlar.Length == 3)
                {
                    double en = Convert.ToDouble(carpanlar[0].Trim());
                    double boy = Convert.ToDouble(carpanlar[1].Trim());
                    double yukseklik = Convert.ToDouble(carpanlar[2].Trim());
                    return (en * boy * yukseklik) / 3000.0;
                }
            }
            catch { } // Girilen metin rakam değilse veya format hatalıysa yut ve 0 döndür
            return 0;
        }
        #endregion

        #region 🖥️ 12.2 ARAYÜZ (UI) TABLOLARI KURULUMU
        // Ambar sekmesindeki 3 ana tablonun (Sol: Tüm Firmalar, Orta: Seçilenler, Alt: Paletler) 
        // sütunlarını, gizli ID'lerini ve arayüz davranışlarını ayarlar.
        private void AmbarSisteminiHazirla()
        {
            if (dgvAmbarTumFirmalar == null || dgvAmbarSecilenFirmalar == null || dgvPaletler == null) return;

            // 1. ORTA TABLO (SEÇİLEN FİRMALAR) SÜTUN AYARLARI
            dgvAmbarSecilenFirmalar.ColumnCount = 6;
            dgvAmbarSecilenFirmalar.Columns[0].Name = "Id"; dgvAmbarSecilenFirmalar.Columns[0].Visible = false;
            dgvAmbarSecilenFirmalar.Columns[1].Name = "Firma Adı";
            dgvAmbarSecilenFirmalar.Columns[2].Name = "Adres";
            dgvAmbarSecilenFirmalar.Columns[3].Name = "İl";
            dgvAmbarSecilenFirmalar.Columns[4].Name = "Telefon 1";
            dgvAmbarSecilenFirmalar.Columns[5].Name = "Telefon 2";
            dgvAmbarSecilenFirmalar.AllowUserToAddRows = false;
            dgvAmbarSecilenFirmalar.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            // 2. PALET TABLOSU SÜTUN AYARLARI
            dgvPaletler.ColumnCount = 3;
            dgvPaletler.Columns[0].Name = "Palet No"; dgvPaletler.Columns[0].ReadOnly = true;
            dgvPaletler.Columns[1].Name = "Ebatlar (En*Boy*Yük)";
            dgvPaletler.Columns[2].Name = "Desi"; dgvPaletler.Columns[2].ReadOnly = true; // Desi otomatik hesaplanacağı için elle müdahaleye kapalı
            dgvPaletler.AllowUserToAddRows = false;

            // 3. SOL TABLO (TÜM FİRMALAR) VE VERİ ÇEKİMİ
            dgvAmbarTumFirmalar.ColumnCount = 6;
            dgvAmbarTumFirmalar.Columns[0].Name = "Id"; dgvAmbarTumFirmalar.Columns[0].Visible = false;
            dgvAmbarTumFirmalar.Columns[1].Name = "Firma Adı";
            dgvAmbarTumFirmalar.Columns[2].Name = "Adres";
            dgvAmbarTumFirmalar.Columns[3].Name = "İl";
            dgvAmbarTumFirmalar.Columns[4].Name = "Telefon 1";
            dgvAmbarTumFirmalar.Columns[5].Name = "Telefon 2";
            dgvAmbarTumFirmalar.AllowUserToAddRows = false;
            dgvAmbarTumFirmalar.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            dgvAmbarTumFirmalar.Rows.Clear();
            var firmalar = DataAccess.GetAllFirmalar();
            foreach (var f in firmalar) dgvAmbarTumFirmalar.Rows.Add(f.Id, f.FirmaAdi, f.Adres, f.Il, f.Telefon1, f.Telefon2);

            // O ekrana özel yazıcı açılır kutusunu bilgisayardaki yazıcılarla doldur
            ComboBox cmbYazici = this.Controls.Find("cmbAmbarYazici", true).FirstOrDefault() as ComboBox;
            if (cmbYazici != null)
            {
                cmbYazici.Items.Clear();
                foreach (string printer in PrinterSettings.InstalledPrinters) cmbYazici.Items.Add(printer);

                PrintDocument pd = new PrintDocument();
                if (cmbYazici.Items.Contains(pd.PrinterSettings.PrinterName))
                    cmbYazici.SelectedItem = pd.PrinterSettings.PrinterName;
                else if (cmbYazici.Items.Count > 0) cmbYazici.SelectedIndex = 0;
            }

            // Arayüz Sütun Doldurma Modları (Ekrana tam sığsınlar diye)
            if (dgvAmbarTumFirmalar != null) dgvAmbarTumFirmalar.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            if (dgvAmbarSecilenFirmalar != null) dgvAmbarSecilenFirmalar.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            if (dgvPaletler != null) dgvPaletler.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }
        #endregion

        #region 🔄 12.3 FİRMA VE PALET BİLGİSİ GİRİŞLERİ
        // Soldaki (Ana) listeden bir firmaya çift tıklanıldığında onu ortadaki (Seçilenler) listesine atar.
        private void dgvAmbarTumFirmalar_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                // Eğer tablo ilk kez kullanılıyorsa sütunları tekrar tanımla (Güvenlik Önlemi)
                if (dgvAmbarSecilenFirmalar.ColumnCount == 0)
                {
                    dgvAmbarSecilenFirmalar.ColumnCount = 6;
                    dgvAmbarSecilenFirmalar.Columns[0].Name = "Id"; dgvAmbarSecilenFirmalar.Columns[0].Visible = false;
                    dgvAmbarSecilenFirmalar.Columns[1].Name = "Firma Adı";
                    dgvAmbarSecilenFirmalar.Columns[2].Name = "Adres";
                    dgvAmbarSecilenFirmalar.Columns[3].Name = "İl";
                    dgvAmbarSecilenFirmalar.Columns[4].Name = "Telefon 1";
                    dgvAmbarSecilenFirmalar.Columns[5].Name = "Telefon 2";
                    dgvAmbarSecilenFirmalar.AllowUserToAddRows = false;
                    dgvAmbarSecilenFirmalar.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                }

                DataGridViewRow secilen = dgvAmbarTumFirmalar.Rows[e.RowIndex];
                string id = secilen.Cells[0].Value?.ToString();
                string firmaAdi = secilen.Cells[1].Value?.ToString();

                // 🛡️ ZIRH: Eğer bu firma zaten listeye eklenmişse ikinci kez eklenmesini engelle
                foreach (DataGridViewRow row in dgvAmbarSecilenFirmalar.Rows)
                {
                    if (row.Cells[0].Value?.ToString() == id)
                    {
                        MessageBox.Show("Bu firma zaten yazdırılacaklar listesinde mevcut!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }

                // Firmayı seçilenler listesine kopyala
                dgvAmbarSecilenFirmalar.Rows.Add(id, firmaAdi, secilen.Cells[2].Value, secilen.Cells[3].Value, secilen.Cells[4].Value, secilen.Cells[5].Value);
            }
        }

        // Ortadaki (Seçilenler) listeden bir firmaya çift tıklanıldığında o firmayı listeden çıkarır.
        private void dgvAmbarSecilenFirmalar_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0) dgvAmbarSecilenFirmalar.Rows.RemoveAt(e.RowIndex);
        }

        // Seçilen palet sayısı değiştiğinde, aşağıdaki tabloyu o sayı kadar "Palet 1, Palet 2..." diye satır satır doldurur.
        private void cmbPaletSayisi_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (dgvPaletler == null) return;

            if (dgvPaletler.ColumnCount == 0)
            {
                dgvPaletler.ColumnCount = 3;
                dgvPaletler.Columns[0].Name = "Palet No"; dgvPaletler.Columns[0].ReadOnly = true;
                dgvPaletler.Columns[1].Name = "Ebatlar (En*Boy*Yük)";
                dgvPaletler.Columns[2].Name = "Desi"; dgvPaletler.Columns[2].ReadOnly = true;
                dgvPaletler.AllowUserToAddRows = false;
            }

            dgvPaletler.Rows.Clear(); // Önceki seçimleri temizle

            if (int.TryParse(cmbPaletSayisi.Text, out int paletSayisi))
            {
                for (int i = 1; i <= paletSayisi; i++)
                {
                    dgvPaletler.Rows.Add($"{i}. PALET", "", "0 Ds.");
                }
            }
        }

        // Kullanıcı "Ebatlar" hücresine veri girdiğinde veya değiştirdiğinde anında tetiklenir ve Desi'yi hesaplar.
        private void dgvPaletler_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == 1 && e.RowIndex >= 0)
            {
                string ebatMetni = dgvPaletler.Rows[e.RowIndex].Cells[1].Value?.ToString().Trim();

                if (!string.IsNullOrWhiteSpace(ebatMetni))
                {
                    // ✨ AKILLI YARDIMCI: Kullanıcı üşenip "080120150" yazarsa 
                    // sistem bunu otomatik olarak "080*120*150" şekline çevirir.
                    if (ebatMetni.Length == 9 && !ebatMetni.Contains("*"))
                    {
                        ebatMetni = $"{ebatMetni.Substring(0, 3)}*{ebatMetni.Substring(3, 3)}*{ebatMetni.Substring(6, 3)}";
                        dgvPaletler.Rows[e.RowIndex].Cells[1].Value = ebatMetni;
                        return; // Value değiştiği için bu metot tekrar tetiklenecek, o yüzden işlemi burada kes
                    }

                    // Doğru formatlı metni Desi hesaplama çekirdeğine yolla
                    double desi = DesiHesapla(ebatMetni);

                    // Çıkan sonucu küsuratsız yuvarlayıp "Ds." takısı ile hücreye yaz
                    dgvPaletler.Rows[e.RowIndex].Cells[2].Value = Math.Round(desi, 0) + " Ds.";
                }
            }
        }
        #endregion

        #region 📝 12.4 YAZDIRMA LİSTESİ OLUŞTURMA VE YÖNETİMİ
        // Firmanın adres bilgilerini ve palet ebatlarını harmanlayıp EN ALTTAKİ son (yazdırılacak) listeye gönderir.
        private void btnAmbarListeyeEkle_Click(object sender, EventArgs e)
        {
            if (dgvAmbarSonListe == null) return;

            // En alt tablonun kurulumu
            if (dgvAmbarSonListe.ColumnCount == 0)
            {
                dgvAmbarSonListe.ColumnCount = 9;
                dgvAmbarSonListe.Columns[0].Name = "Id"; dgvAmbarSonListe.Columns[0].Visible = false;
                dgvAmbarSonListe.Columns[1].Name = "Firma Adı";
                dgvAmbarSonListe.Columns[2].Name = "Adres";
                dgvAmbarSonListe.Columns[3].Name = "İl";
                dgvAmbarSonListe.Columns[4].Name = "Telefon 1";
                dgvAmbarSonListe.Columns[5].Name = "Telefon 2";
                dgvAmbarSonListe.Columns[6].Name = "Palet Sayısı";
                dgvAmbarSonListe.Columns[7].Name = "Ölçüler";
                dgvAmbarSonListe.Columns[8].Name = "Toplam Desi";
                dgvAmbarSonListe.AllowUserToAddRows = false;
                dgvAmbarSonListe.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                dgvAmbarSonListe.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            }

            if (dgvAmbarSecilenFirmalar.SelectedRows.Count == 0) { MessageBox.Show("Lütfen ortadaki listeden listeye eklenecek firmayı seçin!", "Uyarı"); return; }
            if (dgvPaletler.Rows.Count == 0) { MessageBox.Show("Lütfen palet sayısı seçip ölçüleri girin!", "Uyarı"); return; }

            // Seçilen firmanın bilgilerini çek
            var firmaRow = dgvAmbarSecilenFirmalar.SelectedRows[0];
            string id = firmaRow.Cells[0].Value?.ToString();
            string fAdi = firmaRow.Cells[1].Value?.ToString();
            string adres = firmaRow.Cells[2].Value?.ToString();
            string il = firmaRow.Cells[3].Value?.ToString();
            string tel1 = firmaRow.Cells[4].Value?.ToString();
            string tel2 = firmaRow.Cells[5].Value?.ToString();

            // Palet bilgilerini harmanlamak için değişkenler
            string paletSayisi = cmbPaletSayisi.Text;
            List<string> olculerListesi = new List<string>();
            double toplamDesi = 0;

            // Tüm palet satırlarını gez, ebatları ve desileri alt alta yazılacak şekilde birleştir
            foreach (DataGridViewRow prow in dgvPaletler.Rows)
            {
                string ebat = prow.Cells[1].Value?.ToString() ?? "";
                string desiMetni = prow.Cells[2].Value?.ToString() ?? "0 Ds.";

                olculerListesi.Add($"{ebat} ({desiMetni})"); // Örn: 080*120*150 (120 Ds.)
                toplamDesi += DesiHesapla(ebat);
            }

            // Listeyi string'e (Metin) çevir
            string birlesikOlculer = string.Join("\n", olculerListesi);
            string toplamDesiSonucu = Math.Round(toplamDesi, 0).ToString() + " Ds.";

            // Her şeyi son listeye yolla
            dgvAmbarSonListe.Rows.Add(id, fAdi, adres, il, tel1, tel2, paletSayisi, birlesikOlculer, toplamDesiSonucu);

            // İşlem bittikten sonra palet kutusunu sıfırla ki yanlışlıkla aynı ölçüleri ikinciye atmasın
            cmbPaletSayisi.SelectedIndex = -1;
            dgvPaletler.Rows.Clear();
        }

        // "Listeden Çıkar" butonu ile seçilen veya son listedeki satırı temizler
        private void btnAmbarSil_Click(object sender, EventArgs e)
        {
            // Öncelikle en alttaki (Son) listede seçili bir şey var mı diye bak
            if (dgvAmbarSonListe != null && dgvAmbarSonListe.SelectedRows.Count > 0)
            {
                foreach (DataGridViewRow row in dgvAmbarSonListe.SelectedRows) dgvAmbarSonListe.Rows.Remove(row);
                return;
            }

            // Yoksa ortadaki (Seçilen Firmalar) listesine bak
            if (dgvAmbarSecilenFirmalar != null && dgvAmbarSecilenFirmalar.SelectedRows.Count > 0)
            {
                foreach (DataGridViewRow row in dgvAmbarSecilenFirmalar.SelectedRows) dgvAmbarSecilenFirmalar.Rows.Remove(row);
                return;
            }

            MessageBox.Show("Lütfen silmek istediğiniz satırı seçin.\n(Ana firma listesinden silme yapılamaz, sadece seçilenler ve son liste silinebilir).", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // Ana firma tablosunu (Sol Tablo) veritabanından çekerek günceller
        private void btnAmbarYenile_Click(object sender, EventArgs e)
        {
            if (dgvAmbarTumFirmalar == null) return;

            if (dgvAmbarTumFirmalar.ColumnCount == 0)
            {
                dgvAmbarTumFirmalar.ColumnCount = 6;
                dgvAmbarTumFirmalar.Columns[0].Name = "Id"; dgvAmbarTumFirmalar.Columns[0].Visible = false;
                dgvAmbarTumFirmalar.Columns[1].Name = "Firma Adı";
                dgvAmbarTumFirmalar.Columns[2].Name = "Adres";
                dgvAmbarTumFirmalar.Columns[3].Name = "İl";
                dgvAmbarTumFirmalar.Columns[4].Name = "Telefon 1";
                dgvAmbarTumFirmalar.Columns[5].Name = "Telefon 2";
                dgvAmbarTumFirmalar.AllowUserToAddRows = false;
                dgvAmbarTumFirmalar.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            }

            dgvAmbarTumFirmalar.Rows.Clear();
            var firmalar = DataAccess.GetAllFirmalar();
            foreach (var f in firmalar) dgvAmbarTumFirmalar.Rows.Add(f.Id, f.FirmaAdi, f.Adres, f.Il, f.Telefon1, f.Telefon2);
        }

        // Sol tabloda anlık (harf harf yazıldıkça veya butona basıldıkça) arama yapar
        private void btnAmbarAra_Click(object sender, EventArgs e)
        {
            if (dgvAmbarTumFirmalar == null) return;

            string aranan = txtAmbarFirmaAra.Text.Trim().ToLower();
            dgvAmbarTumFirmalar.Rows.Clear();

            var firmalar = DataAccess.GetAllFirmalar();
            foreach (var f in firmalar)
            {
                // "Contains" kullanıldı, böylece kelimenin ortasında bile geçse bulur
                if (f.FirmaAdi.ToLower().Contains(aranan))
                {
                    dgvAmbarTumFirmalar.Rows.Add(f.Id, f.FirmaAdi, f.Adres, f.Il, f.Telefon1, f.Telefon2);
                }
            }

            if (dgvAmbarTumFirmalar.Rows.Count == 0) MessageBox.Show("Aramanıza uygun firma bulunamadı.", "Bilgi");
        }
        #endregion

        #region 🖨️ 12.5 DL ZARF YAZDIRMA MOTORU (SPOOLER)
        // En alttaki listede biriken firmaları ve ebatları standart bir DL Zarfa ortalayarak yazdırır.
        private void btnAmbarYazdir_Click(object sender, EventArgs e)
        {
            if (dgvAmbarSonListe.Rows.Count == 0) { MessageBox.Show("Yazdırılacak hiç palet/firma yok!"); return; }

            PrintDocument pd = new PrintDocument();

            // Özel Ambar Yazıcısını Seç
            ComboBox cmbYazici = this.Controls.Find("cmbAmbarYazici", true).FirstOrDefault() as ComboBox;
            if (cmbYazici != null && cmbYazici.SelectedItem != null)
            {
                pd.PrinterSettings.PrinterName = cmbYazici.SelectedItem.ToString();
            }
            // Yoksa sistemin genel çoklu yazıcısını (fallback) kullan
            else if (cmbCokluPrinter != null && cmbCokluPrinter.SelectedItem != null)
            {
                pd.PrinterSettings.PrinterName = cmbCokluPrinter.SelectedItem.ToString();
            }

            // Windows'a tanıtılmış kağıtlar arasında "DL" ismini taşıyan zarf türünü ara
            PaperSize orijinalDlBoyutu = null;
            try
            {
                foreach (PaperSize kagit in pd.PrinterSettings.PaperSizes)
                {
                    if (kagit.PaperName.ToUpper().Contains("DL")) { orijinalDlBoyutu = kagit; break; }
                }
            }
            catch { }

            // Eğer yazıcının hafızasında resmi bir DL Zarf boyutu varsa onu kullan,
            // Yoksa (ZIRH), milimetrik ölçüleri inch cinsinden vererek sanal bir DL zarf oluştur.
            if (orijinalDlBoyutu != null) pd.DefaultPageSettings.PaperSize = orijinalDlBoyutu;
            else pd.DefaultPageSettings.PaperSize = new PaperSize("DL_Zarf", 433, 866);

            pd.DefaultPageSettings.Landscape = true; // Zarfın geniş kısmı her zaman yatay olur
            pd.BeginPrint += (s, ev) => { batchIndex = 0; };
            pd.PrintPage += AmbarPrintDocument_PrintPage;

            // Hata önleme amacıyla direkt yazdırmak yerine önce önizleme penceresini aç
            PrintPreviewDialog ppd = new PrintPreviewDialog { Document = pd };
            try { ppd.ShowDialog(); } catch { }
        }

        // Zarf üzerine kutuları ve yazıları çizen çekirdek çizim metodu
        private void AmbarPrintDocument_PrintPage(object sender, PrintPageEventArgs e)
        {
            // Çizilecek başka firma kalmadıysa yazdırma motorunu durdur
            if (batchIndex >= dgvAmbarSonListe.Rows.Count) { e.HasMorePages = false; return; }

            // Çift Zırh: Yazdırma esnasında kağıt ölçüsü bozulursa, son bir kez daha DL Zarf ayarlarını bas
            bool dlBulundu = false;
            try
            {
                if (e.PageSettings.PaperSize.PaperName.ToUpper().Contains("DL")) dlBulundu = true;
            }
            catch { }

            if (!dlBulundu) e.PageSettings.PaperSize = new PaperSize("DL_Zarf", 433, 866);
            e.PageSettings.Landscape = true;

            // Sıradaki satırın bilgilerini UI'dan çek
            var row = dgvAmbarSonListe.Rows[batchIndex];
            string firmaAdi = row.Cells[1].Value?.ToString();
            string adres = row.Cells[2].Value?.ToString();
            string il = row.Cells[3].Value?.ToString();
            string tel1 = row.Cells[4].Value?.ToString();
            string tel2 = row.Cells[5].Value?.ToString();
            string paletSayisi = row.Cells[6].Value?.ToString();
            string olculer = row.Cells[7].Value?.ToString();

            // Kalemlerin fontlarını ayarla
            Font baslik = new Font("Arial", 16, FontStyle.Bold);
            Font icerik = new Font("Arial", 12, FontStyle.Bold);

            // Zarfın üzerindeki kutucuklarda (Rectangle) yazıların her zaman TAM ORTADA çıkmasını sağla
            StringFormat ortala = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

            e.Graphics.PageUnit = GraphicsUnit.Display;

            // YAZICI OFSET KALİBRASYONLARI
            // Bu değerler kağıdın fiziksel olarak yazıcıya girdiği yere göre tasarımın kaymasını düzeltir.
            int inceAyarX = 0; // Sağa veya sola kaydır
            int inceAyarY = -25; // Aşağı veya yukarı kaydır

            // Kutuların X-Y Koordinat ve Boyut Matematiği
            int ustBosluk = 76 + inceAyarY;
            int kutuYukseklik = 280;
            int adresGenişlik = 470;
            int paletGenişlik = 296;
            int solBosluk = 40 + inceAyarX;
            int kutuArasiBosluk = 20; // İki kare arasındaki boşluk
            int paletSolKoordinat = solBosluk + adresGenişlik + kutuArasiBosluk; // İkinci kutuyu, birinci kutunun yanına yerleştir

            // 1. SOL KUTU (FİRMA VE ADRES BİLGİLERİ)
            e.Graphics.DrawRectangle(Pens.Black, solBosluk, ustBosluk, adresGenişlik, kutuYukseklik); // Dış çerçeve
            e.Graphics.DrawString("ADRES", baslik, Brushes.Black, new Rectangle(solBosluk, ustBosluk, adresGenişlik, 40), ortala); // Üst başlık alanı
            e.Graphics.DrawLine(Pens.Black, solBosluk, ustBosluk + 40, solBosluk + adresGenişlik, ustBosluk + 40); // Başlığın altını çizen çizgi

            // Verileri alt alta (Satır başı yaparak) tek bir metin halinde yazdır
            e.Graphics.DrawString($"{firmaAdi}\n\n{adres}\n{il}\n{tel1} {tel2}", icerik, Brushes.Black, new Rectangle(solBosluk, ustBosluk + 50, adresGenişlik, kutuYukseklik - 60), ortala);

            // 2. SAĞ KUTU (PALET VE EBAT BİLGİLERİ)
            e.Graphics.DrawRectangle(Pens.Black, paletSolKoordinat, ustBosluk, paletGenişlik, kutuYukseklik); // Dış çerçeve
            e.Graphics.DrawString("PALET ÖLÇÜLERİ", baslik, Brushes.Black, new Rectangle(paletSolKoordinat, ustBosluk, paletGenişlik, 40), ortala);
            e.Graphics.DrawLine(Pens.Black, paletSolKoordinat, ustBosluk + 40, paletSolKoordinat + paletGenişlik, ustBosluk + 40);
            e.Graphics.DrawString($"{olculer}\n\nTOPLAM: {paletSayisi} PALET", icerik, Brushes.Black, new Rectangle(paletSolKoordinat, ustBosluk + 50, paletGenişlik, kutuYukseklik - 60), ortala);

            // Sıradaki firmaya geç
            batchIndex++;

            // Yazdırılacak başka firma kalmadıysa (Listenin sonuysa) döngüyü/motoru durdur
            e.HasMorePages = (batchIndex < dgvAmbarSonListe.Rows.Count);
        }
        #endregion

        #endregion

        // =========================================================================================

        #region 📦 13. WMS SEVKİYAT VE SQL ENTEGRASYONU

        #region 🔌 13.1 SQL BAĞLANTI AYARLARI VE ŞİFRELEME
        // Kullanıcının girdiği SQL ayarlarını kriptolayarak uygulamanın yerel ayarlarına (Settings) kaydeder.
        private void btnSqlKaydet_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.SqlSunucu = txtSqlSunucu.Text;
            Properties.Settings.Default.SqlVeritabani = txtSqlVeritabani.Text;
            Properties.Settings.Default.SqlKullanici = txtSqlKullanici.Text;
            Properties.Settings.Default.SqlSifre = Kripto.Sifrele(txtSqlSifre.Text); // Şifreyi açık metin olarak kaydetme!
            Properties.Settings.Default.Save();
            MessageBox.Show("SQL Bağlantı ayarları güvenli bir şekilde şifrelenerek kaydedildi!", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // SQL ayarlarını programın hafızasından siler.
        private void btnSqlTemizle_Click(object sender, EventArgs e)
        {
            txtSqlSunucu.Clear(); txtSqlVeritabani.Clear(); txtSqlKullanici.Clear(); txtSqlSifre.Clear();

            Properties.Settings.Default.SqlSunucu = ""; Properties.Settings.Default.SqlVeritabani = "";
            Properties.Settings.Default.SqlKullanici = ""; Properties.Settings.Default.SqlSifre = "";
            Properties.Settings.Default.Save();

            MessageBox.Show("SQL Ayarları ve kriptolu şifreler sistemden tamamen silindi!", "Sıfırlandı", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // Kayıtlı ayarlardan yola çıkarak OleDb formatında bağlantı cümlesini oluşturur.
        private string SqlBaglantiDizesiGetir()
        {
            string sunucu = Properties.Settings.Default.SqlSunucu;
            string vt = Properties.Settings.Default.SqlVeritabani;
            string kullanici = Properties.Settings.Default.SqlKullanici;
            string gercekSifre = Kripto.Coz(Properties.Settings.Default.SqlSifre); // Kullanmadan önce şifreyi çöz

            return $"Provider=SQLOLEDB.1;Password={gercekSifre};Persist Security Info=True;User ID={kullanici};Initial Catalog={vt};Data Source={sunucu};Use Procedure for Prepare=1;Auto Translate=True;Packet Size=4096;Use Encryption for Data=False;Tag with column collation when possible=False";
        }
        #endregion

        #region 🏠 13.2 EV MODU (GELİŞTİRİCİ TEST ALANI)
        // ------------------------------------------------------------------------
        // SQL'e bağlanmadan sanal siparişlerle arayüzü test etmek için kullanılır.
        // ------------------------------------------------------------------------
        private void EvModuSahteVerileriniYukle()
        {
            dtTumSiparisler = new DataTable();
            dtTumSiparisler.Columns.Add("BelgeTipi", typeof(string));
            dtTumSiparisler.Columns.Add("SirkuNo", typeof(string));
            dtTumSiparisler.Columns.Add("BelgeNo", typeof(string));
            dtTumSiparisler.Columns.Add("MusteriAdi", typeof(string));
            dtTumSiparisler.Columns.Add("SevkMusteri", typeof(string));
            dtTumSiparisler.Columns.Add("Malzeme", typeof(string));
            dtTumSiparisler.Columns.Add("MalzemeAdi", typeof(string));
            dtTumSiparisler.Columns.Add("SecenekAciklamasi", typeof(string));
            dtTumSiparisler.Columns.Add("Bakiye", typeof(int));

            // Sahte Test Siparişleri 
            dtTumSiparisler.Rows.Add("SE", "S-001", "SE-2026-001", "Ahmet Yılmaz", "Ahmet Yılmaz Depo", "11111", "Masaüstü Bilgisayar", "Siyah", 5);
            dtTumSiparisler.Rows.Add("SE", "S-001", "SE-2026-001", "Ahmet Yılmaz", "Ahmet Yılmaz Depo", "22222", "Klavye Mouse", "Beyaz", 3);
            dtTumSiparisler.Rows.Add("O1", "S-002", "O1-2026-002", "John Doe Limited", "NY Lojistik", "33333", "Çelik Kasa", "Büyük Boy", 2);
        }
        #endregion

        #region 📥 13.3 AÇIK SİPARİŞLERİ YENİLEME (SQL ÇEKİMİ)
        // "Yenile" butonuna basıldığında ERP sistemindeki açık, sevk edilmemiş siparişleri çeker.
        private void btnSiparisYenile_Click(object sender, EventArgs e)
        {
            // DİKKAT: Ana global değişkende Ev Modu açıksa SQL'e gitme, sahte verileri yükle!
            if (EvModuAktif) // Bu değişkeni Region 01'den alıyor. Canlıya alırken false olacak.
            {
                EvModuSahteVerileriniYukle();

                // 👻 GHOST MODU: Sahte veriler içinden bile sevk edilmiş (kapanmış) olanları filtrele
                for (int i = dtTumSiparisler.Rows.Count - 1; i >= 0; i--)
                {
                    if (TamamlananBelgeNolar.Contains(dtTumSiparisler.Rows[i]["BelgeNo"].ToString()))
                    {
                        dtTumSiparisler.Rows.RemoveAt(i);
                    }
                }
                dtTumSiparisler.AcceptChanges();

                cmbMusteri.Items.Clear();
                cmbBelgeNo.Items.Clear();
                var testMusterileri = dtTumSiparisler.AsEnumerable().Select(r => r.Field<string>("MusteriAdi")).Where(m => !string.IsNullOrEmpty(m)).Distinct().OrderBy(m => m).ToArray();
                cmbMusteri.Items.AddRange(testMusterileri);
                MessageBox.Show("Ev Modu Aktif: Sisteme bağlanılmadı, sahte test verileri yüklendi!", "Geliştirici Modu");
                return;
            }

            // ====================================================================
            // 🏢 CANLI ORTAM: ORİJİNAL SQL BAĞLANTISI (Ev Modu kapalıyken çalışır)
            // ====================================================================
            try
            {
                string baglantiDizesi = SqlBaglantiDizesiGetir();

                // ERP sisteminden sevk emri verilmiş (SE, O1) ama henüz kapanmamış/kilitlenmemiş siparişleri çeken devasa sorgu.
                string sorgu = @"
        SELECT  
            A.DOCTYPE AS BelgeTipi,
            A.STFDOCNUM AS SirkuNo,
            A.DOCNUM AS BelgeNo,
            A.NAME1 AS MusteriAdi,
            A.GRCNAME1 AS SevkMusteri,
            B.MATERIAL AS Malzeme,
            B.MTEXT AS MalzemeAdi,
            B.LTEXT AS SecenekAciklamasi,
            B.AVAILQTY AS Bakiye
        FROM IASSALHEAD A 
        LEFT OUTER JOIN IASBAS007X R ON R.CLIENT = A.CLIENT 
            AND R.COMPANY = A.COMPANY 
            AND R.SALDEPT = A.SALDEPT 
            AND R.LANGU = 'T' , IASBAS010 , IASSALITEM B 
        LEFT OUTER JOIN IASSALITEM S ON S.COMPANY = B.COMPANY 
            AND S.DOCTYPE = B.DOCTYPE 
            AND S.DOCNUM = B.DOCNUM 
            AND S.ITEMNUM = B.ITEMNUM 
            AND S.SETITEMNUM = 0 
            AND S.ISSET = 1 , IASMATBASIC C 
        LEFT OUTER JOIN ECESAL016 P ON P.CLIENT = C.CLIENT 
            AND P.COMPANY = C.COMPANY 
            AND P.SEGMENT = C.SEGMENT , IASMATFMS D 
        LEFT OUTER JOIN IASBAS008X Q ON Q.CLIENT = D.CLIENT 
            AND Q.COMPANY = D.COMPANY 
            AND Q.HIERARCHY = D.HIERARCHY 
            AND Q.LANGU = 'T' , IASCUSTOMER CUS 
        LEFT OUTER JOIN ECESAL017 E ON E.CLIENT = CUS.CLIENT 
            AND E.COMPANY = CUS.COMPANY 
            AND E.BRANCHTYPE = CUS.BRANCHTYPE 
        WHERE A.CLIENT = '00' 
            AND A.COMPANY = '09' 
            AND A.DOCTYPE IN  ('SE','O1') 
            AND A.VALIDFROM >= '2023-01-01' 
            AND A.VALIDFROM <= '2030-01-01' 
            AND A.ISORDCHAR = 1 
            AND B.ORDSTAT < 2 
            AND B.ISSTOP = '0' 
            AND A.ISDELETE = 0 
            AND B.CLIENT = A.CLIENT 
            AND B.COMPANY = A.COMPANY 
            AND B.DOCTYPE = A.DOCTYPE 
            AND B.DOCNUM = A.DOCNUM 
            AND CUS.COMPANY = A.COMPANY 
            AND CUS.BUSAREA = A.BUSAREA 
            AND CUS.CUSTOMER = A.CUSTOMER 
            AND (B.ISSET = 0 OR B.SETITEMNUM <> 0) 
            AND B.BUSAREA = '*' 
            AND B.UPTDATE >= '1975-01-01' 
            AND B.UPTDATE <= '2030-01-01' 
            AND C.CLIENT = B.CLIENT 
            AND C.COMPANY = B.COMPANY 
            AND C.MATERIAL = B.MATERIAL 
            AND C.VALIDFROM <= GETDATE() 
            AND C.VALIDUNTIL >= GETDATE() 
            AND D.CLIENT = B.CLIENT 
            AND D.COMPANY = B.COMPANY 
            AND D.PLANT = B.PLANT 
            AND D.MATERIAL = B.MATERIAL 
            AND D.VALIDFROM <= GETDATE() 
            AND D.VALIDUNTIL >= GETDATE() 
            AND IASBAS010.CLIENT = B.CLIENT 
            AND IASBAS010.COMPANY = B.COMPANY 
            AND IASBAS010.PRICELIST = B.PRICELIST 
        ORDER BY A.COMPANY, A.CUSTOMER, A.DOCTYPE, A.DOCNUM, B.ITEMNUM;";

                using (OleDbConnection baglanti = new OleDbConnection(baglantiDizesi))
                {
                    using (OleDbDataAdapter adaptor = new OleDbDataAdapter(sorgu, baglanti))
                    {
                        dtTumSiparisler.Clear();
                        adaptor.Fill(dtTumSiparisler); // Çekilen veriyi yerel tabloya (RAM) at
                    }
                }

                // 👻 GHOST MODU KARA LİSTESİ: 
                // SQL siparişi hala "açık" gösterse de, biz onu 'Tam Sevk' yaptıysak
                // yerel kara listeden onu bulur ve ekrana göstermeden sileriz.
                for (int i = dtTumSiparisler.Rows.Count - 1; i >= 0; i--)
                {
                    // Veritabanından gelen BelgeNo'nun sağında/solunda boşluk varsa onu da keserek (Trim) eşleştir.
                    string dbBelge = dtTumSiparisler.Rows[i]["BelgeNo"].ToString().Trim();

                    if (TamamlananBelgeNolar.Contains(dbBelge))
                    {
                        dtTumSiparisler.Rows.RemoveAt(i);
                    }
                }
                dtTumSiparisler.AcceptChanges();

                // Müşteri seçimi açılır kutusunu doldur
                cmbMusteri.Items.Clear();
                var benzersizMusteriler = dtTumSiparisler.AsEnumerable()
                                                         .Select(row => row.Field<string>("MusteriAdi")?.Trim()) // ZIRH: SQL boşluklarını temizle
                                                         .Where(m => !string.IsNullOrEmpty(m))
                                                         .Distinct()
                                                         .OrderBy(m => m)
                                                         .ToArray();
                cmbMusteri.Items.AddRange(benzersizMusteriler);

                MessageBox.Show("Açık siparişler çekildi! Lütfen önce Müşteri seçin.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("SQL Bağlantı Hatası: \n" + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        #endregion

        #region 🔍 13.4 ARAMA VE EŞLEŞTİRME (BELGE NO DOLDURMA)
        // Kullanıcı bir "Müşteri" seçtiğinde, sadece o müşteriye ait Belge Numaralarını altındaki kutuya doldurur.
        private void cmbMusteri_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbMusteri.SelectedItem == null) return;

            string secilenMusteri = cmbMusteri.SelectedItem.ToString().Trim();
            cmbBelgeNo.Items.Clear();
            List<string> belgeler = new List<string>();

            foreach (DataRow row in dtTumSiparisler.Rows)
            {
                if (row["MusteriAdi"] != DBNull.Value)
                {
                    string tabloMusteri = row["MusteriAdi"].ToString().Trim();
                    if (tabloMusteri.Equals(secilenMusteri, StringComparison.OrdinalIgnoreCase))
                    {
                        string belge = row["BelgeNo"] != DBNull.Value ? row["BelgeNo"].ToString().Trim() : "";
                        if (!string.IsNullOrEmpty(belge) && !belgeler.Contains(belge))
                        {
                            belgeler.Add(belge);
                        }
                    }
                }
            }

            if (belgeler.Count > 0)
            {
                cmbBelgeNo.Items.AddRange(belgeler.ToArray());
                cmbBelgeNo.SelectedIndex = 0;
            }
        }

        // Belge No kutusunun yanındaki "Ara" butonuna basıldığında tabloyu SIFIRLAR ve ürünleri getirir.
        private void btnSevkAra_Click(object sender, EventArgs e)
        {
            string secilenBelge = cmbBelgeNo.Text.Trim();
            if (string.IsNullOrEmpty(secilenBelge)) { MessageBox.Show("Lütfen bir Belge No seçin.", "Uyarı"); return; }

            DataRow[] filtrelenmisSatirlar = dtTumSiparisler.Select($"BelgeNo LIKE '%{secilenBelge}%'");

            if (filtrelenmisSatirlar.Length > 0)
            {
                txtMusteriAdi.Text = filtrelenmisSatirlar[0]["MusteriAdi"].ToString().Trim();
                txtSevkMusteri.Text = filtrelenmisSatirlar[0]["SevkMusteri"].ToString().Trim();

                var yerelUrunler = DataAccess.GetAllUrunler();
                DataTable dtEkran = new DataTable();
                dtEkran.Columns.Add("Belge No", typeof(string)); // 🌟 YENİ SÜTUN: BELGE NO
                dtEkran.Columns.Add("Malzeme Kodu", typeof(string));
                dtEkran.Columns.Add("Barkod", typeof(string));
                dtEkran.Columns.Add("Malzeme Adı", typeof(string));
                dtEkran.Columns.Add("Açıklama", typeof(string));
                dtEkran.Columns.Add("Sipariş Adedi", typeof(int));
                dtEkran.Columns.Add("Okutulan", typeof(int));

                foreach (DataRow satir in filtrelenmisSatirlar)
                {
                    string malzemeKodu = satir["Malzeme"].ToString().Trim();
                    var urun = yerelUrunler.FirstOrDefault(u => u.UrunKodu == malzemeKodu);
                    string barkod = urun != null && !string.IsNullOrWhiteSpace(urun.Barkod) ? urun.Barkod : "BARKOD YOK";

                    dtEkran.Rows.Add(secilenBelge, malzemeKodu, barkod, satir["MalzemeAdi"].ToString().Trim(), satir["SecenekAciklamasi"].ToString().Trim(), Convert.ToInt32(Convert.ToDecimal(satir["Bakiye"])), 0);
                }

                dgvMalzemeler.DataSource = dtEkran;
                dgvMalzemeler.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            }
            else MessageBox.Show("Sipariş bulunamadı!", "Bulunamadı");
        }

        // YENİ BUTON: Mevcut listeyi silmeden, yeni siparişi listenin altına ekler (Konsolidasyon).
        private void btnUzerineEkle_Click(object sender, EventArgs e)
        {
            string secilenBelge = cmbBelgeNo.Text.Trim();
            if (string.IsNullOrEmpty(secilenBelge)) { MessageBox.Show("Lütfen bir Belge No seçin.", "Uyarı"); return; }

            // Eğer tablo boşsa direkt normal "Ara" butonunu tetikle
            if (dgvMalzemeler.DataSource == null || !(dgvMalzemeler.DataSource is DataTable))
            {
                btnSevkAra_Click(null, null); return;
            }

            DataTable dtEkran = (DataTable)dgvMalzemeler.DataSource;
            if (dtEkran.Rows.Count == 0)
            {
                btnSevkAra_Click(null, null); return;
            }

            // Bu sipariş zaten eklendiyse uyar
            foreach (DataRow r in dtEkran.Rows)
            {
                if (r["Belge No"].ToString() == secilenBelge) { MessageBox.Show("Bu sipariş listeye zaten eklenmiş!", "Mükerrer Kayıt"); return; }
            }

            DataRow[] filtrelenmisSatirlar = dtTumSiparisler.Select($"BelgeNo LIKE '%{secilenBelge}%'");

            if (filtrelenmisSatirlar.Length > 0)
            {
                // 🛡️ MÜŞTERİ KARIŞTIRMA ZIRHI: Ekrandaki mevcut müşteri ile yeni seçilen belgenin müşterisi aynı mı?
                string ekrandakiMusteri = txtMusteriAdi.Text.Trim();
                string yeniBelgeMusterisi = filtrelenmisSatirlar[0]["MusteriAdi"].ToString().Trim();

                if (!ekrandakiMusteri.Equals(yeniBelgeMusterisi, StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show($"DUR! Ahmet'in malı Mehmet'e karışamaz.\n\nŞu an ekranda '{ekrandakiMusteri}' müşterisinin siparişleri var, ancak eklemeye çalıştığınız belge '{yeniBelgeMusterisi}' isimli müşteriye ait.\n\nFarklı müşterilerin siparişlerini tek bir sevkiyatta birleştiremezsiniz!", "Kritik Hata: Müşteri Uyuşmazlığı", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return; // İşlemi iptal et
                }

                // Eğer müşteri aynıysa sorun yok, ekleme işlemine devam et
                var yerelUrunler = DataAccess.GetAllUrunler();
                foreach (DataRow satir in filtrelenmisSatirlar)
                {
                    string malzemeKodu = satir["Malzeme"].ToString().Trim();
                    var urun = yerelUrunler.FirstOrDefault(u => u.UrunKodu == malzemeKodu);
                    string barkod = urun != null && !string.IsNullOrWhiteSpace(urun.Barkod) ? urun.Barkod : "BARKOD YOK";

                    dtEkran.Rows.Add(secilenBelge, malzemeKodu, barkod, satir["MalzemeAdi"].ToString().Trim(), satir["SecenekAciklamasi"].ToString().Trim(), Convert.ToInt32(Convert.ToDecimal(satir["Bakiye"])), 0);
                }
                MessageBox.Show($"'{secilenBelge}' nolu sipariş başarıyla listeye ilave edildi! Artık ürünleri okutmaya devam edebilirsiniz.", "Siparişler Birleştirildi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else MessageBox.Show("Sipariş bulunamadı!", "Bulunamadı");
        }
        #endregion

        #region 🔫 13.5 SEVKİYAT BARKOD OKUTMA VE PALETLEME

        // Kullanıcının sevk edilecek ürünleri el terminali (okuyucu) ile tek tek okuttuğu ana motor.
        private void txtBarkod_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                string okutulanBarkod = txtBarkod.Text.Trim();
                if (string.IsNullOrEmpty(okutulanBarkod)) return;

                if (cmbAktifPalet.SelectedItem == null)
                {
                    MessageBox.Show("Lütfen ürünleri okutmadan önce sağdan bir AKTİF PALET seçin!", "Palet Seçilmedi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                int aktifPaletSutunIndex = cmbAktifPalet.SelectedIndex;
                bool urunBulundu = false;

                // 🌟 MÜKEMMEL MANTIK (FİFO): Tablodaki satırları gez, barkodu eşleşen ve ADEDİ HENÜZ DOLMAMIŞ ilk satırı bul!
                DataGridViewRow hedefSatir = null;
                foreach (DataGridViewRow satir in dgvMalzemeler.Rows)
                {
                    if (satir.Cells["Barkod"].Value != null && satir.Cells["Malzeme Kodu"].Value != null)
                    {
                        string tablodakiBarkod = satir.Cells["Barkod"].Value.ToString().Trim();
                        string tablodakiMalzeme = satir.Cells["Malzeme Kodu"].Value.ToString().Trim();

                        if (tablodakiBarkod == okutulanBarkod || tablodakiMalzeme == okutulanBarkod)
                        {
                            int sip = Convert.ToInt32(satir.Cells["Sipariş Adedi"].Value);
                            int oku = Convert.ToInt32(satir.Cells["Okutulan"].Value);

                            // Ürün bulundu ve kotası dolmadıysa bunu hedef seç ve döngüyü kır
                            if (oku < sip) { hedefSatir = satir; break; }
                            // Eğer kotası dolduysa, belki başka bir siparişte aynısından vardır diye döngüye devam et
                        }
                    }
                }

                // Eğer kota dolmamış bir satır bulduysak işlemi ona uygula
                if (hedefSatir != null)
                {
                    urunBulundu = true;
                    int siparisAdedi = Convert.ToInt32(hedefSatir.Cells["Sipariş Adedi"].Value);
                    int okutulanAdet = Convert.ToInt32(hedefSatir.Cells["Okutulan"].Value);

                    okutulanAdet++;
                    hedefSatir.Cells["Okutulan"].Value = okutulanAdet;

                    if (okutulanAdet == siparisAdedi)
                    {
                        hedefSatir.DefaultCellStyle.BackColor = Color.LightGreen;
                        try { string wavYolu = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "basarili.wav"); if (System.IO.File.Exists(wavYolu)) new System.Media.SoundPlayer(wavYolu).Play(); else System.Media.SystemSounds.Asterisk.Play(); } catch { }
                    }
                    else hedefSatir.DefaultCellStyle.BackColor = Color.LightYellow;

                    // --------- PALETE EKLEME MANTIĞI ---------
                    string urunAdi = hedefSatir.Cells["Malzeme Adı"].Value.ToString();
                    string aitOlduguBelge = hedefSatir.Cells["Belge No"].Value.ToString();

                    // 🌟 İŞTE EKSİK OLAN VE HATAYI ÇÖZECEK SATIR BURASI:
                    string tablodakiBarkod = hedefSatir.Cells["Barkod"].Value.ToString().Trim();

                    bool paletSutunundaVarMi = false;

                    foreach (DataGridViewRow paletSatiri in dgvPaletMatrisi.Rows)
                    {
                        if (paletSatiri.Cells[aktifPaletSutunIndex].Value != null)
                        {
                            string hucreMetni = paletSatiri.Cells[aktifPaletSutunIndex].Value.ToString();
                            if (hucreMetni.Contains(tablodakiBarkod) && hucreMetni.Contains(aitOlduguBelge)) // Hem ürün hem belge uyuşmalı
                            {
                                string[] parcalar = hucreMetni.Split(new string[] { "| Adet: " }, StringSplitOptions.None);
                                if (parcalar.Length == 2)
                                {
                                    int mevcutPaletAdeti = int.Parse(parcalar[1]);
                                    paletSatiri.Cells[aktifPaletSutunIndex].Value = $"{parcalar[0]}| Adet: {mevcutPaletAdeti + 1}";
                                }
                                paletSutunundaVarMi = true; break;
                            }
                        }
                    }

                    if (!paletSutunundaVarMi)
                    {
                        bool bosHucreBulundu = false;
                        foreach (DataGridViewRow paletSatiri in dgvPaletMatrisi.Rows)
                        {
                            if (paletSatiri.Cells[aktifPaletSutunIndex].Value == null || string.IsNullOrWhiteSpace(paletSatiri.Cells[aktifPaletSutunIndex].Value.ToString()))
                            {
                                paletSatiri.Cells[aktifPaletSutunIndex].Value = $"{tablodakiBarkod} - {urunAdi} ({aitOlduguBelge}) | Adet: 1";
                                bosHucreBulundu = true; break;
                            }
                        }

                        if (!bosHucreBulundu)
                        {
                            int yeniSatirIndex = dgvPaletMatrisi.Rows.Add();
                            dgvPaletMatrisi.Rows[yeniSatirIndex].Cells[aktifPaletSutunIndex].Value = $"{tablodakiBarkod} - {urunAdi} ({aitOlduguBelge}) | Adet: 1";
                        }
                    }
                }
                else
                {
                    // Satır hiç bulunamadıysa VEYA bulundu ama kotası çoktan dolduysa Hata Ver
                    // ... (Senin eski Hata sesi ve MessageBox kodların burada çalışmaya devam ediyor)

                    if (!urunBulundu)
                    {
                        try
                        {
                            string wavYolu = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "hata.wav");
                            if (System.IO.File.Exists(wavYolu)) new System.Media.SoundPlayer(wavYolu).Play();
                            else System.Media.SystemSounds.Hand.Play();
                        }
                        catch { }

                        MessageBox.Show("HATA! Okutulan BARKOD sipariş listesinde (ekrandaki tabloda) bulunamadı!", "Yanlış Ürün", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }

                    txtBarkod.Clear();
                    txtBarkod.Focus();
                }
            }
        }

        // Olası kilitlenmelerde SQL bağlantısını ve önbellekteki çekilmiş siparişleri sıfırlar.
        private void btnTumVerileriTemizle_Click(object sender, EventArgs e)
        {
            DialogResult onay = MessageBox.Show(
                "DİKKAT: SQL bağlantı ayarları, çekilen tüm siparişler ve önbellekteki veriler SİLİNECEKTİR!",
                "Büyük Temizlik Onayı", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (onay == DialogResult.Yes)
            {
                Properties.Settings.Default.SqlSunucu = ""; Properties.Settings.Default.SqlVeritabani = "";
                Properties.Settings.Default.SqlKullanici = ""; Properties.Settings.Default.SqlSifre = "";
                Properties.Settings.Default.Save();

                cmbBelgeNo.Text = ""; cmbBelgeNo.Items.Clear();
                txtMusteriAdi.Clear(); txtSevkMusteri.Clear(); dgvMalzemeler.DataSource = null;
                if (dtTumSiparisler != null) dtTumSiparisler.Clear();

                MessageBox.Show("Tüm operasyonel veriler sıfırlandı!", "Temizlik Başarılı");
            }
        }

        #endregion

        #region 📝 13.6 TAM VE KISMİ SEVKİYAT İŞLEMLERİ
        // Tüm ürünlerin eksiksiz okutulduğunu onaylar, arşive kaydeder ve belgeyi kalıcı olarak kara listeye ekler.
        private void btnTamSevk_Click(object sender, EventArgs e)
        {
            if (dgvMalzemeler.Rows.Count == 0) return;

            bool eksikVarMi = false;
            foreach (DataGridViewRow satir in dgvMalzemeler.Rows)
            {
                if (satir.IsNewRow || satir.Cells["Malzeme Kodu"].Value == null) continue;

                // Sipariş edilen ile okutulan arasında fark var mı diye kontrol et
                if (Convert.ToInt32(satir.Cells["Okutulan"].Value) < Convert.ToInt32(satir.Cells["Sipariş Adedi"].Value))
                {
                    eksikVarMi = true; break;
                }
            }

            if (eksikVarMi) MessageBox.Show("DUR! Eksik okutulmuş ürünler var, Tam Sevk yapılamaz!", "Eksik Ürün", MessageBoxButtons.OK, MessageBoxIcon.Error);
            else
            {
                MessageBox.Show("HARİKA! Tüm ürünler eksiksiz. Tam Sevk onaylandı!", "Başarılı");

                // 🌟 ÇOKLU BELGE (KONSOLİDE) KAPATMA MANTIĞI
                // Tabloda kaç farklı Belge No varsa hepsini bul ve listeye al
                HashSet<string> bitenBelgeler = new HashSet<string>();
                foreach (DataGridViewRow satir in dgvMalzemeler.Rows)
                {
                    if (satir.Cells["Belge No"].Value != null)
                        bitenBelgeler.Add(satir.Cells["Belge No"].Value.ToString());
                }

                // Birleştirilmiş tek bir isim yarat (Örn: SE-001_SE-002)
                string birlesikBelgeIsmi = string.Join("_", bitenBelgeler);

                // İşlemi geçmiş arşivi (CSV) olarak tek bir dosya halinde kaydet
                SevkiyatArsivle(birlesikBelgeIsmi, txtMusteriAdi.Text, txtSevkMusteri.Text);

                // Her bir belgeyi tek tek Ghost Modu (Kara Liste) listesine at ve RAM'den uçur
                foreach (string bitenBelge in bitenBelgeler)
                {
                    KaliciKaraListeyeEkle(bitenBelge);

                    for (int i = dtTumSiparisler.Rows.Count - 1; i >= 0; i--)
                    {
                        if (dtTumSiparisler.Rows[i]["BelgeNo"].ToString().Trim() == bitenBelge)
                        {
                            dtTumSiparisler.Rows.RemoveAt(i);
                        }
                    }
                }
                dtTumSiparisler.AcceptChanges();

                // 🧹 Başarılı işlem sonrası sessiz arayüz temizliği
                txtMusteriAdi.Clear();
                txtSevkMusteri.Clear();
                txtBarkod.Clear();
                cmbBelgeNo.Items.Clear();
                cmbBelgeNo.Text = "";
                cmbSevkPaletSayisi.SelectedIndex = -1;

                dgvMalzemeler.DataSource = null; // ZIRH: Tabloyu tam sıfırlar

                dgvPaletMatrisi.Columns.Clear();
                dgvPaletMatrisi.Rows.Clear();
                cmbAktifPalet.Items.Clear();

                // Müşteri kutusunu kalan güncel siparişlere göre yeniden doldur
                cmbMusteri.Items.Clear();
                var kalanMusteriler = dtTumSiparisler.AsEnumerable()
                                                            .Select(r => r.Field<string>("MusteriAdi")?.Trim())
                                                            .Where(m => !string.IsNullOrEmpty(m))
                                                            .Distinct()
                                                            .OrderBy(m => m)
                                                            .ToArray();
                cmbMusteri.Items.AddRange(kalanMusteriler);
            }
        }

        // Eksik ürünleri geride bırakarak, sadece okutulmuş olan miktarı kısmi olarak gönderir
        private void btnKismiSevk_Click(object sender, EventArgs e)
        {
            if (dgvMalzemeler.Rows.Count == 0) return;

            List<string> eksikListesi = new List<string>();
            foreach (DataGridViewRow satir in dgvMalzemeler.Rows)
            {
                if (satir.IsNewRow || satir.Cells["Malzeme Kodu"].Value == null) continue; // Boşluklarda çökmeyi önleyen zırh

                int siparis = Convert.ToInt32(satir.Cells["Sipariş Adedi"].Value);
                int okutulan = Convert.ToInt32(satir.Cells["Okutulan"].Value);

                // Hangi ürünlerin eksik olduğunu rapora/mesaja yansıtmak için topla
                if (okutulan < siparis)
                {
                    eksikListesi.Add($"- {satir.Cells["Malzeme Kodu"].Value} | Gerekli: {siparis}, Okutulan: {okutulan}");
                }
            }

            if (eksikListesi.Count > 0)
            {
                if (MessageBox.Show("Eksik ürünler var. Yine de Kısmi Sevk yapılsın mı?\n\nEksikler:\n" + string.Join("\n", eksikListesi), "Kısmi Sevk Onayı", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    MessageBox.Show("Kısmi Sevk onaylandı!");

                    // 🌟 ÇOKLU BELGE (KONSOLİDE) KAPATMA MANTIĞI
                    HashSet<string> bitenBelgeler = new HashSet<string>();
                    foreach (DataGridViewRow satir in dgvMalzemeler.Rows)
                    {
                        if (satir.Cells["Belge No"].Value != null)
                            bitenBelgeler.Add(satir.Cells["Belge No"].Value.ToString());
                    }

                    string birlesikBelgeIsmi = string.Join("_", bitenBelgeler);
                    SevkiyatArsivle(birlesikBelgeIsmi, txtMusteriAdi.Text, txtSevkMusteri.Text);

                    foreach (string bitenBelge in bitenBelgeler)
                    {
                        KaliciKaraListeyeEkle(bitenBelge);

                        for (int i = dtTumSiparisler.Rows.Count - 1; i >= 0; i--)
                        {
                            if (dtTumSiparisler.Rows[i]["BelgeNo"].ToString().Trim() == bitenBelge)
                            {
                                dtTumSiparisler.Rows.RemoveAt(i);
                            }
                        }
                    }
                    dtTumSiparisler.AcceptChanges();

                    // 🧹 Başarılı işlem sonrası sessiz arayüz temizliği
                    txtMusteriAdi.Clear();
                    txtSevkMusteri.Clear();
                    txtBarkod.Clear();
                    cmbBelgeNo.Items.Clear();
                    cmbBelgeNo.Text = "";
                    cmbSevkPaletSayisi.SelectedIndex = -1;

                    dgvMalzemeler.DataSource = null; // ZIRH: Tabloyu tam sıfırlar

                    dgvPaletMatrisi.Columns.Clear();
                    dgvPaletMatrisi.Rows.Clear();
                    cmbAktifPalet.Items.Clear();

                    // Müşteri kutusunu güncel siparişlere göre yeniden doldur
                    cmbMusteri.Items.Clear();
                    var kalanMusteriler = dtTumSiparisler.AsEnumerable()
                                                                .Select(r => r.Field<string>("MusteriAdi")?.Trim())
                                                                .Where(m => !string.IsNullOrEmpty(m))
                                                                .Distinct()
                                                                .OrderBy(m => m)
                                                                .ToArray();
                    cmbMusteri.Items.AddRange(kalanMusteriler);
                }
            }
            else
            {
                // Hiç eksik yoksa Kısmi Sevk butonunu kullanmak saçma olur, o yüzden Tam Sevk'e yönlendir
                MessageBox.Show("Hiçbir ürün eksik değil! Lütfen siparişi bitirmek için 'Tam Sevket' butonunu kullanın.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        #endregion

        #region 🧠 13.7 KALICI HAFIZA MOTORU (TXT)
        // Bitirilen siparişlerin bir daha asla karşına çıkmaması için onları yerel bir .txt dosyasına yazar.
        private string KaraListeDosyaYolu()
        {
            // Belgelerimde veya AppData'da güvenli bir liste tutuyoruz
            string klasor = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TamgaApp");
            if (!Directory.Exists(klasor)) Directory.CreateDirectory(klasor);
            return Path.Combine(klasor, "KapananBelgeler.txt");
        }

        // Program açıldığında veya Yenile dendiğinde RAM'deki listeyi bu txt dosyasından okuyarak doldurur
        private void KaraListeyiYukle()
        {
            string yol = KaraListeDosyaYolu();
            if (File.Exists(yol))
            {
                // Dosyadaki tüm bitmiş belge numaralarını RAM'e aktar, boşlukları temizle
                TamamlananBelgeNolar = File.ReadAllLines(yol)
                                           .Where(x => !string.IsNullOrWhiteSpace(x))
                                           .Select(x => x.Trim()) // ZIRH: Text dosyasında da boşluk olursa uçur
                                           .ToList();
            }
        }

        // Verilen belge numarasını kalıcı olarak txt dosyasının en altına yazar
        private void KaliciKaraListeyeEkle(string belgeNo)
        {
            belgeNo = belgeNo.Trim(); // ZIRH
            if (!string.IsNullOrEmpty(belgeNo) && !TamamlananBelgeNolar.Contains(belgeNo))
            {
                TamamlananBelgeNolar.Add(belgeNo);
                // Belgeyi dosyanın en sonuna kalıcı olarak yaz (Alt alta)
                File.AppendAllText(KaraListeDosyaYolu(), belgeNo + Environment.NewLine);
            }
        }
        #endregion

        #endregion

        // =========================================================================================

        #region 📊 14. DEPO SAYIM VE ENVANTER KONTROLÜ

        #region 📋 14.1 SAYIM TABLOSU VE İLK AYARLAR
        // Fiili depo sayım ekranındaki DataGridView tablosunun sütun düzenini,
        // genişlik modlarını ve satır seçim kurallarını belirler.
        private void SayimSisteminiHazirla()
        {
            if (dgvSayim == null) return;

            dgvSayim.ColumnCount = 3;
            dgvSayim.Columns[0].Name = "Barkod"; dgvSayim.Columns[0].ReadOnly = true;
            dgvSayim.Columns[1].Name = "Açıklama"; dgvSayim.Columns[1].ReadOnly = true;
            dgvSayim.Columns[2].Name = "Adet"; // Sayım esnasında adet elle değiştirilebilir

            dgvSayim.AllowUserToAddRows = false;
            dgvSayim.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvSayim.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill; // Tabloyu ekrana tam sığdır
        }
        #endregion

        #region 🔍 14.2 ANLIK BARKOD OKUTMA VE ADET ARTTIRMA
        // Depoda el terminali veya kablolu okuyucu ile okutulan ürünün 
        // listede varsa adedini 1 arttırır, yoksa yerel veritabanından adını çekerek yeni satır açar.
        private void TxtSayimBarkod_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true; // Hata sesini (bip) engelle
                string okunanBarkod = txtSayimBarkod.Text.Trim();
                if (string.IsNullOrEmpty(okunanBarkod)) return;

                bool urunZatenVarMi = false;

                // 1. ADIM: Okutulan ürün listede daha önce taranmış mı kontrol et
                foreach (DataGridViewRow row in dgvSayim.Rows)
                {
                    if (row.Cells["Barkod"].Value != null && row.Cells["Barkod"].Value.ToString() == okunanBarkod)
                    {
                        // Ürün tabloda zaten mevcutsa, miktarını 1 birim yükselt
                        int mevcutAdet = Convert.ToInt32(row.Cells["Adet"].Value);
                        row.Cells["Adet"].Value = mevcutAdet + 1;
                        urunZatenVarMi = true;
                        break;
                    }
                }

                // 2. ADIM: Ürün ilk defa okutuluyorsa yerel barkod tablosundan açıklamasını sorgula
                if (!urunZatenVarMi)
                {
                    Urun bulunanUrun = DataAccess.GetUrunByBarkod(okunanBarkod);
                    string aciklama = bulunanUrun != null ? bulunanUrun.Aciklama : "SİSTEMDE KAYITLI DEĞİL!";

                    // Tabloya 1 adet olarak yeni kayıt gir
                    dgvSayim.Rows.Add(okunanBarkod, aciklama, 1);
                }

                // Bir sonraki ürün taraması için kutuyu temizle ve odağı (focus) kaybetme
                txtSayimBarkod.Clear();
                txtSayimBarkod.Focus();
            }
        }
        #endregion

        #region 💾 14.3 RAPORLAMA VE GEÇMİŞ SAYIM ARŞİVİ
        // Sayım işlemi bittiğinde verileri Masaüstündeki arşive CSV olarak mühürler.
        private void BtnSayimBitir_Click(object sender, EventArgs e)
        {
            if (dgvSayim.Rows.Count == 0) { MessageBox.Show("Tabloda sayılmış ürün yok!", "Uyarı"); return; }

            string raporIsmi = txtSayimRaporAdi.Text.Trim();
            if (string.IsNullOrEmpty(raporIsmi)) { MessageBox.Show("Lütfen kaydetmeden önce bir Sayım Rapor Adı girin (Örn: Depo_A_Sayimi).", "İsim Eksik"); return; }

            // Masaüstünde ana arşiv klasörünü oluştur
            string anaYol = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "TamgaApp Sayım Raporları");
            if (!Directory.Exists(anaYol)) Directory.CreateDirectory(anaYol);

            // Dosya ismini tarih, saat ve rapor adı kombinasyonuyla eşsiz hale getir
            string dosyaAdi = $"{DateTime.Now:yyyy-MM-dd_HHmm}_{raporIsmi}.csv";
            string tamYol = Path.Combine(anaYol, dosyaAdi);

            // Verileri Excel ve notepad ile uyumlu olacak şekilde UTF8 formatında satır satır yaz dök
            using (StreamWriter sw = new StreamWriter(tamYol, false, System.Text.Encoding.UTF8))
            {
                sw.WriteLine("Barkod;Açıklama;Adet"); // Başlık satırı
                foreach (DataGridViewRow row in dgvSayim.Rows)
                {
                    if (row.Cells[0].Value != null)
                        sw.WriteLine($"{row.Cells[0].Value};{row.Cells[1].Value};{row.Cells[2].Value}");
                }
            }

            MessageBox.Show($"Sayım başarıyla tamamlandı ve arşivlendi!\nKayıt Yeri: {tamYol}", "İşlem Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);

            // Yeni sayım operasyonu için ekranı sıfırla ve geçmiş listesini tazele
            dgvSayim.Rows.Clear();
            txtSayimRaporAdi.Clear();
            BtnSayimYenile_Click(null, null);
        }

        // Arşiv klasöründeki eski sayımları tarar ve oluşturulma tarihine göre en yeniden en eskiye sıralar
        private void BtnSayimYenile_Click(object sender, EventArgs e)
        {
            string anaYol = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "TamgaApp Sayım Raporları");
            if (!Directory.Exists(anaYol)) return;

            DirectoryInfo di = new DirectoryInfo(anaYol);
            FileInfo[] raporlar = di.GetFiles("*.csv").OrderByDescending(f => f.CreationTime).ToArray();

            lstSayimRaporlari.DataSource = null;
            lstSayimRaporlari.DataSource = raporlar;
            lstSayimRaporlari.DisplayMember = "Name"; // Sadece dosya adını arayüzde göster
        }

        // Listeden seçilen eski bir sayım raporunu okur, dinamik olarak yeni bir popup form oluşturur ve verileri canlı filtreli şekilde sunar.
        private void BtnSayimAc_Click(object sender, EventArgs e)
        {
            if (lstSayimRaporlari.SelectedItem == null) { MessageBox.Show("Açmak için bir rapor seçin."); return; }

            FileInfo secilenDosya = (FileInfo)lstSayimRaporlari.SelectedItem;
            string dosyaYolu = secilenDosya.FullName;

            // Dinamik Popup Form Kurulumu
            Form frm = new Form { Text = "Sayım Raporu Detayı: " + secilenDosya.Name, Size = new Size(1000, 700), StartPosition = FormStartPosition.CenterScreen, Icon = this.Icon };

            // Canlı Filtreleme Paneli (Üst Kısım)
            Panel pnlUst = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.FromArgb(15, 76, 58) }; // Elit koyu yeşil
            Label lblAra = new Label { Text = "🔎 Canlı Filtre (Barkod veya İsim):", ForeColor = Color.White, AutoSize = true, Location = new Point(20, 15), Font = new Font("Segoe UI", 12, FontStyle.Bold) };
            TextBox txtFiltre = new TextBox { Location = new Point(320, 12), Width = 400, Font = new Font("Segoe UI", 12) };

            pnlUst.Controls.Add(lblAra);
            pnlUst.Controls.Add(txtFiltre);

            // Verileri Gösterecek Grid Kurulumu
            DataGridView dgv = new DataGridView { Dock = DockStyle.Fill, AllowUserToAddRows = false, ReadOnly = true, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, SelectionMode = DataGridViewSelectionMode.FullRowSelect, BackgroundColor = Color.WhiteSmoke };

            frm.Controls.Add(dgv);
            frm.Controls.Add(pnlUst);

            // Dosyayı satır satır çöz ve tablo sütunlarını/satırlarını inşa et
            string[] satirlar = File.ReadAllLines(dosyaYolu, System.Text.Encoding.UTF8);
            if (satirlar.Length > 0)
            {
                string[] basliklar = satirlar[0].Split(';'); // Sütun başlıklarını çek (Barkod, Açıklama, Adet)
                foreach (string b in basliklar) dgv.Columns.Add(b, b);

                for (int i = 1; i < satirlar.Length; i++)
                {
                    if (!string.IsNullOrWhiteSpace(satirlar[i])) dgv.Rows.Add(satirlar[i].Split(';'));
                }
            }

            // Arama kutusuna her harf yazıldığında (Canlı Filtre) Grid satırlarını gizle/göster
            txtFiltre.TextChanged += (s, ev) =>
            {
                string aranan = txtFiltre.Text.Trim().ToLower();
                foreach (DataGridViewRow row in dgv.Rows)
                {
                    row.Visible = string.IsNullOrEmpty(aranan) ||
                                  (row.Cells[0].Value != null && row.Cells[0].Value.ToString().ToLower().Contains(aranan)) ||
                                  (row.Cells[1].Value != null && row.Cells[1].Value.ToString().ToLower().Contains(aranan));
                }
            };

            frm.ShowDialog(); // Formu kullanıcıya göster
        }
        #endregion

        #region 🚛 14.4 DESTEKLEYİCİ SEVKİYAT VE YARIM KALANLAR (ASKI) METOTLARI

        // Seçilen palet sayısı kadar ambar palet matrisinde dinamik sütun (Palet 1, Palet 2...) inşa eder.
        private void cmbSevkPaletSayisi_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbSevkPaletSayisi.SelectedItem == null) return;

            int paletSayisi = Convert.ToInt32(cmbSevkPaletSayisi.SelectedItem.ToString());

            dgvPaletMatrisi.Columns.Clear();
            dgvPaletMatrisi.Rows.Clear();
            cmbAktifPalet.Items.Clear();

            for (int i = 1; i <= paletSayisi; i++)
            {
                string paletAdi = $"{i}. Palet";
                dgvPaletMatrisi.Columns.Add($"Palet_{i}", paletAdi);
                cmbAktifPalet.Items.Add(paletAdi);
            }

            if (cmbAktifPalet.Items.Count > 0) cmbAktifPalet.SelectedIndex = 0;
        }

        // Sevkiyat esnasında işi yarıda kesilen (Örn: Paydos, mola) sevkiyatları tüm verileriyle diske (JSON) askıya alır.
        private void btnSevkAskayaAl_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(cmbBelgeNo.Text) || dgvMalzemeler.Rows.Count == 0)
            {
                MessageBox.Show("Askıya alınacak açık bir sevkiyat yok!", "Hata"); return;
            }

            YarimSevkiyatHafizasi hafiza = new YarimSevkiyatHafizasi
            {
                MusteriAdi = txtMusteriAdi.Text,
                BelgeNo = cmbBelgeNo.Text,
                SevkMusteri = txtSevkMusteri.Text,
                PaletSayisi = cmbSevkPaletSayisi.SelectedIndex != -1 ? Convert.ToInt32(cmbSevkPaletSayisi.SelectedItem) : 0,
                KayitTarihi = DateTime.Now
            };

            foreach (DataGridViewRow row in dgvMalzemeler.Rows)
            {
                if (row.IsNewRow) continue;

                if (row.Cells["Malzeme Kodu"].Value != null)
                {
                    string barkod = row.Cells["Malzeme Kodu"].Value.ToString();

                    int okutulan = 0;
                    if (row.Cells["Okutulan"].Value != null)
                    {
                        int.TryParse(row.Cells["Okutulan"].Value.ToString(), out okutulan);
                    }

                    if (!hafiza.AnaOkutulanlar.ContainsKey(barkod))
                    {
                        hafiza.AnaOkutulanlar.Add(barkod, okutulan);
                    }
                }
            }

            for (int i = 0; i < dgvPaletMatrisi.Rows.Count; i++)
            {
                hafiza.PaletMatrisiDurumu[i] = new Dictionary<int, string>();
                for (int j = 0; j < dgvPaletMatrisi.Columns.Count; j++)
                {
                    if (dgvPaletMatrisi.Rows[i].Cells[j].Value != null)
                    {
                        hafiza.PaletMatrisiDurumu[i][j] = dgvPaletMatrisi.Rows[i].Cells[j].Value.ToString();
                    }
                }
            }

            string anaYol = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "TamgaApp Yarım Sevkiyatlar");
            if (!Directory.Exists(anaYol)) Directory.CreateDirectory(anaYol);

            string dosyaAdi = $"{cmbBelgeNo.Text}_{DateTime.Now:yyyyMMdd_HHmm}.json";
            string tamYol = Path.Combine(anaYol, dosyaAdi);

            System.IO.File.WriteAllText(tamYol, JsonConvert.SerializeObject(hafiza, Formatting.Indented));

            MessageBox.Show($"Sevkiyat başarıyla ASKIYA ALINDI!\nİstediğiniz zaman kaldığınız yerden devam edebilirsiniz.", "Kaydedildi", MessageBoxButtons.OK, MessageBoxIcon.Information);

            // Ekran temizleme ritüeli
            txtMusteriAdi.Clear(); txtSevkMusteri.Clear(); txtBarkod.Clear();
            cmbBelgeNo.Items.Clear(); cmbBelgeNo.Text = ""; cmbSevkPaletSayisi.SelectedIndex = -1;
            if (dgvMalzemeler.DataSource == null) dgvMalzemeler.Rows.Clear();
            dgvPaletMatrisi.Columns.Clear(); dgvPaletMatrisi.Rows.Clear(); cmbAktifPalet.Items.Clear();
        }

        // Askıya alınmış olan yarım kayıtları bulur ve tarihe göre sıralayarak listeler.
        private void btnYarimGetir_Click(object sender, EventArgs e)
        {
            string anaYol = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "TamgaApp Yarım Sevkiyatlar");
            if (!Directory.Exists(anaYol))
            {
                MessageBox.Show("Henüz askıya alınmış hiçbir yarım sevkiyat bulunamadı.", "Kayıt Yok", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DirectoryInfo di = new DirectoryInfo(anaYol);
            FileInfo[] dosyalar = di.GetFiles("*.json").OrderByDescending(f => f.LastWriteTime).ToArray();

            lstYarimSevkler.DataSource = null;
            lstYarimSevkler.DataSource = dosyalar;
            lstYarimSevkler.DisplayMember = "Name";
        }

        // Askıdaki JSON dosyasını çözer, şayet program yeni açıldıysa arka planda otomatik "Yenile" (SQL Çekim) zırhını tetikler
        // ve tüm palet matrisi durumunu ve okutulan miktarları tek tıkla ekrana geri yükler.
        private void btnYarimAc_Click(object sender, EventArgs e)
        {
            if (lstYarimSevkler.SelectedItem == null)
            {
                MessageBox.Show("Lütfen devam etmek istediğiniz yarım sevkiyatı listeden seçin!", "Seçim Eksik"); return;
            }

            // 🛡️ AKILLI ZIRH: Eğer program yeni açıldıysa ve sipariş havuzu boşsa çaktırmadan veritabanını yenile
            if (dtTumSiparisler == null || dtTumSiparisler.Columns.Count == 0 || dtTumSiparisler.Rows.Count == 0)
            {
                btnSiparisYenile_Click(null, null);

                if (dtTumSiparisler == null || dtTumSiparisler.Columns.Count == 0)
                {
                    MessageBox.Show("Güncel siparişler veritabanından çekilemediği için askıdaki kayıt açılamaz. Lütfen önce 'Yenile' işleminin çalıştığından emin olun.", "Veri Yok", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            FileInfo secilenDosya = (FileInfo)lstYarimSevkler.SelectedItem;

            try
            {
                string jsonIcerik = File.ReadAllText(secilenDosya.FullName);
                YarimSevkiyatHafizasi hafiza = JsonConvert.DeserializeObject<YarimSevkiyatHafizasi>(jsonIcerik);

                if (hafiza == null) return;

                cmbMusteri.Text = hafiza.MusteriAdi;
                txtMusteriAdi.Text = hafiza.MusteriAdi;
                txtSevkMusteri.Text = hafiza.SevkMusteri;
                cmbSevkPaletSayisi.SelectedItem = hafiza.PaletSayisi.ToString();
                cmbBelgeNo.Text = hafiza.BelgeNo;

                btnSevkAra_Click(null, null);

                // 🛡️ ZIRHLI YÜKLEME DÖNGÜSÜ: Eski okutulan miktarları hücrelere basar ve duruma göre yeşil/sarı boyar
                foreach (DataGridViewRow row in dgvMalzemeler.Rows)
                {
                    if (row.IsNewRow || row.Cells["Malzeme Kodu"].Value == null) continue;

                    string barkod = row.Cells["Malzeme Kodu"].Value.ToString();
                    if (hafiza.AnaOkutulanlar.ContainsKey(barkod))
                    {
                        int eskiOkutulan = hafiza.AnaOkutulanlar[barkod];
                        int siparisAdedi = Convert.ToInt32(row.Cells["Sipariş Adedi"].Value);

                        row.Cells["Okutulan"].Value = eskiOkutulan;
                        row.DefaultCellStyle.BackColor = (eskiOkutulan == siparisAdedi) ? Color.LightGreen : (eskiOkutulan > 0 ? Color.LightYellow : Color.White);
                    }
                }

                dgvPaletMatrisi.Rows.Clear();

                int maxSatirIndex = hafiza.PaletMatrisiDurumu.Keys.Count > 0 ? hafiza.PaletMatrisiDurumu.Keys.Max() : -1;
                for (int i = 0; i <= maxSatirIndex; i++) dgvPaletMatrisi.Rows.Add();

                foreach (var satirKvp in hafiza.PaletMatrisiDurumu)
                {
                    int satirIndex = satirKvp.Key;
                    foreach (var sutunKvp in satirKvp.Value)
                    {
                        int sutunIndex = sutunKvp.Key;
                        dgvPaletMatrisi.Rows[satirIndex].Cells[sutunIndex].Value = sutunKvp.Value;
                    }
                }

                // Geri yükleme bittiği için mükerrer açılmaları önlemek adına geçici askı dosyasını diskten uçur
                File.Delete(secilenDosya.FullName);
                btnYarimGetir_Click(null, null);

                MessageBox.Show($"'{hafiza.BelgeNo}' nolu sevkiyat başarıyla geri yüklendi. Kaldığınız yerden devam edebilirsiniz!", "Sistem Hazır", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Geri yükleme sırasında sistemsel hata oluştu: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Tam veya Kısmi teslimatı biten belgelerin palet içerik dökümlerini gün bazlı CSV arşivine yazar.
        private void SevkiyatArsivle(string belgeNo, string musteri, string sevkMusteri)
        {
            try
            {
                string anaYol = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "TamgaApp Tamamlanan Sevkiyatlar", DateTime.Now.ToString("yyyy-MM-dd"));
                if (!Directory.Exists(anaYol)) Directory.CreateDirectory(anaYol);

                string dosyaAdi = $"{belgeNo}_Teslimat.csv";
                string tamYol = Path.Combine(anaYol, dosyaAdi);

                using (StreamWriter sw = new StreamWriter(tamYol, false, System.Text.Encoding.UTF8))
                {
                    sw.WriteLine("Müşteri;SevkMüşteri;BelgeNo;Tarih");
                    sw.WriteLine($"{musteri};{sevkMusteri};{belgeNo};{DateTime.Now:HH:mm}");
                    sw.WriteLine("--- DETAYLAR ---");
                    sw.WriteLine("Palet No;İçerik");

                    for (int j = 0; j < dgvPaletMatrisi.Columns.Count; j++)
                    {
                        string paletAdi = dgvPaletMatrisi.Columns[j].HeaderText;
                        foreach (DataGridViewRow row in dgvPaletMatrisi.Rows)
                        {
                            if (row.Cells[j].Value != null && !string.IsNullOrWhiteSpace(row.Cells[j].Value.ToString()))
                            {
                                sw.WriteLine($"{paletAdi};{row.Cells[j].Value}");
                            }
                        }
                    }
                }
            }
            catch { }
        }

        // Seçilen tarihteki tamamlanmış sevkiyat raporlarını dökümler halinde ekrana listeler.
        private void btnGecmisSevkleriListele_Click(object sender, EventArgs e)
        {
            string secilenTarih = dtpSevkGecmisTarih.Value.ToString("yyyy-MM-dd");
            string hedefKlasor = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "TamgaApp Tamamlanan Sevkiyatlar", secilenTarih);

            if (!Directory.Exists(hedefKlasor))
            {
                MessageBox.Show("Seçilen tarihte tamamlanmış hiçbir sevkiyat kaydı bulunamadı.", "Arşiv Temiz", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            FileInfo[] sevkDosyalari = new DirectoryInfo(hedefKlasor).GetFiles("*.csv");

            Form frmGununSevkleri = new Form { Text = $"{secilenTarih} Tarihli Sevkiyat Listesi", Size = new Size(500, 600), StartPosition = FormStartPosition.CenterScreen, Icon = this.Icon };

            ListBox lstGununSevkleri = new ListBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 11) };
            lstGununSevkleri.DataSource = sevkDosyalari;
            lstGununSevkleri.DisplayMember = "Name";

            Button btnDetayAc = new Button { Dock = DockStyle.Bottom, Height = 50, Text = "Seçilen Sevkiyatın Detayını Aç 🔍", FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(15, 76, 58), ForeColor = Color.White, Font = new Font("Segoe UI", 12, FontStyle.Bold) };

            frmGununSevkleri.Controls.Add(lstGununSevkleri);
            frmGununSevkleri.Controls.Add(btnDetayAc);

            btnDetayAc.Click += (s, ev) =>
            {
                if (lstGununSevkleri.SelectedItem == null) return;
                FileInfo csvDosya = (FileInfo)lstGununSevkleri.SelectedItem;

                Form frmDetay = new Form { Text = "Sevkiyat Rapor Detayı: " + csvDosya.Name, Size = new Size(900, 700), StartPosition = FormStartPosition.CenterScreen, Icon = this.Icon };

                Panel pnlUst = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.FromArgb(45, 52, 54) };
                Label lblFiltre = new Label { Text = "🔎 Palet veya Ürün Ara:", ForeColor = Color.White, Location = new Point(15, 15), Font = new Font("Segoe UI", 11, FontStyle.Bold), AutoSize = true };
                TextBox txtCanliAra = new TextBox { Location = new Point(220, 12), Width = 350, Font = new Font("Segoe UI", 11) };
                Button btnRaporYazdir = new Button { Location = new Point(600, 8), Width = 250, Height = 34, Text = "🖨️ Bu Raporu Yazdır", FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(15, 76, 58), ForeColor = Color.White, Font = new Font("Segoe UI", 11, FontStyle.Bold) };

                pnlUst.Controls.Add(lblFiltre); pnlUst.Controls.Add(txtCanliAra); pnlUst.Controls.Add(btnRaporYazdir);

                DataGridView dgvDetay = new DataGridView { Dock = DockStyle.Fill, AllowUserToAddRows = false, ReadOnly = true, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, BackgroundColor = Color.WhiteSmoke, SelectionMode = DataGridViewSelectionMode.FullRowSelect };
                dgvDetay.Columns.Add("PaletNo", "Palet Numarası");
                dgvDetay.Columns.Add("Icerik", "Okutulan Ürün Bilgisi ve Adeti");

                frmDetay.Controls.Add(dgvDetay); frmDetay.Controls.Add(pnlUst);

                string[] csvSatirlar = File.ReadAllLines(csvDosya.FullName, System.Text.Encoding.UTF8);
                bool detaylarBasladi = false;
                foreach (string satir in csvSatirlar)
                {
                    if (satir.Contains("--- DETAYLAR ---")) { detaylarBasladi = true; continue; }
                    if (detaylarBasladi && !satir.StartsWith("Palet No") && !string.IsNullOrWhiteSpace(satir))
                    {
                        string[] huc = satir.Split(';');
                        if (huc.Length == 2) dgvDetay.Rows.Add(huc[0], huc[1]);
                    }
                }

                txtCanliAra.TextChanged += (senderText, eText) =>
                {
                    string anahtar = txtCanliAra.Text.Trim().ToLower();
                    foreach (DataGridViewRow r in dgvDetay.Rows)
                    {
                        r.Visible = string.IsNullOrEmpty(anahtar) ||
                                    (r.Cells[0].Value != null && r.Cells[0].Value.ToString().ToLower().Contains(anahtar)) ||
                                    (r.Cells[1].Value != null && r.Cells[1].Value.ToString().ToLower().Contains(anahtar));
                    }
                };

                btnRaporYazdir.Click += (sYaz, eYaz) =>
                {
                    PrintDocument pd = new PrintDocument();
                    pd.PrintPage += (sDoc, eDoc) =>
                    {
                        Graphics g = eDoc.Graphics; int yAksis = 60;
                        g.DrawString($"{secilenTarih} SEVKİYAT RAPORU", new Font("Arial", 16, FontStyle.Bold), Brushes.Black, 50, yAksis);
                        yAksis += 50;
                        g.DrawString($"Dosya: {csvDosya.Name}", new Font("Arial", 11, FontStyle.Italic), Brushes.Black, 50, yAksis);
                        yAksis += 40;
                        g.DrawLine(Pens.Black, 50, yAksis, 750, yAksis);
                        yAksis += 20;

                        foreach (DataGridViewRow r in dgvDetay.Rows)
                        {
                            if (r.Visible)
                            {
                                g.DrawString(r.Cells[0].Value?.ToString(), new Font("Arial", 11, FontStyle.Bold), Brushes.Black, 50, yAksis);
                                g.DrawString(r.Cells[1].Value?.ToString(), new Font("Arial", 11), Brushes.Black, 220, yAksis);
                                yAksis += 25;
                            }
                        }
                    };
                    PrintPreviewDialog ppd = new PrintPreviewDialog { Document = pd };
                    ppd.ShowDialog();
                };

                frmDetay.ShowDialog();
            };

            frmGununSevkleri.ShowDialog();
        }
        #endregion

        #endregion

        // =========================================================================================

        #region ⚙️ 15. AYARLAR SEKMESİ (YAZICI ATAMALARI)

        // Ayarlar sekmesindeki kutuları doldurur
        private void YaziciAyarlariniYukle()
        {
            cmbPrintingPages.Items.Clear();
            cmbPrintingPages.Items.Add("Normal Zarf Yazdırma");
            cmbPrintingPages.Items.Add("Çoklu Zarf Yazdırma");
            cmbPrintingPages.Items.Add("Manuel Etiket");

            cmbPrinters.Items.Clear();
            foreach (string printer in System.Drawing.Printing.PrinterSettings.InstalledPrinters)
            {
                cmbPrinters.Items.Add(printer);
            }
        }

        // Sayfa seçimi değiştiğinde, hafızadaki yazıcıyı getirir
        private void cmbPrintingPages_SelectedIndexChanged(object sender, EventArgs e)
        {
            string seciliSayfa = cmbPrintingPages.Text;
            string kayitliYazici = "";

            if (seciliSayfa == "Normal Zarf Yazdırma") kayitliYazici = Properties.Settings.Default.YaziciNormalZarf;
            else if (seciliSayfa == "Çoklu Zarf Yazdırma") kayitliYazici = Properties.Settings.Default.YaziciCokluZarf;
            else if (seciliSayfa == "Manuel Etiket") kayitliYazici = Properties.Settings.Default.YaziciManuelEtiket;

            if (!string.IsNullOrEmpty(kayitliYazici) && cmbPrinters.Items.Contains(kayitliYazici))
                cmbPrinters.SelectedItem = kayitliYazici;
            else
                cmbPrinters.SelectedIndex = -1;
        }

        // KAYDET Butonu
        private void btnSavePrinterMapping_Click(object sender, EventArgs e)
        {
            if (cmbPrintingPages.SelectedIndex == -1 || cmbPrinters.SelectedIndex == -1)
            {
                MessageBox.Show("Lütfen atama yapmak için önce bir Sayfa ve bir Yazıcı seçin!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string seciliSayfa = cmbPrintingPages.Text;
            string seciliYazici = cmbPrinters.Text;

            if (seciliSayfa == "Normal Zarf Yazdırma") Properties.Settings.Default.YaziciNormalZarf = seciliYazici;
            else if (seciliSayfa == "Çoklu Zarf Yazdırma") Properties.Settings.Default.YaziciCokluZarf = seciliYazici;
            else if (seciliSayfa == "Manuel Etiket") Properties.Settings.Default.YaziciManuelEtiket = seciliYazici;

            Properties.Settings.Default.Save();
            MessageBox.Show($"{seciliSayfa} ekranı için varsayılan yazıcı başarıyla\n[{seciliYazici}]\nolarak ayarlandı!", "Atama Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        #endregion

        // =========================================================================================

        #region ✉️ 16. ÇOKLU ZARF YAZDIRMA (MANUEL GİRİŞ VE İŞLEMLER)

        // Çoklu Zarf sayfasındaki "Manuel Ekle" butonunun tıklanma olayı
        private void btnManuelAdresEkle_Click(object sender, EventArgs e)
        {
            // 1. Şık ve Dinamik Bir Popup Form Oluşturuyoruz
            Form frmManuel = new Form
            {
                Width = 400,
                Height = 350,
                Text = "Manuel Adres Girişi (Tek Seferlik)",
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false
            };

            // 2. Kutuları ve Etiketleri Hazırlıyoruz
            Label lblFirma = new Label { Text = "Firma Adı:", Left = 20, Top = 20, Width = 100, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            TextBox txtFirma = new TextBox { Left = 120, Top = 20, Width = 240 };

            Label lblAdres = new Label { Text = "Adres:", Left = 20, Top = 60, Width = 100, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            TextBox txtAdres = new TextBox { Left = 120, Top = 60, Width = 240, Height = 60, Multiline = true };

            Label lblIl = new Label { Text = "İl / İlçe:", Left = 20, Top = 140, Width = 100, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            TextBox txtIl = new TextBox { Left = 120, Top = 140, Width = 240 };

            Label lblTel1 = new Label { Text = "Telefon 1:", Left = 20, Top = 180, Width = 100, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            TextBox txtTel1 = new TextBox { Left = 120, Top = 180, Width = 240 };

            Label lblTel2 = new Label { Text = "Telefon 2:", Left = 20, Top = 220, Width = 100, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            TextBox txtTel2 = new TextBox { Left = 120, Top = 220, Width = 240 };

            Button btnEkle = new Button
            {
                Text = "YAZDIRMA LİSTESİNE EKLE",
                Left = 120,
                Top = 260,
                Width = 240,
                Height = 40,
                BackColor = Color.Teal,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };

            // 3. Hepsini Forma Monte Ediyoruz
            frmManuel.Controls.Add(lblFirma); frmManuel.Controls.Add(txtFirma);
            frmManuel.Controls.Add(lblAdres); frmManuel.Controls.Add(txtAdres);
            frmManuel.Controls.Add(lblIl); frmManuel.Controls.Add(txtIl);
            frmManuel.Controls.Add(lblTel1); frmManuel.Controls.Add(txtTel1);
            frmManuel.Controls.Add(lblTel2); frmManuel.Controls.Add(txtTel2);
            frmManuel.Controls.Add(btnEkle);

            // 4. Kaydet Butonuna Basıldığında Ne Olacak?
            btnEkle.Click += (s, args) =>
            {
                if (string.IsNullOrWhiteSpace(txtFirma.Text))
                {
                    MessageBox.Show("Firma Adı zorunludur!", "Eksik Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // 🚀 HEDEF TABLOYA VERİYİ ATIYORUZ
                dgvAmbarSecilenFirmalar.Rows.Add(
                    "MANUEL",         // 1. Sütun: ID yerine Manuel yazsın
                    txtFirma.Text,    // 2. Sütun: Firma Adı
                    txtAdres.Text,    // 3. Sütun: Adres
                    txtIl.Text,       // 4. Sütun: İl
                    txtTel1.Text,     // 5. Sütun: Telefon 1
                    txtTel2.Text      // 6. Sütun: Telefon 2
                );

                MessageBox.Show("Manuel adres başarıyla eklendi!", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                frmManuel.DialogResult = DialogResult.OK;
                frmManuel.Close();
            };

            // 5. Formu Ekrana Çıkartıyoruz
            frmManuel.ShowDialog();
        }

        #endregion

        // =========================================================================================

        #region 💾 17. ÇOKLU ZARF (GELİŞMİŞ HAFIZA VE SÜTUN KORUMA SİSTEMİ)

        // Çoklu Zarf: Gelişmiş Hafıza Merkezi Butonu (Popup Arşiv Formu)
        private void btnZarfHafiza_Click(object sender, EventArgs e)
        {
            string klasorYolu = System.IO.Path.Combine(Application.StartupPath, "ZarfHafizalari");
            if (!System.IO.Directory.Exists(klasorYolu)) System.IO.Directory.CreateDirectory(klasorYolu);

            // 1. Arşiv Formunu Oluştur
            Form frmHafiza = new Form
            {
                Width = 500,
                Height = 400,
                Text = "Zarf Hafıza ve Sevkiyat Arşivi",
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false
            };

            // 2. Kontrolleri Hazırla
            Label lblListe = new Label { Text = "Kayıtlı Sevkiyatlar / Ölçüler:", Left = 20, Top = 15, Width = 220, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            ListBox lstKayitlar = new ListBox { Left = 20, Top = 40, Width = 220, Height = 260, Font = new Font("Segoe UI", 9) };

            Action KayitlariDoldur = () =>
            {
                lstKayitlar.Items.Clear();
                string[] dosyalar = System.IO.Directory.GetFiles(klasorYolu, "*.txt");
                foreach (string dosya in dosyalar) lstKayitlar.Items.Add(System.IO.Path.GetFileNameWithoutExtension(dosya));
            };
            KayitlariDoldur();

            Label lblYeni = new Label { Text = "Yeni Kayıt Adı (Firma/Ölçü vb.):", Left = 260, Top = 15, Width = 200, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            TextBox txtYeniKayitAdi = new TextBox { Left = 260, Top = 40, Width = 200, Font = new Font("Segoe UI", 10) };

            Button btnAskijaAl = new Button { Text = "Mevcut Ekranı Kaydet", Left = 260, Top = 75, Width = 200, Height = 40, BackColor = Color.Orange, Font = new Font("Segoe UI", 9, FontStyle.Bold), Cursor = Cursors.Hand };
            Button btnDevamEt = new Button { Text = "Seçili Kaydı Ekrana Yükle", Left = 260, Top = 160, Width = 200, Height = 45, BackColor = Color.Teal, ForeColor = Color.White, Font = new Font("Segoe UI", 9, FontStyle.Bold), Cursor = Cursors.Hand };
            Button btnTemizle = new Button { Text = "Seçili Kaydı Sil", Left = 260, Top = 215, Width = 200, Height = 35, BackColor = Color.DarkRed, ForeColor = Color.White, Font = new Font("Segoe UI", 9, FontStyle.Bold), Cursor = Cursors.Hand };
            Button btnKapat = new Button { Text = "Kapat", Left = 260, Top = 310, Width = 200, Height = 35, Cursor = Cursors.Hand };

            frmHafiza.Controls.Add(lblListe); frmHafiza.Controls.Add(lstKayitlar);
            frmHafiza.Controls.Add(lblYeni); frmHafiza.Controls.Add(txtYeniKayitAdi);
            frmHafiza.Controls.Add(btnAskijaAl); frmHafiza.Controls.Add(btnDevamEt);
            frmHafiza.Controls.Add(btnTemizle); frmHafiza.Controls.Add(btnKapat);

            // 3. AKSİYON: YENİ KAYIT (SÜTUN BAŞLIKLARIYLA BERABER KAYDEDER)
            btnAskijaAl.Click += (s, args) =>
            {
                string kayitAdi = txtYeniKayitAdi.Text.Trim();
                if (string.IsNullOrEmpty(kayitAdi))
                {
                    MessageBox.Show("Lütfen kayda bir isim verin!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                foreach (char c in System.IO.Path.GetInvalidFileNameChars()) { kayitAdi = kayitAdi.Replace(c, '_'); }
                string yeniDosyaYolu = System.IO.Path.Combine(klasorYolu, kayitAdi + ".txt");

                try
                {
                    List<string> askidakiVeriler = new List<string>();

                    // Ortak Kayıt Metodu
                    void TabloyuKaydet(DataGridView dgv, string etiket)
                    {
                        if (dgv.Columns.Count > 0)
                        {
                            // 🌟 YENİ: Sütun başlıklarını kaydet
                            List<string> basliklar = new List<string>();
                            foreach (DataGridViewColumn col in dgv.Columns) basliklar.Add(col.HeaderText);
                            askidakiVeriler.Add($"HEADER_{etiket}|" + string.Join("|", basliklar));

                            // Satırları kaydet
                            foreach (DataGridViewRow satir in dgv.Rows)
                            {
                                if (satir.IsNewRow) continue;
                                List<string> hucreler = new List<string>();
                                for (int i = 0; i < satir.Cells.Count; i++) hucreler.Add(satir.Cells[i].Value?.ToString() ?? "");
                                askidakiVeriler.Add($"{etiket}|" + string.Join("|", hucreler));
                            }
                        }
                    }

                    TabloyuKaydet(dgvAmbarSecilenFirmalar, "SECILEN");
                    TabloyuKaydet(dgvPaletler, "PALET");
                    TabloyuKaydet(dgvAmbarSonListe, "SONLISTE");

                    if (askidakiVeriler.Count == 0) return;

                    System.IO.File.WriteAllLines(yeniDosyaYolu, askidakiVeriler);

                    dgvAmbarSecilenFirmalar.Rows.Clear(); dgvPaletler.Rows.Clear(); dgvAmbarSonListe.Rows.Clear();
                    MessageBox.Show($"'{kayitAdi}' başarıyla kaydedildi!", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    txtYeniKayitAdi.Clear(); KayitlariDoldur();
                }
                catch (Exception ex) { MessageBox.Show("Hata:\n" + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            };

            // 4. AKSİYON: GERİ YÜKLE (ORİJİNAL BAŞLIKLARI VE FORMATI KORUYARAK)
            btnDevamEt.Click += (s, args) =>
            {
                if (lstKayitlar.SelectedIndex == -1) return;
                string seciliDosya = System.IO.Path.Combine(klasorYolu, lstKayitlar.SelectedItem.ToString() + ".txt");

                try
                {
                    string[] askidakiVeriler = System.IO.File.ReadAllLines(seciliDosya);

                    // SADECE satırları temizle! (Columns.Clear YAPMIYORUZ ki gizli ID sütunları bozulmasın)
                    dgvAmbarSecilenFirmalar.Rows.Clear();
                    dgvPaletler.Rows.Clear();
                    dgvAmbarSonListe.Rows.Clear();

                    foreach (string satir in askidakiVeriler)
                    {
                        string[] parcalar = satir.Split('|');
                        string tabloAdi = parcalar[0];
                        string[] eklenecekVeri = new string[parcalar.Length - 1];
                        Array.Copy(parcalar, 1, eklenecekVeri, 0, parcalar.Length - 1);

                        // HEADER (SÜTUN BAŞLIKLARI) SATIRI GELDİYSE
                        if (tabloAdi.StartsWith("HEADER_"))
                        {
                            DataGridView hedef = tabloAdi == "HEADER_SECILEN" ? dgvAmbarSecilenFirmalar :
                                                 tabloAdi == "HEADER_PALET" ? dgvPaletler : dgvAmbarSonListe;

                            // SADECE tablonun gerçekten hiç sütunu yoksa (Örn: Program ilk açıldığında bir bug olduysa) yeni sütun çiz.
                            // Aksi halde var olan formatı (Gizli ID vs.) bozmamak için bu adımı pas geç!
                            if (hedef.ColumnCount == 0)
                            {
                                for (int i = 0; i < eklenecekVeri.Length; i++) hedef.Columns.Add($"col{i}", eklenecekVeri[i]);
                            }
                        }
                        // NORMAL VERİ SATIRI GELDİYSE
                        else if (tabloAdi == "SECILEN" || tabloAdi == "PALET" || tabloAdi == "SONLISTE")
                        {
                            DataGridView hedef = tabloAdi == "SECILEN" ? dgvAmbarSecilenFirmalar :
                                                 tabloAdi == "PALET" ? dgvPaletler : dgvAmbarSonListe;

                            if (hedef.ColumnCount > 0) hedef.Rows.Add(eklenecekVeri);
                        }
                    }

                    System.IO.File.Delete(seciliDosya);
                    MessageBox.Show("Seçilen arşiv başarıyla yüklendi!", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    frmHafiza.Close();
                }
                catch (Exception ex) { MessageBox.Show("Hata:\n" + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            };

            // 5. AKSİYON: SİL
            btnTemizle.Click += (s, args) =>
            {
                if (lstKayitlar.SelectedIndex == -1) return;
                string silinecekIsim = lstKayitlar.SelectedItem.ToString();
                if (MessageBox.Show($"'{silinecekIsim}' silinecek. Emin misiniz?", "Uyarı", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    System.IO.File.Delete(System.IO.Path.Combine(klasorYolu, silinecekIsim + ".txt"));
                    KayitlariDoldur();
                }
            };

            btnKapat.Click += (s, args) => { frmHafiza.Close(); };
            frmHafiza.ShowDialog();
        }




        #endregion


    }
}   