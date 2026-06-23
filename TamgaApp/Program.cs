using System;
using System.Windows.Forms;
// using SQLitePCL; // (Eğer kullanmıyorsan bu satırı silebilirsin, hata verirse dursun)

namespace TamgaApp
{
    /// <summary>
    /// Uygulamanın ana başlangıç (tetiklenme) noktasıdır. Program .exe üzerinden açıldığında ilk burası çalışır.
    /// </summary>
    internal static class Program
    {
        /// <summary>
        /// Uygulamanın ana giriş metodu.
        /// </summary>
        [STAThread]
        static void Main()
        {
            #region 🚀 1. GÖRSEL TEMEL AYARLAR VE MOTOR ATEŞLEME

            // Windows Forms uygulamasının modern görsel stillerle ve yumuşak kenarlı yazılarla çalışmasını sağlar.
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // 🚀 KRİTİK DOKUNUŞ: Program açılır açılmaz (şifre ekranı gelse bile) veritabanı var mı diye bakar, yoksa kurar!
            DbHelper.EnsureDatabase();

            #endregion

            // =========================================================================================

            #region 🔒 2. CANLI ORTAM (ORİJİNAL GİRİŞ EKRANI)

            // Giriş ekranını oluşturuyoruz
            LoginForm login = new LoginForm();

            // Eğer kullanıcı doğru şifre girip giriş yaparsa (DialogResult.OK dönerse) ana ekranı aç
            if (login.ShowDialog() == DialogResult.OK)
            {
                Application.Run(new MainForm());
            }
            else
            {
                // Kullanıcı şifre ekranında "Çarpı" (X) tuşuna basarsa programı arka planda açık bırakma, tamamen kapat.
                Application.Exit();
            }

            #endregion
        }
    }
}