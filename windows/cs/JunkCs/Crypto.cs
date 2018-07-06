using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Xml.Serialization;
using System.IO;

namespace Jk {
	/// <summary>
	/// 暗号関係
	/// </summary>
	public class Crypto {
		/// <summary>
		/// オブジェクトを暗号化してファイルに保存する
		/// </summary>
		/// <param name="filePath">保存するファイルパス</param>
		/// <param name="ds">暗号化対象データセット</param>
		/// <param name="key">暗号キー</param>
		/// <param name="IV">初期化ベクタ</param>
		/// <remarks>Rijndael、XmlSerializer を用いている。</remarks>
		public static void EncryptToFile(string filePath, object ds, byte[] key, byte[] IV) {
			XmlSerializer ser = new XmlSerializer(ds.GetType());
			using (SymmetricAlgorithm sa = new RijndaelManaged()) {
				ICryptoTransform encryptor = sa.CreateEncryptor(key, IV);
				using (FileStream msEncrypt = new FileStream(filePath, FileMode.Create)) {
					using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write)) {
						ser.Serialize(csEncrypt, ds);
					}
				}
			}
		}

		/// <summary>
		/// 暗号化されたファイルからオブジェクトを複合化する
		/// </summary>
		/// <param name="filePath">読み込むファイルパス</param>
		/// <param name="ds">暗号化対象データセット</param>
		/// <param name="key">暗号キー</param>
		/// <param name="IV">初期化ベクタ</param>
		/// <remarks>Rijndael、XmlSerializer を用いている。</remarks>
		public static T DecryptFromFile<T>(string filePath, byte[] key, byte[] IV) where T : new() {
			XmlSerializer ser = new XmlSerializer(typeof(T));
			using (SymmetricAlgorithm sa = new RijndaelManaged()) {
				ICryptoTransform decryptor = sa.CreateDecryptor(key, IV);
				using (FileStream msEncrypt = new FileStream(filePath, FileMode.Open)) {
					using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, decryptor, CryptoStreamMode.Read)) {
						return (T)ser.Deserialize(csEncrypt);
					}
				}
			}
		}
	}
}
