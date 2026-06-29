using System;
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

            // KRİTİK: Veritabanı kontrolü Login ekranından ÖNCE yapılmalı ki kullanıcıyı doğrulayabilelim!
            DbHelper.EnsureDatabase();

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
    }
}