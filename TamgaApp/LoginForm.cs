using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using static TamgaApp.DataAccess;

namespace TamgaApp
{
    /// <summary>
    /// Giriş ekranı: kimlik doğrulama, sistem hesapları (kurucu / yönetici acil erişimi), hesap süresi ve
    /// şifre eskime kontrolleri, deneme sınırlama ve "Beni hatırla". Görsel kısım LoginForm.Tasarim.cs'de.
    /// </summary>
    public partial class LoginForm : Form
    {
        #region 📐 01. API TANIMLAMALARI

        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wp, int lp);

        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect, int nWidthEllipse, int nHeightEllipse);

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;

        #endregion

        #region ⚙️ 02. BAŞLANGIÇ

        public LoginForm()
        {
            InitializeComponent();
            this.DoubleBuffered = true;
            ModernTasarimiKur();
            this.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, this.Width + 1, this.Height + 1, 28, 28));
        }

        private void LoginForm_Load(object sender, EventArgs e)
        {
            lblVersiyon.Text = "Sürüm " + Guncelleme.KuruluSurum;

            // Modern güncelleme merkezi: sessiz denetim. Zorunlu sürüm varsa pencere kendiliğinden açılır.
            Guncelleme.DurumDegisti += GuncellemeDurumunuGoster;
            this.FormClosed += (s, a) => Guncelleme.DurumDegisti -= GuncellemeDurumunuGoster;
            Guncelleme.Denetle(true);
            GuncellemeDurumunuGoster();

            // BENİ HATIRLA (şifre Windows DPAPI ile korunur)
            if (Properties.Settings.Default.BeniHatirla)
            {
                txtKullaniciAdi.Text = Properties.Settings.Default.HatirlaKullanici;
                txtSifre.Text = Kripto.Coz(Properties.Settings.Default.HatirlaSifre);
                chkBeniHatirla.Checked = true;
            }

            KilitDurumunuGuncelle();
            BeginInvoke(new Action(() => { if (string.IsNullOrEmpty(txtKullaniciAdi.Text)) txtKullaniciAdi.Focus(); else btnGiris.Focus(); }));
        }

        private void LoginForm_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(this.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        #endregion

        #region 🔑 03. GİRİŞ MOTORU

        private void BeniHatirlaAyariniKaydet(string kAdi, string sifre)
        {
            var a = Properties.Settings.Default;
            if (chkBeniHatirla.Checked)
            {
                a.BeniHatirla = true;
                a.HatirlaKullanici = kAdi;
                try { a.HatirlaSifre = Kripto.Sifrele(sifre); } catch { a.HatirlaSifre = ""; }
            }
            else
            {
                a.BeniHatirla = false;
                a.HatirlaKullanici = "";
                a.HatirlaSifre = "";
            }
            a.Save();
        }

        private void GirisiTamamla(string gorunenAd, string yetkiler, Guvenlik.SistemHesabi sistemHesabi, string kAdi, string sifre, string karsilama)
        {
            Guvenlik.BasariliGiris();
            MainForm.AktifKullaniciAdi = gorunenAd;
            MainForm.AktifYetkiler = yetkiler;
            MainForm.SistemHesabiOturumu = sistemHesabi;
            MainForm.AcilisKarsilamasi = karsilama;
            BeniHatirlaAyariniKaydet(kAdi, sifre);
            Gunluk.Yaz("Oturum", "Giriş yapıldı", sistemHesabi == Guvenlik.SistemHesabi.Yok ? "" : "Sistem hesabı: " + sistemHesabi, gorunenAd);
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        /// <summary>Giriş butonu (ve Enter tuşu). Sistemdeki en kritik güvenlik kapısıdır.</summary>
        private void btnGiris_Click(object sender, EventArgs e)
        {
            HatayiGizle();
            if (Guvenlik.KalanKilitSuresi > TimeSpan.Zero) { KilitDurumunuGuncelle(); return; }

            string kAdi = txtKullaniciAdi.Text.Trim();
            string sifre = txtSifre.Text.Trim();

            if (string.IsNullOrEmpty(kAdi) || string.IsNullOrEmpty(sifre))
            {
                HataGoster("Lütfen kullanıcı adı ve şifrenizi girin.");
                (string.IsNullOrEmpty(kAdi) ? (Control)txtKullaniciAdi : txtSifre).Focus();
                return;
            }

            // ===================================================================
            // 🛡️ SİSTEM HESAPLARI (KURUCU / YÖNETİCİ) — her zaman açık acil erişim.
            // Şifreler kodda değil; Guvenlik.cs'deki tuzlu PBKDF2 özetleriyle doğrulanır.
            // ===================================================================
            var sistem = Guvenlik.SistemHesabiDogrula(kAdi, sifre);
            if (sistem == Guvenlik.SistemHesabi.Kurucu)
            {
                GirisiTamamla("Kurucu (TamgaApp)", "Sınırsız", sistem, kAdi, sifre,
                    "Sisteme hoş geldin Patron! 😎  GOD MODE aktif — tüm kilitler açık.");
                return;
            }
            if (sistem == Guvenlik.SistemHesabi.Yonetici)
            {
                GirisiTamamla("Yönetici", "Sınırsız", sistem, kAdi, sifre,
                    "Hoş geldiniz Yönetici Bey. Tüm departmanlar rapor vermeye hazır.");
                return;
            }

            // ===================================================================
            // 👤 VERİTABANI KULLANICISI
            // ===================================================================
            Kullanici giren;
            try { giren = DataAccess.GetKullaniciGiris(kAdi, sifre); }
            catch (Exception ex) { HataGoster("Veritabanına ulaşılamadı: " + ex.Message); return; }

            if (giren == null)
            {
                Guvenlik.HataliGirisKaydet();
                Gunluk.Yaz("Oturum", "Hatalı giriş denemesi", "Kullanıcı adı: " + kAdi, "-");
                txtSifre.SelectAll();
                txtSifre.Focus();
                if (Guvenlik.KalanKilitSuresi > TimeSpan.Zero)
                {
                    Gunluk.Yaz("Oturum", "Giriş geçici olarak kilitlendi", "Çok sayıda hatalı deneme", "-");
                    KilitDurumunuGuncelle();
                }
                else
                {
                    int kalan = Guvenlik.KalanDeneme;
                    HataGoster("Kullanıcı adı veya şifre hatalı." + (kalan > 0 && kalan <= 3 ? $"  ({kalan} deneme hakkınız kaldı)" : ""));
                }
                Salla();
                return;
            }

            // ⏳ 1. HESAP SÜRESİ KONTROLÜ
            if (giren.BitisTarihi.HasValue && giren.BitisTarihi.Value < DateTime.Now)
            {
                DialogResult cevap = MessageBox.Show($"Sayın {giren.KullaniciAdi}, hesabınızın kullanım süresi dolmuştur!\nYönetici çağırarak süreyi uzatmak ister misiniz?", "Süre Bitti", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (cevap == DialogResult.Yes && YoneticiIleSureUzat(giren.KullaniciAdi))
                {
                    Gunluk.Yaz("Kullanıcı Yönetimi", "Hesap süresi uzatıldı", giren.KullaniciAdi);
                    HataGoster("Süreniz uzatıldı. Lütfen tekrar giriş yapın.", true);
                }
                txtSifre.Clear();
                return;
            }

            // 🔒 2. ŞİFRE ESKİME KONTROLÜ
            if (giren.SifreGecerlilikAyi > 0 && giren.SonSifreDegistirme.AddMonths(giren.SifreGecerlilikAyi) < DateTime.Now)
            {
                MessageBox.Show("Şifrenizin geçerlilik süresi dolmuştur. Güvenliğiniz için yeni bir şifre belirlemelisiniz.", "Şifre Yenileme", MessageBoxButtons.OK, MessageBoxIcon.Information);
                if (KullaniciKendiSifresiniDegistir(giren.KullaniciAdi))
                {
                    Gunluk.Yaz("Oturum", "Süresi dolan şifre yenilendi", giren.KullaniciAdi, giren.KullaniciAdi);
                    HataGoster("Şifreniz yenilendi. Yeni şifrenizle giriş yapın.", true);
                }
                txtSifre.Clear();
                return;
            }

            // 🎉 GİRİŞ BAŞARILI
            GirisiTamamla(giren.KullaniciAdi, giren.Yetkiler, Guvenlik.SistemHesabi.Yok, kAdi, sifre, null);
        }

        #endregion

        #region 🛡️ 04. YÖNETİCİ ONAYI VE ŞİFRE YENİLEME PENCERELERİ

        /// <summary>Süresi dolan kullanıcı için sistem hesabı onayıyla süre uzatma penceresi.</summary>
        private bool YoneticiIleSureUzat(string sureUzatilacakKullanici)
        {
            Form adminForm = new Form { Width = 320, Height = 290, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false, Text = "Yönetici Onayı", StartPosition = FormStartPosition.CenterParent };

            TextBox txtAdminAd = new TextBox { Left = 20, Top = 36, Width = 264 };
            TextBox txtAdminPass = new TextBox { Left = 20, Top = 92, Width = 264, UseSystemPasswordChar = true };
            ComboBox cmbSure = new ComboBox { Left = 20, Top = 148, Width = 264, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbSure.Items.AddRange(new string[] { "1 Saat Uzat", "1 Ay Uzat", "3 Ay Uzat", "6 Ay Uzat", "Sınırsız Yap" });
            cmbSure.SelectedIndex = 1;
            Button btnOnay = new Button { Text = "Süreyi Uzat", Left = 20, Width = 264, Top = 192, Height = 40, BackColor = Color.SeaGreen };

            adminForm.Controls.Add(new Label { Text = "Yönetici kullanıcı adı", Left = 20, Top = 14, Width = 264 });
            adminForm.Controls.Add(txtAdminAd);
            adminForm.Controls.Add(new Label { Text = "Yönetici şifresi", Left = 20, Top = 70, Width = 264 });
            adminForm.Controls.Add(txtAdminPass);
            adminForm.Controls.Add(new Label { Text = "Uzatılacak süre", Left = 20, Top = 126, Width = 264 });
            adminForm.Controls.Add(cmbSure);
            adminForm.Controls.Add(btnOnay);
            adminForm.AcceptButton = btnOnay;

            bool islemTamam = false;
            btnOnay.Click += (s, e) =>
            {
                if (Guvenlik.SistemHesabiDogrula(txtAdminAd.Text.Trim(), txtAdminPass.Text.Trim()) != Guvenlik.SistemHesabi.Yok)
                {
                    DateTime yeniTarih = DateTime.Now;
                    if (cmbSure.SelectedIndex == 0) yeniTarih = yeniTarih.AddHours(1);
                    else if (cmbSure.SelectedIndex == 1) yeniTarih = yeniTarih.AddMonths(1);
                    else if (cmbSure.SelectedIndex == 2) yeniTarih = yeniTarih.AddMonths(3);
                    else if (cmbSure.SelectedIndex == 3) yeniTarih = yeniTarih.AddMonths(6);
                    else yeniTarih = DateTime.MaxValue;

                    DataAccess.KullaniciSuresiUzat(sureUzatilacakKullanici, yeniTarih);
                    islemTamam = true;
                    adminForm.Close();
                }
                else
                {
                    Gunluk.Yaz("Oturum", "Hatalı yönetici onayı denemesi", "Süre uzatma: " + sureUzatilacakKullanici, "-");
                    MessageBox.Show("Yönetici bilgileri hatalı!", "Yetkisiz", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            adminForm.ShowDialog(this);
            return islemTamam;
        }

        /// <summary>Kullanıcının yeni şifre belirlemesi (şifre eskidiğinde veya yönetici sıfırlarken).</summary>
        public static bool KullaniciKendiSifresiniDegistir(string kAdi)
        {
            Form sifreForm = new Form { Width = 320, Height = 250, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false, Text = "Yeni Şifre Belirle — " + kAdi, StartPosition = FormStartPosition.CenterParent };

            TextBox txtYeniSifre1 = new TextBox { Left = 20, Top = 36, Width = 264, UseSystemPasswordChar = true };
            TextBox txtYeniSifre2 = new TextBox { Left = 20, Top = 92, Width = 264, UseSystemPasswordChar = true };
            Label lblKural = new Label { Text = "En az 6 karakter, harf ve rakam içermeli.", Left = 20, Top = 120, Width = 264, ForeColor = Color.Gray };
            Button btnOnay = new Button { Text = "Şifremi Değiştir", Left = 20, Width = 264, Top = 150, Height = 40, BackColor = Color.SeaGreen };

            sifreForm.Controls.Add(new Label { Text = "Yeni şifre", Left = 20, Top = 14, Width = 264 });
            sifreForm.Controls.Add(txtYeniSifre1);
            sifreForm.Controls.Add(new Label { Text = "Yeni şifre (tekrar)", Left = 20, Top = 70, Width = 264 });
            sifreForm.Controls.Add(txtYeniSifre2);
            sifreForm.Controls.Add(lblKural);
            sifreForm.Controls.Add(btnOnay);
            sifreForm.AcceptButton = btnOnay;

            bool islemTamam = false;
            btnOnay.Click += (s, e) =>
            {
                string yeni = txtYeniSifre1.Text.Trim();
                string kural = Guvenlik.SifreKuraliHatasi(yeni);
                if (kural != null)
                    MessageBox.Show(kural, "Zayıf Şifre", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                else if (yeni != txtYeniSifre2.Text.Trim())
                    MessageBox.Show("Şifreler uyuşmuyor!", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                else
                {
                    DataAccess.KullaniciSifreDegistir(kAdi, yeni);
                    islemTamam = true;
                    sifreForm.Close();
                }
            };

            sifreForm.ShowDialog();
            return islemTamam;
        }

        #endregion
    }
}
