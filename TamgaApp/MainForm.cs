using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using ExcelDataReader;
using System.IO.Ports;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using static TamgaApp.DataAccess;

namespace TamgaApp
{
    public partial class MainForm : Form
    {
        #region 🌐 00.TamgaApp

        #region 🌐 01. ÇEKİRDEK SİSTEM VE GLOBAL DEĞİŞKENLER (CORE SYSTEM VARIABLES)

        #region 🧪 01.1 GELİŞTİRİCİ MODU VE MEDYA MOTORU (TEST & MEDIA ENGINE)
        // ------------------------------------------------------------------------
        // [EV MODU ŞALTERİ]
        // NEDEN VAR? Canlı SQL sunucusuna erişimin olmadığı fiziksel ortamlarda (ev, kafe)
        // programın çökmesini engellemek ve arayüz (UI) testleri yapabilmek için sistemi
        // sahte (mock) veri tablolarıyla besleyen ana şalterdir. 
        // DİKKAT: Canlı (Production) ortama derlerken KESİNLİKLE 'false' olmalıdır!
        // ------------------------------------------------------------------------
        public static bool EvModuAktif = false;

        // ------------------------------------------------------------------------
        // [MEDYA MOTORU DEĞİŞKENLERİ]
        // NEDEN RAM'DE BEKLİYOR? Barkod okuyucu saniyede birden fazla tetikleme yapabilir.
        // Her okumada diske (Harddisk/SSD) gidip .wav dosyasını bulmak I/O darboğazı yaratır ve 
        // arayüzü dondurur. Bu yüzden sesleri uygulama açılışında Asenkron (LoadAsync) olarak 
        // RAM'e yüklüyoruz. Böylece sıfır gecikmeyle (lag-free) anında tepki veriyor.
        // ------------------------------------------------------------------------
        private System.Media.SoundPlayer basariliSesMotoru;
        private System.Media.SoundPlayer hataSesMotoru;

        private void SesMotorlariniHazirla()
        {
            try
            {
                string bYol = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "basarili.wav");
                if (System.IO.File.Exists(bYol))
                {
                    basariliSesMotoru = new System.Media.SoundPlayer(bYol);
                    basariliSesMotoru.LoadAsync();
                }

                string hYol = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "hata.wav");
                if (System.IO.File.Exists(hYol))
                {
                    hataSesMotoru = new System.Media.SoundPlayer(hYol);
                    hataSesMotoru.LoadAsync();
                }
            }
            catch { } // Ses dosyası silinmişse programın çökmemesi için sessizce yutulur
        }

        private void BasariliSesCal()
        {
            if (basariliSesMotoru != null) basariliSesMotoru.Play();
            else System.Media.SystemSounds.Asterisk.Play(); // Dosya yoksa Windows varsayılan sesine düş (Fallback)
        }

        private void HataSesCal()
        {
            if (hataSesMotoru != null) hataSesMotoru.Play();
            else System.Media.SystemSounds.Hand.Play(); // Fallback
        }
        #endregion

        #region 🔐 01.2 OTURUM VE YETKİLENDİRME (SESSION & SECURITY)
        // ------------------------------------------------------------------------
        // [OTURUM (SESSION) BELLEĞİ]
        // NEDEN STATİC? Kullanıcı giriş yaptıktan sonra bu veriler programın her köşesinden 
        // (farklı formlardan veya class'lardan) tek bir referansla kontrol edilebilsin diye 
        // static olarak RAM'e kazınır. Role-Based Access Control (RBAC) bu iki değişkene bakar.
        // ------------------------------------------------------------------------
        public static string AktifKullaniciAdi = "";
        public static string AktifYetkiler = "";
        #endregion

        #region 📐 01.3 GÖRSEL TASARIM MOTORU (DRAG & DROP ENGINE)
        // ------------------------------------------------------------------------
        // TASARIM LİSTELERİ: Kağıt (Zarf/Etiket) üzerindeki nesnelerin ve arka plan 
        // firma havuzunun geçici (RAM) olarak tutulduğu List yapıları.
        // ------------------------------------------------------------------------
        private List<DesignItem> designItems = new List<DesignItem>();     // O an aktif tasarım kağıdındaki tüm nesneler
        private List<Control> selectedControls = new List<Control>();      // Çoklu seçimde (CTRL+Tık) toplu işlem (hizalama/renk) görecek nesneler
        private List<Firma> tumFirmalarCache = new List<Firma>();          // SQL'i yormamak için firma listesinin önbelleğe alınmış hali

        // ------------------------------------------------------------------------
        // DEVİNİMSEL EKSEN (STATE MACHINE) DEĞİŞKENLERİ
        // NEDEN VAR? Fare hareketlerinin anlık olarak "Taşıma (Drag)" mı yoksa "Sündürme/
        // Büyütme (Resize)" mi olduğunu algılayıp, ona göre Windows API'sini tetikleyen bayraklar.
        // ------------------------------------------------------------------------
        private bool isDragging = false;                                   // Taşıma durumu aktif mi?
        private bool isResizing = false;                                   // Boyutlandırma aktif mi?
        private string resizeDir = "";                                     // Hangi yöne sündürülüyor? (Örn: WE yatay, NS dikey, NWSE çapraz)
        private Point dragStart;                                           // Farenin ilk tıklandığı milimetrik X,Y koordinatı (Fark hesaplamak için)
        private Control draggingControl;                                   // O an farenin ucuna kilitlenmiş aktif kutucuk
        private DesignItem selectedDesignItem;                             // Özellikler (Properties) paneline bağlanmış aktif nesne modeli

        // ------------------------------------------------------------------------
        // TASARIM MASASI (UI KONTROLLERİ)
        // ------------------------------------------------------------------------
        private Panel pnlWorkspace;                                        // Dış çerçeve: Dev çalışma masası (Taşmaları ve kaybolmaları önler)
        private ComboBox cmbPaperSize;                                     // Global kağıt/zarf ölçüsü yönetici kutusu
        #endregion

        #region 🖨️ 01.4 YAZDIRMA VE SPOOLER YÖNETİMİ (PRINT SPOOLER)
        // ------------------------------------------------------------------------
        // ÇOKLU (BATCH) YAZDIRMA MOTORU
        // NEDEN LİSTE? Kullanıcı 50 firmayı seçip "Yazdır" dediğinde, Windows Spooler'a
        // tek tek 50 komut göndermek yazıcıyı dondurur. Bunun yerine liste olarak alıp
        // tek bir döküman (Document) içinde sayfalandırma yaparak RAM'den kazanç sağlıyoruz.
        // ------------------------------------------------------------------------
        private List<Firma> batchFirms;
        private int batchIndex;                                            // İşlenen sayfanın o anki sırasını tutar (HasMorePages mantığı için)

        // ------------------------------------------------------------------------
        // YAZICI EŞLEŞTİRME (PRINTER MAPPING) DEPOSU
        // NEDEN SÖZLÜK (DICTIONARY)? "Etiket" için Zebra'yı, "A4 Rapor" için HP'yi hatırlaması
        // ve uygulamanın her açılışında json dosyasından okuyup donanıma ataması için anahtar-değer yapısı.
        // ------------------------------------------------------------------------
        private Dictionary<string, string> printerMappings = new Dictionary<string, string>();
        private const string PrinterSettingsFile = "printer_settings.json";
        private PrintDocument pdUretim;                                    // Depo Kabul A4 baskı motoru
        #endregion

        #region 📦 01.5 SEVKİYAT VE KALICI HAFIZA (GHOST MODE CACHE)
        // ------------------------------------------------------------------------
        // SİPARİŞ HAVUZU
        // ------------------------------------------------------------------------
        private DataTable dtTumSiparisler = new DataTable();               // ERP/SQL'den gelen verilerin UI bloklanmasın diye alındığı RAM havuzu

        // ------------------------------------------------------------------------
        // 👻 GHOST MODU KARA LİSTESİ (TAMAMLANMIŞ İŞLER)
        // NEDEN GHOST MOD? ERP sisteminde belge kapanana kadar geçen sürede (asenkron gecikme),
        // personelin aynı belgeyi tekrar sevketmesini engellemek için, lokal .txt tabanlı
        // bir kara liste (Blacklist) tutulur. Bu listeye giren belge SQL'de açık görünse bile
        // ekrandan gizlenir (Hayalet olur).
        // ------------------------------------------------------------------------
        public static List<string> TamamlananBelgeNolar = new List<string>();
        #endregion

        #region 🧩 01.6 VERİ MODELLERİ VE SERİLEŞTİRME (DATA MODELS & SERIALIZATION)
        // ------------------------------------------------------------------------
        // NEDEN [Serializable]? 
        // Bu sınıflar sadece bellekte yaşamak için değil, diske kalıcı olarak kaydedilip 
        // (.json veya binary olarak) daha sonra uygulama kapatılıp açılsa bile eksiksiz 
        // geri dönüştürülmek (Deserialize) için tasarlandığından bu etiketle işaretlenmelidir.
        // ------------------------------------------------------------------------

        public class YarimSevkiyatHafizasi
        {
            public string MusteriAdi { get; set; }
            public string BelgeNo { get; set; }
            public string SevkMusteri { get; set; }
            public int PaletSayisi { get; set; }
            public DateTime KayitTarihi { get; set; }
            public Dictionary<string, int> AnaOkutulanlar { get; set; } = new Dictionary<string, int>();
            public Dictionary<int, Dictionary<int, string>> PaletMatrisiDurumu { get; set; } = new Dictionary<int, Dictionary<int, string>>();
            public Dictionary<string, string> PaletBarkodlari { get; set; } = new Dictionary<string, string>();
        }

        [Serializable]
        public class DesignItem
        {
            public string Id { get; set; } = Guid.NewGuid().ToString();    // GUID: Silme ve seçme işlemlerinde benzersiz kilit (ID) görevi görür
            public string Type { get; set; }                               // Label, Field, Frame, Image tespiti
            public string Text { get; set; }                               // Sabit metin (Resim ise Harddisk Path'i)
            public string PlaceholderKey { get; set; }                     // Dinamik Alan Değişkeni (Örn: {FirmaAdi})

            // NEDEN PİKSEL (PX) DEĞİL DE MİLİMETRE (MM)? 
            // Farklı ekran çözünürlükleri (DPI) ve yazıcı pikselleri (PPI) arasında kayma (shift) 
            // olmaması için matematiksel çekirdek veriyi sadece milimetre üzerinden saklıyoruz.
            public float Xmm { get; set; }
            public float Ymm { get; set; }
            public float Wmm { get; set; }
            public float Hmm { get; set; }

            public string FontName { get; set; } = "Times New Roman";
            public float FontSizePt { get; set; } = 12f;
            public FontStyle FontStyle { get; set; } = FontStyle.Regular;
            public string ColorName { get; set; } = "#000000";             // Çapraz platform uyumluluğu için Color nesnesi yerine HTML Hex Kodu
            public string Alignment { get; set; } = "Center";
            public int Rotation { get; set; } = 0;
        }

        [Serializable]
        private class TemplateFile
        {
            public string TemplateName { get; set; }
            public float PageWidthMm { get; set; }
            public float PageHeightMm { get; set; }
            public string Orientation { get; set; }
            public int Version { get; set; }                  // Gelecekte eklenecek yeni özelliklerde eski json'ların çökmesini önleyen versiyon kontrol bariyeri
            public DateTime CreatedAt { get; set; }
            public List<DesignItem> DesignItems { get; set; }
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

            SesMotorlariniHazirla(); // Sesleri RAM'e yükle

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
            OtomatikPortBaglantisiBaslat();
            TabletModunuAktifEt();
            TablolariJiletGibiYap();
            YeniNesilSevkiyatSisteminiKur();
            KlasorAyarlariniEkranaGetir();
            YedeklemeMotorunuBaslat();

            // 🌟 TASARIM MOTORLARINI ÇALIŞTIR
            ElitTasarimiUygula();     // (Butonlar, saat ve fontları düzeltir)
            SekmeleriModernlestir();  // (Sekmeleri jilet gibi yapar)

            YardimSekmesiniKur();     // 👈 İŞTE SADECE BU SATIRI EKLİYORSUN
            StokSisteminiKur();
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

            // 🌟 --- ANA PANEL (ÜRETİM) YAZICI DOLDURMA --- 🌟
            if (cmbUretimYazici != null)
            {
                cmbUretimYazici.Items.Clear();
                foreach (string printer in System.Drawing.Printing.PrinterSettings.InstalledPrinters)
                {
                    cmbUretimYazici.Items.Add(printer);
                }

                // Varsayılan yazıcıyı seçili getir
                System.Drawing.Printing.PrintDocument pd = new System.Drawing.Printing.PrintDocument();
                string defaultPrinter = pd.PrinterSettings.PrinterName;
                if (cmbUretimYazici.Items.Contains(defaultPrinter)) cmbUretimYazici.SelectedItem = defaultPrinter;
                else if (cmbUretimYazici.Items.Count > 0) cmbUretimYazici.SelectedIndex = 0;
            }

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

            // 🌟 YENİ EKLENEN: Lavabo Renkleri (Sistem Otomatik Dolduracak)
            if (!dgvUretim.Columns.Contains("LavaboRenkleri"))
            {
                dgvUretim.Columns.Add("LavaboRenkleri", "Lavabo Renkleri");
                dgvUretim.Columns["LavaboRenkleri"].ReadOnly = true; // Personel elle değiştiremesin diye kilitledik!
            }

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

            // 🌟 KİLİT ZIRHI: Askıda işlem varken başka sekmeye geçişi iptal et
            tabControl1.Selecting += (s, tabEvent) =>
            {
                if (AskidanIslemKilitAktif && tabEvent.TabPage.Text != "Sevkiyat Plan")
                {
                    HataSesCal();
                    MessageBox.Show("Güvenlik Zırhı Aktif!\n\nAskıdan çekilen işleme devam ediyorsunuz. Bu işlem sonlanmadan başka bir sekmeye geçemezsiniz.", "Karantina İhlali", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    tabEvent.Cancel = true; // e yerine tabEvent kullandık
                }
            };

        }

        // 🌟 SAĞ ÜSTTEKİ ÇARPI (X) TUŞUYLA KAPATILIRSA DEVREYE GİREN ZIRH
        protected override void OnFormClosing(FormClosingEventArgs e)
        {

            // 🌟 BUNU YENİ EKLİYORSUN: Kapatma Koruması
            if (AskidanIslemKilitAktif)
            {
                HataSesCal();
                MessageBox.Show("Sistem Kilitli!\n\nAskıdan çekilen yarım bir işlem varken program kapatılamaz. Önce işlemi bitirin veya tekrar askıya alın.", "Kritik Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                e.Cancel = true;
                return;
            }

            // Animasyon başladıysa ve Windows sistemi kapatıyorsa karışma, kapanmasına izin ver
            if (kapanisBasladi)
            {
                base.OnFormClosing(e);
                return;
            }

            // Eğer sağ üstteki çarpıya basıldıysa
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true; // 🌟 Önce Windows'un kaba kapanışını iptal et!

                DialogResult onay = MessageBox.Show(
                    "Programı kapatmak istediğinize emin misiniz?\n\nKaydedilmemiş veya askıya alınmamış tüm verileriniz kaybolabilir!",
                    "Çıkış Onayı",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2);

                if (onay == DialogResult.Yes)
                {
                    // 🌟 Onay verirse Windows'un kaba kapanışı yerine senin şık animasyonunu tetikle!
                    CikisAnimasyonuVeKapat();
                }
            }
            else
            {
                base.OnFormClosing(e);
            }
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
            if (dgvAmbarSonListe != null) { dgvAmbarSonListe.CellMouseDown -= dgvAmbarSonListe_CellMouseDown; dgvAmbarSonListe.CellMouseDown += dgvAmbarSonListe_CellMouseDown; }

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

            // 🌟 RENK MOTORU: A-Z Sıralamada renklerin kaybolmasını önler
            if (dgvMalzemeler != null)
            {
                dgvMalzemeler.CellFormatting -= dgvMalzemeler_CellFormatting;
                dgvMalzemeler.CellFormatting += dgvMalzemeler_CellFormatting;
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

            // 🌟 1. TABLOLARI KİLİTLE VE YANLIŞ OKUTMA (MIKNATIS) ZIRHINI BAĞLA
            if (dgvMalzemeler != null)
            {
                dgvMalzemeler.ReadOnly = true;
                dgvMalzemeler.AllowUserToAddRows = false;
                dgvMalzemeler.KeyPress -= Dgv_BarkodYonlendir_KeyPress;
                dgvMalzemeler.KeyPress += Dgv_BarkodYonlendir_KeyPress;
            }

            if (dgvPaletMatrisi != null)
            {
                dgvPaletMatrisi.ReadOnly = true;
                dgvPaletMatrisi.AllowUserToAddRows = false;
                dgvPaletMatrisi.KeyPress -= Dgv_BarkodYonlendir_KeyPress;
                dgvPaletMatrisi.KeyPress += Dgv_BarkodYonlendir_KeyPress;

                // 🌟 YENİ EKLENEN: ÜST TABLO (PALET) SAĞ TIK VE DÜZENLEME MOTORU 🌟
                dgvPaletMatrisi.CellMouseDown -= DgvPaletMatrisi_CellMouseDown;
                dgvPaletMatrisi.CellMouseDown += DgvPaletMatrisi_CellMouseDown;
            }

            if (btnManuelEkle != null) { btnManuelEkle.Click -= btnManuelEkle_Click; btnManuelEkle.Click += btnManuelEkle_Click; }

            if (btnPalettenSil != null) { btnPalettenSil.Click -= btnPalettenSil_Click; btnPalettenSil.Click += btnPalettenSil_Click; }
            if (btnSevkTemizle != null) { btnSevkTemizle.Click -= btnSevkTemizle_Click; btnSevkTemizle.Click += btnSevkTemizle_Click; }

            if (btnTumBelgeleriSec != null) { btnTumBelgeleriSec.Click -= btnTumBelgeleriSec_Click; btnTumBelgeleriSec.Click += btnTumBelgeleriSec_Click; }
            if (btnSevkRaporla != null) { btnSevkRaporla.Click -= btnSevkRaporla_Click; btnSevkRaporla.Click += btnSevkRaporla_Click; }

            // Yeni Eksilt Butonunu bağla
            if (this.Controls.Find("btnManuelEksilt", true).FirstOrDefault() is Button btnEksilt)
            {
                btnEksilt.Click -= btnManuelEksilt_Click;
                btnEksilt.Click += btnManuelEksilt_Click;
            }

            // Sağ Tık Menüsünü bağla
            if (dgvMalzemeler != null)
            {
                dgvMalzemeler.CellMouseDown -= dgvMalzemeler_CellMouseDown;
                dgvMalzemeler.CellMouseDown += dgvMalzemeler_CellMouseDown;
            }

            if (dgvPaletler != null) dgvPaletler.CellEndEdit += dgvPaletler_CellEndEdit;

            if (dgvYarimSevkler != null)
            {
                dgvYarimSevkler.CellMouseDown -= dgvYarimSevkler_CellMouseDown;
                dgvYarimSevkler.CellMouseDown += dgvYarimSevkler_CellMouseDown;
            }

            // Sevk Beklet Butonu Bağlantısı
            if (this.Controls.Find("btnSevkBeklet", true).FirstOrDefault() is Button btnBeklet)
            {
                btnBeklet.Click -= btnSevkBeklet_Click;
                btnBeklet.Click += btnSevkBeklet_Click;
            }

            if (btnNormalManuelYazdir != null) btnNormalManuelYazdir.Click += btnNormalManuelYazdir_Click;

        }
        #endregion

        #region 🎨 02.5 GÖRSEL TEMA (ELİT TASARIM - V2)

        // Ana sayfadaki karşılama yazıları ve çıkış butonlarının elit (şık) renk paletine geçirilmesi
        private void ElitTasarimiUygula()
        {
            try
            {
                // 1. ANA PANEL ARKA PLANI: Göz yoran çiğ beyaz yerine, modern ve elit bir "Bulut Grisi / Kirli Beyaz"
                TabPage anaPanel = tabControl1.TabPages.Cast<TabPage>().FirstOrDefault(t => t.Text == "Ana Panel");
                if (anaPanel != null)
                {
                    anaPanel.BackColor = Color.FromArgb(245, 247, 250);
                }

                // 2. HOŞ GELDİN YAZISI: İtalik ve eski tip font yerine, kalın ve modern antrasit
                lblKarsilama.Font = new Font("Segoe UI", 28, FontStyle.Bold);
                lblKarsilama.ForeColor = Color.FromArgb(33, 37, 41); // Koyu antrasit siyah

                // 3. SAAT VE TAKVİM: Dev gibi ince (Light) fontla modern dijital ekran hissi
                lblSaat.Font = new Font("Segoe UI Semilight", 48, FontStyle.Regular);
                lblSaat.ForeColor = Color.FromArgb(15, 76, 58); // Logonun elit koyu yeşili

                lblTakvim.Font = new Font("Segoe UI Semibold", 14, FontStyle.Bold);
                lblTakvim.ForeColor = Color.FromArgb(108, 117, 125); // Şık, soluk gri

                // 4. ÇIKIŞ BUTONU: O kaba parlak kırmızı yerine, modern ve pürüzsüz "Bootstrap Kırmızısı"
                Control[] butonlar = this.Controls.Find("btnCikisYap", true);
                if (butonlar.Length > 0 && butonlar[0] is Button btnCikis)
                {
                    btnCikis.FlatStyle = FlatStyle.Flat; // 3D Çerçeveyi yokedip düzleştirir
                    btnCikis.FlatAppearance.BorderSize = 0;
                    btnCikis.BackColor = Color.FromArgb(220, 53, 69); // Tok kırmızı
                    btnCikis.ForeColor = Color.White;
                    btnCikis.Font = new Font("Segoe UI Semibold", 12, FontStyle.Bold);
                    btnCikis.Cursor = Cursors.Hand;
                    btnCikis.Size = new Size(180, 60); // Butonu dikdörtgen ve şık boyuta getirir

                    // Fare üzerine gelince (Hover) rengi hafif koyulaşsın (Canlılık hissi)
                    btnCikis.MouseEnter += (s, e) => { btnCikis.BackColor = Color.FromArgb(200, 35, 51); };
                    btnCikis.MouseLeave += (s, e) => { btnCikis.BackColor = Color.FromArgb(220, 53, 69); };
                }

                // 5. GİRİŞ EKRANINA DÖN BUTONU: İçi boş, sadece çerçevesi olan "Outline" modern tasarım
                Control[] btnOturumKapat = this.Controls.Find("btnLoginDon", true);
                if (btnOturumKapat.Length > 0 && btnOturumKapat[0] is Button btnKapat)
                {
                    btnKapat.FlatStyle = FlatStyle.Flat;
                    btnKapat.FlatAppearance.BorderSize = 2; // 2 piksellik şık bir çerçeve
                    btnKapat.FlatAppearance.BorderColor = Color.FromArgb(15, 76, 58); // Çerçeve rengi logonun yeşili
                    btnKapat.BackColor = Color.FromArgb(245, 247, 250); // Arkaplanla aynı renk (İçi boş görünür)
                    btnKapat.ForeColor = Color.FromArgb(15, 76, 58); // Yazı rengi yeşil
                    btnKapat.Font = new Font("Segoe UI Semibold", 12, FontStyle.Bold);
                    btnKapat.Cursor = Cursors.Hand;
                    btnKapat.Size = new Size(180, 60);

                    // Fare üzerine gelince içi yeşil dolsun, yazısı beyaz olsun (Harika efekt!)
                    btnKapat.MouseEnter += (s, e) =>
                    {
                        btnKapat.BackColor = Color.FromArgb(15, 76, 58);
                        btnKapat.ForeColor = Color.White;
                    };
                    btnKapat.MouseLeave += (s, e) =>
                    {
                        btnKapat.BackColor = Color.FromArgb(245, 247, 250);
                        btnKapat.ForeColor = Color.FromArgb(15, 76, 58);
                    };
                }
            }
            catch { }
        }

        public static bool AskidanIslemKilitAktif = false;

        // 🌟 KARANTİNA MOTORU 🌟
        private void KarantinayaAl(bool kilitlensinMi)
        {
            AskidanIslemKilitAktif = kilitlensinMi;

            // SADECE CANLI KALMASINI İSTEDİĞİMİZ "DOKUNULMAZ" NESNELER
            List<string> dokunulmazlar = new List<string>
    {
        "txtBarkod", "dgvPaletMatrisi", "dgvMalzemeler", "dgvYarimSevkler", "dgvPaletler",
        "btnSevkAskayaAl", "btnSevkBeklet", "btnTamSevk", "btnKismiSevk", "btnAnlikPaletEtiketi",
        "btnSevkRaporla", "btnSiparisYenile", "btnSevkAra", "btnTumBelgeleriSec", "cmbSevkPaletSayisi", "cmbAktifPalet",
        "btnOncekiPalet", "btnSonrakiPalet", "btnBarkodKilidi", "btnManuelEksilt", "btnManuelEkle", "numManuelAdet",
        "btnPalettenSil", "btnSevkTemizle", "clbBelgeNo", "btnAmbarKaydet", "btnAmbarGetir", "btnAmbarGoruntule"// 🌟 RAPORLA BUTONUNU DA KORUMAYA ALDIK
    };

            if (tabPage13 != null)
            {
                NesneleriDondur(tabPage13, kilitlensinMi, dokunulmazlar);
            }
        }

        private void NesneleriDondur(Control anaKutu, bool kilitlensinMi, List<string> dokunulmazlar)
        {
            foreach (Control ctrl in anaKutu.Controls)
            {
                if (ctrl is Button || ctrl is TextBox || ctrl is ComboBox || ctrl is NumericUpDown || ctrl is CheckedListBox)
                {
                    if (!dokunulmazlar.Contains(ctrl.Name))
                    {
                        ctrl.Enabled = !kilitlensinMi;
                    }
                }

                if (ctrl.Controls.Count > 0)
                {
                    NesneleriDondur(ctrl, kilitlensinMi, dokunulmazlar);
                }
            }
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

        #region 🎨 02.6 SEKME (TABCONTROL) MODERNİZASYONU

        public void SekmeleriModernlestir()
        {
            if (tabControl1 == null) return;

            // WinForms'un varsayılan kaba çizimini iptal edip, fırçayı biz alıyoruz
            tabControl1.DrawMode = TabDrawMode.OwnerDrawFixed;

            // Sekmelerin yüksekliğini ve genişliğini arttırarak daha ferah ve tıklanabilir yapıyoruz
            tabControl1.ItemSize = new Size(120, 40);
            tabControl1.SizeMode = TabSizeMode.Fixed;

            // Çizim olayına kendi metodumuzu bağlıyoruz
            tabControl1.DrawItem -= TabControl1_DrawItem; // Çift eklemeyi önlemek için önce çıkar
            tabControl1.DrawItem += TabControl1_DrawItem;
        }

        private void TabControl1_DrawItem(object sender, DrawItemEventArgs e)
        {
            TabControl tabControl = sender as TabControl;
            TabPage tabPage = tabControl.TabPages[e.Index];
            Rectangle tabBounds = tabControl.GetTabRect(e.Index);

            // Çizim kalitesini pürüzsüz yap
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;

            // --- RENK PALETİ ---
            // Seçiliyse logonun koyu yeşili, seçili değilse şık bir açık gri
            Color arkaPlanRenk = isSelected ? Color.FromArgb(15, 76, 58) : Color.FromArgb(235, 238, 240);
            Color yaziRenk = isSelected ? Color.White : Color.FromArgb(100, 100, 100);

            // 1. Sekme Arka Planını Boya
            using (SolidBrush bgBrush = new SolidBrush(arkaPlanRenk))
            {
                e.Graphics.FillRectangle(bgBrush, tabBounds);
            }

            // 2. Eğer seçiliyse alt kısma hafif bir gölge/çizgi efekti ver (Antrasit)
            if (isSelected)
            {
                Rectangle altCizgi = new Rectangle(tabBounds.X, tabBounds.Bottom - 3, tabBounds.Width, 3);
                using (SolidBrush cizgiBrush = new SolidBrush(Color.FromArgb(33, 37, 41)))
                {
                    e.Graphics.FillRectangle(cizgiBrush, altCizgi);
                }
            }
            // Seçili değilse sekme aralarına ince beyaz bir ayırıcı çizgi koy
            else
            {
                using (Pen ayiriciPen = new Pen(Color.White, 2))
                {
                    e.Graphics.DrawLine(ayiriciPen, tabBounds.Right - 1, tabBounds.Top + 5, tabBounds.Right - 1, tabBounds.Bottom - 5);
                }
            }

            // 🌟 3. RAM SIZINTISINI ÖNLEYEN ZIRHLI KISIM (Yazı Çizimi)
            // using blokları sayesinde işlem bittiği milisaniye RAM'den tertemiz silinir!
            using (Font tabFont = new Font("Segoe UI", 10, isSelected ? FontStyle.Bold : FontStyle.Regular))
            using (StringFormat sFormat = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            using (SolidBrush textBrush = new SolidBrush(yaziRenk))
            {
                // Yazının kutuya tam oturması için ufak bir boşluk ayarı
                Rectangle yaziAlani = new Rectangle(tabBounds.X, tabBounds.Y, tabBounds.Width, tabBounds.Height);
                e.Graphics.DrawString(tabPage.Text, tabFont, textBrush, yaziAlani, sFormat);
            }
        }

        #endregion

        #region 🎨 02.7 EKRAN ESNEKLİĞİ (RESPONSIVE TASARIM VE KORUMA)

        private void EkranEsnekliginiAyarla()
        {
            try
            {
                // 1. FORMUN MİNİMUM BOYUTU: Kullanıcının pencereyi bozacak kadar küçültmesini kesin olarak engeller
                this.MinimumSize = new Size(1200, 750);

                // 2. SEKME DARALTMA: Sağ üstteki o çirkin (◄ ►) kaydırma oklarının çıkmaması için sekmeleri optimize eder
                if (tabControl1 != null)
                {
                    tabControl1.ItemSize = new Size(100, 40); // 120'den 100'e düşürdük, ekrana tam sığacak
                    tabControl1.SizeMode = TabSizeMode.Fixed;
                }

                // 3. AKILLI HİZALAMA (ANCHOR): Formdaki tüm sekmeleri gezip nesneleri otomatik hizalar
                if (tabControl1 != null)
                {
                    foreach (TabPage sayfa in tabControl1.TabPages)
                    {
                        foreach (Control kontrol in sayfa.Controls)
                        {
                            // Eğer nesne bir Tabloysa (DataGridView), ekran büyüdükçe dört yöne de esnesin (Ortayı kaplasın)
                            if (kontrol is DataGridView dgv)
                            {
                                dgv.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
                            }
                            // Eğer nesne bir Rapor ekranıysa (ListBox veya RichTextBox), sadece aşağı doğru esnesin
                            else if (kontrol is ListBox || kontrol is RichTextBox)
                            {
                                kontrol.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
                            }
                            // Geri kalan her şey (Buton, Yazı, TextBox), Sol-Üst köşeye çivilensin (Tablonun altına ezilmesin)
                            else
                            {
                                kontrol.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                            }
                        }
                    }
                }
            }
            catch { }
        }

        #endregion

        #region 🎨 02.8 TABLO (DATAGRIDVIEW) JİLET MOTORU

        private void TablolariJiletGibiYap()
        {
            try
            {
                // Formdaki tüm sekmeleri ve içindeki tabloları tara
                foreach (TabPage sekme in tabControl1.TabPages)
                {
                    foreach (Control ctrl in sekme.Controls)
                    {
                        if (ctrl is DataGridView dgv)
                        {
                            // 🌟 SİHİRLİ DOKUNUŞ: Tabloların kasmasını ve titremesini %100 engelleyen hız aşırtma!
                            typeof(DataGridView).InvokeMember("DoubleBuffered",
                                System.Reflection.BindingFlags.NonPublic |
                                System.Reflection.BindingFlags.Instance |
                                System.Reflection.BindingFlags.SetProperty,
                                null, dgv, new object[] { true });

                            // 1. Sol taraftaki çirkin boşluğu (Ok işaretini) gizle
                            dgv.RowHeadersVisible = false;

                            // 2. Çizgili Defter Efekti (Satırlar bir beyaz, bir açık gri olsun ki göz yormasın)
                            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(245, 247, 250);
                            dgv.BackgroundColor = Color.White;
                            dgv.BorderStyle = BorderStyle.None;
                            dgv.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal; // Sadece yatay ince çizgiler

                            // 3. Başlık Tasarımı (Koyu Yeşil Arka Plan, Beyaz Yazı)
                            dgv.EnableHeadersVisualStyles = false; // Windows'un kaba stilini ez
                            dgv.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
                            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(15, 76, 58);
                            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
                            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
                            dgv.ColumnHeadersHeight = 40; // Başlıkları ferahlat

                            // 4. Seçim Tasarımı (Satır seçilince Lacivert/Altın Sarısı tarzı elit bir renk olsun)
                            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(41, 128, 185);
                            dgv.DefaultCellStyle.SelectionForeColor = Color.White;

                            // Yazı fontu
                            dgv.DefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Regular);
                            dgv.RowTemplate.Height = 35; // Satır yüksekliklerini ferahlat
                        }
                    }
                }
            }
            catch { }
        }

        #endregion

        #region 📱 02.9 KESİN ÇÖZÜM: TÜM SEKMELERE UYGULANAN TABLET ZIRHI VE KAYDIRMA MOTORU

        public void TabletModunuAktifEt()
        {
            try
            {
                // 1. Ekranı tam kapla ve minimum form boyutunu belirle
                this.WindowState = FormWindowState.Maximized;
                this.MinimumSize = new Size(1024, 720);

                // 🌟 2. ANA PANEL (HOŞGELDİN EKRANI) ÇARPIŞMA ZIRHI 🌟
                // Resimdeki facianın sebebi: Çıkış butonları "Sağa Yapışık" ayarlı olduğu için saati eziyordu.
                if (btnCikisYap != null && btnLoginDon != null)
                {
                    // Tablet ekranında (genişlik 1200'den küçükse) butonları sağda bırakıp görünmez yapmak yerine, 
                    // saatin hemen altına şık bir sıraya diziyoruz!
                    if (Screen.PrimaryScreen.WorkingArea.Width < 1200)
                    {
                        btnLoginDon.Location = new Point(67, 460);
                        btnCikisYap.Location = new Point(230, 460);
                    }
                }

                // 🌟 3. EVRENSEL "UÇAN BUTON" ZIRHI (TÜM SEKMELER İÇİN) 🌟
                if (tabControl1 != null)
                {
                    foreach (TabPage sekme in tabControl1.TabPages)
                    {
                        sekme.AutoScroll = true; // Tüm sekmelerde kaydırmayı aç
                        TehlikeliCapalariSok(sekme); // Tüm tehlikeli çapaları temizle
                    }
                }

                // 🌟 4. BÖLÜCÜ (SPLITCONTAINER) KORUMALARI 🌟
                // --- DEPO KABUL ZIRHI ---
                if (splitContainer3 != null)
                {
                    splitContainer3.FixedPanel = FixedPanel.Panel1;
                    splitContainer3.SplitterDistance = 310;
                    splitContainer3.Panel1.AutoScroll = true;
                    if (panel5 != null) panel5.AutoScroll = true;
                }

                // --- DEPO SAYIM ZIRHI ---
                if (splitContainer4 != null)
                {
                    splitContainer4.FixedPanel = FixedPanel.Panel1;
                    splitContainer4.SplitterDistance = 300;
                    splitContainer4.Panel1.AutoScroll = true;
                }

                // --- FİRMA DÜZENLEME ZIRHI ---
                if (splitContainer1 != null)
                {
                    splitContainer1.FixedPanel = FixedPanel.Panel2;
                    splitContainer1.SplitterDistance = Math.Max(500, this.Width - 500);
                    splitContainer1.Panel2.AutoScroll = true;
                    if (panel11 != null) panel11.AutoScroll = true;
                }

                // --- AMBAR / ÇOKLU ZARF ZIRHI ---
                if (splitContainer2 != null)
                {
                    splitContainer2.Panel1.AutoScroll = true;
                    splitContainer2.Panel2.AutoScroll = true;
                }

                // --- SEVKİYAT PLAN (Komple Sayfa Kaydırma Zırhı) ---
                if (splitContainer5 != null)
                {
                    splitContainer5.Dock = DockStyle.Fill;

                    // 🌟 1. ZIRH: Sayfadaki nesnelerin ezilmemesi için Minimum Boyut (Genişlik: 1200, Yükseklik: 850)
                    // Eğer ekran bu piksellerden küçük olursa nesneler asla ezilmez, direkt kaydırma çubuğu çıkar.
                    splitContainer5.MinimumSize = new Size(1200, 850);

                    // (İç içe çift kaydırma çubuğu çıkmasın diye eski lokal ayarları kapatıyoruz)
                    splitContainer5.Panel1.AutoScroll = false;
                    if (panel7 != null) panel7.AutoScroll = false;
                }

                // 🌟 2. ZIRH: Sevkiyat sekmesine genel (komple) kaydırma çubuğu çıkarma yetkisi ver.
                // Tasarımındaki o sekmenin adı 'tabPage13' olduğu için ona yetki veriyoruz.
                if (tabPage13 != null)
                {
                    tabPage13.AutoScroll = true;
                }

                // --- DİĞER SERBEST PANELLER ---
                if (panel9 != null) { panel9.MinimumSize = new Size(1000, 700); panel9.AutoScroll = true; }
                if (panel10 != null) { panel10.MinimumSize = new Size(1000, 700); panel10.AutoScroll = true; }
                if (panel12 != null) { panel12.MinimumSize = new Size(1200, 700); panel12.AutoScroll = true; }

                // --- YÖNETİM EKRANI TABLO SABİTLEYİCİ ---
                if (dgvKullanicilar != null)
                {
                    dgvKullanicilar.Height = 450;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Sanal Ekran Hatası: " + ex.Message);
            }
        }

        // 🌟 WİNFORMS "UÇAN NESNE" BUG'INI TEMİZLEYEN VİRÜS PROGRAMI 🌟
        private void TehlikeliCapalariSok(Control parent)
        {
            foreach (Control ctrl in parent.Controls)
            {
                // Tablo, Panel, Bölücü (Container) ve Sekmelere dokunmuyoruz, onlar tasarım gereği esnemeli.
                // Sadece Buton, Label, TextBox, Checkbox gibi serbest nesnelerin çivilerini söküp Sola ve Üste (Top | Left) kilitliyoruz.
                if (!(ctrl is DataGridView) && !(ctrl is SplitContainer) && !(ctrl is TabControl) && !(ctrl is Panel))
                {
                    ctrl.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                }

                // Eğer nesnenin içinde başka nesneler de varsa (İç içe klasör gibi) onların da içine girip temizlik yap (Matruşka mantığı)
                if (ctrl.Controls.Count > 0)
                {
                    TehlikeliCapalariSok(ctrl);
                }
            }
        }

        #endregion

        #region 📱 02.10 TABLET VE KÜÇÜK EKRAN (SIVI AKIŞ) RESPONSIVE MOTORU



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

            // 🌟 RAM SIZINTISINI ÖNLEYEN ZIRH BURADA
            using (Font font = new Font("Arial", 7))
            {
                Pen pen = Pens.Black;
                Brush brush = Brushes.Black;

                // (Eski Font tanımlaması buradan silindi)

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

            // Nesnedeki verileri, sağdaki panel kutularına yazdır
            // Hatalar artık yutulmuyor, Visual Studio'nun "Çıktı (Output)" ekranına yazdırılıyor!

            try { if (txtPropText != null) txtPropText.Text = item.Text; }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Metin Hatası: " + ex.Message); }

            try { if (cmbPropPlaceholder != null) cmbPropPlaceholder.SelectedItem = item.PlaceholderKey; }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Değişken Alan Hatası: " + ex.Message); }

            try { if (cmbPropFont != null) cmbPropFont.SelectedItem = item.FontName; }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Font Hatası: " + ex.Message); }

            try { if (cmbPropRotation != null) cmbPropRotation.SelectedItem = item.Rotation.ToString(); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Döndürme Hatası: " + ex.Message); }

            try { if (cmbPropAlignment != null) cmbPropAlignment.SelectedItem = string.IsNullOrEmpty(item.Alignment) ? "Center" : item.Alignment; }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Hizalama Hatası: " + ex.Message); }

            try { if (numPropFontSize != null) numPropFontSize.Value = Math.Max(numPropFontSize.Minimum, Math.Min(numPropFontSize.Maximum, (decimal)item.FontSizePt)); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Font Boyutu Hatası: " + ex.Message); }

            try { if (numPropXmm != null) numPropXmm.Value = Math.Max(numPropXmm.Minimum, Math.Min(numPropXmm.Maximum, (decimal)item.Xmm)); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("X Koordinatı Hatası: " + ex.Message); }

            try { if (numPropYmm != null) numPropYmm.Value = Math.Max(numPropYmm.Minimum, Math.Min(numPropYmm.Maximum, (decimal)item.Ymm)); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Y Koordinatı Hatası: " + ex.Message); }

            try { if (numPropWmm != null) numPropWmm.Value = Math.Max(numPropWmm.Minimum, Math.Min(numPropWmm.Maximum, (decimal)item.Wmm)); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Genişlik Hatası: " + ex.Message); }

            try { if (numPropHmm != null) numPropHmm.Value = Math.Max(numPropHmm.Minimum, Math.Min(numPropHmm.Maximum, (decimal)item.Hmm)); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Yükseklik Hatası: " + ex.Message); }

            try { if (btnPropColor != null) btnPropColor.BackColor = ColorTranslator.FromHtml(item.ColorName); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Renk Dönüştürme Hatası: " + ex.Message); }
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

        #region 📄 06.2 TEKLİ YAZDIRMA VE ÖNİZLEME (NORMAL ZARF) - EDGE MOTORU

        // Tasarım ekranındaki nesneleri anında HTML'e çeviren motor (NORMAL ZARF İÇİN)
        private string TasarimiHtmlCevir(List<DesignItem> items, Firma firma, string wMm, string hMm)
        {
            System.Text.StringBuilder html = new System.Text.StringBuilder();
            html.AppendLine("<!DOCTYPE html><html><head><meta charset='utf-8'><style>");

            // 🌟 KAĞIT BOYUTU VİRGÜL/NOKTA ZIRHI
            string pWidth = wMm.Replace(",", ".");
            string pHeight = hMm.Replace(",", ".");

            // Kağıt boyutunu CSS ile zorla
            html.AppendLine($"@page {{ size: {pWidth}mm {pHeight}mm; margin: 0; }}");
            html.AppendLine("body { margin: 0; padding: 0; font-family: 'Segoe UI', Arial, sans-serif; }");
            html.AppendLine($".zarf {{ position: relative; width: {pWidth}mm; height: {pHeight}mm; overflow: hidden; background-color: white; }}");
            html.AppendLine(".nesne { position: absolute; box-sizing: border-box; word-wrap: break-word; overflow: hidden; }");
            html.AppendLine("</style></head><body><div class='zarf'>");

            foreach (var item in items)
            {
                // 🌟 KOORDİNAT VE PUNTO VİRGÜL/NOKTA ZIRHI
                string x = item.Xmm.ToString().Replace(",", ".");
                string y = item.Ymm.ToString().Replace(",", ".");
                string w = item.Wmm.ToString().Replace(",", ".");
                string h = item.Hmm.ToString().Replace(",", ".");
                string pSize = item.FontSizePt.ToString().Replace(",", ".");

                if (item.Type == "Text" || item.Type == "Label" || item.Type == "Field")
                {
                    string icerik = "";
                    if (item.Type == "Field" && firma != null)
                    {
                        if (item.PlaceholderKey == "FirmaAdi") icerik = firma.FirmaAdi;
                        else if (item.PlaceholderKey == "Adres") icerik = firma.Adres;
                        else if (item.PlaceholderKey == "Il") icerik = firma.Il;
                        else if (item.PlaceholderKey == "Telefon1") icerik = firma.Telefon1;
                        else if (item.PlaceholderKey == "Telefon2") icerik = firma.Telefon2;
                    }
                    else { icerik = item.Text; }

                    if (icerik == null) icerik = "";
                    icerik = icerik.Replace("\n", "<br>");

                    string stil = $"left: {x}mm; top: {y}mm; width: {w}mm; height: {h}mm; " +
                                  $"font-family: '{item.FontName ?? "Arial"}'; font-size: {pSize}pt; color: {item.ColorName};";

                    if (item.FontStyle == FontStyle.Bold || item.FontStyle == (FontStyle.Bold | FontStyle.Italic)) stil += " font-weight: bold;";
                    if (item.FontStyle == FontStyle.Italic || item.FontStyle == (FontStyle.Bold | FontStyle.Italic)) stil += " font-style: italic;";
                    if (item.Alignment == "Center") stil += " text-align: center;";
                    else if (item.Alignment == "Right") stil += " text-align: right;";
                    if (item.Rotation != 0) stil += $" transform: rotate({item.Rotation}deg); transform-origin: left top;";

                    html.AppendLine($"<div class='nesne' style=\"{stil}\">{icerik}</div>");
                }
                else if (item.Type == "Image")
                {
                    if (File.Exists(item.Text))
                    {
                        try
                        {
                            byte[] imageBytes = File.ReadAllBytes(item.Text);
                            string base64String = Convert.ToBase64String(imageBytes);
                            string ext = Path.GetExtension(item.Text).ToLower().Replace(".", "");
                            if (ext == "jpg") ext = "jpeg";

                            string imgData = $"data:image/{ext};base64,{base64String}";
                            string stil = $"left: {x}mm; top: {y}mm; width: {w}mm; height: {h}mm;";
                            html.AppendLine($"<img class='nesne' style=\"{stil}\" src='{imgData}' />");
                        }
                        catch { }
                    }
                }
                else if (item.Type == "Frame")
                {
                    string stil = $"left: {x}mm; top: {y}mm; width: {w}mm; height: {h}mm; border: 1.5px solid black;";
                    html.AppendLine($"<div class='nesne' style=\"{stil}\"></div>");
                }
            }
            html.AppendLine("</div></body></html>");
            return html.ToString();
        }

        // Önizleme Butonu (Artık ikisi de aynı mükemmel Edge penceresini açacak)
        private void BtnPreview_Click(object sender, EventArgs e) { RunEdgePrint(); }

        // Yazdır Butonu
        private void BtnPrint_Click(object sender, EventArgs e) { RunEdgePrint(); }

        // Edge Motoru ile Dinamik Yazdırma Penceresi (İstediğin O Ekran)
        private async void RunEdgePrint()
        {
            var firma = GetSelectedFirmaForPreview();
            if (firma == null) { MessageBox.Show("Lütfen deneme yapmak için veritabanında en az 1 firma bulundurun veya seçin."); return; }

            // Kağıt ölçülerini arayüzden al
            string wMm = txtPageWidthMm.Text;
            string hMm = txtPageHeightMm.Text;

            // Eğer kağıt yataysa ölçüleri ters çevir ki motor anlasın
            if (rbLandscape != null && rbLandscape.Checked)
            {
                wMm = txtPageHeightMm.Text;
                hMm = txtPageWidthMm.Text;
            }

            // Tasarımı HTML Koduna Çevir
            string htmlIcerik = TasarimiHtmlCevir(designItems, firma, wMm, hMm);

            // Tıpkı Ambar Sayfasındaki Gibi Geçici Bir Form Oluştur
            Form modernOnizleme = new Form();
            modernOnizleme.Text = "Normal Zarf Yazdırma (Edge Motoru)";
            modernOnizleme.ShowIcon = false;
            modernOnizleme.Width = 1000;
            modernOnizleme.Height = 600;
            modernOnizleme.StartPosition = FormStartPosition.CenterScreen;

            Microsoft.Web.WebView2.WinForms.WebView2 webCizici = new Microsoft.Web.WebView2.WinForms.WebView2();
            webCizici.Dock = DockStyle.Fill;
            modernOnizleme.Controls.Add(webCizici);

            // Form kapanınca belleği temizle (Çökmeyi engelleyen zırh)
            modernOnizleme.FormClosed += (s, ev) => { webCizici.Dispose(); };
            modernOnizleme.Show();

            // Klasör yetki hatasını önlemek için AppData içinde geçici bir profil yarat
            string appDataYolu = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string zarfHafizaYolu = System.IO.Path.Combine(appDataYolu, "TamgaApp", "Profil_TekliZarf");
            var ozelHafiza = await Microsoft.Web.WebView2.Core.CoreWebView2Environment.CreateAsync(null, zarfHafizaYolu);

            try
            {
                await webCizici.EnsureCoreWebView2Async(ozelHafiza);
            }
            catch (Exception)
            {
                MessageBox.Show("DİKKAT: Bilgisayarınızda yazdırma motoru (WebView2 Runtime) eksik!\n\nLütfen Microsoft'un sitesinden 'Edge WebView2 Runtime' indirip kurun.", "Bileşen Eksik", MessageBoxButtons.OK, MessageBoxIcon.Error);
                modernOnizleme.Close();
                return;
            }

            // Kodları motora bas
            webCizici.NavigateToString(htmlIcerik);

            webCizici.NavigationCompleted += (s, args) =>
            {
                // İŞTE İSTEDİĞİN O EKRANI GETİREN SİHİRLİ KOMUT!
                webCizici.CoreWebView2.ShowPrintUI(Microsoft.Web.WebView2.Core.CoreWebView2PrintDialogKind.Browser);
            };
        }
        #endregion

        #region 📂 06.3 ÇOKLU YAZDIRMA (BATCH PRINTING) VE HTML ÇEVİRİ MOTORU

        // Şablonu HTML ve CSS'e çeviren jilet gibi motor (Sadece Çoklu Zarf İçin Çalışır)
        private string SablonuHtmlCevir(TemplateFile template, List<Firma> firmalar)
        {
            System.Text.StringBuilder html = new System.Text.StringBuilder();
            html.AppendLine("<!DOCTYPE html><html><head><meta charset='utf-8'><style>");

            // 🌟 VİRGÜL/NOKTA ZIRHI: Kağıt boyutlarını CSS formatına (noktalı) uygun hale getir
            string pWidth = template.PageWidthMm.ToString().Replace(",", ".");
            string pHeight = template.PageHeightMm.ToString().Replace(",", ".");

            // Kağıt boyutunu CSS ile zorla
            html.AppendLine($"@page {{ size: {pWidth}mm {pHeight}mm; margin: 0; }}");
            html.AppendLine("body { margin: 0; padding: 0; font-family: 'Segoe UI', Arial, sans-serif; }");
            html.AppendLine($".zarf {{ position: relative; width: {pWidth}mm; height: {pHeight}mm; page-break-after: always; overflow: hidden; background-color: white; }}");
            html.AppendLine(".nesne { position: absolute; box-sizing: border-box; word-wrap: break-word; overflow: hidden; }");
            html.AppendLine("</style></head><body>");

            foreach (var firma in firmalar)
            {
                html.AppendLine("<div class='zarf'>");
                foreach (var item in template.DesignItems)
                {
                    // 🌟 VİRGÜL/NOKTA ZIRHI: Bütün koordinatları ve boyutları HTML'in anlayacağı formata çeviriyoruz
                    string x = item.Xmm.ToString().Replace(",", ".");
                    string y = item.Ymm.ToString().Replace(",", ".");
                    string w = item.Wmm.ToString().Replace(",", ".");
                    string h = item.Hmm.ToString().Replace(",", ".");
                    string pSize = item.FontSizePt.ToString().Replace(",", ".");

                    if (item.Type == "Text" || item.Type == "Label" || item.Type == "Field")
                    {
                        string icerik = "";
                        if (item.Type == "Field" && firma != null)
                        {
                            if (item.PlaceholderKey == "FirmaAdi") icerik = firma.FirmaAdi;
                            else if (item.PlaceholderKey == "Adres") icerik = firma.Adres;
                            else if (item.PlaceholderKey == "Il") icerik = firma.Il;
                            else if (item.PlaceholderKey == "Telefon1") icerik = firma.Telefon1;
                            else if (item.PlaceholderKey == "Telefon2") icerik = firma.Telefon2;
                        }
                        else { icerik = item.Text; }

                        if (icerik == null) icerik = "";
                        icerik = icerik.Replace("\n", "<br>");

                        string stil = $"left: {x}mm; top: {y}mm; width: {w}mm; height: {h}mm; " +
                                      $"font-family: '{item.FontName ?? "Arial"}'; font-size: {pSize}pt; color: {item.ColorName};";

                        if (item.FontStyle == FontStyle.Bold || item.FontStyle == (FontStyle.Bold | FontStyle.Italic)) stil += " font-weight: bold;";
                        if (item.FontStyle == FontStyle.Italic || item.FontStyle == (FontStyle.Bold | FontStyle.Italic)) stil += " font-style: italic;";
                        if (item.Alignment == "Center") stil += " text-align: center;";
                        else if (item.Alignment == "Right") stil += " text-align: right;";
                        if (item.Rotation != 0) stil += $" transform: rotate({item.Rotation}deg); transform-origin: left top;";

                        html.AppendLine($"<div class='nesne' style=\"{stil}\">{icerik}</div>");
                    }
                    else if (item.Type == "Image")
                    {
                        if (File.Exists(item.Text))
                        {
                            try
                            {
                                byte[] imageBytes = File.ReadAllBytes(item.Text);
                                string base64String = Convert.ToBase64String(imageBytes);
                                string ext = Path.GetExtension(item.Text).ToLower().Replace(".", "");
                                if (ext == "jpg") ext = "jpeg";

                                string imgData = $"data:image/{ext};base64,{base64String}";
                                string stil = $"left: {x}mm; top: {y}mm; width: {w}mm; height: {h}mm;";
                                html.AppendLine($"<img class='nesne' style=\"{stil}\" src='{imgData}' />");
                            }
                            catch { }
                        }
                    }
                    else if (item.Type == "Frame")
                    {
                        string stil = $"left: {x}mm; top: {y}mm; width: {w}mm; height: {h}mm; border: 1.5px solid black;";
                        html.AppendLine($"<div class='nesne' style=\"{stil}\"></div>");
                    }
                }
                html.AppendLine("</div>");
            }

            html.AppendLine("</body></html>");
            return html.ToString();
        }


        private void btnManuelZarf_Click(object sender, EventArgs e)
        {
            // 🌟 1. AŞAMA: Hiç tasarımla uğraşmadan kodla şık bir pencere (Form) yaratıyoruz
            Form frmManuel = new Form();
            frmManuel.Text = "Manuel Zarf Yazdırma";
            frmManuel.Size = new Size(420, 360);
            frmManuel.StartPosition = FormStartPosition.CenterParent;
            frmManuel.FormBorderStyle = FormBorderStyle.FixedDialog;
            frmManuel.MaximizeBox = false;
            frmManuel.MinimizeBox = false;
            frmManuel.BackColor = Color.FromArgb(245, 247, 250); // Elit Gri Arkaplan

            // 🌟 2. AŞAMA: Kutuları ve Başlıkları (Label) oluşturuyoruz
            string[] etiketler = { "Firma Adı:", "Adres:", "İl:", "Telefon 1:", "Telefon 2:" };
            TextBox[] kutular = new TextBox[5];

            for (int i = 0; i < 5; i++)
            {
                Label lbl = new Label();
                lbl.Text = etiketler[i];
                lbl.Left = 20;
                lbl.Top = 20 + (i * 45);
                lbl.AutoSize = true;
                lbl.Font = new Font("Segoe UI", 10, FontStyle.Bold);
                lbl.ForeColor = Color.FromArgb(33, 37, 41);

                kutular[i] = new TextBox();
                kutular[i].Left = 130;
                kutular[i].Top = 18 + (i * 45);
                kutular[i].Width = 240;
                kutular[i].Font = new Font("Segoe UI", 10);

                // Çok satırlı adres girişi için 2. kutuyu (Adres) büyütelim
                if (i == 1) { kutular[i].Multiline = true; kutular[i].Height = 40; }

                frmManuel.Controls.Add(lbl);
                frmManuel.Controls.Add(kutular[i]);
            }

            // 🌟 3. AŞAMA: Kocaman, şık bir "YAZDIR" butonu ekliyoruz
            Button btnYazdir = new Button();
            btnYazdir.Text = "🖨️ YAZDIR";
            btnYazdir.Left = 130;
            btnYazdir.Top = 250;
            btnYazdir.Width = 240;
            btnYazdir.Height = 45;
            btnYazdir.BackColor = Color.FromArgb(15, 76, 58); // Koyu Yeşil
            btnYazdir.ForeColor = Color.White;
            btnYazdir.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            btnYazdir.FlatStyle = FlatStyle.Flat;
            btnYazdir.Cursor = Cursors.Hand;
            frmManuel.Controls.Add(btnYazdir);

            // 🌟 4. AŞAMA: Yazdır Butonuna basıldığında olacaklar
            btnYazdir.Click += (s, ev) =>
            {
                // Kutulardaki yazıları al
                string mFirma = kutular[0].Text;
                string mAdres = kutular[1].Text;
                string mIl = kutular[2].Text;
                string mTel1 = kutular[3].Text;
                string mTel2 = kutular[4].Text;

                if (string.IsNullOrWhiteSpace(mFirma))
                {
                    MessageBox.Show("Firma Adı boş bırakılamaz!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // --- YAZDIRMA MOTORUNA GÖNDERME KISMI ---
                // Sisteminde yazdırma işlemini yapan metoda bu verileri göndermelisin.
                // Örnek kullanım:
                // Firma geciciFirma = new Firma { FirmaAdi = mFirma, Adres = mAdres, Il = mIl, Telefon1 = mTel1, Telefon2 = mTel2 };
                // ZarfiYaziciyaGonder(geciciFirma);

                MessageBox.Show($"{mFirma} için yazdırma komutu gönderildi!", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                frmManuel.Close(); // İşlem bitince küçük pencereyi kapat
            };

            // 🌟 5. AŞAMA: Pencereyi Ekranda Göster
            frmManuel.ShowDialog();
        }

        // Tıpkı Ambar Sayfasındaki Gibi Dinamik Arayüz Açan Motor
        private async void btnCokluZarfYazdir_Click(object sender, EventArgs e)
        {
            if (lstSecilenFirmalar.CheckedItems.Count == 0) { MessageBox.Show("Lütfen firmaları işaretleyin."); return; }
            if (cmbPrintStyle.SelectedItem == null) { MessageBox.Show("Şablon seçin."); return; }

            string path = Path.Combine(GetTemplatesDirectory(), cmbPrintStyle.SelectedItem.ToString());
            if (!File.Exists(path)) return;

            var loadedTemplate = JsonConvert.DeserializeObject<TemplateFile>(File.ReadAllText(path));
            if (loadedTemplate == null) return;

            // İşaretlenen firmaları listede topla
            List<Firma> batchFirmsList = new List<Firma>();
            foreach (var item in lstSecilenFirmalar.CheckedItems)
            {
                string satirMetni = item.ToString();

                // Manuel Eklenen Adres Kontrolü
                if (satirMetni.Contains("MANUEL"))
                {
                    string[] parcalar = satirMetni.Split('-');
                    if (parcalar.Length >= 2) batchFirmsList.Add(new Firma { FirmaAdi = parcalar[1].Trim() });
                }
                else
                {
                    if (int.TryParse(satirMetni.Split('-')[0].Trim(), out int id))
                    {
                        var f = DataAccess.GetFirmaById(id);
                        if (f != null) batchFirmsList.Add(f);
                    }
                }
            }

            // HTML Koduna Çevir (Döngü her firmayı alt alta 'page-break' ile ekleyecek)
            string htmlIcerik = SablonuHtmlCevir(loadedTemplate, batchFirmsList);

            // 3. YAZDIRMA ALANI PENCERESİNİ DÜZENLE
            Form modernOnizleme = new Form();
            modernOnizleme.Text = "Çoklu Zarf Yazdırma (Edge Motoru)";
            modernOnizleme.ShowIcon = false;
            modernOnizleme.Width = 1000;
            modernOnizleme.Height = 600;
            modernOnizleme.StartPosition = FormStartPosition.CenterScreen;

            // 4. WEB MOTORUNU BAĞLA VE YAZDIR
            Microsoft.Web.WebView2.WinForms.WebView2 webCizici = new Microsoft.Web.WebView2.WinForms.WebView2();
            webCizici.Dock = DockStyle.Fill;
            modernOnizleme.Controls.Add(webCizici);

            // Formu kapattığında arkada açık kalarak sistemi çökertmemesi için temizlik zırhı
            modernOnizleme.FormClosed += (s, ev) => { webCizici.Dispose(); };
            modernOnizleme.Show();

            // Çökmeyi engelleyen, geçici profil klasörü mantığı!
            string appDataYolu = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string zarfHafizaYolu = System.IO.Path.Combine(appDataYolu, "TamgaApp", "Profil_TopluZarf");
            var ozelHafiza = await Microsoft.Web.WebView2.Core.CoreWebView2Environment.CreateAsync(null, zarfHafizaYolu);

            try
            {
                await webCizici.EnsureCoreWebView2Async(ozelHafiza);
            }
            catch (Exception)
            {
                MessageBox.Show("DİKKAT: Bilgisayarınızda yazdırma motoru (WebView2 Runtime) eksik!\n\nLütfen Microsoft'un sitesinden 'Edge WebView2 Runtime' indirip kurun.", "Bileşen Eksik", MessageBoxButtons.OK, MessageBoxIcon.Error);
                modernOnizleme.Close();
                return;
            }

            // Ürettiğimiz HTML'i motora yüklüyoruz
            webCizici.NavigateToString(htmlIcerik);

            // Yükleme bitince İSTEDİĞİN O YAZDIRMA PENCERESİNİ AÇ!
            webCizici.NavigationCompleted += (s, args) =>
            {
                webCizici.CoreWebView2.ShowPrintUI(Microsoft.Web.WebView2.Core.CoreWebView2PrintDialogKind.Browser);
            };
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
                    firma = GetSelectedFirmaForPreview();
                }

                // Çizilecek hiçbir nesne yoksa işlemi bitir
                if (designItems == null || designItems.Count == 0) { e.HasMorePages = false; return; }

                // Kağıt üzerindeki tüm nesneleri (Etiket, Çerçeve, Resim vb.) tek tek çiz
                // 🌟 YAZICI KALİBRASYON (KAYMA) AYARLARI (MİLİMETRE CİNSİNDEN) 🌟
                // Yazıcı markasına göre fiziksel kenar boşlukları değişebilir. 
                // Eğer yazılar kağıtta sağa/sola veya aşağı/yukarı kayıyorsa buradaki 0 değerlerini değiştir.
                // Örnek: Sağa 5mm kaydırmak için 5f, Yukarı kaydırmak için -5f yazabilirsin.
                float xKaymaMm = 0f;
                float yKaymaMm = 0f;

                // Çizilecek hiçbir nesne yoksa işlemi bitir
                if (designItems == null || designItems.Count == 0) { e.HasMorePages = false; return; }

                // Kağıt üzerindeki tüm nesneleri (Etiket, Çerçeve, Resim vb.) tek tek çiz
                foreach (var item in designItems)
                {
                    // 🌟 KAYMA AYARLARINI (xKaymaMm ve yKaymaMm) BURADA DAHİL EDİYORUZ
                    float x = (item.Xmm + xKaymaMm) * printerMmToPx;
                    float y = (item.Ymm + yKaymaMm) * printerMmToPx;
                    float w = item.Wmm * printerMmToPx;
                    float h = item.Hmm * printerMmToPx;

                    // YENİ KOD BU: Koordinatları içeriden başlatıp kutuyu 10 piksel daraltıyoruz.
                    Rectangle rect = new Rectangle(
                        (int)x + 5,   // 5 piksel sağdan başlat
                        (int)y + 5,   // 5 piksel aşağıdan başlat
                        (int)w - 10,  // Kutuyu 10 piksel daralt ki taşmasın
                        (int)h - 10
                    );

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

            // 2. Veritabanından tüm firmaları çek ve MASAYA (Cache'e) KOY!
            tumFirmalarCache = DataAccess.GetAllFirmalar();

            // 3. Çekilen her bir firma için masadan (cache) döngüye gir
            foreach (var f in tumFirmalarCache)
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
                string secilenMetin = lstFirmalar.SelectedItem.ToString();
                string[] parcalar = secilenMetin.Split('-');

                // ZIRH: Eğer '-' işaretiyle bölünmüş bir ID varsa ve bu gerçekten bir rakamsa işlem yap!
                if (parcalar.Length > 0 && int.TryParse(parcalar[0].Trim(), out int id))
                {
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

        // 🌟 SADECE "NORMAL ZARF" SAYFASINDAKİ YENİ BUTON İÇİN DİREKT YAZDIRMA MOTORU 🌟
        private void btnNormalManuelYazdir_Click(object sender, EventArgs e)
        {
            Form frmManuel = new Form
            {
                Width = 400,
                Height = 350,
                Text = "Manuel Zarf Yazdırma (Tek Seferlik)",
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false,
                Icon = this.Icon,
                BackColor = Color.WhiteSmoke
            };

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

            Button btnYazdir = new Button
            {
                Text = "🖨️ DİREKT YAZDIR",
                Left = 120,
                Top = 260,
                Width = 240,
                Height = 40,
                BackColor = Color.Orange,
                ForeColor = Color.Black,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.Flat
            };

            frmManuel.Controls.Add(lblFirma); frmManuel.Controls.Add(txtFirma);
            frmManuel.Controls.Add(lblAdres); frmManuel.Controls.Add(txtAdres);
            frmManuel.Controls.Add(lblIl); frmManuel.Controls.Add(txtIl);
            frmManuel.Controls.Add(lblTel1); frmManuel.Controls.Add(txtTel1);
            frmManuel.Controls.Add(lblTel2); frmManuel.Controls.Add(txtTel2);
            frmManuel.Controls.Add(btnYazdir);

            btnYazdir.Click += (s, args) =>
            {
                if (string.IsNullOrWhiteSpace(txtFirma.Text))
                {
                    MessageBox.Show("Firma Adı zorunludur!", "Eksik Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Listeye VEYA Tabloya bulaşmadan sanal firma oluşturuyoruz
                Firma geciciFirma = new Firma
                {
                    FirmaAdi = txtFirma.Text.Trim(),
                    Adres = txtAdres.Text.Trim(),
                    Il = txtIl.Text.Trim(),
                    Telefon1 = txtTel1.Text.Trim(),
                    Telefon2 = txtTel2.Text.Trim()
                };

                frmManuel.Close();
                ManuelZarfiEdgeIleYazdir(geciciFirma);
            };

            frmManuel.ShowDialog();
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
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                string okunanBarkod = txtBarkodOkut.Text.Trim();

                if (!string.IsNullOrEmpty(okunanBarkod))
                {
                    Urun bulunanUrun = DataAccess.GetUrunByBarkod(okunanBarkod);

                    if (bulunanUrun == null)
                    {
                        bulunanUrun = new Urun { UrunKodu = "KAYITSIZ", Aciklama = "SİSTEMDE BULUNAMADI!", Barkod = okunanBarkod, Renk = "" };
                    }

                    bool varMi = false;
                    foreach (DataGridViewRow row in dgvUretim.Rows)
                    {
                        if (row.Cells[3].Value != null && row.Cells[3].Value.ToString() == okunanBarkod)
                        {
                            int mevcutAdet = Convert.ToInt32(row.Cells[2].Value);
                            row.Cells[2].Value = mevcutAdet + 1;
                            varMi = true;
                            break;
                        }
                    }

                    if (!varMi)
                    {
                        // Artık tek bir tanımlama var, hata vermeyecek:
                        string lavaboRengi = bulunanUrun.Renk;
                        dgvUretim.Rows.Add(bulunanUrun.UrunKodu, bulunanUrun.Aciklama, 1, bulunanUrun.Barkod, lavaboRengi);
                    }
                }
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

            // Klasör yapısını oluştur
            string anaYol = string.IsNullOrWhiteSpace(txtKayitYeri.Text)
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Günlük Üretim Takip")
                : txtKayitYeri.Text;

            if (!Directory.Exists(anaYol)) Directory.CreateDirectory(anaYol);

            // 🌟 SİHİRLİ DOKUNUŞ: Dosya adını direkt tarih yapıyoruz
            string dosyaAdi = $"{secilenTarih:dd.MM.yyyy}.csv";
            string dosyaYolu = Path.Combine(anaYol, dosyaAdi);

            // Aynı isimde dosya varsa üzerine yazar veya ekleme yapar (burada üzerine yazıyor)
            using (StreamWriter sw = new StreamWriter(dosyaYolu, false, System.Text.Encoding.UTF8))
            {
                sw.WriteLine("Ürün Kodu;Ürün Açıklaması;Ürün Adeti;Ürün Barkodu");
                foreach (DataGridViewRow row in dgvUretim.Rows)
                {
                    if (!row.IsNewRow && row.Cells[0].Value != null)
                    {
                        sw.WriteLine($"{row.Cells[0].Value};{row.Cells[1].Value};{row.Cells[2].Value};{row.Cells[3].Value}");
                    }
                }
            }

            if (chkYazdir != null && chkYazdir.Checked) UretimListesiYazdir(secilenTarih);

            MessageBox.Show($"Üretim verileri başarıyla kaydedildi!\nDosya: {dosyaAdi}", "Kayıt Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
            dgvUretim.Rows.Clear();
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

            // 🌟 DİREKT YAZDIRMA İPTAL EDİLDİ: Artık ekrana diğerleri gibi yazdırma penceresi (Önizleme) çıkacak
            PrintPreviewDialog onizleme = new PrintPreviewDialog
            {
                Document = pdUretim,
                Width = 900,
                Height = 700,
                ShowIcon = false,
                StartPosition = FormStartPosition.CenterScreen,
                Text = "Depo Kabul Raporu Yazdırma Penceresi"
            };

            // Yakınlaştırma ayarını %100 (Birebir boyut) yap ki ekranda tam net görünsün
            onizleme.PrintPreviewControl.Zoom = 1.0;

            try
            {
                onizleme.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Önizleme penceresi açılırken hata oluştu: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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

        #region 📂 10.3 GEÇMİŞ RAPORLARI GÖRÜNTÜLEME VE İPTAL MOTORU

        // Kaydedilmiş klasörü tarar ve dosyaları Yıl > Ay ağacına (TreeView) yerleştirir
        private void btnRaporYenile_Click(object sender, EventArgs e)
        {
            string anaYol = string.IsNullOrWhiteSpace(txtKayitYeri.Text)
                ? System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Günlük Üretim Takip")
                : txtKayitYeri.Text;

            if (tvRaporlar != null) tvRaporlar.Nodes.Clear();

            if (!System.IO.Directory.Exists(anaYol))
            {
                MessageBox.Show("Henüz kaydedilmiş hiçbir üretim raporu bulunamadı.", "Arşiv Boş", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Ağacın Kökünü Oluştur
            TreeNode kok = new TreeNode("📦 Üretim (Kabul) Arşivi") { Tag = "KOK" };
            tvRaporlar.Nodes.Add(kok);

            System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(anaYol);
            System.IO.FileInfo[] raporDosyalari = di.GetFiles("*.csv", System.IO.SearchOption.AllDirectories);

            if (raporDosyalari.Length == 0)
            {
                MessageBox.Show("Arşiv klasöründe hiç CSV dosyası yok.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Dosyaları Yıl ve Ay'a göre klasörle
            foreach (var dosya in raporDosyalari)
            {
                string dosyaAdi = dosya.Name;
                string dYil = "Diğer";
                string dAy = "Diğer";

                // Bizim formatımız: 22.07.2026.csv
                string[] adParcalari = dosyaAdi.Replace(".csv", "").Split('.');
                if (adParcalari.Length == 3)
                {
                    dYil = adParcalari[2]; // Yıl (Örn: 2026)
                    dAy = adParcalari[1];  // Ay (Örn: 07)
                }
                else
                {
                    // Eğer isminde tarih yoksa, dosyanın oluşturulma tarihinden cımbızla (Zırh)
                    dYil = dosya.CreationTime.ToString("yyyy");
                    dAy = dosya.CreationTime.ToString("MM");
                }

                // Ağaçta Yıl klasörü var mı bak, yoksa oluştur
                TreeNode yilNode = kok.Nodes.Cast<TreeNode>().FirstOrDefault(n => n.Text == dYil) ?? kok.Nodes.Add(dYil, dYil);

                // Yılın içinde Ay klasörü var mı bak, yoksa oluştur
                TreeNode ayNode = yilNode.Nodes.Cast<TreeNode>().FirstOrDefault(n => n.Text == dAy) ?? yilNode.Nodes.Add(dAy, dAy);

                // Ay klasörünün içine raporu ekle
                ayNode.Nodes.Add(new TreeNode("📄 " + dosyaAdi) { Tag = dosya.FullName, ForeColor = Color.DarkBlue });
            }

            kok.Expand(); // Sadece en dıştaki ana klasörü açık getir, kalabalık yapmasın
        }

        // Ağaçtan seçilen CSV dosyasını okuyup, program içinde yeni bir pencerede (rapor okuyucu) açar
        private void btnRaporAc_Click(object sender, EventArgs e)
        {
            if (tvRaporlar.SelectedNode == null || tvRaporlar.SelectedNode.Tag == null || !tvRaporlar.SelectedNode.Tag.ToString().EndsWith(".csv"))
            {
                MessageBox.Show("Lütfen açmak için ağaçtan bir rapor dosyası (📄) seçin!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string dosyaYolu = tvRaporlar.SelectedNode.Tag.ToString();
            System.IO.FileInfo secilenDosya = new System.IO.FileInfo(dosyaYolu);

            Form frm = new Form { Text = "Rapor Detayı: " + secilenDosya.Name, Size = new Size(1000, 700), StartPosition = FormStartPosition.CenterScreen, Icon = this.Icon };
            DataGridView dgv = new DataGridView { Dock = DockStyle.Fill, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, AllowUserToAddRows = false, ReadOnly = true, BackgroundColor = Color.WhiteSmoke };

            DataTable dt = new DataTable();
            string[] satirlar = System.IO.File.ReadAllLines(secilenDosya.FullName, System.Text.Encoding.UTF8);

            if (satirlar.Length > 0)
            {
                string[] basliklar = satirlar[0].Split(';');
                foreach (string b in basliklar) dt.Columns.Add(b);
                for (int i = 1; i < satirlar.Length; i++) dt.Rows.Add(satirlar[i].Split(';'));
            }

            BindingSource bs = new BindingSource { DataSource = dt };
            dgv.DataSource = bs;

            Panel pnl = new Panel { Dock = DockStyle.Top, Height = 45, BackColor = Color.FromArgb(240, 240, 240) };

            TextBox txtFiltre = new TextBox { Width = 300, Location = new Point(60, 10), Font = new Font("Segoe UI", 10), Text = "Buraya yazarak filtrele...", ForeColor = Color.Gray };

            txtFiltre.Enter += (s, ev) =>
            {
                if (txtFiltre.Text == "Buraya yazarak filtrele...") { txtFiltre.Text = ""; txtFiltre.ForeColor = Color.Black; }
            };
            txtFiltre.Leave += (s, ev) =>
            {
                if (string.IsNullOrWhiteSpace(txtFiltre.Text)) { txtFiltre.Text = "Buraya yazarak filtrele..."; txtFiltre.ForeColor = Color.Gray; }
            };

            txtFiltre.TextChanged += (s, ev) =>
            {
                if (txtFiltre.Text == "Buraya yazarak filtrele...") return;
                string aranan = txtFiltre.Text.Replace("'", "''");
                bs.Filter = string.Join(" OR ", dt.Columns.Cast<DataColumn>().Select(c => $"[{c.ColumnName}] LIKE '%{aranan}%'"));
            };

            pnl.Controls.Add(new Label { Text = "Filtre:", Location = new Point(10, 12), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) });
            pnl.Controls.Add(txtFiltre);

            frm.Controls.Add(dgv);
            frm.Controls.Add(pnl);
            frm.ShowDialog();
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

                    string ayarDosyasi = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "KayitYeri.txt");
                    System.IO.File.WriteAllText(ayarDosyasi, fbd.SelectedPath);

                    MessageBox.Show("Rapor kayıt yeri başarıyla güncellendi!", "Ayarlar Kaydedildi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void btnGecmisSevkleriListele_Click(object sender, EventArgs e)
        {
            string kokYol = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "TamgaApp Tamamlanan Sevkiyatlar");
            if (!Directory.Exists(kokYol))
            {
                MessageBox.Show("Henüz tamamlanmış hiçbir sevkiyat arşivi bulunamadı.", "Arşiv Boş", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            Form frmArsiv = new Form { Text = "Gelişmiş Geçmiş Sevkiyatlar Arşivi", Size = new Size(1200, 750), StartPosition = FormStartPosition.CenterScreen, Icon = this.Icon };

            Panel pnlUst = new Panel { Dock = DockStyle.Top, Height = 70, BackColor = Color.FromArgb(45, 52, 54), ForeColor = Color.White };

            Label lblTur = new Label { Text = "Tür:", AutoSize = true, Location = new Point(15, 25), Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            ComboBox cmbTur = new ComboBox { Location = new Point(55, 22), Width = 100, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbTur.Items.AddRange(new string[] { "Tümü", "İhracat", "Yurtiçi" }); cmbTur.SelectedIndex = 0;

            Label lblYil = new Label { Text = "Yıl:", AutoSize = true, Location = new Point(165, 25), Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            ComboBox cmbYil = new ComboBox { Location = new Point(195, 22), Width = 80, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbYil.Items.Add("Tümü"); cmbYil.SelectedIndex = 0;

            Label lblAy = new Label { Text = "Ay:", AutoSize = true, Location = new Point(285, 25), Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            ComboBox cmbAy = new ComboBox { Location = new Point(315, 22), Width = 60, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbAy.Items.Add("Tümü"); cmbAy.SelectedIndex = 0;

            Label lblGun = new Label { Text = "Gün:", AutoSize = true, Location = new Point(385, 25), Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            ComboBox cmbGun = new ComboBox { Location = new Point(425, 22), Width = 60, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbGun.Items.Add("Tümü"); cmbGun.SelectedIndex = 0;

            Label lblFirma = new Label { Text = "Firma Ara:", AutoSize = true, Location = new Point(495, 25), Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            TextBox txtFirmaAra = new TextBox { Location = new Point(575, 22), Width = 180, Font = new Font("Segoe UI", 10) };

            Button btnFiltrele = new Button { Text = "🔍 Sorgula", Location = new Point(765, 20), Width = 100, Height = 30, BackColor = Color.Teal, ForeColor = Color.White, Font = new Font("Segoe UI", 9, FontStyle.Bold), FlatStyle = FlatStyle.Flat };

            Button btnGeriAl = new Button();
            btnGeriAl.Text = "↩️ SEVKİYATI GERİ AL";
            btnGeriAl.BackColor = Color.FromArgb(231, 76, 60);
            btnGeriAl.ForeColor = Color.White;
            btnGeriAl.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            btnGeriAl.Size = new Size(160, 30);
            btnGeriAl.Location = new Point(875, 20);
            btnGeriAl.Cursor = Cursors.Hand;
            btnGeriAl.FlatStyle = FlatStyle.Flat;

            Button btnDuzenle = new Button();
            btnDuzenle.Text = "✏️ DÜZENLE (REVİZE)";
            btnDuzenle.BackColor = Color.DarkOrange;
            btnDuzenle.ForeColor = Color.Black;
            btnDuzenle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            btnDuzenle.Size = new Size(150, 30);
            btnDuzenle.Location = new Point(1040, 20);
            btnDuzenle.Cursor = Cursors.Hand;
            btnDuzenle.FlatStyle = FlatStyle.Flat;

            pnlUst.Controls.AddRange(new Control[] { lblTur, cmbTur, lblYil, cmbYil, lblAy, cmbAy, lblGun, cmbGun, lblFirma, txtFirmaAra, btnFiltrele, btnGeriAl, btnDuzenle });

            try
            {
                var yillar = Directory.GetDirectories(kokYol, "*", SearchOption.AllDirectories)
                    .Select(d => new DirectoryInfo(d).Name).Where(n => n.Length == 4 && n.StartsWith("20")).Distinct().OrderBy(x => x);
                foreach (var y in yillar) cmbYil.Items.Add(y);
                for (int i = 1; i <= 12; i++) cmbAy.Items.Add(i.ToString("D2"));
                for (int i = 1; i <= 31; i++) cmbGun.Items.Add(i.ToString("D2"));
            }
            catch { }

            SplitContainer split = new SplitContainer { Dock = DockStyle.Fill, SplitterDistance = 350 };
            TreeView tvArsiv = new TreeView { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 11) };
            split.Panel1.Controls.Add(tvArsiv);

            DataGridView dgvDetay = new DataGridView { Dock = DockStyle.Fill, AllowUserToAddRows = false, ReadOnly = true, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, BackgroundColor = Color.WhiteSmoke, SelectionMode = DataGridViewSelectionMode.FullRowSelect };
            Button btnYazdir = new Button { Text = "🖨️ Seçili Raporu Çıktı Al (Edge)", Dock = DockStyle.Bottom, Height = 50, BackColor = Color.Orange, Font = new Font("Segoe UI", 11, FontStyle.Bold) };
            split.Panel2.Controls.Add(dgvDetay);
            split.Panel2.Controls.Add(btnYazdir);

            frmArsiv.Controls.Add(split);
            frmArsiv.Controls.Add(pnlUst);

            string aktifDosyaYolu = "";
            string aktifDosyaAdi = "";

            Action AgaciDoldur = () =>
            {
                tvArsiv.Nodes.Clear();
                TreeNode kok = new TreeNode("📦 Filtrelenmiş Arşiv") { Tag = "KOK" };
                tvArsiv.Nodes.Add(kok);

                string seciliTur = cmbTur.SelectedItem.ToString();
                string seciliYil = cmbYil.SelectedItem.ToString();
                string seciliAy = cmbAy.SelectedItem.ToString();
                string seciliGun = cmbGun.SelectedItem.ToString();
                string arananFirma = txtFirmaAra.Text.Trim().ToLower();

                string[] tumDosyalar = Directory.GetFiles(kokYol, "*.csv", SearchOption.AllDirectories);

                foreach (string dosya in tumDosyalar)
                {
                    string[] parcalar = dosya.Substring(kokYol.Length + 1).Split(Path.DirectorySeparatorChar);
                    if (parcalar.Length >= 4)
                    {
                        string dTur = parcalar[0];
                        string dYil = parcalar[1];
                        string dAy = parcalar[2];
                        string dGun = parcalar[3];
                        string dAd = Path.GetFileNameWithoutExtension(dosya);

                        if (seciliTur != "Tümü" && dTur != seciliTur) continue;
                        if (seciliYil != "Tümü" && dYil != seciliYil) continue;
                        if (seciliAy != "Tümü" && dAy != seciliAy) continue;
                        if (seciliGun != "Tümü" && dGun != seciliGun) continue;
                        if (!string.IsNullOrEmpty(arananFirma) && !dAd.ToLower().Contains(arananFirma)) continue;

                        TreeNode turNode = kok.Nodes.Cast<TreeNode>().FirstOrDefault(n => n.Text == dTur) ?? kok.Nodes.Add(dTur, dTur);
                        TreeNode yilNode = turNode.Nodes.Cast<TreeNode>().FirstOrDefault(n => n.Text == dYil) ?? turNode.Nodes.Add(dYil, dYil);
                        TreeNode ayNode = yilNode.Nodes.Cast<TreeNode>().FirstOrDefault(n => n.Text == dAy) ?? yilNode.Nodes.Add(dAy, dAy);
                        TreeNode gunNode = ayNode.Nodes.Cast<TreeNode>().FirstOrDefault(n => n.Text == dGun) ?? ayNode.Nodes.Add(dGun, dGun);

                        gunNode.Nodes.Add(new TreeNode("📄 " + dAd) { Tag = dosya, ForeColor = Color.DarkBlue });
                    }
                }
                kok.ExpandAll();
            };

            btnFiltrele.Click += (btnSender, btnEv) => AgaciDoldur();
            AgaciDoldur();

            btnGeriAl.Click += (btnSender, btnEv) =>
            {
                if (tvArsiv.SelectedNode == null || tvArsiv.SelectedNode.Tag == null || !tvArsiv.SelectedNode.Tag.ToString().EndsWith(".csv"))
                {
                    MessageBox.Show("Lütfen sol taraftaki ağaçtan iptal etmek istediğiniz sevkiyat dosyasını seçin!", "Dosya Seçilmedi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string dosyaYolu = tvArsiv.SelectedNode.Tag.ToString();
                string dosyaAdi = tvArsiv.SelectedNode.Text;

                DialogResult onay = MessageBox.Show($"DİKKAT! '{dosyaAdi}' sevkiyatını iptal edip GERİ ALMAK istiyor musunuz?\n\nBu işlemle:\n1. Arşivdeki bu CSV dosyası tamamen silinecek.\n2. Sistem bu dosyayı bulamadığı için içindeki belge numaralarını tekrar 'BEKLEYENLER' listesine düşürecektir.\n\nEmin misiniz?", "Sevkiyatı Geri Al Onayı", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);

                if (onay == DialogResult.Yes)
                {
                    try
                    {
                        if (File.Exists(dosyaYolu))
                        {
                            string[] satirlar = File.ReadAllLines(dosyaYolu, System.Text.Encoding.UTF8);
                            if (satirlar.Length > 1)
                            {
                                string[] huc = satirlar[1].Split(';');
                                if (huc.Length >= 3)
                                {
                                    string belgeNolar = huc[2];
                                    string[] belgeler = belgeNolar.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);

                                    foreach (string b in belgeler)
                                    {
                                        TamamlananBelgeNolar.Remove(b);
                                    }

                                    string txtYol = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TamgaApp", "KapananBelgeler.txt");
                                    if (File.Exists(txtYol))
                                    {
                                        var guncelListe = File.ReadAllLines(txtYol).Where(x => !belgeler.Contains(x.Trim())).ToList();
                                        File.WriteAllLines(txtYol, guncelListe);
                                    }
                                }
                            }

                            File.Delete(dosyaYolu);

                            MessageBox.Show("Sevkiyat başarıyla iptal edildi ve geri alındı!", "İşlem Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);

                            tvArsiv.Nodes.Remove(tvArsiv.SelectedNode);
                            dgvDetay.DataSource = null;
                            dgvDetay.Rows.Clear();
                            dgvDetay.Columns.Clear();

                            btnSiparisYenile_Click(null, null);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Geri alma işlemi sırasında hata oluştu: \n" + ex.Message, "Kritik Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            };

            // 🌟 İŞTE HATA VEREN DÜZENLE BUTONUNU TVARSIV OLUŞTURULDUKTAN SONRAYA ALDIK!
            btnDuzenle.Click += (btnSender, btnEv) =>
            {
                if (tvArsiv.SelectedNode == null || tvArsiv.SelectedNode.Tag == null || !tvArsiv.SelectedNode.Tag.ToString().EndsWith(".csv"))
                {
                    MessageBox.Show("Lütfen düzenlemek istediğiniz geçmiş sevkiyat dosyasını sol ağaçtan seçin!", "Seçim Yok", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string dosyaYolu = tvArsiv.SelectedNode.Tag.ToString();
                string dosyaAdi = tvArsiv.SelectedNode.Text.Replace("📄 ", "");

                DialogResult onay = MessageBox.Show($"'{dosyaAdi}' isimli sevkiyat raporu REVİZE edilmek üzere açılacak.\n\nBu işlem mevcut arşivi silecektir. Dosya, ana ekrandaki 'Askıdaki Sevkiyatlar' (Yarım Kalanlar) listesine aktarılacak. Düzenleyip tekrar Tam Sevk yapabilirsiniz.\n\nEmin misiniz?", "Düzenleme Onayı", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);

                if (onay == DialogResult.Yes)
                {
                    try
                    {
                        string[] satirlar = File.ReadAllLines(dosyaYolu, System.Text.Encoding.UTF8);
                        if (satirlar.Length < 4) { MessageBox.Show("Dosya formatı bozuk!"); return; }

                        string[] huc = satirlar[1].Split(';');
                        string musteri = huc[0];
                        string sevkMusteri = huc[1];
                        string belgeler = huc[2];

                        YarimSevkiyatHafizasi hfz = new YarimSevkiyatHafizasi
                        {
                            MusteriAdi = musteri,
                            SevkMusteri = sevkMusteri,
                            BelgeNo = belgeler,
                            KayitTarihi = DateTime.Now,
                            AnaOkutulanlar = new Dictionary<string, int>(),
                            PaletMatrisiDurumu = new Dictionary<int, Dictionary<int, string>>(),
                            PaletBarkodlari = new Dictionary<string, string>()
                        };

                        List<string> paletListesi = new List<string>();
                        bool detayBasladi = false;

                        foreach (string l in satirlar)
                        {
                            if (l.Contains("--- DETAYLAR ---")) { detayBasladi = true; continue; }
                            if (detayBasladi && !l.StartsWith("Palet No") && !string.IsNullOrWhiteSpace(l))
                            {
                                string pAdi = l.Split(';')[0].Trim();
                                if (!paletListesi.Contains(pAdi)) paletListesi.Add(pAdi);
                            }
                        }
                        hfz.PaletSayisi = paletListesi.Count;

                        detayBasladi = false;
                        int rIdx = 0;
                        foreach (string l in satirlar)
                        {
                            if (l.Contains("--- DETAYLAR ---")) { detayBasladi = true; continue; }
                            if (detayBasladi && !l.StartsWith("Palet No") && !string.IsNullOrWhiteSpace(l))
                            {
                                string[] pCols = l.Split(';');
                                string pAdi = pCols[0].Trim();
                                string icerik = pCols[1].Trim();
                                string pBarkod = pCols[2].Trim();

                                if (!hfz.PaletBarkodlari.ContainsKey(pAdi)) hfz.PaletBarkodlari.Add(pAdi, pBarkod);

                                int cIdx = paletListesi.IndexOf(pAdi);

                                if (!hfz.PaletMatrisiDurumu.ContainsKey(rIdx)) hfz.PaletMatrisiDurumu[rIdx] = new Dictionary<int, string>();
                                hfz.PaletMatrisiDurumu[rIdx][cIdx] = icerik;

                                string[] adetBol = icerik.Split(new string[] { " | Adet: " }, StringSplitOptions.None);
                                if (adetBol.Length == 2)
                                {
                                    string urunKismi = adetBol[0];
                                    int adet = 0; int.TryParse(adetBol[1], out adet);

                                    string bNo = "";
                                    int pAc = urunKismi.LastIndexOf('('); int pKapa = urunKismi.LastIndexOf(')');
                                    if (pAc > 0 && pKapa > pAc)
                                    {
                                        bNo = urunKismi.Substring(pAc + 1, pKapa - pAc - 1).Trim();
                                        urunKismi = urunKismi.Substring(0, pAc).Trim();
                                    }

                                    string mKodu = urunKismi; string renk = "";
                                    int kAc = urunKismi.LastIndexOf('['); int kKapa = urunKismi.LastIndexOf(']');
                                    if (kAc > 0 && kKapa > kAc)
                                    {
                                        renk = urunKismi.Substring(kAc + 1, kKapa - kAc - 1).Trim();
                                        urunKismi = urunKismi.Substring(0, kAc).Trim();
                                    }

                                    int tire = urunKismi.IndexOf(" - ");
                                    if (tire > 0) mKodu = urunKismi.Substring(0, tire).Trim();
                                    else mKodu = urunKismi.Trim();

                                    string anahtar = $"{bNo}_{mKodu}_{renk}";
                                    if (!hfz.AnaOkutulanlar.ContainsKey(anahtar)) hfz.AnaOkutulanlar[anahtar] = adet;
                                    else hfz.AnaOkutulanlar[anahtar] += adet;
                                }
                                rIdx++;
                            }
                        }

                        string anaYol = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "TamgaApp Yarım Sevkiyatlar");
                        if (!Directory.Exists(anaYol)) Directory.CreateDirectory(anaYol);

                        string musteriTemiz = string.Join("_", hfz.MusteriAdi.Split(Path.GetInvalidFileNameChars()));
                        string yeniDosyaAdi = $"[REVİZE] {musteriTemiz} - {DateTime.Now:dd.MM.yyyy HH-mm-ss}.json";
                        string jsonYol = Path.Combine(anaYol, yeniDosyaAdi);

                        File.WriteAllText(jsonYol, Newtonsoft.Json.JsonConvert.SerializeObject(hfz, Newtonsoft.Json.Formatting.Indented));

                        string[] iptalEdilenBelgeler = belgeler.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (string b in iptalEdilenBelgeler) TamamlananBelgeNolar.Remove(b);

                        string txtYol = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TamgaApp", "KapananBelgeler.txt");
                        if (File.Exists(txtYol))
                        {
                            var guncelListe = File.ReadAllLines(txtYol).Where(x => !iptalEdilenBelgeler.Contains(x.Trim())).ToList();
                            File.WriteAllLines(txtYol, guncelListe);
                        }

                        File.Delete(dosyaYolu);

                        MessageBox.Show("Sevkiyat başarıyla 'Düzenleme (Revize)' moduna alındı!\n\nAna ekrandaki 'Askıdakileri Getir' butonundan çağırarak düzenleme yapabilirsiniz.", "Revize Modu", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        frmArsiv.Close();
                        btnSiparisYenile_Click(null, null);
                        btnYarimGetir_Click(null, null);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Düzenleme işlemi sırasında hata oluştu:\n" + ex.Message, "Kritik Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            };

            tvArsiv.AfterSelect += (treeSender, treeEv) =>
            {
                if (treeEv.Node.Tag != null && treeEv.Node.Tag.ToString().EndsWith(".csv"))
                {
                    aktifDosyaYolu = treeEv.Node.Tag.ToString();
                    aktifDosyaAdi = Path.GetFileNameWithoutExtension(aktifDosyaYolu);

                    DataTable dtPivot = new DataTable();
                    dtPivot.Columns.Add("Belge No", typeof(string));
                    dtPivot.Columns.Add("TOPLAM ADET", typeof(int));
                    dtPivot.Columns.Add("Malzeme Kodu", typeof(string));
                    dtPivot.Columns.Add("Malzeme Adı", typeof(string));

                    string[] csvSatirlar = File.ReadAllLines(aktifDosyaYolu, System.Text.Encoding.UTF8);
                    bool detaylarBasladi = false;

                    List<string> paletSutunlari = new List<string>();
                    var veriHavuzu = new Dictionary<string, Dictionary<string, int>>();

                    foreach (string satir in csvSatirlar)
                    {
                        if (satir.Contains("--- DETAYLAR ---")) { detaylarBasladi = true; continue; }
                        if (detaylarBasladi && !satir.StartsWith("Palet No") && !string.IsNullOrWhiteSpace(satir))
                        {
                            string[] huc = satir.Split(';');
                            if (huc.Length == 3 || huc.Length == 2)
                            {
                                string paletNo = huc[0].Trim();
                                string icerik = huc[1].Trim();

                                if (!paletSutunlari.Contains(paletNo))
                                {
                                    paletSutunlari.Add(paletNo);
                                    dtPivot.Columns.Add(paletNo, typeof(int));
                                }

                                string[] parcalar = icerik.Split(new string[] { "| Adet: " }, StringSplitOptions.None);
                                if (parcalar.Length == 2)
                                {
                                    string urunVeBelge = parcalar[0].Trim();
                                    int.TryParse(parcalar[1].Trim(), out int adet);

                                    string bNo = "BİLİNMEYEN";
                                    string malzemeKodu = urunVeBelge;
                                    string malzemeAdi = "";

                                    int sonParantezAc = urunVeBelge.LastIndexOf('(');
                                    int sonParantezKapat = urunVeBelge.LastIndexOf(')');
                                    if (sonParantezAc > 0 && sonParantezKapat > sonParantezAc)
                                    {
                                        bNo = urunVeBelge.Substring(sonParantezAc + 1, sonParantezKapat - sonParantezAc - 1).Trim();
                                        urunVeBelge = urunVeBelge.Substring(0, sonParantezAc).Trim();
                                    }

                                    int tireIndex = urunVeBelge.IndexOf(" - ");
                                    if (tireIndex > 0)
                                    {
                                        malzemeKodu = urunVeBelge.Substring(0, tireIndex).Trim();
                                        malzemeAdi = urunVeBelge.Substring(tireIndex + 3).Trim();
                                    }
                                    else { malzemeKodu = urunVeBelge; }

                                    string anahtar = $"{bNo}|||{malzemeKodu}|||{malzemeAdi}";

                                    if (!veriHavuzu.ContainsKey(anahtar)) veriHavuzu[anahtar] = new Dictionary<string, int>();

                                    if (veriHavuzu[anahtar].ContainsKey(paletNo)) veriHavuzu[anahtar][paletNo] += adet;
                                    else veriHavuzu[anahtar][paletNo] = adet;
                                }
                            }
                        }
                    }

                    foreach (var kvp in veriHavuzu)
                    {
                        string[] anahtarParcalar = kvp.Key.Split(new string[] { "|||" }, StringSplitOptions.None);
                        DataRow row = dtPivot.NewRow();
                        row["Belge No"] = anahtarParcalar[0];
                        row["Malzeme Kodu"] = anahtarParcalar[1];
                        row["Malzeme Adı"] = anahtarParcalar[2];

                        int genelToplam = 0;
                        foreach (var paletKvp in kvp.Value)
                        {
                            row[paletKvp.Key] = paletKvp.Value;
                            genelToplam += paletKvp.Value;
                        }
                        row["TOPLAM ADET"] = genelToplam;
                        dtPivot.Rows.Add(row);
                    }

                    dgvDetay.DataSource = dtPivot;
                }
            };

            btnYazdir.Click += async (printSender, printEv) =>
            {
                if (dgvDetay.Rows.Count == 0 || string.IsNullOrEmpty(aktifDosyaYolu)) { MessageBox.Show("Önce soldaki listeden bir rapor seçin!"); return; }

                System.Text.StringBuilder html = new System.Text.StringBuilder();
                html.AppendLine("<html><head><meta charset='utf-8'><style>");
                html.AppendLine("table { border-collapse: collapse; width: 100%; font-family: 'Segoe UI', Arial; font-size: 12px; }");
                html.AppendLine("th, td { border: 1px solid black; padding: 6px; text-align: center; }");
                html.AppendLine("th { background-color: #d9d9d9; font-weight: bold; }");
                html.AppendLine(".sol-hizala { text-align: left; }");
                html.AppendLine(".kalin { font-weight: bold; }");
                html.AppendLine("h2 { text-align: center; font-family: 'Segoe UI', Arial; margin-bottom: 20px; }");
                html.AppendLine("</style></head><body>");

                html.AppendLine($"<h2>{aktifDosyaAdi} - DETAY DÖKÜMÜ</h2>");

                html.AppendLine("<table><tr>");
                foreach (DataGridViewColumn col in dgvDetay.Columns) html.AppendLine($"<th>{col.HeaderText}</th>");
                html.AppendLine("</tr>");

                foreach (DataGridViewRow r in dgvDetay.Rows)
                {
                    if (!r.Visible) continue;
                    html.AppendLine("<tr>");
                    foreach (DataGridViewCell cell in r.Cells)
                    {
                        string sinif = "";
                        if (dgvDetay.Columns[cell.ColumnIndex].HeaderText == "Malzeme Adı") sinif = "class='sol-hizala'";
                        else if (dgvDetay.Columns[cell.ColumnIndex].HeaderText == "TOPLAM ADET" || dgvDetay.Columns[cell.ColumnIndex].HeaderText == "Belge No") sinif = "class='kalin'";

                        html.AppendLine($"<td {sinif}>{cell.Value}</td>");
                    }
                    html.AppendLine("</tr>");
                }
                html.AppendLine("</table></body></html>");

                Form frmYazdir = new Form { Text = "Rapor Yazdırılıyor...", Width = 1000, Height = 600, ShowIcon = false, StartPosition = FormStartPosition.CenterParent };
                Microsoft.Web.WebView2.WinForms.WebView2 web = new Microsoft.Web.WebView2.WinForms.WebView2 { Dock = DockStyle.Fill };
                frmYazdir.Controls.Add(web);
                frmYazdir.FormClosed += (formSender, formEv) => { web.Dispose(); };

                frmYazdir.Shown += async (formSender, formEv) =>
                {
                    string tempFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TamgaApp", "ArsivPrint");
                    var ozelHafiza = await Microsoft.Web.WebView2.Core.CoreWebView2Environment.CreateAsync(null, tempFolder);
                    await web.EnsureCoreWebView2Async(ozelHafiza);

                    web.NavigationCompleted += (webSender, navEv) => { web.CoreWebView2.ShowPrintUI(Microsoft.Web.WebView2.Core.CoreWebView2PrintDialogKind.Browser); };
                    web.NavigateToString(html.ToString());
                };

                frmYazdir.ShowDialog();
            };

            frmArsiv.ShowDialog();
        }

        #endregion

        #region 📊 10.4 EXCEL'DEN VERİTABANINA ÜRÜN AKTARIMI VE SİLME
        // Dışarıdan (Muhasebeden / ERP'den) gelen güncel barkodlu ürün listesini (Excel)
        // tek tıkla veritabanına almayı sağlar.
        private async void btnExcelAktar_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog { Filter = "Excel Dosyası|*.xlsx;*.xls" })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    // 1. ZIRH: Kullanıcı başka bir şeye tıklamasın diye ANA EKRANI KİLİTLE
                    this.Enabled = false;

                    // 2. ŞEKİLLİ ŞUKULLU "ELİT" LÜTFEN BEKLEYİN FORMU
                    Form progressForm = new Form
                    {
                        ControlBox = false,
                        StartPosition = FormStartPosition.CenterScreen,
                        Size = new Size(420, 130),
                        FormBorderStyle = FormBorderStyle.None, // Çirkin standart Windows çerçevesini sildik
                        BackColor = Color.FromArgb(212, 175, 55), // Dış çerçeve için Elit Gold (Altın) Rengi
                        Padding = new Padding(2), // 2 piksellik şık altın çerçeve efekti
                        ShowInTaskbar = false
                    };

                    // İç kısımdaki koyu yeşil arka plan paneli
                    Panel pnlIcerik = new Panel
                    {
                        Dock = DockStyle.Fill,
                        BackColor = Color.FromArgb(15, 76, 58) // Senin uygulamanın özel elit yeşil rengi
                    };

                    Label lblBaslik = new Label
                    {
                        Text = "VERİLER AKTARILIYOR",
                        Dock = DockStyle.Top,
                        Height = 50,
                        TextAlign = ContentAlignment.BottomCenter,
                        ForeColor = Color.FromArgb(212, 175, 55), // Altın rengi kalın başlık
                        Font = new Font("Segoe UI", 14, FontStyle.Bold)
                    };

                    Label lblAnimasyon = new Label
                    {
                        Text = "Excel işleniyor, lütfen bekleyiniz",
                        Dock = DockStyle.Fill,
                        TextAlign = ContentAlignment.MiddleCenter,
                        ForeColor = Color.White,
                        Font = new Font("Segoe UI", 11, FontStyle.Italic)
                    };

                    // Standart çubuk yerine modern metin animasyonu (Noktalar hareket eder)
                    System.Windows.Forms.Timer animTimer = new System.Windows.Forms.Timer { Interval = 400 };
                    int noktaCount = 0;
                    animTimer.Tick += (s, ev) =>
                    {
                        noktaCount = (noktaCount + 1) % 4;
                        lblAnimasyon.Text = "Excel işleniyor, lütfen bekleyiniz" + new string('.', noktaCount);
                    };
                    animTimer.Start();

                    // Form kapanırken arka plandaki saati hafızadan tertemiz sil
                    progressForm.FormClosing += (s, ev) => { animTimer.Stop(); animTimer.Dispose(); };

                    // Parçaları birleştirip ekrana basıyoruz
                    pnlIcerik.Controls.Add(lblAnimasyon);
                    pnlIcerik.Controls.Add(lblBaslik);
                    progressForm.Controls.Add(pnlIcerik);

                    progressForm.Show(this);

                    try
                    {
                        // 3. EXCEL'İ GÜVENLİ VE BAĞIMSIZ MOTÖRLE (EXCELDATAREADER) OKU
                        System.Data.DataTable dt = new System.Data.DataTable();

                        // Dosyayı kilitlemeden, sadece okuma modunda açıyoruz
                        using (var stream = File.Open(ofd.FileName, FileMode.Open, FileAccess.Read))
                        {
                            // Kütüphanenin Excel'i okuması için okuyucuyu başlatıyoruz
                            using (var reader = ExcelReaderFactory.CreateReader(stream))
                            {
                                // Excel'in ilk satırını Sütun Başlıkları (Header) olarak kabul et
                                var result = reader.AsDataSet(new ExcelDataSetConfiguration()
                                {
                                    ConfigureDataTable = (_) => new ExcelDataTableConfiguration()
                                    {
                                        UseHeaderRow = true
                                    }
                                });

                                // İlk sayfadaki (Sheet1) verileri DataTable'a aktar
                                dt = result.Tables[0];
                            }
                        }

                        // 4. AĞIR İŞİ (VERİTABANINA SATIR SATIR YAZMAYI) ARKA PLANA AT Kİ EKRAN DONMASIN!
                        await Task.Run(() =>
                        {
                            foreach (System.Data.DataRow row in dt.Rows)
                            {
                                // Ürün kodu boşsa o satırı atla
                                if (row[0] == DBNull.Value || string.IsNullOrWhiteSpace(row[0].ToString())) continue;

                                string renkGelen = row.ItemArray.Length > 5 ? row[5].ToString().Trim() : "";

                                if (string.IsNullOrEmpty(renkGelen))
                                {
                                    renkGelen = "Beyaz";
                                }

                                Urun yeniUrun = new Urun
                                {
                                    UrunKodu = row[0].ToString().Trim(),
                                    Aciklama = row.ItemArray.Length > 1 ? row[1].ToString().Trim() : "",
                                    IngilizceAciklama = row.ItemArray.Length > 2 ? row[2].ToString().Trim() : "",
                                    Barkod = row.ItemArray.Length > 3 ? row[3].ToString().Trim() : "",
                                    Renk = renkGelen
                                };

                                DataAccess.InsertUrun(yeniUrun);
                            }
                        });

                        MessageBox.Show("Aktarım başarıyla tamamlandı!", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Hata oluştu: " + ex.Message, "Kritik Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        // 5. İŞLEM BİTİNCE HER ŞEYİ NORMALE DÖNDÜR
                        progressForm.Close();
                        progressForm.Dispose();

                        this.Enabled = true; // ANA EKRANIN KİLİDİNİ AÇ!

                        // Listeyi yenile ki yeni gelen ürünler anında görünsün
                        if (btnBarkodVerileri != null) btnBarkodVerileri.PerformClick();
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
            var tabloVerisi = kullanicilar.Select(k => new
            {
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

        // Sol menüdeki kırmızı çıkış butonuna basıldığında
        private void btnCikisYap_Click(object sender, EventArgs e)
        {
            if (kapanisBasladi) return;

            DialogResult onay = MessageBox.Show(
                "Programı kapatmak istediğinize emin misiniz?\n\nKaydedilmemiş veya askıya alınmamış tüm verileriniz kaybolabilir!",
                "Çıkış Onayı",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);

            if (onay == DialogResult.Yes)
            {
                // 🌟 SADECE ANİMASYONU ÇAĞIRIYORUZ (Flag'i o kendi içinde yönetecek)
                CikisAnimasyonuVeKapat();
            }
        }

        // Ana formu gizleyip ekrana Splash (Veda) ekranını getiren ve 1.5 saniye sonra sistemi tamamen öldüren YENİ NESİL ASENKRON motor
        private async void CikisAnimasyonuVeKapat()
        {
            kapanisBasladi = true; // Kilit mekanizmasını aç (çifte kapanmayı önle)

            // 🌟 ZIRH: Eğer barkod okuyucu portu hala açıksa, önce bağlantıyı kopar ki cihaz kilitli kalmasın!
            try { if (barkodPort != null && barkodPort.IsOpen) barkodPort.Close(); } catch { }

            this.Hide(); // Ana program penceresini gizle

            // Veda animasyonlu SplashForm'u ekrana getir
            SplashForm vedaEkrani = new SplashForm
            {
                StartPosition = FormStartPosition.CenterScreen,
                TopMost = true // Veda ekranının arkada kalmasını engeller
            };
            vedaEkrani.Show();

            // 🌟 MODERNİZASYON ZIRHI: Eski Timer (Saatli bomba) yerine Yeni Nesil Asenkron Bekleme (Task.Delay)
            // Bu sayede programın arkasında çöp timer objeleri birikmez, UI (Arayüz) donmaz, işlemci yorulmaz!
            await System.Threading.Tasks.Task.Delay(1500);

            // Bütün arka plan işlemlerini, açık kalan diğer pencereleri ve programı kökten sonlandır
            Environment.Exit(0);
        }

        #endregion

        #region 🕵️ 11.5 YAZILIMSAL HATA DEDEKTİFİ
        // 🌟 YAZILIMSAL HATA DEDEKTİFİ (Arka plan işlemlerindeki gizli hataları ekrana basar)
        public void YazilimsalHataGoster(Exception ex, string islemAdi)
        {
            Form frmYazilimHatasi = new Form
            {
                Text = "🛠️ Yazılımsal İşlem Hatası",
                Size = new Size(600, 450),
                StartPosition = FormStartPosition.CenterScreen,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.WhiteSmoke,
                TopMost = true
            };

            Label lblUyari = new Label
            {
                Text = $"⚠️ '{islemAdi}' işlemi sırasında yazılımsal bir hata meydana geldi!",
                Location = new Point(15, 15),
                Size = new Size(550, 40),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.DarkOrange
            };

            TextBox txtHata = new TextBox
            {
                Text = $"HATA MESAJI:\r\n{ex.Message}\r\n\r\nKOD SATIRI (STACK TRACE):\r\n{ex.StackTrace}",
                Location = new Point(15, 60),
                Size = new Size(550, 280),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 9),
                BackColor = Color.Black,
                ForeColor = Color.Lime // Hacker ekranı gibi yeşil
            };

            Button btnTamam = new Button
            {
                Text = "Anladım",
                Location = new Point(445, 360),
                Size = new Size(120, 40),
                BackColor = Color.Teal,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };

            btnTamam.Click += (s, e) => frmYazilimHatasi.Close();

            frmYazilimHatasi.Controls.Add(lblUyari);
            frmYazilimHatasi.Controls.Add(txtHata);
            frmYazilimHatasi.Controls.Add(btnTamam);
            frmYazilimHatasi.ShowDialog();
        }
        #endregion

        #endregion

        // =========================================================================================

        #region 🚛 12. AMBAR ZARFI VE DESİ HESAPLAMA MOTORU

        #region 🧮 12.1 DESİ HESAPLAMA ÇEKİRDEĞİ
        // Kargo ve ambar standartlarına göre (En x Boy x Yükseklik / 3000) hacimsel ağırlık (Desi) hesaplar.
        // Hane (basamak) sayısı serbesttir, sadece 3 parça olması yeterlidir.
        public double DesiHesapla(string ebatMetni)
        {
            try
            {
                string temizMetin = ebatMetni.ToLower().Replace("x", "*").Replace("-", "*").Replace(" ", "*");
                string[] carpanlar = temizMetin.Split(new char[] { '*' }, StringSplitOptions.RemoveEmptyEntries);

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
            dgvPaletler.Columns[0].Name = "Palet No"; dgvPaletler.Columns[0].ReadOnly = false; // 🌟 KİLİT AÇILDI! Artık "1. Kasa" vs. yazılabilir
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

        // 🌟 SAĞ TIK MENÜSÜ (GERİ AL, DESİ DÜZENLE VE ADRES DÜZENLE MOTORU)
        private void dgvAmbarSonListe_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            // Sadece sağ tıka ve geçerli bir satıra (başlıklara değil) basıldıysa çalış
            if (e.Button == MouseButtons.Right && e.RowIndex >= 0)
            {
                // Tıklanan satırı otomatik olarak seçili hale getir (Maviye boya)
                dgvAmbarSonListe.ClearSelection();
                dgvAmbarSonListe.Rows[e.RowIndex].Selected = true;

                // Şık bir sağ tık menüsü (Context Menu) oluştur
                ContextMenuStrip sagTikMenu = new ContextMenuStrip();
                sagTikMenu.Font = new Font("Segoe UI", 10, FontStyle.Bold);

                // =========================================================
                // 1. SEÇENEK: PALET VE DESİ DÜZENLE (GERİ ÇEKME MOTORU)
                // =========================================================
                ToolStripMenuItem btnDesiDuzenle = new ToolStripMenuItem("✏️ Düzenlemek İçin Geri Çek");
                btnDesiDuzenle.Click += (s, ev) =>
                {
                    var row = dgvAmbarSonListe.Rows[e.RowIndex];

                    // Son listedeki verileri hafızaya al
                    string paletSayisiStr = row.Cells[6].Value?.ToString().Trim() ?? "";
                    string olculerHam = row.Cells[7].Value?.ToString() ?? "";
                    string firmaAdi = row.Cells[1].Value?.ToString() ?? "";

                    // 1. Adım: Palet Sayısını ComboBox'ta seç ve orta tabloyu tetikle
                    if (cmbPaletSayisi.Items.Contains(paletSayisiStr))
                    {
                        cmbPaletSayisi.SelectedItem = paletSayisiStr; // Bu satır dgvPaletler'de boş satırları anında oluşturur!
                    }
                    else
                    {
                        MessageBox.Show("Geçerli bir palet sayısı bulunamadı!", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    // 2. Adım: Orijinal ölçüleri parçala ve orta tabloya (dgvPaletler) geri doldur
                    string[] satirlar = olculerHam.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

                    for (int i = 0; i < satirlar.Length && i < dgvPaletler.Rows.Count; i++)
                    {
                        string satir = satirlar[i]; // Örn: "1. KASA: 80*120*150 (120 Ds.)"
                        int colonIndex = satir.IndexOf(":");
                        if (colonIndex > 0)
                        {
                            string ozelIsim = satir.Substring(0, colonIndex).Trim(); // "1. KASA"
                            string kalan = satir.Substring(colonIndex + 1).Trim();   // "80*120*150 (120 Ds.)"

                            int parantezIndex = kalan.IndexOf("(");
                            string ebat = kalan;
                            string desi = "0 Ds.";

                            if (parantezIndex > 0)
                            {
                                ebat = kalan.Substring(0, parantezIndex).Trim(); // "80*120*150"
                                desi = kalan.Substring(parantezIndex + 1).Replace(")", "").Trim(); // "120 Ds."
                            }

                            dgvPaletler.Rows[i].Cells[0].Value = ozelIsim;
                            dgvPaletler.Rows[i].Cells[1].Value = ebat;
                            dgvPaletler.Rows[i].Cells[2].Value = desi;
                        }
                    }

                    // 3. Adım: İşlemi kolaylaştırmak için Seçilen Firmalar listesinde o firmayı bul ve maviyle seç
                    foreach (DataGridViewRow secilenRow in dgvAmbarSecilenFirmalar.Rows)
                    {
                        if (secilenRow.Cells[1].Value != null && secilenRow.Cells[1].Value.ToString() == firmaAdi)
                        {
                            dgvAmbarSecilenFirmalar.ClearSelection();
                            secilenRow.Selected = true;
                            break;
                        }
                    }

                    // 4. Adım: Alt listeden (dgvAmbarSonListe) bu satırı UÇUR
                    dgvAmbarSonListe.Rows.RemoveAt(e.RowIndex);

                    MessageBox.Show("Kayıt masaya geri çekildi!\n\nOrta tablodan ölçüleri yeniden düzenleyip 'Listeye Ekle' butonuna basarak işleminizi tamamlayabilirsiniz.", "Düzenleme Modu", MessageBoxButtons.OK, MessageBoxIcon.Information);
                };

                // =========================================================
                // 2. SEÇENEK: ADRES VE İLETİŞİM DÜZENLE
                // =========================================================
                ToolStripMenuItem btnAdresDuzenle = new ToolStripMenuItem("🏠 Adres ve İletişim Düzenle");
                btnAdresDuzenle.ForeColor = Color.DarkBlue;
                btnAdresDuzenle.Click += (s, ev) =>
                {
                    var satir = dgvAmbarSonListe.Rows[e.RowIndex];

                    Form frmAdres = new Form
                    {
                        Width = 400,
                        Height = 380,
                        Text = "Adres ve İletişim Düzenle",
                        StartPosition = FormStartPosition.CenterParent,
                        FormBorderStyle = FormBorderStyle.FixedDialog,
                        MaximizeBox = false,
                        MinimizeBox = false,
                        BackColor = Color.WhiteSmoke,
                        ShowIcon = false
                    };

                    Label lbl1 = new Label { Text = "Firma Adı:", Left = 20, Top = 20, Width = 100, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
                    TextBox txtFirma = new TextBox { Left = 120, Top = 18, Width = 240, Text = satir.Cells[1].Value?.ToString() };

                    Label lbl2 = new Label { Text = "Adres:", Left = 20, Top = 60, Width = 100, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
                    TextBox txtAdres = new TextBox { Left = 120, Top = 58, Width = 240, Height = 60, Multiline = true, Text = satir.Cells[2].Value?.ToString() };

                    Label lbl3 = new Label { Text = "İl / İlçe:", Left = 20, Top = 140, Width = 100, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
                    TextBox txtIl = new TextBox { Left = 120, Top = 138, Width = 240, Text = satir.Cells[3].Value?.ToString() };

                    Label lbl4 = new Label { Text = "Telefon 1:", Left = 20, Top = 180, Width = 100, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
                    TextBox txtTel1 = new TextBox { Left = 120, Top = 178, Width = 240, Text = satir.Cells[4].Value?.ToString() };

                    Label lbl5 = new Label { Text = "Telefon 2:", Left = 20, Top = 220, Width = 100, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
                    TextBox txtTel2 = new TextBox { Left = 120, Top = 218, Width = 240, Text = satir.Cells[5].Value?.ToString() };

                    Button btnAdresOnay = new Button { Text = "✅ BİLGİLERİ GÜNCELLE", Left = 20, Top = 270, Width = 340, Height = 45, BackColor = Color.Teal, ForeColor = Color.White, Font = new Font("Segoe UI", 10, FontStyle.Bold), Cursor = Cursors.Hand };

                    btnAdresOnay.Click += (senderObj, args) =>
                    {
                        satir.Cells[1].Value = txtFirma.Text.Trim();
                        satir.Cells[2].Value = txtAdres.Text.Trim();
                        satir.Cells[3].Value = txtIl.Text.Trim();
                        satir.Cells[4].Value = txtTel1.Text.Trim();
                        satir.Cells[5].Value = txtTel2.Text.Trim();

                        frmAdres.Close();
                    };

                    frmAdres.Controls.Add(lbl1); frmAdres.Controls.Add(txtFirma);
                    frmAdres.Controls.Add(lbl2); frmAdres.Controls.Add(txtAdres);
                    frmAdres.Controls.Add(lbl3); frmAdres.Controls.Add(txtIl);
                    frmAdres.Controls.Add(lbl4); frmAdres.Controls.Add(txtTel1);
                    frmAdres.Controls.Add(lbl5); frmAdres.Controls.Add(txtTel2);
                    frmAdres.Controls.Add(btnAdresOnay);

                    frmAdres.ShowDialog();
                };

                // =========================================================
                // 3. SEÇENEK: SİL (GERİ AL)
                // =========================================================
                ToolStripMenuItem btnSil = new ToolStripMenuItem("❌ Seçili Satırı Sil (Geri Al)");
                btnSil.ForeColor = Color.DarkRed;
                btnSil.Click += (s, ev) =>
                {
                    // Sistemde zaten var olan Silme butonunun komutunu gizlice tetikliyoruz
                    btnAmbarSil_Click(null, null);
                };

                // Menüye butonları ekle ve araya şık ayırıcı çizgiler koy
                sagTikMenu.Items.Add(btnDesiDuzenle);
                sagTikMenu.Items.Add(btnAdresDuzenle);
                sagTikMenu.Items.Add(new ToolStripSeparator());
                sagTikMenu.Items.Add(btnSil);

                // 🌟 ÇÖKMEYİ ÖNLEYEN ZIRH (Gecikmeli Silme)
                sagTikMenu.Closed += (senderMenu, argsMenu) => { this.BeginInvoke(new Action(() => sagTikMenu.Dispose())); };

                // Menüyü farenin tam ucunda (tıklanan yerde) göster
                sagTikMenu.Show(Cursor.Position);
            }
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
                dgvPaletler.Columns[0].Name = "Palet No"; dgvPaletler.Columns[0].ReadOnly = false; // 🌟 KİLİT AÇILDI!
                dgvPaletler.Columns[1].Name = "Ebatlar (En*Boy*Yük)";
                dgvPaletler.Columns[2].Name = "Desi"; dgvPaletler.Columns[2].ReadOnly = true;
                dgvPaletler.AllowUserToAddRows = false;
            }

            dgvPaletler.Rows.Clear(); // Önceki seçimleri temizle

            if (int.TryParse(cmbPaletSayisi.Text, out int paletSayisi))
            {
                for (int i = 1; i <= paletSayisi; i++)
                {
                    // Varsayılan olarak Palet yazar ama sen üstüne tıklayıp Kasa veya Koli yapabilirsin
                    dgvPaletler.Rows.Add($"{i}. PALET", "", "0 Ds.");
                }
            }
        }

        // Kullanıcı "Ebatlar" hücresine veri girdiğinde veya değiştirdiğinde anında tetiklenir ve Desi'yi hesaplar.
        private void dgvPaletler_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            
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
                // 🌟 KULLANICININ YAZDIĞI ÖZEL İSMİ (KASA, PARÇA VB.) ÇEK
                string ozelIsim = prow.Cells[0].Value?.ToString() ?? "";

                string ebat = prow.Cells[1].Value?.ToString() ?? "";
                string desiMetni = prow.Cells[2].Value?.ToString() ?? "0 Ds.";

                // Özel isimle birlikte listeye ekle
                olculerListesi.Add($"{ozelIsim}: {ebat} ({desiMetni})"); // Örn: 1. KASA: 80*120*150 (120 Ds.)
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
        // En alttaki listede biriken firmaları ve ebatları standart bir DL Zarfa HTML/Edge Motoru ile yazdırır.

        private async void btnAmbarYazdir_Click(object sender, EventArgs e)
        {
            if (dgvAmbarSonListe.Rows.Count == 0)
            {
                MessageBox.Show("DUR! Yazdırılacak hiç palet/firma yok.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 1. HTML İSKELETİ VE CSS AYARLARI
            System.Text.StringBuilder htmlBuilder = new System.Text.StringBuilder();
            htmlBuilder.Append(@"
    <html>
    <head>
        <style>
            @page { margin: 0; }
            @media print {
                body { margin: 1cm; }
                .sayfa-kes { page-break-after: always; } 
            }
            
            body { font-family: 'Segoe UI', Tahoma, Verdana, sans-serif; }
            .dis-cerceve { 
                border: 2px solid black; 
                width: 100%; 
                max-width: 850px; 
                display: flex; 
                margin: 20px auto;
            }
            .sol-kutu, .sag-kutu { 
                width: 50%; 
                padding: 15px; 
                text-align: center; 
            }
            .sol-kutu { border-right: 2px solid black; }
            .baslik { font-size: 20px; font-weight: bold; border-bottom: 2px solid black; padding-bottom: 10px; margin-bottom: 20px; }
            .veri { font-size: 15px; font-weight: bold; line-height: 1.5; }
            
            /* 🌟 YENİ: Ölçülerin ip gibi düzgün hizalanması için */
            .olcu-listesi { display: inline-block; text-align: left; }
        </style>
    </head>
    <body>");

            // 2. TABLODAKİ VERİLERİ DÖNGÜYLE HTML'E EKLE
            foreach (DataGridViewRow row in dgvAmbarSonListe.Rows)
            {
                if (row.IsNewRow) continue;

                string firmaAdi = row.Cells[1].Value?.ToString() ?? "";
                string adres = row.Cells[2].Value?.ToString() ?? "";
                string il = row.Cells[3].Value?.ToString() ?? "";
                string tel1 = row.Cells[4].Value?.ToString() ?? "";
                string tel2 = row.Cells[5].Value?.ToString() ?? "";
                string paletSayisi = row.Cells[6].Value?.ToString() ?? "";

                string telefonlar = tel1;
                if (!string.IsNullOrWhiteSpace(tel2)) telefonlar += "<br>" + tel2;

                string olculerHam = row.Cells[7].Value?.ToString() ?? "";
                string olculer = olculerHam.Replace(")", ")<br>");
                string toplamDesi = row.Cells[8].Value?.ToString() ?? "";

                // 🌟 SİHİRLİ AMBALAJ SAYACI MOTORU 🌟
                Dictionary<string, int> ambalajTurleri = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                string[] satirlar = olculerHam.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string satir in satirlar)
                {
                    int colonIndex = satir.IndexOf(":");
                    if (colonIndex > 0)
                    {
                        string isimKisimi = satir.Substring(0, colonIndex).ToUpper(); // Örn: "1.KASA" veya "2. PALET"

                        // İçindeki rakamları ve noktaları sil, sadece harfleri (KASA, PALET vb.) cımbızla
                        string tur = new string(isimKisimi.Where(char.IsLetter).ToArray()).Trim();
                        if (string.IsNullOrWhiteSpace(tur)) tur = "PALET"; // Harf bulamazsa varsayılan PALET

                        if (ambalajTurleri.ContainsKey(tur)) ambalajTurleri[tur]++;
                        else ambalajTurleri.Add(tur, 1);
                    }
                }

                // Sayılanları "1 PALET + 1 KASA" şeklinde birleştir
                List<string> ozetListesi = new List<string>();
                foreach (var kvp in ambalajTurleri)
                {
                    ozetListesi.Add($"{kvp.Value} {kvp.Key}");
                }

                string dinamikToplamText = string.Join(" + ", ozetListesi);
                if (string.IsNullOrWhiteSpace(dinamikToplamText)) dinamikToplamText = paletSayisi + " PALET";

                // HTML Tasarımına Ekle
                htmlBuilder.Append($@"
        <div class='dis-cerceve sayfa-kes'>
            <div class='sol-kutu'>
                <div class='baslik'>ADRES</div>
                <div class='veri'>
                    {firmaAdi}<br>
                    {adres}<br>
                    {il}<br>
                    {telefonlar}
                </div>
            </div>
            <div class='sag-kutu'>
                <div class='baslik'>PALET ÖLÇÜLERİ</div>
                <div class='veri'>
                    <br>
                    <div class='olcu-listesi'>{olculer}</div><br>
                    ----------------------<br>
                    Genel Toplam: {toplamDesi}<br><br>
                    TOPLAM: {dinamikToplamText}
                </div>
            </div>
        </div>");
            }

            htmlBuilder.Append(@"
    </body>
    </html>");

            // 3. YAZDIRMA ALANI PENCERESİNİ DÜZENLE 
            Form modernOnizleme = new Form();
            modernOnizleme.Text = "Yazdırma Alanı";
            modernOnizleme.ShowIcon = false;
            modernOnizleme.Width = 1000;
            modernOnizleme.Height = 600;
            modernOnizleme.StartPosition = FormStartPosition.CenterScreen;

            Microsoft.Web.WebView2.WinForms.WebView2 webCizici = new Microsoft.Web.WebView2.WinForms.WebView2();
            webCizici.Dock = DockStyle.Fill;
            modernOnizleme.Controls.Add(webCizici);

            modernOnizleme.FormClosed += (s, ev) =>
            {
                webCizici.Dispose();
            };

            modernOnizleme.Show();

            string appDataYolu = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string zarfHafizaYolu = System.IO.Path.Combine(appDataYolu, "TamgaApp", "Profil_CokluZarf");

            var ozelHafiza = await Microsoft.Web.WebView2.Core.CoreWebView2Environment.CreateAsync(null, zarfHafizaYolu);

            await webCizici.EnsureCoreWebView2Async(ozelHafiza);

            webCizici.NavigateToString(htmlBuilder.ToString());

            webCizici.NavigationCompleted += (s, args) =>
            {
                webCizici.CoreWebView2.ShowPrintUI(Microsoft.Web.WebView2.Core.CoreWebView2PrintDialogKind.Browser);
            };
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

        // 🌟 SİHİRLİ BOYA MOTORU (Sıralama yapıldığında renklerin kaybolmasını %100 engeller)
        private void DgvMalzemeler_Renklendir(object sender, EventArgs e)
        {
            if (dgvMalzemeler == null) return;
            foreach (DataGridViewRow row in dgvMalzemeler.Rows)
            {
                if (row.IsNewRow) continue;
                int sip = 0, oku = 0;

                if (row.Cells["Sipariş Adedi"].Value != null) int.TryParse(row.Cells["Sipariş Adedi"].Value.ToString(), out sip);
                if (row.Cells["Okutulan"].Value != null) int.TryParse(row.Cells["Okutulan"].Value.ToString(), out oku);

                if (oku >= sip && sip > 0) row.DefaultCellStyle.BackColor = Color.LightGreen;
                else if (oku > 0) row.DefaultCellStyle.BackColor = Color.LightYellow;
                else row.DefaultCellStyle.BackColor = Color.White;
            }
        }

        // Kullanıcı bir "Müşteri" seçtiğinde, sadece o müşteriye ait Belge Numaralarını altındaki kutuya doldurur.
        private void cmbMusteri_SelectedIndexChanged(object sender, EventArgs e)
        {

            // 🌟 MÜŞTERİ SEÇİLİR SEÇİLMEZ PALET SAYISINI OTOMATİK "1" YAP
            if (cmbSevkPaletSayisi.Items.Count > 0)
            {
                cmbSevkPaletSayisi.SelectedIndex = 0; // Listenin en başındaki (1) değerini seçer
                // veya alternatif garanti yöntem: cmbSevkPaletSayisi.SelectedItem = "1";
            }

            if (cmbMusteri.SelectedItem == null) return;

            string secilenMusteri = cmbMusteri.SelectedItem.ToString().Trim();

            // Eski ComboBox yerine artık yeni Tikli Liste (CheckedListBox) kullanıyoruz
            clbBelgeNo.Items.Clear();
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
                clbBelgeNo.Items.AddRange(belgeler.ToArray());

                // İstersen ilk gelen belgeye otomatik olarak tik atmasını sağlayabilirsin:
                // clbBelgeNo.SetItemChecked(0, true);
            }
        }

        // 🌟 SEVK BEKLET MOTORU (Üstüne Yazmayı Engelleyen Zırhlı Versiyon)
        private void btnSevkBeklet_Click(object sender, EventArgs e)
        {
            // 🌟 ZIRH: ASKIYA ALMADAN ÖNCE ETİKETSİZ (BARKODSUZ) PALET KONTROLÜ
            dgvPaletMatrisi.EndEdit();
            List<string> etiketsizPaletler = new List<string>();

            // Matristeki (Kamyondaki) tüm paletleri tek tek kontrol et
            for (int j = 0; j < dgvPaletMatrisi.Columns.Count; j++)
            {
                string pAdi = dgvPaletMatrisi.Columns[j].HeaderText;
                bool paletDoluMu = false;

                // Paletin içine ürün konmuş mu? (Boş palet için uyarı vermeyelim)
                foreach (DataGridViewRow row in dgvPaletMatrisi.Rows)
                {
                    if (row.Cells[j].Value != null && !string.IsNullOrWhiteSpace(row.Cells[j].Value.ToString()))
                    {
                        paletDoluMu = true;
                        break;
                    }
                }

                // Eğer palet doluysa AMA hafızada (aktifPaletBarkodlari) barkodu üretilmemişse (yani Yazdır'a basılmamışsa)
                if (paletDoluMu && !aktifPaletBarkodlari.ContainsKey(pAdi))
                {
                    etiketsizPaletler.Add(pAdi);
                }
            }

            // Eğer etiketi basılmamış dolu paletler varsa uyar!
            if (etiketsizPaletler.Count > 0)
            {
                HataSesCal();
                DialogResult cevap = MessageBox.Show(
                    "DİKKAT! Aşağıdaki paletlerin etiketini (EAN-13) henüz YAZDIRMADINIZ:\n\n👉 " +
                    string.Join("\n👉 ", etiketsizPaletler) +
                    "\n\nFiziksel paletlerin sahada isimsiz kalıp kaybolmaması için önce 'Etiket Yazdır' yapmanız tavsiye edilir.\n\nYine de etiketsiz olarak ASKIYA ALMAK istiyor musunuz?",
                    "Etiketi Basılmamış Palet Uyarısı",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2); // Yanlışlıkla basmasın diye 'Hayır'ı seçili getiririz

                if (cevap == DialogResult.No)
                {
                    return; // 🛑 İşlemi iptal et, kullanıcı gidip etiketleri yazdırsın!
                }
            }

            if (clbBelgeNo.CheckedItems.Count == 0 || dgvMalzemeler.Rows.Count == 0)
            {
                MessageBox.Show("Beklemeye alınacak açık bir sevkiyat yok!", "Hata"); return;
            }

            YarimSevkiyatHafizasi hafiza = new YarimSevkiyatHafizasi
            {
                MusteriAdi = txtMusteriAdi.Text,
                BelgeNo = string.Join(", ", clbBelgeNo.CheckedItems.Cast<string>()),
                SevkMusteri = txtSevkMusteri.Text,
                PaletSayisi = cmbSevkPaletSayisi.SelectedIndex != -1 ? Convert.ToInt32(cmbSevkPaletSayisi.SelectedItem) : 0,
                KayitTarihi = DateTime.Now
            };

            foreach (DataGridViewRow row in dgvMalzemeler.Rows)
            {
                if (row.IsNewRow || row.Cells["Malzeme Kodu"].Value == null) continue;

                string belgeNo = row.Cells["Belge No"].Value?.ToString() ?? "";
                string malzemeKodu = row.Cells["Malzeme Kodu"].Value.ToString();
                string aciklama = row.Cells["Açıklama"].Value?.ToString() ?? "";

                string benzersizAnahtar = $"{belgeNo}_{malzemeKodu}_{aciklama}";

                int okutulan = 0;
                if (row.Cells["Okutulan"].Value != null) int.TryParse(row.Cells["Okutulan"].Value.ToString(), out okutulan);

                if (!hafiza.AnaOkutulanlar.ContainsKey(benzersizAnahtar))
                    hafiza.AnaOkutulanlar.Add(benzersizAnahtar, okutulan);
                else
                    hafiza.AnaOkutulanlar[benzersizAnahtar] += okutulan;
            }

            for (int i = 0; i < dgvPaletMatrisi.Rows.Count; i++)
            {
                hafiza.PaletMatrisiDurumu[i] = new Dictionary<int, string>();
                for (int j = 0; j < dgvPaletMatrisi.Columns.Count; j++)
                {
                    if (dgvPaletMatrisi.Rows[i].Cells[j].Value != null)
                        hafiza.PaletMatrisiDurumu[i][j] = dgvPaletMatrisi.Rows[i].Cells[j].Value.ToString();
                }
            }

            hafiza.PaletBarkodlari = aktifPaletBarkodlari;

            string anaYol = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "TamgaApp Yarım Sevkiyatlar");
            if (!Directory.Exists(anaYol)) Directory.CreateDirectory(anaYol);

            string musteriTemiz = string.Join("_", txtMusteriAdi.Text.Split(Path.GetInvalidFileNameChars()));
            if (string.IsNullOrWhiteSpace(musteriTemiz)) musteriTemiz = "BelirtilmeyenFirma";

            // 🌟 SİHİRLİ ZIRH (Saniye + Sayaç ile üstüne yazmayı %100 engeller)
            string zamanDamgasi = DateTime.Now.ToString("dd.MM.yyyy HH-mm-ss");
            string dosyaAdi = $"[BEKLET] {musteriTemiz} - {zamanDamgasi}.json";
            string tamYol = Path.Combine(anaYol, dosyaAdi);

            int sayac = 1;
            while (System.IO.File.Exists(tamYol))
            {
                dosyaAdi = $"[BEKLET] {musteriTemiz} - {zamanDamgasi} ({sayac}).json";
                tamYol = Path.Combine(anaYol, dosyaAdi);
                sayac++;
            }

            System.IO.File.WriteAllText(tamYol, Newtonsoft.Json.JsonConvert.SerializeObject(hafiza, Newtonsoft.Json.Formatting.Indented));

            MessageBox.Show($"Sevkiyat başarıyla BEKLEMEYE ALINDI!", "Kaydedildi", MessageBoxButtons.OK, MessageBoxIcon.Information);

            txtMusteriAdi.Clear();
            txtSevkMusteri.Clear();
            txtBarkod.Clear();
            clbBelgeNo.Items.Clear();
            cmbSevkPaletSayisi.SelectedIndex = -1;

            if (dgvMalzemeler.DataSource == null) dgvMalzemeler.Rows.Clear();
            else dgvMalzemeler.DataSource = null;

            dgvPaletMatrisi.Columns.Clear();
            dgvPaletMatrisi.Rows.Clear();
            cmbAktifPalet.Items.Clear();

            aktifPaletBarkodlari.Clear();
            btnYarimGetir_Click(null, null); // Listeyi anında yenile
            KarantinayaAl(false);
        }

        private void btnSevkAra_Click(object sender, EventArgs e)
        {
            if (clbBelgeNo.CheckedItems.Count == 0)
            {
                MessageBox.Show("Lütfen listelemek için en az bir Belge No işaretleyin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning); return;
            }

            Dictionary<string, int> oncekiOkutulanlar = new Dictionary<string, int>();
            if (dgvMalzemeler.Rows.Count > 0)
            {
                foreach (DataGridViewRow row in dgvMalzemeler.Rows)
                {
                    if (row.IsNewRow || row.Cells["Malzeme Kodu"].Value == null) continue;

                    string bNo = row.Cells["Belge No"].Value.ToString();
                    string mKodu = row.Cells["Malzeme Kodu"].Value.ToString();
                    string aciklama = row.Cells["Açıklama"].Value?.ToString() ?? "";

                    // 🌟 ZIRH: Sipariş adedine bakmadan renk ve belge ile havuz oluşturur!
                    string anahtar = $"{bNo}_{mKodu}_{aciklama}";

                    int okutulan = 0;
                    if (row.Cells["Okutulan"].Value != null) int.TryParse(row.Cells["Okutulan"].Value.ToString(), out okutulan);

                    if (okutulan > 0)
                    {
                        if (!oncekiOkutulanlar.ContainsKey(anahtar)) oncekiOkutulanlar[anahtar] = okutulan;
                        else oncekiOkutulanlar[anahtar] += okutulan;
                    }
                }
            }

            DataTable dtEkran = new DataTable();
            dtEkran.Columns.Add("Belge No", typeof(string));
            dtEkran.Columns.Add("Malzeme Kodu", typeof(string));
            dtEkran.Columns.Add("Barkod", typeof(string));
            dtEkran.Columns.Add("Malzeme Adı", typeof(string));
            dtEkran.Columns.Add("Açıklama", typeof(string));
            dtEkran.Columns.Add("Sipariş Adedi", typeof(int));
            dtEkran.Columns.Add("Okutulan", typeof(int));

            var yerelUrunler = DataAccess.GetAllUrunler();
            string ilkBelge = clbBelgeNo.CheckedItems[0].ToString();
            DataRow[] ilkBelgeSatirlari = dtTumSiparisler.Select($"BelgeNo LIKE '%{ilkBelge}%'");

            if (ilkBelgeSatirlari.Length > 0)
            {
                txtMusteriAdi.Text = ilkBelgeSatirlari[0]["MusteriAdi"].ToString().Trim();
                txtSevkMusteri.Text = ilkBelgeSatirlari[0]["SevkMusteri"].ToString().Trim();
            }

            foreach (var isaretliBelge in clbBelgeNo.CheckedItems)
            {
                string secilenBelge = isaretliBelge.ToString();
                DataRow[] filtrelenmisSatirlar = dtTumSiparisler.Select($"BelgeNo LIKE '%{secilenBelge}%'");

                foreach (DataRow satir in filtrelenmisSatirlar)
                {
                    string malzemeKodu = satir["Malzeme"].ToString().Trim();
                    string siparisRengi = satir["SecenekAciklamasi"].ToString().Trim();

                    var urun = yerelUrunler.FirstOrDefault(u => u.UrunKodu == malzemeKodu &&
                        ((siparisRengi.IndexOf("Beyaz", StringComparison.OrdinalIgnoreCase) >= 0 && string.IsNullOrWhiteSpace(u.Renk)) ||
                        u.Renk.Equals(siparisRengi, StringComparison.OrdinalIgnoreCase) ||
                        (!string.IsNullOrWhiteSpace(u.Renk) && siparisRengi.IndexOf(u.Renk.Replace("2K-", "").Replace("2N-", "").Replace("2L-", "").Replace("2C-", "").Trim(), StringComparison.OrdinalIgnoreCase) >= 0)));

                    if (urun == null) urun = yerelUrunler.FirstOrDefault(u => u.UrunKodu == malzemeKodu);

                    string barkod = urun != null && !string.IsNullOrWhiteSpace(urun.Barkod) ? urun.Barkod : "BARKOD YOK";

                    int sAdet = Convert.ToInt32(Convert.ToDecimal(satir["Bakiye"]));
                    int yazilacakOkutulan = 0;

                    // 🌟 KUSURSUZ ŞELALE DAĞITIMI
                    string anahtar = $"{secilenBelge}_{malzemeKodu}_{siparisRengi}";

                    if (oncekiOkutulanlar.ContainsKey(anahtar) && oncekiOkutulanlar[anahtar] > 0)
                    {
                        yazilacakOkutulan = Math.Min(sAdet, oncekiOkutulanlar[anahtar]);
                        oncekiOkutulanlar[anahtar] -= yazilacakOkutulan; // Havuzdan düş ki diğer satıra kalsın
                    }

                    dtEkran.Rows.Add(secilenBelge, malzemeKodu, barkod, satir["MalzemeAdi"].ToString().Trim(), siparisRengi, sAdet, yazilacakOkutulan);
                }
            }

            dgvMalzemeler.DataSource = dtEkran;
            dgvMalzemeler.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            DgvMalzemeler_Renklendir(null, null); // Renkleri zorla çalıştır

            if (sender != null) MessageBox.Show($"{clbBelgeNo.CheckedItems.Count} adet sipariş fişi eklendi!\nÖnceki okutulan ürünleriniz başarıyla korundu.", "Siparişler Hazır", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        #endregion

        #region 🔫 13.5 SEVKİYAT BARKOD OKUTMA VE PALETLEME

        // 🔒 BARKOD KİLİDİ (ODAK MODU) MOTORU
        private bool barkodModuAktif = false;

        private void btnBarkodKilidi_Click(object sender, EventArgs e)
        {
            btnBarkodKilidi.TabStop = false;
            barkodModuAktif = !barkodModuAktif;

            void KontrolleriKilitle(Control parent)
            {
                foreach (Control ctrl in parent.Controls)
                {
                    // 🌟 DOKUNULMAZLAR LİSTESİ: Barkod kutusu, Kilit Butonu, ÖNCEKİ ve SONRAKİ butonları ASLA pasif olmaz!
                    if (ctrl.Name != "txtBarkod" &&
                        ctrl.Name != "btnBarkodKilidi" &&
                        ctrl.Name != "btnOncekiPalet" &&
                        ctrl.Name != "btnSonrakiPalet")
                    {
                        if (ctrl is Button || ctrl is ComboBox || ctrl is CheckedListBox || ctrl is TextBox)
                        {
                            ctrl.Enabled = !barkodModuAktif;

                            if (ctrl is Button)
                            {
                                ctrl.TabStop = false;
                            }
                        }
                    }

                    if (ctrl.Controls.Count > 0)
                    {
                        KontrolleriKilitle(ctrl);
                    }
                }
            }

            TabPage aktifSekme = tabControl1.SelectedTab;
            if (aktifSekme != null) KontrolleriKilitle(aktifSekme);

            if (barkodModuAktif)
            {
                btnBarkodKilidi.Text = "🔒 BARKOD MODU AÇIK (EKRAN KİLİTLİ)";
                btnBarkodKilidi.BackColor = Color.MediumSeaGreen;
                btnBarkodKilidi.ForeColor = Color.White;
            }
            else
            {
                btnBarkodKilidi.Text = "🔓 BARKOD MODUNU AÇ";
                btnBarkodKilidi.BackColor = Color.Orange;
                btnBarkodKilidi.ForeColor = Color.Black;
            }

            if (txtBarkod != null && txtBarkod.Enabled) txtBarkod.Focus();
        }

        // ⬅️ ÖNCEKİ PALET BUTONU
        private void btnOncekiPalet_Click(object sender, EventArgs e)
        {
            btnOncekiPalet.TabStop = false; // Tab tuşu ile gelinmesini kesin olarak yasakla

            if (cmbAktifPalet.Items.Count > 0 && cmbAktifPalet.SelectedIndex > 0)
            {
                cmbAktifPalet.SelectedIndex--; // Bir önceki paleti seç
            }

            // MIKNATIS: İşlem bitince imleci saniyesinde barkod kutusuna geri çak!
            if (txtBarkod != null && txtBarkod.Enabled) txtBarkod.Focus();
        }

        // ➡️ SONRAKİ PALET BUTONU
        private void btnSonrakiPalet_Click(object sender, EventArgs e)
        {
            btnSonrakiPalet.TabStop = false; // Tab tuşu ile gelinmesini kesin olarak yasakla

            if (cmbAktifPalet.Items.Count > 0 && cmbAktifPalet.SelectedIndex < cmbAktifPalet.Items.Count - 1)
            {
                cmbAktifPalet.SelectedIndex++; // Bir sonraki paleti seç
            }

            // MIKNATIS: İşlem bitince imleci saniyesinde barkod kutusuna geri çak!
            if (txtBarkod != null && txtBarkod.Enabled) txtBarkod.Focus();
        }

        // 🌟 ÜST TABLOYA (PALET MATRİSİ) SAĞ TIKLANDIĞINDA AÇILAN DÜZENLEME MENÜSÜ
        private void DgvPaletMatrisi_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right && e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                if (dgvPaletMatrisi.Rows[e.RowIndex].Cells[e.ColumnIndex].Value == null || string.IsNullOrWhiteSpace(dgvPaletMatrisi.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString()))
                    return; // Boş hücreye tıklandıysa işlem yapma

                dgvPaletMatrisi.ClearSelection();
                dgvPaletMatrisi.Rows[e.RowIndex].Cells[e.ColumnIndex].Selected = true;
                dgvPaletMatrisi.CurrentCell = dgvPaletMatrisi.Rows[e.RowIndex].Cells[e.ColumnIndex];

                ContextMenuStrip menu = new ContextMenuStrip();
                menu.Font = new Font("Segoe UI", 10, FontStyle.Bold);

                ToolStripMenuItem btnDuzenle = new ToolStripMenuItem("✏️ İçindeki Miktarı Değiştir (Düzenle)");
                btnDuzenle.ForeColor = Color.DarkBlue;
                btnDuzenle.Click += (s, ev) => { MatrisHucreDuzenle(e.RowIndex, e.ColumnIndex); };

                ToolStripMenuItem btnSil = new ToolStripMenuItem("❌ Bu Ürünü Paletten Tamamen Sil");
                btnSil.ForeColor = Color.DarkRed;
                btnSil.Click += (s, ev) => { MatrisHucreSil(e.RowIndex, e.ColumnIndex); };

                menu.Items.Add(btnDuzenle);
                menu.Items.Add(new ToolStripSeparator());
                menu.Items.Add(btnSil);

                menu.Show(Cursor.Position);
            }
        }

        // 🌟 MİKTARI ELLE DEĞİŞTİRME MOTORU (ALT TABLOYU DA OTOMATİK DÜZELTİR)
        private void MatrisHucreDuzenle(int rowIndex, int colIndex)
        {
            string hucreMetni = dgvPaletMatrisi.Rows[rowIndex].Cells[colIndex].Value.ToString();
            string[] parcalar = hucreMetni.Split(new string[] { " | Adet: " }, StringSplitOptions.None);
            if (parcalar.Length != 2) return;

            string urunVeBelge = parcalar[0];
            int.TryParse(parcalar[1], out int eskiAdet);

            int sonParantezAc = urunVeBelge.LastIndexOf('(');
            int sonParantezKapat = urunVeBelge.LastIndexOf(')');
            string belgeNo = "";
            if (sonParantezAc > 0 && sonParantezKapat > sonParantezAc)
                belgeNo = urunVeBelge.Substring(sonParantezAc + 1, sonParantezKapat - sonParantezAc - 1).Trim();

            string malzemeKodu = urunVeBelge;
            int tireIndex = urunVeBelge.IndexOf(" - ");
            if (tireIndex > 0) malzemeKodu = urunVeBelge.Substring(0, tireIndex).Trim();
            else if (sonParantezAc > 0) malzemeKodu = urunVeBelge.Substring(0, sonParantezAc).Trim();

            // Sayı girme ekranı
            Form prompt = new Form() { Width = 350, Height = 200, FormBorderStyle = FormBorderStyle.FixedDialog, Text = "Miktarı Düzenle", StartPosition = FormStartPosition.CenterParent, ShowIcon = false };
            Label lbl = new Label() { Left = 20, Top = 20, Text = "Ürünün bu paletteki yeni miktarını girin:", Width = 300, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            NumericUpDown num = new NumericUpDown() { Left = 20, Top = 50, Width = 150, Minimum = 1, Maximum = 99999, Value = eskiAdet, Font = new Font("Segoe UI", 12) };
            Button btnOnay = new Button() { Text = "KAYDET", Left = 20, Top = 90, Width = 150, Height = 40, BackColor = Color.Teal, ForeColor = Color.White, Font = new Font("Segoe UI", 10, FontStyle.Bold), Cursor = Cursors.Hand };

            btnOnay.Click += (sender, e) =>
            {
                int yeniAdet = (int)num.Value;
                if (yeniAdet == eskiAdet) { prompt.Close(); return; }

                int fark = yeniAdet - eskiAdet;
                int kalanFark = Math.Abs(fark);

                // Alt tabloyla senkronize et
                if (fark > 0) // Ekleniyor
                {
                    foreach (DataGridViewRow row in dgvMalzemeler.Rows)
                    {
                        if (row.IsNewRow) continue;
                        if (row.Cells["Malzeme Kodu"].Value?.ToString().Trim() == malzemeKodu && row.Cells["Belge No"].Value?.ToString().Trim() == belgeNo)
                        {
                            int sip = Convert.ToInt32(row.Cells["Sipariş Adedi"].Value);
                            int oku = Convert.ToInt32(row.Cells["Okutulan"].Value);

                            if (oku < sip)
                            {
                                int eklenecekMiktar = Math.Min(sip - oku, kalanFark);
                                row.Cells["Okutulan"].Value = oku + eklenecekMiktar;
                                kalanFark -= eklenecekMiktar;
                                if (kalanFark == 0) break;
                            }
                        }
                    }
                    if (kalanFark > 0) // Taşırma (Siparişten fazla okutulduysa ilk satıra zorla bindir)
                    {
                        foreach (DataGridViewRow row in dgvMalzemeler.Rows)
                        {
                            if (row.IsNewRow) continue;
                            if (row.Cells["Malzeme Kodu"].Value?.ToString().Trim() == malzemeKodu && row.Cells["Belge No"].Value?.ToString().Trim() == belgeNo)
                            {
                                row.Cells["Okutulan"].Value = Convert.ToInt32(row.Cells["Okutulan"].Value) + kalanFark;
                                break;
                            }
                        }
                    }
                }
                else // Eksiltiliyor
                {
                    foreach (DataGridViewRow row in dgvMalzemeler.Rows)
                    {
                        if (row.IsNewRow) continue;
                        if (row.Cells["Malzeme Kodu"].Value?.ToString().Trim() == malzemeKodu && row.Cells["Belge No"].Value?.ToString().Trim() == belgeNo)
                        {
                            int oku = Convert.ToInt32(row.Cells["Okutulan"].Value);
                            if (oku > 0)
                            {
                                int dusulecek = Math.Min(oku, kalanFark);
                                row.Cells["Okutulan"].Value = oku - dusulecek;
                                kalanFark -= dusulecek;
                                if (kalanFark == 0) break;
                            }
                        }
                    }
                }

                dgvPaletMatrisi.Rows[rowIndex].Cells[colIndex].Value = $"{urunVeBelge} | Adet: {yeniAdet}";
                DgvMalzemeler_Renklendir(null, null); // Renkleri güncelle
                prompt.Close();
            };

            prompt.Controls.Add(lbl); prompt.Controls.Add(num); prompt.Controls.Add(btnOnay);
            prompt.ShowDialog();
        }

        // 🌟 PALETTEN TAMAMEN SİLME MOTORU
        private void MatrisHucreSil(int rowIndex, int colIndex)
        {
            string hucreMetni = dgvPaletMatrisi.Rows[rowIndex].Cells[colIndex].Value.ToString();
            string[] parcalar = hucreMetni.Split(new string[] { " | Adet: " }, StringSplitOptions.None);
            if (parcalar.Length != 2) return;

            string urunVeBelge = parcalar[0];
            int.TryParse(parcalar[1], out int eskiAdet);

            int sonParantezAc = urunVeBelge.LastIndexOf('(');
            int sonParantezKapat = urunVeBelge.LastIndexOf(')');
            string belgeNo = "";
            if (sonParantezAc > 0 && sonParantezKapat > sonParantezAc)
                belgeNo = urunVeBelge.Substring(sonParantezAc + 1, sonParantezKapat - sonParantezAc - 1).Trim();

            string malzemeKodu = urunVeBelge;
            int tireIndex = urunVeBelge.IndexOf(" - ");
            if (tireIndex > 0) malzemeKodu = urunVeBelge.Substring(0, tireIndex).Trim();
            else if (sonParantezAc > 0) malzemeKodu = urunVeBelge.Substring(0, sonParantezAc).Trim();

            DialogResult onay = MessageBox.Show($"Bu ürünü ({eskiAdet} adet) paletten tamamen silmek istiyor musunuz?", "Silme Onayı", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (onay == DialogResult.Yes)
            {
                int kalanFark = eskiAdet;

                foreach (DataGridViewRow row in dgvMalzemeler.Rows)
                {
                    if (row.IsNewRow) continue;
                    if (row.Cells["Malzeme Kodu"].Value?.ToString().Trim() == malzemeKodu && row.Cells["Belge No"].Value?.ToString().Trim() == belgeNo)
                    {
                        int oku = Convert.ToInt32(row.Cells["Okutulan"].Value);
                        if (oku > 0)
                        {
                            int dusulecek = Math.Min(oku, kalanFark);
                            row.Cells["Okutulan"].Value = oku - dusulecek;
                            kalanFark -= dusulecek;
                            if (kalanFark == 0) break;
                        }
                    }
                }

                dgvPaletMatrisi.Rows[rowIndex].Cells[colIndex].Value = ""; // Hücreyi temizle
                DgvMalzemeler_Renklendir(null, null); // Renkleri düzelt
            }
        }

        // 🌟 RAPORLAMA İÇİN YARDIMCI VERİ MODELİ (Metodun hemen üstünde durur)
        private class MatrisRaporVerisi
        {
            public string BelgeNo { get; set; }
            public string MalzemeKodu { get; set; }
            public string MalzemeAdi { get; set; }
            public string Aciklama { get; set; }
            public int[] PaletAdetleri { get; set; }
        }

        // 🌟 MATRİS RAPORUNU SAF EXCEL (.XLSX) YAPAN MOTOR (AYNI ÜRÜNLERİ BİRLEŞTİRİR VE ALT SATIRLARI DÜZELTİR)
        private void btnSevkRaporla_Click(object sender, EventArgs e)
        {
            if (dgvPaletMatrisi.Columns.Count == 0) return;

            // Matrisin içi boş mu kontrolü
            bool matrisDoluMu = false;
            foreach (DataGridViewRow r in dgvPaletMatrisi.Rows)
            {
                for (int c = 0; c < dgvPaletMatrisi.Columns.Count; c++)
                {
                    if (r.Cells[c].Value != null && !string.IsNullOrWhiteSpace(r.Cells[c].Value.ToString()))
                    {
                        matrisDoluMu = true; break;
                    }
                }
            }
            if (!matrisDoluMu)
            {
                MessageBox.Show("Raporlanacak palet verisi bulunamadı!", "Liste Boş", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // 🌟 1. ADIM: SADECE PALET MATRİSİNİ OKU VE İÇİNDEKİLERİ TOPLA
                int paletSayisi = dgvPaletMatrisi.Columns.Count;
                var raporHavuzu = new Dictionary<string, MatrisRaporVerisi>();

                for (int j = 0; j < paletSayisi; j++)
                {
                    foreach (DataGridViewRow paletSatiri in dgvPaletMatrisi.Rows)
                    {
                        if (paletSatiri.Cells[j].Value != null && !string.IsNullOrWhiteSpace(paletSatiri.Cells[j].Value.ToString()))
                        {
                            string hucreMetni = paletSatiri.Cells[j].Value.ToString();

                            // Örnek Metin: "8691234 - Kasa [Beyaz] (SE-001) | Adet: 5"
                            string[] parcalar = hucreMetni.Split(new string[] { " | Adet: " }, StringSplitOptions.None);

                            if (parcalar.Length == 2)
                            {
                                string urunVeBelge = parcalar[0];
                                int.TryParse(parcalar[1], out int adet);

                                // Belge Numarasını Çıkar
                                string bNo = "SE";
                                int parantezIndex = urunVeBelge.LastIndexOf('(');
                                int sonParantez = urunVeBelge.LastIndexOf(')');
                                if (parantezIndex > 0 && sonParantez > parantezIndex)
                                {
                                    bNo = urunVeBelge.Substring(parantezIndex + 1, sonParantez - parantezIndex - 1).Trim();
                                    urunVeBelge = urunVeBelge.Substring(0, parantezIndex).Trim();
                                }

                                // Malzeme Kodunu ve Adını Çıkar
                                string mKodu = urunVeBelge;
                                int tireIndex = urunVeBelge.IndexOf(" - ");
                                if (tireIndex > 0) mKodu = urunVeBelge.Substring(0, tireIndex).Trim();

                                string anahtar = mKodu;

                                if (!raporHavuzu.ContainsKey(anahtar))
                                {
                                    raporHavuzu[anahtar] = new MatrisRaporVerisi
                                    {
                                        BelgeNo = bNo,
                                        MalzemeKodu = mKodu,
                                        PaletAdetleri = new int[paletSayisi]
                                    };

                                    // İsim ve Açıklamayı sol tablodan bul
                                    foreach (DataGridViewRow solSatir in dgvMalzemeler.Rows)
                                    {
                                        if (solSatir.IsNewRow) continue;
                                        if (solSatir.Cells["Malzeme Kodu"].Value?.ToString().Trim() == mKodu)
                                        {
                                            // 🌟 YENİ ZIRH: SQL'den gelen gizli alt satır (Enter) karakterlerini temizle ve boşluğa çevir!
                                            string mAdi = solSatir.Cells["Malzeme Adı"].Value?.ToString() ?? "";
                                            string mAciklama = solSatir.Cells["Açıklama"].Value?.ToString() ?? "";

                                            raporHavuzu[anahtar].MalzemeAdi = mAdi.Replace("\r", " ").Replace("\n", " ").Trim();
                                            raporHavuzu[anahtar].Aciklama = mAciklama.Replace("\r", " ").Replace("\n", " ").Trim();
                                            break;
                                        }
                                    }

                                    // Sol tabloda bulamazsa kendi parçaladığı metni yazsın
                                    if (string.IsNullOrEmpty(raporHavuzu[anahtar].MalzemeAdi))
                                    {
                                        string hamAd = tireIndex > 0 ? urunVeBelge.Substring(tireIndex + 3) : "Ürün Adı Yok";
                                        raporHavuzu[anahtar].MalzemeAdi = hamAd.Replace("\r", " ").Replace("\n", " ").Trim();
                                        raporHavuzu[anahtar].Aciklama = "";
                                    }
                                }
                                else
                                {
                                    // BİRLEŞTİRME ZIRHI: Aynı ürün farklı faturadan geldiyse Belge No'sunu yanına virgülle ekle
                                    if (!raporHavuzu[anahtar].BelgeNo.Contains(bNo))
                                    {
                                        raporHavuzu[anahtar].BelgeNo += ", " + bNo;
                                    }
                                }

                                // MATRİSTEKİ GERÇEK ADETİ HAVUZA YAZ VE TOPLA
                                raporHavuzu[anahtar].PaletAdetleri[j] += adet;
                            }
                        }
                    }
                }

                if (raporHavuzu.Count == 0) return;

                // 🌟 2. ADIM: EXCEL'İ OLUŞTUR
                string anaBelgeNo = raporHavuzu.Values.FirstOrDefault()?.BelgeNo.Split(',')[0].Trim() ?? "SE";
                string turKlasoru = "Yurtiçi"; // Varsayılan

                if (dtTumSiparisler != null && dtTumSiparisler.Rows.Count > 0)
                {
                    DataRow[] dbSatirlari = dtTumSiparisler.Select($"BelgeNo LIKE '%{anaBelgeNo}%'");
                    if (dbSatirlari.Length > 0 && dbSatirlari[0]["BelgeTipi"] != DBNull.Value && dbSatirlari[0]["BelgeTipi"].ToString().Trim() == "O1")
                    {
                        turKlasoru = "İhracat";
                    }
                }

                string klasor = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "TamgaApp Sevkiyat Raporları", turKlasoru, DateTime.Now.ToString("yyyy"), DateTime.Now.ToString("MM"), DateTime.Now.ToString("dd"));
                if (!Directory.Exists(klasor)) Directory.CreateDirectory(klasor);

                string musteriTemiz = string.Join("_", txtMusteriAdi.Text.Split(Path.GetInvalidFileNameChars()));
                if (string.IsNullOrWhiteSpace(musteriTemiz)) musteriTemiz = "Musteri";
                string tamYol = Path.Combine(klasor, $"MatrisRaporu_{musteriTemiz}_{DateTime.Now:HHmm}.xlsx");

                using (var wb = new ClosedXML.Excel.XLWorkbook())
                {
                    var ws = wb.Worksheets.Add("Sevkiyat Matrisi");
                    ws.Style.Font.FontName = "Times New Roman";
                    ws.Style.Font.FontSize = 11;

                    // 🌟 TÜM ÇALIŞMA SAYFASI İÇİN "METNİ KAYDIR" ÖZELLİĞİNİ KOMPLE KAPATTIK!
                    ws.Style.Alignment.WrapText = false;

                    // Başlıklar
                    ws.Cell(1, 1).Value = "Müşteri Adı";
                    ws.Cell(1, 2).Value = "Belge No";
                    ws.Cell(1, 3).Value = "Malzeme Kodu";
                    ws.Cell(1, 4).Value = "Malzeme Adı";
                    ws.Cell(1, 5).Value = "Açıklaması";
                    ws.Cell(1, 6).Value = "Toplam Adet";

                    // Palet Başlıkları
                    int colIndex = 7;
                    for (int i = 0; i < paletSayisi; i++)
                    {
                        ws.Cell(1, colIndex).Value = $"PALET {i + 1}";
                        colIndex++;
                    }

                    // Verileri Doldur
                    int satir = 2;
                    foreach (var rv in raporHavuzu.Values)
                    {
                        int genelToplam = rv.PaletAdetleri.Sum();
                        if (genelToplam == 0) continue;

                        ws.Cell(satir, 1).Value = txtMusteriAdi.Text.Trim();
                        ws.Cell(satir, 2).Value = rv.BelgeNo;
                        ws.Cell(satir, 3).Value = rv.MalzemeKodu;
                        ws.Cell(satir, 4).Value = rv.MalzemeAdi;
                        ws.Cell(satir, 5).Value = rv.Aciklama;
                        ws.Cell(satir, 6).Value = genelToplam;

                        int pCol = 7;
                        for (int j = 0; j < paletSayisi; j++)
                        {
                            if (rv.PaletAdetleri[j] > 0)
                            {
                                ws.Cell(satir, pCol).Value = rv.PaletAdetleri[j];
                            }
                            pCol++;
                        }
                        satir++;
                    }

                    // Sütun genişliklerini içeriğe göre otomatik ayarla
                    ws.Columns().AdjustToContents();

                    // Satır yüksekliklerini normale (15) zorla ki bozulma olmasın
                    ws.Rows().Height = 15;

                    wb.SaveAs(tamYol);
                }

                MessageBox.Show($"Saf Excel (.xlsx) Raporu oluşturuldu!\nAynı ürünler birleştirildi.\n\nKayıt Yeri:\n{tamYol}", "Rapor Tamamlandı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(tamYol) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Raporlama sırasında hata oluştu: " + ex.Message, "Kritik Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // 🌟 OTOMATİK BARKOD YÖNLENDİRİCİ ZIRHI (Mıknatıs Motoru)
        // Eğer kullanıcı tabloya tıklayıp unutursa ve barkodu okutursa, 
        // sistem ilk harfi yakalar yakalamaz odak noktasını otomatik olarak barkod kutusuna çeker!
        private void Dgv_BarkodYonlendir_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Gelen tuş kontrol karakteri (Enter, Tab vs.) değilse, yani okuyucu barkodu göndermeye başladıysa
            if (!char.IsControl(e.KeyChar))
            {
                if (txtBarkod != null)
                {
                    txtBarkod.Focus(); // İmleci anında barkod kutusuna çek
                    txtBarkod.AppendText(e.KeyChar.ToString()); // İlk okunan harfi kaybetmeden kutuya yaz
                    e.Handled = true; // Tablonun bu tuşu algılamasını (içine yazmaya çalışmasını) tamamen engelle
                }
            }
        }

        // Kullanıcının sevk edilecek ürünleri el terminali (okuyucu) ile tek tek okuttuğu ana motor.
        private void txtBarkod_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true; // Bip sesini ve Enter'ı engelle

                // 🌟 1. ADIM: SİHİRLİ KUTU TEMİZLİĞİ
                // Metni anında bir değişkene alıp KUTUYU HİÇ BEKLEMEDEN TEMİZLİYORUZ.
                // Böylece sen bu barkodu işlerken, arkadan gelen ikinci okuma tertemiz kutuya yazılmaya başlar.
                string hamVeri = txtBarkod.Text.Trim();
                txtBarkod.Clear();

                if (string.IsNullOrEmpty(hamVeri)) return;

                // 🌟 2. ADIM: HAFIZALI BARKOD AYIRICI (Anti-Üst Üste Yazma)
                // Eğer çok hızlı okuttuğun için 2 veya 3 barkod kutuda birleştiyse (Örn: 26 hane, 39 hane),
                // program bunları 13'erli paketlere böler ve hiçbirini çöpe atmaz, sırayla listeye alır!
                List<string> islenecekBarkodlar = new List<string>();

                if (hamVeri.Length > 13 && hamVeri.Length % 13 == 0)
                {
                    // Tam 2 veya 3 barkod birleşmişse (26, 39...) onları 13'erli parçala
                    for (int i = 0; i < hamVeri.Length; i += 13)
                    {
                        islenecekBarkodlar.Add(hamVeri.Substring(i, 13));
                    }
                }
                else if (hamVeri.Length > 13)
                {
                    // Arada eksik karakter kaynamış ve 13'ün katı değilse, en azından ilk barkodu (ilk 13 haneyi) kurtar
                    islenecekBarkodlar.Add(hamVeri.Substring(0, 13));
                }
                else
                {
                    // Normal 13 hane veya daha kısa bir tekli okumaysa direkt ekle
                    islenecekBarkodlar.Add(hamVeri);
                }

                // 🌟 3. ADIM: AYRILAN BARKODLARI SIRAYLA İŞLE
                foreach (string okutulanBarkod in islenecekBarkodlar)
                {
                    if (cmbAktifPalet.SelectedItem == null)
                    {
                        MessageBox.Show("Lütfen ürünleri okutmadan önce sağdan bir AKTİF PALET seçin!", "Palet Seçilmedi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        break; // Palet yoksa diğer okumaları da iptal et ve döngüyü kır
                    }

                    int aktifPaletSutunIndex = cmbAktifPalet.SelectedIndex;
                    bool urunBulundu = false;
                    DataGridViewRow hedefSatir = null;

                    // MÜKEMMEL MANTIK (FİFO): Tablodaki satırları gez, barkodu eşleşen ve ADEDİ HENÜZ DOLMAMIŞ ilk satırı bul!
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
                            }
                        }
                    }

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
                            BasariliSesCal();
                        }
                        else hedefSatir.DefaultCellStyle.BackColor = Color.LightYellow;

                        // --------- PALETE EKLEME MANTIĞI ---------
                        string urunAdi = hedefSatir.Cells["Malzeme Adı"].Value.ToString();
                        // Palet Matrisine Ekleme İşlemi (Sadece Malzeme Kodu, Belge No ve Adet Yazar)
                        string malzemeKodu = hedefSatir.Cells["Malzeme Kodu"].Value.ToString().Trim();
                        string aitOlduguBelge = hedefSatir.Cells["Belge No"].Value.ToString().Trim();

                        bool paletSutunundaVarMi = false;

                        foreach (DataGridViewRow paletSatiri in dgvPaletMatrisi.Rows)
                        {
                            if (paletSatiri.Cells[aktifPaletSutunIndex].Value != null)
                            {
                                string hucreMetni = paletSatiri.Cells[aktifPaletSutunIndex].Value.ToString();
                                if (hucreMetni.Contains(malzemeKodu) && hucreMetni.Contains(aitOlduguBelge))
                                {
                                    string[] parcalar = hucreMetni.Split(new string[] { "| Adet: " }, StringSplitOptions.None);
                                    if (parcalar.Length == 2)
                                    {
                                        int mevcutPaletAdeti = int.Parse(parcalar[1]);
                                        paletSatiri.Cells[aktifPaletSutunIndex].Value = $"{parcalar[0]}| Adet: {mevcutPaletAdeti + 1}";
                                    }
                                    paletSutunundaVarMi = true;
                                    break;
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
                                    // Sadece Malzeme Kodu, Belge No ve Adet
                                    paletSatiri.Cells[aktifPaletSutunIndex].Value = $"{malzemeKodu} ({aitOlduguBelge}) | Adet: 1";
                                    bosHucreBulundu = true;
                                    break;
                                }
                            }

                            if (!bosHucreBulundu)
                            {
                                int yeniSatirIndex = dgvPaletMatrisi.Rows.Add();
                                dgvPaletMatrisi.Rows[yeniSatirIndex].Cells[aktifPaletSutunIndex].Value = $"{malzemeKodu} ({aitOlduguBelge}) | Adet: 1";
                            }
                        }
                    }
                    else
                    {
                        if (!urunBulundu)
                        {
                            try
                            {
                                string wavYolu = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "hata.wav");
                                if (System.IO.File.Exists(wavYolu)) new System.Media.SoundPlayer(wavYolu).Play();
                                else System.Media.SystemSounds.Hand.Play();
                            }
                            catch { }

                            // Hangi barkodda hata verdiğini ekranda göstersin diye okutulanBarkod'u uyarıya ekledik
                            MessageBox.Show($"HATA! Okutulan BARKOD ({okutulanBarkod}) sipariş listesinde bulunamadı veya kotası dolu!", "Yanlış Ürün", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                } // Foreach döngüsü (Bölünen barkodları tarama) bitti

                // 🌟 4. ADIM: İMLECİ ZORLA GERİ ÇAK
                // Tüm işlemler bittikten sonra odak kaybını %100 önler.
                this.BeginInvoke(new Action(() =>
                {
                    txtBarkod.Focus();
                }));
            }
        }

        // 🌟 ACİL DURUM BUTONU: Barkodu olmayan veya okumayan ürünleri manuel olarak palete ekler
        private void btnManuelEkle_Click(object sender, EventArgs e)
        {
            // 1. Palet seçili mi kalkanı
            if (cmbAktifPalet.SelectedItem == null)
            {
                MessageBox.Show("Lütfen manuel ekleme yapmadan önce sağdan bir AKTİF PALET seçin!", "Palet Seçilmedi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 2. Tablodan ürün seçilmiş mi kalkanı
            if (dgvMalzemeler.SelectedRows.Count == 0)
            {
                MessageBox.Show("Lütfen tablodan okutulmuş saymak istediğiniz ürünü (satırı) seçin!", "Ürün Seçilmedi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Kullanıcının tablodan tıkladığı satırı yakala
            DataGridViewRow hedefSatir = dgvMalzemeler.SelectedRows[0];

            if (hedefSatir.IsNewRow || hedefSatir.Cells["Malzeme Kodu"].Value == null) return;

            int siparisAdedi = Convert.ToInt32(hedefSatir.Cells["Sipariş Adedi"].Value);
            int okutulanAdet = Convert.ToInt32(hedefSatir.Cells["Okutulan"].Value);

            // 🌟 KULLANICININ SEÇTİĞİ ADEDİ NUMARATÖRDEN ALIYORUZ (Varsayılan 1)
            int eklenecekMiktar = (numManuelAdet != null) ? (int)numManuelAdet.Value : 1;

            // Kota aşım kontrolü
            if (okutulanAdet + eklenecekMiktar > siparisAdedi)
            {
                MessageBox.Show($"Eklemek istediğiniz miktar sipariş kotasını aşıyor!\nKalan hak: {siparisAdedi - okutulanAdet}", "Kota Aşımı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 3. Adedi seçilen miktar kadar arttır ve satırın rengini boya
            okutulanAdet += eklenecekMiktar;
            hedefSatir.Cells["Okutulan"].Value = okutulanAdet;

            if (okutulanAdet == siparisAdedi)
            {
                hedefSatir.DefaultCellStyle.BackColor = Color.LightGreen;
                try { string wavYolu = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "basarili.wav"); if (System.IO.File.Exists(wavYolu)) new System.Media.SoundPlayer(wavYolu).Play(); else System.Media.SystemSounds.Asterisk.Play(); } catch { }
            }
            else
            {
                hedefSatir.DefaultCellStyle.BackColor = Color.LightYellow;
            }

            // 🌟 4. PALETE SEÇİLEN MİKTAR KADAR EKLEME MANTIĞI (RENK KÖRÜ ZIRHI EKLENDİ)
            int aktifPaletSutunIndex = cmbAktifPalet.SelectedIndex;
            string urunAdi = hedefSatir.Cells["Malzeme Adı"].Value.ToString();
            string aciklama = hedefSatir.Cells["Açıklama"].Value?.ToString().Trim() ?? ""; // 🌟 YENİ: Rengi Çek
            string aitOlduguBelge = hedefSatir.Cells["Belge No"].Value.ToString();
            string malzemeKodu = hedefSatir.Cells["Malzeme Kodu"].Value.ToString().Trim();

            // 🌟 YENİ: Açıklama (Renk) boş değilse, ürün adının yanına köşeli parantezle ekle
            string tamUrunAdi = string.IsNullOrWhiteSpace(aciklama) ? urunAdi : $"{urunAdi} [{aciklama}]";

            bool paletSutunundaVarMi = false;

            foreach (DataGridViewRow paletSatiri in dgvPaletMatrisi.Rows)
            {
                if (paletSatiri.Cells[aktifPaletSutunIndex].Value != null)
                {
                    string hucreMetni = paletSatiri.Cells[aktifPaletSutunIndex].Value.ToString();

                    // 🌟 YENİ ZIRH: Artık birleştirme yaparken Rengin (Açıklamanın) de aynı olup olmadığına bakıyor!
                    if (hucreMetni.Contains(malzemeKodu) && hucreMetni.Contains(aitOlduguBelge) && (string.IsNullOrWhiteSpace(aciklama) || hucreMetni.Contains(aciklama)))
                    {
                        string[] parcalar = hucreMetni.Split(new string[] { "| Adet: " }, StringSplitOptions.None);
                        if (parcalar.Length == 2)
                        {
                            int mevcutPaletAdeti = int.Parse(parcalar[1]);
                            // Seçilen miktar kadar palet içeriğine toplu ekleme yapıyoruz
                            paletSatiri.Cells[aktifPaletSutunIndex].Value = $"{parcalar[0]}| Adet: {mevcutPaletAdeti + eklenecekMiktar}";
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
                        paletSatiri.Cells[aktifPaletSutunIndex].Value = $"{malzemeKodu} - {tamUrunAdi} ({aitOlduguBelge}) | Adet: {eklenecekMiktar}";
                        bosHucreBulundu = true; break;
                    }
                }

                if (!bosHucreBulundu)
                {
                    int yeniSatirIndex = dgvPaletMatrisi.Rows.Add();
                    dgvPaletMatrisi.Rows[yeniSatirIndex].Cells[aktifPaletSutunIndex].Value = $"{malzemeKodu} - {tamUrunAdi} ({aitOlduguBelge}) | Adet: {eklenecekMiktar}";
                }
            }

            // İşlem bitince numaratörü tekrar güvenli olan 1 değerine sıfırla
            if (numManuelAdet != null) numManuelAdet.Value = 1;

            txtBarkod.Focus();
        }

        // 🌟 ACİL DURUM BUTONU: Yanlış eklenen veya fazla okutulan ürünleri manuel olarak paletten düşer
        private void btnManuelEksilt_Click(object sender, EventArgs e)
        {
            // 1. Palet seçili mi kalkanı
            if (cmbAktifPalet.SelectedItem == null)
            {
                MessageBox.Show("Lütfen eksiltme yapmadan önce sağdan bir AKTİF PALET seçin!", "Palet Seçilmedi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 2. Tablodan ürün seçilmiş mi kalkanı
            if (dgvMalzemeler.SelectedRows.Count == 0)
            {
                MessageBox.Show("Lütfen tablodan miktarını düşmek istediğiniz ürünü (satırı) seçin!", "Ürün Seçilmedi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DataGridViewRow hedefSatir = dgvMalzemeler.SelectedRows[0];

            if (hedefSatir.IsNewRow || hedefSatir.Cells["Malzeme Kodu"].Value == null) return;

            int siparisAdedi = Convert.ToInt32(hedefSatir.Cells["Sipariş Adedi"].Value);
            int okutulanAdet = Convert.ToInt32(hedefSatir.Cells["Okutulan"].Value);

            // Numaratörden düşülecek miktarı al
            int eksiltilecekMiktar = (numManuelAdet != null) ? (int)numManuelAdet.Value : 1;

            if (okutulanAdet < eksiltilecekMiktar)
            {
                MessageBox.Show($"Düşmek istediğiniz miktar, okutulan miktardan ({okutulanAdet}) büyük olamaz!", "Geçersiz İşlem", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 🌟 3. PALETTEN SEÇİLEN MİKTAR KADAR EKSİLTME MANTIĞI
            int aktifPaletSutunIndex = cmbAktifPalet.SelectedIndex;
            string aciklama = hedefSatir.Cells["Açıklama"].Value?.ToString().Trim() ?? "";
            string aitOlduguBelge = hedefSatir.Cells["Belge No"].Value.ToString();
            string malzemeKodu = hedefSatir.Cells["Malzeme Kodu"].Value.ToString().Trim();

            bool paletSutunundaBulundu = false;

            foreach (DataGridViewRow paletSatiri in dgvPaletMatrisi.Rows)
            {
                if (paletSatiri.Cells[aktifPaletSutunIndex].Value != null)
                {
                    string hucreMetni = paletSatiri.Cells[aktifPaletSutunIndex].Value.ToString();

                    // Ürün eşleşmesini kontrol et (Renk/Açıklama körü zırhı dahil)
                    if (hucreMetni.Contains(malzemeKodu) && hucreMetni.Contains(aitOlduguBelge) && (string.IsNullOrWhiteSpace(aciklama) || hucreMetni.Contains(aciklama)))
                    {
                        string[] parcalar = hucreMetni.Split(new string[] { "| Adet: " }, StringSplitOptions.None);
                        if (parcalar.Length == 2)
                        {
                            int mevcutPaletAdeti = int.Parse(parcalar[1]);
                            int yeniAdet = mevcutPaletAdeti - eksiltilecekMiktar;

                            if (yeniAdet > 0)
                            {
                                // Miktar sıfırlanmadıysa sadece rakamı güncelle
                                paletSatiri.Cells[aktifPaletSutunIndex].Value = $"{parcalar[0]}| Adet: {yeniAdet}";
                            }
                            else
                            {
                                // Eğer sıfıra düştüyse hücreyi matristen tamamen temizle (Çöplük yapmasın)
                                paletSatiri.Cells[aktifPaletSutunIndex].Value = "";
                            }
                        }
                        paletSutunundaBulundu = true;
                        break;
                    }
                }
            }

            if (!paletSutunundaBulundu)
            {
                MessageBox.Show("Düşmeye çalıştığınız ürün seçili AKTİF PALET'in içinde bulunamadı!\n\nLütfen sağdan doğru paleti seçtiğinizden emin olun.", "Palette Bulunamadı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return; // Palette bulamadığı ürünü ana listeden de düşmesin, işlemi iptal et
            }

            // 4. Ana tablodaki Adedi düşür ve satırın rengini eski haline (Sarı veya Beyaz) çevir
            okutulanAdet -= eksiltilecekMiktar;
            hedefSatir.Cells["Okutulan"].Value = okutulanAdet;

            if (okutulanAdet >= siparisAdedi && siparisAdedi > 0) hedefSatir.DefaultCellStyle.BackColor = Color.LightGreen;
            else if (okutulanAdet > 0) hedefSatir.DefaultCellStyle.BackColor = Color.LightYellow;
            else hedefSatir.DefaultCellStyle.BackColor = Color.White;

            // İşlem bitince numaratörü güvenli değer olan 1'e sıfırla
            if (numManuelAdet != null) numManuelAdet.Value = 1;

            txtBarkod.Focus();
        }

        // 🌟 SOL TABLO SAĞ TIK MENÜSÜ (Ekle / Çıkar / Tamamını Yap)
        private void dgvMalzemeler_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            // Sadece sağ tıka ve geçerli bir satıra basıldıysa çalış
            if (e.Button == MouseButtons.Right && e.RowIndex >= 0)
            {
                dgvMalzemeler.ClearSelection();
                dgvMalzemeler.Rows[e.RowIndex].Selected = true;

                // Seçili satırdaki Sipariş ve Okutulan verilerini al
                int siparisAdedi = 0, okutulanAdet = 0;
                if (dgvMalzemeler.Rows[e.RowIndex].Cells["Sipariş Adedi"].Value != null)
                    int.TryParse(dgvMalzemeler.Rows[e.RowIndex].Cells["Sipariş Adedi"].Value.ToString(), out siparisAdedi);

                if (dgvMalzemeler.Rows[e.RowIndex].Cells["Okutulan"].Value != null)
                    int.TryParse(dgvMalzemeler.Rows[e.RowIndex].Cells["Okutulan"].Value.ToString(), out okutulanAdet);

                // Eklenebilecek kalan kotayı hesapla
                int kalanAdet = siparisAdedi - okutulanAdet;

                ContextMenuStrip sagTikMenu = new ContextMenuStrip();
                sagTikMenu.Font = new Font("Segoe UI", 11, FontStyle.Bold);

                // 1. SEÇİLİ MİKTARI EKLE (Sadece numaratörde yazan rakam kadar ekler)
                ToolStripMenuItem btnEkle = new ToolStripMenuItem("➕ Seçili Miktarı Ekle");
                btnEkle.ForeColor = Color.DarkGreen;
                btnEkle.Click += (s, ev) => { btnManuelEkle_Click(null, null); };

                // 2. TAMAMINI EKLE (Siparişi kapatacak kadar olan KALAN miktarın hepsini tek tıkla ekler)
                ToolStripMenuItem btnTamaminiEkle = new ToolStripMenuItem($"✅ Tamamını Ekle ({kalanAdet} Adet)");
                btnTamaminiEkle.ForeColor = Color.DarkGreen;
                btnTamaminiEkle.Enabled = (kalanAdet > 0); // Kalan yoksa butonu pasif (tıklanamaz) yap
                btnTamaminiEkle.Click += (s, ev) =>
                {
                    if (numManuelAdet != null)
                    {
                        decimal eskiMax = numManuelAdet.Maximum;
                        // Numaratör limitini aşan bir adet varsa çökmemesi için anlık limiti esnetiyoruz
                        if (kalanAdet > numManuelAdet.Maximum) numManuelAdet.Maximum = kalanAdet;

                        numManuelAdet.Value = kalanAdet;
                        btnManuelEkle_Click(null, null);

                        numManuelAdet.Value = 1; // İşlem bitince 1'e sıfırla
                        numManuelAdet.Maximum = eskiMax; // Limiti eski haline al
                    }
                };

                // 3. SEÇİLİ MİKTARI EKSİLT (Sadece numaratörde yazan rakam kadar düşer)
                ToolStripMenuItem btnCikar = new ToolStripMenuItem("➖ Seçili Miktarı Çıkar (Eksilt)");
                btnCikar.ForeColor = Color.DarkRed;
                btnCikar.Click += (s, ev) => { btnManuelEksilt_Click(null, null); };

                // 4. TAMAMINI EKSİLT (Tabloda okutulmuş olan miktarın alayını paletten siler)
                ToolStripMenuItem btnTamaminiCikar = new ToolStripMenuItem($"❌ Tamamını Eksilt ({okutulanAdet} Adet)");
                btnTamaminiCikar.ForeColor = Color.DarkRed;
                btnTamaminiCikar.Enabled = (okutulanAdet > 0); // Okutulan yoksa butonu pasif yap
                btnTamaminiCikar.Click += (s, ev) =>
                {
                    if (numManuelAdet != null)
                    {
                        decimal eskiMax = numManuelAdet.Maximum;
                        if (okutulanAdet > numManuelAdet.Maximum) numManuelAdet.Maximum = okutulanAdet;

                        numManuelAdet.Value = okutulanAdet;
                        btnManuelEksilt_Click(null, null);

                        numManuelAdet.Value = 1;
                        numManuelAdet.Maximum = eskiMax;
                    }
                };

                // Menü Öğelerini Diz ve Araya Ayırıcı Çizgiler Koy
                sagTikMenu.Items.Add(btnEkle);
                sagTikMenu.Items.Add(btnTamaminiEkle);
                sagTikMenu.Items.Add(new ToolStripSeparator());
                sagTikMenu.Items.Add(btnCikar);
                sagTikMenu.Items.Add(btnTamaminiCikar);

                // 🌟 ÇÖKMEYİ ÖNLEYEN ZIRH (Gecikmeli Silme)
                sagTikMenu.Closed += (senderMenu, argsMenu) => { this.BeginInvoke(new Action(() => sagTikMenu.Dispose())); };

                sagTikMenu.Show(Cursor.Position);
            }
        }

        // 🌟 GERİ ALMA (UNDO) MOTORU: Paletten seçilen ürünün okutulmasını geri alır
        private void btnPalettenSil_Click(object sender, EventArgs e)
        {
            // Hücre seçili mi veya içi boş mu kontrolü
            if (dgvPaletMatrisi.CurrentCell == null || dgvPaletMatrisi.CurrentCell.Value == null || string.IsNullOrWhiteSpace(dgvPaletMatrisi.CurrentCell.Value.ToString()))
            {
                MessageBox.Show("Lütfen sağdaki palet tablosundan silmek (geri almak) istediğiniz ürünü seçin!", "Seçim Yok", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string hucreMetni = dgvPaletMatrisi.CurrentCell.Value.ToString();

            try
            {
                // Örnek Metin: "869123456 - Klavye (SE-001) | Adet: 3"
                string[] anaParcalar = hucreMetni.Split(new string[] { " | Adet: " }, StringSplitOptions.None);
                if (anaParcalar.Length != 2) return;

                string urunVeBelge = anaParcalar[0];
                int paletAdeti = int.Parse(anaParcalar[1]);

                // Metnin içinden Belge No'yu ve Barkodu (veya Kodu) cımbızla çek
                int sonParantezAc = urunVeBelge.LastIndexOf('(');
                int sonParantezKapat = urunVeBelge.LastIndexOf(')');

                string belgeNo = "";
                if (sonParantezAc > 0 && sonParantezKapat > sonParantezAc)
                {
                    belgeNo = urunVeBelge.Substring(sonParantezAc + 1, sonParantezKapat - sonParantezAc - 1);
                }

                string barkod = urunVeBelge.Substring(0, urunVeBelge.IndexOf(" - ")).Trim();

                // 1. ADIM: Soldaki Ana Tablodan (dgvMalzemeler) Düşüş Yap ve Rengi Düzelt
                bool solTablodaBulundu = false;
                foreach (DataGridViewRow row in dgvMalzemeler.Rows)
                {
                    if (row.IsNewRow) continue;

                    string rowBarkod = row.Cells["Barkod"].Value?.ToString();
                    string rowMalzeme = row.Cells["Malzeme Kodu"].Value?.ToString();
                    string rowBelge = row.Cells["Belge No"].Value?.ToString();

                    // Ürün ve Belge No eşleştiyse
                    if ((rowBarkod == barkod || rowMalzeme == barkod) && rowBelge == belgeNo)
                    {
                        int okutulan = Convert.ToInt32(row.Cells["Okutulan"].Value);
                        if (okutulan > 0)
                        {
                            okutulan--;
                            row.Cells["Okutulan"].Value = okutulan;

                            int siparis = Convert.ToInt32(row.Cells["Sipariş Adedi"].Value);

                            // Adet düştüğü için renkleri eski haline (Beyaz veya Sarı) çevir
                            if (okutulan == 0) row.DefaultCellStyle.BackColor = Color.White;
                            else if (okutulan < siparis) row.DefaultCellStyle.BackColor = Color.LightYellow;

                            solTablodaBulundu = true;
                            break; // Ürünü bulduk ve düşürdük, döngüyü kır
                        }
                    }
                }

                if (!solTablodaBulundu)
                {
                    MessageBox.Show("Bu ürün sol tabloda bulunamadığı veya zaten '0' olduğu için silinemiyor!", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // 2. ADIM: Sağdaki Palet Matrisini Güncelle
                if (paletAdeti > 1)
                {
                    // Palette 1'den fazla varsa sayıyı 1 düşür
                    dgvPaletMatrisi.CurrentCell.Value = $"{urunVeBelge} | Adet: {paletAdeti - 1}";
                }
                else
                {
                    // Palette son 1 tane kaldıysa metni tamamen sil (hücreyi boşalt)
                    dgvPaletMatrisi.CurrentCell.Value = "";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Silme işlemi sırasında metin ayrıştırma hatası oluştu: " + ex.Message, "Kritik Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // 🌟 BÜYÜK TEMİZLİK (RESTART) MOTORU: Ekrandaki her şeyi sıfırlar
        private void btnSevkTemizle_Click(object sender, EventArgs e)
        {
            // 🛡️ ZIRH EKLENDİ: MessageBoxDefaultButton.Button2
            DialogResult cevap = MessageBox.Show(
                "DİKKAT: Ekranda okutulmuş olan TÜM ÜRÜNLER ve paletler silinecek. Sevkiyata en baştan başlamak zorunda kalacaksınız.\n\nEmin misiniz?",
                "Tüm Ekranı Sıfırla",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2); // <-- SİHİRLİ KALKAN BURADA

            if (cevap == DialogResult.Yes)
            {
                txtMusteriAdi.Clear();
                txtSevkMusteri.Clear();
                txtBarkod.Clear();
                clbBelgeNo.Items.Clear();
                aktifPaletBarkodlari.Clear();

                dgvMalzemeler.DataSource = null; // Sol tabloyu uçur

                dgvPaletMatrisi.Columns.Clear(); // Palet sütunlarını uçur
                dgvPaletMatrisi.Rows.Clear();    // Palet satırlarını uçur
                cmbAktifPalet.Items.Clear();
                cmbSevkPaletSayisi.SelectedIndex = -1;

                MessageBox.Show("Ekran başarıyla sıfırlandı. Müşteri seçerek en baştan başlayabilirsiniz.", "Temizlendi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        // Olası kilitlenmelerde SQL bağlantısını ve önbellekteki çekilmiş siparişleri sıfırlar.
        private void btnTumVerileriTemizle_Click(object sender, EventArgs e)
        {
            // 🛡️ ZIRH EKLENDİ: MessageBoxDefaultButton.Button2
            DialogResult onay = MessageBox.Show(
                "DİKKAT: SQL bağlantı ayarları, çekilen tüm siparişler ve önbellekteki veriler SİLİNECEKTİR!",
                "Büyük Temizlik Onayı",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2); // <-- SİHİRLİ KALKAN BURADA

            if (onay == DialogResult.Yes)
            {
                Properties.Settings.Default.SqlSunucu = ""; Properties.Settings.Default.SqlVeritabani = "";
                Properties.Settings.Default.SqlKullanici = ""; Properties.Settings.Default.SqlSifre = "";
                Properties.Settings.Default.Save();

                clbBelgeNo.Items.Clear();
                txtMusteriAdi.Clear(); txtSevkMusteri.Clear(); dgvMalzemeler.DataSource = null;
                if (dtTumSiparisler != null) dtTumSiparisler.Clear();

                MessageBox.Show("Tüm operasyonel veriler sıfırlandı!", "Temizlik Başarılı");
            }
        }

        #endregion

        #region 📝 13.6 TAM VE KISMİ SEVKİYAT İŞLEMLERİ

        // 🌟 TAM SEVKİYAT MOTORU (Otomatik Hayalet Yükleme Özellikli)
        private void btnTamSevk_Click(object sender, EventArgs e)
        {

            // 🌟 ZIRH 1: Havada Kalan Verileri Tabloya Yazdır
            dgvPaletler.EndEdit();
            dgvPaletMatrisi.EndEdit();

            // 🌟 ZIRH 2: Barkodsuz ve Boş Palet Dedektörü
            List<string> hataliPaletler = new List<string>();

            foreach (DataGridViewRow row in dgvPaletler.Rows)
            {
                if (row.IsNewRow) continue;

                // NOT: Kendi dgvPaletler tablondaki sütun isimlerine göre buraları güncelle!
                string paletNo = row.Cells["PaletAdi"].Value?.ToString() ?? "Bilinmeyen Palet";
                string barkod = row.Cells["BarkodNo"].Value?.ToString();

                // Paletin içindeki ürün sayısını kontrol et (Miktar sütunu)
                int miktar = 0;
                if (row.Cells["Miktar"] != null && row.Cells["Miktar"].Value != null)
                {
                    int.TryParse(row.Cells["Miktar"].Value.ToString(), out miktar);
                }

                // KURAL 1: Barkod boş mu?
                if (string.IsNullOrWhiteSpace(barkod))
                {
                    hataliPaletler.Add($"{paletNo} (Barkodu Yok veya Yazdırılmamış)");
                }
                // KURAL 2: Palet boş mu?
                else if (miktar == 0)
                {
                    hataliPaletler.Add($"{paletNo} (İçi Boş, Ürün Eklenmemiş)");
                }
            }

            if (hataliPaletler.Count > 0)
            {
                HataSesCal();
                MessageBox.Show("DUR! Sevkiyat yapılamaz!\n\nAşağıdaki paletlerde kritik eksikler var:\n\n👉 " +
                                string.Join("\n👉 ", hataliPaletler) +
                                "\n\nLütfen hataları düzeltip tekrar deneyin.",
                                "Kritik Eksiklik", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return; // 🛑 İŞLEMİ KESER, KAYDETMEZ!
            }

            // 🌟 ZIRH 3: Çift Tıklama Koruması
            Button basilanButon = sender as Button;
            if (basilanButon != null) basilanButon.Enabled = false;

            // 🌟 HAYALET YÜKLEYİCİ: Ekran boşsa ama listede seçili bir iş varsa devreye girer
            if (dgvMalzemeler.Rows.Count == 0)
            {
                if (dgvYarimSevkler.SelectedRows.Count > 0)
                {
                    string kayitAdi = dgvYarimSevkler.SelectedRows[0].Cells["GorunenAd"].Value.ToString();
                    DialogResult otoOnay = MessageBox.Show($"Ekranda açık bir sevkiyat yok.\n\nListeden seçtiğiniz '{kayitAdi}' kaydını masaya yükleyip doğrudan TAM SEVKİYAT işlemini başlatmak ister misiniz?", "Otomatik Hızlı Sevkiyat", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                    if (otoOnay == DialogResult.Yes)
                    {
                        btnYarimAc_Click(null, null); // Ekrana saniyesinde yükle
                        if (dgvMalzemeler.Rows.Count == 0) return; // Yükleme hatası varsa devam etme
                    }
                    else return;
                }
                else return;
            }

            bool eksikVarMi = false;
            foreach (DataGridViewRow satir in dgvMalzemeler.Rows)
            {
                if (satir.IsNewRow || satir.Cells["Malzeme Kodu"].Value == null) continue;

                if (Convert.ToInt32(satir.Cells["Okutulan"].Value) < Convert.ToInt32(satir.Cells["Sipariş Adedi"].Value))
                {
                    eksikVarMi = true; break;
                }
            }

            if (eksikVarMi) MessageBox.Show("DUR! Eksik okutulmuş ürünler var, Tam Sevk yapılamaz!\nLütfen eksikleri tamamlayın veya 'Kısmi Sevk' yapın.", "Eksik Ürün", MessageBoxButtons.OK, MessageBoxIcon.Error);
            else
            {
                // Raporu çıkar
                btnSevkRaporla_Click(null, null);

                MessageBox.Show("HARİKA! Tüm ürünler eksiksiz. Tam Sevk onaylandı!", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);

                HashSet<string> bitenBelgeler = new HashSet<string>();
                foreach (DataGridViewRow satir in dgvMalzemeler.Rows)
                {
                    if (satir.Cells["Belge No"].Value != null)
                        bitenBelgeler.Add(satir.Cells["Belge No"].Value.ToString());
                }

                string birlesikBelgeIsmi = string.Join("_", bitenBelgeler);
                string secilenPalet = cmbSevkPaletSayisi.SelectedItem != null ? cmbSevkPaletSayisi.SelectedItem.ToString() : "0";
                SevkiyatArsivle(birlesikBelgeIsmi, txtMusteriAdi.Text, txtSevkMusteri.Text, "TAM_SEVK", secilenPalet);

                KaliciKaraListeyeTopluEkle(bitenBelgeler);

                foreach (string bitenBelge in bitenBelgeler)
                {
                    for (int i = dtTumSiparisler.Rows.Count - 1; i >= 0; i--)
                    {
                        if (dtTumSiparisler.Rows[i]["BelgeNo"].ToString().Trim() == bitenBelge)
                        {
                            dtTumSiparisler.Rows.RemoveAt(i);
                        }
                    }
                }
                dtTumSiparisler.AcceptChanges();

                // Ekranı temizle
                txtMusteriAdi.Clear();
                txtSevkMusteri.Clear();
                txtBarkod.Clear();
                clbBelgeNo.Items.Clear();
                cmbSevkPaletSayisi.SelectedIndex = -1;

                dgvMalzemeler.DataSource = null;

                dgvPaletMatrisi.Columns.Clear();
                dgvPaletMatrisi.Rows.Clear();
                cmbAktifPalet.Items.Clear();

                cmbMusteri.Items.Clear();
                var kalanMusteriler = dtTumSiparisler.AsEnumerable()
                                                    .Select(r => r.Field<string>("MusteriAdi")?.Trim())
                                                    .Where(m => !string.IsNullOrEmpty(m))
                                                    .Distinct()
                                                    .OrderBy(m => m)
                                                    .ToArray();
                cmbMusteri.Items.AddRange(kalanMusteriler);
            }

            KarantinayaAl(false);

        }

        // 🌟 KISMİ SEVKİYAT MOTORU (Otomatik Hayalet Yükleme Özellikli)
        private void btnKismiSevk_Click(object sender, EventArgs e)
        {

            // 🌟 ZIRH 1: Havada Kalan Verileri Tabloya Yazdır
            dgvPaletler.EndEdit();
            dgvPaletMatrisi.EndEdit();

            // 🌟 ZIRH 2: Barkodsuz ve Boş Palet Dedektörü
            List<string> hataliPaletler = new List<string>();

            foreach (DataGridViewRow row in dgvPaletler.Rows)
            {
                if (row.IsNewRow) continue;

                // NOT: Kendi dgvPaletler tablondaki sütun isimlerine göre buraları güncelle!
                string paletNo = row.Cells["PaletAdi"].Value?.ToString() ?? "Bilinmeyen Palet";
                string barkod = row.Cells["BarkodNo"].Value?.ToString();

                // Paletin içindeki ürün sayısını kontrol et (Miktar sütunu)
                int miktar = 0;
                if (row.Cells["Miktar"] != null && row.Cells["Miktar"].Value != null)
                {
                    int.TryParse(row.Cells["Miktar"].Value.ToString(), out miktar);
                }

                // KURAL 1: Barkod boş mu?
                if (string.IsNullOrWhiteSpace(barkod))
                {
                    hataliPaletler.Add($"{paletNo} (Barkodu Yok veya Yazdırılmamış)");
                }
                // KURAL 2: Palet boş mu?
                else if (miktar == 0)
                {
                    hataliPaletler.Add($"{paletNo} (İçi Boş, Ürün Eklenmemiş)");
                }
            }

            if (hataliPaletler.Count > 0)
            {
                HataSesCal();
                MessageBox.Show("DUR! Sevkiyat yapılamaz!\n\nAşağıdaki paletlerde kritik eksikler var:\n\n👉 " +
                                string.Join("\n👉 ", hataliPaletler) +
                                "\n\nLütfen hataları düzeltip tekrar deneyin.",
                                "Kritik Eksiklik", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return; // 🛑 İŞLEMİ KESER, KAYDETMEZ!
            }

            // 🌟 ZIRH 3: Çift Tıklama Koruması
            Button basilanButon = sender as Button;
            if (basilanButon != null) basilanButon.Enabled = false;

            // 🌟 HAYALET YÜKLEYİCİ: Ekran boşsa ama listede seçili bir iş varsa devreye girer
            if (dgvMalzemeler.Rows.Count == 0)
            {
                if (dgvYarimSevkler.SelectedRows.Count > 0)
                {
                    string kayitAdi = dgvYarimSevkler.SelectedRows[0].Cells["GorunenAd"].Value.ToString();
                    DialogResult otoOnay = MessageBox.Show($"Ekranda açık bir sevkiyat yok.\n\nListeden seçtiğiniz '{kayitAdi}' kaydını masaya yükleyip doğrudan KISMİ SEVKİYAT işlemini başlatmak ister misiniz?", "Otomatik Hızlı Sevkiyat", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                    if (otoOnay == DialogResult.Yes)
                    {
                        btnYarimAc_Click(null, null); // Ekrana saniyesinde yükle
                        if (dgvMalzemeler.Rows.Count == 0) return; // Yükleme hatası varsa devam etme
                    }
                    else return;
                }
                else return;
            }

            List<string> eksikListesi = new List<string>();
            foreach (DataGridViewRow satir in dgvMalzemeler.Rows)
            {
                if (satir.IsNewRow || satir.Cells["Malzeme Kodu"].Value == null) continue;

                int siparis = Convert.ToInt32(satir.Cells["Sipariş Adedi"].Value);
                int okutulan = Convert.ToInt32(satir.Cells["Okutulan"].Value);

                if (okutulan < siparis)
                {
                    eksikListesi.Add($"- {satir.Cells["Malzeme Kodu"].Value} | Gerekli: {siparis}, Okutulan: {okutulan}");
                }
            }

            if (eksikListesi.Count > 0)
            {
                if (MessageBox.Show("Eksik ürünler var. Yine de Kısmi Sevk yapılsın mı?\n\nEksikler:\n" + string.Join("\n", eksikListesi), "Kısmi Sevk Onayı", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    // Raporu çıkar
                    btnSevkRaporla_Click(null, null);

                    MessageBox.Show("Kısmi Sevk onaylandı!", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    HashSet<string> bitenBelgeler = new HashSet<string>();
                    foreach (DataGridViewRow satir in dgvMalzemeler.Rows)
                    {
                        if (satir.Cells["Belge No"].Value != null)
                            bitenBelgeler.Add(satir.Cells["Belge No"].Value.ToString());
                    }

                    string birlesikBelgeIsmi = string.Join("_", bitenBelgeler);
                    string secilenPalet = cmbSevkPaletSayisi.SelectedItem != null ? cmbSevkPaletSayisi.SelectedItem.ToString() : "0";
                    SevkiyatArsivle(birlesikBelgeIsmi, txtMusteriAdi.Text, txtSevkMusteri.Text, "KISMI_SEVK", secilenPalet);

                    KaliciKaraListeyeTopluEkle(bitenBelgeler);

                    foreach (string bitenBelge in bitenBelgeler)
                    {
                        for (int i = dtTumSiparisler.Rows.Count - 1; i >= 0; i--)
                        {
                            if (dtTumSiparisler.Rows[i]["BelgeNo"].ToString().Trim() == bitenBelge)
                            {
                                dtTumSiparisler.Rows.RemoveAt(i);
                            }
                        }
                    }
                    dtTumSiparisler.AcceptChanges();

                    // Ekranı temizle
                    txtMusteriAdi.Clear();
                    txtSevkMusteri.Clear();
                    txtBarkod.Clear();
                    clbBelgeNo.Items.Clear();

                    cmbSevkPaletSayisi.SelectedIndex = -1;

                    dgvMalzemeler.DataSource = null;

                    dgvPaletMatrisi.Columns.Clear();
                    dgvPaletMatrisi.Rows.Clear();
                    cmbAktifPalet.Items.Clear();

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
                MessageBox.Show("Hiçbir ürün eksik değil! Lütfen siparişi bitirmek için 'Tam Sevket' butonunu kullanın.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            KarantinayaAl(false);

        }

        #endregion

        #region 🧠 13.7 KALICI HAFIZA MOTORU (TXT)

        // 🌟 YENİ: Toplu Ekleme Motoru (Diski yormaz, tek seferde yazar)
        private void KaliciKaraListeyeTopluEkle(IEnumerable<string> belgeler)
        {
            List<string> eklenecekler = new List<string>();
            foreach (string b in belgeler)
            {
                string temiz = b.Trim();
                if (!string.IsNullOrEmpty(temiz) && !TamamlananBelgeNolar.Contains(temiz))
                {
                    TamamlananBelgeNolar.Add(temiz);
                    eklenecekler.Add(temiz);
                }
            }

            if (eklenecekler.Count > 0)
            {
                File.AppendAllLines(KaraListeDosyaYolu(), eklenecekler);
            }
        }

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

        #region 🔌 13.8 ENDÜSTRİYEL BARKOD OKUYUCU (COM PORT / ARKA PLAN DİNLEME)

        // 1. GLOBAL DEĞİŞKENLER (Port motoru ve hafıza havuzu)
        private SerialPort barkodPort = new SerialPort();
        private string portBuffer = "";

        // 2. OTOMATİK BAĞLANMA MOTORU (Bunu MainForm_Load metodunun içine çağıracaksın veya içindekileri oraya yapıştıracaksın)
        private void OtomatikPortBaglantisiBaslat()
        {
            if (cmbComPort != null)
            {
                cmbComPort.Items.Clear();
                cmbComPort.Items.AddRange(SerialPort.GetPortNames());

                // Hafızadan (txt dosyasından) son seçili portu oku
                string appDataYol = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TamgaApp");
                if (!Directory.Exists(appDataYol)) Directory.CreateDirectory(appDataYol);

                string ayarDosyasi = Path.Combine(appDataYol, "ComPortAyari.txt");

                if (System.IO.File.Exists(ayarDosyasi))
                {
                    string kayitliPort = System.IO.File.ReadAllText(ayarDosyasi).Trim();

                    // Eğer o port şu an bilgisayara takılıysa otomatik seç ve Bağlan'a tıkla!
                    if (cmbComPort.Items.Contains(kayitliPort))
                    {
                        cmbComPort.SelectedItem = kayitliPort;
                        btnComBaglan.PerformClick(); // Gizlice butona basar
                    }
                    else if (cmbComPort.Items.Count > 0) cmbComPort.SelectedIndex = 0;
                }
                else if (cmbComPort.Items.Count > 0) cmbComPort.SelectedIndex = 0;
            }
        }

        // 3. BAĞLAN BUTONU VE HAFIZAYA KAYDETME
        private void btnComBaglan_Click(object sender, EventArgs e)
        {
            if (barkodPort.IsOpen)
            {
                barkodPort.Close();
                btnComBaglan.Text = "Okuyucuya Bağlan";
                btnComBaglan.BackColor = Color.Gray;
            }
            else
            {
                if (cmbComPort.SelectedItem == null)
                {
                    MessageBox.Show("Lütfen bir COM Port seçin!", "Uyarı");
                    return;
                }

                try
                {
                    barkodPort.PortName = cmbComPort.SelectedItem.ToString();
                    barkodPort.BaudRate = 9600;
                    barkodPort.Parity = Parity.None;
                    barkodPort.DataBits = 8;
                    barkodPort.StopBits = StopBits.One;

                    barkodPort.DataReceived -= BarkodPort_DataReceived;
                    barkodPort.DataReceived += BarkodPort_DataReceived;

                    barkodPort.Open();
                    btnComBaglan.Text = "Bağlantı Aktif (Dinleniyor...)";
                    btnComBaglan.BackColor = Color.MediumSeaGreen;

                    // 🌟 HAFIZAYA KAYIT: Başarılı bağlandıysa portu txt dosyasına yaz (Bir sonraki açılış için)
                    string appDataYol = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TamgaApp");
                    if (!Directory.Exists(appDataYol)) Directory.CreateDirectory(appDataYol);

                    string ayarDosyasi = Path.Combine(appDataYol, "ComPortAyari.txt");
                    System.IO.File.WriteAllText(ayarDosyasi, cmbComPort.SelectedItem.ToString());
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Bağlantı Hatası! Cihazın takılı olduğundan ve COM modunda olduğundan emin olun.\nDetay: " + ex.Message, "Kritik Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // 4. ARKA PLAN DİNLEYİCİSİ VE "TRAFİK POLİSİ" YÖNLENDİRMESİ
        private void BarkodPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            string gelenVeri = barkodPort.ReadExisting();
            portBuffer += gelenVeri;

            // Okuyucu "Enter" gönderdiğinde barkod tamamlanmıştır
            if (portBuffer.Contains("\r") || portBuffer.Contains("\n"))
            {
                string islenecekHamVeri = portBuffer.Replace("\r", "").Replace("\n", "").Trim();
                portBuffer = ""; // Havuzu sıfırla

                if (string.IsNullOrEmpty(islenecekHamVeri)) return;

                // UI Thread'e (Arayüze) geçiş yap
                this.BeginInvoke(new Action(() =>
                {
                    // 🌟 ÖNCELİK 1: EĞER KAMYON KIOSK EKRANI AÇIKSA, BARKODU ORAYA GÖNDER!
                    // Bu sayede COM Port okuyucu Kiosk ekranında "dını dını" etmeden şıkır şıkır çalışır.
                    FrmKamyonKiosk acikKiosk = Application.OpenForms.OfType<FrmKamyonKiosk>().FirstOrDefault();
                    if (acikKiosk != null)
                    {
                        acikKiosk.DisaridanBarkodGeldi(islenecekHamVeri);
                        return; // Kiosk açıkken ana formdaki işlemleri durdur
                    }

                    // Kiosk açık değilse normal sekmelere bak
                    string aktifSekme = tabControl1.SelectedTab.Text;

                    if (aktifSekme == "Sevkiyat")
                    {
                        SevkiyatIcinBarkodIsle(islenecekHamVeri);
                    }
                    else if (aktifSekme == "Depo Sayım")
                    {
                        // Sayım için eklenecek kodlar
                    }
                    else
                    {
                        // Alakasız bir sekmedeyse hata sesi ver ve hiçbir şeyi bozma
                        HataSesCal();
                    }
                }));
            }
        }

        // 5. SEVKİYAT MOTORU ("AKILLI BÖLÜCÜ" VE "FİFO PALETLEME")
        private void SevkiyatIcinBarkodIsle(string hamVeri)
        {
            // 🌟 AKILLI BÖLÜCÜ: Peş peşe çok hızlı okunan (Örn: 26 hane) barkodları 13'erli parçala ve kaybetme
            List<string> islenecekBarkodlar = new List<string>();

            if (hamVeri.Length > 13 && hamVeri.Length % 13 == 0)
            {
                for (int i = 0; i < hamVeri.Length; i += 13) islenecekBarkodlar.Add(hamVeri.Substring(i, 13));
            }
            else if (hamVeri.Length > 13) islenecekBarkodlar.Add(hamVeri.Substring(0, 13));
            else islenecekBarkodlar.Add(hamVeri);

            // Her bir parçalanmış barkodu sırayla palete işle
            foreach (string okutulanBarkod in islenecekBarkodlar)
            {
                if (cmbAktifPalet.SelectedItem == null)
                {
                    System.Media.SystemSounds.Hand.Play(); // Ekrana kutu çıkartma, sadece hata sesi ver
                    break;
                }

                int aktifPaletSutunIndex = cmbAktifPalet.SelectedIndex;
                bool urunBulundu = false;
                DataGridViewRow hedefSatir = null;

                // FİFO Mantığı: İlk boş satırı bul
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

                            if (oku < sip) { hedefSatir = satir; break; }
                        }
                    }
                }

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

                    // 🌟 PALETE EKLEME İŞLEMİ (RENK KÖRÜ ZIRHI EKLENDİ)
                    string urunAdi = hedefSatir.Cells["Malzeme Adı"].Value.ToString();
                    string aciklama = hedefSatir.Cells["Açıklama"].Value?.ToString().Trim() ?? ""; // 🌟 YENİ: Rengi Çek
                    string aitOlduguBelge = hedefSatir.Cells["Belge No"].Value.ToString();
                    string malzemeKodu = hedefSatir.Cells["Malzeme Kodu"].Value.ToString().Trim();

                    // 🌟 YENİ: Açıklama (Renk) boş değilse, ürün adının yanına köşeli parantezle ekle
                    string tamUrunAdi = string.IsNullOrWhiteSpace(aciklama) ? urunAdi : $"{urunAdi} [{aciklama}]";

                    bool paletSutunundaVarMi = false;

                    foreach (DataGridViewRow paletSatiri in dgvPaletMatrisi.Rows)
                    {
                        if (paletSatiri.Cells[aktifPaletSutunIndex].Value != null)
                        {
                            string hucreMetni = paletSatiri.Cells[aktifPaletSutunIndex].Value.ToString();

                            // 🌟 YENİ ZIRH: Artık birleştirme yaparken Rengin de aynı olup olmadığına bakıyor!
                            if (hucreMetni.Contains(malzemeKodu) && hucreMetni.Contains(aitOlduguBelge) && (string.IsNullOrWhiteSpace(aciklama) || hucreMetni.Contains(aciklama)))
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
                                paletSatiri.Cells[aktifPaletSutunIndex].Value = $"{malzemeKodu} - {tamUrunAdi} ({aitOlduguBelge}) | Adet: 1";
                                bosHucreBulundu = true; break;
                            }
                        }

                        if (!bosHucreBulundu)
                        {
                            int yeniSatirIndex = dgvPaletMatrisi.Rows.Add();
                            dgvPaletMatrisi.Rows[yeniSatirIndex].Cells[aktifPaletSutunIndex].Value = $"{malzemeKodu} - {tamUrunAdi} ({aitOlduguBelge}) | Adet: 1";
                        }
                    }
                }
                else
                {
                    if (!urunBulundu)
                    {
                        try { string wavYolu = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "hata.wav"); if (System.IO.File.Exists(wavYolu)) new System.Media.SoundPlayer(wavYolu).Play(); else System.Media.SystemSounds.Hand.Play(); } catch { }
                    }
                }
            }
        }
        #endregion

        #region 📦 13.9 TÜM BELGELERİ SEÇ VE FIFO SIRALA (KONSOLİDASYON)

        private void btnTumBelgeleriSec_Click(object sender, EventArgs e)
        {
            if (cmbMusteri.SelectedItem == null) { MessageBox.Show("Lütfen önce bir Müşteri seçin!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (clbBelgeNo.Items.Count == 0) { MessageBox.Show("Seçilen müşteriye ait Belge No bulunamadı.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }

            DialogResult onay = MessageBox.Show("Bu müşteriye ait TÜM SİPARİŞLER seçilecek ve sıralanacaktır.\n\nEmin misiniz?", "Tümünü Seç Onayı", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (onay == DialogResult.Yes)
            {
                for (int i = 0; i < clbBelgeNo.Items.Count; i++) clbBelgeNo.SetItemChecked(i, true);

                // 🌟 SİHİRLİ ZIRH: Mevcut Ekrandaki Geçmişi Hafızaya Al!
                Dictionary<string, int> oncekiOkutulanlar = new Dictionary<string, int>();
                if (dgvMalzemeler.Rows.Count > 0)
                {
                    foreach (DataGridViewRow row in dgvMalzemeler.Rows)
                    {
                        if (row.IsNewRow || row.Cells["Malzeme Kodu"].Value == null) continue;
                        string bNo = row.Cells["Belge No"].Value.ToString();
                        string mKodu = row.Cells["Malzeme Kodu"].Value.ToString();
                        string aciklama = row.Cells["Açıklama"].Value?.ToString() ?? "";

                        // 🌟 ZIRH: Sipariş adedi kaldırıldı! Sadece Belge, Kod ve Açıklamaya(Renge) bakar.
                        string anahtar = $"{bNo}_{mKodu}_{aciklama}";

                        int okutulan = 0;
                        if (row.Cells["Okutulan"].Value != null) int.TryParse(row.Cells["Okutulan"].Value.ToString(), out okutulan);

                        if (okutulan > 0)
                        {
                            if (!oncekiOkutulanlar.ContainsKey(anahtar)) oncekiOkutulanlar[anahtar] = okutulan;
                            else oncekiOkutulanlar[anahtar] += okutulan; // Ezme, üstüne topla!
                        }
                    }
                }

                DataTable dtEkran = new DataTable();
                dtEkran.Columns.Add("Belge No", typeof(string));
                dtEkran.Columns.Add("Malzeme Kodu", typeof(string));
                dtEkran.Columns.Add("Barkod", typeof(string));
                dtEkran.Columns.Add("Malzeme Adı", typeof(string));
                dtEkran.Columns.Add("Açıklama", typeof(string));
                dtEkran.Columns.Add("Sipariş Adedi", typeof(int));
                dtEkran.Columns.Add("Okutulan", typeof(int));

                var yerelUrunler = DataAccess.GetAllUrunler();
                string ilkBelge = clbBelgeNo.Items[0].ToString();
                DataRow[] ilkBelgeSatirlari = dtTumSiparisler.Select($"BelgeNo LIKE '%{ilkBelge}%'");
                if (ilkBelgeSatirlari.Length > 0)
                {
                    txtMusteriAdi.Text = ilkBelgeSatirlari[0]["MusteriAdi"].ToString().Trim();
                    txtSevkMusteri.Text = ilkBelgeSatirlari[0]["SevkMusteri"].ToString().Trim();
                }

                var siraliBelgeler = clbBelgeNo.CheckedItems.Cast<string>().OrderBy(b => b.Trim()).ToList();
                foreach (string secilenBelge in siraliBelgeler)
                {
                    DataRow[] filtrelenmisSatirlar = dtTumSiparisler.Select($"BelgeNo LIKE '%{secilenBelge}%'");
                    foreach (DataRow satir in filtrelenmisSatirlar)
                    {
                        string malzemeKodu = satir["Malzeme"].ToString().Trim();
                        string siparisRengi = satir["SecenekAciklamasi"].ToString().Trim();
                        if (string.IsNullOrEmpty(siparisRengi) || siparisRengi.Equals("BEYAZ", StringComparison.OrdinalIgnoreCase)) siparisRengi = "Beyaz";

                        var urun = yerelUrunler.FirstOrDefault(u => u.UrunKodu == malzemeKodu && u.Renk.Equals(siparisRengi, StringComparison.OrdinalIgnoreCase));
                        if (urun == null) urun = yerelUrunler.FirstOrDefault(u => u.UrunKodu == malzemeKodu);

                        string barkod = urun != null && !string.IsNullOrWhiteSpace(urun.Barkod) ? urun.Barkod : "BARKOD YOK";

                        int sAdet = Convert.ToInt32(Convert.ToDecimal(satir["Bakiye"]));
                        int yazilacakOkutulan = 0;

                        // 🌟 HAVUZDAN DAĞITMA MANTIĞI (Sipariş adedi kaldırıldı)
                        string anahtar = $"{secilenBelge}_{malzemeKodu}_{siparisRengi}";
                        if (oncekiOkutulanlar.ContainsKey(anahtar) && oncekiOkutulanlar[anahtar] > 0)
                        {
                            yazilacakOkutulan = Math.Min(sAdet, oncekiOkutulanlar[anahtar]);
                            oncekiOkutulanlar[anahtar] -= yazilacakOkutulan; // Havuzdan düş ki diğerine kalsın
                        }

                        dtEkran.Rows.Add(secilenBelge, malzemeKodu, barkod, satir["MalzemeAdi"].ToString().Trim(), satir["SecenekAciklamasi"].ToString().Trim(), sAdet, yazilacakOkutulan);
                    }
                }

                dgvMalzemeler.DataSource = dtEkran;
                dgvMalzemeler.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

                MessageBox.Show("Tüm belgeler eklendi ve sıralandı! Önceki okutulan ürünleriniz korundu.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        // 🌟 BOYA MOTORU (Sıralamada Renklerin Kaybolmasını Engeller)
        private void dgvMalzemeler_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex >= 0 && e.RowIndex < dgvMalzemeler.Rows.Count)
            {
                DataGridViewRow row = dgvMalzemeler.Rows[e.RowIndex];
                if (row.IsNewRow) return;

                int sip = 0, oku = 0;
                if (row.Cells["Sipariş Adedi"].Value != null) int.TryParse(row.Cells["Sipariş Adedi"].Value.ToString(), out sip);
                if (row.Cells["Okutulan"].Value != null) int.TryParse(row.Cells["Okutulan"].Value.ToString(), out oku);

                if (oku >= sip && sip > 0) row.DefaultCellStyle.BackColor = Color.LightGreen;
                else if (oku > 0) row.DefaultCellStyle.BackColor = Color.LightYellow;
                else row.DefaultCellStyle.BackColor = Color.White;
            }
        }

        #endregion

        #endregion

        // =========================================================================================

        #region 📊 14. DEPO SAYIM VE ENVANTER KONTROLÜ

        #region 📋 14.1 SAYIM TABLOSU VE İLK AYARLAR

        private void SayimSisteminiHazirla()
        {
            if (dgvSayim == null) return;

            // Ekranda eski ne varsa tamamen uçur
            dgvSayim.Columns.Clear();

            // 🌟 Sütunları BAŞLIKLARIYLA beraber kalıcı olarak ekle
            dgvSayim.Columns.Add("Barkod", "Barkod");
            dgvSayim.Columns.Add("Malzeme Kodu", "Malzeme Kodu");
            dgvSayim.Columns.Add("Açıklama", "Açıklama");
            dgvSayim.Columns.Add("Renk", "Renk");
            dgvSayim.Columns.Add("SistemStogu", "Sistem Stoğu"); // 🌟 CANLI STOK SÜTUNU EKLENDİ
            dgvSayim.Columns.Add("Adet", "Sayım Adedi");         // 🌟 KAFA KARIŞMAMASI İÇİN İSMİ NETLEŞTİRİLDİ

            // Güvenlik: "Sayım Adedi" hariç her yeri kilitle (Personel stoğu veya ismi yanlışlıkla değiştiremesin)
            dgvSayim.Columns["Barkod"].ReadOnly = true;
            dgvSayim.Columns["Malzeme Kodu"].ReadOnly = true;
            dgvSayim.Columns["Açıklama"].ReadOnly = true;
            dgvSayim.Columns["Renk"].ReadOnly = true;
            dgvSayim.Columns["SistemStogu"].ReadOnly = true; // 🌟 KİLİTLENDİ
            dgvSayim.Columns["Adet"].ReadOnly = false;

            // 🌟 GÖRSEL ZIRH: Sistem Stoğu sütunu ekranda kabak gibi belli olsun diye özel renklendirildi!
            dgvSayim.Columns["SistemStogu"].DefaultCellStyle.BackColor = Color.LightCyan;
            dgvSayim.Columns["SistemStogu"].DefaultCellStyle.ForeColor = Color.DarkBlue;
            dgvSayim.Columns["SistemStogu"].DefaultCellStyle.Font = new Font("Segoe UI", 11, FontStyle.Bold);

            // "Sayım Adedi" sütununu da hafif belirgin yapalım ki personel nereye veri gireceğini anlasın
            dgvSayim.Columns["Adet"].DefaultCellStyle.BackColor = Color.LightYellow;
            dgvSayim.Columns["Adet"].DefaultCellStyle.Font = new Font("Segoe UI", 11, FontStyle.Bold);

            dgvSayim.AllowUserToAddRows = false;
            dgvSayim.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvSayim.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill; // Tam ekrana yay
        }

        #endregion

        #region 🔍 14.2 ANLIK BARKOD OKUTMA VE ADET ARTTIRMA
        private void TxtSayimBarkod_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true; // Bip sesini kes
                string okunanBarkod = txtSayimBarkod.Text.Trim();

                // 🛡️ Çift okuma / uzun gelme zırhı (26 hane olayını keser)
                if (okunanBarkod.Length > 13) okunanBarkod = okunanBarkod.Substring(0, 13);

                if (!string.IsNullOrEmpty(okunanBarkod))
                {
                    bool urunZatenVarMi = false;

                    foreach (DataGridViewRow row in dgvSayim.Rows)
                    {
                        string tablodakiBarkod = row.Cells["Barkod"].Value?.ToString() ?? "";
                        string tablodakiKodu = row.Cells["Malzeme Kodu"].Value?.ToString() ?? "";

                        // Hem barkod hem malzeme kodu eşleşmesi arıyoruz
                        if (tablodakiBarkod == okunanBarkod || tablodakiKodu == okunanBarkod)
                        {
                            int mevcutAdet = Convert.ToInt32(row.Cells["Adet"].Value);
                            row.Cells["Adet"].Value = mevcutAdet + 1;
                            urunZatenVarMi = true;

                            row.Selected = true;
                            dgvSayim.FirstDisplayedScrollingRowIndex = row.Index;
                            break;
                        }
                    }

                    if (!urunZatenVarMi)
                    {
                        // 🌟 SİHİRLİ DOKUNUŞ: Veritabanından Hem Barkoda Hem Koda Göre Tarama
                        var tumUrunler = DataAccess.GetAllUrunler();
                        Urun bulunanUrun = tumUrunler.FirstOrDefault(u => u.Barkod == okunanBarkod || u.UrunKodu == okunanBarkod);

                        string malzemeKodu = bulunanUrun != null && !string.IsNullOrEmpty(bulunanUrun.UrunKodu) ? bulunanUrun.UrunKodu : okunanBarkod;
                        string aciklama = bulunanUrun != null && !string.IsNullOrEmpty(bulunanUrun.Aciklama) ? bulunanUrun.Aciklama : "SİSTEMDE KAYITLI DEĞİL!";
                        string renk = bulunanUrun != null && !string.IsNullOrEmpty(bulunanUrun.Renk) ? bulunanUrun.Renk : "";
                        string barkod = bulunanUrun != null && !string.IsNullOrEmpty(bulunanUrun.Barkod) ? bulunanUrun.Barkod : okunanBarkod;

                        int yeniSatir = dgvSayim.Rows.Add(barkod, malzemeKodu, aciklama, renk, 1);
                        dgvSayim.Rows[yeniSatir].Selected = true;
                        dgvSayim.FirstDisplayedScrollingRowIndex = yeniSatir;
                    }
                }

                txtSayimBarkod.Clear();
                this.BeginInvoke(new Action(() =>
                {
                    txtSayimBarkod.Focus();
                }));
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

            // Dosya ismini tarih, saat ve rapor adı kombinasyonuyla eşsiz hale getir (Örn: 2026-07-23_1430_Sayim.csv)
            string dosyaAdi = $"{DateTime.Now:yyyy-MM-dd_HHmm}_{raporIsmi}.csv";
            string tamYol = Path.Combine(anaYol, dosyaAdi);

            // Verileri Excel ve notepad ile uyumlu olacak şekilde UTF8 formatında satır satır yaz dök
            using (StreamWriter sw = new StreamWriter(tamYol, false, System.Text.Encoding.UTF8))
            {
                sw.WriteLine("Barkod;Malzeme Kodu;Açıklama;Renk;Adet");
                foreach (DataGridViewRow row in dgvSayim.Rows)
                {
                    if (row.Cells[0].Value != null)
                    {
                        sw.WriteLine($"{row.Cells[0].Value};{row.Cells[1].Value};{row.Cells[2].Value};{row.Cells[3].Value};{row.Cells[4].Value}");
                    }
                }
            }

            MessageBox.Show($"Sayım başarıyla tamamlandı ve arşivlendi!\nKayıt Yeri: {tamYol}", "İşlem Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);

            // Yeni sayım operasyonu için ekranı sıfırla ve geçmiş listesini tazele
            dgvSayim.Rows.Clear();
            txtSayimRaporAdi.Clear();
            BtnSayimYenile_Click(null, null);
        }

        // Arşiv klasöründeki eski sayımları tarar ve oluşturulma Yıl/Ay hiyerarşisinde ağaca (TreeView) dizer
        private void BtnSayimYenile_Click(object sender, EventArgs e)
        {
            string anaYol = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "TamgaApp Sayım Raporları");

            // Ağacı temizle
            if (tvSayimRaporlari != null) tvSayimRaporlari.Nodes.Clear();

            if (!Directory.Exists(anaYol)) return;

            // Kök klasörü ekle
            TreeNode kok = new TreeNode("📋 Sayım Arşivi") { Tag = "KOK" };
            tvSayimRaporlari.Nodes.Add(kok);

            DirectoryInfo di = new DirectoryInfo(anaYol);
            FileInfo[] raporlar = di.GetFiles("*.csv").OrderByDescending(f => f.CreationTime).ToArray();

            // Sayım dosyalarını Yıl ve Ay klasörlerine bölüştür
            foreach (var dosya in raporlar)
            {
                string dosyaAdi = dosya.Name;
                string dYil = "Diğer";
                string dAy = "Diğer";

                // Bizim formatımız: 2026-07-23_1430_RaporAdi.csv
                string[] parcalar = dosyaAdi.Split('-');
                if (parcalar.Length >= 3 && dosyaAdi.Length > 10)
                {
                    dYil = dosyaAdi.Substring(0, 4); // Yıl (Örn: 2026)
                    dAy = dosyaAdi.Substring(5, 2);  // Ay (Örn: 07)
                }
                else
                {
                    dYil = dosya.CreationTime.ToString("yyyy");
                    dAy = dosya.CreationTime.ToString("MM");
                }

                // Ağaçta Yıl ve Ay klasörleri var mı bak, yoksa aç
                TreeNode yilNode = kok.Nodes.Cast<TreeNode>().FirstOrDefault(n => n.Text == dYil) ?? kok.Nodes.Add(dYil, dYil);
                TreeNode ayNode = yilNode.Nodes.Cast<TreeNode>().FirstOrDefault(n => n.Text == dAy) ?? yilNode.Nodes.Add(dAy, dAy);

                // Raporu Ay klasörünün içine ekle
                ayNode.Nodes.Add(new TreeNode("📄 " + dosyaAdi) { Tag = dosya.FullName, ForeColor = Color.DarkRed });
            }

            kok.Expand(); // İlk açılışta sadece ana kök açık dursun
        }

        // Ağaçtan seçilen eski bir sayım raporunu okur, dinamik olarak yeni bir popup form oluşturur ve verileri canlı filtreli şekilde sunar.
        private void BtnSayimAc_Click(object sender, EventArgs e)
        {
            // ListBox iptal, artık TreeView üzerinden .csv uzantılı dosya seçilmiş mi diye bakıyoruz
            if (tvSayimRaporlari.SelectedNode == null || tvSayimRaporlari.SelectedNode.Tag == null || !tvSayimRaporlari.SelectedNode.Tag.ToString().EndsWith(".csv"))
            {
                MessageBox.Show("Lütfen açmak için ağaçtan bir sayım dosyası (📄) seçin!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string dosyaYolu = tvSayimRaporlari.SelectedNode.Tag.ToString();
            FileInfo secilenDosya = new FileInfo(dosyaYolu);

            // Dinamik Popup Form Kurulumu
            Form frm = new Form { Text = "Sayım Raporu Detayı: " + secilenDosya.Name, Size = new Size(1000, 700), StartPosition = FormStartPosition.CenterScreen, Icon = this.Icon };

            // Canlı Filtreleme Paneli (Üst Kısım)
            Panel pnlUst = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.FromArgb(15, 76, 58) };
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
                string[] basliklar = satirlar[0].Split(';');
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

        // O anki sevkiyatta erken basılan palet barkodlarını hafızada tutar (Palet No -> EAN13)
        private Dictionary<string, string> aktifPaletBarkodlari = new Dictionary<string, string>();

       // 🌟 AKILLI PALET SAYISI DEĞİŞTİRME MOTORU (Veri Kaybını Önler)

        private void cmbSevkPaletSayisi_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbSevkPaletSayisi.SelectedIndex == -1) return;

            int yeniPaletSayisi = Convert.ToInt32(cmbSevkPaletSayisi.SelectedItem);
            int mevcutPaletSayisi = dgvPaletMatrisi.Columns.Count;

            // 1. EĞER MATRİS HİÇ YOKSA (İLK AÇILIŞ), DİREKT OLUŞTUR
            if (mevcutPaletSayisi == 0)
            {
                for (int i = 1; i <= yeniPaletSayisi; i++)
                {
                    dgvPaletMatrisi.Columns.Add($"Palet{i}", $"{i}. Palet");
                    cmbAktifPalet.Items.Add($"{i}. Palet");
                }
                if (cmbAktifPalet.Items.Count > 0) cmbAktifPalet.SelectedIndex = 0;
                return;
            }

            // Sayı değişmediyse hiçbir şey yapma
            if (yeniPaletSayisi == mevcutPaletSayisi) return;

            // 2. PALET SAYISI ARTTIYSA (Örn: 1'den 3'e) -> Sadece yeni sütun ekle, veriye dokunma!
            if (yeniPaletSayisi > mevcutPaletSayisi)
            {
                for (int i = mevcutPaletSayisi + 1; i <= yeniPaletSayisi; i++)
                {
                    dgvPaletMatrisi.Columns.Add($"Palet{i}", $"{i}. Palet");
                    cmbAktifPalet.Items.Add($"{i}. Palet");
                }
                return;
            }

            // 3. PALET SAYISI AZALDIYSA (Örn: 3'ten 1'e) -> Silinecek sütunlarda veri var mı kontrol et!
            List<DataGridViewCell> doluHucreler = new List<DataGridViewCell>();
            for (int i = yeniPaletSayisi; i < mevcutPaletSayisi; i++)
            {
                foreach (DataGridViewRow row in dgvPaletMatrisi.Rows)
                {
                    if (row.Cells[i].Value != null && !string.IsNullOrWhiteSpace(row.Cells[i].Value.ToString()))
                    {
                        doluHucreler.Add(row.Cells[i]); // İptal edilen paletteki dolu ürünleri hafızaya al
                    }
                }
            }

            // EĞER SİLİNECEK PALETLERDE ÜRÜN VARSA, PATRONA NE YAPACAĞINI SOR
            if (doluHucreler.Count > 0)
            {
                Form frmSoru = new Form
                {
                    Text = "⚠️ Dikkat: Dolu Paletler Siliniyor!",
                    Size = new Size(460, 310),
                    StartPosition = FormStartPosition.CenterParent,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    MaximizeBox = false,
                    MinimizeBox = false,
                    BackColor = Color.WhiteSmoke,
                    Icon = this.Icon
                };

                Label lblUyari = new Label
                {
                    Text = $"İptal etmek istediğiniz paletlerde toplam {doluHucreler.Count} adet okutulmuş satır bulunuyor.\nBu ürünlere ne yapılsın?",
                    Location = new Point(20, 20),
                    Size = new Size(400, 45),
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    ForeColor = Color.DarkRed
                };

                RadioButton rbSil = new RadioButton { Text = "🗑️ Ürünleri tamamen SİL (Sol tablodaki okutulandan düşer)", Location = new Point(30, 75), Size = new Size(400, 25), Font = new Font("Segoe UI", 10, FontStyle.Bold), Checked = true };
                RadioButton rbAktar = new RadioButton { Text = "📦 Ürünleri sağ kalan başka bir palete AKTAR", Location = new Point(30, 110), Size = new Size(400, 25), Font = new Font("Segoe UI", 10, FontStyle.Bold) };

                ComboBox cmbHedefPalet = new ComboBox { Location = new Point(60, 140), Size = new Size(200, 25), DropDownStyle = ComboBoxStyle.DropDownList, Enabled = false, Font = new Font("Segoe UI", 10) };
                for (int i = 1; i <= yeniPaletSayisi; i++) cmbHedefPalet.Items.Add($"{i}. Palet");
                if (cmbHedefPalet.Items.Count > 0) cmbHedefPalet.SelectedIndex = 0;

                rbAktar.CheckedChanged += (s, ev) => { cmbHedefPalet.Enabled = rbAktar.Checked; };

                Button btnOnayla = new Button { Text = "✅ UYGULA", Location = new Point(20, 200), Size = new Size(400, 45), BackColor = Color.Teal, ForeColor = Color.White, Font = new Font("Segoe UI", 11, FontStyle.Bold), Cursor = Cursors.Hand };

                btnOnayla.Click += (s, ev) =>
                {
                    if (rbAktar.Checked)
                    {
                        // 🌟 AKTARMA İŞLEMİ: Silinen palettekileri seçili yeni palete taşır
                        int hedefSutunIndex = cmbHedefPalet.SelectedIndex;
                        foreach (var hucre in doluHucreler)
                        {
                            bool yerlestirildi = false;
                            foreach (DataGridViewRow row in dgvPaletMatrisi.Rows)
                            {
                                if (row.Cells[hedefSutunIndex].Value == null || string.IsNullOrWhiteSpace(row.Cells[hedefSutunIndex].Value.ToString()))
                                {
                                    row.Cells[hedefSutunIndex].Value = hucre.Value;
                                    yerlestirildi = true;
                                    break;
                                }
                            }
                            if (!yerlestirildi)
                            {
                                int yeniSatirIndex = dgvPaletMatrisi.Rows.Add();
                                dgvPaletMatrisi.Rows[yeniSatirIndex].Cells[hedefSutunIndex].Value = hucre.Value;
                            }
                        }
                    }
                    else
                    {
                        // 🌟 SİLME İŞLEMİ: Ana tablodan (dgvMalzemeler) iptal edilenlerin adetini düşer
                        foreach (var hucre in doluHucreler)
                        {
                            string hamVeri = hucre.Value.ToString();
                            string[] parcalar = hamVeri.Split(new string[] { " | Adet: " }, StringSplitOptions.None);

                            if (parcalar.Length == 2)
                            {
                                string urunVeBelge = parcalar[0];
                                int iptalAdet = 0;
                                int.TryParse(parcalar[1], out iptalAdet);

                                string malzemeKodu = urunVeBelge;
                                int parantezIndex = urunVeBelge.LastIndexOf('(');
                                if (parantezIndex > 0) malzemeKodu = urunVeBelge.Substring(0, parantezIndex).Trim();

                                foreach (DataGridViewRow anaRow in dgvMalzemeler.Rows)
                                {
                                    if (anaRow.IsNewRow || anaRow.Cells["Malzeme Kodu"].Value == null) continue;

                                    if (anaRow.Cells["Malzeme Kodu"].Value.ToString().Trim() == malzemeKodu)
                                    {
                                        int okutulan = 0;
                                        if (anaRow.Cells["Okutulan"].Value != null) int.TryParse(anaRow.Cells["Okutulan"].Value.ToString(), out okutulan);

                                        okutulan -= iptalAdet; // Adeti geri al
                                        if (okutulan < 0) okutulan = 0;
                                        anaRow.Cells["Okutulan"].Value = okutulan;

                                        // Renkleri yeniden ayarla
                                        int siparisAdedi = Convert.ToInt32(anaRow.Cells["Sipariş Adedi"].Value);
                                        if (okutulan >= siparisAdedi) anaRow.DefaultCellStyle.BackColor = Color.LightGreen;
                                        else if (okutulan > 0) anaRow.DefaultCellStyle.BackColor = Color.LightYellow;
                                        else anaRow.DefaultCellStyle.BackColor = Color.White;

                                        break;
                                    }
                                }
                            }
                        }
                    }

                    SutunlariTemizle(mevcutPaletSayisi, yeniPaletSayisi);
                    frmSoru.Close();
                };

                frmSoru.Controls.Add(lblUyari);
                frmSoru.Controls.Add(rbSil);
                frmSoru.Controls.Add(rbAktar);
                frmSoru.Controls.Add(cmbHedefPalet);
                frmSoru.Controls.Add(btnOnayla);
                frmSoru.ShowDialog();
            }
            else
            {
                // Silinecek paletler zaten BOŞ ise hiç sormadan direkt sil
                SutunlariTemizle(mevcutPaletSayisi, yeniPaletSayisi);
            }
        }

        // 🌟 YARDIMCI METOT: Fazla sütunları matristen ve combobox'tan güvenle siler
        private void SutunlariTemizle(int mevcutSayi, int yeniSayi)
        {
            for (int i = mevcutSayi - 1; i >= yeniSayi; i--)
            {
                dgvPaletMatrisi.Columns.RemoveAt(i);
                cmbAktifPalet.Items.RemoveAt(i);
            }
            // Aktif palet, silinen bir paletse, son kalan palete geri dön
            if (cmbAktifPalet.SelectedIndex >= yeniSayi)
            {
                cmbAktifPalet.SelectedIndex = yeniSayi - 1;
            }
        }

        // 🌟 Anında Palet Etiketi Basma (TEKLİ VEYA SERİ) ve Barkod Hafızaya Alma
        private async void btnAnlikPaletEtiketi_Click(object sender, EventArgs e)
        {
            if (dgvPaletMatrisi.Columns.Count == 0)
            {
                MessageBox.Show("Yazdırılacak palet bulunamadı!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 🌟 1. KULLANICIYA NE İSTEDİĞİNİ SORUYORUZ
            DialogResult secim = MessageBox.Show(
                "Tüm paletlerin etiketlerini tek seferde SERİ yazdırmak ister misiniz?\n\n" +
                "[EVET] = Tüm Paletler (Seri Yazdırma)\n" +
                "[HAYIR] = Sadece Seçili Palet (Tekli Yazdırma)",
                "Yazdırma Türü Seçimi",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question);

            if (secim == DialogResult.Cancel) return;

            bool seriYazdir = (secim == DialogResult.Yes);

            // Eğer Tekli dediyse ama sağdan palet seçmediyse uyar
            if (!seriYazdir && cmbAktifPalet.SelectedItem == null)
            {
                HataSesCal();
                MessageBox.Show("Lütfen etiketini basmak istediğiniz aktif paleti seçin!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var yerelUrunler = DataAccess.GetAllUrunler();
            string musteriAdi = txtMusteriAdi.Text.Trim();
            string sevkMusteriAdi = txtSevkMusteri.Text.Trim();
            if (string.IsNullOrEmpty(sevkMusteriAdi)) sevkMusteriAdi = "Belirtilmedi";
            string belgeNo = string.Join(", ", clbBelgeNo.CheckedItems.Cast<string>());

            // 🌟 2. HTML BAŞLANGICI VE CSS (Sayfa Kesme Özelliği Eklendi)
            System.Text.StringBuilder html = new System.Text.StringBuilder();
            html.AppendLine(@"<html><head>   <meta charset='utf-8'>   <script src='https://cdn.jsdelivr.net/npm/jsbarcode@3.11.0/dist/JsBarcode.all.min.js'></script>   <style>      body { font-family: 'Segoe UI', Arial, sans-serif; text-align: center; margin: 0; padding: 0; }      .sayfa { width: 100%; height: 100vh; box-sizing: border-box; padding: 20px; page-break-after: always; background: white; }      .firma { font-size: 42px; font-weight: bold; text-transform: uppercase; color: black; margin-bottom: 5px; line-height: 1.1; display: -webkit-box; -webkit-line-clamp: 2; -webkit-box-orient: vertical; overflow: hidden; }      .sevk-musteri { font-size: 24px; font-weight: 600; text-transform: uppercase; color: #444; margin-bottom: 5px; }      .belge { font-size: 22px; margin-bottom: 5px; color: #333; font-weight: bold; }      .palet { font-size: 55px; margin: 10px 0; background: transparent; color: black; font-weight: bold; }      .urunler { text-align: left; font-size: 20px; font-weight: bold; border: 4px dashed black; padding: 15px; width: 98%; box-sizing: border-box; margin: 0 auto; min-height: 140px; }      ul { margin: 0; padding-left: 0; list-style-type: none; }      li { display: flex; justify-content: space-between; align-items: center; margin-bottom: 8px; border-bottom: 1.5px dashed #ccc; padding-bottom: 6px; }      .k-kod { flex: 3; text-align: left; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; padding-right: 5px; font-size: 19px; color: black; }      .k-ad { flex: 5; text-align: left; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; padding-right: 5px; color: #444; font-size: 17px; }      .k-adet { flex: 2; text-align: right; font-size: 20px; color: black; }      .barkod-alani { margin-top: 20px; }    </style></head><body>");

            // 🌟 3. HANGİ PALETLERİN YAZDIRILACAĞINI BELİRLE
            List<int> basilacakSutunlar = new List<int>();
            if (seriYazdir)
            {
                // Seri ise tüm sütunları döngüye al
                for (int j = 0; j < dgvPaletMatrisi.Columns.Count; j++) basilacakSutunlar.Add(j);
            }
            else
            {
                // Tekli ise sadece seçili sütunu al
                basilacakSutunlar.Add(cmbAktifPalet.SelectedIndex);
            }

            bool enAzBirPaletDolu = false;

            // 🌟 4. PALETLERİ OKU VE HTML'E DİZ
            foreach (int j in basilacakSutunlar)
            {
                string paletAdi = "";
                if (cmbAktifPalet.Items.Count > j) paletAdi = cmbAktifPalet.Items[j].ToString();
                else paletAdi = $"{j + 1}. Palet";

                string gosterilenPaletAdi = paletAdi;
                if (dgvPaletMatrisi.Columns.Count == 1) gosterilenPaletAdi = "1 Palet Dolap";

                List<string> paletIcerigi = new List<string>();
                foreach (DataGridViewRow row in dgvPaletMatrisi.Rows)
                {
                    if (row.Cells[j].Value != null && !string.IsNullOrWhiteSpace(row.Cells[j].Value.ToString()))
                    {
                        string hamVeri = row.Cells[j].Value.ToString();
                        string[] parcalar = hamVeri.Split(new string[] { " | Adet: " }, StringSplitOptions.None);
                        string urunKismi = parcalar[0];
                        string adetKismi = parcalar.Length > 1 ? parcalar[1] : "1";

                        int parantezIndex = urunKismi.LastIndexOf('(');
                        if (parantezIndex > 0) urunKismi = urunKismi.Substring(0, parantezIndex).Trim();

                        string uKodu = urunKismi;
                        string uAdi = "";
                        int tireIndex = urunKismi.IndexOf(" - ");

                        if (tireIndex > 0)
                        {
                            uKodu = urunKismi.Substring(0, tireIndex).Trim();
                            uAdi = urunKismi.Substring(tireIndex + 3).Trim();
                        }
                        else
                        {
                            uKodu = urunKismi.Trim();
                            var urun = yerelUrunler.FirstOrDefault(u => u.UrunKodu == uKodu || u.Barkod == uKodu);
                            if (urun != null) uAdi = urun.Aciklama;
                            else uAdi = "Bilinmeyen Ürün";
                        }

                        paletIcerigi.Add($"<li><span class='k-kod'>• {uKodu}</span><span class='k-ad'>{uAdi}</span><span class='k-adet'>Adet: {adetKismi}</span></li>");
                    }
                }

                // Palet boşsa diğer palete geç, boş kağıt israf etme
                if (paletIcerigi.Count == 0) continue;

                enAzBirPaletDolu = true;

                // 🌟 İŞTE GERÇEK KİOSK HAFIZA ZIRHI BURADA! 🌟
                string paletBarkodu = "";
                if (aktifPaletBarkodlari.ContainsKey(paletAdi))
                {
                    // Zaten basılmış, Kiosk'un beklediği o sabit barkodu kullan
                    paletBarkodu = aktifPaletBarkodlari[paletAdi];
                }
                else
                {
                    // İlk defa yazdırılıyor, yeni üret ve hafızaya (Sözlüğe) ekle
                    paletBarkodu = Ean13Olustur();
                    aktifPaletBarkodlari.Add(paletAdi, paletBarkodu);
                }

                string listeHtml = string.Join("", paletIcerigi);
                string barkodId = "barkod_" + j.ToString();

                html.AppendLine($@"   <div class='sayfa'>       <div class='firma'>{musteriAdi}</div>       <div class='sevk-musteri'>Sevk: {sevkMusteriAdi}</div>       <div class='belge'>Belge No: {belgeNo}</div>       <div class='palet'>{gosterilenPaletAdi}</div>                  <div class='urunler'><ul>{listeHtml}</ul></div>                  <div class='barkod-alani'><svg id='{barkodId}'></svg></div>       <script>          JsBarcode('#{barkodId}', '{paletBarkodu}', {{ format: 'EAN13', width: 5, height: 90, displayValue: true, fontSize: 34, fontOptions: 'bold', margin: 0 }});       </script>   </div>");
            }

            if (!enAzBirPaletDolu)
            {
                HataSesCal();
                MessageBox.Show("Seçilen kriterlere uygun DOLU palet bulunamadı! Lütfen yazdırmadan önce ürün okutun.", "Boş Palet", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            html.AppendLine("</body></html>");

            // 🌟 5. WEBVIEW2 EDGE İLE YAZDIR
            Form frmYazdir = new Form { Text = "Palet Etiketi Çıkartılıyor...", Width = 800, Height = 600, StartPosition = FormStartPosition.CenterParent, Icon = this.Icon };
            Microsoft.Web.WebView2.WinForms.WebView2 web = new Microsoft.Web.WebView2.WinForms.WebView2 { Dock = DockStyle.Fill };
            frmYazdir.Controls.Add(web);
            frmYazdir.FormClosed += (s1, e1) => { web.Dispose(); };

            frmYazdir.Shown += async (senderForm, args) =>
            {
                var ozelHafiza = await Microsoft.Web.WebView2.Core.CoreWebView2Environment.CreateAsync(null, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TamgaApp", "EtiketPrintAktif"));
                await web.EnsureCoreWebView2Async(ozelHafiza);
                web.NavigationCompleted += (s2, e2) => { web.CoreWebView2.ShowPrintUI(Microsoft.Web.WebView2.Core.CoreWebView2PrintDialogKind.Browser); };
                web.NavigateToString(html.ToString());
            };

            frmYazdir.ShowDialog();
        }

        // 🌟 TABLO ÜZERİNDE SAĞ TIKLA KLONLAMA VE YENİDEN ADLANDIRMA MOTORU
        private void dgvYarimSevkler_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right && e.RowIndex >= 0)
            {
                dgvYarimSevkler.ClearSelection();
                dgvYarimSevkler.Rows[e.RowIndex].Selected = true;

                ContextMenuStrip sagTikMenu = new ContextMenuStrip();
                sagTikMenu.Font = new Font("Segoe UI", 10, FontStyle.Bold);

                // 🌟 1. SEÇENEK: KOPYALA (KLONLA)
                ToolStripMenuItem btnKopyala = new ToolStripMenuItem("📄 Kopyala (Klonla)");
                btnKopyala.ForeColor = Color.DarkBlue;
                btnKopyala.Click += (s, ev) =>
                {
                    try
                    {
                        string secilenDosyaYolu = dgvYarimSevkler.Rows[e.RowIndex].Cells["DosyaYolu"].Value.ToString();
                        string jsonIcerik = File.ReadAllText(secilenDosyaYolu);

                        YarimSevkiyatHafizasi hafiza = Newtonsoft.Json.JsonConvert.DeserializeObject<YarimSevkiyatHafizasi>(jsonIcerik);
                        if (hafiza == null) return;

                        hafiza.KayitTarihi = DateTime.Now;

                        string anaYol = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "TamgaApp Yarım Sevkiyatlar");
                        string musteriTemiz = string.Join("_", hafiza.MusteriAdi.Split(Path.GetInvalidFileNameChars()));
                        if (string.IsNullOrWhiteSpace(musteriTemiz)) musteriTemiz = "BelirtilmeyenFirma";

                        string yeniDosyaAdi = $"{musteriTemiz} (KOPYA) - {DateTime.Now:dd.MM.yyyy HH-mm-ss}.json";

                        if (Path.GetFileName(secilenDosyaYolu).StartsWith("[BEKLET]"))
                            yeniDosyaAdi = "[BEKLET] " + yeniDosyaAdi;

                        string yeniTamYol = Path.Combine(anaYol, yeniDosyaAdi);

                        File.WriteAllText(yeniTamYol, Newtonsoft.Json.JsonConvert.SerializeObject(hafiza, Newtonsoft.Json.Formatting.Indented));

                        btnYarimGetir_Click(null, null);
                        MessageBox.Show("Sevkiyat başarıyla kopyalandı ve listeye eklendi!", "Kopya Oluşturuldu", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex) { MessageBox.Show("Hata: " + ex.Message); }
                };

                // 🌟 2. SEÇENEK: YENİDEN ADLANDIR (İSİM DEĞİŞTİRME EKRANI)
                ToolStripMenuItem btnYenidenAdlandir = new ToolStripMenuItem("✏️ Yeniden Adlandır");
                btnYenidenAdlandir.ForeColor = Color.DarkOrange;
                btnYenidenAdlandir.Click += (s, ev) =>
                {
                    try
                    {
                        string secilenDosyaYolu = dgvYarimSevkler.Rows[e.RowIndex].Cells["DosyaYolu"].Value.ToString();
                        string eskiDosyaAdi = Path.GetFileNameWithoutExtension(secilenDosyaYolu);
                        string anaYol = Path.GetDirectoryName(secilenDosyaYolu);

                        // Yeniden adlandırma için şık ve küçük bir popup form çıkartıyoruz
                        Form frmRename = new Form { Width = 400, Height = 180, Text = "Kaydı Yeniden Adlandır", FormBorderStyle = FormBorderStyle.FixedDialog, StartPosition = FormStartPosition.CenterParent, ShowIcon = false, MaximizeBox = false, MinimizeBox = false, BackColor = Color.WhiteSmoke };
                        Label lbl = new Label { Left = 20, Top = 20, Text = "Kayıt için yeni bir isim girin:", AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
                        TextBox txtNewName = new TextBox { Left = 20, Top = 50, Width = 340, Font = new Font("Segoe UI", 11), Text = eskiDosyaAdi };
                        Button btnOnay = new Button { Text = "KAYDET", Left = 20, Top = 90, Width = 340, Height = 35, BackColor = Color.Teal, ForeColor = Color.White, Font = new Font("Segoe UI", 10, FontStyle.Bold), Cursor = Cursors.Hand };

                        btnOnay.Click += (senderObj, args) =>
                        {
                            string yeniAd = txtNewName.Text.Trim();
                            if (string.IsNullOrWhiteSpace(yeniAd)) { MessageBox.Show("Geçerli bir isim girin!"); return; }

                            // Windows'un dosya isminde sevmediği karakterleri (\, /, : vb.) temizle
                            foreach (char c in Path.GetInvalidFileNameChars()) { yeniAd = yeniAd.Replace(c, '_'); }

                            string yeniTamYol = Path.Combine(anaYol, yeniAd + ".json");

                            // Eğer isim aynıysa hiçbir şey yapmadan kapat
                            if (secilenDosyaYolu.Equals(yeniTamYol, StringComparison.OrdinalIgnoreCase)) { frmRename.Close(); return; }

                            // Eğer bu isimde başka bir dosya varsa uyar
                            if (File.Exists(yeniTamYol)) { MessageBox.Show("Bu isimde bir kayıt zaten var!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

                            // 🌟 GERÇEK ADLANDIRMA: Dosyayı yeni ismiyle diske taşıyor ve ekranı kapatıyor
                            File.Move(secilenDosyaYolu, yeniTamYol);
                            frmRename.Close();

                            // Listeyi anında yenile
                            btnYarimGetir_Click(null, null);
                        };

                        frmRename.Controls.Add(lbl); frmRename.Controls.Add(txtNewName); frmRename.Controls.Add(btnOnay);
                        frmRename.ShowDialog();
                    }
                    catch (Exception ex) { MessageBox.Show("Hata: " + ex.Message); }
                };

                sagTikMenu.Items.Add(btnKopyala);
                sagTikMenu.Items.Add(new ToolStripSeparator());
                sagTikMenu.Items.Add(btnYenidenAdlandir);

                sagTikMenu.Closed += (senderMenu, argsMenu) => { this.BeginInvoke(new Action(() => sagTikMenu.Dispose())); };
                sagTikMenu.Show(Cursor.Position);
            }
        }

        // 🌟 SEVKİYATI ASKIYA AL MOTORU (Üstüne Yazmayı Engelleyen Zırhlı Versiyon)
        private void btnSevkAskayaAl_Click(object sender, EventArgs e)
        {

            // 🌟 ZIRH: ASKIYA ALMADAN ÖNCE ETİKETSİZ (BARKODSUZ) PALET KONTROLÜ
            dgvPaletMatrisi.EndEdit();
            List<string> etiketsizPaletler = new List<string>();

            // Matristeki (Kamyondaki) tüm paletleri tek tek kontrol et
            for (int j = 0; j < dgvPaletMatrisi.Columns.Count; j++)
            {
                string pAdi = dgvPaletMatrisi.Columns[j].HeaderText;
                bool paletDoluMu = false;

                // Paletin içine ürün konmuş mu? (Boş palet için uyarı vermeyelim)
                foreach (DataGridViewRow row in dgvPaletMatrisi.Rows)
                {
                    if (row.Cells[j].Value != null && !string.IsNullOrWhiteSpace(row.Cells[j].Value.ToString()))
                    {
                        paletDoluMu = true;
                        break;
                    }
                }

                // Eğer palet doluysa AMA hafızada (aktifPaletBarkodlari) barkodu üretilmemişse (yani Yazdır'a basılmamışsa)
                if (paletDoluMu && !aktifPaletBarkodlari.ContainsKey(pAdi))
                {
                    etiketsizPaletler.Add(pAdi);
                }
            }

            // Eğer etiketi basılmamış dolu paletler varsa uyar!
            if (etiketsizPaletler.Count > 0)
            {
                HataSesCal();
                DialogResult cevap = MessageBox.Show(
                    "DİKKAT! Aşağıdaki paletlerin etiketini (EAN-13) henüz YAZDIRMADINIZ:\n\n👉 " +
                    string.Join("\n👉 ", etiketsizPaletler) +
                    "\n\nFiziksel paletlerin sahada isimsiz kalıp kaybolmaması için önce 'Etiket Yazdır' yapmanız tavsiye edilir.\n\nYine de etiketsiz olarak ASKIYA ALMAK istiyor musunuz?",
                    "Etiketi Basılmamış Palet Uyarısı",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2); // Yanlışlıkla basmasın diye 'Hayır'ı seçili getiririz

                if (cevap == DialogResult.No)
                {
                    return; // 🛑 İşlemi iptal et, kullanıcı gidip etiketleri yazdırsın!
                }
            }

            if (clbBelgeNo.CheckedItems.Count == 0 || dgvMalzemeler.Rows.Count == 0)
            {
                MessageBox.Show("Askıya alınacak açık bir sevkiyat yok!", "Hata"); return;
            }

            YarimSevkiyatHafizasi hafiza = new YarimSevkiyatHafizasi
            {
                MusteriAdi = txtMusteriAdi.Text,
                BelgeNo = string.Join(", ", clbBelgeNo.CheckedItems.Cast<string>()),
                SevkMusteri = txtSevkMusteri.Text,
                PaletSayisi = cmbSevkPaletSayisi.SelectedIndex != -1 ? Convert.ToInt32(cmbSevkPaletSayisi.SelectedItem) : 0,
                KayitTarihi = DateTime.Now
            };

            foreach (DataGridViewRow row in dgvMalzemeler.Rows)
            {
                if (row.IsNewRow || row.Cells["Malzeme Kodu"].Value == null) continue;

                string belgeNo = row.Cells["Belge No"].Value?.ToString() ?? "";
                string malzemeKodu = row.Cells["Malzeme Kodu"].Value.ToString();
                string aciklama = row.Cells["Açıklama"].Value?.ToString() ?? "";

                string benzersizAnahtar = $"{belgeNo}_{malzemeKodu}_{aciklama}";

                int okutulan = 0;
                if (row.Cells["Okutulan"].Value != null) int.TryParse(row.Cells["Okutulan"].Value.ToString(), out okutulan);

                if (!hafiza.AnaOkutulanlar.ContainsKey(benzersizAnahtar))
                    hafiza.AnaOkutulanlar.Add(benzersizAnahtar, okutulan);
                else
                    hafiza.AnaOkutulanlar[benzersizAnahtar] += okutulan;
            }

            for (int i = 0; i < dgvPaletMatrisi.Rows.Count; i++)
            {
                hafiza.PaletMatrisiDurumu[i] = new Dictionary<int, string>();
                for (int j = 0; j < dgvPaletMatrisi.Columns.Count; j++)
                {
                    if (dgvPaletMatrisi.Rows[i].Cells[j].Value != null)
                        hafiza.PaletMatrisiDurumu[i][j] = dgvPaletMatrisi.Rows[i].Cells[j].Value.ToString();
                }
            }

            hafiza.PaletBarkodlari = aktifPaletBarkodlari;

            string anaYol = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "TamgaApp Yarım Sevkiyatlar");
            if (!Directory.Exists(anaYol)) Directory.CreateDirectory(anaYol);

            string musteriTemiz = string.Join("_", txtMusteriAdi.Text.Split(Path.GetInvalidFileNameChars()));
            if (string.IsNullOrWhiteSpace(musteriTemiz)) musteriTemiz = "BelirtilmeyenFirma";

            // 🌟 SİHİRLİ ZIRH (Saniye + Sayaç ile üstüne yazmayı %100 engeller)
            string zamanDamgasi = DateTime.Now.ToString("dd.MM.yyyy HH-mm-ss");
            string dosyaAdi = $"{musteriTemiz} - {zamanDamgasi}.json";
            string tamYol = Path.Combine(anaYol, dosyaAdi);

            int sayac = 1;
            while (System.IO.File.Exists(tamYol))
            {
                dosyaAdi = $"{musteriTemiz} - {zamanDamgasi} ({sayac}).json";
                tamYol = Path.Combine(anaYol, dosyaAdi);
                sayac++;
            }

            System.IO.File.WriteAllText(tamYol, Newtonsoft.Json.JsonConvert.SerializeObject(hafiza, Newtonsoft.Json.Formatting.Indented));

            MessageBox.Show($"Sevkiyat başarıyla ASKIYA ALINDI!\nİstediğiniz zaman kaldığınız yerden devam edebilirsiniz.", "Kaydedildi", MessageBoxButtons.OK, MessageBoxIcon.Information);

            txtMusteriAdi.Clear();
            txtSevkMusteri.Clear();
            txtBarkod.Clear();
            clbBelgeNo.Items.Clear();
            cmbSevkPaletSayisi.SelectedIndex = -1;

            if (dgvMalzemeler.DataSource == null) dgvMalzemeler.Rows.Clear();
            else dgvMalzemeler.DataSource = null;

            dgvPaletMatrisi.Columns.Clear();
            dgvPaletMatrisi.Rows.Clear();
            cmbAktifPalet.Items.Clear();

            aktifPaletBarkodlari.Clear();
            btnYarimGetir_Click(null, null); // Listeyi anında yenile

            KarantinayaAl(false);
        }

        // 🌟 ASKI VE BEKLETME LİSTESİNİ DOLDURUR (Punto Küçültüldü ve Şıklaştırıldı)
        private void btnYarimGetir_Click(object sender, EventArgs e)
        {
            // Tablo ilk kez yükleniyorsa iskeletini ve şıklığını kur
            if (dgvYarimSevkler.Columns.Count == 0)
            {
                dgvYarimSevkler.Columns.Add("GorunenAd", "Askıdaki Sevkiyatlar");
                dgvYarimSevkler.Columns.Add("DosyaYolu", "GizliYol");
                dgvYarimSevkler.Columns["DosyaYolu"].Visible = false; // Gerçek dosya yolunu arka planda saklar

                dgvYarimSevkler.AllowUserToAddRows = false;
                dgvYarimSevkler.ReadOnly = true;
                dgvYarimSevkler.RowHeadersVisible = false;
                dgvYarimSevkler.ColumnHeadersVisible = false; // Başlıkları gizle ki ListBox gibi sade dursun
                dgvYarimSevkler.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                dgvYarimSevkler.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                dgvYarimSevkler.BackgroundColor = Color.White;
                dgvYarimSevkler.BorderStyle = BorderStyle.Fixed3D;
                dgvYarimSevkler.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal; // Sadece alt çizgi olsun
                dgvYarimSevkler.DefaultCellStyle.SelectionBackColor = Color.DodgerBlue;
                dgvYarimSevkler.DefaultCellStyle.SelectionForeColor = Color.White;

                // 🌟 YENİLİK: Punto küçüldüğü için satır aralıkları ferahlatıldı (35'ten 28'e düşürüldü)
                dgvYarimSevkler.RowTemplate.Height = 28;
            }

            dgvYarimSevkler.Rows.Clear();

            string anaYol = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "TamgaApp Yarım Sevkiyatlar");
            if (!Directory.Exists(anaYol)) Directory.CreateDirectory(anaYol);

            FileInfo[] dosyalar = new DirectoryInfo(anaYol).GetFiles("*.json").OrderByDescending(f => f.CreationTime).ToArray();

            foreach (FileInfo dosya in dosyalar)
            {
                string gosterilecekAd = Path.GetFileNameWithoutExtension(dosya.Name);
                bool bekletMi = gosterilecekAd.StartsWith("[BEKLET]");

                int index = dgvYarimSevkler.Rows.Add(gosterilecekAd, dosya.FullName);
                DataGridViewRow row = dgvYarimSevkler.Rows[index];

                // 🌟 YENİLİK: Fontlar (Puntolar) 10'dan 8'e düşürüldü, daha çok kayıt sığar!
                if (bekletMi)
                {
                    row.DefaultCellStyle.BackColor = Color.LightGreen;
                    row.DefaultCellStyle.ForeColor = Color.DarkGreen;
                    row.DefaultCellStyle.Font = new Font("Segoe UI", 8, FontStyle.Bold);
                }
                else
                {
                    row.DefaultCellStyle.BackColor = Color.White;
                    row.DefaultCellStyle.ForeColor = Color.Black;
                    row.DefaultCellStyle.Font = new Font("Segoe UI", 8, FontStyle.Regular);
                }
            }
            // ... yukarıdaki döngüler ve tabloya ekleme kodları ...

            // 🌟 DİNAMİK BUTON KİLİDİ: Tablo boşsa işlem butonlarını beton gibi dondur!
            bool kayitVarMi = dgvYarimSevkler.Rows.Count > 0;

            if (btnYarimAc != null) btnYarimAc.Enabled = kayitVarMi;

            // Eğer tasarımında Askıdan Sil butonu varsa onu da otomatik bulur ve kilitler
            var btnSil = this.Controls.Find("btnAskidanSil", true).FirstOrDefault() as Button;
            if (btnSil != null) btnSil.Enabled = kayitVarMi;

        }

        // 🌟 ASKI VE BEKLETME LİSTESİNDEN (TABLODAN) KAYIT SİLME MOTORU
        private void btnAskidanSil_Click(object sender, EventArgs e)
        {
            // Eski lstYarimSevkler yerine yeni dgvYarimSevkler tablomuzu kontrol ediyoruz
            if (dgvYarimSevkler.SelectedRows.Count == 0)
            {
                MessageBox.Show("Lütfen silmek istediğiniz askı kaydını tablodan seçin!", "Seçim Yok", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 🌟 Tablodaki gizli sütundan gerçek dosya yolunu yakala
            string secilenDosyaYolu = dgvYarimSevkler.SelectedRows[0].Cells["DosyaYolu"].Value.ToString();
            FileInfo secilenDosya = new FileInfo(secilenDosyaYolu);

            // Ekranda görünen adı (Örn: [BEKLET] Evtim Yapı) mesaj kutusu için al
            string gosterilecekAd = dgvYarimSevkler.SelectedRows[0].Cells["GorunenAd"].Value.ToString();

            if (MessageBox.Show($"'{gosterilecekAd}' kalıcı olarak silinecek. Emin misiniz?", "Askı Kaydını Sil", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                try
                {
                    File.Delete(secilenDosya.FullName);
                    MessageBox.Show("Askıdaki kayıt başarıyla silindi!", "Silindi", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // Tabloyu otomatik yenile ki silinen kayıt ekrandan uçsun
                    btnYarimGetir_Click(null, null);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Silme işlemi sırasında hata oluştu: " + ex.Message, "Kritik Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // 🌟 ASKI VEYA BEKLETİLEN KAYDI EKRANA GERİ GETİRME (AÇMA)
        private void btnYarimAc_Click(object sender, EventArgs e)
        {
            if (dgvYarimSevkler.SelectedRows.Count == 0)
            {
                MessageBox.Show("Lütfen devam etmek istediğiniz yarım sevkiyatı listeden seçin!", "Seçim Yok", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (dtTumSiparisler == null || dtTumSiparisler.Columns.Count == 0 || dtTumSiparisler.Rows.Count == 0)
            {
                btnSiparisYenile_Click(null, null);
                if (dtTumSiparisler == null || dtTumSiparisler.Columns.Count == 0) return;
            }

            // 🌟 Gizli sütundan dosya yolunu çek
            string secilenDosyaYolu = dgvYarimSevkler.SelectedRows[0].Cells["DosyaYolu"].Value.ToString();
            FileInfo secilenDosya = new FileInfo(secilenDosyaYolu);

            try
            {
                string jsonIcerik = File.ReadAllText(secilenDosya.FullName);
                YarimSevkiyatHafizasi hafiza = Newtonsoft.Json.JsonConvert.DeserializeObject<YarimSevkiyatHafizasi>(jsonIcerik);
                if (hafiza == null) return;

                int musteriIndex = -1;
                for (int i = 0; i < cmbMusteri.Items.Count; i++)
                {
                    if (cmbMusteri.Items[i].ToString().Trim().Equals(hafiza.MusteriAdi.Trim(), StringComparison.OrdinalIgnoreCase)) { musteriIndex = i; break; }
                }

                if (musteriIndex >= 0) cmbMusteri.SelectedIndex = musteriIndex;
                else { cmbMusteri.Items.Add(hafiza.MusteriAdi); cmbMusteri.SelectedIndex = cmbMusteri.Items.Count - 1; }

                txtMusteriAdi.Text = hafiza.MusteriAdi;
                txtSevkMusteri.Text = hafiza.SevkMusteri;
                cmbSevkPaletSayisi.SelectedItem = hafiza.PaletSayisi.ToString();

                string[] kaydedilenBelgeler = hafiza.BelgeNo.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (clbBelgeNo.Items.Count == 0) foreach (string b in kaydedilenBelgeler) clbBelgeNo.Items.Add(b);

                foreach (string belge in kaydedilenBelgeler)
                {
                    for (int i = 0; i < clbBelgeNo.Items.Count; i++)
                    {
                        if (clbBelgeNo.Items[i].ToString().IndexOf(belge, StringComparison.OrdinalIgnoreCase) >= 0) clbBelgeNo.SetItemChecked(i, true);
                    }
                }

                btnSevkAra_Click(null, null);
                btnSevkAra_Click(null, null);

                // 🌟 KAPANMIŞ (İRSALİYE KESİLMİŞ) SİPARİŞLER İÇİN REVİZE ZIRHI 🌟
                // Eğer sipariş SQL'de kapandığı için sol tablo (dgvMalzemeler) boş geldiyse, 
                // sistemi kandırıp ürünleri hafızadaki dosyanın içinden suni olarak tabloya diziyoruz!
                if (dgvMalzemeler.Rows.Count == 0 && hafiza.AnaOkutulanlar.Count > 0)
                {
                    var yerelUrunler = DataAccess.GetAllUrunler();
                    DataTable dtEkran = new DataTable();
                    dtEkran.Columns.Add("Belge No", typeof(string));
                    dtEkran.Columns.Add("Malzeme Kodu", typeof(string));
                    dtEkran.Columns.Add("Barkod", typeof(string));
                    dtEkran.Columns.Add("Malzeme Adı", typeof(string));
                    dtEkran.Columns.Add("Açıklama", typeof(string));
                    dtEkran.Columns.Add("Sipariş Adedi", typeof(int));
                    dtEkran.Columns.Add("Okutulan", typeof(int));

                    foreach (var kvp in hafiza.AnaOkutulanlar)
                    {
                        // anahtar = "SE-001_KOD_Renk"
                        string[] parcalar = kvp.Key.Split('_');
                        string bNo = parcalar[0];
                        string mKodu = parcalar.Length > 1 ? parcalar[1] : "";
                        string renk = parcalar.Length > 2 ? parcalar[2] : "";

                        // Ürünün adını ve barkodunu yerel veritabanımızdan bul
                        var urun = yerelUrunler.FirstOrDefault(u => u.UrunKodu == mKodu && u.Renk.Equals(renk, StringComparison.OrdinalIgnoreCase));
                        if (urun == null) urun = yerelUrunler.FirstOrDefault(u => u.UrunKodu == mKodu);

                        string barkod = urun != null && !string.IsNullOrWhiteSpace(urun.Barkod) ? urun.Barkod : "BARKOD YOK";
                        string mAdi = urun != null ? urun.Aciklama : "Bilinmeyen Ürün";

                        // Sipariş adedini mecburen arşivdeki miktar kadar yapıyoruz, çünkü orijinal siparişi artık bilmiyoruz.
                        // Okutulan'ı 0 yapıyoruz, çünkü hemen aşağıdaki "Şelale Mantığı" döngüsü onu alıp olması gerektiği gibi dolduracak!
                        int adet = kvp.Value;
                        dtEkran.Rows.Add(bNo, mKodu, barkod, mAdi, renk, adet, 0);
                    }

                    dgvMalzemeler.DataSource = dtEkran;
                    dgvMalzemeler.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                }

                // MÜKEMMEL DAĞITIM VE ŞELALE MANTIĞI

                // MÜKEMMEL DAĞITIM VE ŞELALE MANTIĞI
                foreach (DataGridViewRow row in dgvMalzemeler.Rows)
                {
                    if (row.IsNewRow || row.Cells["Malzeme Kodu"].Value == null) continue;

                    string belgeNo = row.Cells["Belge No"].Value?.ToString() ?? "";
                    string malzemeKodu = row.Cells["Malzeme Kodu"].Value.ToString();
                    string aciklama = row.Cells["Açıklama"].Value?.ToString() ?? "";

                    string benzersizAnahtar = $"{belgeNo}_{malzemeKodu}_{aciklama}";

                    if (hafiza.AnaOkutulanlar.ContainsKey(benzersizAnahtar) && hafiza.AnaOkutulanlar[benzersizAnahtar] > 0)
                    {
                        int siparisAdedi = Convert.ToInt32(row.Cells["Sipariş Adedi"].Value);
                        int havuzdaki = hafiza.AnaOkutulanlar[benzersizAnahtar];
                        int yazilacak = Math.Min(siparisAdedi, havuzdaki);
                        row.Cells["Okutulan"].Value = yazilacak;
                        hafiza.AnaOkutulanlar[benzersizAnahtar] -= yazilacak;
                    }
                    else row.Cells["Okutulan"].Value = 0;
                }

                DgvMalzemeler_Renklendir(null, null);

                dgvPaletMatrisi.Rows.Clear();
                int maxSatirIndex = hafiza.PaletMatrisiDurumu.Keys.Count > 0 ? hafiza.PaletMatrisiDurumu.Keys.Max() : -1;
                for (int i = 0; i <= maxSatirIndex; i++) dgvPaletMatrisi.Rows.Add();

                foreach (var satirKvp in hafiza.PaletMatrisiDurumu)
                {
                    int satirIndex = satirKvp.Key;
                    foreach (var sutunKvp in satirKvp.Value)
                    {
                        dgvPaletMatrisi.Rows[satirIndex].Cells[sutunKvp.Key].Value = sutunKvp.Value;
                    }
                }

                aktifPaletBarkodlari = hafiza.PaletBarkodlari ?? new Dictionary<string, string>();

                // 🌟 GÜVENLİK AĞI ZIRHI (SİLMEK YERİNE KURTARMA KLASÖRÜNE AT)
                string kurtarmaKlasoru = Path.Combine(secilenDosya.DirectoryName, "Kurtarma_Yedekleri");
                if (!Directory.Exists(kurtarmaKlasoru)) Directory.CreateDirectory(kurtarmaKlasoru);
                string kurtarmaDosyaYolu = Path.Combine(kurtarmaKlasoru, secilenDosya.Name);
                if (File.Exists(kurtarmaDosyaYolu)) File.Delete(kurtarmaDosyaYolu);
                File.Move(secilenDosya.FullName, kurtarmaDosyaYolu);

                btnYarimGetir_Click(null, null); // Listeyi yenile

                MessageBox.Show($"Sevkiyat başarıyla geri yüklendi. Kaldığınız yerden devam edebilirsiniz!", "Sistem Hazır", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Geri yükleme sırasında hata oluştu: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            KarantinayaAl(true);

        }

        // 🌟 SİHİRLİ ZIRH: "Random" motorunu sınıf seviyesine (dışarı) alıyoruz ki 
        // bilgisayar çok hızlı döngüye girse bile ASLA aynı barkodu üretmesin!
        private static Random paletRnd = new Random();

        // Her palete özel, uluslararası standartlarda benzersiz EAN-13 barkodu üretir
        private string Ean13Olustur()
        {
            string base12 = "20" + DateTime.Now.ToString("yyMMdd") + paletRnd.Next(1000, 9999).ToString("D4");
            int sum = 0;
            for (int i = 0; i < 12; i++)
            {
                int num = int.Parse(base12[i].ToString());
                sum += (i % 2 == 0) ? num : num * 3;
            }
            int check = (10 - (sum % 10)) % 10;
            return base12 + check.ToString();
        }

        // 🌟 SEVKİYAT ARŞİVLEME MOTORU (GİZLİ ENTER VE NOKTALI VİRGÜL HATASINI DÜZELTEN ZIRH EKLENDİ)
        private void SevkiyatArsivle(string belgeNo, string musteri, string sevkMusteri, string sevkTuru, string paletSayisi)
        {
            try
            {
                string belgeKontrol = string.IsNullOrEmpty(belgeNo) ? "SE" : belgeNo.Split(new[] { ',', '_' }, StringSplitOptions.RemoveEmptyEntries)[0].Trim();
                string turKlasoru = "Yurtiçi"; // Varsayılan

                // 🌟 Belge Tipi kontrolü (İhracat / Yurtiçi)
                if (dtTumSiparisler != null && dtTumSiparisler.Rows.Count > 0)
                {
                    DataRow[] dbSatirlari = dtTumSiparisler.Select($"BelgeNo LIKE '%{belgeKontrol}%'");
                    if (dbSatirlari.Length > 0 && dbSatirlari[0]["BelgeTipi"] != DBNull.Value && dbSatirlari[0]["BelgeTipi"].ToString().Trim() == "O1")
                    {
                        turKlasoru = "İhracat";
                    }
                }

                string yil = DateTime.Now.ToString("yyyy");
                string ay = DateTime.Now.ToString("MM");
                string gun = DateTime.Now.ToString("dd");

                string anaYol = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "TamgaApp Tamamlanan Sevkiyatlar", turKlasoru, yil, ay, gun);
                if (!Directory.Exists(anaYol)) Directory.CreateDirectory(anaYol);

                string temizMusteri = string.Join("_", musteri.Split(Path.GetInvalidFileNameChars()));
                if (string.IsNullOrWhiteSpace(temizMusteri)) temizMusteri = "BilinmeyenFirma";

                string dosyaAdi = $"{temizMusteri}_{paletSayisi}Palet_{sevkTuru}_{DateTime.Now:HHmm}.csv";
                string tamYol = Path.Combine(anaYol, dosyaAdi);

                using (StreamWriter sw = new StreamWriter(tamYol, false, System.Text.Encoding.UTF8))
                {
                    sw.WriteLine("Müşteri;SevkMüşteri;BelgeNo;Tarih;Sevk Türü");
                    sw.WriteLine($"{musteri};{sevkMusteri};{belgeNo};{DateTime.Now:HH:mm};{sevkTuru}");
                    sw.WriteLine("--- DETAYLAR ---");
                    sw.WriteLine("Palet No;İçerik;PaletBarkodu");

                    for (int j = 0; j < dgvPaletMatrisi.Columns.Count; j++)
                    {
                        string paletAdi = dgvPaletMatrisi.Columns[j].HeaderText;
                        string paletBarkodu = "";

                        if (aktifPaletBarkodlari.ContainsKey(paletAdi))
                        {
                            paletBarkodu = aktifPaletBarkodlari[paletAdi];
                        }
                        else
                        {
                            paletBarkodu = Ean13Olustur();
                        }

                        foreach (DataGridViewRow row in dgvPaletMatrisi.Rows)
                        {
                            if (row.Cells[j].Value != null && !string.IsNullOrWhiteSpace(row.Cells[j].Value.ToString()))
                            {
                                // 🌟 İŞTE HAYAT KURTARAN ZIRH: 
                                // Hem Excel'i hem CSV'yi bozan o gizli "Enter" tuşlarını boşluğa çeviriyoruz.
                                // Ayrıca ürün isminin içinde yanlışlıkla ";" (noktalı virgül) varsa onu da tireye çeviriyoruz ki barkod satırı kaymasın!
                                string icerik = row.Cells[j].Value.ToString().Replace("\r", " ").Replace("\n", " ").Replace(";", "-");

                                sw.WriteLine($"{paletAdi};{icerik};{paletBarkodu}");
                            }
                        }
                    }
                }
                aktifPaletBarkodlari.Clear();
            }
            catch { }
        }



        #endregion

        #region 📦 14.5 YENİ NESİL SEVKİYAT (WMS), ARŞİV VE ETİKET YAZDIRMA MOTORU

        // Arayüzdeki "Sevk Plan" işleyişinden bağımsız olarak, Arşiv ve Geçmişi gösteren devasa "Sevkiyat" sekmesini dinamik inşa eder.
        public void YeniNesilSevkiyatSisteminiKur()
        {
            // 1. Zaten var olan "Sevkiyat Plan" sekmesine HİÇ DOKUNMUYORUZ.

            // 2. Yeni "Sevkiyat" (Arşiv/Barkod) sekmesi var mı diye bak, yoksa oluştur.
            TabPage yeniSekme = tabControl1.TabPages.Cast<TabPage>().FirstOrDefault(t => t.Text == "Sevkiyat");
            if (yeniSekme == null)
            {
                yeniSekme = new TabPage("Sevkiyat");
                yeniSekme.BackColor = Color.FromArgb(245, 247, 250);

                // YENİ: Add yerine Insert kullanıyoruz. 
                // Buradaki '3' rakamı sekmenin sırasıdır. İstediğin yere göre (2, 3, 4 vs.) değiştirebilirsin.
                // Unutma: İlk sekme 0'dır, ikinci sekme 1'dir.
                tabControl1.TabPages.Insert(3, yeniSekme);
            }
            else
            {
                yeniSekme.Controls.Clear(); // Yenilemelerde butonlar üst üste binmesin diye
            }

            // 🌟 Ana Bölücü (Sola Dev Tablo, Sağa İnce Ağaç)
            SplitContainer split = new SplitContainer { Dock = DockStyle.Fill, FixedPanel = FixedPanel.Panel2 };
            yeniSekme.Controls.Add(split);

            // 🌟 Ekran ne kadar büyürse büyüsün Sağdaki Ağaç Paneli hep ince (300px) kalacak, Sol taraf devasa genişleyecek!
            split.Resize += (s, e) => { split.SplitterDistance = Math.Max(500, split.Width - 300); };

            // --- SAĞ PANEL: HİYERARŞİK KLASÖR AĞACI (TREEVIEW) ---
            TreeView tvArsiv = new TreeView { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 11) };
            Button btnAgiYenile = new Button { Dock = DockStyle.Bottom, Height = 40, Text = "🔄 Klasörleri Yenile", BackColor = Color.Teal, ForeColor = Color.White, Font = new Font("Segoe UI", 10, FontStyle.Bold) };

            split.Panel2.Controls.Add(tvArsiv);
            split.Panel2.Controls.Add(btnAgiYenile);

            // --- SOL PANEL: DETAYLAR, YAZDIRMA VE BARKOD SORGULAMA ---
            Panel pnlUst = new Panel { Dock = DockStyle.Top, Height = 100, BackColor = Color.FromArgb(45, 52, 54) };

            Label lblSorgu = new Label { Text = "🔍 Palet Barkodu Okut:", ForeColor = Color.White, Location = new Point(20, 20), Font = new Font("Segoe UI", 11, FontStyle.Bold), AutoSize = true };
            TextBox txtSorgu = new TextBox { Location = new Point(220, 18), Width = 300, Font = new Font("Segoe UI", 12) };

            Label lblFirmaSorgu = new Label { Text = "Müşteri / Palet Ara:", ForeColor = Color.White, Location = new Point(20, 60), Font = new Font("Segoe UI", 10, FontStyle.Bold), AutoSize = true };
            TextBox txtFirmaSorgu = new TextBox { Location = new Point(220, 58), Width = 300, Font = new Font("Segoe UI", 11) };

            Button btnEtiketYazdir = new Button { Text = "🖨️ Seçili Paletin Etiketini (EAN13) Yazdır", Location = new Point(550, 18), Width = 350, Height = 60, BackColor = Color.Orange, Font = new Font("Segoe UI", 11, FontStyle.Bold) };

            Button btnTopluBarkodArsivi = new Button { Text = "📊 Gelişmiş Barkod Listesi", Location = new Point(915, 18), Width = 260, Height = 60, BackColor = Color.DodgerBlue, ForeColor = Color.White, Font = new Font("Segoe UI", 11, FontStyle.Bold), Cursor = Cursors.Hand };

            pnlUst.Controls.Add(lblSorgu); pnlUst.Controls.Add(txtSorgu);
            pnlUst.Controls.Add(lblFirmaSorgu); pnlUst.Controls.Add(txtFirmaSorgu);
            pnlUst.Controls.Add(btnEtiketYazdir);
            pnlUst.Controls.Add(btnTopluBarkodArsivi);

            btnTopluBarkodArsivi.Click += (s, e) => { GelistirilmisBarkodArsiviniAc(); };

            DataGridView dgvDetay = new DataGridView { Dock = DockStyle.Fill, AllowUserToAddRows = false, ReadOnly = true, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, SelectionMode = DataGridViewSelectionMode.FullRowSelect, BackgroundColor = Color.White };
            dgvDetay.Columns.Add("PaletNo", "Palet No");
            dgvDetay.Columns.Add("Icerik", "Ürün İçeriği");
            dgvDetay.Columns.Add("Barkod", "EAN13 Barkod");

            // 🌟 EŞLEŞTİRMEYİ TERSİNE ÇEVİRDİK (Sola Tabloyu, Sağa Ağacı koyduk)
            split.Panel1.Controls.Add(dgvDetay);
            split.Panel1.Controls.Add(pnlUst);

            // === OLAYLAR (EVENTS) ===

            // Ağacı Dolduran Metot
            Action AgaciDoldur = () =>
            {
                tvArsiv.Nodes.Clear();
                string kokYol = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "TamgaApp Tamamlanan Sevkiyatlar");
                if (!Directory.Exists(kokYol)) return;

                TreeNode kok = new TreeNode("📦 Tamamlanan Sevkiyatlar") { Tag = kokYol };
                tvArsiv.Nodes.Add(kok);

                void KlasorTaramasi(string dizin, TreeNode ebeveyn)
                {
                    foreach (string klasor in Directory.GetDirectories(dizin))
                    {
                        TreeNode dugum = new TreeNode("📁 " + Path.GetFileName(klasor)) { Tag = klasor };
                        ebeveyn.Nodes.Add(dugum);
                        KlasorTaramasi(klasor, dugum);
                    }
                    foreach (string dosya in Directory.GetFiles(dizin, "*.csv"))
                    {
                        ebeveyn.Nodes.Add(new TreeNode("📄 " + Path.GetFileNameWithoutExtension(dosya)) { Tag = dosya, ForeColor = Color.DarkBlue });
                    }
                }
                KlasorTaramasi(kokYol, kok);
                kok.Expand();
            };

            btnAgiYenile.Click += (s, e) => AgaciDoldur();
            AgaciDoldur(); // İlk Açılışta Doldur

            // 🌟 KÜRESEL DEĞİŞKENLER (Arşiv Okuma İçin Sevk Müşteri Eklendi)
            string aktifMusteri = "";
            string aktifSevkMusteri = "";
            string aktifBelge = "";

            // Ağaçtaki bir fişe tıklanınca gridi doldur
            tvArsiv.AfterSelect += (s, e) =>
            {
                if (e.Node.Tag != null && e.Node.Tag.ToString().EndsWith(".csv"))
                {
                    dgvDetay.Rows.Clear();
                    string[] satirlar = File.ReadAllLines(e.Node.Tag.ToString());
                    bool detaylar = false;

                    if (satirlar.Length > 1)
                    {
                        // CSV'nin 2. satırı: Müşteri ; SevkMüşteri ; BelgeNo ; Tarih ; Sevk Türü
                        string[] huc = satirlar[1].Split(';');
                        if (huc.Length >= 3)
                        {
                            aktifMusteri = huc[0];
                            aktifSevkMusteri = huc[1];
                            aktifBelge = huc[2];
                        }
                    }

                    foreach (string satir in satirlar)
                    {
                        if (satir.Contains("--- DETAYLAR ---")) { detaylar = true; continue; }
                        if (detaylar && !satir.StartsWith("Palet No") && !string.IsNullOrWhiteSpace(satir))
                        {
                            string[] h = satir.Split(';');
                            if (h.Length >= 3) dgvDetay.Rows.Add(h[0], h[1], h[2]);
                        }
                    }
                }
            };

            // Grid İçi Arama / Filtreleme
            txtFirmaSorgu.TextChanged += (s, e) =>
            {
                string ara = txtFirmaSorgu.Text.ToLower();
                foreach (DataGridViewRow r in dgvDetay.Rows)
                {
                    r.Visible = string.IsNullOrEmpty(ara) ||
                                (r.Cells[0].Value != null && r.Cells[0].Value.ToString().ToLower().Contains(ara)) ||
                                (r.Cells[1].Value != null && r.Cells[1].Value.ToString().ToLower().Contains(ara));
                }
            };

            // 🌟 ETİKET YAZDIRMA MOTORU (Geçmiş Arşivden Etiket Çıkarma)
            btnEtiketYazdir.Click += async (s, e) =>
            {
                if (dgvDetay.SelectedRows.Count == 0) { MessageBox.Show("Lütfen etiketini yazdırmak istediğiniz ürünü seçin!"); return; }

                string seciliPalet = dgvDetay.SelectedRows[0].Cells[0].Value.ToString();
                string barkod = dgvDetay.SelectedRows[0].Cells[2].Value.ToString();

                // 🌟 SİHİRLİ DOKUNUŞ: Arşivdeki o sevkiyatta toplam kaç benzersiz palet var bul
                int toplamPaletSayisi = dgvDetay.Rows.Cast<DataGridViewRow>()
                                        .Select(r => r.Cells[0].Value?.ToString())
                                        .Where(v => !string.IsNullOrEmpty(v))
                                        .Distinct()
                                        .Count();

                string gosterilenPaletAdi = seciliPalet;
                if (toplamPaletSayisi == 1)
                {
                    gosterilenPaletAdi = "1 Palet Dolap";
                }

                if (string.IsNullOrEmpty(aktifSevkMusteri)) aktifSevkMusteri = "Belirtilmedi";

                // 🌟 VERİTABANI BAĞLANTISI: Eski arşiv kayıtlarında isim yoksa veritabanından çek!
                var yerelUrunler = DataAccess.GetAllUrunler();

                List<string> urunler = new List<string>();
                foreach (DataGridViewRow r in dgvDetay.Rows)
                {
                    if (r.Cells[0].Value.ToString() == seciliPalet && r.Cells[2].Value.ToString() == barkod)
                    {
                        string hamVeri = r.Cells[1].Value.ToString();

                        string[] parcalar = hamVeri.Split(new string[] { " | Adet: " }, StringSplitOptions.None);
                        string urunKismi = parcalar[0];
                        string adetKismi = parcalar.Length > 1 ? parcalar[1] : "1";

                        int parantezIndex = urunKismi.LastIndexOf('(');
                        if (parantezIndex > 0) urunKismi = urunKismi.Substring(0, parantezIndex).Trim();

                        // 🌟 KOD VE AD AYIRIMI (Eski Arşiv Zırhı)
                        string uKodu = urunKismi;
                        string uAdi = "";
                        int tireIndex = urunKismi.IndexOf(" - ");

                        if (tireIndex > 0)
                        {
                            uKodu = urunKismi.Substring(0, tireIndex).Trim();
                            uAdi = urunKismi.Substring(tireIndex + 3).Trim();
                        }
                        else
                        {
                            // Arşivde ürün adı bulunamadıysa SQL'e bağlanıp soruyoruz
                            uKodu = urunKismi.Trim();
                            var urun = yerelUrunler.FirstOrDefault(u => u.UrunKodu == uKodu || u.Barkod == uKodu);
                            if (urun != null) uAdi = urun.Aciklama;
                            else uAdi = "Bilinmeyen Ürün";
                        }

                        urunler.Add($"<li><span class='k-kod'>• {uKodu}</span><span class='k-ad'>{uAdi}</span><span class='k-adet'>Adet: {adetKismi}</span></li>");
                    }
                }

                string listeHtml = string.Join("", urunler);

                string html = $@"<html>
        <head>
           <meta charset='utf-8'>
           <script src='https://cdn.jsdelivr.net/npm/jsbarcode@3.11.0/dist/JsBarcode.all.min.js'></script>
           <style>
              body {{ font-family: 'Segoe UI', Arial, sans-serif; text-align: center; margin: 10px; }}
              
              /* Firma adı küçültüldü ve maks 2 satır kilidi konuldu */
              .firma {{ font-size: 42px; font-weight: bold; text-transform: uppercase; color: black; margin-bottom: 5px; line-height: 1.1; display: -webkit-box; -webkit-line-clamp: 2; -webkit-box-orient: vertical; overflow: hidden; }}
              .sevk-musteri {{ font-size: 24px; font-weight: 600; text-transform: uppercase; color: #444; margin-bottom: 5px; }}
              .belge {{ font-size: 22px; margin-bottom: 5px; color: #333; font-weight: bold; }}
              .palet {{ font-size: 55px; margin: 10px 0; background: transparent; color: black; font-weight: bold; }}
              
              /* YENİ: Kutu ve İçindeki Listelerin 3'lü Dağılımı */
              .urunler {{ text-align: left; font-size: 20px; font-weight: bold; border: 4px dashed black; padding: 15px; width: 98%; box-sizing: border-box; margin: 0 auto; min-height: 140px; }}
              ul {{ margin: 0; padding-left: 0; list-style-type: none; }}
              
              /* YENİ: Tek Satır, Taşmayan Esnek Tasarım */
              li {{ display: flex; justify-content: space-between; align-items: center; margin-bottom: 8px; border-bottom: 1.5px dashed #ccc; padding-bottom: 6px; }}
              .k-kod {{ flex: 3; text-align: left; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; padding-right: 5px; font-size: 19px; color: black; }}
              .k-ad {{ flex: 5; text-align: left; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; padding-right: 5px; color: #444; font-size: 17px; }}
              .k-adet {{ flex: 2; text-align: right; font-size: 20px; color: black; }}
              
              .barkod-alani {{ margin-top: 20px; }} 
           </style>
        </head>
        <body>
           <div class='firma'>{aktifMusteri}</div>
           <div class='sevk-musteri'>Sevk: {aktifSevkMusteri}</div>
           <div class='belge'>Belge No: {aktifBelge}</div>
           <div class='palet'>{gosterilenPaletAdi}</div>
           
           <div class='urunler'><ul>{listeHtml}</ul></div>
           
           <div class='barkod-alani'><svg id='barkod'></svg></div>
           <script>
              JsBarcode('#barkod', '{barkod}', {{ format: 'EAN13', width: 5, height: 90, displayValue: true, fontSize: 34, fontOptions: 'bold', margin: 0 }});
           </script>
        </body></html>";

                Form frmYazdir = new Form { Text = "Etiket Yazdırılıyor...", Width = 800, Height = 600, StartPosition = FormStartPosition.CenterParent, Icon = this.Icon };
                Microsoft.Web.WebView2.WinForms.WebView2 web = new Microsoft.Web.WebView2.WinForms.WebView2 { Dock = DockStyle.Fill };
                frmYazdir.Controls.Add(web);
                frmYazdir.FormClosed += (s1, e1) => { web.Dispose(); };

                frmYazdir.Shown += async (senderForm, args) =>
                {
                    var ozelHafiza = await Microsoft.Web.WebView2.Core.CoreWebView2Environment.CreateAsync(null, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TamgaApp", "EtiketPrintArsiv"));
                    await web.EnsureCoreWebView2Async(ozelHafiza);
                    web.NavigationCompleted += (s2, e2) => { web.CoreWebView2.ShowPrintUI(Microsoft.Web.WebView2.Core.CoreWebView2PrintDialogKind.Browser); };
                    web.NavigateToString(html);
                };

                frmYazdir.ShowDialog();
            };

            // 🌟 KÜRESEL BARKOD SORGULAMA MOTORU (Cihazdan okutulan EAN-13'ü tüm arşivde arar)
            txtSorgu.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    string arananBarkod = txtSorgu.Text.Trim();
                    if (string.IsNullOrEmpty(arananBarkod)) return;

                    string kokYol = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "TamgaApp Tamamlanan Sevkiyatlar");
                    if (!Directory.Exists(kokYol)) return;

                    string[] tumDosyalar = Directory.GetFiles(kokYol, "*.csv", SearchOption.AllDirectories);
                    List<string> icerikler = new List<string>();
                    string bFirma = "", bTarih = "", bPalet = "";

                    foreach (string dosya in tumDosyalar)
                    {
                        string[] lines = File.ReadAllLines(dosya);
                        bool detayaGec = false;

                        string tMusteri = "", tTarih = "";
                        if (lines.Length > 1) { var huc = lines[1].Split(';'); if (huc.Length >= 4) { tMusteri = huc[0]; tTarih = huc[3]; } }

                        foreach (string line in lines)
                        {
                            if (line.Contains("--- DETAYLAR ---")) { detayaGec = true; continue; }
                            if (detayaGec)
                            {
                                string[] cols = line.Split(';');
                                if (cols.Length >= 3 && cols[2].Trim() == arananBarkod)
                                {
                                    bFirma = tMusteri; bTarih = tTarih; bPalet = cols[0];
                                    icerikler.Add("- " + cols[1]);
                                }
                            }
                        }
                        if (icerikler.Count > 0) break;
                    }

                    if (icerikler.Count > 0)
                        MessageBox.Show($"📌 FİRMA: {bFirma}\n🕒 TARİH: {bTarih}\n📦 PALET: {bPalet}\n\nİÇERİK DÖKÜMÜ:\n{string.Join("\n", icerikler)}", "✅ Palet Bulundu!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    else
                        MessageBox.Show("Bu barkoda ait sistemde kayıtlı hiçbir palet bulunamadı!", "❌ Bulunamadı", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                    txtSorgu.Clear();
                    txtSorgu.Focus();
                }
            };
        }

        // 🌟 YENİ EKLENEN: GELİŞMİŞ BARKOD SORGULAMA VE YAZDIRMA EKRANI
        private void GelistirilmisBarkodArsiviniAc()
        {
            string kokYol = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "TamgaApp Tamamlanan Sevkiyatlar");
            if (!Directory.Exists(kokYol))
            {
                MessageBox.Show("Arşiv bulunamadı.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Form frm = new Form { Text = "Gelişmiş Barkod ve Etiket Arşivi", Size = new Size(1200, 750), StartPosition = FormStartPosition.CenterScreen, Icon = this.Icon, BackColor = Color.WhiteSmoke };

            Panel pnlTop = new Panel { Dock = DockStyle.Top, Height = 70, BackColor = Color.FromArgb(45, 52, 54), ForeColor = Color.White };

            Label lblYil = new Label { Text = "Yıl:", AutoSize = true, Location = new Point(20, 25), Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            ComboBox cmbYil = new ComboBox { Location = new Point(50, 22), Width = 80, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbYil.Items.Add("Tümü"); cmbYil.SelectedIndex = 0;

            Label lblAy = new Label { Text = "Ay:", AutoSize = true, Location = new Point(140, 25), Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            ComboBox cmbAy = new ComboBox { Location = new Point(170, 22), Width = 60, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbAy.Items.Add("Tümü"); cmbAy.SelectedIndex = 0;

            Label lblGun = new Label { Text = "Gün:", AutoSize = true, Location = new Point(240, 25), Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            ComboBox cmbGun = new ComboBox { Location = new Point(280, 22), Width = 60, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbGun.Items.Add("Tümü"); cmbGun.SelectedIndex = 0;

            Label lblArama = new Label { Text = "Firma / Palet Ara:", AutoSize = true, Location = new Point(360, 25), Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            TextBox txtAra = new TextBox { Location = new Point(490, 22), Width = 200, Font = new Font("Segoe UI", 10) };

            Button btnSorgula = new Button { Text = "🔍 Filtrele", Location = new Point(710, 20), Width = 120, Height = 30, BackColor = Color.Teal, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9, FontStyle.Bold) };

            pnlTop.Controls.AddRange(new Control[] { lblYil, cmbYil, lblAy, cmbAy, lblGun, cmbGun, lblArama, txtAra, btnSorgula });

            try
            {
                var yillar = Directory.GetDirectories(kokYol, "*", SearchOption.AllDirectories)
                    .Select(d => new DirectoryInfo(d).Name).Where(n => n.Length == 4 && n.StartsWith("20")).Distinct().OrderBy(x => x);
                foreach (var y in yillar) cmbYil.Items.Add(y);
                for (int i = 1; i <= 12; i++) cmbAy.Items.Add(i.ToString("D2"));
                for (int i = 1; i <= 31; i++) cmbGun.Items.Add(i.ToString("D2"));
            }
            catch { }

            DataGridView dgv = new DataGridView { Dock = DockStyle.Fill, AllowUserToAddRows = false, ReadOnly = true, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, SelectionMode = DataGridViewSelectionMode.FullRowSelect, BackgroundColor = Color.White };
            dgv.Columns.Add("Tarih", "Tarih");
            dgv.Columns.Add("Firma", "Firma Adı");
            dgv.Columns.Add("Belge", "Belge No");
            dgv.Columns.Add("Palet", "Palet Adı");
            dgv.Columns.Add("Barkod", "Barkod Numarası");
            dgv.Columns.Add("Html", "Html"); dgv.Columns["Html"].Visible = false;
            dgv.Columns.Add("SevkMusteri", "SevkMusteri"); dgv.Columns["SevkMusteri"].Visible = false;

            // Görsel Zırh
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(15, 76, 58);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.EnableHeadersVisualStyles = false;
            dgv.DefaultCellStyle.Font = new Font("Segoe UI", 10);
            dgv.RowTemplate.Height = 35;

            Button btnYazdir = new Button { Text = "🖨️ SEÇİLİ PALET ETİKETİNİ YAZDIR", Dock = DockStyle.Bottom, Height = 60, BackColor = Color.Orange, Font = new Font("Segoe UI", 12, FontStyle.Bold), Cursor = Cursors.Hand };

            frm.Controls.Add(dgv);
            frm.Controls.Add(btnYazdir);
            frm.Controls.Add(pnlTop);

            Action TabloyuDoldur = () => {
                dgv.Rows.Clear();
                string sYil = cmbYil.SelectedItem.ToString();
                string sAy = cmbAy.SelectedItem.ToString();
                string sGun = cmbGun.SelectedItem.ToString();
                string aranan = txtAra.Text.Trim().ToLower();

                string[] dosyalar = Directory.GetFiles(kokYol, "*.csv", SearchOption.AllDirectories);

                foreach (string dosya in dosyalar)
                {
                    string[] parcalar = dosya.Substring(kokYol.Length + 1).Split(Path.DirectorySeparatorChar);
                    if (parcalar.Length >= 4)
                    {
                        string dYil = parcalar[1];
                        string dAy = parcalar[2];
                        string dGun = parcalar[3];

                        if (sYil != "Tümü" && dYil != sYil) continue;
                        if (sAy != "Tümü" && dAy != sAy) continue;
                        if (sGun != "Tümü" && dGun != sGun) continue;

                        string[] lines = File.ReadAllLines(dosya, System.Text.Encoding.UTF8);
                        if (lines.Length < 4) continue;

                        string musteri = "", sevkMusteri = "", belgeNo = "", tarih = "";
                        string[] huc = lines[1].Split(';');
                        if (huc.Length >= 4) { musteri = huc[0]; sevkMusteri = huc[1]; belgeNo = huc[2]; tarih = huc[3]; }

                        bool detaylar = false;

                        Dictionary<string, string> paletBarkodlari = new Dictionary<string, string>();
                        Dictionary<string, List<string>> paletIcerikleri = new Dictionary<string, List<string>>();

                        foreach (string line in lines)
                        {
                            if (line.Contains("--- DETAYLAR ---")) { detaylar = true; continue; }
                            if (detaylar && !line.StartsWith("Palet No") && !string.IsNullOrWhiteSpace(line))
                            {
                                string[] cols = line.Split(';');
                                if (cols.Length >= 3)
                                {
                                    string pNo = cols[0].Trim();
                                    string icerik = cols[1].Trim();
                                    string barkod = cols[2].Trim();

                                    if (!paletBarkodlari.ContainsKey(pNo))
                                    {
                                        paletBarkodlari.Add(pNo, barkod);
                                        paletIcerikleri.Add(pNo, new List<string>());
                                    }

                                    string[] parcalar2 = icerik.Split(new string[] { " | Adet: " }, StringSplitOptions.None);
                                    string urunKismi = parcalar2[0];
                                    string adetKismi = parcalar2.Length > 1 ? parcalar2[1] : "1";

                                    int pIdx = urunKismi.LastIndexOf('(');
                                    if (pIdx > 0) urunKismi = urunKismi.Substring(0, pIdx).Trim();

                                    string uKodu = urunKismi;
                                    string uAdi = "";
                                    int tireIndex = urunKismi.IndexOf(" - ");
                                    if (tireIndex > 0)
                                    {
                                        uKodu = urunKismi.Substring(0, tireIndex).Trim();
                                        uAdi = urunKismi.Substring(tireIndex + 3).Trim();
                                    }
                                    else { uKodu = urunKismi.Trim(); uAdi = "Ürün"; }

                                    paletIcerikleri[pNo].Add($"<li><span class='k-kod'>• {uKodu}</span><span class='k-ad'>{uAdi}</span><span class='k-adet'>Adet: {adetKismi}</span></li>");
                                }
                            }
                        }

                        foreach (var kvp in paletBarkodlari)
                        {
                            string pNo = kvp.Key;
                            string barkod = kvp.Value;
                            string htmlList = string.Join("", paletIcerikleri[pNo]);

                            if (!string.IsNullOrEmpty(aranan) && !musteri.ToLower().Contains(aranan) && !pNo.ToLower().Contains(aranan) && !barkod.ToLower().Contains(aranan))
                                continue;

                            dgv.Rows.Add($"{dYil}-{dAy}-{dGun} {tarih}", musteri, belgeNo, pNo, barkod, htmlList, sevkMusteri);
                        }
                    }
                }
            };

            btnSorgula.Click += (s, e) => TabloyuDoldur();
            TabloyuDoldur();

            // 🌟 5. EAN-13 ETİKET YAZDIRMA MOTORU
            btnYazdir.Click += async (s, e) =>
            {
                if (dgv.SelectedRows.Count == 0) { MessageBox.Show("Lütfen yazdırılacak paleti seçin."); return; }

                string pMusteri = dgv.SelectedRows[0].Cells["Firma"].Value.ToString();
                string pBelge = dgv.SelectedRows[0].Cells["Belge"].Value.ToString();
                string pPalet = dgv.SelectedRows[0].Cells["Palet"].Value.ToString();
                string pBarkod = dgv.SelectedRows[0].Cells["Barkod"].Value.ToString();
                string pIcerikHtml = dgv.SelectedRows[0].Cells["Html"].Value.ToString();
                string pSevkMusteri = dgv.SelectedRows[0].Cells["SevkMusteri"].Value.ToString();
                if (string.IsNullOrEmpty(pSevkMusteri)) pSevkMusteri = "Belirtilmedi";

                string html = $@"<html>
        <head>
           <meta charset='utf-8'>
           <script src='https://cdn.jsdelivr.net/npm/jsbarcode@3.11.0/dist/JsBarcode.all.min.js'></script>
           <style>
              body {{ font-family: 'Segoe UI', Arial, sans-serif; text-align: center; margin: 10px; }}
              .firma {{ font-size: 42px; font-weight: bold; text-transform: uppercase; color: black; margin-bottom: 5px; line-height: 1.1; display: -webkit-box; -webkit-line-clamp: 2; -webkit-box-orient: vertical; overflow: hidden; }}
              .sevk-musteri {{ font-size: 24px; font-weight: 600; text-transform: uppercase; color: #444; margin-bottom: 5px; }}
              .belge {{ font-size: 22px; margin-bottom: 5px; color: #333; font-weight: bold; }}
              .palet {{ font-size: 55px; margin: 10px 0; background: transparent; color: black; font-weight: bold; }}
              .urunler {{ text-align: left; font-size: 20px; font-weight: bold; border: 4px dashed black; padding: 15px; width: 98%; box-sizing: border-box; margin: 0 auto; min-height: 140px; }}
              ul {{ margin: 0; padding-left: 0; list-style-type: none; }}
              li {{ display: flex; justify-content: space-between; align-items: center; margin-bottom: 8px; border-bottom: 1.5px dashed #ccc; padding-bottom: 6px; }}
              .k-kod {{ flex: 3; text-align: left; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; padding-right: 5px; font-size: 19px; color: black; }}
              .k-ad {{ flex: 5; text-align: left; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; padding-right: 5px; color: #444; font-size: 17px; }}
              .k-adet {{ flex: 2; text-align: right; font-size: 20px; color: black; }}
              .barkod-alani {{ margin-top: 20px; }} 
           </style>
        </head>
        <body>
           <div class='firma'>{pMusteri}</div>
           <div class='sevk-musteri'>Sevk: {pSevkMusteri}</div>
           <div class='belge'>Belge No: {pBelge}</div>
           <div class='palet'>{pPalet}</div>
           <div class='urunler'><ul>{pIcerikHtml}</ul></div>
           <div class='barkod-alani'><svg id='barkod'></svg></div>
           <script>
              JsBarcode('#barkod', '{pBarkod}', {{ format: 'EAN13', width: 5, height: 90, displayValue: true, fontSize: 34, fontOptions: 'bold', margin: 0 }});
           </script>
        </body></html>";

                Form frmYazdir = new Form { Text = "Etiket Yazdırılıyor...", Width = 800, Height = 600, StartPosition = FormStartPosition.CenterParent, ShowIcon = false };
                Microsoft.Web.WebView2.WinForms.WebView2 web = new Microsoft.Web.WebView2.WinForms.WebView2 { Dock = DockStyle.Fill };
                frmYazdir.Controls.Add(web);
                frmYazdir.FormClosed += (s1, e1) => { web.Dispose(); };

                frmYazdir.Shown += async (senderForm, args) =>
                {
                    var ozelHafiza = await Microsoft.Web.WebView2.Core.CoreWebView2Environment.CreateAsync(null, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TamgaApp", "GelistirilmisPrint"));
                    await web.EnsureCoreWebView2Async(ozelHafiza);
                    web.NavigationCompleted += (s2, e2) => { web.CoreWebView2.ShowPrintUI(Microsoft.Web.WebView2.Core.CoreWebView2PrintDialogKind.Browser); };
                    web.NavigateToString(html);
                };

                frmYazdir.ShowDialog();
            };

            frm.ShowDialog();
        }

        #endregion

        #region 📊 14.6 CANLI SAYIM - STOK SAYFASINDAN (YEREL TABLODAN) VERİ ÇEKME MOTORU

        private string CanliStokGetir(string malzemeKodu)
        {
            // 1. ZIRH: Eğer Stok sekmesi boşsa veya henüz veri çekilmemişse uyar
            if (tabStokPivotlar == null || tabStokPivotlar.TabPages.Count == 0)
            {
                return "Stok Çekilmedi";
            }

            try
            {
                // 2. MOTOR: Stok sekmesinin içindeki tüm alt raporları (sekmeleri) gez
                foreach (TabPage sekme in tabStokPivotlar.TabPages)
                {
                    // Sekmenin içindeki Tabloyu (DataGridView) bul
                    DataGridView dgvStok = sekme.Controls.OfType<DataGridView>().FirstOrDefault();
                    if (dgvStok == null) continue;

                    // 3. SÜTUN DEDEKTÖRÜ: Tabloda Hangi Sütunda Kod, Hangi Sütunda Miktar var?
                    string kodSutunu = null;
                    string miktarSutunu = null;

                    foreach (DataGridViewColumn col in dgvStok.Columns)
                    {
                        string baslik = col.HeaderText.ToUpper().Replace(" ", ""); // Boşlukları silip büyütür (Örn: TOPLAMSTOK)

                        // Malzeme kodunun olabileceği sütun isimleri
                        if (baslik == "MALZEMEKODU" || baslik == "MALZEME" || baslik == "ITEM")
                            kodSutunu = col.Name;

                        // Stok miktarının olabileceği sütun isimleri
                        if (baslik == "TOPLAMSTOK" || baslik == "MİKTAR" || baslik == "STOK" || baslik == "BAKİYE" || baslik == "STKQTY")
                            miktarSutunu = col.Name;
                    }

                    // Eğer bu sekmedeki tabloda uygun sütunlar yoksa, diğer sekmeye geç
                    if (kodSutunu == null || miktarSutunu == null) continue;

                    // 4. VERİYİ BUL VE ÇEK: Tablodaki satırları tara
                    foreach (DataGridViewRow row in dgvStok.Rows)
                    {
                        if (row.IsNewRow) continue;

                        if (row.Cells[kodSutunu].Value != null && row.Cells[kodSutunu].Value.ToString().Trim() == malzemeKodu)
                        {
                            object miktarDegeri = row.Cells[miktarSutunu].Value;
                            if (miktarDegeri != null)
                            {
                                // Ürünü buldu! Miktarı binlik ayracıyla (1.500) şekillendirip hemen gönder.
                                return Convert.ToDouble(miktarDegeri).ToString("N0");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Stok tablosundan okuma hatası: " + ex.Message);
                return "Hata";
            }

            // Eğer tüm sekmeler tarandı ama ürün bulunamadıysa
            return "Yok";
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
                MinimizeBox = false,
                Icon = this.Icon
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

        // Manuel oluşturulan geçici firmayı alıp HTML şablonuna giydiren özel yazdırma motoru
        private async void ManuelZarfiEdgeIleYazdir(Firma manuelFirma)
        {
            if (manuelFirma == null) return;

            // Kağıt ölçülerini arayüzden al
            string wMm = txtPageWidthMm.Text;
            string hMm = txtPageHeightMm.Text;

            // Eğer kağıt yataysa ölçüleri ters çevir ki motor anlasın
            if (rbLandscape != null && rbLandscape.Checked)
            {
                wMm = txtPageHeightMm.Text;
                hMm = txtPageWidthMm.Text;
            }

            // 🌟 SİHİRLİ KISIM: Senin mevcut HTML Çevirici motoruna bu geçici firmayı veriyoruz!
            string htmlIcerik = TasarimiHtmlCevir(designItems, manuelFirma, wMm, hMm);

            // Arka planda yazdırma işlemini başlatacak Edge Penceresini oluştur
            Form modernOnizleme = new Form();
            modernOnizleme.Text = "Manuel Zarf Yazdırılıyor...";
            modernOnizleme.ShowIcon = false;
            modernOnizleme.Width = 1000;
            modernOnizleme.Height = 600;
            modernOnizleme.StartPosition = FormStartPosition.CenterScreen;

            Microsoft.Web.WebView2.WinForms.WebView2 webCizici = new Microsoft.Web.WebView2.WinForms.WebView2();
            webCizici.Dock = DockStyle.Fill;
            modernOnizleme.Controls.Add(webCizici);

            modernOnizleme.FormClosed += (s, ev) => { webCizici.Dispose(); };
            modernOnizleme.Show();

            // Klasör yetki hatasını önlemek için AppData içinde bu işe özel geçici bir profil yarat
            string appDataYolu = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string zarfHafizaYolu = System.IO.Path.Combine(appDataYolu, "TamgaApp", "Profil_ManuelEdgeZarf");

            try
            {
                var ozelHafiza = await Microsoft.Web.WebView2.Core.CoreWebView2Environment.CreateAsync(null, zarfHafizaYolu);
                await webCizici.EnsureCoreWebView2Async(ozelHafiza);
            }
            catch (Exception)
            {
                MessageBox.Show("Yazıcı motoru başlatılamadı. Lütfen 'Edge WebView2 Runtime' kurulu olduğundan emin olun.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                modernOnizleme.Close();
                return;
            }

            // HTML kodlarını motora bas
            webCizici.NavigateToString(htmlIcerik);

            webCizici.NavigationCompleted += (s, args) =>
            {
                // Yükleme bittiği milisaniye doğrudan yazdırma ekranını (Print UI) kullanıcının karşısına çıkart!
                webCizici.CoreWebView2.ShowPrintUI(Microsoft.Web.WebView2.Core.CoreWebView2PrintDialogKind.Browser);
            };
        }

        // 🗂️ ZARF HAFIZA VE SEVKİYAT ARŞİVİ MOTORU (İLAVE EKLEME DESTEKLİ)

        // 🌟 1. HAFIZA BEYNİ VE KALICI DOSYA YOLU (Format atılana kadar silinmez)
        Dictionary<string, string> ZarfHafizasi = new Dictionary<string, string>();
        string ZarfHafizaYolu = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TamgaApp", "ZarfArsivi.json");

        private void ZarfHafizasiniYukle()
        {
            if (System.IO.File.Exists(ZarfHafizaYolu))
            {
                try { ZarfHafizasi = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(System.IO.File.ReadAllText(ZarfHafizaYolu)) ?? new Dictionary<string, string>(); }
                catch { ZarfHafizasi = new Dictionary<string, string>(); }
            }
        }

        private void ZarfHafizasiniKaydet()
        {
            try
            {
                string klasor = System.IO.Path.GetDirectoryName(ZarfHafizaYolu);
                if (!System.IO.Directory.Exists(klasor)) System.IO.Directory.CreateDirectory(klasor);
                System.IO.File.WriteAllText(ZarfHafizaYolu, Newtonsoft.Json.JsonConvert.SerializeObject(ZarfHafizasi, Newtonsoft.Json.Formatting.Indented));
            }
            catch { }
        }

        // 🌟 2. HAFIZA BUTONUNA BASINCA AÇILACAK OLAN ANA PENCERE
        private void btnHafiza_Click(object sender, EventArgs e)
        {
            ZarfHafizasiniYukle(); // Önce diskteki eski kayıtları çek

            Form frmHafiza = new Form
            {
                Text = "📂 Zarf Hafıza ve Sevkiyat Arşivi",
                Size = new Size(550, 480),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.WhiteSmoke,
                Icon = this.Icon
            };

            // SOL TARAF: Kayıtlı Liste
            Label lblListe = new Label { Text = "Kayıtlı Sevkiyatlar / Ölçüler:", Location = new Point(20, 15), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            ListBox lstKayitlar = new ListBox { Location = new Point(20, 40), Size = new Size(220, 370), Font = new Font("Segoe UI", 10) };
            foreach (var kayit in ZarfHafizasi.Keys) lstKayitlar.Items.Add(kayit);

            // SAĞ TARAF: Kontrol Paneli
            Label lblYeniAd = new Label { Text = "Yeni Kayıt Adı (Firma/Ölçü vb.):", Location = new Point(260, 15), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            TextBox txtYeniKayitAd = new TextBox { Location = new Point(260, 40), Size = new Size(250, 25), Font = new Font("Segoe UI", 10) };

            Button btnKaydet = new Button { Text = "💾 Yeni Kayıt Olarak Kaydet", Location = new Point(260, 80), Size = new Size(250, 45), BackColor = Color.Orange, ForeColor = Color.Black, Font = new Font("Segoe UI", 10, FontStyle.Bold), Cursor = Cursors.Hand };

            // 🌟 İŞTE İSTEDİĞİN O YENİ AKILLI BUTON 🌟
            Button btnUstuneEkle = new Button { Text = "➕ Seçili Kayıta İlave Ekle", Location = new Point(260, 140), Size = new Size(250, 45), BackColor = Color.DodgerBlue, ForeColor = Color.White, Font = new Font("Segoe UI", 10, FontStyle.Bold), Cursor = Cursors.Hand };

            Button btnYukle = new Button { Text = "⬇️ Seçili Kaydı Ekrana Yükle", Location = new Point(260, 220), Size = new Size(250, 45), BackColor = Color.Teal, ForeColor = Color.White, Font = new Font("Segoe UI", 10, FontStyle.Bold), Cursor = Cursors.Hand };
            Button btnSil = new Button { Text = "❌ Seçili Kaydı Sil", Location = new Point(260, 280), Size = new Size(250, 45), BackColor = Color.DarkRed, ForeColor = Color.White, Font = new Font("Segoe UI", 10, FontStyle.Bold), Cursor = Cursors.Hand };
            Button btnKapat = new Button { Text = "Kapat", Location = new Point(260, 365), Size = new Size(250, 45), BackColor = Color.LightGray, ForeColor = Color.Black, Font = new Font("Segoe UI", 10, FontStyle.Bold), Cursor = Cursors.Hand };

            // Olaylar (Events)
            btnKapat.Click += (s, ev) => frmHafiza.Close();

            lstKayitlar.SelectedIndexChanged += (s, ev) =>
            {
                if (lstKayitlar.SelectedItem != null) txtYeniKayitAd.Text = lstKayitlar.SelectedItem.ToString();
            };

            // ✅ İŞLEM 1: SIFIRDAN YENİ KAYIT
            btnKaydet.Click += (s, ev) =>
            {
                string ad = txtYeniKayitAd.Text.Trim();
                if (string.IsNullOrEmpty(ad)) { MessageBox.Show("Lütfen bir kayıt adı girin!"); return; }
                if (dgvAmbarSonListe.Rows.Count == 0) { MessageBox.Show("Kaydedilecek veri yok!"); return; }

                var liste = new List<Dictionary<string, string>>();
                foreach (DataGridViewRow r in dgvAmbarSonListe.Rows)
                {
                    if (r.IsNewRow) continue;
                    var satir = new Dictionary<string, string>();
                    for (int i = 0; i < dgvAmbarSonListe.Columns.Count; i++) satir[dgvAmbarSonListe.Columns[i].Name] = r.Cells[i].Value?.ToString() ?? "";
                    liste.Add(satir);
                }

                string json = Newtonsoft.Json.JsonConvert.SerializeObject(liste, Newtonsoft.Json.Formatting.Indented);
                if (ZarfHafizasi.ContainsKey(ad)) ZarfHafizasi[ad] = json;
                else { ZarfHafizasi.Add(ad, json); lstKayitlar.Items.Add(ad); }

                ZarfHafizasiniKaydet();
                MessageBox.Show($"'{ad}' başarıyla kaydedildi!", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            // 🌟 YENİ İŞLEM 2: SEÇİLİ KAYDIN ÜSTÜNE İLAVE ETME (MERGE) MOTORU 🌟
            btnUstuneEkle.Click += (s, ev) =>
            {
                if (lstKayitlar.SelectedItem == null)
                {
                    MessageBox.Show("Lütfen üzerine ekleme yapmak istediğiniz arşivi (rotayı) soldan seçin!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (dgvAmbarSonListe.Rows.Count == 0 || (dgvAmbarSonListe.Rows.Count == 1 && dgvAmbarSonListe.Rows[0].IsNewRow))
                {
                    MessageBox.Show("Ekranda ilave edilecek yeni veri yok! Lütfen önce listeye firma/palet ekleyin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string seciliKayit = lstKayitlar.SelectedItem.ToString();
                string eskiJson = ZarfHafizasi[seciliKayit];

                try
                {
                    // Eski paketi aç
                    var mevcutListe = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(eskiJson) ?? new List<Dictionary<string, string>>();

                    // Ekrana girilen yeni paketleri topla ve eskisinin üstüne ekle
                    int eklenenAdet = 0;
                    foreach (DataGridViewRow r in dgvAmbarSonListe.Rows)
                    {
                        if (r.IsNewRow) continue;
                        var satir = new Dictionary<string, string>();
                        for (int i = 0; i < dgvAmbarSonListe.Columns.Count; i++) satir[dgvAmbarSonListe.Columns[i].Name] = r.Cells[i].Value?.ToString() ?? "";

                        mevcutListe.Add(satir); // Eski listeye yeni satırı gömüyoruz
                        eklenenAdet++;
                    }

                    // Güncellenmiş dev listeyi tekrar paketle ve diske mühürle
                    ZarfHafizasi[seciliKayit] = Newtonsoft.Json.JsonConvert.SerializeObject(mevcutListe, Newtonsoft.Json.Formatting.Indented);
                    ZarfHafizasiniKaydet();

                    MessageBox.Show($"{eklenenAdet} adet yeni palet/firma '{seciliKayit}' arşivinin içine başarıyla eklendi!", "İşlem Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("İlave işlemi sırasında hata oluştu: " + ex.Message, "Kritik Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            // ✅ İŞLEM 3: EKRANA GERİ YÜKLE
            btnYukle.Click += (s, ev) =>
            {
                if (lstKayitlar.SelectedItem == null) { MessageBox.Show("Lütfen yüklenecek bir kayıt seçin!"); return; }
                string secili = lstKayitlar.SelectedItem.ToString();

                try
                {
                    var liste = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(ZarfHafizasi[secili]);
                    dgvAmbarSonListe.Rows.Clear(); // Ekranı temizle

                    foreach (var satir in liste)
                    {
                        int index = dgvAmbarSonListe.Rows.Add();
                        foreach (var kvp in satir)
                        {
                            if (dgvAmbarSonListe.Columns.Contains(kvp.Key))
                                dgvAmbarSonListe.Rows[index].Cells[kvp.Key].Value = kvp.Value;
                        }
                    }
                    MessageBox.Show($"'{secili}' arşivi masaya indirildi!", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    frmHafiza.Close();
                }
                catch { MessageBox.Show("Kayıt çözülürken hata oluştu!"); }
            };

            // ✅ İŞLEM 4: SİL
            btnSil.Click += (s, ev) =>
            {
                if (lstKayitlar.SelectedItem == null) return;
                string secili = lstKayitlar.SelectedItem.ToString();
                if (MessageBox.Show($"'{secili}' kaydı tamamen silinecek. Emin misiniz?", "Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    ZarfHafizasi.Remove(secili);
                    ZarfHafizasiniKaydet();
                    lstKayitlar.Items.Remove(secili);
                    txtYeniKayitAd.Clear();
                }
            };

            frmHafiza.Controls.Add(lblListe); frmHafiza.Controls.Add(lstKayitlar);
            frmHafiza.Controls.Add(lblYeniAd); frmHafiza.Controls.Add(txtYeniKayitAd);
            frmHafiza.Controls.Add(btnKaydet); frmHafiza.Controls.Add(btnUstuneEkle);
            frmHafiza.Controls.Add(btnYukle); frmHafiza.Controls.Add(btnSil); frmHafiza.Controls.Add(btnKapat);

            frmHafiza.ShowDialog();
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
                MinimizeBox = false,
                Icon = this.Icon
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
            Button btnUstuneEkle = new Button { Text = "➕ Seçili Kayıta İlave Ekle", Left = 260, Top = 120, Width = 200, Height = 40, BackColor = Color.DodgerBlue, ForeColor = Color.White, Font = new Font("Segoe UI", 9, FontStyle.Bold), Cursor = Cursors.Hand };
            Button btnDevamEt = new Button { Text = "Seçili Kaydı Ekrana Yükle", Left = 260, Top = 165, Width = 200, Height = 45, BackColor = Color.Teal, ForeColor = Color.White, Font = new Font("Segoe UI", 9, FontStyle.Bold), Cursor = Cursors.Hand };
            Button btnTemizle = new Button { Text = "Seçili Kaydı Sil", Left = 260, Top = 215, Width = 200, Height = 35, BackColor = Color.DarkRed, ForeColor = Color.White, Font = new Font("Segoe UI", 9, FontStyle.Bold), Cursor = Cursors.Hand };
            Button btnKapat = new Button { Text = "Kapat", Left = 260, Top = 310, Width = 200, Height = 35, Cursor = Cursors.Hand };

            frmHafiza.Controls.Add(lblListe); frmHafiza.Controls.Add(lstKayitlar);
            frmHafiza.Controls.Add(lblYeni); frmHafiza.Controls.Add(txtYeniKayitAdi);
            frmHafiza.Controls.Add(btnAskijaAl); frmHafiza.Controls.Add(btnUstuneEkle);
            frmHafiza.Controls.Add(btnDevamEt);
            frmHafiza.Controls.Add(btnTemizle); frmHafiza.Controls.Add(btnKapat);

            // 3. AKSİYON: YENİ KAYIT (ŞİFRELİ KAYIT MOTORU)
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

                    void TabloyuKaydet(DataGridView dgv, string etiket)
                    {
                        if (dgv.Columns.Count > 0)
                        {
                            List<string> basliklar = new List<string>();
                            foreach (DataGridViewColumn col in dgv.Columns) basliklar.Add(col.HeaderText);
                            askidakiVeriler.Add($"HEADER_{etiket}|" + string.Join("|", basliklar));

                            foreach (DataGridViewRow satir in dgv.Rows)
                            {
                                if (satir.IsNewRow) continue;
                                List<string> hucreler = new List<string>();
                                for (int i = 0; i < satir.Cells.Count; i++)
                                {
                                    // 🌟 SATIR ATLATMA ZIRHI: txt dosyasını bozmaması için \n karakterlerini şifreliyoruz
                                    string hucreDegeri = satir.Cells[i].Value?.ToString() ?? "";
                                    hucreDegeri = hucreDegeri.Replace("\r", "").Replace("\n", "[YENISATIR]");
                                    hucreler.Add(hucreDegeri);
                                }
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

            // 🌟 İLAVE ETME (ŞİFRELİ) 🌟
            btnUstuneEkle.Click += (s, args) =>
            {
                if (lstKayitlar.SelectedIndex == -1)
                {
                    MessageBox.Show("Lütfen üzerine ekleme yapmak istediğiniz arşivi soldan seçin!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string seciliDosya = System.IO.Path.Combine(klasorYolu, lstKayitlar.SelectedItem.ToString() + ".txt");

                try
                {
                    List<string> eklenecekVeriler = new List<string>();

                    void TablodanYeniVeriAl(DataGridView dgv, string etiket)
                    {
                        foreach (DataGridViewRow satir in dgv.Rows)
                        {
                            if (satir.IsNewRow) continue;
                            List<string> hucreler = new List<string>();
                            for (int i = 0; i < satir.Cells.Count; i++)
                            {
                                // 🌟 SATIR ATLATMA ZIRHI
                                string hucreDegeri = satir.Cells[i].Value?.ToString() ?? "";
                                hucreDegeri = hucreDegeri.Replace("\r", "").Replace("\n", "[YENISATIR]");
                                hucreler.Add(hucreDegeri);
                            }
                            eklenecekVeriler.Add($"{etiket}|" + string.Join("|", hucreler));
                        }
                    }

                    TablodanYeniVeriAl(dgvAmbarSecilenFirmalar, "SECILEN");
                    TablodanYeniVeriAl(dgvPaletler, "PALET");
                    TablodanYeniVeriAl(dgvAmbarSonListe, "SONLISTE");

                    if (eklenecekVeriler.Count == 0)
                    {
                        MessageBox.Show("Ekranda ilave edilecek yeni veri yok! Lütfen önce tablolara veri ekleyin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    System.IO.File.AppendAllLines(seciliDosya, eklenecekVeriler);

                    dgvAmbarSecilenFirmalar.Rows.Clear(); dgvPaletler.Rows.Clear(); dgvAmbarSonListe.Rows.Clear();
                    MessageBox.Show("Yeni kayıtlar başarıyla seçili arşivin içine ilave edildi!", "İşlem Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex) { MessageBox.Show("İlave işlemi sırasında hata oluştu:\n" + ex.Message, "Kritik Hata", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            };

            // 4. AKSİYON: GERİ YÜKLE (ŞİFRE ÇÖZÜCÜ EKLENDİ)
            btnDevamEt.Click += (s, args) =>
            {
                if (lstKayitlar.SelectedIndex == -1) return;
                string seciliDosya = System.IO.Path.Combine(klasorYolu, lstKayitlar.SelectedItem.ToString() + ".txt");

                try
                {
                    string[] askidakiVeriler = System.IO.File.ReadAllLines(seciliDosya);

                    dgvAmbarSecilenFirmalar.Rows.Clear();
                    dgvPaletler.Rows.Clear();
                    dgvAmbarSonListe.Rows.Clear();

                    foreach (string satir in askidakiVeriler)
                    {
                        string[] parcalar = satir.Split('|');
                        string tabloAdi = parcalar[0];
                        string[] eklenecekVeri = new string[parcalar.Length - 1];

                        // 🌟 ŞİFRE ÇÖZÜCÜ: Şifrelenmiş satır atlatmaları tekrar gerçek alt satıra (\n) çeviriyoruz
                        for (int i = 1; i < parcalar.Length; i++)
                        {
                            eklenecekVeri[i - 1] = parcalar[i].Replace("[YENISATIR]", "\n");
                        }

                        if (tabloAdi.StartsWith("HEADER_"))
                        {
                            DataGridView hedef = tabloAdi == "HEADER_SECILEN" ? dgvAmbarSecilenFirmalar :
                                                 tabloAdi == "HEADER_PALET" ? dgvPaletler : dgvAmbarSonListe;

                            if (hedef.ColumnCount == 0)
                            {
                                for (int i = 0; i < eklenecekVeri.Length; i++) hedef.Columns.Add($"col{i}", eklenecekVeri[i]);
                            }
                        }
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

        // =========================================================================================

        #region 🏷️ 18. SERİ ETİKET YAZDIRMA (EXCEL MAKROSU YERİNE)

        // 1. Excel Makrosunun HTML & CSS Karşılığı (Devasa Fontlar ve Kalınlık Ayarları)
        private string SeriEtiketHtmlOlustur(string firma, string urun, string arac, int baslangic, int kacarli, int toplamPalet)
        {
            System.Text.StringBuilder html = new System.Text.StringBuilder();
            html.AppendLine("<!DOCTYPE html><html><head><meta charset='utf-8'><style>");

            // A4 Yatay (Landscape) kağıt ayarı. Devasa etiketler genelde yatay A4'e basılır.
            html.AppendLine("@page { size: A4 landscape; margin: 0; }");
            html.AppendLine("body { margin: 0; padding: 0; font-family: 'Times New Roman', serif; text-align: center; }");

            // Her bir etiketi ekranın tam ortasına hizalar ve her etiketten sonra yeni kağıda geçer
            html.AppendLine(".sayfa { width: 100vw; height: 100vh; display: flex; flex-direction: column; justify-content: center; align-items: center; page-break-after: always; overflow: hidden; background-color: white; }");

            // VBA kodundaki '.Font.Size = 72' ve '.Bold = True' ayarlarının birebir CSS karşılığı
            html.AppendLine(".firma { font-size: 72pt; font-weight: bold; margin-bottom: 30px; }");
            html.AppendLine(".urun { font-size: 72pt; font-weight: normal; margin-bottom: 30px; }");
            html.AppendLine(".palet { font-size: 72pt; font-weight: bold; }");
            html.AppendLine("</style></head><body>");

            // Excel'deki For i ve For j döngülerinin aynısı
            for (int i = baslangic; i < baslangic + toplamPalet; i++)
            {
                for (int j = 0; j < kacarli; j++)
                {
                    html.AppendLine("<div class='sayfa'>");
                    html.AppendLine($"<div class='firma'>{firma}</div>");
                    html.AppendLine($"<div class='urun'>{urun}</div>");
                    html.AppendLine($"<div class='palet'>{arac} {i}.PALET</div>");
                    html.AppendLine("</div>");
                }
            }

            html.AppendLine("</body></html>");
            return html.ToString();
        }

        // 2. Butona basılınca çalışacak Ana Motor (O Şık Edge Ekranı)
        private async void RunSeriEtiketPrint()
        {
            // Arayüzdeki (Resmini attığın formdaki) kutuların verilerini al
            // NOT: Arayüzdeki nesnelerinin adını bu isimlerle değiştir (veya burayı kendine göre uyarla)
            string firma = txtSeriFirma.Text.Trim();
            string urun = txtSeriUrun.Text.Trim();
            string arac = txtSeriArac.Text.Trim();

            if (string.IsNullOrWhiteSpace(firma))
            {
                MessageBox.Show("Lütfen en azından bir Firma adı giriniz!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Kullanıcı harf falan girdiyse çökmeyi önleyen, varsayılan değer atayan zırh (TryParse)
            int baslangic = int.TryParse(txtSeriBaslangic.Text, out int b) ? b : 1;
            int kacarli = int.TryParse(txtSeriKacarli.Text, out int k) ? k : 1;
            int toplamPalet = int.TryParse(txtSeriToplamPalet.Text, out int t) ? t : 1;

            // HTML İçeriğini Oluştur
            string htmlIcerik = SeriEtiketHtmlOlustur(firma, urun, arac, baslangic, kacarli, toplamPalet);

            // Modern Önizleme Penceresini Yarat
            Form modernOnizleme = new Form();
            modernOnizleme.Text = "Seri Etiket Yazdırma";
            modernOnizleme.ShowIcon = false;
            modernOnizleme.Width = 1000;
            modernOnizleme.Height = 600;
            modernOnizleme.StartPosition = FormStartPosition.CenterScreen;

            Microsoft.Web.WebView2.WinForms.WebView2 webCizici = new Microsoft.Web.WebView2.WinForms.WebView2();
            webCizici.Dock = DockStyle.Fill;
            modernOnizleme.Controls.Add(webCizici);

            modernOnizleme.FormClosed += (s, ev) => { webCizici.Dispose(); };
            modernOnizleme.Show();

            string appDataYolu = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string zarfHafizaYolu = System.IO.Path.Combine(appDataYolu, "TamgaApp", "Profil_SeriEtiket");
            var ozelHafiza = await Microsoft.Web.WebView2.Core.CoreWebView2Environment.CreateAsync(null, zarfHafizaYolu);

            await webCizici.EnsureCoreWebView2Async(ozelHafiza);
            webCizici.NavigateToString(htmlIcerik);

            webCizici.NavigationCompleted += (s, args) =>
            {
                // Yükleme bitince otomatik yazdırma penceresini çıkart
                webCizici.CoreWebView2.ShowPrintUI(Microsoft.Web.WebView2.Core.CoreWebView2PrintDialogKind.Browser);
            };
        }

        // 3. Resmini Attığın Formdaki Önizle ve Yazdır Butonlarının Olayları
        // İstersen doğrudan arayüzdeki butonlarına çift tıklayıp içlerine "RunSeriEtiketPrint();" yazabilirsin.
        private void btnSeriOnizle_Click(object sender, EventArgs e) { RunSeriEtiketPrint(); }
        private void btnSeriYazdir_Click(object sender, EventArgs e) { RunSeriEtiketPrint(); }

        #endregion

        // =========================================================================================

        #region 📦 19. AKILLI DESİ HESAPLAMA MOTORU (ÇOKLU ZARF)
        private void dgvPaletler_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            // Sütun indeksini (1 = Ebatlar Sütunu) baz alıyoruz ki isim değişse bile çökmesin
            if (e.ColumnIndex == 1 && e.RowIndex >= 0)
            {
                var hucreDegeri = dgvPaletler.Rows[e.RowIndex].Cells[1].Value;

                if (hucreDegeri != null && !string.IsNullOrWhiteSpace(hucreDegeri.ToString()))
                {
                    string girdi = hucreDegeri.ToString().Trim().ToLower();
                    double desi = 0;

                    try
                    {
                        // 🌟 1. DURUM: Araya herhangi bir işaret konduysa (*, x, - veya boşluk)
                        // Kural: Kaç haneli olursa olsun tam 3 sayı (En, Boy, Yük) girilmesi şarttır!
                        if (girdi.Contains("*") || girdi.Contains("x") || girdi.Contains(" ") || girdi.Contains("-"))
                        {
                            girdi = girdi.Replace("x", "*").Replace(" ", "*").Replace("-", "*");
                            string[] parcalar = girdi.Split(new char[] { '*' }, StringSplitOptions.RemoveEmptyEntries);

                            // Sadece 3 parça varsa işlemi yap (Hane sayısı önemli değil)
                            if (parcalar.Length == 3)
                            {
                                if (double.TryParse(parcalar[0].Trim(), out double en) &&
                                    double.TryParse(parcalar[1].Trim(), out double boy) &&
                                    double.TryParse(parcalar[2].Trim(), out double yuk))
                                {
                                    desi = (en * boy * yuk) / 3000.0;

                                    // Kutuyu da jilet gibi formata geri çevir (Örn: 2x123x23 yazdıysa 2*123*23 yapar)
                                    dgvPaletler.Rows[e.RowIndex].Cells[1].Value = $"{en}*{boy}*{yuk}";
                                }
                            }
                            else
                            {
                                MessageBox.Show("Lütfen ölçüleri 'En x Boy x Yükseklik' formatında tam 3 parça olarak girin! (Örn: 10x20x30)", "Format Hatası", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            }
                        }
                        // 🌟 2. DURUM: Dümdüz bitişik rakam yazıldıysa (Örn: 6510012, 80120150)
                        else
                        {
                            string sadeceRakam = new string(girdi.Where(char.IsDigit).ToArray());
                            double en = 0, boy = 0, yuk = 0;

                            if (sadeceRakam.Length == 9) // Örn: 100120150 -> 100*120*150
                            {
                                en = Convert.ToDouble(sadeceRakam.Substring(0, 3));
                                boy = Convert.ToDouble(sadeceRakam.Substring(3, 3));
                                yuk = Convert.ToDouble(sadeceRakam.Substring(6, 3));
                            }
                            else if (sadeceRakam.Length == 8) // İKİ İHTİMAL VAR
                            {
                                if (sadeceRakam.Substring(3, 3) == "100" || sadeceRakam.Substring(3, 3) == "120") // Örn: 10012015 -> 100*120*15
                                {
                                    en = Convert.ToDouble(sadeceRakam.Substring(0, 3));
                                    boy = Convert.ToDouble(sadeceRakam.Substring(3, 3));
                                    yuk = Convert.ToDouble(sadeceRakam.Substring(6, 2));
                                }
                                else if (sadeceRakam.Substring(2, 3) == "100" || sadeceRakam.Substring(2, 3) == "120") // Örn: 80120150 -> 80*120*150
                                {
                                    en = Convert.ToDouble(sadeceRakam.Substring(0, 2));
                                    boy = Convert.ToDouble(sadeceRakam.Substring(2, 3));
                                    yuk = Convert.ToDouble(sadeceRakam.Substring(5, 3));
                                }
                                else // Örn: 12080150 -> 120*80*150
                                {
                                    en = Convert.ToDouble(sadeceRakam.Substring(0, 3));
                                    boy = Convert.ToDouble(sadeceRakam.Substring(3, 2));
                                    yuk = Convert.ToDouble(sadeceRakam.Substring(5, 3));
                                }
                            }
                            else if (sadeceRakam.Length == 7) // İKİ İHTİMAL VAR
                            {
                                // 🌟 İŞTE SENİN HATAYI ÇÖZEN YER: Ortadaki rakam 100 veya 120 ise anla ki bu 65x100x12'dir!
                                if (sadeceRakam.Substring(2, 3) == "100" || sadeceRakam.Substring(2, 3) == "120")
                                {
                                    en = Convert.ToDouble(sadeceRakam.Substring(0, 2)); // 65
                                    boy = Convert.ToDouble(sadeceRakam.Substring(2, 3)); // 100
                                    yuk = Convert.ToDouble(sadeceRakam.Substring(5, 2)); // 12
                                }
                                else // Ortası 100 değilse anla ki bu 65x80x150'dir!
                                {
                                    en = Convert.ToDouble(sadeceRakam.Substring(0, 2)); // 65
                                    boy = Convert.ToDouble(sadeceRakam.Substring(2, 2)); // 80
                                    yuk = Convert.ToDouble(sadeceRakam.Substring(4, 3)); // 150
                                }
                            }
                            else if (sadeceRakam.Length == 6) // Örn: 658080 -> 65*80*80
                            {
                                en = Convert.ToDouble(sadeceRakam.Substring(0, 2));
                                boy = Convert.ToDouble(sadeceRakam.Substring(2, 2));
                                yuk = Convert.ToDouble(sadeceRakam.Substring(4, 2));
                            }

                            if (en > 0 && boy > 0 && yuk > 0)
                            {
                                desi = (en * boy * yuk) / 3000.0;
                                dgvPaletler.Rows[e.RowIndex].Cells[1].Value = $"{en}*{boy}*{yuk}";
                            }
                        }

                        // 🌟 SONUÇ: Desi başarıyla hesaplandıysa yuvarla ve yan hücreye çak!
                        if (desi > 0)
                        {
                            dgvPaletler.Rows[e.RowIndex].Cells[2].Value = Math.Round(desi, 0) + " Ds.";
                        }
                    }
                    catch
                    {
                        // Kullanıcı saçma sapan bir şey yazarsa program çökmesin, görmezden gelsin
                    }
                }
            }
        }
        #endregion

        // =========================================================================================

        #region ⚙️20. KÜRESEL KLASÖR AYARLARI MERKEZİ
        public class KlasorAyarlari
        {
            public string UretimYolu { get; set; }
            public string SevkArsivYolu { get; set; }
            public string SevkRaporYolu { get; set; }
            public string YarimSevkYolu { get; set; }
            public string SayimYolu { get; set; }

            // Ayarları JSON olarak kaydeder
            public static void AyarlariKaydet(KlasorAyarlari ayarlar)
            {
                string dosyaYolu = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "KlasorAyarlari.json");
                System.IO.File.WriteAllText(dosyaYolu, Newtonsoft.Json.JsonConvert.SerializeObject(ayarlar, Newtonsoft.Json.Formatting.Indented));
            }

            // Ayarları JSON'dan okur, yoksa varsayılan olarak Masaüstünü tanımlar
            public static KlasorAyarlari AyarlariYukle()
            {
                string dosyaYolu = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "KlasorAyarlari.json");
                string masaustu = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

                if (System.IO.File.Exists(dosyaYolu))
                {
                    string json = System.IO.File.ReadAllText(dosyaYolu);
                    return Newtonsoft.Json.JsonConvert.DeserializeObject<KlasorAyarlari>(json);
                }
                else
                {
                    // İlk açılışta varsayılan yollar masaüstü klasörleridir
                    return new KlasorAyarlari
                    {
                        UretimYolu = System.IO.Path.Combine(masaustu, "Günlük Üretim Takip"),
                        SevkArsivYolu = System.IO.Path.Combine(masaustu, "TamgaApp Tamamlanan Sevkiyatlar"),
                        SevkRaporYolu = System.IO.Path.Combine(masaustu, "TamgaApp Sevkiyat Raporları"),
                        YarimSevkYolu = System.IO.Path.Combine(masaustu, "TamgaApp Yarım Sevkiyatlar"),
                        SayimYolu = System.IO.Path.Combine(masaustu, "TamgaApp Sayım Raporları")
                    };
                }
            }
        }
        #endregion

        // =========================================================================================

        #region ⚙️21. AYARLAR EKRANI BUTON İŞLEMLERİ

        // Form yüklenirken (Load olayında) bu metodu çağır ki kutular dolsun!
        private void KlasorAyarlariniEkranaGetir()
        {
            KlasorAyarlari aktifAyarlar = KlasorAyarlari.AyarlariYukle();
            txtUretimYolu.Text = aktifAyarlar.UretimYolu;
            txtSevkArsivYolu.Text = aktifAyarlar.SevkArsivYolu;
            txtSevkRaporYolu.Text = aktifAyarlar.SevkRaporYolu;
            txtYarimSevkYolu.Text = aktifAyarlar.YarimSevkYolu;
            txtSayimYolu.Text = aktifAyarlar.SayimYolu;
        }

        // GÖZAT BUTONLARI (Kısa ve tek satırlık pratik atamalar)
        private void btnUretimSec_Click(object sender, EventArgs e) { KlasorSec(txtUretimYolu); }
        private void btnSevkArsivSec_Click(object sender, EventArgs e) { KlasorSec(txtSevkArsivYolu); }
        private void btnSevkRaporSec_Click(object sender, EventArgs e) { KlasorSec(txtSevkRaporYolu); }
        private void btnYarimSevkSec_Click(object sender, EventArgs e) { KlasorSec(txtYarimSevkYolu); }
        private void btnSayimSec_Click(object sender, EventArgs e) { KlasorSec(txtSayimYolu); }

        // Ortak Klasör Seçme Motoru
        private void KlasorSec(TextBox hedefKutu)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    hedefKutu.Text = fbd.SelectedPath;
                }
            }
        }

        // KAYDET BUTONU
        private void btnAyarlariKaydet_Click(object sender, EventArgs e)
        {
            KlasorAyarlari yeniAyarlar = new KlasorAyarlari
            {
                UretimYolu = txtUretimYolu.Text,
                SevkArsivYolu = txtSevkArsivYolu.Text,
                SevkRaporYolu = txtSevkRaporYolu.Text,
                YarimSevkYolu = txtYarimSevkYolu.Text,
                SayimYolu = txtSayimYolu.Text
            };

            KlasorAyarlari.AyarlariKaydet(yeniAyarlar);
            MessageBox.Show("Tüm klasör yolları başarıyla kaydedildi! Sistem artık bu yolları kullanacak.", "Ayarlar Kaydedildi", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        #endregion

        // =========================================================================================

        #region 🚛 22. KAMYON YÜKLEME (KIOSK) MODÜLÜ - SEVKİYAT PLAN SAYFASI
        private void btnKamyonYukle_Click(object sender, EventArgs e)
        {
            string kokYol = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "TamgaApp Tamamlanan Sevkiyatlar");
            if (!Directory.Exists(kokYol))
            {
                MessageBox.Show("Henüz tamamlanmış (arşive alınmış) hiçbir sevkiyat bulunamadı. Lütfen önce sevkiyat işlemi yapın.", "Arşiv Boş", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            Form frmSecim = new Form
            {
                Text = "Araç Yükleme - Sevkiyat Dosyası Seçimi",
                Size = new Size(800, 600),
                StartPosition = FormStartPosition.CenterScreen,
                Icon = this.Icon,
                ShowIcon = false
            };

            TreeView tvArsiv = new TreeView { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 12) };
            Button btnDevam = new Button { Text = "🚀 SEÇİLİ SEVKİYATIN KAMYON YÜKLEMESİNİ BAŞLAT", Dock = DockStyle.Bottom, Height = 60, BackColor = Color.MediumSeaGreen, ForeColor = Color.White, Font = new Font("Segoe UI", 12, FontStyle.Bold), Cursor = Cursors.Hand };

            frmSecim.Controls.Add(tvArsiv);
            frmSecim.Controls.Add(btnDevam);

            TreeNode kok = new TreeNode("📦 Tamamlanan Sevkiyatlar") { Tag = "KOK" };
            tvArsiv.Nodes.Add(kok);

            void KlasorTaramasi(string dizin, TreeNode ebeveyn)
            {
                foreach (string klasor in Directory.GetDirectories(dizin))
                {
                    TreeNode dugum = new TreeNode("📁 " + Path.GetFileName(klasor)) { Tag = klasor };
                    ebeveyn.Nodes.Add(dugum);
                    KlasorTaramasi(klasor, dugum);
                }
                foreach (string dosya in Directory.GetFiles(dizin, "*.csv"))
                {
                    ebeveyn.Nodes.Add(new TreeNode("📄 " + Path.GetFileNameWithoutExtension(dosya)) { Tag = dosya, ForeColor = Color.DarkBlue });
                }
            }

            KlasorTaramasi(kokYol, kok);
            kok.ExpandAll();

            // 🌟 HATAYI ÇÖZEN ZIRH: Dictionary yerine artık yeni KioskPaletModel Listesi kullanıyoruz!
            List<FrmKamyonKiosk.KioskPaletModel> secilenPaletler = null;
            string secilenMusteriAdi = "";

            btnDevam.Click += (s2, e2) =>
            {
                if (tvArsiv.SelectedNode == null || tvArsiv.SelectedNode.Tag == null || !tvArsiv.SelectedNode.Tag.ToString().EndsWith(".csv"))
                {
                    MessageBox.Show("Lütfen kamyona yüklenecek sevkiyat dosyasını (📄) listeden seçin!", "Seçim Yapılmadı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string seciliDosya = tvArsiv.SelectedNode.Tag.ToString();
                string[] satirlar = File.ReadAllLines(seciliDosya, System.Text.Encoding.UTF8);
                if (satirlar.Length < 4) { return; }

                secilenMusteriAdi = satirlar[1].Split(';')[0];

                // Kiosk listesini RAM'de oluştur
                secilenPaletler = new List<FrmKamyonKiosk.KioskPaletModel>();
                bool detaylarBasladi = false;

                foreach (string satir in satirlar)
                {
                    if (satir.Contains("--- DETAYLAR ---")) { detaylarBasladi = true; continue; }
                    if (detaylarBasladi && !satir.StartsWith("Palet No") && !string.IsNullOrWhiteSpace(satir))
                    {
                        string[] hucreler = satir.Split(';');
                        if (hucreler.Length >= 3)
                        {
                            string pAdi = hucreler[0].Trim();
                            string pBarkod = hucreler[2].Trim();

                            // 🌟 YENİ FORMAT: Eğer barkod listeye henüz eklenmemişse, onu yeni modele çevirip ekle
                            if (!string.IsNullOrEmpty(pBarkod) && !secilenPaletler.Any(p => p.Barkod == pBarkod))
                            {
                                secilenPaletler.Add(new FrmKamyonKiosk.KioskPaletModel
                                {
                                    Barkod = pBarkod,
                                    PaletAdi = pAdi,
                                    EtiketBasildiMi = true, // Geçmiş arşivden geldiği için etiketi basılı ve sağlam kabul ediyoruz
                                    EtiketHtml = ""         // Zaten basılı olduğu için HTML şablonuna gerek yok
                                });
                            }
                        }
                    }
                }

                if (secilenPaletler.Count == 0) return;

                // Seçim ekranını KÖKTEN Kapat
                frmSecim.DialogResult = DialogResult.OK;
            };

            // Kiosk Başlıyor
            if (frmSecim.ShowDialog() == DialogResult.OK && secilenPaletler != null)
            {
                FrmKamyonKiosk kiosk = new FrmKamyonKiosk(secilenMusteriAdi, secilenPaletler);
                kiosk.ShowDialog();
            }
        }
        #endregion

        // =========================================================================================

        #region 🚛 23. KIOSK MOTORU (TAM EKRAN YÜKLEME EKRANI)
        public class FrmKamyonKiosk : Form
        {
            // 🌟 YENİ: Kiosk için özel geliştirilmiş palet modeli
            public class KioskPaletModel
            {
                public string Barkod { get; set; }
                public string PaletAdi { get; set; }
                public bool EtiketBasildiMi { get; set; }
                public string EtiketHtml { get; set; }
            }

            private List<KioskPaletModel> paletModelleri;
            private List<string> okutulanBarkodlar;
            private string firmaAdi;

            private Panel pnlOrta;
            private Label lblMesaj;
            private ListBox lstSagPanel;
            private TextBox txtGizliBarkod;
            private Timer renkSifirlayici;

            public FrmKamyonKiosk(string musteriAdi, List<KioskPaletModel> paletler)
            {
                this.firmaAdi = musteriAdi;
                this.paletModelleri = paletler;
                this.okutulanBarkodlar = new List<string>();

                this.Text = "Kamyon Yükleme Kiosk";
                this.WindowState = FormWindowState.Maximized;
                this.FormBorderStyle = FormBorderStyle.None;
                this.BackColor = Color.FromArgb(33, 37, 41);
                this.KeyPreview = true;
                this.TopMost = true;

                renkSifirlayici = new Timer { Interval = 2000 };
                renkSifirlayici.Tick += (s, e) => SinyalSifirla();

                ArayuzuCiz();
                ListeyiGuncelle();
            }

            private void ArayuzuCiz()
            {
                Panel pnlSag = new Panel { Dock = DockStyle.Right, Width = 350, BackColor = Color.White };
                Label lblFirma = new Label { Text = firmaAdi, Dock = DockStyle.Top, Height = 90, Font = new Font("Segoe UI", 18, FontStyle.Bold), TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.FromArgb(15, 76, 58), ForeColor = Color.White };

                lstSagPanel = new ListBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 15, FontStyle.Bold), ItemHeight = 35, IntegralHeight = false };

                // 🌟 SİHİRLİ SAĞ TIK MENÜSÜ BURADA BAŞLIYOR 🌟
                lstSagPanel.MouseDown += (s, e) =>
                {
                    if (e.Button == MouseButtons.Right)
                    {
                        int index = lstSagPanel.IndexFromPoint(e.Location);
                        if (index != ListBox.NoMatches)
                        {
                            lstSagPanel.SelectedIndex = index;
                            var seciliModel = paletModelleri[index];

                            // Sadece okutulmamış ürünlerde sağ tıka izin ver
                            if (!okutulanBarkodlar.Contains(seciliModel.Barkod))
                            {
                                ContextMenuStrip menu = new ContextMenuStrip();
                                menu.Font = new Font("Segoe UI", 12, FontStyle.Bold);

                                // Sadece etiketi basılmamış olanlara yazdırma izni ver
                                if (!seciliModel.EtiketBasildiMi)
                                {
                                    ToolStripMenuItem btnYazdir = new ToolStripMenuItem("🖨️ Etiketi Yazdır");
                                    btnYazdir.Click += (ms, me) => { EtiketYazdir(seciliModel); };
                                    menu.Items.Add(btnYazdir);
                                }

                                ToolStripMenuItem btnAtla = new ToolStripMenuItem("⏭️ Manuel Onayla (Atla)");
                                btnAtla.Click += (ms, me) => { DisaridanBarkodGeldi(seciliModel.Barkod); };
                                menu.Items.Add(btnAtla);

                                // 🌟 YENİ: PALET İPTAL (YÜKLEMEDEN ÇIKARTMA) BUTONU
                                ToolStripMenuItem btnIptal = new ToolStripMenuItem("❌ Paleti İptal Et (Yüklemeden Çıkar)");
                                btnIptal.ForeColor = Color.DarkRed;
                                btnIptal.Click += (ms, me) =>
                                {
                                    DialogResult onay = MessageBox.Show($"'{seciliModel.PaletAdi}' kamyona yüklenmekten İPTAL edilecek.\n\nBu işlem paleti listeden çıkartır ve sistem bu paleti araca yüklemenizi beklemez. Emin misiniz?", "Palet İptal", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                                    if (onay == DialogResult.Yes)
                                    {
                                        paletModelleri.Remove(seciliModel);
                                        if (okutulanBarkodlar.Contains(seciliModel.Barkod)) okutulanBarkodlar.Remove(seciliModel.Barkod);

                                        ListeyiGuncelle();

                                        if (paletModelleri.Count > 0 && okutulanBarkodlar.Count == paletModelleri.Count)
                                        {
                                            renkSifirlayici.Stop();
                                            pnlOrta.BackColor = Color.FromArgb(46, 204, 113);
                                            lblMesaj.Text = "🎉 YÜKLEME TAMAMLANDI!\nTÜM PALETLER ARAÇTA.";
                                            MessageBox.Show("Kalan tüm paletler başarıyla araca yüklendi. Araç çıkış yapabilir.", "Sevkiyat Tamamlandı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                            this.DialogResult = DialogResult.OK;
                                            this.Close();
                                        }
                                        else if (paletModelleri.Count == 0)
                                        {
                                            MessageBox.Show("Yüklenecek hiçbir palet kalmadı. İşlem iptal edildi.", "İptal", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                            this.Close();
                                        }
                                    }
                                };

                                menu.Items.Add(new ToolStripSeparator());
                                menu.Items.Add(btnIptal);

                                menu.Show(Cursor.Position);
                            }
                        }
                    }
                };

                pnlSag.Controls.Add(lstSagPanel);
                pnlSag.Controls.Add(lblFirma);
                this.Controls.Add(pnlSag);

                pnlOrta = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(33, 37, 41) };
                lblMesaj = new Label { Text = "ARAÇ YÜKLEMESİ HAZIR\n\nİLK PALET ETİKETİNİ OKUTUNUZ...", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 55, FontStyle.Bold), TextAlign = ContentAlignment.MiddleCenter, ForeColor = Color.White };
                pnlOrta.Controls.Add(lblMesaj);
                this.Controls.Add(pnlOrta);

                Button btnCikis = new Button { Text = "X ÇIKIŞ", Size = new Size(150, 60), Location = new Point(20, 20), BackColor = Color.Red, ForeColor = Color.White, Font = new Font("Segoe UI", 16, FontStyle.Bold), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
                btnCikis.Click += (s, e) => this.Close();
                pnlOrta.Controls.Add(btnCikis);
                btnCikis.BringToFront();

                Button btnManuel = new Button { Text = "✍️ MANUEL EKLE", Size = new Size(220, 60), Location = new Point(190, 20), BackColor = Color.DarkOrange, ForeColor = Color.White, Font = new Font("Segoe UI", 16, FontStyle.Bold), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
                btnManuel.Click += BtnManuel_Click;
                pnlOrta.Controls.Add(btnManuel);
                btnManuel.BringToFront();

                txtGizliBarkod = new TextBox { Width = 0, Height = 0, Location = new Point(-100, -100) };
                txtGizliBarkod.KeyDown += Barkod_KeyDown;
                this.Controls.Add(txtGizliBarkod);
            }

            // 🌟 KİOSK İÇİNDEN DİREKT ETİKET YAZDIRMA MOTORU 🌟
            private void EtiketYazdir(KioskPaletModel model)
            {
                Form frmYazdir = new Form { Text = "Etiket Yazdırılıyor...", Width = 800, Height = 600, StartPosition = FormStartPosition.CenterScreen, TopMost = true, ShowIcon = false };
                Microsoft.Web.WebView2.WinForms.WebView2 web = new Microsoft.Web.WebView2.WinForms.WebView2 { Dock = DockStyle.Fill };
                frmYazdir.Controls.Add(web);

                frmYazdir.FormClosed += (s, e) => { web.Dispose(); };

                frmYazdir.Shown += async (s, e) =>
                {
                    var ozelHafiza = await Microsoft.Web.WebView2.Core.CoreWebView2Environment.CreateAsync(null, System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TamgaApp", "KioskPrint"));
                    await web.EnsureCoreWebView2Async(ozelHafiza);
                    web.NavigationCompleted += (ws, we) =>
                    {
                        web.CoreWebView2.ShowPrintUI(Microsoft.Web.WebView2.Core.CoreWebView2PrintDialogKind.Browser);

                        // 🌟 Etiket basıldığı an "(ETİKETSİZ)" uyarısını kaldırır ve bekliyor moduna alır
                        model.EtiketBasildiMi = true;
                        ListeyiGuncelle();
                    };
                    web.NavigateToString(model.EtiketHtml);
                };

                frmYazdir.ShowDialog(this);
                txtGizliBarkod.Focus(); // Yazdırma bitince barkod okuyucuyu tekrar aktif et
            }

            private void BtnManuel_Click(object sender, EventArgs e)
            {
                var bekleyenler = paletModelleri.Where(p => !okutulanBarkodlar.Contains(p.Barkod)).ToList();

                if (bekleyenler.Count == 0)
                {
                    MessageBox.Show("Yüklenecek (bekleyen) palet kalmadı!", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                Form frmManuel = new Form
                {
                    Text = "✍️ Manuel Palet Yükleme",
                    Size = new Size(650, 450),
                    StartPosition = FormStartPosition.CenterScreen,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    MaximizeBox = false,
                    MinimizeBox = false,
                    BackColor = Color.WhiteSmoke,
                    ShowIcon = false,
                    TopMost = true
                };

                Label lbl = new Label { Text = "Lütfen kamyona manuel olarak eklemek istediğiniz paleti seçin:", Location = new Point(20, 20), AutoSize = true, Font = new Font("Segoe UI", 12, FontStyle.Bold) };
                ListBox lstBekleyen = new ListBox { Location = new Point(20, 60), Size = new Size(590, 250), Font = new Font("Segoe UI", 14) };

                lstBekleyen.DataSource = new BindingSource(bekleyenler, null);
                lstBekleyen.DisplayMember = "PaletAdi";
                lstBekleyen.ValueMember = "Barkod";

                Button btnOnay = new Button { Text = "✅ SEÇİLİ PALETİ YÜKLE", Location = new Point(20, 330), Size = new Size(590, 60), BackColor = Color.Teal, ForeColor = Color.White, Font = new Font("Segoe UI", 14, FontStyle.Bold), Cursor = Cursors.Hand };

                btnOnay.Click += (s, ev) =>
                {
                    if (lstBekleyen.SelectedItem != null)
                    {
                        string seciliBarkod = ((KioskPaletModel)lstBekleyen.SelectedItem).Barkod;
                        frmManuel.Close();
                        DisaridanBarkodGeldi(seciliBarkod);
                    }
                };

                frmManuel.Controls.Add(lbl); frmManuel.Controls.Add(lstBekleyen); frmManuel.Controls.Add(btnOnay);
                frmManuel.ShowDialog(this);
                txtGizliBarkod.Focus();
            }

            protected override void OnShown(EventArgs e) { base.OnShown(e); txtGizliBarkod.Focus(); }
            protected override void OnClick(EventArgs e) { base.OnClick(e); txtGizliBarkod.Focus(); }

            // 🌟 EKRANDAKİ LİSTEYİ OLUŞTURURKEN (ETİKETSİZ) ZIRHINI DEVREYE SOKAR
            private void ListeyiGuncelle()
            {
                lstSagPanel.Items.Clear();
                foreach (var p in paletModelleri)
                {
                    if (okutulanBarkodlar.Contains(p.Barkod))
                        lstSagPanel.Items.Add($"✅ {p.PaletAdi} (YÜKLENDİ)");
                    else if (!p.EtiketBasildiMi)
                        lstSagPanel.Items.Add($"⚠️ {p.PaletAdi} (ETİKETSİZ)");
                    else
                        lstSagPanel.Items.Add($"📦 {p.PaletAdi} (Bekliyor)");
                }
            }

            public void DisaridanBarkodGeldi(string okunanKod)
            {
                if (string.IsNullOrEmpty(okunanKod)) return;

                if (okutulanBarkodlar.Contains(okunanKod))
                {
                    HataVer("❌ DİKKAT!\nBU PALET ZATEN KAMYONA YÜKLENDİ!");
                    return;
                }

                var hedeflenenModel = paletModelleri.FirstOrDefault(p => p.Barkod == okunanKod);
                if (hedeflenenModel != null)
                {
                    okutulanBarkodlar.Add(okunanKod);

                    OnayVer($"✅ {hedeflenenModel.PaletAdi.ToUpper()} ONAYLANDI\nPALET YÜKLENEBİLİR!");
                    ListeyiGuncelle();

                    if (okutulanBarkodlar.Count == paletModelleri.Count)
                    {
                        renkSifirlayici.Stop();
                        pnlOrta.BackColor = Color.FromArgb(46, 204, 113);
                        lblMesaj.Text = "🎉 YÜKLEME TAMAMLANDI!\nTÜM PALETLER ARAÇTA.";
                        try { Console.Beep(1000, 200); Console.Beep(1500, 400); } catch { }

                        MessageBox.Show("Tüm paletler başarıyla araca yüklendi. Araç çıkış yapabilir.", "Sevkiyat Tamamlandı", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        this.DialogResult = DialogResult.OK;
                        this.Close();
                    }
                }
                else
                {
                    HataVer("❌ YANLIŞ PALET!\nBU BARKOD SİPARİŞE AİT DEĞİL!");
                }
            }

            private void Barkod_KeyDown(object sender, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Enter)
                {
                    string okunanKod = txtGizliBarkod.Text.Trim();
                    txtGizliBarkod.Clear();
                    e.SuppressKeyPress = true;
                    DisaridanBarkodGeldi(okunanKod);
                }
            }

            private void OnayVer(string mesaj)
            {
                pnlOrta.BackColor = Color.FromArgb(46, 204, 113);
                lblMesaj.Text = mesaj;
                try { Console.Beep(800, 300); } catch { }
                renkSifirlayici.Stop(); renkSifirlayici.Start();
            }

            private void HataVer(string mesaj)
            {
                pnlOrta.BackColor = Color.FromArgb(231, 76, 60);
                lblMesaj.Text = mesaj;
                try { Console.Beep(300, 1000); } catch { }
                renkSifirlayici.Stop(); renkSifirlayici.Start();
            }

            private void SinyalSifirla()
            {
                renkSifirlayici.Stop();
                pnlOrta.BackColor = Color.FromArgb(33, 37, 41);
                lblMesaj.Text = "SIRADAKİ PALET ETİKETİNİ OKUTUNUZ...";
            }
        }
        #endregion

        // =========================================================================================

        #region 📖 24. DİNAMİK KULLANIM KILAVUZU MOTORU (ANSİKLOPEDİK SÜRÜM)

        public void YardimSekmesiniKur()
        {
            // 1. "❓ Yardım" sekmesi var mı kontrol et, yoksa en sona oluştur
            TabPage yardimSekmesi = tabControl1.TabPages.Cast<TabPage>().FirstOrDefault(t => t.Text == "❓ Yardım");
            if (yardimSekmesi == null)
            {
                yardimSekmesi = new TabPage("❓ Yardım");
                yardimSekmesi.BackColor = Color.WhiteSmoke;
                tabControl1.TabPages.Add(yardimSekmesi);
            }
            else
            {
                yardimSekmesi.Controls.Clear();
            }

            Panel pnlIcerik = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(40),
                BackColor = Color.WhiteSmoke
            };

            RichTextBox rtbIcerik = new RichTextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 12),
                ReadOnly = true,
                BackColor = Color.WhiteSmoke,
                BorderStyle = BorderStyle.None
            };

            List<string> yetkiler = (AktifKullaniciAdi == "TamgaApp" || AktifYetkiler == "Sınırsız")
                                    ? new List<string> { "Sınırsız" }
                                    : AktifYetkiler.Split(',').Select(y => y.Trim()).ToList();

            System.Text.StringBuilder kilavuzMetni = new System.Text.StringBuilder();

            kilavuzMetni.AppendLine($"SİSTEME HOŞ GELDİNİZ SAYIN {AktifKullaniciAdi.ToUpper()}!\n");
            kilavuzMetni.AppendLine("TAMGAAPP OTOMASYON V2.0 - KAPSAMLI SİSTEM MİMARİSİ VE KULLANIM ANSİKLOPEDİSİ");
            kilavuzMetni.AppendLine("Bu doküman, sistemin tüm modüllerinin arka plan çalışma prensiplerini, operasyonel iş akışlarını (Workflow) ve hata giderme (Troubleshooting) prosedürlerini detaylandıran resmi ansiklopedik rehberdir.\n");
            kilavuzMetni.AppendLine(new string('=', 100) + "\n");

            if (yetkiler.Contains("Sınırsız") || yetkiler.Contains("Ana Panel"))
            {
                kilavuzMetni.AppendLine("📌 MODÜL 1: ANA PANEL (KONTROL MERKEZİ VE SİSTEM GÜVENLİĞİ)\n");
                kilavuzMetni.AppendLine("SİSTEM MİMARİSİ VE AMACI:");
                kilavuzMetni.AppendLine("Ana Panel, uygulamanın yaşam döngüsünü (Lifecycle) yöneten kök dizindir. Asenkron saat motoru, oturum yönetimi ve RAM/Port temizlik (Garbage Collection) süreçleri buradan komuta edilir.\n");
                kilavuzMetni.AppendLine("OPERASYONEL İŞ AKIŞI VE TEKNİK DETAYLAR:");
                kilavuzMetni.AppendLine("➤ Güvenli Çıkış (Kırmızı Buton): Sistemden ayrılırken pencereyi (X) ikonundan kapatmak yerine bu modül kullanılmalıdır. Bu işlem; açık olan donanımsal COM (Barkod) portlarını güvenlice serbest bırakır, veritabanı yığınlarını (Cache) temizler ve donmayı/kilitlenmeyi engelleyen Asenkron Kapanış (Fade-out) motorunu tetikler.");
                kilavuzMetni.AppendLine("➤ Oturumu Kapat: Vardiya değişimlerinde, programın çekirdek dosyalarını kapatmadan yalnızca Aktif Kullanıcı (Session) kimliğini sıfırlayarak giriş ekranına döner.\n");
                kilavuzMetni.AppendLine(new string('-', 100) + "\n");
            }

            if (yetkiler.Contains("Sınırsız") || yetkiler.Contains("Depo Kabul"))
            {
                kilavuzMetni.AppendLine("📌 MODÜL 2: DEPO KABUL VE ÜRETİM TAKİP (GİRİŞ KALİTE KONTROL)\n");
                kilavuzMetni.AppendLine("SİSTEM MİMARİSİ VE AMACI:");
                kilavuzMetni.AppendLine("Fabrika üretim hattından çıkan mamullerin veya dış tedarikçiden gelen malzemelerin, SQL veritabanındaki Master Data (Ana Veri) ile eşleştirilerek envantere dahil edilmesini sağlar.\n");
                kilavuzMetni.AppendLine("OPERASYONEL İŞ AKIŞI VE TEKNİK DETAYLAR:");
                kilavuzMetni.AppendLine("➤ Akıllı Barkod Eşleştirme (Smart Matching): Okutulan her barkod, saliseler içinde SQL veritabanında taranır. Eşleşme bulunursa Ürün Kodu, Adı ve Lavabo Rengi otomatik olarak tabloya yansıtılır. Eşleşme bulunamazsa sistem 'KAYITSIZ' statüsünde dummy (sanal) bir kayıt oluşturur.");
                kilavuzMetni.AppendLine("➤ Otomatik Yığma (Aggregation): Aynı barkod peş peşe okutulduğunda, sistem yorgunluğunu önlemek adına yeni satır açılmaz; mevcut satırın 'Adet' hücresindeki sayı matematiksel olarak (+1) güncellenir.");
                kilavuzMetni.AppendLine("➤ Hata İzolasyonu (Geri Alma): Yanlış veya fazla okutulan kalemler, satır seçilip 'Seçilen Kalemleri Sil' komutuyla listeden çıkartılır.");
                kilavuzMetni.AppendLine("➤ CSV Dışa Aktarım ve Yazdırma: 'Kaydet ve Yazdır' komutu, oluşturulan listeyi öncelikle Masaüstündeki 'Günlük Üretim Takip' klasörüne o günün tarihiyle .csv formatında (Excel uyumlu) mühürler. Ardından GDI+ çizim motorunu kullanarak endüstriyel standartlarda A4 Kabul Fişi önizlemesini ekrana yansıtır.\n");
                kilavuzMetni.AppendLine(new string('-', 100) + "\n");
            }

            if (yetkiler.Contains("Sınırsız") || yetkiler.Contains("Sevkiyat Plan"))
            {
                kilavuzMetni.AppendLine("📌 MODÜL 3: SEVKİYAT PLAN (WMS ÇEKİRDEĞİ VE DESİ/PALETLEME)\n");
                kilavuzMetni.AppendLine("SİSTEM MİMARİSİ VE AMACI:");
                kilavuzMetni.AppendLine("Canias/ERP veritabanından çekilen 'Açık Siparişlerin (Backorders)' lojistik kurallarına göre toplanması, paletlere bölünmesi ve hatasız çıkış (Poka-Yoke) yapılmasını sağlayan kompleks yönlendirme motorudur.\n");
                kilavuzMetni.AppendLine("OPERASYONEL İŞ AKIŞI VE TEKNİK DETAYLAR:");
                kilavuzMetni.AppendLine("➤ OLEDB SQL Entegrasyonu: 'Yenile' komutu; SE (Yurtiçi) ve O1 (İhracat) belge tiplerindeki, silinmemiş ve sevk edilmemiş tüm açık siparişleri devasa bir JOIN sorgusuyla ERP'den çeker.");
                kilavuzMetni.AppendLine("➤ FİFO (İlk Giren İlk Çıkar) Konsolidasyonu: 'Tüm Belge No Seç' komutu, bir müşteriye ait birden fazla sipariş belgesini tarih sırasına göre birleştirerek tek bir yükleme ekranında (Konsolide) sunar.");
                kilavuzMetni.AppendLine("➤ Palet Matrisi ve Desi Algoritması: Açılan palet kolonlarına okutulan her ürün yerleştirilir. 'Palet ve Desiyi Düzenle' menüsü ile En x Boy x Yükseklik / 3000 formülü arka planda çalıştırılarak Lojistik Desi hesaplaması otomatik yapılır.");
                kilavuzMetni.AppendLine("➤ Manyetik İmleç Zırhı (Magnetic Focus): Operatör ekranda başka bir yere tıklasa dahi, barkod okuyucunun gönderdiği ilk veri byte'ı algılandığı an, sistem imleci ışık hızında barkod giriş kutusuna kilitler (Sıfır Veri Kaybı).");
                kilavuzMetni.AppendLine("➤ Kapanış (Sevk) Stratejileri:");
                kilavuzMetni.AppendLine("   - TAM SEVK (Green State): Tüm satırlar hedeflenen adede (Yeşil) ulaştığında çalışır. Belgeyi Ghost (Hayalet) Modu kara listesine alır, arşivler ve ERP'de açık görünse dahi bir daha ekrana yansıtmaz.");
                kilavuzMetni.AppendLine("   - KISMİ SEVK (Orange State): Eksik okutulan (Sarı) satırları analiz eder. Mevcut okutulanı arşive atıp, kalan bakiyeyi sistemde açık (bekleyen) olarak bırakır.");
                kilavuzMetni.AppendLine("   - ASKIYA AL (Snapshot): Yüklemenin yarım kalması durumunda, ekranın birebir kopyasını (Snapshot) JSON formatında diske yazar. 'Askıdakileri Getir' ile milisaniyeler içinde tüm tablo geri yüklenir.\n");
                kilavuzMetni.AppendLine(new string('-', 100) + "\n");
            }

            if (yetkiler.Contains("Sınırsız") || yetkiler.Contains("Sevkiyat"))
            {
                kilavuzMetni.AppendLine("📌 MODÜL 4: SEVKİYAT ARŞİVİ, AMBAR KONSOLİDASYONU VE KIOSK\n");
                kilavuzMetni.AppendLine("SİSTEM MİMARİSİ VE AMACI:");
                kilavuzMetni.AppendLine("Kapanmış belgelerin Hiyerarşik Ağaç (Tree) mimarisiyle saklandığı, çapraz sorguların yapıldığı ve forklift/yükleme personeli için Kiosk (Tam Ekran) barkod doğrulamasının yapıldığı istasyondur.\n");
                kilavuzMetni.AppendLine("OPERASYONEL İŞ AKIŞI VE TEKNİK DETAYLAR:");
                kilavuzMetni.AppendLine("➤ Çok Yönlü Arama (Cross-Search): Sağ üstteki arama çubuğu, binlerce CSV dosyası içindeki her bir satırı tarayarak, spesifik bir barkodun veya ürünün hangi tarihte, hangi müşteriye ve hangi palet içinde gönderildiğini anında ekrana basar.");
                kilavuzMetni.AppendLine("➤ Ambar (Parsiyel Yükleme) Modülü: Farklı müşterilere ait küçük hacimli siparişlerin (Örn: 1 palet A müşterisi, 2 palet B müşterisi) 'Ambar Aracı' isimli sanal bir havuzda (JSON) toplanmasını sağlar. 'Ambarı Tamamla' komutuyla bu havuz tek bir araca yüklenmiş gibi konsolide (Toplu) Excel Raporu üretir.");
                kilavuzMetni.AppendLine("➤ Kamyon Yükleme Kiosk (Terminal Modu): Araç yükleme rampasında çalışır. Araca bindirilen her paletin EAN-13 etiketi okutulduğunda sistem yeşil onay verir. Yanlış palet yüklemesinde kırmızı hata ekranı ve sesli alarm devreye girerek Poka-Yoke (Hata Önleme) kuralını uygular. Etiketsiz paletler için sağ tık menüsünden anında 'Edge WebView2' motoruyla etiket basılabilir.\n");
                kilavuzMetni.AppendLine(new string('-', 100) + "\n");
            }

            if (yetkiler.Contains("Sınırsız") || yetkiler.Contains("Depo Sayım"))
            {
                kilavuzMetni.AppendLine("📌 MODÜL 5: FİZİKSEL ENVANTER VE DEPO SAYIM\n");
                kilavuzMetni.AppendLine("SİSTEM MİMARİSİ VE AMACI:");
                kilavuzMetni.AppendLine("Periyodik veya anlık stok sayımlarının (Cycle Count) yapılarak ERP sistemi ile fiziki ambarın karşılaştırılmasına olanak tanıyan hızlı kayıt modülüdür.\n");
                kilavuzMetni.AppendLine("OPERASYONEL İŞ AKIŞI VE TEKNİK DETAYLAR:");
                kilavuzMetni.AppendLine("➤ Çift Yönlü Algılama: Kullanıcı barkod okuttuğunda sistem hem 'Barkod (EAN)' alanına hem de 'Malzeme Kodu' alanına bakar. Kayıtsız ürünlerde dahi sayım durdurulmaz, manuel müdahale için listeye eklenir.");
                kilavuzMetni.AppendLine("➤ Arşiv Mimarisi: Sayımlar, kullanıcının belirlediği isimle (Örn: A_Koridoru_Hirdavat) Yıl ve Ay klasörlerine ayrıştırılarak CSV formatında kaydedilir. Ağaç (Tree) menüsünden eski sayımlara ulaşılıp canlı filtreleme (Live Filter) yapılabilir.\n");
                kilavuzMetni.AppendLine(new string('-', 100) + "\n");
            }

            if (yetkiler.Contains("Sınırsız") || yetkiler.Contains("Normal Zarf Yazdırma") || yetkiler.Contains("Çoklu Zarf Yazdırma"))
            {
                kilavuzMetni.AppendLine("📌 MODÜL 6: DİNAMİK TASARIM, ŞABLON VE EDGE (WEBVIEW2) YAZDIRMA\n");
                kilavuzMetni.AppendLine("SİSTEM MİMARİSİ VE AMACI:");
                kilavuzMetni.AppendLine("Firmanın kargo etiketi, mektup zarfı veya palet fişlerini Sürükle-Bırak (Drag & Drop) mantığıyla tasarladığı ve Chromium altyapısıyla sıfır çözünürlük kaybıyla kağıda döktüğü motordur.\n");
                kilavuzMetni.AppendLine("OPERASYONEL İŞ AKIŞI VE TEKNİK DETAYLAR:");
                kilavuzMetni.AppendLine("➤ Akıllı Koordinat Sistemi: Ekrana eklenen nesnelerin X,Y konumları Px (Piksel) yerine donanımdan bağımsız Mm (Milimetre) cinsinden hesaplanarak JSON olarak kaydedilir. Bu sayede tasarım her marka yazıcıda milimi milimine aynı yerden çıkar.");
                kilavuzMetni.AppendLine("➤ Değişken (Placeholder) Mimarisi: Metinlerin içine gömülen {FirmaAdi}, {Il} gibi değişkenler, yazdırma anında (Runtime) SQL'den veya UI'dan gelen gerçek firma bilgileriyle Replace edilerek basılır.");
                kilavuzMetni.AppendLine("➤ Edge Render Motoru (WebView2): Tasarımlar, arka planda dinamik olarak CSS3 ve HTML5 kodlarına dönüştürülür. WebView2 Runtime motoru bu kodları derleyerek, klasik WinForms yazdırmasındaki pikselleşmeyi (Bulanıklığı) %100 yok eder ve Vektörel (Cam gibi) baskı alınmasını sağlar.\n");
                kilavuzMetni.AppendLine(new string('-', 100) + "\n");
            }

            if (yetkiler.Contains("Sınırsız") || yetkiler.Contains("Stok"))
            {
                kilavuzMetni.AppendLine("📌 MODÜL 8: CANLI STOK PİVOTLARI VE SQL ENTEGRASYONU\n");
                kilavuzMetni.AppendLine("SİSTEM MİMARİSİ VE AMACI:");
                kilavuzMetni.AppendLine("Kullanıcıların kendi yazdıkları özel SQL sorgularını (Query) sisteme entegre edip, dinamik rapor sekmeleri (Pivot Tablolar) yaratmasını sağlayan Business Intelligence (İş Zekası) aracıdır.\n");
                kilavuzMetni.AppendLine("OPERASYONEL İŞ AKIŞI VE TEKNİK DETAYLAR:");
                kilavuzMetni.AppendLine("➤ Dinamik Sekme Yaratımı (Runtime Injection): Girilen SQL sorgusu çalıştırılır ve dönen DataTable sonucu, programı yeniden başlatmaya gerek kalmadan anında yeni bir sekme (TabPage) olarak üst menüye enjekte edilir.");
                kilavuzMetni.AppendLine("➤ Asenkron Veri Çekimi (Task.Run): Veritabanı sorguları arka plan iş parçacıklarında (Background Thread) çalıştırılır, böylece devasa veriler çekilirken programın arayüzü asla donmaz veya kilitlenmez (Not Responding hatası engellenir).");
                kilavuzMetni.AppendLine("➤ Kalıcı Hafıza (JSON Serialization): Eklenen her SQL rapor ayarı AppData klasörüne şifrelenerek kaydedilir. Uygulama açıldığında raporlar otomatik olarak son güncel halleriyle tabloya dökülür.\n");
                kilavuzMetni.AppendLine(new string('-', 100) + "\n");
            }

            if (yetkiler.Contains("Sınırsız") || yetkiler.Contains("Yönetim"))
            {
                kilavuzMetni.AppendLine("👑 MODÜL 7: YÖNETİCİ KONTROLLERİ VE GÜVENLİK KATMANI\n");
                kilavuzMetni.AppendLine("SİSTEM MİMARİSİ VE AMACI:");
                kilavuzMetni.AppendLine("Sistemin kriptografik güvenlik politikalarının (Hashing), yetki bazlı menü gizlemelerinin (Isolation) ve yıkıcı/toplu veri silme işlemlerinin (CRUD Destructive) yönetildiği 'Super Admin' terminalidir.\n");
                kilavuzMetni.AppendLine("OPERASYONEL İŞ AKIŞI VE TEKNİK DETAYLAR:");
                kilavuzMetni.AppendLine("➤ Kriptolojik Veri Güvenliği: Yönetim panelinden kaydedilen SQL bağlantı dizesi şifreleri (Connection String Passwords) ve Kullanıcı giriş şifreleri, AES/SHA algoritması benzeri özel Hashing kütüphanesiyle şifrelenir (Encrypted). Diske açık metin (Cleartext) olarak hiçbir şifre yazılmaz.");
                kilavuzMetni.AppendLine("➤ Yetki İzolasyonu (Role-Based Access Control): Bir personele sadece 'Depo Kabul' yetkisi verilirse, Sistem Açılış (Init) motoru diğer tüm sekmeleri (Sevkiyat, Ayarlar vs.) RAM'den fiziksel olarak siler. Kullanıcı ekranı büyüterek veya kısayol deneyerek bu sekmelere ulaşamaz.");
                kilavuzMetni.AppendLine("➤ Yıkıcı Komut Zırhı: 'Operasyonel Fabrika Ayarlarına Dön' veya 'Tüm Firmaları Sil' gibi kritik komutlar, kazara tıklanmaları önlemek amacıyla Çift Katmanlı Onay diyaloğuna (Double-Check Confirmation) ve Focus kaybı zırhına tabi tutulmuştur.\n");
            }

            kilavuzMetni.AppendLine(new string('=', 100));
            kilavuzMetni.AppendLine("\nSistem Mimarisi, Geliştirme ve Optimizasyon: TamgaApp Operasyon Otomasyonu V2.0");
            kilavuzMetni.AppendLine("TamgaApp altyapısı, C# / .NET / OLEDB / WebView2 Teknolojileri kullanılarak yüksek performans ve lojistik standartlarına göre inşa edilmiştir.");

            rtbIcerik.Text = kilavuzMetni.ToString();

            // 5. Renklendirme Motorunu Çalıştır (Kurumsal Ansiklopedi Formatı)
            Renklendir(rtbIcerik, "TAMGAAPP OTOMASYON V2.0 - KAPSAMLI SİSTEM MİMARİSİ VE KULLANIM ANSİKLOPEDİSİ", Color.DarkBlue);
            Renklendir(rtbIcerik, $"SİSTEME HOŞ GELDİNİZ SAYIN {AktifKullaniciAdi.ToUpper()}!", Color.DarkRed);

            // Başlıkları renklendir
            string[] basliklar = {
                "📌 MODÜL 1: ANA PANEL (KONTROL MERKEZİ VE SİSTEM GÜVENLİĞİ)",
                "📌 MODÜL 2: DEPO KABUL VE ÜRETİM TAKİP (GİRİŞ KALİTE KONTROL)",
                "📌 MODÜL 3: SEVKİYAT PLAN (WMS ÇEKİRDEĞİ VE DESİ/PALETLEME)",
                "📌 MODÜL 4: SEVKİYAT ARŞİVİ, AMBAR KONSOLİDASYONU VE KIOSK",
                "📌 MODÜL 5: FİZİKSEL ENVANTER VE DEPO SAYIM",
                "📌 MODÜL 6: DİNAMİK TASARIM, ŞABLON VE EDGE (WEBVIEW2) YAZDIRMA",
                "📌 MODÜL 8: CANLI STOK PİVOTLARI VE SQL ENTEGRASYONU",
                "👑 MODÜL 7: YÖNETİCİ KONTROLLERİ VE GÜVENLİK KATMANI"
            };

            foreach (var baslik in basliklar)
            {
                Renklendir(rtbIcerik, baslik, Color.DarkRed);
            }

            // Alt Başlıkları ve Önemli Uyarıları Renklendir
            string[] altBasliklar = { "SİSTEM MİMARİSİ VE AMACI:", "OPERASYONEL İŞ AKIŞI VE TEKNİK DETAYLAR:" };
            foreach (var alt in altBasliklar) Renklendir(rtbIcerik, alt, Color.Blue);

            // Maddeleri Kalın Yap (Siyah kalsın, sadece kalınlaşsın)
            string[] maddeler = {
                "➤ Güvenli Çıkış (Kırmızı Buton):", "➤ Oturumu Kapat:",
                "➤ Akıllı Barkod Eşleştirme (Smart Matching):", "➤ Otomatik Yığma (Aggregation):", "➤ Hata İzolasyonu (Geri Alma):", "➤ CSV Dışa Aktarım ve Yazdırma:",
                "➤ OLEDB SQL Entegrasyonu:", "➤ FİFO (İlk Giren İlk Çıkar) Konsolidasyonu:", "➤ Palet Matrisi ve Desi Algoritması:", "➤ Manyetik İmleç Zırhı (Magnetic Focus):", "➤ Kapanış (Sevk) Stratejileri:",
                "➤ Çok Yönlü Arama (Cross-Search):", "➤ Ambar (Parsiyel Yükleme) Modülü:", "➤ Kamyon Yükleme Kiosk (Terminal Modu):",
                "➤ Çift Yönlü Algılama:", "➤ Arşiv Mimarisi:",
                "➤ Akıllı Koordinat Sistemi:", "➤ Değişken (Placeholder) Mimarisi:", "➤ Edge Render Motoru (WebView2):",
                "➤ Dinamik Sekme Yaratımı (Runtime Injection):", "➤ Asenkron Veri Çekimi (Task.Run):", "➤ Kalıcı Hafıza (JSON Serialization):",
                "➤ Kriptolojik Veri Güvenliği:", "➤ Yetki İzolasyonu (Role-Based Access Control):", "➤ Yıkıcı Komut Zırhı:"
            };

            foreach (var madde in maddeler) Renklendir(rtbIcerik, madde, Color.Black);

            // Özel Terim Vurguları
            Renklendir(rtbIcerik, "- TAM SEVK (Green State):", Color.DarkGreen);
            Renklendir(rtbIcerik, "- KISMİ SEVK (Orange State):", Color.DarkOrange);
            Renklendir(rtbIcerik, "- ASKIYA AL (Snapshot):", Color.DarkBlue);
            Renklendir(rtbIcerik, "Ghost (Hayalet) Modu", Color.Purple);
            Renklendir(rtbIcerik, "Poka-Yoke (Hata Önleme)", Color.DarkMagenta);

            // Kutuyu panele, paneli de sekmeye ekle
            pnlIcerik.Controls.Add(rtbIcerik);
            yardimSekmesi.Controls.Add(pnlIcerik);
        }

        // Metin içindeki başlıkları otomatik bulup kalın ve renkli yapan küçük yardımcı metot
        private void Renklendir(RichTextBox rtb, string kelime, Color renk)
        {
            int baslangic = 0;
            while (baslangic < rtb.TextLength)
            {
                int pos = rtb.Text.IndexOf(kelime, baslangic, StringComparison.OrdinalIgnoreCase);
                if (pos >= 0)
                {
                    rtb.Select(pos, kelime.Length);
                    rtb.SelectionColor = renk;
                    rtb.SelectionFont = new Font(rtb.Font, FontStyle.Bold);
                    baslangic = pos + kelime.Length;
                }
                else
                {
                    break;
                }
            }
            rtb.Select(0, 0); // Seçimi bırak
        }
        #endregion

        // =========================================================================================

        #region 📦 25. AMBAR YÖNETİMİ (PARSİYEL YÜKLEME) SİSTEMİ

        // 🌟 1. HAFIZA BEYNİ VE KALICI DOSYA YOLU (Format atılana kadar silinmez)
        Dictionary<string, string> AmbarHafizasi = new Dictionary<string, string>();
        string AmbarDosyaYolu = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TamgaApp", "AmbarAraci.json");

        // 🌟 1.1 AMBARI DİSKTEN OKUMA MOTORU (Uygulama kapansa bile eskiyi getirir)
        private void AmbarHafizasiniYukle()
        {
            if (System.IO.File.Exists(AmbarDosyaYolu))
            {
                try
                {
                    string json = System.IO.File.ReadAllText(AmbarDosyaYolu);
                    AmbarHafizasi = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
                }
                catch { AmbarHafizasi = new Dictionary<string, string>(); }
            }
        }

        // 🌟 1.2 AMBARI DİSKE MÜHÜRLEME MOTORU (Elektrik gitse bile silinmez)
        private void AmbarHafizasiniKaydet()
        {
            try
            {
                string klasor = System.IO.Path.GetDirectoryName(AmbarDosyaYolu);
                if (!System.IO.Directory.Exists(klasor)) System.IO.Directory.CreateDirectory(klasor);

                string json = Newtonsoft.Json.JsonConvert.SerializeObject(AmbarHafizasi, Newtonsoft.Json.Formatting.Indented);
                System.IO.File.WriteAllText(AmbarDosyaYolu, json);
            }
            catch { }
        }

        // 🌟 2. AMBARA KAYDET BUTONU: Okutulan malları kamyona kalıcı atar ve ekranı temizler
        private void btnAmbarKaydet_Click(object sender, EventArgs e)
        {
            AmbarHafizasiniYukle(); // Önce diskteki eski kamyonu getir ki üstüne binmesin

            string orjinalFirma = cmbMusteri.Text.Trim();
            if (string.IsNullOrEmpty(orjinalFirma))
            {
                MessageBox.Show("Lütfen önce bir firma seçin!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (clbBelgeNo.CheckedItems.Count == 0 || dgvMalzemeler.Rows.Count == 0)
            {
                MessageBox.Show("Ambara eklenecek açık bir sevkiyat yok!", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 🌟 SİHİRLİ İSİM ZIRHI: Aynı firmadan varsa ismin sonuna - 1, - 2 ekleyerek benzersiz yapar!
            string seciliFirma = orjinalFirma;
            int sayac = 1;
            while (AmbarHafizasi.ContainsKey(seciliFirma))
            {
                seciliFirma = $"{orjinalFirma} - {sayac}";
                sayac++;
            }

            // Ekrandaki tüm veriyi "YarimSevkiyatHafizasi" kalıbında topla
            YarimSevkiyatHafizasi hafiza = new YarimSevkiyatHafizasi
            {
                MusteriAdi = seciliFirma, // 🌟 Artık benzersiz ismiyle hafızaya yazılıyor
                BelgeNo = string.Join(", ", clbBelgeNo.CheckedItems.Cast<string>()),
                SevkMusteri = txtSevkMusteri.Text,
                PaletSayisi = cmbSevkPaletSayisi.SelectedIndex != -1 ? Convert.ToInt32(cmbSevkPaletSayisi.SelectedItem) : 0,
                KayitTarihi = DateTime.Now
            };

            // 🌟 SOL TABLOYU TOPLA (RENK ZIRHLI VE MÜKERRER KORUMALI)
            foreach (DataGridViewRow row in dgvMalzemeler.Rows)
            {
                if (row.IsNewRow || row.Cells["Malzeme Kodu"].Value == null) continue;

                string belgeNo = row.Cells["Belge No"].Value?.ToString() ?? "";
                string malzemeKodu = row.Cells["Malzeme Kodu"].Value.ToString();
                string aciklama = row.Cells["Açıklama"].Value?.ToString() ?? ""; // 🌟 ZIRH EKLENDİ

                // 🌟 KUSURSUZ ANAHTAR ZIRHI: Artık sadece koda değil, renge/açıklamaya da bakıyor!
                string benzersizAnahtar = $"{belgeNo}_{malzemeKodu}_{aciklama}";

                int okutulan = 0;
                if (row.Cells["Okutulan"].Value != null) int.TryParse(row.Cells["Okutulan"].Value.ToString(), out okutulan);

                // 🌟 TOPLAMA ZIRHI: Aynı üründen 2 satır varsa sayıları birbirinin üstüne ezmez, toplayarak havuz yapar!
                if (!hafiza.AnaOkutulanlar.ContainsKey(benzersizAnahtar))
                    hafiza.AnaOkutulanlar.Add(benzersizAnahtar, okutulan);
                else
                    hafiza.AnaOkutulanlar[benzersizAnahtar] += okutulan;
            }

            // Sağ tabloyu topla
            for (int i = 0; i < dgvPaletMatrisi.Rows.Count; i++)
            {
                hafiza.PaletMatrisiDurumu[i] = new Dictionary<int, string>();
                for (int j = 0; j < dgvPaletMatrisi.Columns.Count; j++)
                {
                    if (dgvPaletMatrisi.Rows[i].Cells[j].Value != null)
                        hafiza.PaletMatrisiDurumu[i][j] = dgvPaletMatrisi.Rows[i].Cells[j].Value.ToString();
                }
            }

            hafiza.PaletBarkodlari = aktifPaletBarkodlari;

            // JSON'a Çevir
            string jsonVeri = Newtonsoft.Json.JsonConvert.SerializeObject(hafiza, Newtonsoft.Json.Formatting.Indented);

            // 🌟 ARTIK ÜZERİNE YAZMIYOR, GÜVENLE YENİ KAYIT OLARAK EKLİYOR
            AmbarHafizasi.Add(seciliFirma, jsonVeri);

            AmbarHafizasiniKaydet(); // 🌟 YAPILAN İŞLEMİ DİSKE MÜHÜRLE!

            MessageBox.Show($"'{seciliFirma}' ürünleri Ambar Aracına KALICI olarak yüklendi!", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);

            btnSevkTemizle_Click(null, null); // Ekranı temizle
        }

        // 🌟 3. AMBAR GETİR BUTONU: Ambardaki malı geri ekrana (palet matrisine) dizer
        private void btnAmbarGetir_Click(object sender, EventArgs e)
        {
            AmbarHafizasiniYukle(); // Diskten oku

            if (AmbarHafizasi.Count == 0)
            {
                MessageBox.Show("Ambar aracı şu an boş! Getirilecek herhangi bir firma bulunmuyor.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Form frmSecim = new Form
            {
                Text = "🔄 Ambardan Geri Getir",
                Size = new Size(400, 350),
                StartPosition = FormStartPosition.CenterScreen,
                BackColor = Color.WhiteSmoke,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                Icon = this.Icon
            };

            Label lblBilgi = new Label { Text = "Ekrana geri getirmek istediğiniz firmayı seçin:", Location = new Point(20, 15), Size = new Size(340, 20), Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            ListBox lstAmbardakiFirmalar = new ListBox { Location = new Point(20, 40), Size = new Size(340, 200), Font = new Font("Segoe UI", 12) };

            foreach (var firma in AmbarHafizasi.Keys) lstAmbardakiFirmalar.Items.Add(firma);

            Button btnGetir = new Button { Text = "⬇️ Seçili Firmayı Ekrana Getir", Location = new Point(20, 250), Size = new Size(340, 45), BackColor = Color.Teal, ForeColor = Color.White, Font = new Font("Segoe UI", 10, FontStyle.Bold), Cursor = Cursors.Hand };

            btnGetir.Click += (s, ev) =>
            {
                if (lstAmbardakiFirmalar.SelectedItem == null)
                {
                    MessageBox.Show("Lütfen listeden bir firma seçin!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string seciliFirma = lstAmbardakiFirmalar.SelectedItem.ToString();
                string geriGelenJson = AmbarHafizasi[seciliFirma];

                try
                {
                    YarimSevkiyatHafizasi hafiza = Newtonsoft.Json.JsonConvert.DeserializeObject<YarimSevkiyatHafizasi>(geriGelenJson);
                    if (hafiza == null) return;

                    int musteriIndex = -1;
                    for (int i = 0; i < cmbMusteri.Items.Count; i++)
                    {
                        if (cmbMusteri.Items[i].ToString().Trim().Equals(hafiza.MusteriAdi.Trim(), StringComparison.OrdinalIgnoreCase))
                        {
                            musteriIndex = i; break;
                        }
                    }

                    if (musteriIndex >= 0) cmbMusteri.SelectedIndex = musteriIndex;
                    else
                    {
                        cmbMusteri.Items.Add(hafiza.MusteriAdi);
                        cmbMusteri.SelectedIndex = cmbMusteri.Items.Count - 1;
                    }

                    txtMusteriAdi.Text = hafiza.MusteriAdi;
                    txtSevkMusteri.Text = hafiza.SevkMusteri;
                    cmbSevkPaletSayisi.SelectedItem = hafiza.PaletSayisi.ToString();

                    string[] kaydedilenBelgeler = hafiza.BelgeNo.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (clbBelgeNo.Items.Count == 0) foreach (string b in kaydedilenBelgeler) clbBelgeNo.Items.Add(b);

                    foreach (string belge in kaydedilenBelgeler)
                    {
                        for (int i = 0; i < clbBelgeNo.Items.Count; i++)
                        {
                            if (clbBelgeNo.Items[i].ToString().IndexOf(belge, StringComparison.OrdinalIgnoreCase) >= 0) clbBelgeNo.SetItemChecked(i, true);
                        }
                    }

                    // Tablonun ana iskeletini kursun diye arama butonunu sanal olarak tetikliyoruz
                    btnSevkAra_Click(null, null);

                    // 🌟 MÜKEMMEL DAĞITIM ZIRHI: Eski verileri satır satır nokta atışıyla bul ve yapıştır
                    foreach (DataGridViewRow row in dgvMalzemeler.Rows)
                    {
                        if (row.IsNewRow || row.Cells["Malzeme Kodu"].Value == null) continue;

                        string belgeNo = row.Cells["Belge No"].Value?.ToString() ?? "";
                        string malzemeKodu = row.Cells["Malzeme Kodu"].Value.ToString();
                        string aciklama = row.Cells["Açıklama"].Value?.ToString() ?? ""; // 🌟 RENK ZIRHI

                        // 🌟 ANAHTAR DÜZELTİLDİ
                        string benzersizAnahtar = $"{belgeNo}_{malzemeKodu}_{aciklama}";

                        if (hafiza.AnaOkutulanlar.ContainsKey(benzersizAnahtar) && hafiza.AnaOkutulanlar[benzersizAnahtar] > 0)
                        {
                            int siparisAdedi = Convert.ToInt32(row.Cells["Sipariş Adedi"].Value);
                            int havuzdaki = hafiza.AnaOkutulanlar[benzersizAnahtar];

                            // 🌟 ŞELALE MANTIĞI: Taşırmayı önle, sadece sipariş adedi kadarını yaz
                            int yazilacak = Math.Min(siparisAdedi, havuzdaki);

                            row.Cells["Okutulan"].Value = yazilacak;
                            hafiza.AnaOkutulanlar[benzersizAnahtar] -= yazilacak; // Dağıtılanı havuzdan düş
                        }
                        else
                        {
                            row.Cells["Okutulan"].Value = 0;
                        }
                    }

                    DgvMalzemeler_Renklendir(null, null); // 🌟 Boya motorunu zorla tetikle ki renkler canlansın!

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

                    aktifPaletBarkodlari = hafiza.PaletBarkodlari ?? new Dictionary<string, string>();

                    frmSecim.Close();
                    MessageBox.Show($"{seciliFirma} firması ambardan masaya indirildi. İşleme devam edebilirsiniz.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ambar verisi çözülürken hata oluştu: " + ex.Message, "Kritik Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            frmSecim.Controls.Add(lblBilgi);
            frmSecim.Controls.Add(lstAmbardakiFirmalar);
            frmSecim.Controls.Add(btnGetir);
            frmSecim.ShowDialog();
        }

        // 🌟 4. AMBAR GÖRÜNTÜLE BUTONU: Kamyonun içini gösteren yönetim paneli
        private void btnAmbarGoruntule_Click(object sender, EventArgs e)
        {
            AmbarHafizasiniYukle(); // Diskten güncel durumu çek

            Form frmAmbar = new Form
            {
                Text = "🚛 Ambar Aracı Yönetim Paneli",
                Size = new Size(700, 500),
                StartPosition = FormStartPosition.CenterScreen,
                BackColor = Color.WhiteSmoke,
                Icon = this.Icon
            };

            ListBox lstAmbardakiFirmalar = new ListBox { Location = new Point(20, 20), Size = new Size(300, 400), Font = new Font("Segoe UI", 12) };

            foreach (var firma in AmbarHafizasi.Keys) lstAmbardakiFirmalar.Items.Add(firma);

            Button btnSil = new Button { Text = "❌ Seçileni Ambardan Sil", Location = new Point(350, 30), Size = new Size(300, 50), BackColor = Color.DarkRed, ForeColor = Color.White, Font = new Font("Segoe UI", 10, FontStyle.Bold), Cursor = Cursors.Hand };
            Button btnYuklemeBaslat = new Button { Text = "🚀 AMBAR YÜKLEMEYİ BAŞLAT", Location = new Point(350, 100), Size = new Size(300, 70), BackColor = Color.MediumSeaGreen, ForeColor = Color.White, Font = new Font("Segoe UI", 12, FontStyle.Bold), Cursor = Cursors.Hand };

            // 🌟 BAŞLANGIÇTA GRİ VE KİLİTLİ GÖRÜNEN TAMAMLA BUTONU
            Button btnTamamla = new Button { Text = "✅ Ambarı Tamamla (Toplu Sevk)", Location = new Point(350, 190), Size = new Size(300, 70), BackColor = Color.Gray, ForeColor = Color.White, Font = new Font("Segoe UI", 11, FontStyle.Bold), Cursor = Cursors.No };

            // 🌟 GÜVENLİK ZIRHI: Tümü okutulana kadar false kalacak
            bool tumPaletlerOkundu = false;

            // ❌ SİL BUTONU İŞLEMİ
            btnSil.Click += (s, ev) =>
            {
                if (lstAmbardakiFirmalar.SelectedItem != null)
                {
                    string silinecekFirma = lstAmbardakiFirmalar.SelectedItem.ToString();
                    AmbarHafizasi.Remove(silinecekFirma);
                    AmbarHafizasiniKaydet();

                    lstAmbardakiFirmalar.Items.Remove(silinecekFirma);
                    MessageBox.Show($"{silinecekFirma} firmasının malları ambardan tamamen indirildi!", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Lütfen silinecek firmayı listeden seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };

            // 🚀 AMBAR YÜKLEMEYİ BAŞLAT BUTONU (KİOSK MOTORU)
            btnYuklemeBaslat.Click += (s, ev) =>
            {
                if (AmbarHafizasi.Count == 0)
                {
                    MessageBox.Show("Ambar aracı şu an boş! Yüklenecek palet bulunamadı.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // 🌟 YENİ: Kiosk'a gidecek detaylı palet modelleri listesi
                List<FrmKamyonKiosk.KioskPaletModel> kioskPaletler = new List<FrmKamyonKiosk.KioskPaletModel>();
                bool degisiklikYapildi = false;
                var yerelUrunler = DataAccess.GetAllUrunler(); // Ürün isimleri için veritabanını çek

                foreach (var kvp in AmbarHafizasi.ToList())
                {
                    try
                    {
                        var hafiza = Newtonsoft.Json.JsonConvert.DeserializeObject<YarimSevkiyatHafizasi>(kvp.Value);
                        if (hafiza == null) continue;

                        if (hafiza.PaletBarkodlari == null) hafiza.PaletBarkodlari = new Dictionary<string, string>();

                        int sutunSayisi = hafiza.PaletMatrisiDurumu.Values.FirstOrDefault()?.Count ?? 0;

                        for (int j = 0; j < sutunSayisi; j++)
                        {
                            string pAdi = $"{j + 1}. Palet";
                            bool etiketBasildiMi = true;

                            // 🌟 Eğer palete daha önce etiket basılmamışsa, Kiosk için işaretle ve arka planda barkod uydur
                            if (!hafiza.PaletBarkodlari.ContainsKey(pAdi))
                            {
                                hafiza.PaletBarkodlari[pAdi] = Ean13Olustur();
                                etiketBasildiMi = false;
                                degisiklikYapildi = true;
                            }

                            string barkod = hafiza.PaletBarkodlari[pAdi];
                            string paletGosterimAdi = $"{kvp.Key} - {pAdi}";

                            // 🌟 HTML ETİKET ÜRETİMİ (Kiosk'un içinden yazdırabilmek için hazır ediyoruz)
                            List<string> urunler = new List<string>();
                            int maxSatir = hafiza.PaletMatrisiDurumu.Keys.Count > 0 ? hafiza.PaletMatrisiDurumu.Keys.Max() : -1;

                            for (int i = 0; i <= maxSatir; i++)
                            {
                                if (hafiza.PaletMatrisiDurumu.ContainsKey(i) && hafiza.PaletMatrisiDurumu[i].ContainsKey(j))
                                {
                                    string hamVeri = hafiza.PaletMatrisiDurumu[i][j];
                                    if (!string.IsNullOrWhiteSpace(hamVeri))
                                    {
                                        string[] parcalar = hamVeri.Split(new string[] { " | Adet: " }, StringSplitOptions.None);
                                        string urunKismi = parcalar[0];
                                        string adetKismi = parcalar.Length > 1 ? parcalar[1] : "1";

                                        int parantezIndex = urunKismi.LastIndexOf('(');
                                        if (parantezIndex > 0) urunKismi = urunKismi.Substring(0, parantezIndex).Trim();

                                        string uKodu = urunKismi;
                                        string uAdi = "";
                                        int tireIndex = urunKismi.IndexOf(" - ");

                                        if (tireIndex > 0)
                                        {
                                            uKodu = urunKismi.Substring(0, tireIndex).Trim();
                                            uAdi = urunKismi.Substring(tireIndex + 3).Trim();
                                        }
                                        else
                                        {
                                            uKodu = urunKismi.Trim();
                                            var urun = yerelUrunler.FirstOrDefault(u => u.UrunKodu == uKodu || u.Barkod == uKodu);
                                            if (urun != null) uAdi = urun.Aciklama;
                                            else uAdi = "Bilinmeyen Ürün";
                                        }

                                        urunler.Add($"<li><span class='k-kod'>• {uKodu}</span><span class='k-ad'>{uAdi}</span><span class='k-adet'>Adet: {adetKismi}</span></li>");
                                    }
                                }
                            }

                            string listeHtml = string.Join("", urunler);
                            string yaziPaletAdi = (sutunSayisi == 1) ? "1 Palet Dolap" : pAdi;
                            string sevkMusteriAdi = string.IsNullOrEmpty(hafiza.SevkMusteri) ? "Belirtilmedi" : hafiza.SevkMusteri;
                            string belgeNo = string.IsNullOrEmpty(hafiza.BelgeNo) ? "" : hafiza.BelgeNo;

                            string html = $@"<html>
                            <head>
                               <meta charset='utf-8'>
                               <script src='https://cdn.jsdelivr.net/npm/jsbarcode@3.11.0/dist/JsBarcode.all.min.js'></script>
                               <style>
                                  body {{ font-family: 'Segoe UI', Arial, sans-serif; text-align: center; margin: 10px; }}
                                  .firma {{ font-size: 42px; font-weight: bold; text-transform: uppercase; color: black; margin-bottom: 5px; line-height: 1.1; display: -webkit-box; -webkit-line-clamp: 2; -webkit-box-orient: vertical; overflow: hidden; }}
                                  .sevk-musteri {{ font-size: 24px; font-weight: 600; text-transform: uppercase; color: #444; margin-bottom: 5px; }}
                                  .belge {{ font-size: 22px; margin-bottom: 5px; color: #333; font-weight: bold; }}
                                  .palet {{ font-size: 55px; margin: 10px 0; background: transparent; color: black; font-weight: bold; }}
                                  .urunler {{ text-align: left; font-size: 20px; font-weight: bold; border: 4px dashed black; padding: 15px; width: 98%; box-sizing: border-box; margin: 0 auto; min-height: 140px; }}
                                  ul {{ margin: 0; padding-left: 0; list-style-type: none; }}
                                  li {{ display: flex; justify-content: space-between; align-items: center; margin-bottom: 8px; border-bottom: 1.5px dashed #ccc; padding-bottom: 6px; }}
                                  .k-kod {{ flex: 3; text-align: left; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; padding-right: 5px; font-size: 19px; color: black; }}
                                  .k-ad {{ flex: 5; text-align: left; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; padding-right: 5px; color: #444; font-size: 17px; }}
                                  .k-adet {{ flex: 2; text-align: right; font-size: 20px; color: black; }}
                                  .barkod-alani {{ margin-top: 20px; }} 
                               </style>
                            </head>
                            <body>
                               <div class='firma'>{hafiza.MusteriAdi}</div>
                               <div class='sevk-musteri'>Sevk: {sevkMusteriAdi}</div>
                               <div class='belge'>Belge No: {belgeNo}</div>
                               <div class='palet'>{yaziPaletAdi}</div>
                               <div class='urunler'><ul>{listeHtml}</ul></div>
                               <div class='barkod-alani'><svg id='barkod'></svg></div>
                               <script>
                                  JsBarcode('#barkod', '{barkod}', {{ format: 'EAN13', width: 5, height: 90, displayValue: true, fontSize: 34, fontOptions: 'bold', margin: 0 }});
                               </script>
                            </body></html>";

                            // Modele ekliyoruz
                            kioskPaletler.Add(new FrmKamyonKiosk.KioskPaletModel
                            {
                                Barkod = barkod,
                                PaletAdi = paletGosterimAdi,
                                EtiketBasildiMi = etiketBasildiMi,
                                EtiketHtml = html
                            });
                        }

                        if (degisiklikYapildi)
                        {
                            AmbarHafizasi[kvp.Key] = Newtonsoft.Json.JsonConvert.SerializeObject(hafiza, Newtonsoft.Json.Formatting.Indented);
                        }
                    }
                    catch { }
                }

                if (degisiklikYapildi) AmbarHafizasiniKaydet();

                if (kioskPaletler.Count == 0)
                {
                    MessageBox.Show("Ambardaki firmalara ait yüklenecek palet bulunamadı!", "Eksik Veri", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                frmAmbar.Hide();
                FrmKamyonKiosk kiosk = new FrmKamyonKiosk("🚛 AMBAR ARACI ORTAK SEVKİYAT", kioskPaletler);

                if (kiosk.ShowDialog() == DialogResult.OK)
                {
                    tumPaletlerOkundu = true;
                    btnTamamla.BackColor = Color.DarkGreen;
                    btnTamamla.Cursor = Cursors.Hand;
                }

                frmAmbar.Show();
            };

            // ✅ AMBAR TAMAMLA BUTONU İŞLEMİ
            btnTamamla.Click += (s, ev) =>
            {
                if (AmbarHafizasi.Count == 0)
                {
                    MessageBox.Show("Ambar aracı şu an boş!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // 🌟 ZIRH KONTROLÜ BURADA YAPILIYOR
                if (!tumPaletlerOkundu)
                {
                    MessageBox.Show("DUR! Kamyona yüklenen paletlerin barkod okutması (Kiosk) henüz tamamlanmadı.\nLütfen önce 'Ambar Yüklemeyi Başlat' butonuna tıklayarak tüm paletleri okutun!", "Eksik İşlem", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                DialogResult onay = MessageBox.Show("Ambardaki TÜM firmaların sevkiyatı kapatılıp arşive gönderilecek. Emin misiniz?", "Ambar Çıkışı", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (onay == DialogResult.Yes)
                {
                    // Ambarı boşaltmadan önce içindeki tüm firmaları Arşive (CSV) Döküyoruz!
                    foreach (var kvp in AmbarHafizasi)
                    {
                        try
                        {
                            YarimSevkiyatHafizasi hafiza = Newtonsoft.Json.JsonConvert.DeserializeObject<YarimSevkiyatHafizasi>(kvp.Value);
                            if (hafiza == null) continue;

                            string musteri = string.IsNullOrWhiteSpace(hafiza.MusteriAdi) ? kvp.Key : hafiza.MusteriAdi;
                            string sevkMusteri = hafiza.SevkMusteri;
                            string belgeNo = hafiza.BelgeNo;
                            string paletSayisi = hafiza.PaletSayisi.ToString();
                            string sevkTuru = "AMBAR_SEVK";

                            string belgeKontrol = string.IsNullOrEmpty(belgeNo) ? "SE" : belgeNo.Split(new[] { ',', '_' }, StringSplitOptions.RemoveEmptyEntries)[0].Trim();
                            string turKlasoru = "Yurtiçi"; // Varsayılan

                            // 🌟 GERÇEK ÇÖZÜM: İsme değil, arka plandaki SQL Belge Tipi (O1) sütununa bak!
                            if (dtTumSiparisler != null && dtTumSiparisler.Rows.Count > 0)
                            {
                                DataRow[] dbSatirlari = dtTumSiparisler.Select($"BelgeNo LIKE '%{belgeKontrol}%'");
                                if (dbSatirlari.Length > 0 && dbSatirlari[0]["BelgeTipi"] != DBNull.Value && dbSatirlari[0]["BelgeTipi"].ToString().Trim() == "O1")
                                {
                                    turKlasoru = "İhracat";
                                }
                            }

                            string yil = DateTime.Now.ToString("yyyy");
                            string ay = DateTime.Now.ToString("MM");
                            string gun = DateTime.Now.ToString("dd");

                            string anaYol = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "TamgaApp Tamamlanan Sevkiyatlar", turKlasoru, yil, ay, gun);
                            if (!Directory.Exists(anaYol)) Directory.CreateDirectory(anaYol);

                            string temizMusteri = string.Join("_", musteri.Split(Path.GetInvalidFileNameChars()));
                            if (string.IsNullOrWhiteSpace(temizMusteri)) temizMusteri = "BilinmeyenFirma";

                            string dosyaAdi = $"{temizMusteri}_{paletSayisi}Palet_{sevkTuru}_{DateTime.Now:HHmm}.csv";
                            string tamYol = Path.Combine(anaYol, dosyaAdi);

                            using (StreamWriter sw = new StreamWriter(tamYol, false, System.Text.Encoding.UTF8))
                            {
                                sw.WriteLine("Müşteri;SevkMüşteri;BelgeNo;Tarih;Sevk Türü");
                                sw.WriteLine($"{musteri};{sevkMusteri};{belgeNo};{DateTime.Now:HH:mm};{sevkTuru}");
                                sw.WriteLine("--- DETAYLAR ---");
                                sw.WriteLine("Palet No;İçerik;PaletBarkodu");

                                int sutunSayisi = hafiza.PaletMatrisiDurumu.Values.FirstOrDefault()?.Count ?? 0;

                                for (int j = 0; j < sutunSayisi; j++)
                                {
                                    string paletAdi = $"{j + 1}. Palet";
                                    string paletBarkodu = "";

                                    if (hafiza.PaletBarkodlari != null && hafiza.PaletBarkodlari.ContainsKey(paletAdi))
                                    {
                                        paletBarkodu = hafiza.PaletBarkodlari[paletAdi];
                                    }
                                    else
                                    {
                                        paletBarkodu = Ean13Olustur();
                                    }

                                    int maxSatir = hafiza.PaletMatrisiDurumu.Keys.Count > 0 ? hafiza.PaletMatrisiDurumu.Keys.Max() : -1;
                                    for (int i = 0; i <= maxSatir; i++)
                                    {
                                        if (hafiza.PaletMatrisiDurumu.ContainsKey(i) && hafiza.PaletMatrisiDurumu[i].ContainsKey(j))
                                        {
                                            string icerik = hafiza.PaletMatrisiDurumu[i][j];
                                            if (!string.IsNullOrWhiteSpace(icerik))
                                            {
                                                sw.WriteLine($"{paletAdi};{icerik};{paletBarkodu}");
                                            }
                                        }
                                    }
                                }
                            }

                            string[] belgeler = belgeNo.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            foreach (string b in belgeler)
                            {
                                KaliciKaraListeyeEkle(b);
                            }
                        }
                        catch { }
                    }

                    AmbarHafizasi.Clear();
                    AmbarHafizasiniKaydet();

                    frmAmbar.Close();
                    MessageBox.Show("Kamyon yola çıktı! İçindeki tüm firmalar başarıyla Arşive kaydedildi ve açık sipariş listesinden düşüldü.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    btnSiparisYenile_Click(null, null);
                }
            };

            frmAmbar.Controls.Add(lstAmbardakiFirmalar);
            frmAmbar.Controls.Add(btnSil);
            frmAmbar.Controls.Add(btnYuklemeBaslat);
            frmAmbar.Controls.Add(btnTamamla);
            frmAmbar.ShowDialog();
        }

        #endregion

        // =========================================================================================

        #region 🚛 26. AMBAR VE SEVK KONTROL RAPORLAMA MOTORU

        private void btnAmbar_Click(object sender, EventArgs e)
        {
            AmbarRaporlamaEkraniniAc();
        }

        // 🌟 "Ambar" butonuna bastığında açılacak olan Ana Pencereyi (UI) dinamik olarak çizer
        private void AmbarRaporlamaEkraniniAc()
        {
            if (dgvAmbarSonListe == null || dgvAmbarSonListe.Rows.Count == 0)
            {
                MessageBox.Show("Raporlanacak hiçbir ambar verisi bulunamadı. Lütfen önce listeye palet ekleyin!", "Liste Boş", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 🌟 1. DİNAMİK PALET SAYISINI BUL (SANA İTAAT EDEN ZIRHLI MOTOR)
            int maxPalet = 1;
            foreach (DataGridViewRow row in dgvAmbarSonListe.Rows)
            {
                if (row.IsNewRow) continue;

                // 1. ÖNCELİK: Senin seçtiğin "Palet Sayısı" (Hücre 6) içindeki rakama bak
                string adetMetni = row.Cells[6].Value?.ToString() ?? "1";
                adetMetni = adetMetni.Replace("PALET", "").Trim();

                if (int.TryParse(adetMetni, out int pSayisi))
                {
                    if (pSayisi > maxPalet) maxPalet = pSayisi;
                }

                // 2. ÖNCELİK (Garanti Zırhı): Ölçüler hücresinde alt alta kaç satır var onu say
                string olculerHam = row.Cells[7].Value?.ToString() ?? "";
                int pCount = olculerHam.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
                if (pCount > maxPalet) maxPalet = pCount;
            }

            // 2. Şık Raporlama Formu
            Form frmRapor = new Form
            {
                Text = "📦 Ambar ve Sevk Kontrol Raporlama Merkezi",
                Size = new Size(1200, 700),
                StartPosition = FormStartPosition.CenterScreen,
                BackColor = Color.WhiteSmoke,
                Icon = this.Icon
            };

            // 3. Üst Kontrol Paneli (Şoför Bilgileri ve Butonların duracağı yer)
            Panel pnlUst = new Panel { Dock = DockStyle.Top, Height = 100, BackColor = Color.FromArgb(45, 52, 54) };

            Button btnAmbarRapor = new Button { Text = "📄 AMBAR RAPORU (Geniş)", Location = new Point(20, 15), Size = new Size(300, 45), BackColor = Color.Gold, ForeColor = Color.Black, Font = new Font("Segoe UI", 11, FontStyle.Bold), Cursor = Cursors.Hand, FlatStyle = FlatStyle.Flat };
            Button btnSevkKontrol = new Button { Text = "📋 SEVK KONTROL RAPORU (Dar)", Location = new Point(340, 15), Size = new Size(300, 45), BackColor = Color.White, ForeColor = Color.Black, Font = new Font("Segoe UI", 11, FontStyle.Bold), Cursor = Cursors.Hand, FlatStyle = FlatStyle.Flat };

            // 🌟 YENİ: EXCEL KAYDETME SEÇENEĞİ (CHECKBOX)
            CheckBox chkExcelKaydet = new CheckBox
            {
                Text = "📊 Raporu Excel Olarak Kaydet",
                Location = new Point(660, 25),
                AutoSize = true,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Cursor = Cursors.Hand
            };

            // 🌟 ŞOFÖR, PLAKA, TELEFON KUTULARI
            Label lblSofor = new Label { Text = "Şoför:", Location = new Point(20, 72), AutoSize = true, ForeColor = Color.White, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            TextBox txtSofor = new TextBox { Location = new Point(65, 70), Width = 150, Font = new Font("Segoe UI", 9) };

            Label lblPlaka = new Label { Text = "Plaka:", Location = new Point(240, 72), AutoSize = true, ForeColor = Color.White, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            TextBox txtPlaka = new TextBox { Location = new Point(285, 70), Width = 120, Font = new Font("Segoe UI", 9) };

            Label lblTelefon = new Label { Text = "Telefon:", Location = new Point(430, 72), AutoSize = true, ForeColor = Color.White, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            TextBox txtTelefon = new TextBox { Location = new Point(490, 70), Width = 130, Font = new Font("Segoe UI", 9) };

            pnlUst.Controls.Add(btnAmbarRapor);
            pnlUst.Controls.Add(btnSevkKontrol);
            pnlUst.Controls.Add(chkExcelKaydet); // Excel kutusunu panele ekledik
            pnlUst.Controls.Add(lblSofor); pnlUst.Controls.Add(txtSofor);
            pnlUst.Controls.Add(lblPlaka); pnlUst.Controls.Add(txtPlaka);
            pnlUst.Controls.Add(lblTelefon); pnlUst.Controls.Add(txtTelefon);

            frmRapor.Controls.Add(pnlUst);

            // 4. Verileri Gösterecek Ortak Tablo (DataGridView)
            DataGridView dgvKonsolide = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White,
                RowHeadersVisible = false,
                Font = new Font("Segoe UI", 10)
            };

            dgvKonsolide.Columns.Add("Firma", "FİRMA");
            dgvKonsolide.Columns.Add("Adres", "ADRES (İletişim)");

            // 🌟 DİNAMİK SÜTUN EKLEME (Max Palet sayısına göre sütun inşa et)
            for (int i = 1; i <= maxPalet; i++)
            {
                dgvKonsolide.Columns.Add($"Palet{i}", $"{i}. PALET DESİ");
            }
            dgvKonsolide.Columns.Add("Adet", "PALET ADETİ");

            // dgvAmbarSonListe'deki verileri bu yeni tabloya uygun şekilde ayrıştırarak ekliyoruz
            foreach (DataGridViewRow row in dgvAmbarSonListe.Rows)
            {
                if (row.IsNewRow) continue;

                string firma = row.Cells[1].Value?.ToString() ?? "";
                string adres = $"{row.Cells[2].Value} - {row.Cells[3].Value} | Tel: {row.Cells[4].Value} {row.Cells[5].Value}";
                string adet = row.Cells[6].Value?.ToString() ?? "0";

                string olculerHam = row.Cells[7].Value?.ToString() ?? "";
                string[] olculer = olculerHam.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

                // Dinamik satır oluştur
                int rowIndex = dgvKonsolide.Rows.Add();
                DataGridViewRow newRow = dgvKonsolide.Rows[rowIndex];
                newRow.Cells["Firma"].Value = firma;
                newRow.Cells["Adres"].Value = adres;

                for (int i = 1; i <= maxPalet; i++)
                {
                    newRow.Cells[$"Palet{i}"].Value = olculer.Length >= i ? olculer[i - 1] : "";
                }
                newRow.Cells["Adet"].Value = $"{adet} PALET";
            }

            frmRapor.Controls.Add(dgvKonsolide);
            dgvKonsolide.BringToFront(); // Tabloyu butonların altına yerleştir

            // ====================================================================
            // 🚀 BUTON 1: AMBAR RAPORU (SARI BAŞLIKLI GENİŞ FORMAT)
            // ====================================================================
            btnAmbarRapor.Click += async (s, ev) =>
            {
                if (chkExcelKaydet.Checked)
                {
                    int toplamPaletSayisi = 0;
                    foreach (DataGridViewRow r in dgvKonsolide.Rows)
                    {
                        if (r.IsNewRow) continue;
                        string adetStr = r.Cells["Adet"].Value?.ToString().Replace("PALET", "").Trim() ?? "0";
                        if (int.TryParse(adetStr, out int p)) toplamPaletSayisi += p;
                    }

                    string klasorYolu = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "TamgaApp Tamamlanan Sevkiyatlar", "Ambar", DateTime.Now.ToString("yyyy"), DateTime.Now.ToString("MM"), DateTime.Now.ToString("dd"));
                    if (!Directory.Exists(klasorYolu)) Directory.CreateDirectory(klasorYolu);

                    string tamYol = Path.Combine(klasorYolu, $"{DateTime.Now:dd.MM.yyyy} AMBAR YÜKLEMESİ {toplamPaletSayisi} PALET.xlsx");

                    using (var wb = new ClosedXML.Excel.XLWorkbook())
                    {
                        var ws = wb.Worksheets.Add("Ambar Raporu");
                        ws.Style.Font.FontName = "Times New Roman";
                        ws.Style.Font.FontSize = 11;

                        ws.Cell(1, 1).Value = "FİRMA";
                        ws.Cell(1, 2).Value = "ADRES";
                        int colIndex = 3;
                        for (int i = 1; i <= maxPalet; i++) { ws.Cell(1, colIndex).Value = $"{i}. PALET DESİ"; colIndex++; }
                        ws.Cell(1, colIndex).Value = "PALET ADETİ";

                        int satir = 2;
                        foreach (DataGridViewRow r in dgvKonsolide.Rows)
                        {
                            if (r.IsNewRow) continue;
                            ws.Cell(satir, 1).Value = r.Cells["Firma"].Value?.ToString();
                            ws.Cell(satir, 2).Value = r.Cells["Adres"].Value?.ToString();

                            int cIdx = 3;
                            for (int i = 1; i <= maxPalet; i++) { ws.Cell(satir, cIdx).Value = r.Cells[$"Palet{i}"].Value?.ToString(); cIdx++; }
                            ws.Cell(satir, cIdx).Value = r.Cells["Adet"].Value?.ToString();
                            satir++;
                        }

                        satir += 2;
                        ws.Cell(satir, 1).Value = "ŞOFÖR:"; ws.Cell(satir, 2).Value = txtSofor.Text.Trim(); satir++;
                        ws.Cell(satir, 1).Value = "PLAKA:"; ws.Cell(satir, 2).Value = txtPlaka.Text.Trim(); satir++;
                        ws.Cell(satir, 1).Value = "TELEFON:"; ws.Cell(satir, 2).Value = txtTelefon.Text.Trim();

                        ws.Columns().AdjustToContents();
                        wb.SaveAs(tamYol);
                    }

                    MessageBox.Show($"Saf Excel (.xlsx) olarak kaydedildi!\n\nKayıt Yeri: {tamYol}", "Otomatik Excel Kaydı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(tamYol) { UseShellExecute = true });
                }
                else
                {
                    // 🌟 EXCEL TİKLİ DEĞİLSE ÇALIŞACAK YAZICI (HTML) MOTORU BURADA
                    System.Text.StringBuilder html = new System.Text.StringBuilder();
                    html.AppendLine("<html><head><meta charset='utf-8'><style>");
                    html.AppendLine("@page { size: A4 landscape; margin: 10mm; }");
                    html.AppendLine("body { font-family: 'Calibri', Arial, sans-serif; font-size: 11px; }");
                    html.AppendLine("table { width: 100%; border-collapse: collapse; margin-bottom: 20px; }");
                    html.AppendLine("th, td { border: 1.5px solid black; padding: 4px 6px; }");
                    html.AppendLine("th { background-color: #FFFF00; font-weight: bold; text-align: center; font-size: 12px; }");
                    html.AppendLine(".adres-hucresi { font-size: 10px; max-width: 300px; }");
                    html.AppendLine(".sari-bg { background-color: #FFFF00; font-weight: bold; text-align: center; }");
                    html.AppendLine(".sofor-tablo { width: 350px; font-weight: bold; font-size: 12px; margin-top: 30px; }");
                    html.AppendLine(".sofor-tablo th { background-color: transparent; text-align: left; width: 100px; }");
                    html.AppendLine("</style></head><body>");

                    html.AppendLine("<table><tr>");
                    html.AppendLine("<th style='width: 25%;'>FİRMA</th>");
                    html.AppendLine("<th style='width: 35%;'>ADRES</th>");

                    for (int i = 1; i <= maxPalet; i++) html.AppendLine($"<th>{i}. PALET DESİ</th>");

                    html.AppendLine("<th>PALET ADETİ</th>");
                    html.AppendLine("</tr>");

                    foreach (DataGridViewRow r in dgvKonsolide.Rows)
                    {
                        if (r.IsNewRow) continue;
                        html.AppendLine("<tr>");
                        html.AppendLine($"<td><b>{r.Cells["Firma"].Value}</b></td>");
                        html.AppendLine($"<td class='adres-hucresi'>{r.Cells["Adres"].Value}</td>");

                        for (int i = 1; i <= maxPalet; i++) html.AppendLine($"<td>{r.Cells[$"Palet{i}"].Value}</td>");

                        html.AppendLine($"<td class='sari-bg'>{r.Cells["Adet"].Value}</td>");
                        html.AppendLine("</tr>");
                    }

                    html.AppendLine("</table>");
                    html.AppendLine("<table class='sofor-tablo'>");
                    html.AppendLine($"<tr><th>ŞOFÖR</th><td>: {txtSofor.Text.Trim()}</td></tr>");
                    html.AppendLine($"<tr><th>PLAKA</th><td>: {txtPlaka.Text.Trim()}</td></tr>");
                    html.AppendLine($"<tr><th>TELEFON</th><td>: {txtTelefon.Text.Trim()}</td></tr>");
                    html.AppendLine("</table>");
                    html.AppendLine("</body></html>");

                    await HtmlYaziciMotorunuCalistir(html.ToString(), "Ambar Raporu");
                }
            };

            // ====================================================================
            // 🚀 BUTON 2: SEVK KONTROL RAPORU (BEYAZ BAŞLIKLI DAR FORMAT)
            // ====================================================================
            btnSevkKontrol.Click += async (s, ev) =>
            {
                if (chkExcelKaydet.Checked)
                {
                    using (SaveFileDialog sfd = new SaveFileDialog { Filter = "Excel Dosyası|*.xlsx", FileName = $"Sevk_Kontrol_{DateTime.Now:ddMMyyyy_HHmm}.xlsx" })
                    {
                        if (sfd.ShowDialog() == DialogResult.OK)
                        {
                            using (var wb = new ClosedXML.Excel.XLWorkbook())
                            {
                                var ws = wb.Worksheets.Add("Sevk Kontrol");
                                ws.Style.Font.FontName = "Times New Roman";
                                ws.Style.Font.FontSize = 11;

                                ws.Cell(1, 1).Value = "MÜŞTERİ ADI";
                                ws.Cell(1, 2).Value = "PALET ADETİ";
                                int colIndex = 3;
                                for (int i = 1; i <= maxPalet; i++) { ws.Cell(1, colIndex).Value = "DESİ"; colIndex++; }

                                int satir = 2;
                                foreach (DataGridViewRow r in dgvKonsolide.Rows)
                                {
                                    if (r.IsNewRow) continue;
                                    ws.Cell(satir, 1).Value = r.Cells["Firma"].Value?.ToString();
                                    ws.Cell(satir, 2).Value = r.Cells["Adet"].Value?.ToString();
                                    int cIdx = 3;
                                    for (int i = 1; i <= maxPalet; i++) { ws.Cell(satir, cIdx).Value = r.Cells[$"Palet{i}"].Value?.ToString(); cIdx++; }
                                    satir++;
                                }

                                ws.Columns().AdjustToContents();
                                wb.SaveAs(sfd.FileName);
                            }
                            MessageBox.Show("Saf Excel (.xlsx) olarak kaydedildi!", "Aktarıldı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(sfd.FileName) { UseShellExecute = true });
                        }
                    }
                }
                else
                {
                    // 🌟 EXCEL TİKLİ DEĞİLSE ÇALIŞACAK YAZICI (HTML) MOTORU BURADA
                    System.Text.StringBuilder html = new System.Text.StringBuilder();
                    html.AppendLine("<html><head><meta charset='utf-8'><style>");
                    html.AppendLine("@page { size: A4 portrait; margin: 10mm; }");
                    html.AppendLine("body { font-family: 'Times New Roman', serif; font-size: 13px; }");
                    html.AppendLine("table { width: 100%; border-collapse: collapse; }");
                    html.AppendLine("th, td { border: 1.5px solid black; padding: 6px; text-align: center; }");
                    html.AppendLine("th { font-weight: bold; font-size: 14px; background-color: #F0F0F0; }");
                    html.AppendLine(".sol-hizala { text-align: left; font-weight: bold; }");
                    html.AppendLine("</style></head><body>");

                    html.AppendLine("<table><tr>");
                    html.AppendLine("<th style='width: 45%;'>MÜŞTERİ ADI</th>");
                    html.AppendLine("<th style='width: 15%;'>PALET ADETİ</th>");

                    for (int i = 1; i <= maxPalet; i++) html.AppendLine($"<th>DESİ</th>");

                    html.AppendLine("</tr>");

                    foreach (DataGridViewRow r in dgvKonsolide.Rows)
                    {
                        if (r.IsNewRow) continue;
                        html.AppendLine("<tr>");
                        html.AppendLine($"<td class='sol-hizala'>{r.Cells["Firma"].Value}</td>");
                        html.AppendLine($"<td>{r.Cells["Adet"].Value}</td>");

                        for (int i = 1; i <= maxPalet; i++) html.AppendLine($"<td>{r.Cells[$"Palet{i}"].Value}</td>");

                        html.AppendLine("</tr>");
                    }

                    html.AppendLine("</table></body></html>");

                    await HtmlYaziciMotorunuCalistir(html.ToString(), "Sevk Kontrol Raporu");
                }
            };

            frmRapor.ShowDialog();
        }

        // 🌟 Her iki butonun da kullandığı ortak Edge (WebView2) Yazdırma Motoru
        private async Task HtmlYaziciMotorunuCalistir(string htmlIcerik, string baslik)
        {
            Form frmYazdir = new Form { Text = $"{baslik} Yazdırılıyor...", Width = 900, Height = 600, StartPosition = FormStartPosition.CenterParent, ShowIcon = false };
            Microsoft.Web.WebView2.WinForms.WebView2 web = new Microsoft.Web.WebView2.WinForms.WebView2 { Dock = DockStyle.Fill };
            frmYazdir.Controls.Add(web);
            frmYazdir.FormClosed += (sender, args) => { web.Dispose(); };

            frmYazdir.Shown += async (sender, args) =>
            {
                // Yetki hatasını önlemek için AppData içinde geçici profil yaratıyoruz
                var ozelHafiza = await Microsoft.Web.WebView2.Core.CoreWebView2Environment.CreateAsync(null, System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TamgaApp", "RaporPrint"));
                await web.EnsureCoreWebView2Async(ozelHafiza);

                web.NavigationCompleted += (s, e) => { web.CoreWebView2.ShowPrintUI(Microsoft.Web.WebView2.Core.CoreWebView2PrintDialogKind.Browser); };
                web.NavigateToString(htmlIcerik);
            };

            frmYazdir.ShowDialog();
        }

        #endregion

        // =========================================================================================

        #region 📦 27. STOK VE PİVOT YÖNETİMİ (CANLI SQL ENTEGRASYONLU)



        // 🌟 SINIF (CLASS) SEVİYESİNDE DEĞİŞKENİMİZ (En üste veya metotların dışına koy)
        private FlowLayoutPanel pnlDinamikFiltreler;

        #region 🔍 DİNAMİK STOK ÇOKLU FİLTRE MOTORU

        // 1. MOTOR: Tabloya bakıp otomatik TextBox üreten fabrika
        public void DinamikFiltreKutulariniOlustur(DataGridView dgv)
        {
            if (pnlDinamikFiltreler == null) return;
            pnlDinamikFiltreler.Controls.Clear(); // Önceki sekmenin filtrelerini temizle

            if (dgv == null || dgv.DataSource == null) return;

            DataTable dt = null;
            if (dgv.DataSource is DataTable) dt = (DataTable)dgv.DataSource;
            else if (dgv.DataSource is BindingSource bs && bs.DataSource is DataTable) dt = (DataTable)bs.DataSource;

            if (dt == null) return;

            // Tablodaki her bir sütun için dön
            foreach (DataColumn col in dt.Columns)
            {
                // Her filtre için küçük bir taşıyıcı panel
                Panel pnlHuc = new Panel { Width = 150, Height = 50, Margin = new Padding(5, 0, 5, 0) };

                // Sütun İsmi (Başlık)
                Label lbl = new Label
                {
                    Text = col.ColumnName,
                    Dock = DockStyle.Top,
                    ForeColor = Color.Orange,
                    Font = new Font("Segoe UI", 9, FontStyle.Bold),
                    AutoEllipsis = true,
                    Height = 20
                };

                // Arama Kutusu
                TextBox txt = new TextBox
                {
                    Dock = DockStyle.Bottom,
                    Tag = col.ColumnName, // Kutunun kimliğini Tag içinde saklıyoruz
                    Font = new Font("Segoe UI", 10),
                    BackColor = Color.WhiteSmoke
                };

                // Kutuya her harf yazıldığında anında filtreleme motorunu tetikle!
                txt.TextChanged += (s, e) => FiltreleriUygula(dt);

                pnlHuc.Controls.Add(lbl);
                pnlHuc.Controls.Add(txt);
                pnlDinamikFiltreler.Controls.Add(pnlHuc);
            }
        }

        // 2. MOTOR: Kutulara yazılanları birleştirip SQL RowFilter uygulayan mekanizma
        private void FiltreleriUygula(DataTable dt)
        {
            if (dt == null || pnlDinamikFiltreler == null) return;

            List<string> sqlFiltreleri = new List<string>();

            // Ürettiğimiz tüm kutuları gez
            foreach (Control pnl in pnlDinamikFiltreler.Controls)
            {
                if (pnl is Panel)
                {
                    foreach (Control ctrl in pnl.Controls)
                    {
                        if (ctrl is TextBox txt && !string.IsNullOrWhiteSpace(txt.Text))
                        {
                            string kolonAdi = txt.Tag.ToString();
                            string aranan = txt.Text.Replace("'", "''"); // Tırnak işareti hatasını (SQL Injection) önle

                            // 🌟 SİHİRLİ ZIRH: Sayısal, Tarih veya Metin fark etmeksizin her sütunda LIKE ile arama yapabilmek için 
                            // Convert kullanarak her şeyi string (metin) gibi okutuyoruz. Çökmeyi %100 engeller!
                            sqlFiltreleri.Add($"Convert([{kolonAdi}], 'System.String') LIKE '%{aranan}%'");
                        }
                    }
                }
            }

            // Oluşan filtreleri (Örn: Adı LIKE '%Ahmet%' AND Fiyat LIKE '%50%') tabloya uygula
            string finalFilter = string.Join(" AND ", sqlFiltreleri);
            dt.DefaultView.RowFilter = finalFilter;
        }

        #endregion
        private TabControl tabStokPivotlar;

        // 🌟 1. VERİ MODELİ (Hafızaya kazınacak SQL ayarları)
        public class StokRaporAyari
        {
            public string RaporAdi { get; set; }
            public string BaglantiDizesi { get; set; }
            public string SqlSorgusu { get; set; }
        }

        // 🌟 2. STOK SEKME VE ARAYÜZ KURULUMU
        public void StokSisteminiKur()
        {
            TabPage sekmeStok = tabControl1.TabPages.Cast<TabPage>().FirstOrDefault(t => t.Text == "📦 Stok");

            int anaPanelIndex = 0;
            for (int i = 0; i < tabControl1.TabPages.Count; i++)
            {
                if (tabControl1.TabPages[i].Text.Contains("Ana Panel")) { anaPanelIndex = i; break; }
            }

            if (sekmeStok == null)
            {
                sekmeStok = new TabPage("📦 Stok");
                sekmeStok.BackColor = Color.WhiteSmoke;
                tabControl1.TabPages.Insert(anaPanelIndex + 1, sekmeStok);
            }
            else
            {
                sekmeStok.Controls.Clear();
                tabControl1.TabPages.Remove(sekmeStok);
                tabControl1.TabPages.Insert(anaPanelIndex + 1, sekmeStok);
            }

            // 🌟 1. YENİLİK: Üst Panelin Yüksekliğini Artırdık (Filtreler Sığsın Diye)
            Panel pnlStokUst = new Panel { Dock = DockStyle.Top, Height = 120, BackColor = Color.FromArgb(45, 52, 54) };

            Button btnYeniEkle = new Button { Text = "➕ Yeni SQL Raporu Ekle", Location = new Point(20, 15), Size = new Size(250, 40), BackColor = Color.MediumSeaGreen, ForeColor = Color.White, Font = new Font("Segoe UI", 11, FontStyle.Bold), Cursor = Cursors.Hand, FlatStyle = FlatStyle.Flat };
            Button btnTumuSil = new Button { Text = "❌ Tüm Raporları Sil", Location = new Point(280, 15), Size = new Size(200, 40), BackColor = Color.DarkRed, ForeColor = Color.White, Font = new Font("Segoe UI", 11, FontStyle.Bold), Cursor = Cursors.Hand, FlatStyle = FlatStyle.Flat };

            btnYeniEkle.Click += BtnStokSqlEkle_Click;
            btnTumuSil.Click += BtnStokTumuSil_Click;

            // 🌟 2. YENİLİK: Filtre Kutularının Dizileceği Yatay Panel (Scrollable)
            pnlDinamikFiltreler = new FlowLayoutPanel
            {
                Location = new Point(20, 65),
                Width = 1500, // Ekran genişliğine göre uzar
                Height = 55,
                BackColor = Color.FromArgb(45, 52, 54),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                AutoScroll = true, // Çok sütun varsa yatay kaydırma çubuğu çıkar!
                WrapContents = false // Kutuları alt alta değil, ip gibi yan yana dizer
            };

            pnlStokUst.Controls.Add(btnYeniEkle);
            pnlStokUst.Controls.Add(btnTumuSil);
            pnlStokUst.Controls.Add(pnlDinamikFiltreler);

            tabStokPivotlar = new TabControl { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 11, FontStyle.Bold), ItemSize = new Size(150, 35), SizeMode = TabSizeMode.Fixed };

            tabStokPivotlar.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabStokPivotlar.DrawItem += (s, e) =>
            {
                e.Graphics.FillRectangle(new SolidBrush(e.State == DrawItemState.Selected ? Color.Teal : Color.LightGray), e.Bounds);
                e.Graphics.DrawString(tabStokPivotlar.TabPages[e.Index].Text, new Font("Segoe UI", 10, FontStyle.Bold), new SolidBrush(e.State == DrawItemState.Selected ? Color.White : Color.Black), e.Bounds, new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
            };

            // 🌟 3. YENİLİK: Sekme Değiştiğinde Filtre Motorunu Tetikle!
            tabStokPivotlar.SelectedIndexChanged += (s, e) =>
            {
                if (tabStokPivotlar.SelectedTab != null && tabStokPivotlar.SelectedTab.Controls.Count > 0)
                {
                    // Seçilen sekmenin içindeki DataGridView'i (Tabloyu) bul
                    DataGridView aktifTablo = tabStokPivotlar.SelectedTab.Controls.OfType<DataGridView>().FirstOrDefault();
                    if (aktifTablo != null)
                    {
                        DinamikFiltreKutulariniOlustur(aktifTablo); // Fabrikayı çalıştır
                    }
                }
            };

            sekmeStok.Controls.Add(tabStokPivotlar);
            sekmeStok.Controls.Add(pnlStokUst);

            StokHafizaYukle();
        }

        // 🌟 3. DİNAMİK SQL GİRİŞ EKRANI (POPUP)
        private async void BtnStokSqlEkle_Click(object sender, EventArgs e)
        {
            Form frmSql = new Form
            {
                Text = "⚙️ Canias / SQL Veri Çekme Motoru",
                Size = new Size(700, 550),
                StartPosition = FormStartPosition.CenterScreen,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.WhiteSmoke
            };

            Label lbl1 = new Label { Text = "Rapor (Sekme) Adı:", Location = new Point(20, 20), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            TextBox txtAd = new TextBox { Location = new Point(20, 40), Width = 640, Font = new Font("Segoe UI", 11) };

            Label lbl2 = new Label { Text = "Veritabanı Bağlantı Dizesi (Connection String):", Location = new Point(20, 80), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            TextBox txtBaglanti = new TextBox { Location = new Point(20, 100), Width = 640, Height = 60, Multiline = true, Font = new Font("Segoe UI", 10) };

            // Kolaylık olsun diye sistemdeki mevcut bağlantı dizesini otomatik getiriyoruz (İstersen değiştirirsin)
            try { txtBaglanti.Text = SqlBaglantiDizesiGetir(); } catch { }

            Label lbl3 = new Label { Text = "Çalıştırılacak SQL Sorgusu (Select ...):", Location = new Point(20, 180), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            TextBox txtSorgu = new TextBox { Location = new Point(20, 200), Width = 640, Height = 200, Multiline = true, ScrollBars = ScrollBars.Vertical, Font = new Font("Consolas", 10), BackColor = Color.Black, ForeColor = Color.Lime };

            Button btnGetir = new Button { Text = "🚀 BAĞLAN, GETİR VE KAYDET", Location = new Point(20, 420), Size = new Size(640, 50), BackColor = Color.Teal, ForeColor = Color.White, Font = new Font("Segoe UI", 12, FontStyle.Bold), Cursor = Cursors.Hand };

            btnGetir.Click += async (s, ev) =>
            {
                if (string.IsNullOrWhiteSpace(txtAd.Text) || string.IsNullOrWhiteSpace(txtBaglanti.Text) || string.IsNullOrWhiteSpace(txtSorgu.Text))
                {
                    MessageBox.Show("Lütfen tüm alanları doldurun!", "Eksik Veri", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (tabStokPivotlar.TabPages.Cast<TabPage>().Any(t => t.Text == txtAd.Text.Trim()))
                {
                    MessageBox.Show("Bu isimde bir rapor sekmesi zaten var!", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                btnGetir.Enabled = false;
                btnGetir.Text = "Veriler Çekiliyor, Lütfen Bekleyin...";

                StokRaporAyari ayar = new StokRaporAyari
                {
                    RaporAdi = txtAd.Text.Trim(),
                    BaglantiDizesi = txtBaglanti.Text.Trim(),
                    SqlSorgusu = txtSorgu.Text.Trim()
                };

                DataTable dt = await SqlVeriCekAsync(ayar);

                if (dt != null)
                {
                    StokSekmesiYarat(ayar, dt);
                    StokHafizaKaydet(ayar); // Sadece SQL kodunu ve ayarları JSON olarak diske kaydeder (Veriyi değil!)
                    MessageBox.Show($"'{ayar.RaporAdi}' başarıyla oluşturuldu ve veriler çekildi!", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    frmSql.Close();
                }
                else
                {
                    btnGetir.Enabled = true;
                    btnGetir.Text = "🚀 BAĞLAN, GETİR VE KAYDET";
                }
            };

            frmSql.Controls.Add(lbl1); frmSql.Controls.Add(txtAd);
            frmSql.Controls.Add(lbl2); frmSql.Controls.Add(txtBaglanti);
            frmSql.Controls.Add(lbl3); frmSql.Controls.Add(txtSorgu);
            frmSql.Controls.Add(btnGetir);
            frmSql.ShowDialog();
        }

        // 🌟 4. DİNAMİK SEKME VE DATAGRIDVIEW İNŞA MOTORU
        private void StokSekmesiYarat(StokRaporAyari ayar, DataTable dt)
        {
            TabPage yeniSekme = new TabPage(ayar.RaporAdi) { BackColor = Color.WhiteSmoke, Tag = ayar };

            // O Sekmeye Özel Kontrol Paneli (Eski arama kutusu UÇURULDU, ekran ferahladı)
            Panel pnlKontrol = new Panel { Dock = DockStyle.Top, Height = 55, BackColor = Color.FromArgb(235, 238, 240) };

            // Butonları artık en sola (Point 20) alıyoruz, çok daha şık duracak
            Button btnYenile = new Button { Text = "🔄 SQL'den Veriyi Yenile", Location = new Point(20, 12), Size = new Size(180, 32), BackColor = Color.DodgerBlue, ForeColor = Color.White, Font = new Font("Segoe UI", 9, FontStyle.Bold), Cursor = Cursors.Hand, FlatStyle = FlatStyle.Flat };
            Button btnSekmeSil = new Button { Text = "❌ Raporu Sil", Location = new Point(220, 12), Size = new Size(130, 32), BackColor = Color.Crimson, ForeColor = Color.White, Font = new Font("Segoe UI", 9, FontStyle.Bold), Cursor = Cursors.Hand, FlatStyle = FlatStyle.Flat };

            pnlKontrol.Controls.Add(btnYenile);
            pnlKontrol.Controls.Add(btnSekmeSil);

            // Veri Tablosu (Jilet Gibi, Otomatik Hizalanmış ve Korumalı)
            DataGridView dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                ReadOnly = true, // 🌟 KESİN ZIRH: Elle veri değiştirilemez!
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White,
                RowHeadersVisible = false,
                EnableHeadersVisualStyles = false,
                Font = new Font("Segoe UI", 10)
            };

            // Tablo Tasarımı
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(15, 76, 58);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            dgv.ColumnHeadersHeight = 40;
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.WhiteSmoke;

            BindingSource bs = new BindingSource { DataSource = dt };
            dgv.DataSource = bs;

            // --- OLAYLAR (EVENTS) ---

            // Sekme Silme İşlemi
            btnSekmeSil.Click += (s, ev) =>
            {
                if (MessageBox.Show($"'{ayar.RaporAdi}' raporunu silmek istediğinize emin misiniz?", "Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    tabStokPivotlar.TabPages.Remove(yeniSekme);
                    string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TamgaApp", "StokRaporlari", $"{ayar.RaporAdi}.json");
                    if (File.Exists(path)) File.Delete(path); // Diskten de sil
                }
            };

            // 🌟 CANLI SQL YENİLEME MANTIĞI
            btnYenile.Click += async (s, ev) =>
            {
                btnYenile.Enabled = false;
                btnYenile.Text = "⌛ Yenileniyor...";

                DataTable yeniDt = await SqlVeriCekAsync(ayar);
                if (yeniDt != null)
                {
                    bs.DataSource = yeniDt; // Ekranı anında yeni tabloyla değiştir

                    // Veri yenilendikten sonra filtre kutularını da yeni sütunlara göre tekrar üret (Eğer SQL değiştiyse)
                    DinamikFiltreKutulariniOlustur(dgv);

                    MessageBox.Show("Veriler SQL veritabanından başarıyla güncellendi!", "Yenilendi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                btnYenile.Enabled = true;
                btnYenile.Text = "🔄 SQL'den Veriyi Yenile";
            };

            yeniSekme.Controls.Add(dgv);
            yeniSekme.Controls.Add(pnlKontrol);
            tabStokPivotlar.TabPages.Add(yeniSekme);
            tabStokPivotlar.SelectedTab = yeniSekme;
        }

        // 🌟 5. ARKA PLAN SQL VERİ ÇEKME MOTORU (Arayüzü Dondurmaz)
        private Task<DataTable> SqlVeriCekAsync(StokRaporAyari ayar)
        {
            return Task.Run(() =>
            {
                try
                {
                    DataTable dt = new DataTable();
                    using (OleDbConnection baglanti = new OleDbConnection(ayar.BaglantiDizesi))
                    {
                        using (OleDbDataAdapter adaptor = new OleDbDataAdapter(ayar.SqlSorgusu, baglanti))
                        {
                            adaptor.Fill(dt);
                        }
                    }
                    return dt;
                }
                catch (Exception ex)
                {
                    this.Invoke(new Action(() =>
                    {
                        MessageBox.Show($"SQL Sorgusu çalıştırılamadı!\n\nHata Detayı:\n{ex.Message}", "Veritabanı Hatası", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }));
                    return null;
                }
            });
        }

        // 🌟 6. KALICI HAFIZA (Sadece Ayarları JSON Kaydeder)
        private void StokHafizaKaydet(StokRaporAyari ayar)
        {
            try
            {
                string klasor = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TamgaApp", "StokRaporlari");
                if (!Directory.Exists(klasor)) Directory.CreateDirectory(klasor);

                string json = Newtonsoft.Json.JsonConvert.SerializeObject(ayar, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(Path.Combine(klasor, $"{ayar.RaporAdi}.json"), json);
            }
            catch { }
        }

        // Program Açılışında Çalışır
        private async void StokHafizaYukle()
        {
            string klasor = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TamgaApp", "StokRaporlari");
            if (!Directory.Exists(klasor)) return;

            foreach (string dosya in Directory.GetFiles(klasor, "*.json"))
            {
                try
                {
                    string json = File.ReadAllText(dosya);
                    StokRaporAyari ayar = Newtonsoft.Json.JsonConvert.DeserializeObject<StokRaporAyari>(json);

                    if (ayar != null)
                    {
                        // Ayarları okuyup arka planda SQL'e vurur ve güncel veriyle tabloyu kurar
                        DataTable dt = await SqlVeriCekAsync(ayar);
                        if (dt != null) StokSekmesiYarat(ayar, dt);
                    }
                }
                catch { }
            }
        }

        private void BtnStokTumuSil_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("DİKKAT! Ekli olan TÜM SQL raporları silinecek. Emin misiniz?", "Tümünü Sil", MessageBoxButtons.YesNo, MessageBoxIcon.Error) == DialogResult.Yes)
            {
                tabStokPivotlar.TabPages.Clear();
                string klasor = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TamgaApp", "StokRaporlari");
                if (Directory.Exists(klasor))
                {
                    foreach (string dosya in Directory.GetFiles(klasor, "*.json")) File.Delete(dosya);
                }
            }
        }
        #endregion

        // =========================================================================================

        #region 🛡️ 28. OTOMATİK YEDEKLEME VE FELAKET KURTARMA MOTORU (DISASTER RECOVERY)

        private Timer yedeklemeZamanlayici;
        private string anaVeriKlasoru = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TamgaApp");
        private string yedeklerKlasoru = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "TamgaApp_Sistem_Yedekleri");

        // 1. ZAMANLAYICIYI BAŞLAT (MainForm_Load içine eklenecek)
        public void YedeklemeMotorunuBaslat()
        {
            if (!Directory.Exists(anaVeriKlasoru)) Directory.CreateDirectory(anaVeriKlasoru);
            if (!Directory.Exists(yedeklerKlasoru)) Directory.CreateDirectory(yedeklerKlasoru);

            // Arka planda her 4 saatte bir sessizce çalışır (4 saat = 14.400.000 milisaniye)
            yedeklemeZamanlayici = new Timer();
            yedeklemeZamanlayici.Interval = 4 * 60 * 60 * 1000;
            yedeklemeZamanlayici.Tick += (s, e) => SessizYedekAl();
            yedeklemeZamanlayici.Start();

            // Yönetim sekmesine Backup (Yedek) butonlarını ekler
            YonetimSekmesineYedeklemeUIEkle();
        }

        // 2. SESSİZ YEDEKLEME MANTIĞI (Hayalet Modu - Arayüzü Asla Dondurmaz)
        private async void SessizYedekAl()
        {
            // 🌟 HAYALET ZIRHI: İşlemi ana ekrandan koparıp arka plan işlemcisine (Task) atıyoruz.
            // Sen barkod okuturken sistem 1 milisaniye bile donmaz veya takılmaz!
            await Task.Run(() =>
            {
                try
                {
                    string tarihDamgasi = DateTime.Now.ToString("yyyy-MM-dd_HH-mm");
                    string buYedekKlasoru = Path.Combine(yedeklerKlasoru, $"Yedek_{tarihDamgasi}");

                    KlasorKopyala(anaVeriKlasoru, buYedekKlasoru);
                    System.Diagnostics.Debug.WriteLine($"Sistem yedeği alındı: {buYedekKlasoru}");
                }
                catch
                {
                    // 🌟 ÇÖKME ZIRHI: Ne olursa olsun (dosya kilitli vs.) hataları yutar, 
                    // ekrana ASLA uyarı/hata mesajı çıkartıp senin operasyonunu bölmez!
                }
            });
        }

        // 3. YÖNETİCİ KONTROL PANELİNE (YÖNETİM SEKME) BUTONLARI EKLEME
        private void YonetimSekmesineYedeklemeUIEkle()
        {
            TabPage sekmeYonetim = tabControl1.TabPages.Cast<TabPage>().FirstOrDefault(t => t.Text == "Yönetim");
            if (sekmeYonetim == null) return;

            Panel pnlYedek = new Panel { Dock = DockStyle.Bottom, Height = 80, BackColor = Color.FromArgb(45, 52, 54) };

            Label lblBilgi = new Label { Text = "🛡️ SİSTEM YEDEKLEME VE KURTARMA MERKEZİ", ForeColor = Color.White, Font = new Font("Segoe UI", 10, FontStyle.Bold), AutoSize = true, Location = new Point(10, 10) };

            Button btnManuelYedek = new Button { Text = "💾 ŞİMDİ YEDEK AL", Location = new Point(10, 35), Size = new Size(180, 35), BackColor = Color.MediumSeaGreen, ForeColor = Color.White, Font = new Font("Segoe UI", 9, FontStyle.Bold), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            Button btnYedektenDon = new Button { Text = "♻️ YEDEKTEN GERİ YÜKLE", Location = new Point(200, 35), Size = new Size(200, 35), BackColor = Color.Orange, ForeColor = Color.Black, Font = new Font("Segoe UI", 9, FontStyle.Bold), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            Button btnKlasoruAc = new Button { Text = "📂 YEDEK KLASÖRÜNÜ AÇ", Location = new Point(410, 35), Size = new Size(180, 35), BackColor = Color.Teal, ForeColor = Color.White, Font = new Font("Segoe UI", 9, FontStyle.Bold), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };

            // Olaylar (Events)
            btnManuelYedek.Click += (s, e) =>
            {
                SessizYedekAl();
                MessageBox.Show("Tüm sistem verileri başarıyla yedeklendi!\n\nMasaüstündeki 'TamgaApp_Sistem_Yedekleri' klasöründen ulaşabilirsiniz.", "Yedekleme Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            btnKlasoruAc.Click += (s, e) =>
            {
                if (Directory.Exists(yedeklerKlasoru)) System.Diagnostics.Process.Start("explorer.exe", yedeklerKlasoru);
            };

            btnYedektenDon.Click += BtnYedektenDon_Click;

            pnlYedek.Controls.Add(lblBilgi);
            pnlYedek.Controls.Add(btnManuelYedek);
            pnlYedek.Controls.Add(btnYedektenDon);
            pnlYedek.Controls.Add(btnKlasoruAc);

            sekmeYonetim.Controls.Add(pnlYedek);
        }

        // 4. GERİ YÜKLEME (RESTORE) MOTORU (SADECE YÖNETİCİ KULLANABİLİR)
        private void BtnYedektenDon_Click(object sender, EventArgs e)
        {
            // 🌟 YÖNETİCİ ZIRHI: Sadece tam yetkili kişiler bu butonu çalıştırabilir!
            if (AktifYetkiler != "Sınırsız" && AktifKullaniciAdi.ToLower() != "yönetici")
            {
                MessageBox.Show("Bu işlem Sistem Yöneticisi yetkisi gerektirir! Geri yükleme yapamazsınız.", "Erişim Engellendi", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return;
            }

            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                fbd.Description = "Geri yüklemek istediğiniz Yedek Klasörünü seçin (Masaüstü -> TamgaApp_Sistem_Yedekleri klasörü içindedir):";
                fbd.SelectedPath = yedeklerKlasoru;

                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    string secilenYedek = fbd.SelectedPath;

                    DialogResult onay = MessageBox.Show(
                        "DİKKAT: Mevcut sistemdeki tüm ayarlar silinecek ve seçtiğiniz tarihteki yedeğe geri dönülecektir. Bu işlem geri alınamaz!\n\nDevam etmek istiyor musunuz?",
                        "Kritik Uyarı", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);

                    if (onay == DialogResult.Yes)
                    {
                        try
                        {
                            // Önce mevcut bozuk/eski sistemi sil, sonra yedeği kopyala
                            if (Directory.Exists(anaVeriKlasoru)) Directory.Delete(anaVeriKlasoru, true);
                            KlasorKopyala(secilenYedek, anaVeriKlasoru);

                            MessageBox.Show("Sistem başarıyla yedekten kurtarıldı! Değişikliklerin uygulanması için program şimdi yeniden başlatılacak.", "Kurtarma Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);

                            // Ayarların uygulanması için programı zorla yeniden başlat
                            Application.Restart();
                            Environment.Exit(0);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Geri yükleme sırasında bir dosya kilitli olduğu için hata oluştu:\n" + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
        }

        // 5. KLASÖR VE İÇERİK KOPYALAYICI (Yardımcı Metot)
        private void KlasorKopyala(string kaynakKlasor, string hedefKlasor)
        {
            Directory.CreateDirectory(hedefKlasor);

            // Klasördeki dosyaları kopyala
            foreach (string dosya in Directory.GetFiles(kaynakKlasor))
            {
                try
                {
                    string hedefDosya = Path.Combine(hedefKlasor, Path.GetFileName(dosya));
                    File.Copy(dosya, hedefDosya, true);
                }
                catch { /* Webview kilitli dosyalarını atla (çökmeyi önler) */ }
            }

            // Alt klasörleri de matruşka gibi kopyala (Recursive)
            foreach (string altKlasor in Directory.GetDirectories(kaynakKlasor))
            {
                // Edge Webview'in geçici kilitli klasörünü yedeğe dahil etme
                if (altKlasor.Contains("EBWebView")) continue;

                string hedefAltKlasor = Path.Combine(hedefKlasor, Path.GetFileName(altKlasor));
                KlasorKopyala(altKlasor, hedefAltKlasor);
            }
        }

        #endregion

        // =========================================================================================

        #region 🗄️ 29. GELİŞMİŞ TAM SİSTEM YEDEKLEME (MASTER BACKUP)

        // 🌟 Bu metodu Yönetim sekmesine koyacağın yeni bir "Tüm Sistemi Yedekle" butonunun Click olayına bağlayabilirsin.
        private async void btnTamSistemYedekle_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                fbd.Description = "Yedeğin alınacağı yeri seçin (Örn: Flash Bellek veya D: Sürücüsü)";

                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    string hedefAnaKlasor = fbd.SelectedPath;
                    string tarihDamgasi = DateTime.Now.ToString("yyyy-MM-dd_HH-mm");
                    string yedekKlasoru = Path.Combine(hedefAnaKlasor, $"TamgaApp_TamYedek_{tarihDamgasi}");

                    // 1. ZIRH: Kullanıcı başka bir şeye tıklamasın diye ANA EKRANI KİLİTLE
                    this.Enabled = false;

                    // 2. ŞEKİLLİ ŞUKULLU "ELİT" LÜTFEN BEKLEYİN FORMU (Excel aktarımındaki gibi)
                    Form progressForm = new Form
                    {
                        ControlBox = false,
                        StartPosition = FormStartPosition.CenterScreen,
                        Size = new Size(450, 150),
                        FormBorderStyle = FormBorderStyle.None,
                        BackColor = Color.FromArgb(41, 128, 185), // Güven veren mavi tonu
                        Padding = new Padding(3),
                        ShowInTaskbar = false,
                        TopMost = true
                    };

                    Panel pnlIcerik = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(33, 37, 41) };

                    Label lblBaslik = new Label
                    {
                        Text = "SİSTEM YEDEKLENİYOR",
                        Dock = DockStyle.Top,
                        Height = 50,
                        TextAlign = ContentAlignment.BottomCenter,
                        ForeColor = Color.FromArgb(41, 128, 185),
                        Font = new Font("Segoe UI", 16, FontStyle.Bold)
                    };

                    Label lblDurum = new Label
                    {
                        Text = "Tüm veriler güvene alınıyor, lütfen bekleyiniz...",
                        Dock = DockStyle.Fill,
                        TextAlign = ContentAlignment.MiddleCenter,
                        ForeColor = Color.White,
                        Font = new Font("Segoe UI", 11, FontStyle.Italic)
                    };

                    Timer animTimer = new Timer { Interval = 400 };
                    int noktaCount = 0;
                    animTimer.Tick += (s, ev) =>
                    {
                        noktaCount = (noktaCount + 1) % 4;
                        lblDurum.Text = "Tüm veriler güvene alınıyor" + new string('.', noktaCount);
                    };
                    animTimer.Start();

                    progressForm.FormClosing += (s, ev) => { animTimer.Stop(); animTimer.Dispose(); };

                    pnlIcerik.Controls.Add(lblDurum);
                    pnlIcerik.Controls.Add(lblBaslik);
                    progressForm.Controls.Add(pnlIcerik);

                    progressForm.Show(this);

                    try
                    {
                        // 3. AĞIR İŞİ ARKA PLANA (TASK) AT Kİ EKRAN DONMASIN!
                        await Task.Run(() =>
                        {
                            Directory.CreateDirectory(yedekKlasoru);

                            // YEDEK 1: APPDATA (Tüm Ayarlar, SQL Bağlantıları, Hafıza ve Şablon Klasörleri)
                            string appDataYol = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TamgaApp");
                            if (Directory.Exists(appDataYol))
                            {
                                KlasorKopyala_V2(appDataYol, Path.Combine(yedekKlasoru, "Sistem_Ayarlari_AppData"));
                            }

                            // YEDEK 2: MASAÜSTÜ KLASÖRLERİ (Geçmiş Raporlar, Arşivler, Yarım Kalanlar vs.)
                            string masaustu = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

                            string[] yedeklenecekMasaustuKlasorleri = new string[]
                            {
                                "TamgaApp Tamamlanan Sevkiyatlar",
                                "TamgaApp Yarım Sevkiyatlar",
                                "TamgaApp Sayım Raporları",
                                "TamgaApp Sevkiyat Raporları",
                                "Günlük Üretim Takip"
                            };

                            foreach (string kAd in yedeklenecekMasaustuKlasorleri)
                            {
                                string kYol = Path.Combine(masaustu, kAd);
                                if (Directory.Exists(kYol))
                                {
                                    KlasorKopyala_V2(kYol, Path.Combine(yedekKlasoru, kAd));
                                }
                            }
                        });

                        MessageBox.Show($"Tüm sistem başarıyla yedeklendi!\n\nYedek Yeri:\n{yedekKlasoru}", "Yedekleme Tamamlandı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Yedekleme sırasında bir hata oluştu:\n" + ex.Message, "Kritik Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        // 4. İşlem bitince ekranı temizle ve kilidi aç
                        progressForm.Close();
                        progressForm.Dispose();
                        this.Enabled = true;
                    }
                }
            }
        }

        // 🌟 ZIRHLI KLASÖR KOPYALAMA MOTORU (Açık ve Kilitli Dosyaları Atlar, Çökmeyi Önler)
        private void KlasorKopyala_V2(string kaynakKlasor, string hedefKlasor)
        {
            if (!Directory.Exists(hedefKlasor)) Directory.CreateDirectory(hedefKlasor);

            foreach (string dosya in Directory.GetFiles(kaynakKlasor))
            {
                try
                {
                    string hedefDosya = Path.Combine(hedefKlasor, Path.GetFileName(dosya));
                    File.Copy(dosya, hedefDosya, true);
                }
                catch { /* Kilitli WebView veya açık Excel dosyası varsa atlar, sistemi çökertmez! */ }
            }

            foreach (string altKlasor in Directory.GetDirectories(kaynakKlasor))
            {
                // Edge Webview'in geçici kilitli klasörünü yedeğe dahil etme
                if (altKlasor.Contains("EBWebView") || altKlasor.Contains("EtiketPrintAktif")) continue;

                string hedefAltKlasor = Path.Combine(hedefKlasor, Path.GetFileName(altKlasor));
                KlasorKopyala_V2(altKlasor, hedefAltKlasor);
            }
        }

        #endregion

        // =========================================================================================

        #endregion
    }
}