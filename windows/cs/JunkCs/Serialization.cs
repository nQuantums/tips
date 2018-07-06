using System;
using System.Collections.Generic;
using System.Text;

namespace Jk
{
	/// <summary>
	/// オブジェクトをファイルへ保存したり、ファイルから読み込んだりする処理を行う。
	/// </summary>
	public static class Serialization
	{
		#region フィールド
		private static string m_AppDataFileName;
		#endregion

		#region プロパティ
		/// <summary>
		/// アプリケーション依存のデータのファイル名の取得。
		/// </summary>
		public static string AppDataFileName
		{
			get
			{
				if (m_AppDataFileName == null)
				{
					m_AppDataFileName = Environment.GetCommandLineArgs()[0] + ".xml";
				}
				return m_AppDataFileName;
			}
		}
		#endregion

		#region メソッド
		/// <summary>
		/// 指定されたオブジェクトを指定されたファイルへXML形式で保存する。
		/// </summary>
		/// <typeparam name="T">書き込むオブジェクトの型。</typeparam>
		/// <param name="obj">保存したいオブジェクト。</param>
		/// <param name="fileName">保存先XMLファイル名。</param>
		public static void SaveXml<T>(T obj, string fileName)
		{
			//	XmlSerializerオブジェクトを作成
			//	書き込むオブジェクトの型を指定する
			System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(T));

			//	ファイルを開く
			using (System.IO.FileStream fs = new System.IO.FileStream(fileName, System.IO.FileMode.Create))
			{
				//	シリアル化し、XMLファイルに保存する
				serializer.Serialize(fs, obj);
				//	閉じる
				fs.Close();
			}
		}

		/// <summary>
		/// XMLファイルからオブジェクトを読み込み、指定された変数へ書き込む。
		/// </summary>
		/// <typeparam name="T">読み込むオブジェクトの型。</typeparam>
		/// <param name="obj">読み込まれたオブジェクトがこの変数に書き込まれる。</param>
		/// <param name="fileName">読み込み元XMLファイル名。</param>
		public static void LoadXml<T>(out T obj, string fileName) where T : new()
		{
			try
			{
				//	XmlSerializerオブジェクトの作成
				System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(T));
				//	ファイルを開く
				using (System.IO.FileStream fs = new System.IO.FileStream(fileName, System.IO.FileMode.Open))
				{
					//	XMLファイルから読み込み、逆シリアル化する
					obj = (T)serializer.Deserialize(fs);
					//	閉じる
					fs.Close();
				}
			}
			catch (Exception)
			{
				obj = new T();
			}
		}

		/// <summary>
		/// アプリケーションデータをXML形式で保存する。ファイル名は "exe名.xml" で保存される。
		/// </summary>
		/// <typeparam name="T">アプリケーションデータオブジェクトの型。</typeparam>
		/// <param name="appData">保存するアプリケーションデータオブジェクト。</param>
		public static void SaveAppData<T>(T appData)
		{
			SaveXml(appData, AppDataFileName);
		}

		/// <summary>
		/// XMLファイルからアプリケーションデータを読み込む。
		/// </summary>
		/// <typeparam name="T">アプリケーションデータオブジェクトの型。</typeparam>
		/// <param name="appData">読み込まれたアプリケーションデータが書き込まれる。</param>
		public static void LoadAppData<T>(out T appData) where T : new()
		{
			LoadXml(out appData, AppDataFileName);
		}
		#endregion
	}
}
