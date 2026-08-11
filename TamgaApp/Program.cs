using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace TamgaApp
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // 🌟 1. UI (Arayüz) İçi Hataları Yakalayan Küresel Zırh
            Application.ThreadException += new ThreadExceptionEventHandler(GlobalThreadException);

            // 🌟 2. Arka Plan ve Thread İçi Hataları Yakalayan Küresel Zırh
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(GlobalUnhandledException);

            // KRİTİK: Veritabanı kontrolü Login ekranından ÖNCE yapılmalı ki kullanıcıyı doğrulayabilelim!
            try
            {
                DbHelper.EnsureDatabase();
            }
            catch (Exception ex)
            {
                HataEkraniniGoster(ex, "Veritabanı Başlatma (DbHelper) Hatası");
                return; // Veritabanı yoksa programı devam ettirme
            }

            #region 🛠️ GELİŞTİRİCİ (GOD MODE) BYPASS
            // Geliştirme yaparken şifre ekranını atlamak için aşağıdaki 4 satırın başındaki "//" işaretlerini silin.
            // Müşteriye teslim etmeden (Canlıya almadan) önce mutlaka tekrar "//" koyarak gizleyin!

            // MainForm.AktifKullaniciAdi = "Geliştirici (Patron)"; // Adını zorla yazdırdık
            // MainForm.AktifYetkiler = "Sınırsız";                 // Tüm yetkileri zorla verdik
            // Application.Run(new MainForm());                     // Direkt ana ekranı açtık
            // return;                                              // Geliştirici modundayken aşağıdaki Splash/Login kısmının çalışmasını engeller!
            #endregion

            // =========================================================================================

            #region 🔒 AKILLI SIRALAMA (SPLASH -> LOGIN -> MAIN)

            // ADIM 1: Önce Açılış Ekranını (Logoyu) diyalog olarak başlatıyoruz
            SplashForm splash = new SplashForm();
            splash.ShowDialog(); // Logo saniyesi dolana kadar ekranda kalacak ve kendi kapanacak.

            // ADIM 2: Logo kapandıktan sonra Giriş Ekranını (LoginForm) açıyoruz
            LoginForm login = new LoginForm();

            // Eğer kullanıcı doğru şifre girip giriş yaparsa (DialogResult.OK dönerse)
            if (login.ShowDialog() == DialogResult.OK)
            {
                // ADIM 3: Artık uygulamanın asıl kalbi olan ANA EKRANI (MainForm) başlatıyoruz!
                Application.Run(new MainForm());
            }
            else
            {
                // Kullanıcı şifre ekranında giriş yapmaktan vazgeçip X'e basarsa programı tamamen kapat
                Application.Exit();
            }

            #endregion
        }

        // =========================================================================================
        // 🛡️ KÜRESEL HATA YAKALAMA MOTORLARI VE EKRAN ÇİZİMİ
        // =========================================================================================

        private static void GlobalThreadException(object sender, ThreadExceptionEventArgs e)
        {
            HataEkraniniGoster(e.Exception, "Arayüz Hatası (UI Exception)");
        }

        private static void GlobalUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = e.ExceptionObject as Exception;
            HataEkraniniGoster(ex, "Kritik Sistem Hatası (Unhandled Exception)");
        }

        private static void HataEkraniniGoster(Exception ex, string hataTuru)
        {
            string hataMesaji = ex != null ? ex.Message : "Bilinmeyen bir hata oluştu.";
            string hataDetayi = ex != null ? ex.StackTrace : "Stacktrace bilgisi bulunamadı.";

            Form frmHata = new Form
            {
                Text = "⚠️ TamgaApp - Kritik Sistem Hatası",
                Size = new Size(600, 450),
                StartPosition = FormStartPosition.CenterScreen,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.WhiteSmoke,
                TopMost = true
            };

            Label lblBaslik = new Label
            {
                Text = $"❌ Programda beklenmeyen bir hata oluştu!\nTür: {hataTuru}",
                Location = new Point(20, 20),
                Size = new Size(540, 50),
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.DarkRed
            };

            TextBox txtDetay = new TextBox
            {
                Text = $"HATA MESAJI:\r\n{hataMesaji}\r\n\r\nTEKNİK DETAY (STACK TRACE):\r\n{hataDetayi}",
                Location = new Point(20, 80),
                Size = new Size(540, 250),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 9),
                BackColor = Color.White,
                ForeColor = Color.Black
            };

            Button btnKapat = new Button
            {
                Text = "Programı Kapat",
                Location = new Point(430, 350),
                Size = new Size(130, 40),
                BackColor = Color.DarkRed,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };

            btnKapat.Click += (s, args) => { frmHata.Close(); Environment.Exit(1); };

            frmHata.Controls.Add(lblBaslik);
            frmHata.Controls.Add(txtDetay);
            frmHata.Controls.Add(btnKapat);

            // Eğer program o an kilitliyse hata penceresi gömülü kalmasın diye en üste çağırıyoruz
            frmHata.ShowDialog();
        }
    }
}