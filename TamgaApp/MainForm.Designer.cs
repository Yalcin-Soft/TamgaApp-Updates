using System;
using System.Drawing.Printing;
using System.Windows.Forms;

namespace TamgaApp
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;


        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }
        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.tabPage5 = new System.Windows.Forms.TabPage();
            this.panel3 = new System.Windows.Forms.Panel();
            this.btnTumFirmalariSil = new System.Windows.Forms.Button();
            this.label10 = new System.Windows.Forms.Label();
            this.btnSil = new System.Windows.Forms.Button();
            this.lstFirmalar = new System.Windows.Forms.ListBox();
            this.txtEditFirmaAdi = new System.Windows.Forms.TextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.btnListele = new System.Windows.Forms.Button();
            this.label8 = new System.Windows.Forms.Label();
            this.btnGuncelle = new System.Windows.Forms.Button();
            this.label7 = new System.Windows.Forms.Label();
            this.txtEditTel2 = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.txtEditTel1 = new System.Windows.Forms.TextBox();
            this.txtEditAdres = new System.Windows.Forms.TextBox();
            this.txtEditIl = new System.Windows.Forms.TextBox();
            this.dgvFirmalar = new System.Windows.Forms.DataGridView();
            this.Id = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.FirmaAdi = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Adres = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Il = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Telefon1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Telefon2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.tabPage4 = new System.Windows.Forms.TabPage();
            this.btnTopluAktar = new System.Windows.Forms.Button();
            this.txtTel2 = new System.Windows.Forms.TextBox();
            this.txtTel1 = new System.Windows.Forms.TextBox();
            this.txtIl = new System.Windows.Forms.TextBox();
            this.txtAdres = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.btnKaydet = new System.Windows.Forms.Button();
            this.txtFirmaAdi = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.pnlAmbar = new System.Windows.Forms.Panel();
            this.btnAmbarAra = new System.Windows.Forms.Button();
            this.txtAmbarFirmaAra = new System.Windows.Forms.TextBox();
            this.cmbAmbarYazici = new System.Windows.Forms.ComboBox();
            this.label45 = new System.Windows.Forms.Label();
            this.btnAmbarListeyeEkle = new System.Windows.Forms.Button();
            this.dgvAmbarSonListe = new System.Windows.Forms.DataGridView();
            this.btnAmbarSil = new System.Windows.Forms.Button();
            this.btnAmbarYenile = new System.Windows.Forms.Button();
            this.btnAmbarYazdir = new System.Windows.Forms.Button();
            this.dgvPaletler = new System.Windows.Forms.DataGridView();
            this.label43 = new System.Windows.Forms.Label();
            this.cmbPaletSayisi = new System.Windows.Forms.ComboBox();
            this.dgvAmbarSecilenFirmalar = new System.Windows.Forms.DataGridView();
            this.dgvAmbarTumFirmalar = new System.Windows.Forms.DataGridView();
            this.cmbZarfTuru = new System.Windows.Forms.ComboBox();
            this.pnlNormal = new System.Windows.Forms.Panel();
            this.dgvZarfFirmalar = new System.Windows.Forms.DataGridView();
            this.ZarfId = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ZarfFirmaAdi = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ZarfAdres = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ZarfIl = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ZarfTelefon1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ZarfTelefon2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.label11 = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.labell = new System.Windows.Forms.Label();
            this.btnTemizle = new System.Windows.Forms.Button();
            this.cmbCokluPrinter = new System.Windows.Forms.ComboBox();
            this.cmbPrintStyle = new System.Windows.Forms.ComboBox();
            this.lstSecilenFirmalar = new System.Windows.Forms.CheckedListBox();
            this.btnAra = new System.Windows.Forms.Button();
            this.btnCokluZarfYazdir = new System.Windows.Forms.Button();
            this.btnCikar = new System.Windows.Forms.Button();
            this.btnZarfYenile = new System.Windows.Forms.Button();
            this.txtAramaFirmaAdi = new System.Windows.Forms.TextBox();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.btnCikisYap = new System.Windows.Forms.Button();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.TabPage = new System.Windows.Forms.TabPage();
            this.tabPrintSettings = new System.Windows.Forms.TabControl();
            this.tabPage11 = new System.Windows.Forms.TabPage();
            this.pnlProperties = new System.Windows.Forms.Panel();
            this.label35 = new System.Windows.Forms.Label();
            this.cmbAdet = new System.Windows.Forms.ComboBox();
            this.btnApplyProp = new System.Windows.Forms.Button();
            this.label31 = new System.Windows.Forms.Label();
            this.cmbPropRotation = new System.Windows.Forms.ComboBox();
            this.label30 = new System.Windows.Forms.Label();
            this.numPropHmm = new System.Windows.Forms.NumericUpDown();
            this.label29 = new System.Windows.Forms.Label();
            this.numPropWmm = new System.Windows.Forms.NumericUpDown();
            this.label28 = new System.Windows.Forms.Label();
            this.numPropYmm = new System.Windows.Forms.NumericUpDown();
            this.label27 = new System.Windows.Forms.Label();
            this.numPropXmm = new System.Windows.Forms.NumericUpDown();
            this.btnPropColor = new System.Windows.Forms.Button();
            this.label26 = new System.Windows.Forms.Label();
            this.cmbPropAlignment = new System.Windows.Forms.ComboBox();
            this.label25 = new System.Windows.Forms.Label();
            this.label24 = new System.Windows.Forms.Label();
            this.numPropFontSize = new System.Windows.Forms.NumericUpDown();
            this.cmbPropFont = new System.Windows.Forms.ComboBox();
            this.label23 = new System.Windows.Forms.Label();
            this.cmbPropPlaceholder = new System.Windows.Forms.ComboBox();
            this.label21 = new System.Windows.Forms.Label();
            this.txtPropText = new System.Windows.Forms.TextBox();
            this.pnlDesignSurface = new System.Windows.Forms.Panel();
            this.pnlToolbox = new System.Windows.Forms.Panel();
            this.btnTemizleTasarm = new System.Windows.Forms.Button();
            this.btnDeleteTemplate = new System.Windows.Forms.Button();
            this.btnExportSample = new System.Windows.Forms.Button();
            this.btnPrint = new System.Windows.Forms.Button();
            this.btnDeleteItem = new System.Windows.Forms.Button();
            this.btnPreview = new System.Windows.Forms.Button();
            this.btnLoadTemplate = new System.Windows.Forms.Button();
            this.btnSaveTemplate = new System.Windows.Forms.Button();
            this.btnAddImage = new System.Windows.Forms.Button();
            this.btnAddFrame = new System.Windows.Forms.Button();
            this.btnAddField = new System.Windows.Forms.Button();
            this.btnAddLabel = new System.Windows.Forms.Button();
            this.label22 = new System.Windows.Forms.Label();
            this.lstTemplates = new System.Windows.Forms.ListBox();
            this.panel2 = new System.Windows.Forms.Panel();
            this.label20 = new System.Windows.Forms.Label();
            this.numGridMm = new System.Windows.Forms.NumericUpDown();
            this.chkSnapToGrid = new System.Windows.Forms.CheckBox();
            this.rbLandscape = new System.Windows.Forms.RadioButton();
            this.rbPortrait = new System.Windows.Forms.RadioButton();
            this.label19 = new System.Windows.Forms.Label();
            this.txtPageHeightMm = new System.Windows.Forms.TextBox();
            this.label18 = new System.Windows.Forms.Label();
            this.txtPageWidthMm = new System.Windows.Forms.TextBox();
            this.tabPage6 = new System.Windows.Forms.TabPage();
            this.label13 = new System.Windows.Forms.Label();
            this.label12 = new System.Windows.Forms.Label();
            this.btnSavePrinterMapping = new System.Windows.Forms.Button();
            this.cmbPrinters = new System.Windows.Forms.ComboBox();
            this.cmbPrintingPages = new System.Windows.Forms.ComboBox();
            this.tabPage8 = new System.Windows.Forms.TabPage();
            this.btnTumVerileriSil = new System.Windows.Forms.Button();
            this.btnKayitYeriSec = new System.Windows.Forms.Button();
            this.label38 = new System.Windows.Forms.Label();
            this.txtKayitYeri = new System.Windows.Forms.TextBox();
            this.dgvBarkodVerileri = new System.Windows.Forms.DataGridView();
            this.btnBarkodVerileri = new System.Windows.Forms.Button();
            this.btnExcelAktar = new System.Windows.Forms.Button();
            this.tabPage9 = new System.Windows.Forms.TabPage();
            this.btnTumVerileriTemizle = new System.Windows.Forms.Button();
            this.btnSifreYenileAdmin = new System.Windows.Forms.Button();
            this.btnKullaniciSil = new System.Windows.Forms.Button();
            this.btnKullaniciListele = new System.Windows.Forms.Button();
            this.dgvKullanicilar = new System.Windows.Forms.DataGridView();
            this.label42 = new System.Windows.Forms.Label();
            this.cmbSifreYenileme = new System.Windows.Forms.ComboBox();
            this.label41 = new System.Windows.Forms.Label();
            this.cmbHesapSuresi = new System.Windows.Forms.ComboBox();
            this.btnKullaniciEkle = new System.Windows.Forms.Button();
            this.clbYetkiler = new System.Windows.Forms.CheckedListBox();
            this.label40 = new System.Windows.Forms.Label();
            this.txtYeniSifre = new System.Windows.Forms.TextBox();
            this.label39 = new System.Windows.Forms.Label();
            this.txtYeniKullanici = new System.Windows.Forms.TextBox();
            this.tabPage10 = new System.Windows.Forms.TabPage();
            this.btnSqlTemizle = new System.Windows.Forms.Button();
            this.btnSqlKaydet = new System.Windows.Forms.Button();
            this.label48 = new System.Windows.Forms.Label();
            this.txtSqlSifre = new System.Windows.Forms.TextBox();
            this.label47 = new System.Windows.Forms.Label();
            this.txtSqlKullanici = new System.Windows.Forms.TextBox();
            this.label46 = new System.Windows.Forms.Label();
            this.txtSqlVeritabani = new System.Windows.Forms.TextBox();
            this.label44 = new System.Windows.Forms.Label();
            this.txtSqlSunucu = new System.Windows.Forms.TextBox();
            this.tabPage12 = new System.Windows.Forms.TabPage();
            this.label34 = new System.Windows.Forms.Label();
            this.cmbManuelPrinter = new System.Windows.Forms.ComboBox();
            this.label33 = new System.Windows.Forms.Label();
            this.label32 = new System.Windows.Forms.Label();
            this.label17 = new System.Windows.Forms.Label();
            this.label16 = new System.Windows.Forms.Label();
            this.label15 = new System.Windows.Forms.Label();
            this.label14 = new System.Windows.Forms.Label();
            this.btnManuelYazdir = new System.Windows.Forms.Button();
            this.btnManuelOnizle = new System.Windows.Forms.Button();
            this.txtManTel2 = new System.Windows.Forms.TextBox();
            this.txtManTel1 = new System.Windows.Forms.TextBox();
            this.txtManIl = new System.Windows.Forms.TextBox();
            this.txtManAdres = new System.Windows.Forms.TextBox();
            this.txtManFirma = new System.Windows.Forms.TextBox();
            this.cmbManualTemplate = new System.Windows.Forms.ComboBox();
            this.tabPage7 = new System.Windows.Forms.TabPage();
            this.pnlBarkodOkuma = new System.Windows.Forms.Panel();
            this.btnSecileniSil = new System.Windows.Forms.Button();
            this.btnUretimGeri = new System.Windows.Forms.Button();
            this.cmbUretimYazici = new System.Windows.Forms.ComboBox();
            this.btnUretimKaydet = new System.Windows.Forms.Button();
            this.chkYazdir = new System.Windows.Forms.CheckBox();
            this.label37 = new System.Windows.Forms.Label();
            this.label36 = new System.Windows.Forms.Label();
            this.dgvUretim = new System.Windows.Forms.DataGridView();
            this.ÜrünKodu = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ÜrünAçıklaması = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ÜrünAdeti = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ÜrünBarkodu = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.txtBarkodOkut = new System.Windows.Forms.TextBox();
            this.pnlGunSecimi = new System.Windows.Forms.Panel();
            this.btnUretimIleri = new System.Windows.Forms.Button();
            this.dtpUretimTarihi = new System.Windows.Forms.DateTimePicker();
            this.tabPage13 = new System.Windows.Forms.TabPage();
            this.btnKismiSevk = new System.Windows.Forms.Button();
            this.btnTamSevk = new System.Windows.Forms.Button();
            this.btnSevkAra = new System.Windows.Forms.Button();
            this.label51 = new System.Windows.Forms.Label();
            this.txtSevkMusteri = new System.Windows.Forms.TextBox();
            this.label50 = new System.Windows.Forms.Label();
            this.dgvMalzemeler = new System.Windows.Forms.DataGridView();
            this.cmbBelgeNo = new System.Windows.Forms.ComboBox();
            this.btnSiparisYenile = new System.Windows.Forms.Button();
            this.label49 = new System.Windows.Forms.Label();
            this.txtMusteriAdi = new System.Windows.Forms.TextBox();
            this.printPreviewDialog1 = new System.Windows.Forms.PrintPreviewDialog();
            this.backgroundWorker1 = new System.ComponentModel.BackgroundWorker();
            this.printDialog1 = new System.Windows.Forms.PrintDialog();
            this.label56 = new System.Windows.Forms.Label();
            this.txtBarkod = new System.Windows.Forms.TextBox();
            this.tabPage5.SuspendLayout();
            this.panel3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvFirmalar)).BeginInit();
            this.tabPage4.SuspendLayout();
            this.tabPage3.SuspendLayout();
            this.pnlAmbar.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvAmbarSonListe)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvPaletler)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvAmbarSecilenFirmalar)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvAmbarTumFirmalar)).BeginInit();
            this.pnlNormal.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvZarfFirmalar)).BeginInit();
            this.panel1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.TabPage.SuspendLayout();
            this.tabPrintSettings.SuspendLayout();
            this.tabPage11.SuspendLayout();
            this.pnlProperties.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numPropHmm)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numPropWmm)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numPropYmm)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numPropXmm)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numPropFontSize)).BeginInit();
            this.pnlToolbox.SuspendLayout();
            this.panel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numGridMm)).BeginInit();
            this.tabPage6.SuspendLayout();
            this.tabPage8.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvBarkodVerileri)).BeginInit();
            this.tabPage9.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvKullanicilar)).BeginInit();
            this.tabPage10.SuspendLayout();
            this.tabPage12.SuspendLayout();
            this.tabPage7.SuspendLayout();
            this.pnlBarkodOkuma.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvUretim)).BeginInit();
            this.pnlGunSecimi.SuspendLayout();
            this.tabPage13.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvMalzemeler)).BeginInit();
            this.SuspendLayout();
            // 
            // tabPage5
            // 
            this.tabPage5.Controls.Add(this.panel3);
            this.tabPage5.Controls.Add(this.dgvFirmalar);
            this.tabPage5.Location = new System.Drawing.Point(4, 22);
            this.tabPage5.Margin = new System.Windows.Forms.Padding(2);
            this.tabPage5.Name = "tabPage5";
            this.tabPage5.Padding = new System.Windows.Forms.Padding(2);
            this.tabPage5.Size = new System.Drawing.Size(1503, 770);
            this.tabPage5.TabIndex = 4;
            this.tabPage5.Text = "Firma Düzenleme";
            this.tabPage5.UseVisualStyleBackColor = true;
            // 
            // panel3
            // 
            this.panel3.AutoSize = true;
            this.panel3.Controls.Add(this.btnTumFirmalariSil);
            this.panel3.Controls.Add(this.label10);
            this.panel3.Controls.Add(this.btnSil);
            this.panel3.Controls.Add(this.lstFirmalar);
            this.panel3.Controls.Add(this.txtEditFirmaAdi);
            this.panel3.Controls.Add(this.label9);
            this.panel3.Controls.Add(this.btnListele);
            this.panel3.Controls.Add(this.label8);
            this.panel3.Controls.Add(this.btnGuncelle);
            this.panel3.Controls.Add(this.label7);
            this.panel3.Controls.Add(this.txtEditTel2);
            this.panel3.Controls.Add(this.label6);
            this.panel3.Controls.Add(this.txtEditTel1);
            this.panel3.Controls.Add(this.txtEditAdres);
            this.panel3.Controls.Add(this.txtEditIl);
            this.panel3.Dock = System.Windows.Forms.DockStyle.Right;
            this.panel3.Location = new System.Drawing.Point(999, 2);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(502, 766);
            this.panel3.TabIndex = 47;
            // 
            // btnTumFirmalariSil
            // 
            this.btnTumFirmalariSil.BackColor = System.Drawing.Color.DarkRed;
            this.btnTumFirmalariSil.Font = new System.Drawing.Font("Times New Roman", 15.75F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.btnTumFirmalariSil.Location = new System.Drawing.Point(393, 142);
            this.btnTumFirmalariSil.Margin = new System.Windows.Forms.Padding(2);
            this.btnTumFirmalariSil.Name = "btnTumFirmalariSil";
            this.btnTumFirmalariSil.Size = new System.Drawing.Size(104, 100);
            this.btnTumFirmalariSil.TabIndex = 47;
            this.btnTumFirmalariSil.Text = "Tümünü Sil";
            this.btnTumFirmalariSil.UseVisualStyleBackColor = false;
            this.btnTumFirmalariSil.Click += new System.EventHandler(this.btnTumFirmalariSil_Click);
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Font = new System.Drawing.Font("Times New Roman", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.label10.Location = new System.Drawing.Point(9, 13);
            this.label10.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(73, 17);
            this.label10.TabIndex = 33;
            this.label10.Text = "Firma Adı :";
            // 
            // btnSil
            // 
            this.btnSil.BackColor = System.Drawing.Color.Red;
            this.btnSil.Font = new System.Drawing.Font("Times New Roman", 15.75F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.btnSil.Location = new System.Drawing.Point(216, 142);
            this.btnSil.Margin = new System.Windows.Forms.Padding(2);
            this.btnSil.Name = "btnSil";
            this.btnSil.Size = new System.Drawing.Size(104, 100);
            this.btnSil.TabIndex = 46;
            this.btnSil.Text = "Sil";
            this.btnSil.UseVisualStyleBackColor = false;
            this.btnSil.Click += new System.EventHandler(this.btnSil_Click);
            // 
            // lstFirmalar
            // 
            this.lstFirmalar.Font = new System.Drawing.Font("Times New Roman", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.lstFirmalar.FormattingEnabled = true;
            this.lstFirmalar.ItemHeight = 17;
            this.lstFirmalar.Location = new System.Drawing.Point(12, 246);
            this.lstFirmalar.Margin = new System.Windows.Forms.Padding(2);
            this.lstFirmalar.Name = "lstFirmalar";
            this.lstFirmalar.Size = new System.Drawing.Size(488, 497);
            this.lstFirmalar.TabIndex = 0;
            this.lstFirmalar.DoubleClick += new System.EventHandler(this.lstFirmalar_DoubleClick);
            // 
            // txtEditFirmaAdi
            // 
            this.txtEditFirmaAdi.Font = new System.Drawing.Font("Times New Roman", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.txtEditFirmaAdi.Location = new System.Drawing.Point(82, 6);
            this.txtEditFirmaAdi.Margin = new System.Windows.Forms.Padding(2);
            this.txtEditFirmaAdi.Name = "txtEditFirmaAdi";
            this.txtEditFirmaAdi.Size = new System.Drawing.Size(418, 25);
            this.txtEditFirmaAdi.TabIndex = 34;
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Font = new System.Drawing.Font("Times New Roman", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.label9.Location = new System.Drawing.Point(9, 37);
            this.label9.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(51, 17);
            this.label9.TabIndex = 35;
            this.label9.Text = "Adres :";
            // 
            // btnListele
            // 
            this.btnListele.Font = new System.Drawing.Font("Times New Roman", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.btnListele.Location = new System.Drawing.Point(2, 142);
            this.btnListele.Margin = new System.Windows.Forms.Padding(2);
            this.btnListele.Name = "btnListele";
            this.btnListele.Size = new System.Drawing.Size(112, 100);
            this.btnListele.TabIndex = 44;
            this.btnListele.Text = "Tüm Firmaları Listele";
            this.btnListele.UseVisualStyleBackColor = true;
            this.btnListele.Click += new System.EventHandler(this.btnListele_Click);
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Font = new System.Drawing.Font("Times New Roman", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.label8.Location = new System.Drawing.Point(9, 61);
            this.label8.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(23, 17);
            this.label8.TabIndex = 36;
            this.label8.Text = "İl :";
            // 
            // btnGuncelle
            // 
            this.btnGuncelle.Font = new System.Drawing.Font("Times New Roman", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.btnGuncelle.Location = new System.Drawing.Point(118, 142);
            this.btnGuncelle.Margin = new System.Windows.Forms.Padding(2);
            this.btnGuncelle.Name = "btnGuncelle";
            this.btnGuncelle.Size = new System.Drawing.Size(94, 100);
            this.btnGuncelle.TabIndex = 43;
            this.btnGuncelle.Text = "Güncelle";
            this.btnGuncelle.UseVisualStyleBackColor = true;
            this.btnGuncelle.Click += new System.EventHandler(this.btnGuncelle_Click);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Font = new System.Drawing.Font("Times New Roman", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.label7.Location = new System.Drawing.Point(9, 85);
            this.label7.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(77, 17);
            this.label7.TabIndex = 37;
            this.label7.Text = "Telefon 01 :";
            // 
            // txtEditTel2
            // 
            this.txtEditTel2.Font = new System.Drawing.Font("Times New Roman", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.txtEditTel2.Location = new System.Drawing.Point(82, 102);
            this.txtEditTel2.Margin = new System.Windows.Forms.Padding(2);
            this.txtEditTel2.Name = "txtEditTel2";
            this.txtEditTel2.Size = new System.Drawing.Size(418, 25);
            this.txtEditTel2.TabIndex = 42;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font("Times New Roman", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.label6.Location = new System.Drawing.Point(9, 109);
            this.label6.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(77, 17);
            this.label6.TabIndex = 38;
            this.label6.Text = "Telefon 02 :";
            // 
            // txtEditTel1
            // 
            this.txtEditTel1.Font = new System.Drawing.Font("Times New Roman", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.txtEditTel1.Location = new System.Drawing.Point(82, 78);
            this.txtEditTel1.Margin = new System.Windows.Forms.Padding(2);
            this.txtEditTel1.Name = "txtEditTel1";
            this.txtEditTel1.Size = new System.Drawing.Size(418, 25);
            this.txtEditTel1.TabIndex = 41;
            // 
            // txtEditAdres
            // 
            this.txtEditAdres.Font = new System.Drawing.Font("Times New Roman", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.txtEditAdres.Location = new System.Drawing.Point(82, 30);
            this.txtEditAdres.Margin = new System.Windows.Forms.Padding(2);
            this.txtEditAdres.Name = "txtEditAdres";
            this.txtEditAdres.Size = new System.Drawing.Size(418, 25);
            this.txtEditAdres.TabIndex = 39;
            // 
            // txtEditIl
            // 
            this.txtEditIl.Font = new System.Drawing.Font("Times New Roman", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.txtEditIl.Location = new System.Drawing.Point(82, 54);
            this.txtEditIl.Margin = new System.Windows.Forms.Padding(2);
            this.txtEditIl.Name = "txtEditIl";
            this.txtEditIl.Size = new System.Drawing.Size(418, 25);
            this.txtEditIl.TabIndex = 40;
            // 
            // dgvFirmalar
            // 
            this.dgvFirmalar.AllowUserToAddRows = false;
            this.dgvFirmalar.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
            this.dgvFirmalar.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
            this.dgvFirmalar.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.dgvFirmalar.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.Sunken;
            this.dgvFirmalar.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvFirmalar.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Id,
            this.FirmaAdi,
            this.Adres,
            this.Il,
            this.Telefon1,
            this.Telefon2});
            this.dgvFirmalar.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvFirmalar.Location = new System.Drawing.Point(2, 2);
            this.dgvFirmalar.Margin = new System.Windows.Forms.Padding(2);
            this.dgvFirmalar.Name = "dgvFirmalar";
            this.dgvFirmalar.RowHeadersWidth = 51;
            this.dgvFirmalar.RowTemplate.Height = 24;
            this.dgvFirmalar.Size = new System.Drawing.Size(1499, 766);
            this.dgvFirmalar.TabIndex = 45;
            // 
            // Id
            // 
            this.Id.HeaderText = "ID";
            this.Id.MinimumWidth = 6;
            this.Id.Name = "Id";
            this.Id.ReadOnly = true;
            this.Id.Width = 43;
            // 
            // FirmaAdi
            // 
            this.FirmaAdi.HeaderText = "Firma Adı";
            this.FirmaAdi.MinimumWidth = 6;
            this.FirmaAdi.Name = "FirmaAdi";
            this.FirmaAdi.ReadOnly = true;
            this.FirmaAdi.Width = 75;
            // 
            // Adres
            // 
            this.Adres.HeaderText = "Adres";
            this.Adres.MinimumWidth = 6;
            this.Adres.Name = "Adres";
            this.Adres.ReadOnly = true;
            this.Adres.Width = 59;
            // 
            // Il
            // 
            this.Il.HeaderText = "İl";
            this.Il.MinimumWidth = 6;
            this.Il.Name = "Il";
            this.Il.ReadOnly = true;
            this.Il.Width = 37;
            // 
            // Telefon1
            // 
            this.Telefon1.HeaderText = "Telefon1";
            this.Telefon1.MinimumWidth = 6;
            this.Telefon1.Name = "Telefon1";
            this.Telefon1.ReadOnly = true;
            this.Telefon1.Width = 74;
            // 
            // Telefon2
            // 
            this.Telefon2.HeaderText = "Telefon2";
            this.Telefon2.MinimumWidth = 6;
            this.Telefon2.Name = "Telefon2";
            this.Telefon2.ReadOnly = true;
            this.Telefon2.Width = 74;
            // 
            // tabPage4
            // 
            this.tabPage4.AutoScroll = true;
            this.tabPage4.Controls.Add(this.btnTopluAktar);
            this.tabPage4.Controls.Add(this.txtTel2);
            this.tabPage4.Controls.Add(this.txtTel1);
            this.tabPage4.Controls.Add(this.txtIl);
            this.tabPage4.Controls.Add(this.txtAdres);
            this.tabPage4.Controls.Add(this.label5);
            this.tabPage4.Controls.Add(this.label4);
            this.tabPage4.Controls.Add(this.label3);
            this.tabPage4.Controls.Add(this.label2);
            this.tabPage4.Controls.Add(this.btnKaydet);
            this.tabPage4.Controls.Add(this.txtFirmaAdi);
            this.tabPage4.Controls.Add(this.label1);
            this.tabPage4.Location = new System.Drawing.Point(4, 22);
            this.tabPage4.Margin = new System.Windows.Forms.Padding(2);
            this.tabPage4.Name = "tabPage4";
            this.tabPage4.Padding = new System.Windows.Forms.Padding(2);
            this.tabPage4.Size = new System.Drawing.Size(1503, 770);
            this.tabPage4.TabIndex = 3;
            this.tabPage4.Text = "Yeni Firma Ekleme";
            this.tabPage4.UseVisualStyleBackColor = true;
            // 
            // btnTopluAktar
            // 
            this.btnTopluAktar.Location = new System.Drawing.Point(137, 206);
            this.btnTopluAktar.Margin = new System.Windows.Forms.Padding(2);
            this.btnTopluAktar.Name = "btnTopluAktar";
            this.btnTopluAktar.Size = new System.Drawing.Size(305, 114);
            this.btnTopluAktar.TabIndex = 33;
            this.btnTopluAktar.Text = "Excel/CSV\'den Toplu İçe Aktar";
            this.btnTopluAktar.UseVisualStyleBackColor = true;
            this.btnTopluAktar.Click += new System.EventHandler(this.btnTopluAktar_Click);
            // 
            // txtTel2
            // 
            this.txtTel2.Location = new System.Drawing.Point(137, 167);
            this.txtTel2.Margin = new System.Windows.Forms.Padding(2);
            this.txtTel2.Name = "txtTel2";
            this.txtTel2.Size = new System.Drawing.Size(449, 20);
            this.txtTel2.TabIndex = 32;
            // 
            // txtTel1
            // 
            this.txtTel1.Location = new System.Drawing.Point(137, 135);
            this.txtTel1.Margin = new System.Windows.Forms.Padding(2);
            this.txtTel1.Name = "txtTel1";
            this.txtTel1.Size = new System.Drawing.Size(449, 20);
            this.txtTel1.TabIndex = 31;
            // 
            // txtIl
            // 
            this.txtIl.Location = new System.Drawing.Point(137, 111);
            this.txtIl.Margin = new System.Windows.Forms.Padding(2);
            this.txtIl.Name = "txtIl";
            this.txtIl.Size = new System.Drawing.Size(449, 20);
            this.txtIl.TabIndex = 30;
            // 
            // txtAdres
            // 
            this.txtAdres.Location = new System.Drawing.Point(137, 87);
            this.txtAdres.Margin = new System.Windows.Forms.Padding(2);
            this.txtAdres.Name = "txtAdres";
            this.txtAdres.Size = new System.Drawing.Size(449, 20);
            this.txtAdres.TabIndex = 29;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(58, 174);
            this.label5.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(64, 13);
            this.label5.TabIndex = 28;
            this.label5.Text = "Telefon 02 :";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(58, 142);
            this.label4.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(64, 13);
            this.label4.TabIndex = 27;
            this.label4.Text = "Telefon 01 :";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(58, 118);
            this.label3.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(18, 13);
            this.label3.TabIndex = 26;
            this.label3.Text = "İl :";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(58, 94);
            this.label2.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(40, 13);
            this.label2.TabIndex = 25;
            this.label2.Text = "Adres :";
            // 
            // btnKaydet
            // 
            this.btnKaydet.Location = new System.Drawing.Point(446, 206);
            this.btnKaydet.Margin = new System.Windows.Forms.Padding(2);
            this.btnKaydet.Name = "btnKaydet";
            this.btnKaydet.Size = new System.Drawing.Size(122, 114);
            this.btnKaydet.TabIndex = 24;
            this.btnKaydet.Text = "Kaydet";
            this.btnKaydet.UseVisualStyleBackColor = true;
            this.btnKaydet.Click += new System.EventHandler(this.btnKaydet_Click);
            // 
            // txtFirmaAdi
            // 
            this.txtFirmaAdi.Location = new System.Drawing.Point(137, 63);
            this.txtFirmaAdi.Margin = new System.Windows.Forms.Padding(2);
            this.txtFirmaAdi.Name = "txtFirmaAdi";
            this.txtFirmaAdi.Size = new System.Drawing.Size(449, 20);
            this.txtFirmaAdi.TabIndex = 23;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(58, 70);
            this.label1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(56, 13);
            this.label1.TabIndex = 22;
            this.label1.Text = "Firma Adı :";
            // 
            // tabPage3
            // 
            this.tabPage3.Controls.Add(this.pnlAmbar);
            this.tabPage3.Controls.Add(this.cmbZarfTuru);
            this.tabPage3.Controls.Add(this.pnlNormal);
            this.tabPage3.Location = new System.Drawing.Point(4, 22);
            this.tabPage3.Margin = new System.Windows.Forms.Padding(2);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Padding = new System.Windows.Forms.Padding(2);
            this.tabPage3.Size = new System.Drawing.Size(1503, 770);
            this.tabPage3.TabIndex = 2;
            this.tabPage3.Text = "Çoklu Zarf Yazdırma";
            this.tabPage3.UseVisualStyleBackColor = true;
            // 
            // pnlAmbar
            // 
            this.pnlAmbar.Controls.Add(this.btnAmbarAra);
            this.pnlAmbar.Controls.Add(this.txtAmbarFirmaAra);
            this.pnlAmbar.Controls.Add(this.cmbAmbarYazici);
            this.pnlAmbar.Controls.Add(this.label45);
            this.pnlAmbar.Controls.Add(this.btnAmbarListeyeEkle);
            this.pnlAmbar.Controls.Add(this.dgvAmbarSonListe);
            this.pnlAmbar.Controls.Add(this.btnAmbarSil);
            this.pnlAmbar.Controls.Add(this.btnAmbarYenile);
            this.pnlAmbar.Controls.Add(this.btnAmbarYazdir);
            this.pnlAmbar.Controls.Add(this.dgvPaletler);
            this.pnlAmbar.Controls.Add(this.label43);
            this.pnlAmbar.Controls.Add(this.cmbPaletSayisi);
            this.pnlAmbar.Controls.Add(this.dgvAmbarSecilenFirmalar);
            this.pnlAmbar.Controls.Add(this.dgvAmbarTumFirmalar);
            this.pnlAmbar.Location = new System.Drawing.Point(0, 28);
            this.pnlAmbar.Name = "pnlAmbar";
            this.pnlAmbar.Size = new System.Drawing.Size(1498, 742);
            this.pnlAmbar.TabIndex = 60;
            // 
            // btnAmbarAra
            // 
            this.btnAmbarAra.Font = new System.Drawing.Font("Times New Roman", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.btnAmbarAra.Location = new System.Drawing.Point(814, 269);
            this.btnAmbarAra.Name = "btnAmbarAra";
            this.btnAmbarAra.Size = new System.Drawing.Size(150, 40);
            this.btnAmbarAra.TabIndex = 27;
            this.btnAmbarAra.Text = "Firma Ara";
            this.btnAmbarAra.UseVisualStyleBackColor = true;
            this.btnAmbarAra.Click += new System.EventHandler(this.btnAmbarAra_Click);
            // 
            // txtAmbarFirmaAra
            // 
            this.txtAmbarFirmaAra.Location = new System.Drawing.Point(814, 245);
            this.txtAmbarFirmaAra.Name = "txtAmbarFirmaAra";
            this.txtAmbarFirmaAra.Size = new System.Drawing.Size(265, 20);
            this.txtAmbarFirmaAra.TabIndex = 26;
            // 
            // cmbAmbarYazici
            // 
            this.cmbAmbarYazici.FormattingEnabled = true;
            this.cmbAmbarYazici.Location = new System.Drawing.Point(893, 388);
            this.cmbAmbarYazici.Name = "cmbAmbarYazici";
            this.cmbAmbarYazici.Size = new System.Drawing.Size(186, 21);
            this.cmbAmbarYazici.TabIndex = 25;
            // 
            // label45
            // 
            this.label45.AutoSize = true;
            this.label45.Font = new System.Drawing.Font("Times New Roman", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.label45.Location = new System.Drawing.Point(811, 394);
            this.label45.Name = "label45";
            this.label45.Size = new System.Drawing.Size(67, 15);
            this.label45.TabIndex = 24;
            this.label45.Text = "Yazıcı Seç:";
            // 
            // btnAmbarListeyeEkle
            // 
            this.btnAmbarListeyeEkle.Font = new System.Drawing.Font("Times New Roman", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.btnAmbarListeyeEkle.Location = new System.Drawing.Point(814, 460);
            this.btnAmbarListeyeEkle.Name = "btnAmbarListeyeEkle";
            this.btnAmbarListeyeEkle.Size = new System.Drawing.Size(265, 37);
            this.btnAmbarListeyeEkle.TabIndex = 23;
            this.btnAmbarListeyeEkle.Text = "Listeye Ekle";
            this.btnAmbarListeyeEkle.UseVisualStyleBackColor = true;
            // 
            // dgvAmbarSonListe
            // 
            this.dgvAmbarSonListe.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvAmbarSonListe.Location = new System.Drawing.Point(814, 503);
            this.dgvAmbarSonListe.Name = "dgvAmbarSonListe";
            this.dgvAmbarSonListe.Size = new System.Drawing.Size(682, 239);
            this.dgvAmbarSonListe.TabIndex = 22;
            // 
            // btnAmbarSil
            // 
            this.btnAmbarSil.Font = new System.Drawing.Font("Times New Roman", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.btnAmbarSil.Location = new System.Drawing.Point(814, 418);
            this.btnAmbarSil.Name = "btnAmbarSil";
            this.btnAmbarSil.Size = new System.Drawing.Size(122, 37);
            this.btnAmbarSil.TabIndex = 21;
            this.btnAmbarSil.Text = "Seçileni Sil";
            this.btnAmbarSil.UseVisualStyleBackColor = true;
            // 
            // btnAmbarYenile
            // 
            this.btnAmbarYenile.Font = new System.Drawing.Font("Times New Roman", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.btnAmbarYenile.Location = new System.Drawing.Point(970, 269);
            this.btnAmbarYenile.Name = "btnAmbarYenile";
            this.btnAmbarYenile.Size = new System.Drawing.Size(109, 40);
            this.btnAmbarYenile.TabIndex = 20;
            this.btnAmbarYenile.Text = "Yenile";
            this.btnAmbarYenile.UseVisualStyleBackColor = true;
            this.btnAmbarYenile.Click += new System.EventHandler(this.btnAmbarYenile_Click);
            // 
            // btnAmbarYazdir
            // 
            this.btnAmbarYazdir.Font = new System.Drawing.Font("Times New Roman", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.btnAmbarYazdir.Location = new System.Drawing.Point(942, 418);
            this.btnAmbarYazdir.Name = "btnAmbarYazdir";
            this.btnAmbarYazdir.Size = new System.Drawing.Size(137, 37);
            this.btnAmbarYazdir.TabIndex = 18;
            this.btnAmbarYazdir.Text = "Yazdır";
            this.btnAmbarYazdir.UseVisualStyleBackColor = true;
            this.btnAmbarYazdir.Click += new System.EventHandler(this.btnAmbarYazdir_Click);
            // 
            // dgvPaletler
            // 
            this.dgvPaletler.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvPaletler.Location = new System.Drawing.Point(1085, 245);
            this.dgvPaletler.Name = "dgvPaletler";
            this.dgvPaletler.Size = new System.Drawing.Size(410, 252);
            this.dgvPaletler.TabIndex = 10;
            // 
            // label43
            // 
            this.label43.AutoSize = true;
            this.label43.Font = new System.Drawing.Font("Times New Roman", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.label43.Location = new System.Drawing.Point(811, 367);
            this.label43.Name = "label43";
            this.label43.Size = new System.Drawing.Size(76, 15);
            this.label43.TabIndex = 9;
            this.label43.Text = "Palet Sayısı:";
            // 
            // cmbPaletSayisi
            // 
            this.cmbPaletSayisi.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbPaletSayisi.FormattingEnabled = true;
            this.cmbPaletSayisi.Items.AddRange(new object[] {
            "1",
            "2",
            "3",
            "4",
            "5"});
            this.cmbPaletSayisi.Location = new System.Drawing.Point(893, 361);
            this.cmbPaletSayisi.Name = "cmbPaletSayisi";
            this.cmbPaletSayisi.Size = new System.Drawing.Size(186, 21);
            this.cmbPaletSayisi.TabIndex = 2;
            // 
            // dgvAmbarSecilenFirmalar
            // 
            this.dgvAmbarSecilenFirmalar.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvAmbarSecilenFirmalar.Location = new System.Drawing.Point(814, 0);
            this.dgvAmbarSecilenFirmalar.Name = "dgvAmbarSecilenFirmalar";
            this.dgvAmbarSecilenFirmalar.Size = new System.Drawing.Size(683, 239);
            this.dgvAmbarSecilenFirmalar.TabIndex = 1;
            // 
            // dgvAmbarTumFirmalar
            // 
            this.dgvAmbarTumFirmalar.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvAmbarTumFirmalar.Location = new System.Drawing.Point(0, 0);
            this.dgvAmbarTumFirmalar.Name = "dgvAmbarTumFirmalar";
            this.dgvAmbarTumFirmalar.Size = new System.Drawing.Size(808, 741);
            this.dgvAmbarTumFirmalar.TabIndex = 0;
            // 
            // cmbZarfTuru
            // 
            this.cmbZarfTuru.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbZarfTuru.FormattingEnabled = true;
            this.cmbZarfTuru.Items.AddRange(new object[] {
            "Normal Zarf",
            "Ambar Zarfı"});
            this.cmbZarfTuru.Location = new System.Drawing.Point(1316, 4);
            this.cmbZarfTuru.Margin = new System.Windows.Forms.Padding(2);
            this.cmbZarfTuru.Name = "cmbZarfTuru";
            this.cmbZarfTuru.Size = new System.Drawing.Size(171, 21);
            this.cmbZarfTuru.TabIndex = 59;
            this.cmbZarfTuru.SelectedIndexChanged += new System.EventHandler(this.cmbZarfTuru_SelectedIndexChanged);
            // 
            // pnlNormal
            // 
            this.pnlNormal.Controls.Add(this.dgvZarfFirmalar);
            this.pnlNormal.Controls.Add(this.label11);
            this.pnlNormal.Controls.Add(this.panel1);
            this.pnlNormal.Location = new System.Drawing.Point(0, 28);
            this.pnlNormal.Name = "pnlNormal";
            this.pnlNormal.Size = new System.Drawing.Size(1500, 741);
            this.pnlNormal.TabIndex = 58;
            // 
            // dgvZarfFirmalar
            // 
            this.dgvZarfFirmalar.AllowUserToAddRows = false;
            this.dgvZarfFirmalar.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
            this.dgvZarfFirmalar.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvZarfFirmalar.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.ZarfId,
            this.ZarfFirmaAdi,
            this.ZarfAdres,
            this.ZarfIl,
            this.ZarfTelefon1,
            this.ZarfTelefon2});
            this.dgvZarfFirmalar.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvZarfFirmalar.Location = new System.Drawing.Point(0, 0);
            this.dgvZarfFirmalar.Margin = new System.Windows.Forms.Padding(2);
            this.dgvZarfFirmalar.Name = "dgvZarfFirmalar";
            this.dgvZarfFirmalar.RowHeadersWidth = 51;
            this.dgvZarfFirmalar.RowTemplate.Height = 24;
            this.dgvZarfFirmalar.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvZarfFirmalar.Size = new System.Drawing.Size(1311, 741);
            this.dgvZarfFirmalar.TabIndex = 59;
            // 
            // ZarfId
            // 
            this.ZarfId.HeaderText = "ID";
            this.ZarfId.MinimumWidth = 6;
            this.ZarfId.Name = "ZarfId";
            this.ZarfId.ReadOnly = true;
            this.ZarfId.Width = 43;
            // 
            // ZarfFirmaAdi
            // 
            this.ZarfFirmaAdi.HeaderText = "Firma Adı";
            this.ZarfFirmaAdi.MinimumWidth = 6;
            this.ZarfFirmaAdi.Name = "ZarfFirmaAdi";
            this.ZarfFirmaAdi.ReadOnly = true;
            this.ZarfFirmaAdi.Width = 75;
            // 
            // ZarfAdres
            // 
            this.ZarfAdres.HeaderText = "Adres";
            this.ZarfAdres.MinimumWidth = 6;
            this.ZarfAdres.Name = "ZarfAdres";
            this.ZarfAdres.ReadOnly = true;
            this.ZarfAdres.Width = 59;
            // 
            // ZarfIl
            // 
            this.ZarfIl.HeaderText = "İl";
            this.ZarfIl.MinimumWidth = 6;
            this.ZarfIl.Name = "ZarfIl";
            this.ZarfIl.ReadOnly = true;
            this.ZarfIl.Width = 37;
            // 
            // ZarfTelefon1
            // 
            this.ZarfTelefon1.HeaderText = "Telefon1";
            this.ZarfTelefon1.MinimumWidth = 6;
            this.ZarfTelefon1.Name = "ZarfTelefon1";
            this.ZarfTelefon1.ReadOnly = true;
            this.ZarfTelefon1.Width = 74;
            // 
            // ZarfTelefon2
            // 
            this.ZarfTelefon2.HeaderText = "Telefon2";
            this.ZarfTelefon2.MinimumWidth = 6;
            this.ZarfTelefon2.Name = "ZarfTelefon2";
            this.ZarfTelefon2.ReadOnly = true;
            this.ZarfTelefon2.Width = 74;
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Font = new System.Drawing.Font("Times New Roman", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.label11.Location = new System.Drawing.Point(82, 186);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(66, 22);
            this.label11.TabIndex = 68;
            this.label11.Text = "Şablon";
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.labell);
            this.panel1.Controls.Add(this.btnTemizle);
            this.panel1.Controls.Add(this.cmbCokluPrinter);
            this.panel1.Controls.Add(this.cmbPrintStyle);
            this.panel1.Controls.Add(this.lstSecilenFirmalar);
            this.panel1.Controls.Add(this.btnAra);
            this.panel1.Controls.Add(this.btnCokluZarfYazdir);
            this.panel1.Controls.Add(this.btnCikar);
            this.panel1.Controls.Add(this.btnZarfYenile);
            this.panel1.Controls.Add(this.txtAramaFirmaAdi);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Right;
            this.panel1.Location = new System.Drawing.Point(1311, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(189, 741);
            this.panel1.TabIndex = 0;
            // 
            // labell
            // 
            this.labell.AutoSize = true;
            this.labell.Font = new System.Drawing.Font("Times New Roman", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.labell.Location = new System.Drawing.Point(33, 232);
            this.labell.Name = "labell";
            this.labell.Size = new System.Drawing.Size(112, 22);
            this.labell.TabIndex = 70;
            this.labell.Text = "Yazıcı Seçim";
            // 
            // btnTemizle
            // 
            this.btnTemizle.Font = new System.Drawing.Font("Times New Roman", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.btnTemizle.Location = new System.Drawing.Point(4, 369);
            this.btnTemizle.Margin = new System.Windows.Forms.Padding(2);
            this.btnTemizle.Name = "btnTemizle";
            this.btnTemizle.Size = new System.Drawing.Size(180, 57);
            this.btnTemizle.TabIndex = 66;
            this.btnTemizle.Text = "Seçilenleri Temizle";
            this.btnTemizle.UseVisualStyleBackColor = true;
            // 
            // cmbCokluPrinter
            // 
            this.cmbCokluPrinter.FormattingEnabled = true;
            this.cmbCokluPrinter.Location = new System.Drawing.Point(5, 260);
            this.cmbCokluPrinter.Margin = new System.Windows.Forms.Padding(2);
            this.cmbCokluPrinter.Name = "cmbCokluPrinter";
            this.cmbCokluPrinter.Size = new System.Drawing.Size(180, 21);
            this.cmbCokluPrinter.TabIndex = 69;
            // 
            // cmbPrintStyle
            // 
            this.cmbPrintStyle.FormattingEnabled = true;
            this.cmbPrintStyle.Location = new System.Drawing.Point(4, 207);
            this.cmbPrintStyle.Margin = new System.Windows.Forms.Padding(2);
            this.cmbPrintStyle.Name = "cmbPrintStyle";
            this.cmbPrintStyle.Size = new System.Drawing.Size(180, 21);
            this.cmbPrintStyle.TabIndex = 64;
            // 
            // lstSecilenFirmalar
            // 
            this.lstSecilenFirmalar.FormattingEnabled = true;
            this.lstSecilenFirmalar.Location = new System.Drawing.Point(5, 431);
            this.lstSecilenFirmalar.Margin = new System.Windows.Forms.Padding(2);
            this.lstSecilenFirmalar.Name = "lstSecilenFirmalar";
            this.lstSecilenFirmalar.Size = new System.Drawing.Size(181, 304);
            this.lstSecilenFirmalar.TabIndex = 67;
            // 
            // btnAra
            // 
            this.btnAra.Font = new System.Drawing.Font("Times New Roman", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.btnAra.Location = new System.Drawing.Point(4, 138);
            this.btnAra.Margin = new System.Windows.Forms.Padding(2);
            this.btnAra.Name = "btnAra";
            this.btnAra.Size = new System.Drawing.Size(180, 33);
            this.btnAra.TabIndex = 63;
            this.btnAra.Text = "Ara";
            this.btnAra.UseVisualStyleBackColor = true;
            // 
            // btnCokluZarfYazdir
            // 
            this.btnCokluZarfYazdir.Font = new System.Drawing.Font("Times New Roman", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.btnCokluZarfYazdir.Location = new System.Drawing.Point(4, 286);
            this.btnCokluZarfYazdir.Margin = new System.Windows.Forms.Padding(2);
            this.btnCokluZarfYazdir.Name = "btnCokluZarfYazdir";
            this.btnCokluZarfYazdir.Size = new System.Drawing.Size(180, 36);
            this.btnCokluZarfYazdir.TabIndex = 60;
            this.btnCokluZarfYazdir.Text = "Seçilenleri Yazdır";
            this.btnCokluZarfYazdir.UseVisualStyleBackColor = true;
            // 
            // btnCikar
            // 
            this.btnCikar.Font = new System.Drawing.Font("Times New Roman", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.btnCikar.Location = new System.Drawing.Point(4, 326);
            this.btnCikar.Margin = new System.Windows.Forms.Padding(2);
            this.btnCikar.Name = "btnCikar";
            this.btnCikar.Size = new System.Drawing.Size(180, 36);
            this.btnCikar.TabIndex = 65;
            this.btnCikar.Text = "Seçileni Çıkar";
            this.btnCikar.UseVisualStyleBackColor = true;
            // 
            // btnZarfYenile
            // 
            this.btnZarfYenile.Font = new System.Drawing.Font("Times New Roman", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.btnZarfYenile.Location = new System.Drawing.Point(5, 53);
            this.btnZarfYenile.Margin = new System.Windows.Forms.Padding(2);
            this.btnZarfYenile.Name = "btnZarfYenile";
            this.btnZarfYenile.Size = new System.Drawing.Size(180, 57);
            this.btnZarfYenile.TabIndex = 61;
            this.btnZarfYenile.Text = "Yenile";
            this.btnZarfYenile.UseVisualStyleBackColor = true;
            // 
            // txtAramaFirmaAdi
            // 
            this.txtAramaFirmaAdi.Location = new System.Drawing.Point(4, 114);
            this.txtAramaFirmaAdi.Margin = new System.Windows.Forms.Padding(2);
            this.txtAramaFirmaAdi.Name = "txtAramaFirmaAdi";
            this.txtAramaFirmaAdi.Size = new System.Drawing.Size(180, 20);
            this.txtAramaFirmaAdi.TabIndex = 62;
            // 
            // tabPage2
            // 
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Margin = new System.Windows.Forms.Padding(2);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(2);
            this.tabPage2.Size = new System.Drawing.Size(1503, 770);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Zarf Yazdırma";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.btnCikisYap);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Margin = new System.Windows.Forms.Padding(2);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(2);
            this.tabPage1.Size = new System.Drawing.Size(1503, 770);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Ana Panel";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // btnCikisYap
            // 
            this.btnCikisYap.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCikisYap.BackColor = System.Drawing.Color.Red;
            this.btnCikisYap.Font = new System.Drawing.Font("Times New Roman", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.btnCikisYap.Location = new System.Drawing.Point(443, 347);
            this.btnCikisYap.Name = "btnCikisYap";
            this.btnCikisYap.Size = new System.Drawing.Size(285, 122);
            this.btnCikisYap.TabIndex = 17;
            this.btnCikisYap.Text = "Çıkış Yap / Kapat";
            this.btnCikisYap.UseVisualStyleBackColor = false;
            this.btnCikisYap.Click += new System.EventHandler(this.btnCikisYap_Click);
            // 
            // tabControl1
            // 
            this.tabControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Controls.Add(this.tabPage3);
            this.tabControl1.Controls.Add(this.tabPage4);
            this.tabControl1.Controls.Add(this.tabPage5);
            this.tabControl1.Controls.Add(this.TabPage);
            this.tabControl1.Controls.Add(this.tabPage12);
            this.tabControl1.Controls.Add(this.tabPage7);
            this.tabControl1.Controls.Add(this.tabPage13);
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Margin = new System.Windows.Forms.Padding(2);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(1511, 796);
            this.tabControl1.TabIndex = 0;
            // 
            // TabPage
            // 
            this.TabPage.AutoScroll = true;
            this.TabPage.Controls.Add(this.tabPrintSettings);
            this.TabPage.Location = new System.Drawing.Point(4, 22);
            this.TabPage.Margin = new System.Windows.Forms.Padding(2);
            this.TabPage.Name = "TabPage";
            this.TabPage.Padding = new System.Windows.Forms.Padding(2);
            this.TabPage.Size = new System.Drawing.Size(1503, 770);
            this.TabPage.TabIndex = 6;
            this.TabPage.Text = "Ayarlar";
            this.TabPage.UseVisualStyleBackColor = true;
            // 
            // tabPrintSettings
            // 
            this.tabPrintSettings.Controls.Add(this.tabPage11);
            this.tabPrintSettings.Controls.Add(this.tabPage6);
            this.tabPrintSettings.Controls.Add(this.tabPage8);
            this.tabPrintSettings.Controls.Add(this.tabPage9);
            this.tabPrintSettings.Controls.Add(this.tabPage10);
            this.tabPrintSettings.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabPrintSettings.Location = new System.Drawing.Point(2, 2);
            this.tabPrintSettings.Margin = new System.Windows.Forms.Padding(2);
            this.tabPrintSettings.Name = "tabPrintSettings";
            this.tabPrintSettings.SelectedIndex = 0;
            this.tabPrintSettings.Size = new System.Drawing.Size(1499, 766);
            this.tabPrintSettings.TabIndex = 0;
            // 
            // tabPage11
            // 
            this.tabPage11.Controls.Add(this.pnlProperties);
            this.tabPage11.Controls.Add(this.pnlDesignSurface);
            this.tabPage11.Controls.Add(this.pnlToolbox);
            this.tabPage11.Controls.Add(this.panel2);
            this.tabPage11.Location = new System.Drawing.Point(4, 22);
            this.tabPage11.Margin = new System.Windows.Forms.Padding(2);
            this.tabPage11.Name = "tabPage11";
            this.tabPage11.Padding = new System.Windows.Forms.Padding(2);
            this.tabPage11.Size = new System.Drawing.Size(1491, 740);
            this.tabPage11.TabIndex = 3;
            this.tabPage11.Text = "Şablon Tasarım";
            this.tabPage11.UseVisualStyleBackColor = true;
            // 
            // pnlProperties
            // 
            this.pnlProperties.BackColor = System.Drawing.Color.Transparent;
            this.pnlProperties.Controls.Add(this.label35);
            this.pnlProperties.Controls.Add(this.cmbAdet);
            this.pnlProperties.Controls.Add(this.btnApplyProp);
            this.pnlProperties.Controls.Add(this.label31);
            this.pnlProperties.Controls.Add(this.cmbPropRotation);
            this.pnlProperties.Controls.Add(this.label30);
            this.pnlProperties.Controls.Add(this.numPropHmm);
            this.pnlProperties.Controls.Add(this.label29);
            this.pnlProperties.Controls.Add(this.numPropWmm);
            this.pnlProperties.Controls.Add(this.label28);
            this.pnlProperties.Controls.Add(this.numPropYmm);
            this.pnlProperties.Controls.Add(this.label27);
            this.pnlProperties.Controls.Add(this.numPropXmm);
            this.pnlProperties.Controls.Add(this.btnPropColor);
            this.pnlProperties.Controls.Add(this.label26);
            this.pnlProperties.Controls.Add(this.cmbPropAlignment);
            this.pnlProperties.Controls.Add(this.label25);
            this.pnlProperties.Controls.Add(this.label24);
            this.pnlProperties.Controls.Add(this.numPropFontSize);
            this.pnlProperties.Controls.Add(this.cmbPropFont);
            this.pnlProperties.Controls.Add(this.label23);
            this.pnlProperties.Controls.Add(this.cmbPropPlaceholder);
            this.pnlProperties.Controls.Add(this.label21);
            this.pnlProperties.Controls.Add(this.txtPropText);
            this.pnlProperties.Dock = System.Windows.Forms.DockStyle.Right;
            this.pnlProperties.Location = new System.Drawing.Point(1384, 51);
            this.pnlProperties.Margin = new System.Windows.Forms.Padding(2);
            this.pnlProperties.Name = "pnlProperties";
            this.pnlProperties.Size = new System.Drawing.Size(105, 687);
            this.pnlProperties.TabIndex = 0;
            // 
            // label35
            // 
            this.label35.AutoSize = true;
            this.label35.Location = new System.Drawing.Point(16, 82);
            this.label35.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label35.Name = "label35";
            this.label35.Size = new System.Drawing.Size(65, 13);
            this.label35.TabIndex = 27;
            this.label35.Text = "Nesne Adeti";
            // 
            // cmbAdet
            // 
            this.cmbAdet.FormattingEnabled = true;
            this.cmbAdet.Items.AddRange(new object[] {
            "1",
            "2",
            "3",
            "4",
            "5"});
            this.cmbAdet.Location = new System.Drawing.Point(4, 99);
            this.cmbAdet.Margin = new System.Windows.Forms.Padding(2);
            this.cmbAdet.Name = "cmbAdet";
            this.cmbAdet.Size = new System.Drawing.Size(92, 21);
            this.cmbAdet.TabIndex = 26;
            // 
            // btnApplyProp
            // 
            this.btnApplyProp.Location = new System.Drawing.Point(4, 457);
            this.btnApplyProp.Margin = new System.Windows.Forms.Padding(2);
            this.btnApplyProp.Name = "btnApplyProp";
            this.btnApplyProp.Size = new System.Drawing.Size(91, 32);
            this.btnApplyProp.TabIndex = 25;
            this.btnApplyProp.Text = "UYGULA";
            this.btnApplyProp.UseVisualStyleBackColor = true;
            this.btnApplyProp.Click += new System.EventHandler(this.BtnApplyProp_Click);
            // 
            // label31
            // 
            this.label31.AutoSize = true;
            this.label31.Location = new System.Drawing.Point(9, 422);
            this.label31.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label31.Name = "label31";
            this.label31.Size = new System.Drawing.Size(52, 13);
            this.label31.TabIndex = 24;
            this.label31.Text = "Rotasyon";
            // 
            // cmbPropRotation
            // 
            this.cmbPropRotation.FormattingEnabled = true;
            this.cmbPropRotation.Location = new System.Drawing.Point(4, 433);
            this.cmbPropRotation.Margin = new System.Windows.Forms.Padding(2);
            this.cmbPropRotation.Name = "cmbPropRotation";
            this.cmbPropRotation.Size = new System.Drawing.Size(92, 21);
            this.cmbPropRotation.TabIndex = 23;
            // 
            // label30
            // 
            this.label30.AutoSize = true;
            this.label30.Location = new System.Drawing.Point(9, 380);
            this.label30.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label30.Name = "label30";
            this.label30.Size = new System.Drawing.Size(72, 13);
            this.label30.TabIndex = 22;
            this.label30.Text = "Yükseklik mm";
            // 
            // numPropHmm
            // 
            this.numPropHmm.Location = new System.Drawing.Point(4, 395);
            this.numPropHmm.Margin = new System.Windows.Forms.Padding(2);
            this.numPropHmm.Name = "numPropHmm";
            this.numPropHmm.Size = new System.Drawing.Size(89, 20);
            this.numPropHmm.TabIndex = 21;
            // 
            // label29
            // 
            this.label29.AutoSize = true;
            this.label29.Location = new System.Drawing.Point(9, 345);
            this.label29.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label29.Name = "label29";
            this.label29.Size = new System.Drawing.Size(63, 13);
            this.label29.TabIndex = 20;
            this.label29.Text = "Genişlik mm";
            // 
            // numPropWmm
            // 
            this.numPropWmm.Location = new System.Drawing.Point(4, 360);
            this.numPropWmm.Margin = new System.Windows.Forms.Padding(2);
            this.numPropWmm.Name = "numPropWmm";
            this.numPropWmm.Size = new System.Drawing.Size(89, 20);
            this.numPropWmm.TabIndex = 19;
            // 
            // label28
            // 
            this.label28.AutoSize = true;
            this.label28.Location = new System.Drawing.Point(9, 310);
            this.label28.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label28.Name = "label28";
            this.label28.Size = new System.Drawing.Size(74, 13);
            this.label28.TabIndex = 18;
            this.label28.Text = "Y konumu mm";
            // 
            // numPropYmm
            // 
            this.numPropYmm.Location = new System.Drawing.Point(4, 325);
            this.numPropYmm.Margin = new System.Windows.Forms.Padding(2);
            this.numPropYmm.Name = "numPropYmm";
            this.numPropYmm.Size = new System.Drawing.Size(89, 20);
            this.numPropYmm.TabIndex = 17;
            // 
            // label27
            // 
            this.label27.AutoSize = true;
            this.label27.Location = new System.Drawing.Point(9, 275);
            this.label27.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label27.Name = "label27";
            this.label27.Size = new System.Drawing.Size(74, 13);
            this.label27.TabIndex = 16;
            this.label27.Text = "X konumu mm";
            // 
            // numPropXmm
            // 
            this.numPropXmm.Location = new System.Drawing.Point(4, 290);
            this.numPropXmm.Margin = new System.Windows.Forms.Padding(2);
            this.numPropXmm.Name = "numPropXmm";
            this.numPropXmm.Size = new System.Drawing.Size(89, 20);
            this.numPropXmm.TabIndex = 15;
            // 
            // btnPropColor
            // 
            this.btnPropColor.Location = new System.Drawing.Point(4, 242);
            this.btnPropColor.Margin = new System.Windows.Forms.Padding(2);
            this.btnPropColor.Name = "btnPropColor";
            this.btnPropColor.Size = new System.Drawing.Size(91, 32);
            this.btnPropColor.TabIndex = 6;
            this.btnPropColor.Text = "Renk seçici";
            this.btnPropColor.UseVisualStyleBackColor = true;
            // 
            // label26
            // 
            this.label26.AutoSize = true;
            this.label26.Location = new System.Drawing.Point(26, 201);
            this.label26.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label26.Name = "label26";
            this.label26.Size = new System.Drawing.Size(50, 13);
            this.label26.TabIndex = 14;
            this.label26.Text = "Hizalama";
            // 
            // cmbPropAlignment
            // 
            this.cmbPropAlignment.FormattingEnabled = true;
            this.cmbPropAlignment.Location = new System.Drawing.Point(4, 218);
            this.cmbPropAlignment.Margin = new System.Windows.Forms.Padding(2);
            this.cmbPropAlignment.Name = "cmbPropAlignment";
            this.cmbPropAlignment.Size = new System.Drawing.Size(92, 21);
            this.cmbPropAlignment.TabIndex = 13;
            // 
            // label25
            // 
            this.label25.AutoSize = true;
            this.label25.Location = new System.Drawing.Point(9, 166);
            this.label25.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label25.Name = "label25";
            this.label25.Size = new System.Drawing.Size(64, 13);
            this.label25.TabIndex = 12;
            this.label25.Text = "Font Boyutu";
            // 
            // label24
            // 
            this.label24.AutoSize = true;
            this.label24.Location = new System.Drawing.Point(9, 124);
            this.label24.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label24.Name = "label24";
            this.label24.Size = new System.Drawing.Size(75, 13);
            this.label24.TabIndex = 10;
            this.label24.Text = "Sistem Fontları";
            // 
            // numPropFontSize
            // 
            this.numPropFontSize.Location = new System.Drawing.Point(4, 181);
            this.numPropFontSize.Margin = new System.Windows.Forms.Padding(2);
            this.numPropFontSize.Name = "numPropFontSize";
            this.numPropFontSize.Size = new System.Drawing.Size(89, 20);
            this.numPropFontSize.TabIndex = 11;
            // 
            // cmbPropFont
            // 
            this.cmbPropFont.FormattingEnabled = true;
            this.cmbPropFont.Location = new System.Drawing.Point(4, 141);
            this.cmbPropFont.Margin = new System.Windows.Forms.Padding(2);
            this.cmbPropFont.Name = "cmbPropFont";
            this.cmbPropFont.Size = new System.Drawing.Size(92, 21);
            this.cmbPropFont.TabIndex = 9;
            // 
            // label23
            // 
            this.label23.AutoSize = true;
            this.label23.Location = new System.Drawing.Point(12, 40);
            this.label23.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label23.Name = "label23";
            this.label23.Size = new System.Drawing.Size(72, 13);
            this.label23.TabIndex = 8;
            this.label23.Text = "Nesne Seçimi";
            // 
            // cmbPropPlaceholder
            // 
            this.cmbPropPlaceholder.FormattingEnabled = true;
            this.cmbPropPlaceholder.Location = new System.Drawing.Point(4, 57);
            this.cmbPropPlaceholder.Margin = new System.Windows.Forms.Padding(2);
            this.cmbPropPlaceholder.Name = "cmbPropPlaceholder";
            this.cmbPropPlaceholder.Size = new System.Drawing.Size(92, 21);
            this.cmbPropPlaceholder.TabIndex = 7;
            // 
            // label21
            // 
            this.label21.AutoSize = true;
            this.label21.Location = new System.Drawing.Point(3, 2);
            this.label21.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label21.Name = "label21";
            this.label21.Size = new System.Drawing.Size(112, 13);
            this.label21.TabIndex = 4;
            this.label21.Text = "Görüntülenecek Metin";
            // 
            // txtPropText
            // 
            this.txtPropText.Location = new System.Drawing.Point(4, 18);
            this.txtPropText.Margin = new System.Windows.Forms.Padding(2);
            this.txtPropText.Name = "txtPropText";
            this.txtPropText.Size = new System.Drawing.Size(90, 20);
            this.txtPropText.TabIndex = 3;
            // 
            // pnlDesignSurface
            // 
            this.pnlDesignSurface.AutoScroll = true;
            this.pnlDesignSurface.BackColor = System.Drawing.Color.Gray;
            this.pnlDesignSurface.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlDesignSurface.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlDesignSurface.Location = new System.Drawing.Point(118, 51);
            this.pnlDesignSurface.Margin = new System.Windows.Forms.Padding(2);
            this.pnlDesignSurface.Name = "pnlDesignSurface";
            this.pnlDesignSurface.Size = new System.Drawing.Size(1371, 687);
            this.pnlDesignSurface.TabIndex = 0;
            // 
            // pnlToolbox
            // 
            this.pnlToolbox.Controls.Add(this.btnTemizleTasarm);
            this.pnlToolbox.Controls.Add(this.btnDeleteTemplate);
            this.pnlToolbox.Controls.Add(this.btnExportSample);
            this.pnlToolbox.Controls.Add(this.btnPrint);
            this.pnlToolbox.Controls.Add(this.btnDeleteItem);
            this.pnlToolbox.Controls.Add(this.btnPreview);
            this.pnlToolbox.Controls.Add(this.btnLoadTemplate);
            this.pnlToolbox.Controls.Add(this.btnSaveTemplate);
            this.pnlToolbox.Controls.Add(this.btnAddImage);
            this.pnlToolbox.Controls.Add(this.btnAddFrame);
            this.pnlToolbox.Controls.Add(this.btnAddField);
            this.pnlToolbox.Controls.Add(this.btnAddLabel);
            this.pnlToolbox.Controls.Add(this.label22);
            this.pnlToolbox.Controls.Add(this.lstTemplates);
            this.pnlToolbox.Dock = System.Windows.Forms.DockStyle.Left;
            this.pnlToolbox.Location = new System.Drawing.Point(2, 51);
            this.pnlToolbox.Margin = new System.Windows.Forms.Padding(2);
            this.pnlToolbox.Name = "pnlToolbox";
            this.pnlToolbox.Size = new System.Drawing.Size(116, 687);
            this.pnlToolbox.TabIndex = 0;
            // 
            // btnTemizleTasarm
            // 
            this.btnTemizleTasarm.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.btnTemizleTasarm.Location = new System.Drawing.Point(4, 201);
            this.btnTemizleTasarm.Margin = new System.Windows.Forms.Padding(2);
            this.btnTemizleTasarm.Name = "btnTemizleTasarm";
            this.btnTemizleTasarm.Size = new System.Drawing.Size(111, 34);
            this.btnTemizleTasarm.TabIndex = 33;
            this.btnTemizleTasarm.Text = "TEMİZLE";
            this.btnTemizleTasarm.UseVisualStyleBackColor = true;
            // 
            // btnDeleteTemplate
            // 
            this.btnDeleteTemplate.Location = new System.Drawing.Point(2, 328);
            this.btnDeleteTemplate.Margin = new System.Windows.Forms.Padding(2);
            this.btnDeleteTemplate.Name = "btnDeleteTemplate";
            this.btnDeleteTemplate.Size = new System.Drawing.Size(111, 34);
            this.btnDeleteTemplate.TabIndex = 32;
            this.btnDeleteTemplate.Text = "Şablon Sil";
            this.btnDeleteTemplate.UseVisualStyleBackColor = true;
            this.btnDeleteTemplate.Click += new System.EventHandler(this.btnDeleteTemplate_Click);
            // 
            // btnExportSample
            // 
            this.btnExportSample.Location = new System.Drawing.Point(2, 445);
            this.btnExportSample.Margin = new System.Windows.Forms.Padding(2);
            this.btnExportSample.Name = "btnExportSample";
            this.btnExportSample.Size = new System.Drawing.Size(111, 34);
            this.btnExportSample.TabIndex = 27;
            this.btnExportSample.UseVisualStyleBackColor = true;
            // 
            // btnPrint
            // 
            this.btnPrint.Location = new System.Drawing.Point(2, 406);
            this.btnPrint.Margin = new System.Windows.Forms.Padding(2);
            this.btnPrint.Name = "btnPrint";
            this.btnPrint.Size = new System.Drawing.Size(111, 34);
            this.btnPrint.TabIndex = 31;
            this.btnPrint.Text = "Yazdır";
            this.btnPrint.UseVisualStyleBackColor = true;
            // 
            // btnDeleteItem
            // 
            this.btnDeleteItem.Location = new System.Drawing.Point(2, 158);
            this.btnDeleteItem.Margin = new System.Windows.Forms.Padding(2);
            this.btnDeleteItem.Name = "btnDeleteItem";
            this.btnDeleteItem.Size = new System.Drawing.Size(111, 34);
            this.btnDeleteItem.TabIndex = 5;
            this.btnDeleteItem.Text = "Seçili öğeyi sil";
            this.btnDeleteItem.UseVisualStyleBackColor = true;
            // 
            // btnPreview
            // 
            this.btnPreview.Location = new System.Drawing.Point(2, 367);
            this.btnPreview.Margin = new System.Windows.Forms.Padding(2);
            this.btnPreview.Name = "btnPreview";
            this.btnPreview.Size = new System.Drawing.Size(111, 34);
            this.btnPreview.TabIndex = 28;
            this.btnPreview.Text = "Önizle";
            this.btnPreview.UseVisualStyleBackColor = true;
            // 
            // btnLoadTemplate
            // 
            this.btnLoadTemplate.Location = new System.Drawing.Point(2, 289);
            this.btnLoadTemplate.Margin = new System.Windows.Forms.Padding(2);
            this.btnLoadTemplate.Name = "btnLoadTemplate";
            this.btnLoadTemplate.Size = new System.Drawing.Size(111, 34);
            this.btnLoadTemplate.TabIndex = 29;
            this.btnLoadTemplate.Text = "Şablon Yükle";
            this.btnLoadTemplate.UseVisualStyleBackColor = true;
            // 
            // btnSaveTemplate
            // 
            this.btnSaveTemplate.Location = new System.Drawing.Point(2, 250);
            this.btnSaveTemplate.Margin = new System.Windows.Forms.Padding(2);
            this.btnSaveTemplate.Name = "btnSaveTemplate";
            this.btnSaveTemplate.Size = new System.Drawing.Size(111, 34);
            this.btnSaveTemplate.TabIndex = 30;
            this.btnSaveTemplate.Text = "Şablon Kaydet";
            this.btnSaveTemplate.UseVisualStyleBackColor = true;
            // 
            // btnAddImage
            // 
            this.btnAddImage.Location = new System.Drawing.Point(2, 119);
            this.btnAddImage.Margin = new System.Windows.Forms.Padding(2);
            this.btnAddImage.Name = "btnAddImage";
            this.btnAddImage.Size = new System.Drawing.Size(111, 34);
            this.btnAddImage.TabIndex = 4;
            this.btnAddImage.Text = "Logo/resim ekle";
            this.btnAddImage.UseVisualStyleBackColor = true;
            // 
            // btnAddFrame
            // 
            this.btnAddFrame.Location = new System.Drawing.Point(2, 80);
            this.btnAddFrame.Margin = new System.Windows.Forms.Padding(2);
            this.btnAddFrame.Name = "btnAddFrame";
            this.btnAddFrame.Size = new System.Drawing.Size(111, 34);
            this.btnAddFrame.TabIndex = 3;
            this.btnAddFrame.Text = "Çerçeve ekle";
            this.btnAddFrame.UseVisualStyleBackColor = true;
            // 
            // btnAddField
            // 
            this.btnAddField.Location = new System.Drawing.Point(2, 41);
            this.btnAddField.Margin = new System.Windows.Forms.Padding(2);
            this.btnAddField.Name = "btnAddField";
            this.btnAddField.Size = new System.Drawing.Size(111, 34);
            this.btnAddField.TabIndex = 2;
            this.btnAddField.Text = "Dinamik alan ekle";
            this.btnAddField.UseVisualStyleBackColor = true;
            // 
            // btnAddLabel
            // 
            this.btnAddLabel.Location = new System.Drawing.Point(2, 2);
            this.btnAddLabel.Margin = new System.Windows.Forms.Padding(2);
            this.btnAddLabel.Name = "btnAddLabel";
            this.btnAddLabel.Size = new System.Drawing.Size(111, 34);
            this.btnAddLabel.TabIndex = 1;
            this.btnAddLabel.Text = "Statik metin ekle";
            this.btnAddLabel.UseVisualStyleBackColor = true;
            // 
            // label22
            // 
            this.label22.AutoSize = true;
            this.label22.Location = new System.Drawing.Point(40, 530);
            this.label22.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label22.Name = "label22";
            this.label22.Size = new System.Drawing.Size(25, 13);
            this.label22.TabIndex = 26;
            this.label22.Text = "İsim";
            // 
            // lstTemplates
            // 
            this.lstTemplates.FormattingEnabled = true;
            this.lstTemplates.Location = new System.Drawing.Point(4, 554);
            this.lstTemplates.Margin = new System.Windows.Forms.Padding(2);
            this.lstTemplates.Name = "lstTemplates";
            this.lstTemplates.Size = new System.Drawing.Size(108, 95);
            this.lstTemplates.TabIndex = 1;
            // 
            // panel2
            // 
            this.panel2.AutoScroll = true;
            this.panel2.Controls.Add(this.label20);
            this.panel2.Controls.Add(this.numGridMm);
            this.panel2.Controls.Add(this.chkSnapToGrid);
            this.panel2.Controls.Add(this.rbLandscape);
            this.panel2.Controls.Add(this.rbPortrait);
            this.panel2.Controls.Add(this.label19);
            this.panel2.Controls.Add(this.txtPageHeightMm);
            this.panel2.Controls.Add(this.label18);
            this.panel2.Controls.Add(this.txtPageWidthMm);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel2.Location = new System.Drawing.Point(2, 2);
            this.panel2.Margin = new System.Windows.Forms.Padding(2);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(1487, 49);
            this.panel2.TabIndex = 5;
            // 
            // label20
            // 
            this.label20.AutoSize = true;
            this.label20.Location = new System.Drawing.Point(368, 8);
            this.label20.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label20.Name = "label20";
            this.label20.Size = new System.Drawing.Size(83, 13);
            this.label20.TabIndex = 10;
            this.label20.Text = "Sayfa yüksekliği";
            // 
            // numGridMm
            // 
            this.numGridMm.Location = new System.Drawing.Point(363, 24);
            this.numGridMm.Margin = new System.Windows.Forms.Padding(2);
            this.numGridMm.Maximum = new decimal(new int[] {
            50,
            0,
            0,
            0});
            this.numGridMm.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numGridMm.Name = "numGridMm";
            this.numGridMm.Size = new System.Drawing.Size(89, 20);
            this.numGridMm.TabIndex = 6;
            this.numGridMm.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.numGridMm.Value = new decimal(new int[] {
            5,
            0,
            0,
            0});
            // 
            // chkSnapToGrid
            // 
            this.chkSnapToGrid.AutoSize = true;
            this.chkSnapToGrid.Location = new System.Drawing.Point(269, 15);
            this.chkSnapToGrid.Margin = new System.Windows.Forms.Padding(2);
            this.chkSnapToGrid.Name = "chkSnapToGrid";
            this.chkSnapToGrid.Size = new System.Drawing.Size(99, 17);
            this.chkSnapToGrid.TabIndex = 9;
            this.chkSnapToGrid.Text = "Izgara ON/OFF";
            this.chkSnapToGrid.UseVisualStyleBackColor = true;
            // 
            // rbLandscape
            // 
            this.rbLandscape.AutoSize = true;
            this.rbLandscape.Location = new System.Drawing.Point(199, 25);
            this.rbLandscape.Margin = new System.Windows.Forms.Padding(2);
            this.rbLandscape.Name = "rbLandscape";
            this.rbLandscape.Size = new System.Drawing.Size(72, 17);
            this.rbLandscape.TabIndex = 8;
            this.rbLandscape.Text = "Yatay yön";
            this.rbLandscape.UseVisualStyleBackColor = true;
            // 
            // rbPortrait
            // 
            this.rbPortrait.AutoSize = true;
            this.rbPortrait.Checked = true;
            this.rbPortrait.Location = new System.Drawing.Point(199, 6);
            this.rbPortrait.Margin = new System.Windows.Forms.Padding(2);
            this.rbPortrait.Name = "rbPortrait";
            this.rbPortrait.Size = new System.Drawing.Size(72, 17);
            this.rbPortrait.TabIndex = 7;
            this.rbPortrait.TabStop = true;
            this.rbPortrait.Text = "Dikey yön";
            this.rbPortrait.UseVisualStyleBackColor = true;
            // 
            // label19
            // 
            this.label19.AutoSize = true;
            this.label19.Location = new System.Drawing.Point(103, 6);
            this.label19.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label19.Name = "label19";
            this.label19.Size = new System.Drawing.Size(83, 13);
            this.label19.TabIndex = 4;
            this.label19.Text = "Sayfa yüksekliği";
            // 
            // txtPageHeightMm
            // 
            this.txtPageHeightMm.Location = new System.Drawing.Point(98, 24);
            this.txtPageHeightMm.Margin = new System.Windows.Forms.Padding(2);
            this.txtPageHeightMm.Name = "txtPageHeightMm";
            this.txtPageHeightMm.Size = new System.Drawing.Size(90, 20);
            this.txtPageHeightMm.TabIndex = 3;
            this.txtPageHeightMm.Text = "110";
            this.txtPageHeightMm.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label18
            // 
            this.label18.AutoSize = true;
            this.label18.Location = new System.Drawing.Point(10, 6);
            this.label18.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label18.Name = "label18";
            this.label18.Size = new System.Drawing.Size(74, 13);
            this.label18.TabIndex = 2;
            this.label18.Text = "Sayfa genişliği";
            // 
            // txtPageWidthMm
            // 
            this.txtPageWidthMm.Location = new System.Drawing.Point(4, 24);
            this.txtPageWidthMm.Margin = new System.Windows.Forms.Padding(2);
            this.txtPageWidthMm.Name = "txtPageWidthMm";
            this.txtPageWidthMm.Size = new System.Drawing.Size(90, 20);
            this.txtPageWidthMm.TabIndex = 1;
            this.txtPageWidthMm.Text = "220";
            this.txtPageWidthMm.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // tabPage6
            // 
            this.tabPage6.Controls.Add(this.label13);
            this.tabPage6.Controls.Add(this.label12);
            this.tabPage6.Controls.Add(this.btnSavePrinterMapping);
            this.tabPage6.Controls.Add(this.cmbPrinters);
            this.tabPage6.Controls.Add(this.cmbPrintingPages);
            this.tabPage6.Location = new System.Drawing.Point(4, 22);
            this.tabPage6.Name = "tabPage6";
            this.tabPage6.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage6.Size = new System.Drawing.Size(1491, 740);
            this.tabPage6.TabIndex = 4;
            this.tabPage6.Text = "Yazdırma Ayarları";
            this.tabPage6.UseVisualStyleBackColor = true;
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Font = new System.Drawing.Font("Times New Roman", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.label13.Location = new System.Drawing.Point(585, 190);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(86, 19);
            this.label13.TabIndex = 4;
            this.label13.Text = "Yazıcı Listesi";
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Font = new System.Drawing.Font("Times New Roman", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.label12.Location = new System.Drawing.Point(348, 190);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(87, 19);
            this.label12.TabIndex = 3;
            this.label12.Text = "Sayfa Seçimi";
            // 
            // btnSavePrinterMapping
            // 
            this.btnSavePrinterMapping.Font = new System.Drawing.Font("Times New Roman", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.btnSavePrinterMapping.Location = new System.Drawing.Point(387, 280);
            this.btnSavePrinterMapping.Name = "btnSavePrinterMapping";
            this.btnSavePrinterMapping.Size = new System.Drawing.Size(254, 72);
            this.btnSavePrinterMapping.TabIndex = 2;
            this.btnSavePrinterMapping.Text = "KAYDET";
            this.btnSavePrinterMapping.UseVisualStyleBackColor = true;
            // 
            // cmbPrinters
            // 
            this.cmbPrinters.FormattingEnabled = true;
            this.cmbPrinters.Location = new System.Drawing.Point(530, 227);
            this.cmbPrinters.Name = "cmbPrinters";
            this.cmbPrinters.Size = new System.Drawing.Size(200, 21);
            this.cmbPrinters.TabIndex = 1;
            // 
            // cmbPrintingPages
            // 
            this.cmbPrintingPages.FormattingEnabled = true;
            this.cmbPrintingPages.Location = new System.Drawing.Point(284, 227);
            this.cmbPrintingPages.Name = "cmbPrintingPages";
            this.cmbPrintingPages.Size = new System.Drawing.Size(200, 21);
            this.cmbPrintingPages.TabIndex = 0;
            // 
            // tabPage8
            // 
            this.tabPage8.Controls.Add(this.btnTumVerileriSil);
            this.tabPage8.Controls.Add(this.btnKayitYeriSec);
            this.tabPage8.Controls.Add(this.label38);
            this.tabPage8.Controls.Add(this.txtKayitYeri);
            this.tabPage8.Controls.Add(this.dgvBarkodVerileri);
            this.tabPage8.Controls.Add(this.btnBarkodVerileri);
            this.tabPage8.Controls.Add(this.btnExcelAktar);
            this.tabPage8.Location = new System.Drawing.Point(4, 22);
            this.tabPage8.Name = "tabPage8";
            this.tabPage8.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage8.Size = new System.Drawing.Size(1491, 740);
            this.tabPage8.TabIndex = 5;
            this.tabPage8.Text = "Barkod Verileri";
            this.tabPage8.UseVisualStyleBackColor = true;
            // 
            // btnTumVerileriSil
            // 
            this.btnTumVerileriSil.BackColor = System.Drawing.Color.Crimson;
            this.btnTumVerileriSil.Font = new System.Drawing.Font("Times New Roman", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.btnTumVerileriSil.ForeColor = System.Drawing.Color.White;
            this.btnTumVerileriSil.Location = new System.Drawing.Point(29, 664);
            this.btnTumVerileriSil.Name = "btnTumVerileriSil";
            this.btnTumVerileriSil.Size = new System.Drawing.Size(245, 37);
            this.btnTumVerileriSil.TabIndex = 13;
            this.btnTumVerileriSil.Text = "Barkod Verilerini Sil";
            this.btnTumVerileriSil.UseVisualStyleBackColor = false;
            this.btnTumVerileriSil.Click += new System.EventHandler(this.btnTumVerileriSil_Click);
            // 
            // btnKayitYeriSec
            // 
            this.btnKayitYeriSec.Font = new System.Drawing.Font("Times New Roman", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.btnKayitYeriSec.Location = new System.Drawing.Point(217, 121);
            this.btnKayitYeriSec.Name = "btnKayitYeriSec";
            this.btnKayitYeriSec.Size = new System.Drawing.Size(107, 37);
            this.btnKayitYeriSec.TabIndex = 12;
            this.btnKayitYeriSec.Text = "Klasör Seç";
            this.btnKayitYeriSec.UseVisualStyleBackColor = true;
            this.btnKayitYeriSec.Click += new System.EventHandler(this.btnKayitYeriSec_Click);
            // 
            // label38
            // 
            this.label38.AutoSize = true;
            this.label38.Font = new System.Drawing.Font("Times New Roman", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.label38.Location = new System.Drawing.Point(27, 135);
            this.label38.Name = "label38";
            this.label38.Size = new System.Drawing.Size(184, 15);
            this.label38.TabIndex = 11;
            this.label38.Text = "Üretim Takip Raporu Kayıt Yeri:";
            // 
            // txtKayitYeri
            // 
            this.txtKayitYeri.Location = new System.Drawing.Point(29, 164);
            this.txtKayitYeri.Name = "txtKayitYeri";
            this.txtKayitYeri.ReadOnly = true;
            this.txtKayitYeri.Size = new System.Drawing.Size(295, 20);
            this.txtKayitYeri.TabIndex = 10;
            // 
            // dgvBarkodVerileri
            // 
            this.dgvBarkodVerileri.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvBarkodVerileri.Location = new System.Drawing.Point(330, 25);
            this.dgvBarkodVerileri.Name = "dgvBarkodVerileri";
            this.dgvBarkodVerileri.Size = new System.Drawing.Size(1155, 709);
            this.dgvBarkodVerileri.TabIndex = 5;
            // 
            // btnBarkodVerileri
            // 
            this.btnBarkodVerileri.BackColor = System.Drawing.Color.Aqua;
            this.btnBarkodVerileri.Font = new System.Drawing.Font("Times New Roman", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.btnBarkodVerileri.ForeColor = System.Drawing.Color.Black;
            this.btnBarkodVerileri.Location = new System.Drawing.Point(29, 68);
            this.btnBarkodVerileri.Name = "btnBarkodVerileri";
            this.btnBarkodVerileri.Size = new System.Drawing.Size(245, 37);
            this.btnBarkodVerileri.TabIndex = 4;
            this.btnBarkodVerileri.Text = "Barkod Verilerini Listele";
            this.btnBarkodVerileri.UseVisualStyleBackColor = false;
            this.btnBarkodVerileri.Click += new System.EventHandler(this.btnBarkodVerileri_Click);
            // 
            // btnExcelAktar
            // 
            this.btnExcelAktar.BackColor = System.Drawing.Color.Lime;
            this.btnExcelAktar.Font = new System.Drawing.Font("Times New Roman", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.btnExcelAktar.ForeColor = System.Drawing.Color.Black;
            this.btnExcelAktar.Location = new System.Drawing.Point(29, 25);
            this.btnExcelAktar.Name = "btnExcelAktar";
            this.btnExcelAktar.Size = new System.Drawing.Size(135, 37);
            this.btnExcelAktar.TabIndex = 3;
            this.btnExcelAktar.Text = "Ürün Aktar";
            this.btnExcelAktar.UseVisualStyleBackColor = false;
            this.btnExcelAktar.Click += new System.EventHandler(this.btnExcelAktar_Click);
            // 
            // tabPage9
            // 
            this.tabPage9.Controls.Add(this.btnTumVerileriTemizle);
            this.tabPage9.Controls.Add(this.btnSifreYenileAdmin);
            this.tabPage9.Controls.Add(this.btnKullaniciSil);
            this.tabPage9.Controls.Add(this.btnKullaniciListele);
            this.tabPage9.Controls.Add(this.dgvKullanicilar);
            this.tabPage9.Controls.Add(this.label42);
            this.tabPage9.Controls.Add(this.cmbSifreYenileme);
            this.tabPage9.Controls.Add(this.label41);
            this.tabPage9.Controls.Add(this.cmbHesapSuresi);
            this.tabPage9.Controls.Add(this.btnKullaniciEkle);
            this.tabPage9.Controls.Add(this.clbYetkiler);
            this.tabPage9.Controls.Add(this.label40);
            this.tabPage9.Controls.Add(this.txtYeniSifre);
            this.tabPage9.Controls.Add(this.label39);
            this.tabPage9.Controls.Add(this.txtYeniKullanici);
            this.tabPage9.Location = new System.Drawing.Point(4, 22);
            this.tabPage9.Name = "tabPage9";
            this.tabPage9.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage9.Size = new System.Drawing.Size(1491, 740);
            this.tabPage9.TabIndex = 6;
            this.tabPage9.Text = "Yönetim";
            this.tabPage9.UseVisualStyleBackColor = true;
            // 
            // btnTumVerileriTemizle
            // 
            this.btnTumVerileriTemizle.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnTumVerileriTemizle.BackColor = System.Drawing.Color.Red;
            this.btnTumVerileriTemizle.Font = new System.Drawing.Font("Times New Roman", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.btnTumVerileriTemizle.Location = new System.Drawing.Point(35, 641);
            this.btnTumVerileriTemizle.Name = "btnTumVerileriTemizle";
            this.btnTumVerileriTemizle.Size = new System.Drawing.Size(363, 37);
            this.btnTumVerileriTemizle.TabIndex = 26;
            this.btnTumVerileriTemizle.Text = "Operasyonel Fabrika Ayarlarına Dön!!!";
            this.btnTumVerileriTemizle.UseVisualStyleBackColor = false;
            this.btnTumVerileriTemizle.Click += new System.EventHandler(this.btnTumVerileriTemizle_Click);
            // 
            // btnSifreYenileAdmin
            // 
            this.btnSifreYenileAdmin.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSifreYenileAdmin.Font = new System.Drawing.Font("Times New Roman", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.btnSifreYenileAdmin.Location = new System.Drawing.Point(676, 510);
            this.btnSifreYenileAdmin.Name = "btnSifreYenileAdmin";
            this.btnSifreYenileAdmin.Size = new System.Drawing.Size(308, 37);
            this.btnSifreYenileAdmin.TabIndex = 25;
            this.btnSifreYenileAdmin.Text = "Seçili Kullanıcının Şifresini Sıfırla";
            this.btnSifreYenileAdmin.UseVisualStyleBackColor = true;
            this.btnSifreYenileAdmin.Click += new System.EventHandler(this.btnSifreYenileAdmin_Click);
            // 
            // btnKullaniciSil
            // 
            this.btnKullaniciSil.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnKullaniciSil.BackColor = System.Drawing.Color.Red;
            this.btnKullaniciSil.Font = new System.Drawing.Font("Times New Roman", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.btnKullaniciSil.Location = new System.Drawing.Point(676, 553);
            this.btnKullaniciSil.Name = "btnKullaniciSil";
            this.btnKullaniciSil.Size = new System.Drawing.Size(191, 37);
            this.btnKullaniciSil.TabIndex = 24;
            this.btnKullaniciSil.Text = "Seçili Kullanıcıyı Sil";
            this.btnKullaniciSil.UseVisualStyleBackColor = false;
            this.btnKullaniciSil.Click += new System.EventHandler(this.btnKullaniciSil_Click);
            // 
            // btnKullaniciListele
            // 
            this.btnKullaniciListele.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnKullaniciListele.Font = new System.Drawing.Font("Times New Roman", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.btnKullaniciListele.Location = new System.Drawing.Point(676, 467);
            this.btnKullaniciListele.Name = "btnKullaniciListele";
            this.btnKullaniciListele.Size = new System.Drawing.Size(191, 37);
            this.btnKullaniciListele.TabIndex = 23;
            this.btnKullaniciListele.Text = "Kullanıcıları Listele";
            this.btnKullaniciListele.UseVisualStyleBackColor = true;
            this.btnKullaniciListele.Click += new System.EventHandler(this.btnKullaniciListele_Click);
            // 
            // dgvKullanicilar
            // 
            this.dgvKullanicilar.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvKullanicilar.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvKullanicilar.Location = new System.Drawing.Point(676, 53);
            this.dgvKullanicilar.Name = "dgvKullanicilar";
            this.dgvKullanicilar.Size = new System.Drawing.Size(716, 408);
            this.dgvKullanicilar.TabIndex = 22;
            // 
            // label42
            // 
            this.label42.AutoSize = true;
            this.label42.Font = new System.Drawing.Font("Times New Roman", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.label42.Location = new System.Drawing.Point(51, 365);
            this.label42.Name = "label42";
            this.label42.Size = new System.Drawing.Size(92, 15);
            this.label42.TabIndex = 21;
            this.label42.Text = "Şifre Yenileme:";
            // 
            // cmbSifreYenileme
            // 
            this.cmbSifreYenileme.FormattingEnabled = true;
            this.cmbSifreYenileme.Items.AddRange(new object[] {
            "1 Ayda Bir",
            "3 Ayda Bir",
            "Hiçbir Zaman"});
            this.cmbSifreYenileme.Location = new System.Drawing.Point(206, 362);
            this.cmbSifreYenileme.Name = "cmbSifreYenileme";
            this.cmbSifreYenileme.Size = new System.Drawing.Size(201, 21);
            this.cmbSifreYenileme.TabIndex = 20;
            // 
            // label41
            // 
            this.label41.AutoSize = true;
            this.label41.Font = new System.Drawing.Font("Times New Roman", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.label41.Location = new System.Drawing.Point(51, 313);
            this.label41.Name = "label41";
            this.label41.Size = new System.Drawing.Size(84, 15);
            this.label41.TabIndex = 19;
            this.label41.Text = "Hesap Süresi:";
            // 
            // cmbHesapSuresi
            // 
            this.cmbHesapSuresi.FormattingEnabled = true;
            this.cmbHesapSuresi.Items.AddRange(new object[] {
            "1 Saat",
            "1 Ay",
            "3 Ay",
            "6 Ay",
            "Süresiz"});
            this.cmbHesapSuresi.Location = new System.Drawing.Point(206, 310);
            this.cmbHesapSuresi.Name = "cmbHesapSuresi";
            this.cmbHesapSuresi.Size = new System.Drawing.Size(201, 21);
            this.cmbHesapSuresi.TabIndex = 18;
            // 
            // btnKullaniciEkle
            // 
            this.btnKullaniciEkle.Font = new System.Drawing.Font("Times New Roman", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.btnKullaniciEkle.Location = new System.Drawing.Point(137, 158);
            this.btnKullaniciEkle.Name = "btnKullaniciEkle";
            this.btnKullaniciEkle.Size = new System.Drawing.Size(191, 37);
            this.btnKullaniciEkle.TabIndex = 17;
            this.btnKullaniciEkle.Text = "Kullanıcı Ekle";
            this.btnKullaniciEkle.UseVisualStyleBackColor = true;
            this.btnKullaniciEkle.Click += new System.EventHandler(this.btnKullaniciEkle_Click);
            // 
            // clbYetkiler
            // 
            this.clbYetkiler.FormattingEnabled = true;
            this.clbYetkiler.Items.AddRange(new object[] {
            "Ana Panel",
            "Zarf Yazdırma",
            "Çoklu Zarf Yazdırma",
            "Yeni Firma Ekleme",
            "Firma Düzenleme",
            "Ayarlar",
            "Manuel Etiket",
            "Üretim Takip"});
            this.clbYetkiler.Location = new System.Drawing.Point(447, 53);
            this.clbYetkiler.Name = "clbYetkiler";
            this.clbYetkiler.Size = new System.Drawing.Size(191, 409);
            this.clbYetkiler.TabIndex = 16;
            // 
            // label40
            // 
            this.label40.AutoSize = true;
            this.label40.Font = new System.Drawing.Font("Times New Roman", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.label40.Location = new System.Drawing.Point(134, 98);
            this.label40.Name = "label40";
            this.label40.Size = new System.Drawing.Size(66, 15);
            this.label40.TabIndex = 15;
            this.label40.Text = "Yeni Şifre:";
            // 
            // txtYeniSifre
            // 
            this.txtYeniSifre.Location = new System.Drawing.Point(206, 93);
            this.txtYeniSifre.Name = "txtYeniSifre";
            this.txtYeniSifre.Size = new System.Drawing.Size(182, 20);
            this.txtYeniSifre.TabIndex = 14;
            // 
            // label39
            // 
            this.label39.AutoSize = true;
            this.label39.Font = new System.Drawing.Font("Times New Roman", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.label39.Location = new System.Drawing.Point(90, 58);
            this.label39.Name = "label39";
            this.label39.Size = new System.Drawing.Size(110, 15);
            this.label39.TabIndex = 13;
            this.label39.Text = "Yeni Kullanıcı Adı:";
            // 
            // txtYeniKullanici
            // 
            this.txtYeniKullanici.Location = new System.Drawing.Point(206, 53);
            this.txtYeniKullanici.Name = "txtYeniKullanici";
            this.txtYeniKullanici.Size = new System.Drawing.Size(182, 20);
            this.txtYeniKullanici.TabIndex = 12;
            // 
            // tabPage10
            // 
            this.tabPage10.Controls.Add(this.btnSqlTemizle);
            this.tabPage10.Controls.Add(this.btnSqlKaydet);
            this.tabPage10.Controls.Add(this.label48);
            this.tabPage10.Controls.Add(this.txtSqlSifre);
            this.tabPage10.Controls.Add(this.label47);
            this.tabPage10.Controls.Add(this.txtSqlKullanici);
            this.tabPage10.Controls.Add(this.label46);
            this.tabPage10.Controls.Add(this.txtSqlVeritabani);
            this.tabPage10.Controls.Add(this.label44);
            this.tabPage10.Controls.Add(this.txtSqlSunucu);
            this.tabPage10.Location = new System.Drawing.Point(4, 22);
            this.tabPage10.Name = "tabPage10";
            this.tabPage10.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage10.Size = new System.Drawing.Size(1491, 740);
            this.tabPage10.TabIndex = 7;
            this.tabPage10.Text = "SQL Ayarları";
            this.tabPage10.UseVisualStyleBackColor = true;
            // 
            // btnSqlTemizle
            // 
            this.btnSqlTemizle.Font = new System.Drawing.Font("Times New Roman", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.btnSqlTemizle.Location = new System.Drawing.Point(486, 272);
            this.btnSqlTemizle.Name = "btnSqlTemizle";
            this.btnSqlTemizle.Size = new System.Drawing.Size(194, 60);
            this.btnSqlTemizle.TabIndex = 25;
            this.btnSqlTemizle.Text = "SQL Bağlantısını Temizle";
            this.btnSqlTemizle.UseVisualStyleBackColor = true;
            this.btnSqlTemizle.Click += new System.EventHandler(this.btnSqlTemizle_Click);
            // 
            // btnSqlKaydet
            // 
            this.btnSqlKaydet.Font = new System.Drawing.Font("Times New Roman", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.btnSqlKaydet.Location = new System.Drawing.Point(230, 272);
            this.btnSqlKaydet.Name = "btnSqlKaydet";
            this.btnSqlKaydet.Size = new System.Drawing.Size(194, 60);
            this.btnSqlKaydet.TabIndex = 24;
            this.btnSqlKaydet.Text = "SQL Bağlantısını Kaydet";
            this.btnSqlKaydet.UseVisualStyleBackColor = true;
            this.btnSqlKaydet.Click += new System.EventHandler(this.btnSqlKaydet_Click);
            // 
            // label48
            // 
            this.label48.AutoSize = true;
            this.label48.Font = new System.Drawing.Font("Times New Roman", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.label48.Location = new System.Drawing.Point(196, 210);
            this.label48.Name = "label48";
            this.label48.Size = new System.Drawing.Size(81, 15);
            this.label48.TabIndex = 23;
            this.label48.Text = "Şifre kutusu:";
            // 
            // txtSqlSifre
            // 
            this.txtSqlSifre.Location = new System.Drawing.Point(283, 205);
            this.txtSqlSifre.Name = "txtSqlSifre";
            this.txtSqlSifre.PasswordChar = '*';
            this.txtSqlSifre.Size = new System.Drawing.Size(182, 20);
            this.txtSqlSifre.TabIndex = 22;
            // 
            // label47
            // 
            this.label47.AutoSize = true;
            this.label47.Font = new System.Drawing.Font("Times New Roman", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.label47.Location = new System.Drawing.Point(173, 160);
            this.label47.Name = "label47";
            this.label47.Size = new System.Drawing.Size(104, 15);
            this.label47.TabIndex = 21;
            this.label47.Text = "Kullanıcı kutusu:";
            // 
            // txtSqlKullanici
            // 
            this.txtSqlKullanici.Location = new System.Drawing.Point(283, 155);
            this.txtSqlKullanici.Name = "txtSqlKullanici";
            this.txtSqlKullanici.Size = new System.Drawing.Size(182, 20);
            this.txtSqlKullanici.TabIndex = 20;
            // 
            // label46
            // 
            this.label46.AutoSize = true;
            this.label46.Font = new System.Drawing.Font("Times New Roman", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.label46.Location = new System.Drawing.Point(167, 110);
            this.label46.Name = "label46";
            this.label46.Size = new System.Drawing.Size(110, 15);
            this.label46.TabIndex = 19;
            this.label46.Text = "Veritabanı kutusu:";
            // 
            // txtSqlVeritabani
            // 
            this.txtSqlVeritabani.Location = new System.Drawing.Point(283, 105);
            this.txtSqlVeritabani.Name = "txtSqlVeritabani";
            this.txtSqlVeritabani.Size = new System.Drawing.Size(182, 20);
            this.txtSqlVeritabani.TabIndex = 18;
            // 
            // label44
            // 
            this.label44.AutoSize = true;
            this.label44.Font = new System.Drawing.Font("Times New Roman", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.label44.Location = new System.Drawing.Point(182, 61);
            this.label44.Name = "label44";
            this.label44.Size = new System.Drawing.Size(95, 15);
            this.label44.TabIndex = 17;
            this.label44.Text = "Sunucu kutusu:";
            // 
            // txtSqlSunucu
            // 
            this.txtSqlSunucu.Location = new System.Drawing.Point(283, 56);
            this.txtSqlSunucu.Name = "txtSqlSunucu";
            this.txtSqlSunucu.Size = new System.Drawing.Size(182, 20);
            this.txtSqlSunucu.TabIndex = 16;
            // 
            // tabPage12
            // 
            this.tabPage12.Controls.Add(this.label34);
            this.tabPage12.Controls.Add(this.cmbManuelPrinter);
            this.tabPage12.Controls.Add(this.label33);
            this.tabPage12.Controls.Add(this.label32);
            this.tabPage12.Controls.Add(this.label17);
            this.tabPage12.Controls.Add(this.label16);
            this.tabPage12.Controls.Add(this.label15);
            this.tabPage12.Controls.Add(this.label14);
            this.tabPage12.Controls.Add(this.btnManuelYazdir);
            this.tabPage12.Controls.Add(this.btnManuelOnizle);
            this.tabPage12.Controls.Add(this.txtManTel2);
            this.tabPage12.Controls.Add(this.txtManTel1);
            this.tabPage12.Controls.Add(this.txtManIl);
            this.tabPage12.Controls.Add(this.txtManAdres);
            this.tabPage12.Controls.Add(this.txtManFirma);
            this.tabPage12.Controls.Add(this.cmbManualTemplate);
            this.tabPage12.Location = new System.Drawing.Point(4, 22);
            this.tabPage12.Name = "tabPage12";
            this.tabPage12.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage12.Size = new System.Drawing.Size(1503, 770);
            this.tabPage12.TabIndex = 7;
            this.tabPage12.Text = "Manuel Etiket";
            this.tabPage12.UseVisualStyleBackColor = true;
            // 
            // label34
            // 
            this.label34.AutoSize = true;
            this.label34.Font = new System.Drawing.Font("Times New Roman", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.label34.Location = new System.Drawing.Point(556, 110);
            this.label34.Name = "label34";
            this.label34.Size = new System.Drawing.Size(85, 15);
            this.label34.TabIndex = 15;
            this.label34.Text = "Yazıcı Seçimi:";
            // 
            // cmbManuelPrinter
            // 
            this.cmbManuelPrinter.FormattingEnabled = true;
            this.cmbManuelPrinter.Location = new System.Drawing.Point(711, 107);
            this.cmbManuelPrinter.Name = "cmbManuelPrinter";
            this.cmbManuelPrinter.Size = new System.Drawing.Size(201, 21);
            this.cmbManuelPrinter.TabIndex = 14;
            // 
            // label33
            // 
            this.label33.AutoSize = true;
            this.label33.Location = new System.Drawing.Point(556, 264);
            this.label33.Name = "label33";
            this.label33.Size = new System.Drawing.Size(99, 13);
            this.label33.TabIndex = 13;
            this.label33.Text = "Telefon 2 (Ek Bilgi):";
            // 
            // label32
            // 
            this.label32.AutoSize = true;
            this.label32.Location = new System.Drawing.Point(556, 238);
            this.label32.Name = "label32";
            this.label32.Size = new System.Drawing.Size(99, 13);
            this.label32.TabIndex = 12;
            this.label32.Text = "Telefon 1 (Ek Bilgi):";
            // 
            // label17
            // 
            this.label17.AutoSize = true;
            this.label17.Location = new System.Drawing.Point(556, 212);
            this.label17.Name = "label17";
            this.label17.Size = new System.Drawing.Size(77, 13);
            this.label17.TabIndex = 11;
            this.label17.Text = "İl (Örn: Çorum):";
            // 
            // label16
            // 
            this.label16.AutoSize = true;
            this.label16.Location = new System.Drawing.Point(556, 186);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(154, 13);
            this.label16.TabIndex = 10;
            this.label16.Text = "Adres (Örn: Dünyanın Merkezi):";
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Location = new System.Drawing.Point(556, 160);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(153, 13);
            this.label15.TabIndex = 9;
            this.label15.Text = "Firma Adı (Örn: Uğur Software):";
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Font = new System.Drawing.Font("Times New Roman", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.label14.Location = new System.Drawing.Point(556, 56);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(91, 15);
            this.label14.TabIndex = 8;
            this.label14.Text = "Etiket Şablonu:";
            // 
            // btnManuelYazdir
            // 
            this.btnManuelYazdir.Font = new System.Drawing.Font("Times New Roman", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.btnManuelYazdir.Location = new System.Drawing.Point(744, 301);
            this.btnManuelYazdir.Name = "btnManuelYazdir";
            this.btnManuelYazdir.Size = new System.Drawing.Size(168, 79);
            this.btnManuelYazdir.TabIndex = 7;
            this.btnManuelYazdir.Text = "Yazdır";
            this.btnManuelYazdir.UseVisualStyleBackColor = true;
            // 
            // btnManuelOnizle
            // 
            this.btnManuelOnizle.Font = new System.Drawing.Font("Times New Roman", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.btnManuelOnizle.Location = new System.Drawing.Point(559, 301);
            this.btnManuelOnizle.Name = "btnManuelOnizle";
            this.btnManuelOnizle.Size = new System.Drawing.Size(171, 79);
            this.btnManuelOnizle.TabIndex = 6;
            this.btnManuelOnizle.Text = "Önizle";
            this.btnManuelOnizle.UseVisualStyleBackColor = true;
            // 
            // txtManTel2
            // 
            this.txtManTel2.Location = new System.Drawing.Point(711, 257);
            this.txtManTel2.Name = "txtManTel2";
            this.txtManTel2.Size = new System.Drawing.Size(201, 20);
            this.txtManTel2.TabIndex = 5;
            // 
            // txtManTel1
            // 
            this.txtManTel1.Location = new System.Drawing.Point(711, 231);
            this.txtManTel1.Name = "txtManTel1";
            this.txtManTel1.Size = new System.Drawing.Size(201, 20);
            this.txtManTel1.TabIndex = 4;
            // 
            // txtManIl
            // 
            this.txtManIl.Location = new System.Drawing.Point(711, 205);
            this.txtManIl.Name = "txtManIl";
            this.txtManIl.Size = new System.Drawing.Size(201, 20);
            this.txtManIl.TabIndex = 3;
            // 
            // txtManAdres
            // 
            this.txtManAdres.Location = new System.Drawing.Point(711, 179);
            this.txtManAdres.Name = "txtManAdres";
            this.txtManAdres.Size = new System.Drawing.Size(201, 20);
            this.txtManAdres.TabIndex = 2;
            // 
            // txtManFirma
            // 
            this.txtManFirma.Location = new System.Drawing.Point(711, 153);
            this.txtManFirma.Name = "txtManFirma";
            this.txtManFirma.Size = new System.Drawing.Size(201, 20);
            this.txtManFirma.TabIndex = 1;
            // 
            // cmbManualTemplate
            // 
            this.cmbManualTemplate.FormattingEnabled = true;
            this.cmbManualTemplate.Location = new System.Drawing.Point(711, 53);
            this.cmbManualTemplate.Name = "cmbManualTemplate";
            this.cmbManualTemplate.Size = new System.Drawing.Size(201, 21);
            this.cmbManualTemplate.TabIndex = 0;
            // 
            // tabPage7
            // 
            this.tabPage7.Controls.Add(this.pnlBarkodOkuma);
            this.tabPage7.Controls.Add(this.pnlGunSecimi);
            this.tabPage7.Location = new System.Drawing.Point(4, 22);
            this.tabPage7.Name = "tabPage7";
            this.tabPage7.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage7.Size = new System.Drawing.Size(1503, 770);
            this.tabPage7.TabIndex = 8;
            this.tabPage7.Text = "Üretim Takip";
            this.tabPage7.UseVisualStyleBackColor = true;
            // 
            // pnlBarkodOkuma
            // 
            this.pnlBarkodOkuma.Controls.Add(this.btnSecileniSil);
            this.pnlBarkodOkuma.Controls.Add(this.btnUretimGeri);
            this.pnlBarkodOkuma.Controls.Add(this.cmbUretimYazici);
            this.pnlBarkodOkuma.Controls.Add(this.btnUretimKaydet);
            this.pnlBarkodOkuma.Controls.Add(this.chkYazdir);
            this.pnlBarkodOkuma.Controls.Add(this.label37);
            this.pnlBarkodOkuma.Controls.Add(this.label36);
            this.pnlBarkodOkuma.Controls.Add(this.dgvUretim);
            this.pnlBarkodOkuma.Controls.Add(this.txtBarkodOkut);
            this.pnlBarkodOkuma.Location = new System.Drawing.Point(324, 26);
            this.pnlBarkodOkuma.Name = "pnlBarkodOkuma";
            this.pnlBarkodOkuma.Size = new System.Drawing.Size(1169, 731);
            this.pnlBarkodOkuma.TabIndex = 1;
            this.pnlBarkodOkuma.Visible = false;
            // 
            // btnSecileniSil
            // 
            this.btnSecileniSil.Font = new System.Drawing.Font("Times New Roman", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.btnSecileniSil.Location = new System.Drawing.Point(11, 146);
            this.btnSecileniSil.Name = "btnSecileniSil";
            this.btnSecileniSil.Size = new System.Drawing.Size(271, 37);
            this.btnSecileniSil.TabIndex = 17;
            this.btnSecileniSil.Text = "Seçilen Kalemleri Sil";
            this.btnSecileniSil.UseVisualStyleBackColor = true;
            this.btnSecileniSil.Click += new System.EventHandler(this.btnSecileniSil_Click);
            // 
            // btnUretimGeri
            // 
            this.btnUretimGeri.Font = new System.Drawing.Font("Times New Roman", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.btnUretimGeri.Location = new System.Drawing.Point(11, 189);
            this.btnUretimGeri.Name = "btnUretimGeri";
            this.btnUretimGeri.Size = new System.Drawing.Size(90, 37);
            this.btnUretimGeri.TabIndex = 16;
            this.btnUretimGeri.Text = "Geri";
            this.btnUretimGeri.UseVisualStyleBackColor = true;
            this.btnUretimGeri.Click += new System.EventHandler(this.btnUretimGeri_Click);
            // 
            // cmbUretimYazici
            // 
            this.cmbUretimYazici.FormattingEnabled = true;
            this.cmbUretimYazici.Location = new System.Drawing.Point(100, 110);
            this.cmbUretimYazici.Name = "cmbUretimYazici";
            this.cmbUretimYazici.Size = new System.Drawing.Size(182, 21);
            this.cmbUretimYazici.TabIndex = 15;
            // 
            // btnUretimKaydet
            // 
            this.btnUretimKaydet.Font = new System.Drawing.Font("Times New Roman", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.btnUretimKaydet.Location = new System.Drawing.Point(100, 54);
            this.btnUretimKaydet.Name = "btnUretimKaydet";
            this.btnUretimKaydet.Size = new System.Drawing.Size(182, 47);
            this.btnUretimKaydet.TabIndex = 9;
            this.btnUretimKaydet.Text = "Kaydet ve Yazdır";
            this.btnUretimKaydet.UseVisualStyleBackColor = true;
            this.btnUretimKaydet.Click += new System.EventHandler(this.btnUretimKaydet_Click);
            // 
            // chkYazdir
            // 
            this.chkYazdir.AutoSize = true;
            this.chkYazdir.Location = new System.Drawing.Point(6, 71);
            this.chkYazdir.Name = "chkYazdir";
            this.chkYazdir.Size = new System.Drawing.Size(87, 17);
            this.chkYazdir.TabIndex = 11;
            this.chkYazdir.Text = "Rapor Yazdır";
            this.chkYazdir.UseVisualStyleBackColor = true;
            // 
            // label37
            // 
            this.label37.AutoSize = true;
            this.label37.Font = new System.Drawing.Font("Times New Roman", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.label37.Location = new System.Drawing.Point(8, 116);
            this.label37.Name = "label37";
            this.label37.Size = new System.Drawing.Size(85, 15);
            this.label37.TabIndex = 10;
            this.label37.Text = "Yazıcı Seçimi:";
            // 
            // label36
            // 
            this.label36.AutoSize = true;
            this.label36.Font = new System.Drawing.Font("Times New Roman", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.label36.Location = new System.Drawing.Point(3, 30);
            this.label36.Name = "label36";
            this.label36.Size = new System.Drawing.Size(90, 15);
            this.label36.TabIndex = 9;
            this.label36.Text = "Ürün Barkodu:";
            // 
            // dgvUretim
            // 
            this.dgvUretim.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvUretim.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.ÜrünKodu,
            this.ÜrünAçıklaması,
            this.ÜrünAdeti,
            this.ÜrünBarkodu});
            this.dgvUretim.Location = new System.Drawing.Point(288, 25);
            this.dgvUretim.Name = "dgvUretim";
            this.dgvUretim.Size = new System.Drawing.Size(865, 688);
            this.dgvUretim.TabIndex = 1;
            // 
            // ÜrünKodu
            // 
            this.ÜrünKodu.HeaderText = "Ürün Kodu";
            this.ÜrünKodu.Name = "ÜrünKodu";
            // 
            // ÜrünAçıklaması
            // 
            this.ÜrünAçıklaması.HeaderText = "Ürün Açıklaması";
            this.ÜrünAçıklaması.Name = "ÜrünAçıklaması";
            // 
            // ÜrünAdeti
            // 
            this.ÜrünAdeti.HeaderText = "Ürün Adeti";
            this.ÜrünAdeti.Name = "ÜrünAdeti";
            // 
            // ÜrünBarkodu
            // 
            this.ÜrünBarkodu.HeaderText = "Ürün Barkodu";
            this.ÜrünBarkodu.Name = "ÜrünBarkodu";
            // 
            // txtBarkodOkut
            // 
            this.txtBarkodOkut.Location = new System.Drawing.Point(100, 25);
            this.txtBarkodOkut.Name = "txtBarkodOkut";
            this.txtBarkodOkut.Size = new System.Drawing.Size(182, 20);
            this.txtBarkodOkut.TabIndex = 0;
            this.txtBarkodOkut.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtBarkodOkut_KeyDown);
            // 
            // pnlGunSecimi
            // 
            this.pnlGunSecimi.Controls.Add(this.btnUretimIleri);
            this.pnlGunSecimi.Controls.Add(this.dtpUretimTarihi);
            this.pnlGunSecimi.Location = new System.Drawing.Point(8, 26);
            this.pnlGunSecimi.Name = "pnlGunSecimi";
            this.pnlGunSecimi.Size = new System.Drawing.Size(310, 81);
            this.pnlGunSecimi.TabIndex = 0;
            // 
            // btnUretimIleri
            // 
            this.btnUretimIleri.Font = new System.Drawing.Font("Times New Roman", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.btnUretimIleri.Location = new System.Drawing.Point(207, 18);
            this.btnUretimIleri.Name = "btnUretimIleri";
            this.btnUretimIleri.Size = new System.Drawing.Size(90, 37);
            this.btnUretimIleri.TabIndex = 8;
            this.btnUretimIleri.Text = "İleri";
            this.btnUretimIleri.UseVisualStyleBackColor = true;
            this.btnUretimIleri.Click += new System.EventHandler(this.btnUretimIleri_Click);
            // 
            // dtpUretimTarihi
            // 
            this.dtpUretimTarihi.Location = new System.Drawing.Point(12, 25);
            this.dtpUretimTarihi.Name = "dtpUretimTarihi";
            this.dtpUretimTarihi.Size = new System.Drawing.Size(177, 20);
            this.dtpUretimTarihi.TabIndex = 0;
            // 
            // tabPage13
            // 
            this.tabPage13.Controls.Add(this.label56);
            this.tabPage13.Controls.Add(this.txtBarkod);
            this.tabPage13.Controls.Add(this.btnKismiSevk);
            this.tabPage13.Controls.Add(this.btnTamSevk);
            this.tabPage13.Controls.Add(this.btnSevkAra);
            this.tabPage13.Controls.Add(this.label51);
            this.tabPage13.Controls.Add(this.txtSevkMusteri);
            this.tabPage13.Controls.Add(this.label50);
            this.tabPage13.Controls.Add(this.dgvMalzemeler);
            this.tabPage13.Controls.Add(this.cmbBelgeNo);
            this.tabPage13.Controls.Add(this.btnSiparisYenile);
            this.tabPage13.Controls.Add(this.label49);
            this.tabPage13.Controls.Add(this.txtMusteriAdi);
            this.tabPage13.Location = new System.Drawing.Point(4, 22);
            this.tabPage13.Name = "tabPage13";
            this.tabPage13.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage13.Size = new System.Drawing.Size(1503, 770);
            this.tabPage13.TabIndex = 9;
            this.tabPage13.Text = "Sevkiyat";
            this.tabPage13.UseVisualStyleBackColor = true;
            // 
            // btnKismiSevk
            // 
            this.btnKismiSevk.BackColor = System.Drawing.Color.DarkOrange;
            this.btnKismiSevk.Font = new System.Drawing.Font("Times New Roman", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.btnKismiSevk.Location = new System.Drawing.Point(436, 467);
            this.btnKismiSevk.Name = "btnKismiSevk";
            this.btnKismiSevk.Size = new System.Drawing.Size(191, 37);
            this.btnKismiSevk.TabIndex = 33;
            this.btnKismiSevk.Text = "KISMİ SEVKET";
            this.btnKismiSevk.UseVisualStyleBackColor = false;
            this.btnKismiSevk.Click += new System.EventHandler(this.btnKismiSevk_Click);
            // 
            // btnTamSevk
            // 
            this.btnTamSevk.BackColor = System.Drawing.Color.Lime;
            this.btnTamSevk.Font = new System.Drawing.Font("Times New Roman", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.btnTamSevk.Location = new System.Drawing.Point(436, 401);
            this.btnTamSevk.Name = "btnTamSevk";
            this.btnTamSevk.Size = new System.Drawing.Size(191, 37);
            this.btnTamSevk.TabIndex = 32;
            this.btnTamSevk.Text = "TAM SEVKET";
            this.btnTamSevk.UseVisualStyleBackColor = false;
            this.btnTamSevk.Click += new System.EventHandler(this.btnTamSevk_Click);
            // 
            // btnSevkAra
            // 
            this.btnSevkAra.Font = new System.Drawing.Font("Times New Roman", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.btnSevkAra.Location = new System.Drawing.Point(436, 308);
            this.btnSevkAra.Name = "btnSevkAra";
            this.btnSevkAra.Size = new System.Drawing.Size(191, 37);
            this.btnSevkAra.TabIndex = 31;
            this.btnSevkAra.Text = "Ara";
            this.btnSevkAra.UseVisualStyleBackColor = true;
            this.btnSevkAra.Click += new System.EventHandler(this.btnSevkAra_Click);
            // 
            // label51
            // 
            this.label51.AutoSize = true;
            this.label51.Font = new System.Drawing.Font("Times New Roman", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.label51.Location = new System.Drawing.Point(710, 146);
            this.label51.Name = "label51";
            this.label51.Size = new System.Drawing.Size(95, 15);
            this.label51.TabIndex = 30;
            this.label51.Text = "Sevk Müşterisi:";
            // 
            // txtSevkMusteri
            // 
            this.txtSevkMusteri.Location = new System.Drawing.Point(540, 200);
            this.txtSevkMusteri.Name = "txtSevkMusteri";
            this.txtSevkMusteri.ReadOnly = true;
            this.txtSevkMusteri.Size = new System.Drawing.Size(426, 20);
            this.txtSevkMusteri.TabIndex = 29;
            // 
            // label50
            // 
            this.label50.AutoSize = true;
            this.label50.Font = new System.Drawing.Font("Times New Roman", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.label50.Location = new System.Drawing.Point(117, 330);
            this.label50.Name = "label50";
            this.label50.Size = new System.Drawing.Size(61, 15);
            this.label50.TabIndex = 28;
            this.label50.Text = "Belge No:";
            // 
            // dgvMalzemeler
            // 
            this.dgvMalzemeler.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvMalzemeler.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvMalzemeler.Location = new System.Drawing.Point(733, 324);
            this.dgvMalzemeler.Name = "dgvMalzemeler";
            this.dgvMalzemeler.Size = new System.Drawing.Size(716, 408);
            this.dgvMalzemeler.TabIndex = 27;
            // 
            // cmbBelgeNo
            // 
            this.cmbBelgeNo.FormattingEnabled = true;
            this.cmbBelgeNo.Items.AddRange(new object[] {
            "1 Saat",
            "1 Ay",
            "3 Ay",
            "6 Ay",
            "Süresiz"});
            this.cmbBelgeNo.Location = new System.Drawing.Point(192, 324);
            this.cmbBelgeNo.Name = "cmbBelgeNo";
            this.cmbBelgeNo.Size = new System.Drawing.Size(201, 21);
            this.cmbBelgeNo.TabIndex = 26;
            // 
            // btnSiparisYenile
            // 
            this.btnSiparisYenile.Font = new System.Drawing.Font("Times New Roman", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.btnSiparisYenile.Location = new System.Drawing.Point(183, 379);
            this.btnSiparisYenile.Name = "btnSiparisYenile";
            this.btnSiparisYenile.Size = new System.Drawing.Size(191, 37);
            this.btnSiparisYenile.TabIndex = 25;
            this.btnSiparisYenile.Text = "Yenile";
            this.btnSiparisYenile.UseVisualStyleBackColor = true;
            this.btnSiparisYenile.Click += new System.EventHandler(this.btnSiparisYenile_Click);
            // 
            // label49
            // 
            this.label49.AutoSize = true;
            this.label49.Font = new System.Drawing.Font("Times New Roman", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.label49.Location = new System.Drawing.Point(233, 146);
            this.label49.Name = "label49";
            this.label49.Size = new System.Drawing.Size(76, 15);
            this.label49.TabIndex = 24;
            this.label49.Text = "Müşteri Adı:";
            // 
            // txtMusteriAdi
            // 
            this.txtMusteriAdi.Location = new System.Drawing.Point(79, 200);
            this.txtMusteriAdi.Name = "txtMusteriAdi";
            this.txtMusteriAdi.ReadOnly = true;
            this.txtMusteriAdi.Size = new System.Drawing.Size(426, 20);
            this.txtMusteriAdi.TabIndex = 23;
            // 
            // printPreviewDialog1
            // 
            this.printPreviewDialog1.AutoScrollMargin = new System.Drawing.Size(0, 0);
            this.printPreviewDialog1.AutoScrollMinSize = new System.Drawing.Size(0, 0);
            this.printPreviewDialog1.ClientSize = new System.Drawing.Size(400, 300);
            this.printPreviewDialog1.Enabled = true;
            this.printPreviewDialog1.Icon = ((System.Drawing.Icon)(resources.GetObject("printPreviewDialog1.Icon")));
            this.printPreviewDialog1.Name = "printPreviewDialog1";
            this.printPreviewDialog1.Text = "Baskı önizleme";
            this.printPreviewDialog1.Visible = false;
            // 
            // printDialog1
            // 
            this.printDialog1.UseEXDialog = true;
            // 
            // label56
            // 
            this.label56.AutoSize = true;
            this.label56.Font = new System.Drawing.Font("Times New Roman", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.label56.Location = new System.Drawing.Point(88, 505);
            this.label56.Name = "label56";
            this.label56.Size = new System.Drawing.Size(90, 15);
            this.label56.TabIndex = 35;
            this.label56.Text = "Ürün Barkodu:";
            // 
            // txtBarkod
            // 
            this.txtBarkod.Location = new System.Drawing.Point(183, 503);
            this.txtBarkod.Name = "txtBarkod";
            this.txtBarkod.Size = new System.Drawing.Size(182, 20);
            this.txtBarkod.TabIndex = 34;
            this.txtBarkod.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtBarkod_KeyDown);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.ClientSize = new System.Drawing.Size(1511, 796);
            this.Controls.Add(this.tabControl1);
            this.Margin = new System.Windows.Forms.Padding(2);
            this.Name = "MainForm";
            this.Text = "Form1";
            this.tabPage5.ResumeLayout(false);
            this.tabPage5.PerformLayout();
            this.panel3.ResumeLayout(false);
            this.panel3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvFirmalar)).EndInit();
            this.tabPage4.ResumeLayout(false);
            this.tabPage4.PerformLayout();
            this.tabPage3.ResumeLayout(false);
            this.pnlAmbar.ResumeLayout(false);
            this.pnlAmbar.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvAmbarSonListe)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvPaletler)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvAmbarSecilenFirmalar)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvAmbarTumFirmalar)).EndInit();
            this.pnlNormal.ResumeLayout(false);
            this.pnlNormal.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvZarfFirmalar)).EndInit();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.tabPage1.ResumeLayout(false);
            this.tabControl1.ResumeLayout(false);
            this.TabPage.ResumeLayout(false);
            this.tabPrintSettings.ResumeLayout(false);
            this.tabPage11.ResumeLayout(false);
            this.pnlProperties.ResumeLayout(false);
            this.pnlProperties.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numPropHmm)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numPropWmm)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numPropYmm)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numPropXmm)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numPropFontSize)).EndInit();
            this.pnlToolbox.ResumeLayout(false);
            this.pnlToolbox.PerformLayout();
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numGridMm)).EndInit();
            this.tabPage6.ResumeLayout(false);
            this.tabPage6.PerformLayout();
            this.tabPage8.ResumeLayout(false);
            this.tabPage8.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvBarkodVerileri)).EndInit();
            this.tabPage9.ResumeLayout(false);
            this.tabPage9.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvKullanicilar)).EndInit();
            this.tabPage10.ResumeLayout(false);
            this.tabPage10.PerformLayout();
            this.tabPage12.ResumeLayout(false);
            this.tabPage12.PerformLayout();
            this.tabPage7.ResumeLayout(false);
            this.pnlBarkodOkuma.ResumeLayout(false);
            this.pnlBarkodOkuma.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvUretim)).EndInit();
            this.pnlGunSecimi.ResumeLayout(false);
            this.tabPage13.ResumeLayout(false);
            this.tabPage13.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvMalzemeler)).EndInit();
            this.ResumeLayout(false);

        }


        #endregion
        private System.Windows.Forms.TabPage tabPage5;
        private System.Windows.Forms.TabPage tabPage4;
        private System.Windows.Forms.TabPage tabPage3;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage TabPage;
        private System.Windows.Forms.TextBox txtTel2;
        private System.Windows.Forms.TextBox txtTel1;
        private System.Windows.Forms.TextBox txtIl;
        private System.Windows.Forms.TextBox txtAdres;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button btnKaydet;
        private System.Windows.Forms.TextBox txtFirmaAdi;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnGuncelle;
        private System.Windows.Forms.TextBox txtEditTel2;
        private System.Windows.Forms.TextBox txtEditTel1;
        private System.Windows.Forms.TextBox txtEditIl;
        private System.Windows.Forms.TextBox txtEditAdres;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.TextBox txtEditFirmaAdi;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.ListBox lstFirmalar;
        private System.Windows.Forms.DataGridView dgvFirmalar;
        private System.Windows.Forms.Button btnListele;
        private System.Windows.Forms.DataGridViewTextBoxColumn Id;
        private System.Windows.Forms.DataGridViewTextBoxColumn FirmaAdi;
        private System.Windows.Forms.DataGridViewTextBoxColumn Adres;
        private System.Windows.Forms.DataGridViewTextBoxColumn Il;
        private System.Windows.Forms.DataGridViewTextBoxColumn Telefon1;
        private System.Windows.Forms.DataGridViewTextBoxColumn Telefon2;
        private PrintDocument printDocument1 = new PrintDocument();
        private System.Windows.Forms.PrintPreviewDialog printPreviewDialog1;
        private System.Windows.Forms.TabControl tabPrintSettings;
        private System.Windows.Forms.TabPage tabPage11;
        private Panel pnlDesignSurface;
        private Panel pnlToolbox;
        private Button btnAddImage;
        private Button btnAddFrame;
        private Button btnAddField;
        private Button btnAddLabel;
        private Panel panel2;
        private Label label19;
        private TextBox txtPageHeightMm;
        private Label label18;
        private TextBox txtPageWidthMm;
        private Label label20;
        private NumericUpDown numGridMm;
        private CheckBox chkSnapToGrid;
        private RadioButton rbLandscape;
        private RadioButton rbPortrait;
        private Button btnDeleteItem;
        private Panel pnlProperties;
        private Button btnApplyProp;
        private Label label31;
        private ComboBox cmbPropRotation;
        private Label label30;
        private NumericUpDown numPropHmm;
        private Label label29;
        private NumericUpDown numPropWmm;
        private Label label28;
        private NumericUpDown numPropYmm;
        private Label label27;
        private NumericUpDown numPropXmm;
        private Button btnPropColor;
        private Label label26;
        private ComboBox cmbPropAlignment;
        private Label label25;
        private Label label24;
        private NumericUpDown numPropFontSize;
        private ComboBox cmbPropFont;
        private Label label23;
        private ComboBox cmbPropPlaceholder;
        private Label label21;
        private TextBox txtPropText;
        private ListBox lstTemplates;
        private Button btnPrint;
        private Button btnSaveTemplate;
        private Button btnLoadTemplate;
        private Button btnPreview;
        private Button btnExportSample;
        private Label label22;
        private Button btnSil;
        private Button btnDeleteTemplate;
        private Button btnTopluAktar;
        private Panel panel3;
        private TabPage tabPage6;
        private Label label13;
        private Label label12;
        private Button btnSavePrinterMapping;
        private ComboBox cmbPrinters;
        private ComboBox cmbPrintingPages;
        private TabPage tabPage12;
        private Label label33;
        private Label label32;
        private Label label17;
        private Label label16;
        private Label label15;
        private Label label14;
        private Button btnManuelYazdir;
        private Button btnManuelOnizle;
        private TextBox txtManTel2;
        private TextBox txtManTel1;
        private TextBox txtManIl;
        private TextBox txtManAdres;
        private TextBox txtManFirma;
        private ComboBox cmbManualTemplate;
        private Label label34;
        private ComboBox cmbManuelPrinter;
        private Label label35;
        private ComboBox cmbAdet;
        private Button btnTemizleTasarm;
        private TabPage tabPage7;
        private Panel pnlGunSecimi;
        private DateTimePicker dtpUretimTarihi;
        private System.ComponentModel.BackgroundWorker backgroundWorker1;
        private Panel pnlBarkodOkuma;
        private Button btnUretimIleri;
        private DataGridView dgvUretim;
        private TextBox txtBarkodOkut;
        private ComboBox cmbUretimYazici;
        private Button btnUretimKaydet;
        private CheckBox chkYazdir;
        private Label label37;
        private Label label36;
        private DataGridViewTextBoxColumn ÜrünKodu;
        private DataGridViewTextBoxColumn ÜrünAçıklaması;
        private DataGridViewTextBoxColumn ÜrünAdeti;
        private DataGridViewTextBoxColumn ÜrünBarkodu;
        private TabPage tabPage8;
        private Button btnExcelAktar;
        private Button btnBarkodVerileri;
        private DataGridView dgvBarkodVerileri;
        private Button btnKayitYeriSec;
        private Label label38;
        private TextBox txtKayitYeri;
        private Button btnUretimGeri;
        private Button btnTumVerileriSil;
        private Button btnSecileniSil;
        private TabPage tabPage9;
        private Button btnKullaniciEkle;
        private CheckedListBox clbYetkiler;
        private Label label40;
        private TextBox txtYeniSifre;
        private Label label39;
        private TextBox txtYeniKullanici;
        private Label label42;
        private ComboBox cmbSifreYenileme;
        private Label label41;
        private ComboBox cmbHesapSuresi;
        private Button btnCikisYap;
        private Button btnSifreYenileAdmin;
        private Button btnKullaniciSil;
        private Button btnKullaniciListele;
        private DataGridView dgvKullanicilar;
        private ComboBox cmbZarfTuru;
        private Panel pnlNormal;
        private DataGridView dgvZarfFirmalar;
        private DataGridViewTextBoxColumn ZarfId;
        private DataGridViewTextBoxColumn ZarfFirmaAdi;
        private DataGridViewTextBoxColumn ZarfAdres;
        private DataGridViewTextBoxColumn ZarfIl;
        private DataGridViewTextBoxColumn ZarfTelefon1;
        private DataGridViewTextBoxColumn ZarfTelefon2;
        private Label label11;
        private Panel panel1;
        private Label labell;
        private Button btnTemizle;
        private ComboBox cmbCokluPrinter;
        private ComboBox cmbPrintStyle;
        private CheckedListBox lstSecilenFirmalar;
        private Button btnAra;
        private Button btnCokluZarfYazdir;
        private Button btnCikar;
        private Button btnZarfYenile;
        private TextBox txtAramaFirmaAdi;
        private Panel pnlAmbar;
        private DataGridView dgvPaletler;
        private Label label43;
        private ComboBox cmbPaletSayisi;
        private DataGridView dgvAmbarSecilenFirmalar;
        private DataGridView dgvAmbarTumFirmalar;
        private Button btnAmbarYazdir;
        private Button btnAmbarYenile;
        private Button btnAmbarSil;
        private Button btnAmbarListeyeEkle;
        private DataGridView dgvAmbarSonListe;
        private PrintDialog printDialog1;
        private ComboBox cmbAmbarYazici;
        private Label label45;
        private Button btnAmbarAra;
        private TextBox txtAmbarFirmaAra;
        private Button btnTumFirmalariSil;
        private TabPage tabPage10;
        private Label label48;
        private TextBox txtSqlSifre;
        private Label label47;
        private TextBox txtSqlKullanici;
        private Label label46;
        private TextBox txtSqlVeritabani;
        private Label label44;
        private TextBox txtSqlSunucu;
        private Button btnSqlKaydet;
        private TabPage tabPage13;
        private Label label51;
        private TextBox txtSevkMusteri;
        private Label label50;
        private DataGridView dgvMalzemeler;
        private ComboBox cmbBelgeNo;
        private Button btnSiparisYenile;
        private Label label49;
        private TextBox txtMusteriAdi;
        private Button btnSqlTemizle;
        private Button btnSevkAra;
        private Button btnTumVerileriTemizle;
        private Button btnKismiSevk;
        private Button btnTamSevk;
        private Label label56;
        private TextBox txtBarkod;
    }
}

