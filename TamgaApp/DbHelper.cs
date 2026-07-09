using System;
using System.IO;
using Microsoft.Data.Sqlite;

namespace TamgaApp
{
    /// <summary>
    /// SQLite veritabanı bağlantılarını yöneten, dosya yollarını belirleyen ve ilk kurulumları gerçekleştiren ana yardımcı sınıftır.
    /// </summary>
    public static class DbHelper
    {
        #region 📂 BAĞLANTI VE DOSYA YOLLARI (APPDATA ZIRHLI)

        private const string DbFileName = "TamgaApp_Data.db";

        /// <summary>Veritabanının Windows güncellemelerinden etkilenmeyeceği Güvenli AppData Klasörü</summary>
        public static string DataFolder
        {
            get
            {
                string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TamgaApp", "Veritabani");
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                return path;
            }
        }

        /// <summary>Veritabanının tam fiziksel dosya yolu (AppData Klasörü + Dosya Adı)</summary>
        public static string DbPath => Path.Combine(DataFolder, DbFileName);

        /// <summary>SQLite motorunun veritabanına erişmek için kullanacağı bağlantı dizesi</summary>
        public static string ConnectionString => $"Data Source={DbPath}";

        #endregion

        // ==============================================================================================

        #region 🛠️ VERİTABANI KURULUMU VE GÜNCELLEME MOTORU

        /// <summary>
        /// Program ilk çalıştığında veritabanı dosyasının ve gerekli tabloların var olup olmadığını kontrol eder. 
        /// Yoksa kurulum klasöründen kopyalar veya sıfırdan oluşturur, eksik sütunları (Örn: Renk) otomatik ekler.
        /// </summary>
        public static void EnsureDatabase()
        {
            // 🛡️ ADIM 1: GÜVENLİ BÖLGEYE (APPDATA) TAŞIMA ZIRHI
            // Eğer AppData'da veritabanı yoksa, programın Data klasöründeki orijinal, dolu veritabanını kopyala
            if (!File.Exists(DbPath))
            {
                string kurulumDbYolu = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", DbFileName);
                if (File.Exists(kurulumDbYolu))
                {
                    File.Copy(kurulumDbYolu, DbPath);
                }
            }

            // 🛡️ ADIM 2: EKSİK TABLO VE SÜTUNLARI OTOMATİK TAMAMLAMA
            using (var conn = new SqliteConnection(ConnectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        CREATE TABLE IF NOT EXISTS Firmalar (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            FirmaAdi TEXT NOT NULL,
                            Adres TEXT,
                            Il TEXT,
                            Telefon1 TEXT,
                            Telefon2 TEXT
                        );

                        CREATE TABLE IF NOT EXISTS Urunler (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            UrunKodu TEXT,
                            Aciklama TEXT,
                            IngilizceAciklama TEXT,
                            Barkod TEXT,
                            Renk TEXT
                        );

                        CREATE TABLE IF NOT EXISTS Kullanicilar (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            KullaniciAdi TEXT UNIQUE NOT NULL,
                            SifreHash TEXT NOT NULL,
                            Yetkiler TEXT
                        );";

                    cmd.ExecuteNonQuery();

                    // 🔄 GÜNCELLEME MOTORU: Eski tablolara yeni eklenen sütunları zorla ekliyoruz. (RENK DAHİL)
                    try { cmd.CommandText = "ALTER TABLE Kullanicilar ADD COLUMN BitisTarihi DATETIME;"; cmd.ExecuteNonQuery(); } catch { }
                    try { cmd.CommandText = "ALTER TABLE Kullanicilar ADD COLUMN SonSifreDegistirme DATETIME;"; cmd.ExecuteNonQuery(); } catch { }
                    try { cmd.CommandText = "ALTER TABLE Kullanicilar ADD COLUMN SifreGecerlilikAyi INTEGER DEFAULT 0;"; cmd.ExecuteNonQuery(); } catch { }

                    // 🌟 5. VİTES (RENK) GÜNCELLEMESİ: Eski Urunler tablosunda Renk yoksa otomatik ekle!
                    try { cmd.CommandText = "ALTER TABLE Urunler ADD COLUMN Renk TEXT DEFAULT '';"; cmd.ExecuteNonQuery(); } catch { }
                }
            }
        }

        #endregion

        // ==============================================================================================

        #region 🔌 BAĞLANTI SAĞLAYICI (PROVIDER)

        /// <summary>
        /// Veritabanı (DataAccess) işlemleri için kullanılacak hazır ve yapılandırılmış bir SQLite bağlantısı döndürür.
        /// </summary>
        public static SqliteConnection GetConnection()
        {
            // Her bağlantı açıldığında tabloların ve sütunların sağlam olduğundan emin ol
            EnsureDatabase();
            return new SqliteConnection(ConnectionString);
        }

        #endregion
    }
}