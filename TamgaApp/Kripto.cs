using System;
using System.Security.Cryptography;
using System.Text;

namespace TamgaApp
{
    /// <summary>
    /// Ayarlarda saklanan gizli bilgileri (SQL şifresi, "Beni hatırla" şifresi) Windows DPAPI ile korur.
    /// DPAPI anahtarı Windows kullanıcı hesabına bağlıdır; kodda gizli anahtar bulunmaz ve başka bir
    /// bilgisayara/kullanıcıya kopyalanan ayar dosyası çözülemez.
    /// Eski sürümlerin sabit anahtarlı AES ile şifrelediği değerler hâlâ okunabilir ve
    /// <see cref="EskiAyarlariYukselt"/> ile otomatik olarak DPAPI'ye taşınır.
    /// </summary>
    public static class Kripto
    {
        private const string OnEk = "dpapi1:";
        private static readonly byte[] EkVeri = Encoding.UTF8.GetBytes("TamgaApp.Ayarlar.v1");

        public static string Sifrele(string duzMetin)
        {
            if (string.IsNullOrEmpty(duzMetin)) return "";
            byte[] korumali = ProtectedData.Protect(Encoding.UTF8.GetBytes(duzMetin), EkVeri, DataProtectionScope.CurrentUser);
            return OnEk + Convert.ToBase64String(korumali);
        }

        public static string Coz(string sifreliMetin)
        {
            if (string.IsNullOrEmpty(sifreliMetin)) return "";
            try
            {
                if (sifreliMetin.StartsWith(OnEk, StringComparison.Ordinal))
                {
                    byte[] veri = Convert.FromBase64String(sifreliMetin.Substring(OnEk.Length));
                    return Encoding.UTF8.GetString(ProtectedData.Unprotect(veri, EkVeri, DataProtectionScope.CurrentUser));
                }
                return EskiAesCoz(sifreliMetin);
            }
            catch { return ""; }
        }

        public static bool YeniBicimMi(string deger)
        {
            return !string.IsNullOrEmpty(deger) && deger.StartsWith(OnEk, StringComparison.Ordinal);
        }

        /// <summary>Program açılışında bir kez çağrılır: eski AES değerlerini DPAPI'ye yükseltir.</summary>
        public static void EskiAyarlariYukselt()
        {
            try
            {
                var a = Properties.Settings.Default;
                bool degisti = false;
                if (!string.IsNullOrEmpty(a.SqlSifre) && !YeniBicimMi(a.SqlSifre))
                {
                    string duz = EskiAesCoz(a.SqlSifre);
                    a.SqlSifre = string.IsNullOrEmpty(duz) ? "" : Sifrele(duz);
                    degisti = true;
                }
                if (!string.IsNullOrEmpty(a.HatirlaSifre) && !YeniBicimMi(a.HatirlaSifre))
                {
                    string duz = EskiAesCoz(a.HatirlaSifre);
                    a.HatirlaSifre = string.IsNullOrEmpty(duz) ? "" : Sifrele(duz);
                    degisti = true;
                }
                if (degisti) a.Save();
            }
            catch { /* Ayar yükseltmesi başarısız olursa eski değerler yerinde kalır */ }
        }

        // ---- Eski sürüm uyumluluğu (yalnızca okuma) ----
        private static string EskiAesCoz(string sifreliMetin)
        {
            try
            {
                byte[] dizi = Convert.FromBase64String(sifreliMetin);
                using (Aes aes = Aes.Create())
                {
                    aes.Key = Encoding.UTF8.GetBytes("TamgaAppGvnlik16");
                    aes.IV = Encoding.UTF8.GetBytes("YalcinSoft123456");
                    using (ICryptoTransform cozucu = aes.CreateDecryptor(aes.Key, aes.IV))
                    {
                        byte[] sonuc = cozucu.TransformFinalBlock(dizi, 0, dizi.Length);
                        return Encoding.UTF8.GetString(sonuc);
                    }
                }
            }
            catch { return ""; }
        }
    }
}
