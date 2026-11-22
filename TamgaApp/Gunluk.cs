using System;
using System.Data;
using Microsoft.Data.Sqlite;

namespace TamgaApp
{
    /// <summary>
    /// İşlem günlüğü (denetim kaydı): kim, ne zaman, hangi modülde, ne yaptı. Giriş denemeleri, kullanıcı
    /// yönetimi, yedekleme/geri yükleme ve sevk edilen belgeler burada tutulur. Günlük yazımı hiçbir
    /// zaman asıl işlemi engellemez; hata olursa sessizce atlanır.
    /// </summary>
    public static class Gunluk
    {
        public static void Yaz(string modul, string islem, string detay = "", string kullanici = null)
        {
            try
            {
                using (var conn = DbHelper.GetConnection())
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "INSERT INTO IslemGunlugu (Tarih, Kullanici, Bilgisayar, Modul, Islem, Detay) VALUES (@t, @k, @b, @m, @i, @d);";
                        cmd.Parameters.AddWithValue("@t", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                        cmd.Parameters.AddWithValue("@k", kullanici ?? (string.IsNullOrEmpty(MainForm.AktifKullaniciAdi) ? "-" : MainForm.AktifKullaniciAdi));
                        cmd.Parameters.AddWithValue("@b", Environment.MachineName);
                        cmd.Parameters.AddWithValue("@m", modul ?? "");
                        cmd.Parameters.AddWithValue("@i", islem ?? "");
                        cmd.Parameters.AddWithValue("@d", detay ?? "");
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch { }
        }

        /// <summary>Filtreye uyan son kayıtları getirir (en yeni üstte).</summary>
        public static DataTable Getir(DateTime baslangic, DateTime bitis, string arama, int limit = 5000)
        {
            var dt = new DataTable();
            dt.Columns.Add("Tarih");
            dt.Columns.Add("Kullanıcı");
            dt.Columns.Add("Bilgisayar");
            dt.Columns.Add("Modül");
            dt.Columns.Add("İşlem");
            dt.Columns.Add("Detay");
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"SELECT Tarih, Kullanici, Bilgisayar, Modul, Islem, Detay FROM IslemGunlugu
                                        WHERE Tarih >= @bas AND Tarih < @bit
                                          AND (@ara = '' OR Kullanici LIKE @like OR Modul LIKE @like OR Islem LIKE @like OR Detay LIKE @like)
                                        ORDER BY Id DESC LIMIT @lim;";
                    cmd.Parameters.AddWithValue("@bas", baslangic.Date.ToString("yyyy-MM-dd 00:00:00"));
                    cmd.Parameters.AddWithValue("@bit", bitis.Date.AddDays(1).ToString("yyyy-MM-dd 00:00:00"));
                    cmd.Parameters.AddWithValue("@ara", arama ?? "");
                    cmd.Parameters.AddWithValue("@like", "%" + (arama ?? "") + "%");
                    cmd.Parameters.AddWithValue("@lim", limit);
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            dt.Rows.Add(r.IsDBNull(0) ? "" : r.GetString(0), r.IsDBNull(1) ? "" : r.GetString(1), r.IsDBNull(2) ? "" : r.GetString(2),
                                        r.IsDBNull(3) ? "" : r.GetString(3), r.IsDBNull(4) ? "" : r.GetString(4), r.IsDBNull(5) ? "" : r.GetString(5));
                        }
                    }
                }
            }
            return dt;
        }

        /// <summary>180 günden eski kayıtları temizler (veritabanının sınırsız büyümesini önler).</summary>
        public static void EskileriTemizle(int gun = 180)
        {
            try
            {
                using (var conn = DbHelper.GetConnection())
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "DELETE FROM IslemGunlugu WHERE Tarih < @t;";
                        cmd.Parameters.AddWithValue("@t", DateTime.Now.AddDays(-gun).ToString("yyyy-MM-dd HH:mm:ss"));
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch { }
        }
    }
}
