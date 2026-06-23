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
        #region 📂 BAĞLANTI VE DOSYA YOLLARI

        /// <summary>Veritabanı dosyasının fiziksel adı</summary>
        private const string DbFileName = "TamgaApp_Data.db";

        /// <summary>Veritabanının kaydedileceği klasör yolu (Programın çalıştığı dizin içindeki 'Data' klasörü)</summary>
        public static string DataFolder => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");

        /// <summary>Veritabanının tam fiziksel dosya yolu (Klasör + Dosya Adı)</summary>
        public static string DbPath => Path.Combine(DataFolder, DbFileName);

        /// <summary>SQLite motorunun veritabanına erişmek için kullanacağı bağlantı dizesi (Connection String)</summary>
        public static string ConnectionString => $"Data Source={DbPath}";

        #endregion

        // ==============================================================================================

        #region 🛠️ VERİTABANI KURULUMU VE GÜNCELLEME MOTORU

        /// <summary>
        /// Program ilk çalıştığında veritabanı dosyasının ve gerekli tabloların var olup olmadığını kontrol eder. 
        /// Yoksa sıfırdan oluşturur, geçmiş sürümlerden kalan eksik sütunlar varsa tabloyu otomatik günceller.
        /// </summary>
        public static void EnsureDatabase()
        {
            // Data klasörü yoksa hata vermemesi için önce klasörü oluşturuyoruz
            Directory.CreateDirectory(DataFolder);

            using (var conn = new SqliteConnection(ConnectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    // 🚀 SİHİRLİ DOKUNUŞ: Üç temel tabloyu (Firmalar, Urunler, Kullanicilar) tek seferde, yoksa kuruyoruz!
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
                            Barkod TEXT
                        );

                        CREATE TABLE IF NOT EXISTS Kullanicilar (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            KullaniciAdi TEXT UNIQUE NOT NULL,
                            SifreHash TEXT NOT NULL,
                            Yetkiler TEXT
                        );";

                    cmd.ExecuteNonQuery();

                    // 🔄 GÜNCELLEME MOTORU: Eski tablolara yeni eklenen sütunları zorla ekliyoruz.
                    // Hata verirse sütun zaten var demektir, catch bloğu ile sessizce es geçiyoruz.
                    try { cmd.CommandText = "ALTER TABLE Kullanicilar ADD COLUMN BitisTarihi DATETIME;"; cmd.ExecuteNonQuery(); } catch { }
                    try { cmd.CommandText = "ALTER TABLE Kullanicilar ADD COLUMN SonSifreDegistirme DATETIME;"; cmd.ExecuteNonQuery(); } catch { }
                    try { cmd.CommandText = "ALTER TABLE Kullanicilar ADD COLUMN SifreGecerlilikAyi INTEGER DEFAULT 0;"; cmd.ExecuteNonQuery(); } catch { }
                }
            }
        }

        #endregion

        // ==============================================================================================

        #region 🔌 BAĞLANTI SAĞLAYICI (PROVIDER)

        /// <summary>
        /// Veritabanı (DataAccess) işlemleri için kullanılacak hazır ve yapılandırılmış bir SQLite bağlantısı döndürür.
        /// </summary>
        /// <returns>Bağlantıya hazır SqliteConnection nesnesi</returns>
        public static SqliteConnection GetConnection()
        {
            return new SqliteConnection(ConnectionString);
        }

        #endregion
    }
}