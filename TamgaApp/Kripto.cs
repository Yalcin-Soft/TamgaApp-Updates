using System;
using System.Security.Cryptography;
using System.Text;

namespace TamgaApp
{
    public static class Kripto
    {
        // Kendi gizli anahtarımız ve vektörümüz (16 karakter olmalı)
        private static readonly string Anahtar = "TamgaAppGvnlik16";
        private static readonly string IV = "YalcinSoft123456";

        public static string Sifrele(string duzMetin)
        {
            if (string.IsNullOrEmpty(duzMetin)) return "";
            byte[] dizi = Encoding.UTF8.GetBytes(duzMetin);
            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(Anahtar);
                aes.IV = Encoding.UTF8.GetBytes(IV);
                ICryptoTransform sifreleyici = aes.CreateEncryptor(aes.Key, aes.IV);
                byte[] sonuc = sifreleyici.TransformFinalBlock(dizi, 0, dizi.Length);
                return Convert.ToBase64String(sonuc);
            }
        }

        public static string Coz(string sifreliMetin)
        {
            if (string.IsNullOrEmpty(sifreliMetin)) return "";
            try
            {
                byte[] dizi = Convert.FromBase64String(sifreliMetin);
                using (Aes aes = Aes.Create())
                {
                    aes.Key = Encoding.UTF8.GetBytes(Anahtar);
                    aes.IV = Encoding.UTF8.GetBytes(IV);
                    ICryptoTransform cozucu = aes.CreateDecryptor(aes.Key, aes.IV);
                    byte[] sonuc = cozucu.TransformFinalBlock(dizi, 0, dizi.Length);
                    return Encoding.UTF8.GetString(sonuc);
                }
            }
            catch { return ""; }
        }
    }
}