using System;
using System.Windows.Forms;

namespace TamgaApp
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            #region 🚀 1. GÖRSEL TEMEL AYARLAR VE MOTOR ATEŞLEME

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            MainForm.AktifKullaniciAdi = "Geliştirici (Patron)"; // Adını zorla yazdırdık
            MainForm.AktifYetkiler = "Sınırsız";                 // Tüm yetkileri zorla verdik
            Application.Run(new MainForm());                     // Direkt ana ekranı açtık

            // Veritabanı kontrolü
            DbHelper.EnsureDatabase();

            #endregion

            // =========================================================================================

            #region 🔒 2. AKILLI SIRALAMA (SPLASH -> LOGIN -> MAIN)

            // ADIM 1: Önce Açılış Ekranını (Logoyu) diyalog olarak başlatıyoruz
           // SplashForm splash = new SplashForm();
            //splash.ShowDialog(); // Logo 3 saniye boyunca ekranda kalacak ve kendi kapanacak.

            // ADIM 2: Logo kapandıktan sonra Giriş Ekranını (LoginForm) açıyoruz
            //LoginForm login = new LoginForm();

            // Eğer kullanıcı doğru şifre girip giriş yaparsa (DialogResult.OK dönerse)
            //if (login.ShowDialog() == DialogResult.OK)
            //{
                // ADIM 3: Artık uygulamanın asıl kalbi olan ANA EKRANI (MainForm) başlatıyoruz!
              //  Application.Run(new MainForm());
            //}
            //else
            //{
                // Kullanıcı şifre ekranında X'e basarsa programı tamamen kapat
              //  Application.Exit();
            //}

            #endregion
        }
    }
}