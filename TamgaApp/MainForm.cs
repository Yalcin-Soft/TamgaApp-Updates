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
    
namespace TamgaApp
{
    public partial class MainForm : Form
    {
        #region 🌐 1. GLOBAL DEĞİŞKENLER VE MODELLER

        // --- Global Hafıza ve Yetki Değişkenleri ---
        public static string AktifKullaniciAdi = "";
        public static string AktifYetkiler = "";

        // --- Tasarım Ekranı (Sürükle-Bırak Motoru) Değişkenleri ---
        private List<DesignItem> designItems = new List<DesignItem>(); // Tüm eklenen kutuları tutan ana liste
        private List<Control> selectedControls = new List<Control>();  // CTRL ile seçilen birden fazla kutu burada durur
        private bool isDragging = false;                               // Kutu taşınıyor mu?
        private bool isResizing = false;                               // Kutu sündürülüyor mu? (Kenardan çekme)
        private string resizeDir = "";                                 // Hangi yöne sündürülüyor? ("WE" sağ-sol, "NS" alt-üst, vb.)
        private Point dragStart;                                       // Sürüklemenin başladığı ilk nokta
        private Control draggingControl;                               // Şu an farenin ucunda olan kutu
        private DesignItem selectedDesignItem;                         // Özellikleri sağda gösterilen aktif kutu

        // --- Tasarım Arayüzü (Masa ve Kağıt) ---
        private Panel pnlWorkspace;                                    // Arka plandaki devasa gri masa
        private ComboBox cmbPaperSize;                                 // Üstteki "DL Zarf, A4, Etiket" seçici menü

        // --- Yazdırma ve Çoklu Zarf Değişkenleri ---
        private Firma currentPreviewFirma;
        private List<Firma> batchFirms;
        private int batchIndex;
        private Dictionary<string, string> printerMappings = new Dictionary<string, string>();
        private const string PrinterSettingsFile = "printer_settings.json";
        private PrintDocument pdUretim;

        [Serializable]
        public class DesignItem
        {
            public string Id { get; set; } = Guid.NewGuid().ToString();
            public string Type { get; set; } // "Label", "Field", "Frame", "Image"
            public string Text { get; set; }
            public string PlaceholderKey { get; set; }

            // Milimetrik Koordinatlar ve Boyutlar
            public float Xmm { get; set; }
            public float Ymm { get; set; }
            public float Wmm { get; set; }
            public float Hmm { get; set; }

            // Tipografi ve Stil
            public string FontName { get; set; } = "Times New Roman";
            public float FontSizePt { get; set; } = 12f;
            public FontStyle FontStyle { get; set; } = FontStyle.Regular;
            public string ColorName { get; set; } = "#000000";
            public string Alignment { get; set; } = "Center"; // Hizalama (Left, Center, Right)
            public int Rotation { get; set; } = 0; // Metin döndürme (0, 90, 180, 270)
        }

        [Serializable]
        private class TemplateFile
        {
            public string TemplateName { get; set; }
            public float PageWidthMm { get; set; }
            public float PageHeightMm { get; set; }
            public string Orientation { get; set; }
            public int Version { get; set; }
            public DateTime CreatedAt { get; set; }
            public List<DesignItem> DesignItems { get; set; }
        }

        #endregion

        // =========================================================================================

        #region ⚙️ 2. KURUCU METOT VE BAŞLANGIÇ AYARLARI

        public MainForm()
        {
            InitializeComponent();
            this.Load += MainForm_Load;
            WireUiEvents();
        }

        /// <summary>
        /// Form ekrana yüklenirken çalışacak olan ana başlangıç motoru. Tüm ayarlamalar burada yapılır.
        /// </summary>
        private void MainForm_Load(object sender, EventArgs e)
        {
            // GİRİŞ YAPAN KULLANICIYI PENCERE BAŞLIĞINA YAZ
            this.Text = $"TamgaApp Otomasyon - Aktif Kullanıcı: {AktifKullaniciAdi}";

            // 🛡️ YETKİLENDİRME KALKANI (Otomatik Tarama Motoru)
            if (AktifYetkiler != "Sınırsız")
            {
                var yetkiListesi = AktifYetkiler.Split(',').Select(y => y.Trim()).ToList();
                foreach (TabPage sekme in tabControl1.TabPages.Cast<TabPage>().ToList())
                {
                    if (!yetkiListesi.Contains(sekme.Text))
                    {
                        tabControl1.TabPages.Remove(sekme);
                    }
                }
            }

            LoadFirmalar();

            

            // Sağ paneldeki araçların varsayılan değerleri
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

            numGridMm.Minimum = 1;
            numGridMm.Maximum = 50;
            numGridMm.Value = 5;
            chkSnapToGrid.Checked = true;

            txtPageWidthMm.Text = "220";
            txtPageHeightMm.Text = "110";
            rbPortrait.Checked = true;

            LoadTemplateList();
            InitializePrinterSettingsTab();

            if (cmbAdet != null) cmbAdet.SelectedIndex = 0;

            // Tasarım ekranının temel motorlarını başlatıyoruz
            SetupPaperSizes();
            SetupResponsiveLayout();

            // Manuel Etiket ekranındaki şablon seçiciyi (ComboBox) dolduran bağımsız motor
            if (cmbManualTemplate != null)
            {
                cmbManualTemplate.Items.Clear();
                string templatesDir = GetTemplatesDirectory();
                var files = Directory.GetFiles(templatesDir, "*.json")
                                     .Select(Path.GetFileName)
                                     .Where(f => f != PrinterSettingsFile)
                                     .ToArray();

                cmbManualTemplate.Items.AddRange(files);
                if (files.Length > 0) cmbManualTemplate.SelectedIndex = 0;
            }

            // Manuel ekrandaki Yazıcı Listesini (cmbManuelPrinter) bilgisayardaki yazıcılarla dolduran motor
            if (cmbManuelPrinter != null)
            {
                cmbManuelPrinter.Items.Clear();
                foreach (string printer in System.Drawing.Printing.PrinterSettings.InstalledPrinters)
                {
                    cmbManuelPrinter.Items.Add(printer);
                }

                PrintDocument pd = new PrintDocument();
                string defaultPrinter = pd.PrinterSettings.PrinterName;
                if (cmbManuelPrinter.Items.Contains(defaultPrinter))
                {
                    cmbManuelPrinter.SelectedItem = defaultPrinter;
                }
                else if (cmbManuelPrinter.Items.Count > 0)
                {
                    cmbManuelPrinter.SelectedIndex = 0;
                }
            }

            // Çoklu Zarf Yazdırma ekranındaki Yazıcı Listesini dolduran motor
            if (cmbCokluPrinter != null)
            {
                cmbCokluPrinter.Items.Clear();
                foreach (string printer in System.Drawing.Printing.PrinterSettings.InstalledPrinters)
                {
                    cmbCokluPrinter.Items.Add(printer);
                }

                PrintDocument pdCoklu = new PrintDocument();
                string defPrinterCoklu = pdCoklu.PrinterSettings.PrinterName;
                if (cmbCokluPrinter.Items.Contains(defPrinterCoklu))
                {
                    cmbCokluPrinter.SelectedItem = defPrinterCoklu;
                }
                else if (cmbCokluPrinter.Items.Count > 0)
                {
                    cmbCokluPrinter.SelectedIndex = 0;
                }
            }

            pnlDesignSurface.Paint += PnlDesignSurface_Paint;
            pnlDesignSurface.MouseDown += PnlDesignSurface_MouseDown;

            // Kayıt yeri ayarını hafızadan yükleme
            string ayarDosyasi = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "KayitYeri.txt");
            if (System.IO.File.Exists(ayarDosyasi))
            {
                if (txtKayitYeri != null) txtKayitYeri.Text = System.IO.File.ReadAllText(ayarDosyasi);
            }

            // Üretim Takip (dgvUretim) Tablo Düzenleme Ayarları
            dgvUretim.ReadOnly = false;
            dgvUretim.Columns["ÜrünKodu"].ReadOnly = true;
            dgvUretim.Columns["ÜrünAçıklaması"].ReadOnly = true;
            dgvUretim.Columns["ÜrünBarkodu"].ReadOnly = true;
            dgvUretim.Columns["ÜrünAdeti"].ReadOnly = false;
            dgvUretim.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvBarkodVerileri.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            // AMBAR TABLOLARINI VE SÜTUNLARINI YARATAN MOTORU ÇALIŞTIR
            AmbarSisteminiHazirla();
        }

        /// <summary>
        /// Ekrandaki tüm butonların, tabloların ve açılır menülerin tıklandığında çalışacak metotlara bağlandığı yer.
        /// </summary>
        private void WireUiEvents()
        {
            if (btnAddLabel != null) btnAddLabel.Click += BtnAddLabel_Click;
            if (btnAddField != null) btnAddField.Click += BtnAddField_Click;
            if (btnAddFrame != null) btnAddFrame.Click += BtnAddFrame_Click;
            if (btnAddImage != null) btnAddImage.Click += BtnAddImage_Click;
            if (btnDeleteItem != null) btnDeleteItem.Click += BtnDeleteItem_Click;

            if (btnApplyProp != null) btnApplyProp.Click += BtnApplyProp_Click;
            if (btnPropColor != null) btnPropColor.Click += BtnPropColor_Click;

            if (btnSaveTemplate != null) btnSaveTemplate.Click += BtnSaveTemplate_Click;
            if (btnLoadTemplate != null) btnLoadTemplate.Click += BtnLoadTemplate_Click;
            if (btnPreview != null) btnPreview.Click += BtnPreview_Click;
            if (btnPrint != null) btnPrint.Click += BtnPrint_Click;

            if (btnManuelOnizle != null) btnManuelOnizle.Click += (s, e) => RunManualPrint(true);
            if (btnManuelYazdir != null) btnManuelYazdir.Click += (s, e) => RunManualPrint(false);
            if (btnTemizleTasarm != null) btnTemizleTasarm.Click += BtnTemizleTasarm_Click;

            if (btnZarfYenile != null) { btnZarfYenile.Click -= btnZarfYenile_Click; btnZarfYenile.Click += btnZarfYenile_Click; }
            if (btnAra != null) { btnAra.Click -= btnAra_Click; btnAra.Click += btnAra_Click; }
            if (btnCikar != null) { btnCikar.Click -= btnCikar_Click; btnCikar.Click += btnCikar_Click; }
            if (btnTemizle != null) { btnTemizle.Click -= btnTemizle_Click; btnTemizle.Click += btnTemizle_Click; }
            if (btnCokluZarfYazdir != null) { btnCokluZarfYazdir.Click -= btnCokluZarfYazdir_Click; btnCokluZarfYazdir.Click += btnCokluZarfYazdir_Click; }
            if (dgvZarfFirmalar != null) { dgvZarfFirmalar.CellDoubleClick -= dgvZarfFirmalar_CellDoubleClick; dgvZarfFirmalar.CellDoubleClick += dgvZarfFirmalar_CellDoubleClick; }
            if (btnAmbarAra != null) { btnAmbarAra.Click -= btnAmbarAra_Click; btnAmbarAra.Click += btnAmbarAra_Click; }
            if (btnTumFirmalariSil != null) { btnTumFirmalariSil.Click -= btnTumFirmalariSil_Click; btnTumFirmalariSil.Click += btnTumFirmalariSil_Click; }

            // Seçici Kutu (Geçiş Motoru) Bağlantısı
            if (cmbZarfTuru != null) { cmbZarfTuru.SelectedIndexChanged -= cmbZarfTuru_SelectedIndexChanged; cmbZarfTuru.SelectedIndexChanged += cmbZarfTuru_SelectedIndexChanged; }

            // AMBAR MOTORU BAĞLANTILARI 
            if (dgvAmbarTumFirmalar != null) { dgvAmbarTumFirmalar.CellDoubleClick -= dgvAmbarTumFirmalar_CellDoubleClick; dgvAmbarTumFirmalar.CellDoubleClick += dgvAmbarTumFirmalar_CellDoubleClick; }
            if (dgvAmbarSecilenFirmalar != null) { dgvAmbarSecilenFirmalar.CellDoubleClick -= dgvAmbarSecilenFirmalar_CellDoubleClick; dgvAmbarSecilenFirmalar.CellDoubleClick += dgvAmbarSecilenFirmalar_CellDoubleClick; }
            if (cmbPaletSayisi != null) { cmbPaletSayisi.SelectedIndexChanged -= cmbPaletSayisi_SelectedIndexChanged; cmbPaletSayisi.SelectedIndexChanged += cmbPaletSayisi_SelectedIndexChanged; }
            if (dgvPaletler != null) { dgvPaletler.CellValueChanged -= dgvPaletler_CellValueChanged; dgvPaletler.CellValueChanged += dgvPaletler_CellValueChanged; }

            if (btnAmbarYenile != null) { btnAmbarYenile.Click -= btnAmbarYenile_Click; btnAmbarYenile.Click += btnAmbarYenile_Click; }
            if (btnAmbarListeyeEkle != null) { btnAmbarListeyeEkle.Click -= btnAmbarListeyeEkle_Click; btnAmbarListeyeEkle.Click += btnAmbarListeyeEkle_Click; }
            if (btnAmbarSil != null) { btnAmbarSil.Click -= btnAmbarSil_Click; btnAmbarSil.Click += btnAmbarSil_Click; }
            if (btnAmbarYazdir != null) { btnAmbarYazdir.Click -= btnAmbarYazdir_Click; btnAmbarYazdir.Click += btnAmbarYazdir_Click; }
        }

        #endregion

        // =========================================================================================

        #region 📐 3. ARAYÜZ VE TASARIM MASASI MOTORU (Responsive & Ruler)

        private void SetupResponsiveLayout()
        {
            if (panel2 != null) { panel2.Dock = DockStyle.Top; panel2.BringToFront(); }
            if (pnlToolbox != null) { pnlToolbox.Dock = DockStyle.Left; pnlToolbox.BringToFront(); }
            if (pnlProperties != null) { pnlProperties.Dock = DockStyle.Right; pnlProperties.BringToFront(); }

            if (pnlWorkspace == null)
            {
                pnlWorkspace = new Panel();
                pnlWorkspace.Dock = DockStyle.Fill;
                pnlWorkspace.BackColor = Color.Gray;
                pnlWorkspace.AutoScroll = true;

                tabPage11.Controls.Add(pnlWorkspace);
                pnlWorkspace.BringToFront();

                pnlDesignSurface.Dock = DockStyle.None;
                pnlDesignSurface.BackColor = Color.White;

                pnlWorkspace.Controls.Add(pnlDesignSurface);

                pnlWorkspace.Resize += (s, e) => CenterDesignSurface();
                pnlWorkspace.Paint += PnlWorkspace_Paint;
            }
        }

        private void CenterDesignSurface()
        {
            if (pnlWorkspace != null && pnlDesignSurface != null)
            {
                int x = (pnlWorkspace.Width - pnlDesignSurface.Width) / 2;
                int y = (pnlWorkspace.Height - pnlDesignSurface.Height) / 2;
                pnlDesignSurface.Location = new Point(Math.Max(20, x), Math.Max(20, y));
                pnlWorkspace.Invalidate();
            }
        }

        private void SetupPaperSizes()
        {
            cmbPaperSize = new ComboBox();
            cmbPaperSize.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbPaperSize.Items.AddRange(new string[] { "DL Zarf (220x110 mm)", "A4 Kağıt (210x297 mm)", "10x15 Etiket (100x150 mm)", "Özel Boyut" });
            cmbPaperSize.Width = 170;

            Label lblPaper = new Label() { Text = "Kağıt Biçimi:", Width = 75, AutoSize = true };

            if (panel2 != null)
            {
                panel2.Controls.Add(lblPaper);
                panel2.Controls.Add(cmbPaperSize);
                lblPaper.Left = 450; lblPaper.Top = 10;
                cmbPaperSize.Left = 530; cmbPaperSize.Top = 8;
            }

            cmbPaperSize.SelectedIndexChanged += CmbPaperSize_SelectedIndexChanged;

            if (rbLandscape != null) rbLandscape.CheckedChanged += (s, e) => { if (rbLandscape.Checked) ApplyDesignSurfaceSize(); };
            if (rbPortrait != null) rbPortrait.CheckedChanged += (s, e) => { if (rbPortrait.Checked) ApplyDesignSurfaceSize(); };

            cmbPaperSize.SelectedIndex = 0;
        }

        private void CmbPaperSize_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbPaperSize.SelectedIndex == 0) { txtPageWidthMm.Text = "220"; txtPageHeightMm.Text = "110"; rbLandscape.Checked = true; }
            else if (cmbPaperSize.SelectedIndex == 1) { txtPageWidthMm.Text = "210"; txtPageHeightMm.Text = "297"; rbPortrait.Checked = true; }
            else if (cmbPaperSize.SelectedIndex == 2) { txtPageWidthMm.Text = "100"; txtPageHeightMm.Text = "150"; rbPortrait.Checked = true; }

            ApplyDesignSurfaceSize();
        }

        private void ApplyDesignSurfaceSize(float w = 0, float h = 0, bool isLand = false)
        {
            if (!float.TryParse(txtPageWidthMm.Text, out w)) w = 220f;
            if (!float.TryParse(txtPageHeightMm.Text, out h)) h = 110f;

            if (rbLandscape != null && rbLandscape.Checked && h > w) { float temp = w; w = h; h = temp; txtPageWidthMm.Text = w.ToString(); txtPageHeightMm.Text = h.ToString(); }
            if (rbPortrait != null && rbPortrait.Checked && w > h) { float temp = w; w = h; h = temp; txtPageWidthMm.Text = w.ToString(); txtPageHeightMm.Text = h.ToString(); }

            float screenDpiX = GetScreenDpiX();
            float widthPx = MmToPx(w, screenDpiX);
            float heightPx = MmToPx(h, screenDpiX);

            if (pnlDesignSurface != null)
            {
                pnlDesignSurface.Width = Math.Max(100, (int)Math.Round(widthPx));
                pnlDesignSurface.Height = Math.Max(100, (int)Math.Round(heightPx));
                pnlDesignSurface.Invalidate();
            }
            CenterDesignSurface();
        }

        private void PnlWorkspace_Paint(object sender, PaintEventArgs e)
        {
            if (pnlDesignSurface == null) return;
            Graphics g = e.Graphics;
            Pen pen = Pens.Black; Brush brush = Brushes.Black; Font font = new Font("Arial", 7);
            int rulerSize = 20;
            int paperX = pnlDesignSurface.Left; int paperY = pnlDesignSurface.Top;
            int paperW = pnlDesignSurface.Width; int paperH = pnlDesignSurface.Height;

            g.FillRectangle(Brushes.WhiteSmoke, paperX, paperY - rulerSize, paperW, rulerSize);
            g.DrawRectangle(pen, paperX, paperY - rulerSize, paperW, rulerSize);
            g.FillRectangle(Brushes.WhiteSmoke, paperX - rulerSize, paperY, rulerSize, paperH);
            g.DrawRectangle(pen, paperX - rulerSize, paperY, rulerSize, paperH);

            float mmToPx = g.DpiX / 25.4f;

            for (int mm = 0; mm * mmToPx <= paperW; mm += 5)
            {
                float px = paperX + (mm * mmToPx);
                if (mm % 10 == 0) { g.DrawLine(pen, px, paperY - rulerSize, px, paperY); g.DrawString(mm.ToString(), font, brush, px + 2, paperY - rulerSize + 2); }
                else { g.DrawLine(pen, px, paperY - rulerSize / 2, px, paperY); }
            }

            for (int mm = 0; mm * mmToPx <= paperH; mm += 5)
            {
                float px = paperY + (mm * mmToPx);
                if (mm % 10 == 0) { g.DrawLine(pen, paperX - rulerSize, px, paperX, px); g.DrawString(mm.ToString(), font, brush, paperX - rulerSize + 2, px + 2); }
                else { g.DrawLine(pen, paperX - rulerSize / 2, px, paperX, px); }
            }
        }

        #endregion

        // =========================================================================================

        #region 🎨 4. GÖRSEL TASARIM EDİTÖRÜ (Sürükle-Bırak, Kutu Ekleme)

        private void PnlDesignSurface_Paint(object sender, PaintEventArgs e)
        {
            if (!chkSnapToGrid.Checked) return;
            float gridPx = e.Graphics.DpiX * (float)numGridMm.Value / 25.4f;
            using (var pen = new Pen(Color.LightGray, 1f))
            {
                for (float x = 0; x < pnlDesignSurface.Width; x += gridPx) e.Graphics.DrawLine(pen, x, 0, x, pnlDesignSurface.Height);
                for (float y = 0; y < pnlDesignSurface.Height; y += gridPx) e.Graphics.DrawLine(pen, 0, y, pnlDesignSurface.Width, y);
            }
        }

        private void CreateControlForDesignItem(DesignItem item)
        {
            if (item == null) return;
            if (item.FontSizePt <= 0) item.FontSizePt = 12f;

            Control ctrl;
            if (item.Type == "Label" || item.Type == "Field")
            {
                var lbl = new Label { AutoSize = false };
                lbl.Text = item.Type == "Field" ? $"{{{item.PlaceholderKey}}}" : (item.Text ?? "");
                try { lbl.Font = new Font(item.FontName ?? "Arial", item.FontSizePt, item.FontStyle); } catch { lbl.Font = new Font("Arial", item.FontSizePt, item.FontStyle); }
                try { lbl.ForeColor = ColorTranslator.FromHtml(item.ColorName); } catch { lbl.ForeColor = Color.Black; }
                lbl.TextAlign = item.Alignment == "Center" ? ContentAlignment.MiddleCenter : item.Alignment == "Right" ? ContentAlignment.MiddleRight : ContentAlignment.MiddleLeft;
                lbl.BackColor = Color.Transparent;
                lbl.BorderStyle = BorderStyle.FixedSingle;
                ctrl = lbl;
            }
            else if (item.Type == "Frame")
            {
                ctrl = new Panel { BorderStyle = BorderStyle.FixedSingle, BackColor = Color.Transparent };
            }
            else
            {
                var pb = new PictureBox { SizeMode = PictureBoxSizeMode.StretchImage, BackColor = Color.LightGray };
                if (item.Type == "Image" && !string.IsNullOrWhiteSpace(item.Text) && File.Exists(item.Text))
                {
                    try { pb.Image = Image.FromFile(item.Text); } catch { pb.Image = null; }
                }
                ctrl = pb;
            }

            ctrl.Tag = item;
            pnlDesignSurface.Controls.Add(ctrl);
            PlaceControlOnDesignSurface(ctrl, item);

            ctrl.DoubleClick -= DesignControl_DoubleClick;
            ctrl.DoubleClick += DesignControl_DoubleClick;
        }

        private void PlaceControlOnDesignSurface(Control ctrl, DesignItem item)
        {
            if (ctrl == null || item == null) return;
            float screenDpi = GetScreenDpiX();
            float mmToPx = screenDpi / 25.4f;

            ctrl.Left = (int)Math.Round(item.Xmm * mmToPx);
            ctrl.Top = (int)Math.Round(item.Ymm * mmToPx);
            ctrl.Width = Math.Max(1, (int)Math.Round(item.Wmm * mmToPx));
            ctrl.Height = Math.Max(1, (int)Math.Round(item.Hmm * mmToPx));

            ctrl.Tag = item;

            ctrl.MouseDown -= DesignControl_MouseDown;
            ctrl.MouseMove -= DesignControl_MouseMove;
            ctrl.MouseUp -= DesignControl_MouseUp;

            ctrl.MouseDown += DesignControl_MouseDown;
            ctrl.MouseMove += DesignControl_MouseMove;
            ctrl.MouseUp += DesignControl_MouseUp;
        }

        private void BtnAddLabel_Click(object sender, EventArgs e)
        {
            var item = new DesignItem
            {
                Type = "Label",
                Text = "Yeni Label",
                Xmm = 10,
                Ymm = 10,
                Wmm = 60,
                Hmm = 10,
                FontName = cmbPropFont.SelectedItem?.ToString() ?? "Arial",
                FontSizePt = (float)numPropFontSize.Value
            };

            designItems.Add(item);
            CreateControlForDesignItem(item);
        }

        private void BtnAddField_Click(object sender, EventArgs e)
        {
            string secilenAlan = cmbPropPlaceholder.SelectedItem?.ToString() ?? "FirmaAdi";

            int izinVerilenMaxAdet = 1;
            if (cmbAdet != null && cmbAdet.SelectedItem != null)
            {
                int.TryParse(cmbAdet.SelectedItem.ToString(), out izinVerilenMaxAdet);
            }

            if (izinVerilenMaxAdet <= 0) izinVerilenMaxAdet = 1;

            int kagitUzerindekiAdet = designItems.Count(x => x.Type == "Field" && x.PlaceholderKey == secilenAlan);

            if (kagitUzerindekiAdet >= izinVerilenMaxAdet)
            {
                MessageBox.Show($"DİKKAT: Kağıda en fazla {izinVerilenMaxAdet} adet '{{{secilenAlan}}}' eklemenize izin verilmiştir!\n\nLütfen limiti artırın veya kağıttakilerden birini silin.", "Kota Doldu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var yeniAlan = new DesignItem
            {
                Type = "Field",
                PlaceholderKey = secilenAlan,
                Text = "",
                Xmm = 10 + (kagitUzerindekiAdet * 10),
                Ymm = 25 + (kagitUzerindekiAdet * 10),
                Wmm = 100,
                Hmm = 12,
                FontName = cmbPropFont.SelectedItem?.ToString() ?? "Arial",
                FontSizePt = (float)numPropFontSize.Value
            };

            designItems.Add(yeniAlan);
            CreateControlForDesignItem(yeniAlan);
        }

        private void BtnAddFrame_Click(object sender, EventArgs e)
        {
            var item = new DesignItem { Type = "Frame", Text = "", Xmm = 5, Ymm = 5, Wmm = 200, Hmm = 50 };
            designItems.Add(item);
            CreateControlForDesignItem(item);
        }

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

        private void BtnDeleteItem_Click(object sender, EventArgs e)
        {
            if (selectedDesignItem == null) return;
            var ctrl = pnlDesignSurface.Controls.Cast<Control>().FirstOrDefault(c => c.Tag == selectedDesignItem);

            if (ctrl != null) pnlDesignSurface.Controls.Remove(ctrl);

            designItems.Remove(selectedDesignItem);
            selectedDesignItem = null;
        }

        private void DesignControl_DoubleClick(object sender, EventArgs e)
        {
            var ctrl = sender as Control;
            var item = ctrl?.Tag as DesignItem;
            if (item == null) return;

            selectedDesignItem = item;

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

        private void BtnApplyProp_Click(object sender, EventArgs e)
        {
            foreach (var ctrl in selectedControls)
            {
                var item = ctrl.Tag as DesignItem;
                if (item == null) continue;

                if (selectedControls.Count == 1)
                {
                    item.Text = txtPropText.Text;
                    item.PlaceholderKey = cmbPropPlaceholder.SelectedItem?.ToString();
                }

                item.FontName = cmbPropFont.SelectedItem?.ToString() ?? "Arial";
                item.FontSizePt = (float)numPropFontSize.Value;
                item.Alignment = cmbPropAlignment.SelectedItem?.ToString() ?? "Left";
                item.ColorName = ColorTranslator.ToHtml(btnPropColor.BackColor);
                item.Rotation = int.Parse(cmbPropRotation.SelectedItem?.ToString() ?? "0");

                if (ctrl is Label lbl)
                {
                    lbl.Text = item.Type == "Field" ? $"{{{item.PlaceholderKey}}}" : item.Text;
                    lbl.Font = new Font(item.FontName, item.FontSizePt, item.FontStyle);
                    try { lbl.ForeColor = ColorTranslator.FromHtml(item.ColorName); } catch { lbl.ForeColor = Color.Black; }
                    lbl.TextAlign = item.Alignment == "Center" ? ContentAlignment.MiddleCenter : item.Alignment == "Right" ? ContentAlignment.MiddleRight : ContentAlignment.MiddleLeft;
                }
            }
        }

        private void BtnPropColor_Click(object sender, EventArgs e)
        {
            using (var cd = new ColorDialog())
            {
                if (cd.ShowDialog() == DialogResult.OK) btnPropColor.BackColor = cd.Color;
            }
        }

        #endregion

        // =========================================================================================

        #region ⌨️ 5. KLAVYE KISAYOLLARI VE KOORDİNAT HESAPLAMALARI (Mouse & Math)

        private void DesignControl_MouseDown(object sender, MouseEventArgs e)
        {
            Control ctrl = sender as Control;
            if (ctrl == null) return;

            if (Control.ModifierKeys == Keys.Control)
            {
                if (selectedControls.Contains(ctrl))
                {
                    selectedControls.Remove(ctrl);
                    ctrl.BackColor = (ctrl is PictureBox) ? Color.LightGray : Color.Transparent;
                }
                else
                {
                    selectedControls.Add(ctrl);
                    ctrl.BackColor = Color.LightBlue;
                }
            }
            else
            {
                if (!selectedControls.Contains(ctrl))
                {
                    ClearSelection();
                    selectedControls.Add(ctrl);
                    ctrl.BackColor = Color.LightBlue;
                }
            }

            draggingControl = ctrl;
            dragStart = e.Location;
            selectedDesignItem = ctrl.Tag as DesignItem;
            ctrl.BringToFront();

            if (ctrl.Cursor == Cursors.SizeNWSE) { isResizing = true; resizeDir = "NWSE"; }
            else if (ctrl.Cursor == Cursors.SizeWE) { isResizing = true; resizeDir = "WE"; }
            else if (ctrl.Cursor == Cursors.SizeNS) { isResizing = true; resizeDir = "NS"; }
            else { isDragging = true; }
        }

        private void DesignControl_MouseMove(object sender, MouseEventArgs e)
        {
            Control ctrl = sender as Control;
            if (ctrl == null) return;

            if (!isDragging && !isResizing)
            {
                int edge = 10;
                if (e.X > ctrl.Width - edge && e.Y > ctrl.Height - edge) ctrl.Cursor = Cursors.SizeNWSE;
                else if (e.X > ctrl.Width - edge) ctrl.Cursor = Cursors.SizeWE;
                else if (e.Y > ctrl.Height - edge) ctrl.Cursor = Cursors.SizeNS;
                else ctrl.Cursor = Cursors.SizeAll;
            }

            if (isResizing && draggingControl != null)
            {
                if (resizeDir == "WE" || resizeDir == "NWSE") draggingControl.Width = Math.Max(10, e.X);
                if (resizeDir == "NS" || resizeDir == "NWSE") draggingControl.Height = Math.Max(10, e.Y);
                UpdateItemPositionFromControl(draggingControl, selectedDesignItem);
            }
            else if (isDragging && draggingControl != null)
            {
                int nx = draggingControl.Left + (e.X - dragStart.X);
                int ny = draggingControl.Top + (e.Y - dragStart.Y);
                if (chkSnapToGrid != null && chkSnapToGrid.Checked) { nx = SnapToGrid(nx); ny = SnapToGrid(ny); }

                draggingControl.Left = Math.Max(0, nx);
                draggingControl.Top = Math.Max(0, ny);
            }
        }

        private void DesignControl_MouseUp(object sender, MouseEventArgs e)
        {
            if (draggingControl != null && draggingControl.Tag is DesignItem item)
            {
                UpdateItemPositionFromControl(draggingControl, item);
            }

            isDragging = false;
            isResizing = false;

            if (draggingControl != null) draggingControl.Cursor = Cursors.SizeAll;
            draggingControl = null;
        }

        private void ClearSelection()
        {
            foreach (var ctrl in selectedControls)
            {
                if (ctrl is PictureBox) ctrl.BackColor = Color.LightGray;
                else ctrl.BackColor = Color.Transparent;
            }
            selectedControls.Clear();
        }

        private void PnlDesignSurface_MouseDown(object sender, MouseEventArgs e)
        {
            ClearSelection();
            selectedDesignItem = null;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (tabPrintSettings != null && tabPrintSettings.SelectedTab == tabPage11 && selectedControls.Count > 0)
            {
                if (keyData == (Keys.Control | Keys.L))
                {
                    int minLeft = selectedControls.Min(c => c.Left);
                    foreach (var c in selectedControls) { c.Left = minLeft; UpdateItemPositionFromControl(c, c.Tag as DesignItem); }
                    return true;
                }

                if (keyData == (Keys.Control | Keys.T))
                {
                    int minTop = selectedControls.Min(c => c.Top);
                    foreach (var c in selectedControls) { c.Top = minTop; UpdateItemPositionFromControl(c, c.Tag as DesignItem); }
                    return true;
                }

                if (keyData == Keys.Delete)
                {
                    foreach (var c in selectedControls.ToList()) { selectedDesignItem = c.Tag as DesignItem; BtnDeleteItem_Click(null, null); }
                    ClearSelection();
                    return true;
                }

                int dx = 0, dy = 0;
                bool isShift = (Control.ModifierKeys & Keys.Shift) == Keys.Shift;

                if ((keyData & Keys.KeyCode) == Keys.Up) dy = -1;
                else if ((keyData & Keys.KeyCode) == Keys.Down) dy = 1;
                else if ((keyData & Keys.KeyCode) == Keys.Left) dx = -1;
                else if ((keyData & Keys.KeyCode) == Keys.Right) dx = 1;

                if (dx != 0 || dy != 0)
                {
                    foreach (var c in selectedControls)
                    {
                        if (isShift) { c.Width = Math.Max(10, c.Width + dx); c.Height = Math.Max(10, c.Height + dy); }
                        else { c.Left += dx; c.Top += dy; }
                        UpdateItemPositionFromControl(c, c.Tag as DesignItem);
                    }
                    return true;
                }
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void UpdateItemPositionFromControl(Control ctrl, DesignItem item)
        {
            using (var g = pnlDesignSurface.CreateGraphics())
            {
                item.Xmm = PxToMm(ctrl.Left, g);
                item.Ymm = PxToMm(ctrl.Top, g);
                item.Wmm = PxToMm(ctrl.Width, g);
                item.Hmm = PxToMm(ctrl.Height, g);

                try
                {
                    if (numPropXmm != null) numPropXmm.Value = Math.Max(numPropXmm.Minimum, Math.Min(numPropXmm.Maximum, (decimal)item.Xmm));
                    if (numPropYmm != null) numPropYmm.Value = Math.Max(numPropYmm.Minimum, Math.Min(numPropYmm.Maximum, (decimal)item.Ymm));
                    if (numPropWmm != null) numPropWmm.Value = Math.Max(numPropWmm.Minimum, Math.Min(numPropWmm.Maximum, (decimal)item.Wmm));
                    if (numPropHmm != null) numPropHmm.Value = Math.Max(numPropHmm.Minimum, Math.Min(numPropHmm.Maximum, (decimal)item.Hmm));
                }
                catch { }
            }
        }

        private int SnapToGrid(int px)
        {
            using (var g = pnlDesignSurface.CreateGraphics())
            {
                float gridPx = MmToPx((float)numGridMm.Value, g);
                return gridPx <= 0 ? px : (int)(Math.Round(px / gridPx) * gridPx);
            }
        }

        private int MmToPx(float mm, Graphics g) { return (int)Math.Round(mm * g.DpiX / 25.4f); }
        private float PxToMm(int px, Graphics g) { return px * 25.4f / g.DpiX; }
        private float GetScreenDpiX() { using (var g = pnlDesignSurface.CreateGraphics()) return g.DpiX; }
        private float MmToPx(float mm, float dpi) => dpi / 25.4f * mm;

        #endregion

        // =========================================================================================

        #region 🖨️ 6. YAZDIRMA VE ÖNİZLEME MOTORU (Printer Settings)

        private void InitializePrinterSettingsTab()
        {
            try
            {
                if (cmbPrinters == null || cmbPrintingPages == null) return;

                cmbPrinters.Items.Clear();
                foreach (string printer in PrinterSettings.InstalledPrinters)
                {
                    cmbPrinters.Items.Add(printer);
                }

                cmbPrintingPages.Items.Clear();
                cmbPrintingPages.Items.AddRange(new string[] { "Tekli Zarf Yazdırma", "Çoklu Zarf Yazdırma" });

                LoadPrinterMappings();

                if (cmbPrintingPages.Items.Count > 0) cmbPrintingPages.SelectedIndex = 0;
                cmbPrintingPages.SelectedIndexChanged += CmbPrintingPages_SelectedIndexChanged;

                if (btnSavePrinterMapping != null) btnSavePrinterMapping.Click += BtnSavePrinterMapping_Click;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Yazıcı listesi yükleme hatası: " + ex.Message);
            }
        }

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

        private void BtnSavePrinterMapping_Click(object sender, EventArgs e)
        {
            if (cmbPrintingPages.SelectedItem == null || cmbPrinters.SelectedItem == null)
            {
                MessageBox.Show("Lütfen önce bir sayfa ve bir yazıcı seçin!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string selectedPage = cmbPrintingPages.SelectedItem.ToString();
            string selectedPrinter = cmbPrinters.SelectedItem.ToString();

            printerMappings[selectedPage] = selectedPrinter;

            try
            {
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

        private void ApplyPrinterMapping(PrintDocument doc, string sayfaAdi)
        {
            if (printerMappings != null && printerMappings.ContainsKey(sayfaAdi))
            {
                string atananYazici = printerMappings[sayfaAdi];
                if (PrinterSettings.InstalledPrinters.Cast<string>().Contains(atananYazici))
                {
                    doc.PrinterSettings.PrinterName = atananYazici;
                }
            }
        }

        private void BtnPreview_Click(object sender, EventArgs e)
        {
            batchFirms = null;
            if (printDocument1 != null) { printDocument1.Dispose(); }
            printDocument1 = new PrintDocument();

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

            ApplyDesignSurfaceSize(pageW, pageH, rbLandscape != null && rbLandscape.Checked);

            foreach (var item in designItems)
            {
                Control ctrl = pnlDesignSurface.Controls.Cast<Control>().FirstOrDefault(c => object.ReferenceEquals(c.Tag, item));
                if (ctrl != null) PlaceControlOnDesignSurface(ctrl, item);
            }

            printPreviewDialog1.Document = printDocument1;
            try { printPreviewDialog1.ShowDialog(); } catch { }
        }

        private void BtnPrint_Click(object sender, EventArgs e)
        {
            batchFirms = null;
            var firma = GetSelectedFirmaForPreview();

            if (firma == null) { MessageBox.Show("Yazdırılacak firma seçin."); return; }
            currentPreviewFirma = firma;

            if (printDocument1 != null) { printDocument1.Dispose(); }
            printDocument1 = new PrintDocument();

            ApplyPrinterMapping(printDocument1, "Tekli Zarf Yazdırma");

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
            printDocument1.Print();
        }

        private void btnCokluZarfYazdir_Click(object sender, EventArgs e)
        {
            if (lstSecilenFirmalar.CheckedItems.Count == 0) { MessageBox.Show("Lütfen firmaları işaretleyin."); return; }
            if (cmbPrintStyle.SelectedItem == null) { MessageBox.Show("Şablon seçin."); return; }

            string path = Path.Combine(GetTemplatesDirectory(), cmbPrintStyle.SelectedItem.ToString());
            if (!File.Exists(path)) return;

            var loadedTemplate = JsonConvert.DeserializeObject<TemplateFile>(File.ReadAllText(path));
            if (loadedTemplate == null) return;

            if (printDocument1 != null) { printDocument1.Dispose(); }
            printDocument1 = new PrintDocument();

            if (labell != null && cmbCokluPrinter.SelectedItem != null)
            {
                printDocument1.PrinterSettings.PrinterName = cmbCokluPrinter.SelectedItem.ToString();
            }
            else
            {
                ApplyPrinterMapping(printDocument1, "Çoklu Zarf Yazdırma");
            }

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

            designItems = loadedTemplate.DesignItems ?? new List<DesignItem>();
            batchFirms = new List<Firma>();

            foreach (var item in lstSecilenFirmalar.CheckedItems)
            {
                int id = int.Parse(item.ToString().Split('-')[0].Trim());
                var f = DataAccess.GetFirmaById(id);
                if (f != null) batchFirms.Add(f);
            }

            batchIndex = 0;
            printPreviewDialog1.Document = printDocument1;
            try { printPreviewDialog1.ShowDialog(); } catch { }
        }

        private void PrintDocument1_BeginPrint(object sender, PrintEventArgs e) { batchIndex = 0; }

        private void PrintDocument1_PrintPage(object sender, PrintPageEventArgs e)
        {
            try
            {
                float printerMmToPx = 100f / 25.4f;
                Firma firma;

                if (batchFirms != null && batchFirms.Count > 0)
                {
                    if (batchIndex < 0 || batchIndex >= batchFirms.Count) batchIndex = 0;
                    firma = batchFirms[batchIndex];
                }
                else
                {
                    firma = currentPreviewFirma ?? GetSelectedFirmaForPreview();
                }

                if (designItems == null || designItems.Count == 0) { e.HasMorePages = false; return; }

                foreach (var item in designItems)
                {
                    float x = item.Xmm * printerMmToPx, y = item.Ymm * printerMmToPx, w = item.Wmm * printerMmToPx, h = item.Hmm * printerMmToPx;
                    RectangleF rect = new RectangleF(x, y, w, h);

                    if (item.Type == "Frame") { e.Graphics.DrawRectangle(Pens.Black, Rectangle.Round(rect)); continue; }

                    if (item.Type == "Image")
                    {
                        if (File.Exists(item.Text))
                        {
                            try { using (var img = Image.FromFile(item.Text)) e.Graphics.DrawImage(img, rect); } catch { }
                        }
                        continue;
                    }

                    string text = "";
                    if (item.Type == "Field")
                    {
                        if (item.PlaceholderKey == "FirmaAdi") text = firma.FirmaAdi;
                        else if (item.PlaceholderKey == "Adres") text = firma.Adres;
                        else if (item.PlaceholderKey == "Il") text = firma.Il;
                        else if (item.PlaceholderKey == "Telefon1") text = firma.Telefon1;
                        else if (item.PlaceholderKey == "Telefon2") text = firma.Telefon2;

                        if (string.IsNullOrWhiteSpace(text)) text = "";
                    }
                    else
                    {
                        text = item.Text;
                    }

                    if (string.IsNullOrWhiteSpace(text)) continue;

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
                            e.Graphics.DrawString(text, f, drawBrush, rect, sf);
                        }

                        if (drawBrush != Brushes.Black) drawBrush.Dispose();
                    }
                }

                if (batchFirms != null && batchFirms.Count > 0)
                {
                    batchIndex++;
                    e.HasMorePages = (batchIndex < batchFirms.Count);
                }
                else
                {
                    e.HasMorePages = false;
                }
            }
            catch
            {
                e.HasMorePages = false;
            }
        }

        #endregion

        // =========================================================================================

        #region 🗃️ 7. FİRMA VE VERİTABANI İŞLEMLERİ (CRUD, Excel Aktarım)

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
                MessageBox.Show("Firma kaydedildi.");
                txtFirmaAdi.Clear();
                txtAdres.Clear();
                txtIl.Clear();
                txtTel1.Clear();
                txtTel2.Clear();
                LoadFirmalar();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Kayıt sırasında hata: " + ex.Message);
            }
        }

        private void LoadFirmalar()
        {
            lstFirmalar.Items.Clear();
            var firmalar = DataAccess.GetAllFirmalar();
            foreach (var f in firmalar)
            {
                lstFirmalar.Items.Add($"{f.Id} - {f.FirmaAdi}");
            }
        }

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

        private void btnGuncelle_Click(object sender, EventArgs e)
        {
            if (lstFirmalar.SelectedItem != null)
            {
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
                MessageBox.Show("Firma güncellendi.");
                LoadFirmalar();
            }
        }

        private void btnListele_Click(object sender, EventArgs e)
        {
            dgvFirmalar.Rows.Clear();
            var firmalar = DataAccess.GetAllFirmalar();
            foreach (var f in firmalar)
            {
                dgvFirmalar.Rows.Add(f.Id, f.FirmaAdi, f.Adres, f.Il, f.Telefon1, f.Telefon2);
            }
            if (firmalar.Count == 0) MessageBox.Show("Veritabanında kayıtlı firma yok.");
        }

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

        private void btnZarfYenile_Click(object sender, EventArgs e)
        {
            LoadZarfFirmalar();
        }

        private void btnAra_Click(object sender, EventArgs e)
        {
            string aranan = txtAramaFirmaAdi.Text.Trim().ToLower();
            dgvZarfFirmalar.Rows.Clear();
            var firmalar = DataAccess.GetAllFirmalar();

            foreach (var f in firmalar)
            {
                if (f.FirmaAdi.ToLower().StartsWith(aranan))
                    dgvZarfFirmalar.Rows.Add(f.Id, f.FirmaAdi, f.Adres, f.Il, f.Telefon1, f.Telefon2);
            }
            if (dgvZarfFirmalar.Rows.Count == 0) MessageBox.Show("Aramanıza uygun firma bulunamadı.");
        }

        private void dgvZarfFirmalar_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                var row = dgvZarfFirmalar.Rows[e.RowIndex];
                string item = $"{row.Cells[0].Value} - {row.Cells[1].Value}";
                if (!lstSecilenFirmalar.Items.Contains(item)) lstSecilenFirmalar.Items.Add(item, true);
            }
        }

        private void btnCikar_Click(object sender, EventArgs e)
        {
            if (lstSecilenFirmalar.SelectedItem != null)
                lstSecilenFirmalar.Items.Remove(lstSecilenFirmalar.SelectedItem);
            else
                MessageBox.Show("Lütfen listeden çıkarmak istediğiniz firmayı seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void btnTemizle_Click(object sender, EventArgs e)
        {
            lstSecilenFirmalar.Items.Clear();
        }

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

        private void lstFirmalar_DoubleClick(object sender, EventArgs e)
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

        private void btnTopluAktar_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "CSV Dosyaları (*.csv)|*.csv|Metin Dosyaları (*.txt)|*.txt|Tüm Dosyalar (*.*)|*.*";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string[] satirlar = File.ReadAllLines(ofd.FileName, System.Text.Encoding.Default);
                        int eklenenSayisi = 0;

                        foreach (string satir in satirlar)
                        {
                            if (string.IsNullOrWhiteSpace(satir)) continue;

                            // 🚀 KRİTİK ÇÖZÜM BÖLÜMÜ: Excel'in eklediği o gıcık tırnak işaretlerini (") toptan yok ediyoruz!
                            string temizSatir = satir.Replace("\"", "");

                            string[] hucreler = temizSatir.Split(';');
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
                        MessageBox.Show($"{eklenenSayisi} adet firma başarıyla içe aktarıldı!", "İşlem Tamam", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Dosya okunurken bir hata oluştu. Hata Detayı: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

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

        // 💣 FİRMA DÜZENLEME SAYFASI - TOPLU SİLME MOTORU
        private void btnTumFirmalariSil_Click(object sender, EventArgs e)
        {
            DialogResult ilkCevap = MessageBox.Show(
                "DİKKAT: Veritabanındaki KAYITLI TÜM FİRMALAR kalıcı olarak silinecektir!\n\nBu işlemi geri alamazsınız. Devam etmek istediğinize emin misiniz?",
                "Kritik Toplu Silme Onayı",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);

            if (ilkCevap == DialogResult.Yes)
            {
                try
                {
                    DataAccess.DeleteAllFirmalar();

                    // 🔄 EKRANDAKİ TÜM LİSTELERİ ANINDA SIFIRLA
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
                    MessageBox.Show("Silme işlemi sırasında bir hata oluştu:\n\n" + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        #endregion

        // =========================================================================================

        #region 📂 8. ŞABLON DOSYA YÖNETİMİ (KAYDET/YÜKLE/SİL)

        private string GetTemplatesDirectory()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string templatesDir = Path.Combine(appData, "TamgaApp", "Templates");
            if (!Directory.Exists(templatesDir)) Directory.CreateDirectory(templatesDir);
            return templatesDir;
        }

        private void LoadTemplateList()
        {
            string templatesDir = GetTemplatesDirectory();
            var files = Directory.GetFiles(templatesDir, "*.json").Select(Path.GetFileName).Where(f => f != PrinterSettingsFile).OrderBy(n => n).ToArray();

            lstTemplates.Items.Clear();
            cmbPrintStyle.Items.Clear();

            if (files.Length > 0)
            {
                lstTemplates.Items.AddRange(files);
                cmbPrintStyle.Items.AddRange(files);
            }
        }

        private void BtnSaveTemplate_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog sfd = new SaveFileDialog { InitialDirectory = GetTemplatesDirectory(), Title = "Şablonu Hangi İsimle Kaydetmek İstersiniz?", Filter = "Şablon Dosyası (*.json)|*.json", DefaultExt = "json", FileName = "Yeni_Zarf_Sablonu" })
            {
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string fileName = Path.GetFileName(sfd.FileName);
                        var tf = new TemplateFile
                        {
                            TemplateName = Path.GetFileNameWithoutExtension(fileName),
                            PageWidthMm = float.TryParse(txtPageWidthMm.Text, out float w) ? w : 220,
                            PageHeightMm = float.TryParse(txtPageHeightMm.Text, out float h) ? h : 110,
                            Orientation = rbLandscape.Checked ? "Landscape" : "Portrait",
                            CreatedAt = DateTime.Now,
                            DesignItems = designItems
                        };

                        File.WriteAllText(Path.Combine(GetTemplatesDirectory(), fileName), JsonConvert.SerializeObject(tf, Formatting.Indented));
                        LoadTemplateList();
                        MessageBox.Show($"Şablon başarıyla kaydedildi:\n{fileName}", "Kayıt Tamamlandı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Şablon kaydedilirken bir hata oluştu: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void BtnLoadTemplate_Click(object sender, EventArgs e)
        {
            if (lstTemplates.SelectedItem == null)
            {
                MessageBox.Show("Lütfen bir şablon seçin.");
                return;
            }

            string path = Path.Combine(GetTemplatesDirectory(), lstTemplates.SelectedItem.ToString());
            if (!File.Exists(path))
            {
                MessageBox.Show("Şablon dosyası bulunamadı.");
                return;
            }

            var loaded = JsonConvert.DeserializeObject<TemplateFile>(File.ReadAllText(path));
            if (loaded == null)
            {
                MessageBox.Show("Şablon yüklenemedi.");
                return;
            }

            txtPageWidthMm.Text = loaded.PageWidthMm.ToString();
            txtPageHeightMm.Text = loaded.PageHeightMm.ToString();

            if (loaded.Orientation == "Landscape") rbLandscape.Checked = true;
            else rbPortrait.Checked = true;

            designItems = loaded.DesignItems ?? new List<DesignItem>();
            pnlDesignSurface.Controls.Clear();

            foreach (var item in designItems) CreateControlForDesignItem(item);
        }

        private void btnDeleteTemplate_Click(object sender, EventArgs e)
        {
            if (lstTemplates.SelectedItem == null)
            {
                MessageBox.Show("Lütfen silmek istediğiniz şablonu listeden seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string selectedTemplate = lstTemplates.SelectedItem.ToString();
            if (MessageBox.Show($"'{selectedTemplate}' adlı şablonu kalıcı olarak silmek istediğinize emin misiniz?", "Şablon Sil", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                try
                {
                    string fullPath = Path.Combine(GetTemplatesDirectory(), selectedTemplate);
                    if (File.Exists(fullPath)) File.Delete(fullPath);
                    LoadTemplateList();
                    MessageBox.Show("Şablon başarıyla silindi.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Şablon silinirken bir hata oluştu: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        #endregion

        // =========================================================================================

        #region 🚀 9. MANUEL ETİKET YAZDIRMA (TASARIM EKRANINA BAĞLI MOTOR)

        private void RunManualPrint(bool isPreview)
        {
            if (cmbManualTemplate.SelectedItem == null) { MessageBox.Show("Lütfen önce bir şablon seçin!"); return; }

            string path = Path.Combine(GetTemplatesDirectory(), cmbManualTemplate.SelectedItem.ToString());
            if (!File.Exists(path)) return;

            var loadedTemplate = JsonConvert.DeserializeObject<TemplateFile>(File.ReadAllText(path));

            // Ekrandaki TextBox'lardan verileri alıp "Hayalet" bir Firma kaydı oluşturuyoruz
            Firma manualFirma = new Firma
            {
                FirmaAdi = txtManFirma.Text.Trim(),
                Adres = txtManAdres.Text.Trim(),
                Il = txtManIl.Text.Trim(),
                Telefon1 = txtManTel1.Text.Trim(),
                Telefon2 = txtManTel2.Text.Trim()
            };

            batchFirms = new List<Firma> { manualFirma };
            batchIndex = 0;

            if (printDocument1 != null) printDocument1.Dispose();
            printDocument1 = new PrintDocument();

            if (cmbManuelPrinter != null && cmbManuelPrinter.SelectedItem != null)
            {
                printDocument1.PrinterSettings.PrinterName = cmbManuelPrinter.SelectedItem.ToString();
            }
            else
            {
                ApplyPrinterMapping(printDocument1, "Tekli Zarf Yazdırma");
            }

            int printW = (int)(loadedTemplate.PageWidthMm * 100f / 25.4f);
            int printH = (int)(loadedTemplate.PageHeightMm * 100f / 25.4f);
            bool isLandscape = (loadedTemplate.Orientation == "Landscape");

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

            designItems = loadedTemplate.DesignItems ?? new List<DesignItem>();

            printDocument1.PrintPage += PrintDocument1_PrintPage;
            printDocument1.BeginPrint += PrintDocument1_BeginPrint;

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

        // Kağıdı tertemiz yapan, tüm hayaletleri silen motor
        private void BtnTemizleTasarm_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Tüm tasarım nesnelerini silmek istediğinize emin misiniz? Bu işlem geri alınamaz!",
                "Tasarımı Sıfırla", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                designItems.Clear();
                pnlDesignSurface.Controls.Clear();
                selectedControls.Clear();
                selectedDesignItem = null;
                pnlDesignSurface.Invalidate();

                MessageBox.Show("Tasarım ekranı başarıyla temizlendi!", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        #endregion

        // =========================================================================================

        #region 🏭 10. ÜRETİM VE BARKOD TAKİP MOTORU (Üretim İleri/Geri, Barkod Oku, Kaydet)

        private void btnUretimIleri_Click(object sender, EventArgs e)
        {
            pnlGunSecimi.Visible = false;
            pnlBarkodOkuma.Visible = true;

            // Bilgisayardaki yazıcıları ComboBox'a doldur
            cmbUretimYazici.Items.Clear();
            foreach (string printer in System.Drawing.Printing.PrinterSettings.InstalledPrinters)
            {
                cmbUretimYazici.Items.Add(printer);
            }
            if (cmbUretimYazici.Items.Count > 0) cmbUretimYazici.SelectedIndex = 0;

            txtBarkodOkut.Focus();
        }

        private void btnUretimGeri_Click(object sender, EventArgs e)
        {
            pnlBarkodOkuma.Visible = false;
            pnlGunSecimi.Visible = true;
        }

        private void txtBarkodOkut_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true; // "Dıt" uyarı sesini engelle
                string okunanBarkod = txtBarkodOkut.Text.Trim();

                if (!string.IsNullOrEmpty(okunanBarkod))
                {
                    // 1. GERÇEK ARAMA: Veritabanına git ve bu barkodu getir!
                    Urun bulunanUrun = DataAccess.GetUrunByBarkod(okunanBarkod);

                    // Eğer okutulan barkod Excel'den çektiğimiz veritabanında YOKSA:
                    if (bulunanUrun == null)
                    {
                        bulunanUrun = new Urun { UrunKodu = "KAYITSIZ", Aciklama = "SİSTEMDE BULUNAMADI!", Barkod = okunanBarkod };
                    }

                    // 2. Tabloda bu barkod zaten var mı kontrolü (Sadece Adet artırmak için)
                    bool varMi = false;
                    foreach (DataGridViewRow row in dgvUretim.Rows)
                    {
                        if (row.Cells[3].Value != null && row.Cells[3].Value.ToString() == okunanBarkod)
                        {
                            int mevcutAdet = Convert.ToInt32(row.Cells[2].Value);
                            row.Cells[2].Value = mevcutAdet + 1; // Zaten varsa adeti 1 artır
                            varMi = true;
                            break;
                        }
                    }

                    // 3. Tabloda yoksa veritabanından gelen GERÇEK bilgileri bas!
                    if (!varMi)
                    {
                        dgvUretim.Rows.Add(bulunanUrun.UrunKodu, bulunanUrun.Aciklama, 1, bulunanUrun.Barkod);
                    }
                }

                // Kutuyu anında temizle ve odaklan
                txtBarkodOkut.Clear();
                txtBarkodOkut.Focus();
            }
        }

        private void btnUretimKaydet_Click(object sender, EventArgs e)
        {
            if (dgvUretim.Rows.Count == 0) { MessageBox.Show("Tabloda okutulmuş ürün yok!"); return; }

            DateTime secilenTarih = dtpUretimTarihi.Value;

            string anaYol = string.IsNullOrWhiteSpace(txtKayitYeri.Text)
                ? System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Günlük Üretim Takip")
                : txtKayitYeri.Text;

            string yil = secilenTarih.ToString("yyyy");
            string ay = secilenTarih.ToString("MMMM");
            string gun = secilenTarih.ToString("dd-MM-yyyy");

            string tamHedefKlasor = Path.Combine(anaYol, yil, ay, gun);
            if (!Directory.Exists(tamHedefKlasor)) Directory.CreateDirectory(tamHedefKlasor);

            string dosyaAdi = $"Uretim_{DateTime.Now.ToString("HHmmss")}.csv";
            string dosyaYolu = Path.Combine(tamHedefKlasor, dosyaAdi);

            using (StreamWriter sw = new StreamWriter(dosyaYolu, false, System.Text.Encoding.UTF8))
            {
                sw.WriteLine("Ürün Kodu;Ürün Açıklaması;Ürün Adeti;Ürün Barkodu");
                foreach (DataGridViewRow row in dgvUretim.Rows)
                {
                    if (row.Cells[0].Value != null)
                    {
                        sw.WriteLine($"{row.Cells[0].Value};{row.Cells[1].Value};{row.Cells[2].Value};{row.Cells[3].Value}");
                    }
                }
            }

            if (chkYazdir != null && chkYazdir.Checked)
            {
                UretimListesiYazdir(secilenTarih);
            }

            MessageBox.Show($"Üretim verileri başarıyla kaydedildi!\nYol: {dosyaYolu}", "Kayıt Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);

            dgvUretim.Rows.Clear();
            if (pnlBarkodOkuma != null) pnlBarkodOkuma.Visible = false;
            if (pnlGunSecimi != null) pnlGunSecimi.Visible = true;
        }

        private void btnExcelAktar_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog { Filter = "Excel Dosyası|*.xlsx;*.xls", Title = "Ürün Listesi Seçin" })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string connString = $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={ofd.FileName};Extended Properties=\"Excel 12.0 Xml;HDR=YES;IMEX=1\";";

                        using (System.Data.OleDb.OleDbConnection conn = new System.Data.OleDb.OleDbConnection(connString))
                        {
                            conn.Open();
                            System.Data.DataTable dtExcelSchema = conn.GetOleDbSchemaTable(System.Data.OleDb.OleDbSchemaGuid.Tables, null);

                            string sheetName = "";
                            foreach (System.Data.DataRow schemaRow in dtExcelSchema.Rows)
                            {
                                string tempName = schemaRow["TABLE_NAME"].ToString();
                                if (tempName.EndsWith("$") || tempName.EndsWith("$'"))
                                {
                                    sheetName = tempName;
                                    break;
                                }
                            }
                            if (string.IsNullOrEmpty(sheetName)) sheetName = dtExcelSchema.Rows[0]["TABLE_NAME"].ToString();

                            System.Data.OleDb.OleDbDataAdapter da = new System.Data.OleDb.OleDbDataAdapter($"SELECT * FROM [{sheetName}]", conn);
                            System.Data.DataTable dt = new System.Data.DataTable();
                            da.Fill(dt);

                            int eklenenSayisi = 0;
                            int atlananSayisi = 0;

                            foreach (System.Data.DataRow row in dt.Rows)
                            {
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
                                    Barkod = row.ItemArray.Length > 3 ? row[3].ToString().Trim() : ""
                                };

                                DataAccess.InsertUrun(yeniUrun);
                                eklenenSayisi++;
                            }

                            MessageBox.Show($"İşlem Tamamlandı!\n\nExcel'de Bulunan Toplam Satır: {dt.Rows.Count}\nVeritabanına Başarıyla Eklenen: {eklenenSayisi}\nBoş Olduğu İçin Atlanan: {atlananSayisi}", "Excel Aktarım Raporu", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Excel okunurken veya veritabanına kaydedilirken hata oluştu!\n\nHata Detayı: " + ex.Message, "Kritik Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void UretimListesiYazdir(DateTime secilenTarih)
        {
            pdUretim = new System.Drawing.Printing.PrintDocument();

            if (cmbUretimYazici != null && cmbUretimYazici.SelectedItem != null)
            {
                pdUretim.PrinterSettings.PrinterName = cmbUretimYazici.SelectedItem.ToString();
            }

            pdUretim.PrintPage += PdUretim_PrintPage;
            pdUretim.Print();
        }

        private void PdUretim_PrintPage(object sender, System.Drawing.Printing.PrintPageEventArgs e)
        {
            Graphics g = e.Graphics;
            Font baslikFont = new Font("Arial", 16, FontStyle.Bold);
            Font icerikFont = new Font("Arial", 10);
            Font barkodFont = new Font("Arial", 12, FontStyle.Bold);

            int y = 50;
            string baslik = $"{dtpUretimTarihi.Value.ToString("dd.MM.yyyy")} Mamul Depo Kabul Listesi";

            g.DrawString(baslik, baslikFont, Brushes.Black, 50, y);
            y += 40;

            g.DrawString("KOD", icerikFont, Brushes.Black, 50, y);
            g.DrawString("AÇIKLAMA", icerikFont, Brushes.Black, 150, y);
            g.DrawString("ADET", icerikFont, Brushes.Black, 450, y);
            g.DrawString("BARKOD", icerikFont, Brushes.Black, 520, y);
            y += 20;

            g.DrawLine(Pens.Black, 50, y, 750, y);
            y += 10;

            foreach (DataGridViewRow row in dgvUretim.Rows)
            {
                if (row.IsNewRow) continue;

                string kod = row.Cells[0].Value?.ToString() ?? "";
                string aciklama = row.Cells[1].Value?.ToString() ?? "";
                string adet = row.Cells[2].Value?.ToString() ?? "";
                string barkod = row.Cells[3].Value?.ToString() ?? "";

                if (aciklama.Length > 35) aciklama = aciklama.Substring(0, 35) + "...";

                g.DrawString(kod, icerikFont, Brushes.Black, 50, y);
                g.DrawString(aciklama, icerikFont, Brushes.Black, 150, y);
                g.DrawString(adet, icerikFont, Brushes.Black, 450, y);
                g.DrawString(barkod, barkodFont, Brushes.Black, 520, y);

                y += 30;

                if (y > e.MarginBounds.Bottom)
                {
                    e.HasMorePages = true;
                    return;
                }
            }
            e.HasMorePages = false;
        }

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

            if (dgvBarkodVerileri.Columns["Id"] != null) dgvBarkodVerileri.Columns["Id"].Visible = false;
            if (dgvBarkodVerileri.Columns["UrunKodu"] != null) dgvBarkodVerileri.Columns["UrunKodu"].HeaderText = "Ürün Kodu";
            if (dgvBarkodVerileri.Columns["Aciklama"] != null) dgvBarkodVerileri.Columns["Aciklama"].HeaderText = "Açıklama";
            if (dgvBarkodVerileri.Columns["IngilizceAciklama"] != null) dgvBarkodVerileri.Columns["IngilizceAciklama"].HeaderText = "İngilizce Açıklama";
            if (dgvBarkodVerileri.Columns["Barkod"] != null) dgvBarkodVerileri.Columns["Barkod"].HeaderText = "Barkod (EAN)";
        }

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

        private void btnTumVerileriSil_Click(object sender, EventArgs e)
        {
            DialogResult cevap = MessageBox.Show("Veritabanındaki TÜM ürünler kalıcı olarak silinecek!\nBu işlemi geri alamazsınız. Emin misiniz?", "Kritik Uyarı", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (cevap == DialogResult.Yes)
            {
                DataAccess.DeleteAllUrunler();

                if (dgvBarkodVerileri != null) dgvBarkodVerileri.DataSource = null;

                MessageBox.Show("Veritabanı başarıyla tertemiz yapıldı!", "İşlem Tamam", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void btnSecileniSil_Click(object sender, EventArgs e)
        {
            if (dgvUretim.Rows.Count == 0) return;

            if (dgvUretim.SelectedRows.Count > 0)
            {
                foreach (DataGridViewRow row in dgvUretim.SelectedRows)
                {
                    if (!row.IsNewRow) dgvUretim.Rows.Remove(row);
                }
            }
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

            txtBarkodOkut.Focus();
        }

        #endregion

        // =========================================================================================

        #region 👤 11. KULLANICI YÖNETİMİ (Kullanıcı Ekle/Sil/Listele)

        private void btnKullaniciEkle_Click(object sender, EventArgs e)
        {
            string kAdi = txtYeniKullanici.Text.Trim();
            string sifre = txtYeniSifre.Text.Trim();

            if (string.IsNullOrEmpty(kAdi) || string.IsNullOrEmpty(sifre) || string.IsNullOrWhiteSpace(cmbHesapSuresi.Text) || string.IsNullOrWhiteSpace(cmbSifreYenileme.Text))
            {
                MessageBox.Show("Lütfen Kullanıcı Adı, Şifre, Hesap Süresi ve Şifre Yenileme alanlarının TAMAMINI doldurun!", "Eksik Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            System.Collections.Generic.List<string> seciliYetkiler = new System.Collections.Generic.List<string>();
            foreach (var item in clbYetkiler.CheckedItems) seciliYetkiler.Add(item.ToString());
            string yetkiString = string.Join(",", seciliYetkiler);

            DateTime? bitis = null;
            if (cmbHesapSuresi.Text == "1 Saat") bitis = DateTime.Now.AddHours(1);
            else if (cmbHesapSuresi.Text == "1 Ay") bitis = DateTime.Now.AddMonths(1);
            else if (cmbHesapSuresi.Text == "3 Ay") bitis = DateTime.Now.AddMonths(3);
            else if (cmbHesapSuresi.Text == "6 Ay") bitis = DateTime.Now.AddMonths(6);

            int sifreSure = 0;
            if (cmbSifreYenileme.Text == "1 Ayda Bir") sifreSure = 1;
            else if (cmbSifreYenileme.Text == "3 Ayda Bir") sifreSure = 3;

            Kullanici yeniKullanici = new Kullanici
            {
                KullaniciAdi = kAdi,
                SifreHash = SecurityHelper.HashPassword(sifre),
                Yetkiler = yetkiString,
                BitisTarihi = bitis,
                SonSifreDegistirme = DateTime.Now,
                SifreGecerlilikAyi = sifreSure
            };

            try
            {
                DataAccess.InsertKullanici(yeniKullanici);
                MessageBox.Show($"'{kAdi}' sisteme eklendi!\nYetkileri: {yetkiString}", "İşlem Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtYeniKullanici.Clear();
                txtYeniSifre.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Kayıt hatası! Bu isimde bir kullanıcı zaten olabilir.\n\nSistem Mesajı: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnCikisYap_Click(object sender, EventArgs e)
        {
            Application.Restart();
        }

        private void btnKullaniciListele_Click(object sender, EventArgs e)
        {
            var kullanicilar = DataAccess.GetAllKullanicilar();

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

        private void btnKullaniciSil_Click(object sender, EventArgs e)
        {
            if (dgvKullanicilar.SelectedRows.Count == 0)
            {
                MessageBox.Show("Lütfen tablodan silmek istediğiniz kullanıcıyı (satırı) seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string seciliKullanici = dgvKullanicilar.SelectedRows[0].Cells["Kullanıcı_Adı"].Value.ToString();

            DialogResult cevap = MessageBox.Show($"'{seciliKullanici}' isimli kullanıcıyı SİLMEK istediğinize emin misiniz?", "Kritik Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Error);
            if (cevap == DialogResult.Yes)
            {
                DataAccess.DeleteKullanici(seciliKullanici);
                MessageBox.Show("Kullanıcı başarıyla silindi!", "İşlem Tamam");
                btnKullaniciListele.PerformClick();
            }
        }

        private void btnSifreYenileAdmin_Click(object sender, EventArgs e)
        {
            if (dgvKullanicilar.SelectedRows.Count == 0)
            {
                MessageBox.Show("Lütfen tablodan şifresini sıfırlamak istediğiniz kullanıcıyı seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string seciliKullanici = dgvKullanicilar.SelectedRows[0].Cells["Kullanıcı_Adı"].Value.ToString();

            if (LoginForm.KullaniciKendiSifresiniDegistir(seciliKullanici))
            {
                MessageBox.Show($"'{seciliKullanici}' isimli kullanıcının şifresi başarıyla yenilendi!", "İşlem Tamam", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        #endregion

        // =========================================================================================

        #region 🚛 12. AMBAR ZARFI VE DESİ MOTORU KODLARI

        private void cmbZarfTuru_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbZarfTuru.Text == "Normal Zarf")
            {
                if (pnlNormal != null) pnlNormal.Visible = true;
                if (pnlAmbar != null) pnlAmbar.Visible = false;
            }
            else if (cmbZarfTuru.Text == "Ambar Zarfı")
            {
                if (pnlNormal != null) pnlNormal.Visible = false;
                if (pnlAmbar != null) pnlAmbar.Visible = true;
            }
        }

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
            catch { }
            return 0;
        }

        private void AmbarSisteminiHazirla()
        {
            if (dgvAmbarTumFirmalar == null || dgvAmbarSecilenFirmalar == null || dgvPaletler == null) return;

            // 1. SAĞ TABLO (Seçilenler) SÜTUN AYARLARI
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
            dgvPaletler.Columns[2].Name = "Desi"; dgvPaletler.Columns[2].ReadOnly = true;
            dgvPaletler.AllowUserToAddRows = false;

            // 3. SOL TABLOYU VERİTABANINDAN ÇEK VE DOLDUR
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

            if (dgvAmbarTumFirmalar != null) dgvAmbarTumFirmalar.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            if (dgvAmbarSecilenFirmalar != null) dgvAmbarSecilenFirmalar.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            if (dgvPaletler != null) dgvPaletler.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }

        private void dgvAmbarTumFirmalar_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                if (dgvAmbarSecilenFirmalar.ColumnCount == 0)
                {
                    dgvAmbarSecilenFirmalar.ColumnCount = 6;
                    dgvAmbarSecilenFirmalar.Columns[0].Name = "Id";
                    dgvAmbarSecilenFirmalar.Columns[0].Visible = false;
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

                foreach (DataGridViewRow row in dgvAmbarSecilenFirmalar.Rows)
                {
                    if (row.Cells[0].Value?.ToString() == id)
                    {
                        MessageBox.Show("Bu firma zaten yazdırılacaklar listesinde mevcut!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }

                dgvAmbarSecilenFirmalar.Rows.Add(id, firmaAdi, secilen.Cells[2].Value, secilen.Cells[3].Value, secilen.Cells[4].Value, secilen.Cells[5].Value);
            }
        }

        private void dgvAmbarSecilenFirmalar_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                dgvAmbarSecilenFirmalar.Rows.RemoveAt(e.RowIndex);
            }
        }

        private void cmbPaletSayisi_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (dgvPaletler == null) return;

            if (dgvPaletler.ColumnCount == 0)
            {
                dgvPaletler.ColumnCount = 3;
                dgvPaletler.Columns[0].Name = "Palet No";
                dgvPaletler.Columns[0].ReadOnly = true;
                dgvPaletler.Columns[1].Name = "Ebatlar (En*Boy*Yük)";
                dgvPaletler.Columns[2].Name = "Desi";
                dgvPaletler.Columns[2].ReadOnly = true;
                dgvPaletler.AllowUserToAddRows = false;
            }

            dgvPaletler.Rows.Clear();

            if (int.TryParse(cmbPaletSayisi.Text, out int paletSayisi))
            {
                for (int i = 1; i <= paletSayisi; i++)
                {
                    dgvPaletler.Rows.Add($"{i}. PALET", "", "0 Ds.");
                }
            }
        }

        private void dgvPaletler_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == 1 && e.RowIndex >= 0)
            {
                string ebatMetni = dgvPaletler.Rows[e.RowIndex].Cells[1].Value?.ToString().Trim();
                if (!string.IsNullOrWhiteSpace(ebatMetni))
                {
                    if (ebatMetni.Length == 9 && !ebatMetni.Contains("*"))
                    {
                        ebatMetni = $"{ebatMetni.Substring(0, 3)}*{ebatMetni.Substring(3, 3)}*{ebatMetni.Substring(6, 3)}";
                        dgvPaletler.Rows[e.RowIndex].Cells[1].Value = ebatMetni;
                        return;
                    }

                    double desi = DesiHesapla(ebatMetni);
                    dgvPaletler.Rows[e.RowIndex].Cells[2].Value = Math.Round(desi, 0) + " Ds.";
                }
            }
        }

        private void btnAmbarListeyeEkle_Click(object sender, EventArgs e)
        {
            if (dgvAmbarSonListe == null) return;

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

            if (dgvAmbarSecilenFirmalar.SelectedRows.Count == 0)
            {
                MessageBox.Show("Lütfen ortadaki listeden listeye eklenecek firmayı seçin!", "Uyarı"); return;
            }
            if (dgvPaletler.Rows.Count == 0)
            {
                MessageBox.Show("Lütfen palet sayısı seçip ölçüleri girin!", "Uyarı"); return;
            }

            var firmaRow = dgvAmbarSecilenFirmalar.SelectedRows[0];
            string id = firmaRow.Cells[0].Value?.ToString();
            string fAdi = firmaRow.Cells[1].Value?.ToString();
            string adres = firmaRow.Cells[2].Value?.ToString();
            string il = firmaRow.Cells[3].Value?.ToString();
            string tel1 = firmaRow.Cells[4].Value?.ToString();
            string tel2 = firmaRow.Cells[5].Value?.ToString();

            string paletSayisi = cmbPaletSayisi.Text;
            List<string> olculerListesi = new List<string>();
            double toplamDesi = 0;

            foreach (DataGridViewRow prow in dgvPaletler.Rows)
            {
                string ebat = prow.Cells[1].Value?.ToString() ?? "";
                string desiMetni = prow.Cells[2].Value?.ToString() ?? "0 Ds.";

                olculerListesi.Add($"{ebat} ({desiMetni})");
                toplamDesi += DesiHesapla(ebat);
            }

            string birlesikOlculer = string.Join("\n", olculerListesi);
            string toplamDesiSonucu = Math.Round(toplamDesi, 0).ToString() + " Ds.";

            dgvAmbarSonListe.Rows.Add(id, fAdi, adres, il, tel1, tel2, paletSayisi, birlesikOlculer, toplamDesiSonucu);

            cmbPaletSayisi.SelectedIndex = -1;
            dgvPaletler.Rows.Clear();
        }

        private void btnAmbarSil_Click(object sender, EventArgs e)
        {
            if (dgvAmbarSonListe != null && dgvAmbarSonListe.SelectedRows.Count > 0)
            {
                foreach (DataGridViewRow row in dgvAmbarSonListe.SelectedRows) dgvAmbarSonListe.Rows.Remove(row);
                return;
            }

            if (dgvAmbarSecilenFirmalar != null && dgvAmbarSecilenFirmalar.SelectedRows.Count > 0)
            {
                foreach (DataGridViewRow row in dgvAmbarSecilenFirmalar.SelectedRows) dgvAmbarSecilenFirmalar.Rows.Remove(row);
                return;
            }

            MessageBox.Show("Lütfen silmek istediğiniz satırı seçin.\n(Ana firma listesinden silme yapılamaz, sadece seçilenler ve son liste silinebilir).", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnAmbarYenile_Click(object sender, EventArgs e)
        {
            if (dgvAmbarTumFirmalar == null) return;

            if (dgvAmbarTumFirmalar.ColumnCount == 0)
            {
                dgvAmbarTumFirmalar.ColumnCount = 6;
                dgvAmbarTumFirmalar.Columns[0].Name = "Id";
                dgvAmbarTumFirmalar.Columns[0].Visible = false;
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
            foreach (var f in firmalar)
            {
                dgvAmbarTumFirmalar.Rows.Add(f.Id, f.FirmaAdi, f.Adres, f.Il, f.Telefon1, f.Telefon2);
            }
        }

        // 🚀 TÜM LİSTEYİ ZARFLARA GÖNDERME BUTONU
        // 🚀 TÜM LİSTEYİ ZARFLARA GÖNDERME BUTONU
        private void btnAmbarYazdir_Click(object sender, EventArgs e)
        {
            if (dgvAmbarSonListe.Rows.Count == 0) { MessageBox.Show("Yazdırılacak hiç palet/firma yok!"); return; }

            PrintDocument pd = new PrintDocument();

            // Yazıcıyı seçiyoruz
            ComboBox cmbYazici = this.Controls.Find("cmbAmbarYazici", true).FirstOrDefault() as ComboBox;
            if (cmbYazici != null && cmbYazici.SelectedItem != null)
            {
                pd.PrinterSettings.PrinterName = cmbYazici.SelectedItem.ToString();
            }
            else if (cmbCokluPrinter != null && cmbCokluPrinter.SelectedItem != null)
            {
                pd.PrinterSettings.PrinterName = cmbCokluPrinter.SelectedItem.ToString();
            }

            // 🎯 HATASIZ ÇÖZÜM: PaperKind kullanmadan doğrudan PaperName üzerinden DL zarfı arıyoruz
            PaperSize orijinalDlBoyutu = null;
            try
            {
                foreach (PaperSize kagit in pd.PrinterSettings.PaperSizes)
                {
                    if (kagit.PaperName.ToUpper().Contains("DL"))
                    {
                        orijinalDlBoyutu = kagit;
                        break;
                    }
                }
            }
            catch { }

            if (orijinalDlBoyutu != null)
            {
                pd.DefaultPageSettings.PaperSize = orijinalDlBoyutu;
            }
            else
            {
                pd.DefaultPageSettings.PaperSize = new PaperSize("DL_Zarf", 433, 866);
            }

            pd.DefaultPageSettings.Landscape = true;

            pd.BeginPrint += (s, ev) => { batchIndex = 0; };
            pd.PrintPage += AmbarPrintDocument_PrintPage;

            PrintPreviewDialog ppd = new PrintPreviewDialog { Document = pd };
            try { ppd.ShowDialog(); } catch { }
        }

        private void AmbarPrintDocument_PrintPage(object sender, PrintPageEventArgs e)
        {
            if (batchIndex >= dgvAmbarSonListe.Rows.Count) { e.HasMorePages = false; return; }

            // 🎯 HATASIZ ÇÖZÜM: Burayı da .PaperName kontrolüyle temizledik
            bool dlBulundu = false;
            try
            {
                if (e.PageSettings.PaperSize.PaperName.ToUpper().Contains("DL"))
                {
                    dlBulundu = true;
                }
            }
            catch { }

            if (!dlBulundu)
            {
                e.PageSettings.PaperSize = new PaperSize("DL_Zarf", 433, 866);
            }
            e.PageSettings.Landscape = true;

            var row = dgvAmbarSonListe.Rows[batchIndex];
            string firmaAdi = row.Cells[1].Value?.ToString();
            string adres = row.Cells[2].Value?.ToString();
            string il = row.Cells[3].Value?.ToString();
            string tel1 = row.Cells[4].Value?.ToString();
            string tel2 = row.Cells[5].Value?.ToString();
            string paletSayisi = row.Cells[6].Value?.ToString();
            string olculer = row.Cells[7].Value?.ToString();

            Font baslik = new Font("Arial", 16, FontStyle.Bold);
            Font icerik = new Font("Arial", 12, FontStyle.Bold);
            StringFormat ortala = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

            e.Graphics.PageUnit = GraphicsUnit.Display;

            // =========================================================================================
            // 📐 MİLİMETRİK İNCE AYAR MOTORU
            // =========================================================================================
            int inceAyarX = -45; // 🎯 45 piksel SOLA kaydırdık (Logoya çarpmayacak kadar tatlı bir mesafe)
            int inceAyarY = -35; // 🎯 35 piksel YUKARI kaldırdık (Alttaki cebi tamamen kurtaracak)
                                 // =========================================================================================

            int ustBosluk = 76 + inceAyarY;
            int kutuYukseklik = 280;
            int adresGenişlik = 470;
            int paletGenişlik = 296;
            int solBosluk = 40 + inceAyarX;
            int kutuArasiBosluk = 20;
            int paletSolKoordinat = solBosluk + adresGenişlik + kutuArasiBosluk;
            // =========================================================================================

            // 1. ADRES KUTUSU
            e.Graphics.DrawRectangle(Pens.Black, solBosluk, ustBosluk, adresGenişlik, kutuYukseklik);
            e.Graphics.DrawString("ADRES", baslik, Brushes.Black, new Rectangle(solBosluk, ustBosluk, adresGenişlik, 40), ortala);
            e.Graphics.DrawLine(Pens.Black, solBosluk, ustBosluk + 40, solBosluk + adresGenişlik, ustBosluk + 40);
            e.Graphics.DrawString($"{firmaAdi}\n\n{adres}\n{il}\n{tel1} {tel2}", icerik, Brushes.Black, new Rectangle(solBosluk, ustBosluk + 50, adresGenişlik, kutuYukseklik - 60), ortala);

            // 2. PALET ÖLÇÜLERİ KUTUSU
            e.Graphics.DrawRectangle(Pens.Black, paletSolKoordinat, ustBosluk, paletGenişlik, kutuYukseklik);
            e.Graphics.DrawString("PALET ÖLÇÜLERİ", baslik, Brushes.Black, new Rectangle(paletSolKoordinat, ustBosluk, paletGenişlik, 40), ortala);
            e.Graphics.DrawLine(Pens.Black, paletSolKoordinat, ustBosluk + 40, paletSolKoordinat + paletGenişlik, ustBosluk + 40);
            e.Graphics.DrawString($"{olculer}\n\nTOPLAM: {paletSayisi} PALET", icerik, Brushes.Black, new Rectangle(paletSolKoordinat, ustBosluk + 50, paletGenişlik, kutuYukseklik - 60), ortala);

            batchIndex++;
            e.HasMorePages = (batchIndex < dgvAmbarSonListe.Rows.Count);
        }

        private void btnAmbarAra_Click(object sender, EventArgs e)
        {
            if (dgvAmbarTumFirmalar == null) return;

            string aranan = txtAmbarFirmaAra.Text.Trim().ToLower();
            dgvAmbarTumFirmalar.Rows.Clear();

            var firmalar = DataAccess.GetAllFirmalar();
            foreach (var f in firmalar)
            {
                if (f.FirmaAdi.ToLower().Contains(aranan))
                {
                    dgvAmbarTumFirmalar.Rows.Add(f.Id, f.FirmaAdi, f.Adres, f.Il, f.Telefon1, f.Telefon2);
                }
            }

            if (dgvAmbarTumFirmalar.Rows.Count == 0)
            {
                MessageBox.Show("Aramanıza uygun firma bulunamadı.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        #endregion
    }
}