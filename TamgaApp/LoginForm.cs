using AutoUpdaterDotNET;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static TamgaApp.DataAccess;

namespace TamgaApp
{
    /// <summary>
    /// Sistemin ilk açılışında kimlik doğrulamasını, God Mode girişlerini ve süre/şifre kontrollerini yapan ana giriş formudur.
    /// </summary>
    public partial class LoginForm : Form
    {
        public LoginForm()
        {
            InitializeComponent();
        }

        #region 🔑 1. ANA GİRİŞ MOTORU (Kullanıcı Doğrulama ve Yönetici Kodları)

        /// <summary>Giriş butonuna tıklandığında çalışır. Sistemdeki en kritik güvenlik kontrol kapısıdır.</summary>
        private void btnGiris_Click(object sender, EventArgs e)
        {
            string kAdi = txtKullaniciAdi.Text.Trim();
            string sifre = txtSifre.Text.Trim();

            if (string.IsNullOrEmpty(kAdi) || string.IsNullOrEmpty(sifre)) return;

            // ===================================================================
            // 🚀 SİHİRLİ GOD MODE (KURUCU HESABI - ACİL DURUM ARKA KAPISI) 
            // ===================================================================
            if (kAdi == "TamgaApp" && sifre == "ugur-19Yalcin.97")
            {
                MainForm.AktifKullaniciAdi = "Kurucu (TamgaApp)";
                MainForm.AktifYetkiler = "Sınırsız";

                // 🌟 AFİLLİ KARŞILAMA MESAJI
                MessageBox.Show("Sisteme Hoş Geldin Patron! 😎\n\n[ ⚡ GOD MODE AKTİF ⚡ ]\nBütün güvenlik duvarları aşıldı, tüm kilitler açıldı. Sistemin tam kontrolü artık sende!", "Sistem Bildirimi: YÜCE YETKİ", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);

                this.DialogResult = DialogResult.OK; // Kapıyı aç!
                this.Close();
                return;
            }

            // ===================================================================
            // 👔 GÜNLÜK YÖNETİCİ HESABI (Normal Patron Kullanımı)
            // ===================================================================
            if (kAdi == "Yönetici" && sifre == "UY.19-97")
            {
                MainForm.AktifKullaniciAdi = "Yönetici";
                MainForm.AktifYetkiler = "Sınırsız";

                MessageBox.Show("Hoş Geldiniz Yönetici Bey. Tüm departmanlar rapor vermeye hazır.", "Yönetici Girişi", MessageBoxButtons.OK, MessageBoxIcon.Information);

                this.DialogResult = DialogResult.OK;
                this.Close();
                return;
            }

            // ===================================================================
            // 👤 NORMAL KULLANICI KONTROLÜ (Veritabanı Doğrulaması)
            // ===================================================================
            Kullanici giren = DataAccess.GetKullaniciGiris(kAdi, sifre);

            if (giren != null)
            {
                // ⏳ 1. SÜRE KONTROLÜ
                if (giren.BitisTarihi.HasValue && giren.BitisTarihi.Value < DateTime.Now)
                {
                    DialogResult cevap = MessageBox.Show($"Sayın {giren.KullaniciAdi}, hesabınızın kullanım süresi dolmuştur!\nYönetici çağırarak süreyi uzatmak ister misiniz?", "Süre Bitti", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                    if (cevap == DialogResult.Yes)
                    {
                        if (YoneticiIleSureUzat(giren.KullaniciAdi))
                        {
                            MessageBox.Show("Süreniz başarıyla uzatıldı! Lütfen tekrar giriş yapınız.", "Başarılı");
                        }
                    }
                    txtSifre.Clear();
                    return;
                }

                // 🔒 2. ŞİFRE ESKİME KONTROLÜ (Süresi dolan şifreyi zorla yeniletme)
                if (giren.SifreGecerlilikAyi > 0 && giren.SonSifreDegistirme.AddMonths(giren.SifreGecerlilikAyi) < DateTime.Now)
                {
                    MessageBox.Show("Şifrenizin geçerlilik süresi dolmuştur. Güvenliğiniz için yeni bir şifre belirlemelisiniz.", "Şifre Yenileme", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    if (KullaniciKendiSifresiniDegistir(giren.KullaniciAdi))
                    {
                        MessageBox.Show("Şifreniz başarıyla yenilendi! Lütfen yeni şifrenizle giriş yapınız.", "Başarılı");
                    }
                    txtSifre.Clear();
                    return;
                }

                // 🎉 GİRİŞ BAŞARILI
                MainForm.AktifKullaniciAdi = giren.KullaniciAdi;
                MainForm.AktifYetkiler = giren.Yetkiler;

                this.DialogResult = DialogResult.OK; // Ana programa sinyali çak
                this.Close(); // Login ekranını devreden çıkar
            }
            else
            {
                MessageBox.Show("Hatalı kullanıcı adı veya şifre girdiniz!", "Giriş Başarısız", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        // =========================================================================================

        #region 🛡️ 2. DİNAMİK YETKİLENDİRME VE YENİLEME PENCERELERİ (KOD İLE ÜRETİLEN FORMLAR)

        /// <summary>
        /// Kullanıcının süresi bittiğinde ekrana anında geçici bir "Yönetici Şifre İsteme" penceresi (formu) çizen metot.
        /// </summary>
        private bool YoneticiIleSureUzat(string sureUzatilacakKullanici)
        {
            Form adminForm = new Form { Width = 300, Height = 250, FormBorderStyle = FormBorderStyle.FixedDialog, Text = "Yönetici Onayı", StartPosition = FormStartPosition.CenterParent };

            TextBox txtAdminAd = new TextBox { Left = 20, Top = 30, Width = 240 };
            TextBox txtAdminPass = new TextBox { Left = 20, Top = 80, Width = 240, PasswordChar = '*' };

            ComboBox cmbSure = new ComboBox { Left = 20, Top = 130, Width = 240, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbSure.Items.AddRange(new string[] { "1 Saat Uzat", "1 Ay Uzat", "3 Ay Uzat", "6 Ay Uzat", "Sınırsız Yap" });
            cmbSure.SelectedIndex = 1;

            Button btnOnay = new Button { Text = "UZAT", Left = 20, Width = 240, Top = 170, BackColor = Color.Green, ForeColor = Color.White };

            adminForm.Controls.Add(new Label { Text = "Yönetici Kullanıcı Adı:", Left = 20, Top = 10, Width = 240 });
            adminForm.Controls.Add(txtAdminAd);
            adminForm.Controls.Add(new Label { Text = "Yönetici Şifresi:", Left = 20, Top = 60, Width = 240 });
            adminForm.Controls.Add(txtAdminPass);
            adminForm.Controls.Add(new Label { Text = "Uzatılacak Süre:", Left = 20, Top = 110, Width = 240 });
            adminForm.Controls.Add(cmbSure);
            adminForm.Controls.Add(btnOnay);

            bool islemTamam = false;

            btnOnay.Click += (s, e) =>
            {
                if ((txtAdminAd.Text == "Yönetici" && txtAdminPass.Text == "UY.19-97") || (txtAdminAd.Text == "TamgaApp" && txtAdminPass.Text == "ugur-19Yalcin.97"))
                {
                    DateTime yeniTarih = DateTime.Now;
                    if (cmbSure.SelectedIndex == 0) yeniTarih = yeniTarih.AddHours(1);
                    else if (cmbSure.SelectedIndex == 1) yeniTarih = yeniTarih.AddMonths(1);
                    else if (cmbSure.SelectedIndex == 2) yeniTarih = yeniTarih.AddMonths(3);
                    else if (cmbSure.SelectedIndex == 3) yeniTarih = yeniTarih.AddMonths(6);
                    else yeniTarih = DateTime.MaxValue; // Sınırsız

                    DataAccess.KullaniciSuresiUzat(sureUzatilacakKullanici, cmbSure.SelectedIndex == 4 ? DateTime.MaxValue : yeniTarih);
                    islemTamam = true;
                    adminForm.Close();
                }
                else MessageBox.Show("Yetkisiz Giriş!", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };

            adminForm.ShowDialog();
            return islemTamam;
        }

        /// <summary>
        /// Kullanıcıya "Eski şifreni değiştir" uyarısı geldiğinde (veya adminden tetiklendiğinde) ekrana anında geçici bir şifre yenileme penceresi çizen metot.
        /// </summary>
        public static bool KullaniciKendiSifresiniDegistir(string kAdi)
        {
            Form sifreForm = new Form { Width = 300, Height = 180, FormBorderStyle = FormBorderStyle.FixedDialog, Text = "Yeni Şifre Belirle", StartPosition = FormStartPosition.CenterParent };

            TextBox txtYeniSifre1 = new TextBox { Left = 20, Top = 30, Width = 240, PasswordChar = '*' };
            TextBox txtYeniSifre2 = new TextBox { Left = 20, Top = 80, Width = 240, PasswordChar = '*' };
            Button btnOnay = new Button { Text = "Şifremi Değiştir", Left = 20, Width = 240, Top = 110, BackColor = Color.Blue, ForeColor = Color.White };

            sifreForm.Controls.Add(new Label { Text = "Yeni Şifre:", Left = 20, Top = 10, Width = 240 });
            sifreForm.Controls.Add(txtYeniSifre1);
            sifreForm.Controls.Add(new Label { Text = "Yeni Şifre (Tekrar):", Left = 20, Top = 60, Width = 240 });
            sifreForm.Controls.Add(txtYeniSifre2);
            sifreForm.Controls.Add(btnOnay);

            bool islemTamam = false;

            btnOnay.Click += (s, e) =>
            {
                if (txtYeniSifre1.Text.Length < 3)
                    MessageBox.Show("Şifre en az 3 karakter olmalı.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                else if (txtYeniSifre1.Text != txtYeniSifre2.Text)
                    MessageBox.Show("Şifreler uyuşmuyor!", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                else
                {
                    DataAccess.KullaniciSifreDegistir(kAdi, txtYeniSifre1.Text);
                    islemTamam = true;
                    sifreForm.Close();
                }
            };

            sifreForm.ShowDialog();
            return islemTamam;
        }

        #endregion

        private void LoginForm_Load(object sender, EventArgs e)
        {
            // 1. Ekrana o anki gerçek sürümü yazdırıyoruz!
            lblVersiyon.Text = "Sürüm: " + Application.ProductVersion;

            // 2. VIP Kapı (Güvenlik Duvarını Aşmak İçin)
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

            // 3. AutoUpdater'ı Çalıştır
            AutoUpdater.Start("https://raw.githubusercontent.com/Yalcin-Soft/TamgaApp-Updates/refs/heads/main/update.xml");
        }
    }
}