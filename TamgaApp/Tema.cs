using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace TamgaApp
{
    /// <summary>
    /// TamgaApp'in merkezi görsel tema motoru. Bütün formlara (tasarımcıda çizilenler ve kod
    /// içinde anlık oluşturulan pencereler dahil) tek bir renk paleti, yazı tipi ve yuvarlak
    /// köşeli buton çizimi uygular. İş mantığına dokunmaz: yalnızca görünüm özelliklerini değiştirir.
    /// Bir kontrolün temaya girmemesi gerekiyorsa Tag özelliğine "TemaYok" yazılması yeterlidir.
    /// </summary>
    public static class Tema
    {
        #region 🎨 RENK PALETİ (ZÜMRÜT & GRAFİT)

        public static readonly Color Zemin = Color.FromArgb(243, 245, 248);
        public static readonly Color Kart = Color.White;
        public static readonly Color Cizgi = Color.FromArgb(226, 232, 240);
        public static readonly Color Metin = Color.FromArgb(30, 41, 59);
        public static readonly Color SolukMetin = Color.FromArgb(100, 116, 139);
        public static readonly Color Grafit = Color.FromArgb(17, 24, 39);
        public static readonly Color GrafitAcik = Color.FromArgb(31, 41, 55);
        public static readonly Color Vurgu = Color.FromArgb(15, 118, 110);
        public static readonly Color VurguKoyu = Color.FromArgb(17, 94, 89);
        public static readonly Color VurguAcik = Color.FromArgb(204, 251, 241);
        public static readonly Color VurguCokAcik = Color.FromArgb(240, 253, 250);
        public static readonly Color Basari = Color.FromArgb(5, 150, 105);
        public static readonly Color Tehlike = Color.FromArgb(220, 38, 38);
        public static readonly Color Uyari = Color.FromArgb(217, 119, 6);
        public static readonly Color Sari = Color.FromArgb(202, 138, 4);
        public static readonly Color Bilgi = Color.FromArgb(37, 99, 235);
        public static readonly Color Cyan = Color.FromArgb(14, 116, 144);
        public static readonly Color Mor = Color.FromArgb(124, 58, 237);

        public const string TemaYok = "TemaYok";

        #endregion

        #region 🔤 YAZI TİPLERİ (ÖNBELLEKLİ)

        private static readonly Dictionary<string, Font> fontlar = new Dictionary<string, Font>();

        public static Font Yazi(float boyut, bool kalin = false, FontStyle ekStil = FontStyle.Regular)
        {
            string aile = kalin ? "Segoe UI Semibold" : "Segoe UI";
            string anahtar = aile + "|" + boyut + "|" + (int)ekStil;
            Font f;
            if (!fontlar.TryGetValue(anahtar, out f))
            {
                f = new Font(aile, boyut, ekStil);
                fontlar[anahtar] = f;
            }
            return f;
        }

        public static Font Ikon(float boyut)
        {
            string anahtar = "ikon|" + boyut;
            Font f;
            if (!fontlar.TryGetValue(anahtar, out f))
            {
                f = new Font(IkonAilesi, boyut);
                fontlar[anahtar] = f;
            }
            return f;
        }

        private static string ikonAilesi;
        public static string IkonAilesi
        {
            get
            {
                if (ikonAilesi == null)
                {
                    ikonAilesi = "Segoe UI Symbol";
                    foreach (FontFamily ff in FontFamily.Families)
                    {
                        if (ff.Name == "Segoe Fluent Icons") { ikonAilesi = ff.Name; break; }
                        if (ff.Name == "Segoe MDL2 Assets") ikonAilesi = ff.Name;
                    }
                }
                return ikonAilesi;
            }
        }

        private static bool EskiYaziMi(Font f)
        {
            string ad = f.FontFamily.Name;
            return ad == "Times New Roman" || ad == "Microsoft Sans Serif" || ad == "Arial" || ad == "Tahoma"
                || ad == "Segoe UI" || ad == "Segoe UI Semibold" || ad == "Segoe UI Semilight" || ad == "Calibri" || ad == "Verdana";
        }

        #endregion

        #region 🛠️ YARDIMCI ÇİZİM

        public static GraphicsPath YuvarlakDikdortgen(RectangleF r, float yaricap)
        {
            var yol = new GraphicsPath();
            float d = Math.Min(yaricap * 2, Math.Min(r.Width, r.Height));
            if (d <= 1) { yol.AddRectangle(r); return yol; }
            yol.AddArc(r.X, r.Y, d, d, 180, 90);
            yol.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            yol.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            yol.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            yol.CloseFigure();
            return yol;
        }

        public static Color Karistir(Color a, Color b, float oran)
        {
            oran = Math.Max(0, Math.Min(1, oran));
            return Color.FromArgb(
                (int)(a.R + (b.R - a.R) * oran),
                (int)(a.G + (b.G - a.G) * oran),
                (int)(a.B + (b.B - a.B) * oran));
        }

        /// <summary>Saydam arka planlı kontrollerin gerçekte üzerinde durduğu rengi bulur.</summary>
        public static Color GercekArkaPlan(Control c)
        {
            while (c != null)
            {
                if (c.BackColor.A == 255) return c.BackColor;
                c = c.Parent;
            }
            return Zemin;
        }

        private static bool KoyuMu(Color c)
        {
            return (c.R * 299 + c.G * 587 + c.B * 114) / 1000 < 110;
        }

        /// <summary>
        /// Kodun içinde yazılmış ham renkleri (Red, Lime, Orange...) paletteki karşılığına çevirir.
        /// Böylece "kırmızı = tehlike, yeşil = onay" gibi anlamlar korunur ama tonlar tek bir aileden gelir.
        /// Ham renk gri/beyaz ise null döner (ikincil/sade görünüm kullanılır).
        /// </summary>
        public static Color? AnlamRengi(Color ham)
        {
            if (ham.A < 255) return null;
            float doygunluk = ham.GetSaturation();
            float parlaklik = ham.GetBrightness();
            // Çok açık tonlar (AliceBlue, Honeydew, GhostWhite...) HSL'de "doygun" görünse de gözle beyazdır.
            if (parlaklik > 0.88f) return null;
            if (parlaklik < 0.16f) return GrafitAcik;
            if (doygunluk < 0.18f)
            {
                if (parlaklik < 0.35f) return GrafitAcik;
                return null;
            }
            float ton = ham.GetHue();
            if (ton < 14 || ton >= 340) return Tehlike;
            if (ton < 42) return Uyari;
            if (ton < 64) return Sari;
            if (ton < 165) return parlaklik < 0.3f ? VurguKoyu : Basari;
            if (ton < 195) return Cyan;
            if (ton < 255) return Bilgi;
            return Mor;
        }

        #endregion

        #region ⚙️ OTOMATİK TEMA (TÜM FORMLAR)

        private static readonly ConditionalWeakTable<Control, object> islenenler = new ConditionalWeakTable<Control, object>();
        private static bool izleniyor;

        /// <summary>
        /// Program açılışında bir kez çağrılır. Ekrana gelen her yeni pencereyi (MessageBox hariç)
        /// otomatik olarak temalar. Sonradan eklenen kontroller de ControlAdded ile yakalanır.
        /// </summary>
        public static void OtomatikTemayiBaslat()
        {
            if (izleniyor) return;
            izleniyor = true;
            Application.Idle += (s, e) =>
            {
                foreach (Form f in Application.OpenForms)
                {
                    object x;
                    if (!islenenler.TryGetValue(f, out x)) Uygula(f);
                }
            };
        }

        public static void Uygula(Control kok)
        {
            if (kok == null || HaricMi(kok)) return;
            object x;
            bool yeni = !islenenler.TryGetValue(kok, out x);
            if (yeni)
            {
                islenenler.Add(kok, null);
                try { Stillendir(kok); } catch { /* Tema hatası iş akışını asla durdurmamalı */ }
                kok.ControlAdded += (s, e) => Uygula(e.Control);
            }
            foreach (Control c in kok.Controls) Uygula(c);
        }

        public static void HaricTut(Control c)
        {
            if (c == null) return;
            c.Tag = c.Tag ?? TemaYok;
            object x;
            if (!islenenler.TryGetValue(c, out x)) islenenler.Add(c, null);
        }

        private static bool HaricMi(Control c)
        {
            if (c.Tag is string t && t == TemaYok) return true;
            if (c.Name == "pnlDesignSurface") return true;
            string tur = c.GetType().FullName;
            if (tur.StartsWith("Microsoft.Web.WebView2")) return true;
            return false;
        }

        private static void Stillendir(Control c)
        {
            if (c is Form) { FormStil((Form)c); return; }
            if (c is Button) { ButonStil((Button)c); return; }
            if (c is DataGridView) { TabloStil((DataGridView)c); return; }
            if (c is TextBoxBase) { KutuStil((TextBoxBase)c); return; }
            if (c is ComboBox) { SecimStil((ComboBox)c); return; }
            if (c is NumericUpDown || c is DateTimePicker) { GirdiYazisiniAyarla(c); return; }
            if (c is CheckedListBox || c is ListBox || c is TreeView || c is ListView) { ListeStil(c); return; }
            if (c is CheckBox || c is RadioButton) { SecenekStil((ButtonBase)c); return; }
            if (c is Label) { EtiketStil((Label)c); return; }
            if (c is TabControl) { SekmeStil((TabControl)c); return; }
            // DİKKAT: Form/TabPage/GroupBox gibi kapsayıcıların Font'u değiştirilmez. WinForms
            // AutoScaleMode=Font nedeniyle bu, içindeki bütün nesneleri büyütüp yerlerini kaydırır.
            if (c is TabPage) { c.BackColor = Zemin; c.ForeColor = Metin; return; }
            if (c is SplitContainer) { BolucuStil((SplitContainer)c); return; }
            if (c is GroupBox) { c.ForeColor = Metin; return; }
            if (c is PictureBox) return;
            if (c is Panel || c is FlowLayoutPanel || c is TableLayoutPanel) { PanelStil(c); return; }
        }

        private static void YaziyiCevir(Control c, float varsayilan, bool kalin = false)
        {
            if (c.Font == null || !EskiYaziMi(c.Font)) return;
            bool k = kalin || c.Font.Bold || c.Font.Name.Contains("Semibold");
            float boyut = c.Font.SizeInPoints;
            if (boyut < 7) boyut = varsayilan;
            FontStyle ek = c.Font.Italic ? FontStyle.Regular : FontStyle.Regular;
            c.Font = Yazi(boyut, k, ek);
        }

        #region 🪟 PENCERE BAŞLIĞI (WINDOWS 11 DWM)

        [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int nitelik, ref int deger, int boyut);

        private static int RenkRef(Color c) { return c.R | (c.G << 8) | (c.B << 16); }

        /// <summary>
        /// Windows'un beyaz pencere başlığını programın koyu grafit rengine boyar (Windows 11). Windows 10'da
        /// koyu mod başlığı kullanılır; daha eski sistemlerde hiçbir şey değişmez (hata vermez).
        /// </summary>
        public static void BasligiBoya(Form f)
        {
            if (f == null || f.FormBorderStyle == FormBorderStyle.None) return;
            Action uygula = () =>
            {
                try
                {
                    int koyu = 1;
                    if (DwmSetWindowAttribute(f.Handle, 20, ref koyu, 4) != 0) DwmSetWindowAttribute(f.Handle, 19, ref koyu, 4); // koyu mod (Win10 eski/yeni kimlik)
                    int baslik = RenkRef(Grafit), yazi = RenkRef(Color.FromArgb(226, 232, 240)), kenar = RenkRef(Grafit);
                    DwmSetWindowAttribute(f.Handle, 35, ref baslik, 4); // DWMWA_CAPTION_COLOR
                    DwmSetWindowAttribute(f.Handle, 36, ref yazi, 4);   // DWMWA_TEXT_COLOR
                    DwmSetWindowAttribute(f.Handle, 34, ref kenar, 4);  // DWMWA_BORDER_COLOR
                }
                catch { }
            };
            if (f.IsHandleCreated) uygula();
            f.HandleCreated += (s, e) => uygula();
        }

        #endregion

        private static void FormStil(Form f)
        {
            BasligiBoya(f);
            if (!(f is LoginForm) && !(f is SplashForm))
            {
                Color? anlam = AnlamRengi(f.BackColor);
                f.BackColor = anlam.HasValue && KoyuMu(f.BackColor) ? Grafit : (anlam ?? Zemin);
                if (f.BackColor == Zemin) f.ForeColor = Metin;
            }
        }

        private static void PanelStil(Control p)
        {
            Color b = p.BackColor;
            if (b.A < 255) return;
            if (KoyuMu(b)) { p.BackColor = Grafit; return; }
            Color? anlam = AnlamRengi(b);
            if (anlam.HasValue) p.BackColor = Karistir(anlam.Value, Color.White, 0.88f);
            else if (b == SystemColors.Control || b == Color.WhiteSmoke || b.GetBrightness() > 0.9f) p.BackColor = Zemin;
            if (!KoyuMu(p.BackColor)) p.ForeColor = Metin;
        }

        private static void BolucuStil(SplitContainer s)
        {
            s.BackColor = Cizgi;
            PanelStil(s.Panel1);
            PanelStil(s.Panel2);
            if (s.Panel1.BackColor == Cizgi) s.Panel1.BackColor = Zemin;
            if (s.Panel2.BackColor == Cizgi) s.Panel2.BackColor = Zemin;
        }

        private static void EtiketStil(Label l)
        {
            Color zemin = GercekArkaPlan(l.Parent);
            if (l.BackColor.A == 255)
            {
                Color? anlam = AnlamRengi(l.BackColor);
                if (anlam.HasValue) { l.BackColor = anlam.Value; l.ForeColor = Color.White; }
                else if (l.BackColor.GetBrightness() > 0.85f || l.BackColor == SystemColors.Control) l.BackColor = Color.Transparent;
            }
            Color gercek = GercekArkaPlan(l);
            if (KoyuMu(gercek))
            {
                if (KoyuMu(l.ForeColor) || l.ForeColor == SystemColors.ControlText) l.ForeColor = Color.White;
            }
            else if (l.ForeColor == SystemColors.ControlText || KoyuMu(l.ForeColor) || l.ForeColor.GetSaturation() < 0.15f)
            {
                l.ForeColor = Metin;
            }
            else
            {
                Color? anlam = AnlamRengi(l.ForeColor);
                if (anlam.HasValue) l.ForeColor = anlam.Value;
            }

            // Koyu panelde koyu kutucuk içinde kalan etiketler panelle bütünleşsin
            if (l.BackColor == GrafitAcik && KoyuMu(zemin)) l.BackColor = Color.Transparent;

            if (l.Font != null && EskiYaziMi(l.Font))
            {
                float boyut = l.Font.SizeInPoints;
                bool kalin = l.Font.Bold;
                int asilYukseklik = l.Height;
                if (boyut >= 16) l.Font = Yazi(boyut, true);
                else
                {
                    boyut = Math.Max(8f, Math.Min(boyut, 10.5f));
                    l.Font = Yazi(boyut, kalin);

                    if (l.AutoSize)
                    {
                        // Etiket eski yüksekliğini aşmasın (aşarsa hemen altındaki kutunun kenarına biner)
                        while (l.PreferredHeight > asilYukseklik + 1 && boyut > 8f) { boyut -= 0.25f; l.Font = Yazi(boyut, kalin); }
                        EtiketCakismasiniOnle(l);
                    }
                    else
                    {
                        // Sabit boyutlu etiket: metin kutusuna sığana kadar küçült (alt satıra kayıp kırpılmasın)
                        while (boyut > 7.5f && !EtiketSigiyor(l)) { boyut -= 0.25f; l.Font = Yazi(boyut, kalin && boyut >= 8.5f); }
                    }
                }
            }
        }

        private static bool EtiketSigiyor(Label l)
        {
            if (string.IsNullOrEmpty(l.Text) || l.Width < 10 || l.Height < 8) return true;
            var bayrak = TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix;
            Size olcu = TextRenderer.MeasureText(l.Text, l.Font, new Size(l.ClientSize.Width, int.MaxValue), bayrak);
            bool tekKelimeSigiyor = true;
            foreach (string k in l.Text.Split(' '))
                if (TextRenderer.MeasureText(k, l.Font).Width > l.ClientSize.Width) { tekKelimeSigiyor = false; break; }
            return olcu.Height <= l.ClientSize.Height + 1 && tekKelimeSigiyor;
        }

        /// <summary>
        /// Segoe UI, Times New Roman'dan daha geniştir. Yazı değişince etiket sağındaki kutunun üstüne
        /// biniyorsa, sığana kadar (en fazla 8 pt'ye) küçültülür.
        /// </summary>
        private static void EtiketCakismasiniOnle(Label l)
        {
            if (!l.AutoSize || l.Parent == null) return;
            Control komsu = null;
            foreach (Control c in l.Parent.Controls)
            {
                // Not: c.Visible gizli sekmelerde her zaman false döner; bu yüzden burada kullanılmaz.
                if (c == l || c is Label) continue;
                if (c.Top < l.Bottom && c.Bottom > l.Top && c.Left >= l.Left + 4)
                    if (komsu == null || c.Left < komsu.Left) komsu = c;
            }
            if (komsu == null) return;
            float boyut = l.Font.SizeInPoints;
            bool kalin = l.Font.Name.Contains("Semibold");
            while (l.Left + l.PreferredWidth > komsu.Left - 4 && boyut > 7.5f)
            {
                boyut -= 0.25f;
                l.Font = Yazi(boyut, kalin);
            }
            if (l.Left + l.PreferredWidth > komsu.Left - 4 && komsu.Left - l.Left > 30)
            {
                // Hâlâ sığmıyorsa metni "…" ile kısalt, tamamını fare üzerine gelince göster.
                string tamMetin = l.Text;
                l.AutoSize = false;
                l.AutoEllipsis = true;
                l.Width = komsu.Left - l.Left - 4;
                ipucu.SetToolTip(l, tamMetin);
            }
        }

        private static readonly ToolTip ipucu = new ToolTip();

        private static void SecenekStil(ButtonBase b)
        {
            if (b.Font != null && EskiYaziMi(b.Font)) b.Font = Yazi(Math.Max(8f, Math.Min(b.Font.SizeInPoints, 10f)), b.Font.Bold);
            if (!KoyuMu(GercekArkaPlan(b))) b.ForeColor = Metin;
            if (b.BackColor.A == 255 && (b.BackColor == SystemColors.Control || b.BackColor.GetBrightness() > 0.85f)) b.BackColor = Color.Transparent;
        }

        /// <summary>
        /// Giriş kutularının (TextBox, ComboBox, NumericUpDown, DateTimePicker) yazısını Segoe UI'ye çevirir
        /// ama kutunun YÜKSEKLİĞİNİ büyütmez: tek satırlık kutular yazı büyüdükçe uzar ve alt alta dizilmiş
        /// kutuların kenarları üst üste biner. Tarih kutularında metin genişliğe de sığdırılır.
        /// </summary>
        private static void GirdiYazisiniAyarla(Control c)
        {
            if (c.Font == null || !EskiYaziMi(c.Font)) return;
            float boyut = c.Font.SizeInPoints;
            if (boyut >= 16) { c.Font = Yazi(boyut, c.Font.Bold); return; }

            int asilYukseklik = c.Height;
            bool cokSatir = c is TextBoxBase && ((TextBoxBase)c).Multiline;
            boyut = Math.Max(8.25f, Math.Min(boyut + 1f, 10.5f));
            c.Font = Yazi(boyut);
            if (!cokSatir)
            {
                while (c.Height > asilYukseklik + 1 && boyut > 8f)
                {
                    boyut -= 0.25f;
                    c.Font = Yazi(boyut);
                }
            }

            if (c is DateTimePicker dtp)
            {
                // Uzun tarih biçimi ("16 Kasım 2026 Pazartesi") dar kutulara sığmıyordu: daha kısa ama aynı
                // bilgiyi veren biçime geç. Yalnızca görünüm değişir; kodun okuduğu Value aynıdır.
                if (dtp.Format == DateTimePickerFormat.Long)
                {
                    dtp.Format = DateTimePickerFormat.Custom;
                    dtp.CustomFormat = "dd.MM.yyyy  dddd";
                }
                // En uzun gün adına (Cumartesi) göre sığdır
                string ornek = dtp.Format == DateTimePickerFormat.Custom && !string.IsNullOrEmpty(dtp.CustomFormat)
                    ? new DateTime(2026, 12, 26).ToString(dtp.CustomFormat) : dtp.Value.ToString(dtp.Format == DateTimePickerFormat.Short ? "d" : "D");
                while (TextRenderer.MeasureText(ornek, dtp.Font).Width + 42 > dtp.Width && boyut > 7.5f)
                {
                    boyut -= 0.25f;
                    dtp.Font = Yazi(boyut);
                }
            }
        }

        private static void KutuStil(TextBoxBase t)
        {
            GirdiYazisiniAyarla(t);
            if (t.BorderStyle == BorderStyle.Fixed3D) t.BorderStyle = BorderStyle.FixedSingle;
            if (t.ReadOnly || !t.Enabled) t.BackColor = Color.FromArgb(248, 250, 252);
            else if (t.BackColor == SystemColors.Window || t.BackColor == Color.White) t.BackColor = Kart;
            if (t.ForeColor == SystemColors.WindowText || t.ForeColor == Color.Black) t.ForeColor = Metin;

            Color normal = t.BackColor;
            t.Enter += (s, e) => { if (!t.ReadOnly) t.BackColor = VurguCokAcik; };
            t.Leave += (s, e) => { if (!t.ReadOnly) t.BackColor = normal; };
        }

        private static void SecimStil(ComboBox c)
        {
            // Not: FlatStyle.Flat kullanılmaz; düz combobox kenarlığı komşu kutularla birleşip "üst üste binmiş"
            // gibi görünüyordu. Standart (System) çizim Windows 10/11'de zaten sade ve net bir kenarlık verir.
            c.FlatStyle = FlatStyle.Standard;
            GirdiYazisiniAyarla(c);
            if (c.BackColor == SystemColors.Window) c.BackColor = Kart;
            c.ForeColor = Metin;
        }

        private static void ListeStil(Control l)
        {
            if (l.Font != null && EskiYaziMi(l.Font)) l.Font = Yazi(Math.Max(8.5f, Math.Min(l.Font.SizeInPoints + 1f, 10f)));
            l.BackColor = Kart;
            l.ForeColor = Metin;
            // İnce kenarlık korunur: kenarlıksız beyaz liste, yanındaki butonlarla birleşip sınırı kayboluyordu.
            if (l is ListBox && ((ListBox)l).BorderStyle == BorderStyle.Fixed3D) ((ListBox)l).BorderStyle = BorderStyle.FixedSingle;
            else if (l is TreeView && ((TreeView)l).BorderStyle == BorderStyle.Fixed3D) ((TreeView)l).BorderStyle = BorderStyle.FixedSingle;
        }

        public static void TabloStil(DataGridView d)
        {
            try
            {
                typeof(DataGridView).InvokeMember("DoubleBuffered",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.SetProperty,
                    null, d, new object[] { true });
            }
            catch { }

            // Tablo gövdesi beyaz satırlar, boş alan çok açık gri, ince çerçeve ve koyu başlık:
            // böylece tablo sayfa zemininden net ayrılır ("her yer bembeyaz" görünmez).
            d.BackgroundColor = Color.FromArgb(248, 250, 252);
            d.BorderStyle = BorderStyle.FixedSingle;
            d.GridColor = Color.FromArgb(226, 232, 240);
            d.CellBorderStyle = DataGridViewCellBorderStyle.Single;
            d.RowHeadersVisible = false;
            d.EnableHeadersVisualStyles = false;
            d.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            d.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            d.ColumnHeadersHeight = 38;

            var baslik = d.ColumnHeadersDefaultCellStyle;
            baslik.BackColor = GrafitAcik;
            baslik.ForeColor = Color.White;
            baslik.SelectionBackColor = baslik.BackColor;
            baslik.SelectionForeColor = baslik.ForeColor;
            baslik.Font = Yazi(9.5f, true);
            baslik.Padding = new Padding(6, 0, 6, 0);

            var hucre = d.DefaultCellStyle;
            hucre.Font = Yazi(10f);
            hucre.ForeColor = Metin;
            if (hucre.BackColor.IsEmpty || hucre.BackColor == SystemColors.Window) hucre.BackColor = Kart;
            hucre.SelectionBackColor = VurguAcik;
            hucre.SelectionForeColor = Grafit;
            hucre.Padding = new Padding(6, 0, 6, 0);

            d.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(241, 245, 249);
            if (d.RowTemplate.Height < 34) d.RowTemplate.Height = 34;
            foreach (DataGridViewRow r in d.Rows) if (r.Height < 34) r.Height = 34;
        }

        private static void SekmeStil(TabControl t)
        {
            if (t.Font != null && EskiYaziMi(t.Font)) t.Font = Yazi(10f);
        }

        #endregion

        #region 🔘 MODERN BUTON ÇİZİMİ

        private class ButonDurumu { public bool Ustunde; public bool Basili; }

        private static readonly ConditionalWeakTable<Button, ButonDurumu> butonDurumlari = new ConditionalWeakTable<Button, ButonDurumu>();

        /// <summary>
        /// Butonu WinForms'un 3D çiziminden kurtarıp yuvarlak köşeli, gölgesiz, düz (web tarzı) çizer.
        /// Butonun BackColor'u her çizimde paletteki karşılığına çevrildiği için, kodun çalışma
        /// anında renk değiştirmesi (ör. bağlantı aktif = yeşil) de otomatik olarak temaya uyar.
        /// </summary>
        public static void ButonStil(Button b)
        {
            if (b.Font != null && EskiYaziMi(b.Font))
            {
                float boyut = b.Font.SizeInPoints;
                if (boyut >= 18) b.Font = Yazi(boyut, true);
                else
                {
                    // Küçük butonlarda yazı büyütülmez; metin butona sığana kadar küçültülür.
                    bool kalin = b.Font.Bold || boyut >= 11;
                    boyut = Math.Max(8.5f, Math.Min(boyut, 11f));
                    b.Font = Yazi(boyut, kalin);
                    while (boyut > 8f && !MetinSigiyor(b))
                    {
                        boyut -= 0.5f;
                        b.Font = Yazi(boyut, kalin && boyut >= 9f);
                    }
                }
            }
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.UseVisualStyleBackColor = false;
            b.Cursor = Cursors.Hand;

            var durum = butonDurumlari.GetOrCreateValue(b);
            b.MouseEnter += (s, e) => { durum.Ustunde = true; b.Invalidate(); };
            b.MouseLeave += (s, e) => { durum.Ustunde = false; durum.Basili = false; b.Invalidate(); };
            b.MouseDown += (s, e) => { durum.Basili = true; b.Invalidate(); };
            b.MouseUp += (s, e) => { durum.Basili = false; b.Invalidate(); };
            b.Paint += (s, e) => ButonCiz(b, durum, e.Graphics);
            b.Invalidate();
        }

        private static bool MetinSigiyor(Button b)
        {
            if (string.IsNullOrEmpty(b.Text) || b.Width < 20) return true;
            int genislik = b.Width - 16 - (b.Image != null ? b.Image.Width + 6 : 0);
            if (genislik < 10) return true;
            Size olcu = TextRenderer.MeasureText(b.Text, b.Font, new Size(genislik, int.MaxValue), TextFormatFlags.WordBreak);
            bool tekKelimeTasiyor = false;
            foreach (string kelime in b.Text.Split(' '))
                if (TextRenderer.MeasureText(kelime, b.Font).Width > genislik) { tekKelimeTasiyor = true; break; }
            return olcu.Height <= b.Height - 4 && !tekKelimeTasiyor;
        }

        private static bool BirincilMetinMi(string metin)
        {
            if (string.IsNullOrEmpty(metin)) return false;
            string m = metin.ToLower(new System.Globalization.CultureInfo("tr-TR"));
            return m.Contains("kaydet") || m.Contains("yazdır") || m.Contains("giriş") || m.Contains("uygula");
        }

        private static void ButonCiz(Button b, ButonDurumu durum, Graphics g)
        {
            Color zemin = GercekArkaPlan(b.Parent);
            g.Clear(zemin);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Color? anlam = AnlamRengi(b.BackColor);
            bool ikincil = !anlam.HasValue;
            Color dolgu, yazi, kenar;

            if (ikincil && BirincilMetinMi(b.Text) && !KoyuMu(zemin))
            {
                anlam = Vurgu;
                ikincil = false;
            }

            if (ikincil)
            {
                bool koyuZemin = KoyuMu(zemin);
                dolgu = koyuZemin ? GrafitAcik : Kart;
                yazi = koyuZemin ? Color.White : Metin;
                kenar = koyuZemin ? Color.FromArgb(55, 65, 81) : Color.FromArgb(203, 213, 225);
                if (durum.Ustunde) { dolgu = koyuZemin ? Color.FromArgb(55, 65, 81) : Color.FromArgb(241, 245, 249); kenar = koyuZemin ? kenar : Vurgu; yazi = koyuZemin ? yazi : VurguKoyu; }
                if (durum.Basili) dolgu = koyuZemin ? Grafit : Color.FromArgb(226, 232, 240);
            }
            else
            {
                dolgu = anlam.Value;
                yazi = Color.White;
                if (dolgu == Sari) yazi = Grafit;
                kenar = Color.Empty;
                if (durum.Ustunde) dolgu = Karistir(dolgu, Color.Black, 0.12f);
                if (durum.Basili) dolgu = Karistir(dolgu, Color.Black, 0.22f);
            }

            if (!b.Enabled)
            {
                dolgu = Karistir(dolgu, zemin, 0.55f);
                yazi = Karistir(yazi, zemin, 0.45f);
                if (!kenar.IsEmpty) kenar = Karistir(kenar, zemin, 0.5f);
            }

            // 2 px iç boşluk: tasarımda bitişik duran butonlar arasında görsel aralık oluşur
            // (eski 3D butonlar bu boşluğu kendi kenarlıklarıyla veriyordu).
            float bosluk = (b.Width > 40 && b.Height > 24) ? 2f : 0.5f;
            float yaricap = Math.Min(9f, (b.Height - bosluk * 2) / 2f);
            var alan = new RectangleF(bosluk, bosluk, b.Width - bosluk * 2 - 1, b.Height - bosluk * 2 - 1);
            using (var yol = YuvarlakDikdortgen(alan, yaricap))
            {
                using (var fircha = new SolidBrush(dolgu)) g.FillPath(fircha, yol);
                if (!kenar.IsEmpty)
                    using (var kalem = new Pen(kenar, 1f)) g.DrawPath(kalem, yol);
            }

            Rectangle metinAlani = new Rectangle(8, 2, b.Width - 16, b.Height - 4);
            if (b.Image != null)
            {
                int iw = b.Image.Width, ih = b.Image.Height;
                Size metinBoyu = TextRenderer.MeasureText(b.Text ?? "", b.Font);
                int toplam = iw + (string.IsNullOrEmpty(b.Text) ? 0 : 6 + metinBoyu.Width);
                int x = Math.Max(6, (b.Width - toplam) / 2);
                g.DrawImage(b.Image, x, (b.Height - ih) / 2, iw, ih);
                metinAlani = new Rectangle(x + iw + 6, 2, b.Width - (x + iw + 6) - 6, b.Height - 4);
                TextRenderer.DrawText(g, b.Text, b.Font, metinAlani, yazi, dolgu,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis);
                return;
            }

            // Tek karakterlik simge butonları (☰, ⓘ) için iç boşluk ve "…" kısaltması kullanılmaz
            if (b.Text != null && b.Text.Length <= 2)
            {
                TextRenderer.DrawText(g, b.Text, b.Font, new Rectangle(0, 0, b.Width, b.Height), yazi, dolgu,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                return;
            }
            TextRenderer.DrawText(g, b.Text, b.Font, metinAlani, yazi, dolgu,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis);
        }

        #endregion

        #region 🗂️ SEKME BAŞLIKLARINI GİZLEME (MENÜ İLE YÖNETİLEN SEKMELER İÇİN)

        /// <summary>
        /// TabControl'ün başlık şeridini ve çerçevesini tamamen gizler; sayfalar kodla değiştirilmeye
        /// devam eder (SelectedTab, Selecting, TabPages.Remove vb. aynen çalışır).
        /// </summary>
        public static void BasliklariGizle(TabControl t)
        {
            var gizleyici = new SekmeGizleyici();
            if (t.IsHandleCreated) gizleyici.AssignHandle(t.Handle);
            t.HandleCreated += (s, e) => gizleyici.AssignHandle(t.Handle);
            t.HandleDestroyed += (s, e) => gizleyici.ReleaseHandle();
            t.Appearance = TabAppearance.Normal;
            t.Padding = new Point(0, 0);
            // Gizli şeridin arka planda çizilmeye devam etmemesi için sekme düğmelerini sıfır boyuta indir
            t.DrawMode = TabDrawMode.Normal;
            t.SizeMode = TabSizeMode.Fixed;
            t.ItemSize = new Size(0, 1);
            t.PerformLayout();
            foreach (TabPage p in t.TabPages) p.Padding = new Padding(0);
        }

        private class SekmeGizleyici : NativeWindow
        {
            private const int TCM_ADJUSTRECT = 0x1328;
            protected override void WndProc(ref Message m)
            {
                if (m.Msg == TCM_ADJUSTRECT) { m.Result = (IntPtr)1; return; }
                base.WndProc(ref m);
            }
        }

        #endregion
    }

    /// <summary>Yuvarlak köşeli beyaz kart görünümlü panel (Ana Panel, menü vb. için).</summary>
    public class KartPanel : Panel
    {
        public int Yaricap { get; set; } = 14;
        public Color KartRengi { get; set; } = Tema.Kart;
        public Color KenarRengi { get; set; } = Tema.Cizgi;
        /// <summary>Boş değilse kart, KartRengi'nden bu renge yatay geçişli boyanır.</summary>
        public Color GecisRengi { get; set; } = Color.Empty;

        public KartPanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Tag = Tema.TemaYok;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var r = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
            using (var yol = Tema.YuvarlakDikdortgen(r, Yaricap))
            {
                if (GecisRengi.IsEmpty)
                {
                    using (var b = new SolidBrush(KartRengi)) e.Graphics.FillPath(b, yol);
                }
                else if (Width > 1 && Height > 1)
                {
                    using (var b = new LinearGradientBrush(new Rectangle(0, 0, Width, Height), KartRengi, GecisRengi, 20f))
                        e.Graphics.FillPath(b, yol);
                }
                if (!KenarRengi.IsEmpty) using (var p = new Pen(KenarRengi)) e.Graphics.DrawPath(p, yol);
            }
            base.OnPaint(e);
        }
    }
}
