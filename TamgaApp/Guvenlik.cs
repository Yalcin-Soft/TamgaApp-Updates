using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace TamgaApp
{
    /// <summary>
    /// Şifre özetleme (PBKDF2), sistem hesapları (kurucu / yönetici arka kapısı) ve giriş denemesi
    /// sınırlama işlemlerinin tek merkezi. Kaynak kodda hiçbir şifre düz metin olarak tutulmaz.
    /// </summary>
    public static class Guvenlik
    {
        #region 🔑 ŞİFRE ÖZETLEME (PBKDF2-SHA256, TUZLU)

        private const int Iterasyon = 120000;
        private const string OnEk = "pbkdf2$";

        /// <summary>Şifreden "pbkdf2$iterasyon$tuz$özet" biçiminde, her seferinde farklı tuzlu bir özet üretir.</summary>
        public static string OzetUret(string sifre)
        {
            byte[] tuz = new byte[16];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(tuz);
            byte[] ozet = Turet(sifre ?? "", tuz, Iterasyon);
            return OnEk + Iterasyon + "$" + Convert.ToBase64String(tuz) + "$" + Convert.ToBase64String(ozet);
        }

        /// <summary>
        /// Şifreyi kayıtlı özetle karşılaştırır. Eski sürümlerin tuzsuz SHA-256 (hex) özetlerini de tanır;
        /// <paramref name="yenilenmeli"/> true dönerse kayıt yeni biçime yükseltilmelidir.
        /// </summary>
        public static bool Dogrula(string sifre, string kayitliOzet, out bool yenilenmeli)
        {
            yenilenmeli = false;
            if (string.IsNullOrEmpty(kayitliOzet) || sifre == null) return false;

            if (kayitliOzet.StartsWith(OnEk, StringComparison.Ordinal))
            {
                string[] p = kayitliOzet.Split('$');
                if (p.Length != 4 || !int.TryParse(p[1], out int iter)) return false;
                byte[] tuz, beklenen;
                try { tuz = Convert.FromBase64String(p[2]); beklenen = Convert.FromBase64String(p[3]); }
                catch (FormatException) { return false; }
                byte[] hesaplanan = Turet(sifre, tuz, iter);
                bool esit = SabitSureliEsit(hesaplanan, beklenen);
                if (esit && iter < Iterasyon) yenilenmeli = true;
                return esit;
            }

            // Eski biçim: tuzsuz SHA-256 hex
            bool eskiEsit = string.Equals(EskiSha256(sifre), kayitliOzet, StringComparison.OrdinalIgnoreCase);
            if (eskiEsit) yenilenmeli = true;
            return eskiEsit;
        }

        public static string EskiSha256(string sifre)
        {
            using (var sha = SHA256.Create())
            {
                byte[] b = sha.ComputeHash(Encoding.UTF8.GetBytes(sifre ?? ""));
                var sb = new StringBuilder(64);
                foreach (byte x in b) sb.Append(x.ToString("x2"));
                return sb.ToString();
            }
        }

        private static byte[] Turet(string sifre, byte[] tuz, int iter)
        {
            using (var k = new Rfc2898DeriveBytes(sifre, tuz, iter, HashAlgorithmName.SHA256))
                return k.GetBytes(32);
        }

        private static bool SabitSureliEsit(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length) return false;
            int fark = 0;
            for (int i = 0; i < a.Length; i++) fark |= a[i] ^ b[i];
            return fark == 0;
        }

        /// <summary>Yeni şifreler için en az 6 karakter, harf ve rakam içeren basit bir kural.</summary>
        public static string SifreKuraliHatasi(string sifre)
        {
            if (string.IsNullOrEmpty(sifre) || sifre.Length < 6) return "Şifre en az 6 karakter olmalıdır.";
            if (!sifre.Any(char.IsLetter) || !sifre.Any(char.IsDigit)) return "Şifre en az bir harf ve bir rakam içermelidir.";
            return null;
        }

        #endregion

        #region 🛡️ SİSTEM HESAPLARI (KURUCU / YÖNETİCİ ARKA KAPISI)

        // Kurucu ve Yönetici hesapları veritabanı silinse bile her zaman çalışır (acil durum erişimi).
        // Şifreler burada YOKTUR; yalnızca tuzlu PBKDF2 özetleri vardır. Şifreyi değiştirmek için:
        // Ayarlar > Yönetim > "Kurucu şifre özeti üret" ile yeni özeti üretip aşağıdaki sabite yapıştırın.
        public const string KurucuKullaniciAdi = "TamgaApp";
        public const string YoneticiKullaniciAdi = "Yönetici";
        private const string KurucuOzeti = "pbkdf2$120000$2LPppixxIN9CCu5u5/9O4g==$DgHSo5ybSfXyV2v8ztrrBQv4EhsjlQFXt6qgsbkR55w=";
        private const string YoneticiOzeti = "pbkdf2$120000$R5AcnQxk6bckZiswBXYvPw==$ZL31WHhTnyud3bE8j3anrQn8vDYE846g8bJrkZQOnvc=";

        public enum SistemHesabi { Yok, Kurucu, Yonetici }

        public static SistemHesabi SistemHesabiDogrula(string kullaniciAdi, string sifre)
        {
            bool y;
            if (string.Equals(kullaniciAdi, KurucuKullaniciAdi, StringComparison.Ordinal) && Dogrula(sifre, KurucuOzeti, out y)) return SistemHesabi.Kurucu;
            if (string.Equals(kullaniciAdi, YoneticiKullaniciAdi, StringComparison.Ordinal) && Dogrula(sifre, YoneticiOzeti, out y)) return SistemHesabi.Yonetici;
            return SistemHesabi.Yok;
        }

        /// <summary>
        /// Veritabanında bu isimlerle kullanıcı açılmasına izin verilmez; aksi halde isim üzerinden tam yetki
        /// kazanılabilirdi.
        /// </summary>
        public static bool AyrilmisKullaniciAdiMi(string kullaniciAdi)
        {
            if (string.IsNullOrWhiteSpace(kullaniciAdi)) return true;
            var tr = new System.Globalization.CultureInfo("tr-TR");
            string k = kullaniciAdi.Trim().ToLower(tr);
            string[] ayrilmis = { "tamgaapp", "yönetici", "yonetici", "kurucu", "admin", "administrator", "sistem" };
            return ayrilmis.Any(a => k == a || k.StartsWith("kurucu ("));
        }

        #endregion

        #region ⏳ GİRİŞ DENEMESİ SINIRLAMA

        private static int hataliDeneme;
        private static DateTime kilitBitis = DateTime.MinValue;
        public const int AzamiDeneme = 5;

        public static TimeSpan KalanKilitSuresi
        {
            get { var k = kilitBitis - DateTime.Now; return k > TimeSpan.Zero ? k : TimeSpan.Zero; }
        }

        /// <summary>Hatalı girişi sayar. 5 hatadan sonra her yeni hatada kilit süresi artarak uzar.</summary>
        public static void HataliGirisKaydet()
        {
            hataliDeneme++;
            if (hataliDeneme >= AzamiDeneme)
            {
                int saniye = Math.Min(300, 30 * (hataliDeneme - AzamiDeneme + 1));
                kilitBitis = DateTime.Now.AddSeconds(saniye);
            }
        }

        public static int KalanDeneme { get { return Math.Max(0, AzamiDeneme - hataliDeneme); } }

        public static void BasariliGiris() { hataliDeneme = 0; kilitBitis = DateTime.MinValue; }

        #endregion
    }
}
