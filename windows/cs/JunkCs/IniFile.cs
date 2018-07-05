using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace Jk
{
	/// <summary>
	///	INIファイルアクセス用クラス。
	/// </summary>
	public class IniFile : ICloneable
	{
		#region API Methods
		[DllImport("KERNEL32.DLL", EntryPoint = "GetPrivateProfileStringA", SetLastError = true)]
		private static extern uint GetPrivateProfileStringByByteArray(string lpAppName, string lpKeyName, string lpDefault, byte[] lpReturnedString, uint nSize, string lpFileName);
		[DllImport("KERNEL32.DLL", SetLastError = true)]
		private static extern uint GetPrivateProfileInt(string lpAppName, string lpKeyName, int nDefault, string lpFileName);
		[DllImport("KERNEL32.DLL", SetLastError = true)]
		private static extern uint WritePrivateProfileString(string lpAppName, string lpKeyName, string lpString, string lpFileName);
		[DllImport("KERNEL32.DLL", EntryPoint = "WritePrivateProfileString", SetLastError = true)]
		private static extern uint WritePrivateProfileStringByStringBuilder(string lpAppName, string lpKeyName, StringBuilder lpString, string lpFileName);
		[DllImport("KERNEL32.DLL", SetLastError = true)]
		private static extern uint GetPrivateProfileSection(string lpAppName, byte[] lpReturnedString, uint nSize, string lpFileName);
		[DllImport("KERNEL32.DLL", SetLastError = true)]
		private static extern int WritePrivateProfileSection(string lpAppName, byte[] lpString, string lpFileName);
		#endregion

		#region Static Methods
		/// <summary>
		/// INIファイル内のCSV形式の文字列をフィールドに分ける。
		/// </summary>
		/// <param name="text">フィールドに分割したい文字列。</param>
		/// <returns>フィールドの配列。</returns>
		static string[] CsvSplit(string text)
		{
			int i, start, end;
			bool bQuoteStart, bQuoteEnd;
			int len = text.Length;
			int term = len + 1;

			int iField;
			string[] fields = new string[256];

			//	文字列からフィールドを取り出していく
			i = 0;
			iField = 0;
			while (i < term)
			{
				//	フィールドの先頭を探す
				while (i < len && (text[i] == ' ' || text[i] == '\t'))
				{
					i++;
				}

				//	ダブルクォーテーションで括られているか調べる
				if (i < len && text[i] == '"')
				{
					bQuoteStart = true;
					bQuoteEnd = false;
					i++;
				}
				else
				{
					bQuoteStart = false;
					bQuoteEnd = false;
				}

				start = end = i;

				//	フィールドの終端を探す
				for (; i < term; i++)
				{
					if (i == len)
					{
						end = i;
						i++;
						break;
					}

					if (text[i] == '"')
					{
						end = i;
						bQuoteEnd = true;

						//	" の後に , が来たら文字の終端だと判断
						//	(角度の秒(")が出てくると対処できないが、INIファイルの仕様上どうしようもない)
						if (i + 1 < len && text[i + 1] == ',')
						{
							i += 2;
							break;
						}
					}
					else if (text[i] == ',')
					{
						if (bQuoteStart)
						{
							//	" で括られている場合は , は区切り記号とはみなさない
							if (bQuoteEnd)
							{
								i++;
								break;
							}
						}
						else
						{
							end = i;
							i++;
							break;
						}
					}
				}

				//	フィールド配列にフィールドを追加
				if (fields.Length <= iField)
				{
					System.Array.Resize(ref fields, fields.Length * 2);
				}
				fields[iField] = text.Substring(start, end - start);
				iField++;
			}

			if (iField < fields.Length)
				System.Array.Resize(ref fields, iField);

			return fields;
		}
		#endregion

		#region Fields
		private string m_FileName;
		private string m_Section;
		private string[] m_GetArrayValueCache; // GetArrayValue を呼び出したときに作成されるキャッシュ
		private string m_LastGetArrayValueKey; // GetArrayValue で最後にアクセスしたキー
		#endregion


		#region Constructors
		/// <summary>
		///	デフォルトコンストラクタ
		/// </summary>
		public IniFile()
		{
		}
		/// <summary>
		///	ファイル名を指定して初期化する
		/// </summary>
		public IniFile(string fileName)
		{
			m_FileName = fileName;
		}
		/// <summary>
		///	ファイル名とセクション名を指定して初期化する
		/// </summary>
		public IniFile(string fileName, string section)
		{
			m_FileName = fileName;
			m_Section = section;
		}
		#endregion


		#region Properties
		/// <summary>
		///	アクセス対象のINIファイル名
		/// </summary>
		public string FileName
		{
			get
			{
				return m_FileName;
			}
			set
			{
				m_FileName = value;
				m_GetArrayValueCache = null;
				m_LastGetArrayValueKey = null;
			}
		}

		/// <summary>
		///	アクセス対象のセクション名
		/// </summary>
		public string Section
		{
			get
			{
				return m_Section;
			}
			set
			{
				m_Section = value;
				m_GetArrayValueCache = null;
				m_LastGetArrayValueKey = null;
			}
		}
		#endregion


		#region ICloneable interface Methods
		/// <summary>
		///	クローンを作成します。
		/// </summary>
		public object Clone()
		{
			IniFile c = (IniFile)MemberwiseClone();
			//	参照型の変数をディープコピー
			Cloner.SetClone(ref c.m_GetArrayValueCache);
			return c;
		}
		#endregion

		#region SetValue Methods
		/// <summary>
		///	指定されたキーへ文字列を設定する
		/// </summary>
		public bool SetValue(string key, string val)
		{
			if (val == null)
				return true;
			return WritePrivateProfileString(m_Section, key, val) != 0;
		}
		/// <summary>
		///	指定されたキーへboolを設定する
		/// </summary>
		public bool SetValue(string key, bool val)
		{
			int iw = 0;
			if (val) iw = 1;
			return WritePrivateProfileString(m_Section, key, iw.ToString()) != 0;
		}
		/// <summary>
		///	指定されたキーへ整数を設定する
		/// </summary>
		public bool SetValue(string key, int val)
		{
			return WritePrivateProfileString(m_Section, key, val.ToString()) != 0;
		}
		/// <summary>
		///	指定されたキーへ実数を設定する
		/// </summary>
		public bool SetValue(string key, double val)
		{
			return WritePrivateProfileString(m_Section, key, val.ToString()) != 0;
		}
		#endregion

		#region GetValue Methods
		/// <summary>
		///	指定されたキーから文字列を取得する
		/// </summary>
		public void GetValue(string key, ref string val, string def)
		{
			val = GetString(key, def);
		}
		/// <summary>
		///	指定されたキーから文字列を取得する
		/// </summary>
		public void GetValue(string key, ref string val)
		{
			val = GetString(key);
		}
		/// <summary>
		///	指定されたキーからboolを取得する
		/// </summary>
		public void GetValue(string key, ref bool val, bool def)
		{
			val = GetBool(key, def);
		}
		/// <summary>
		///	指定されたキーから整数を取得する
		/// </summary>
		public void GetValue(string key, ref bool val)
		{
			val = GetBool(key);
		}
		/// <summary>
		///	指定されたキーから整数を取得する
		/// </summary>
		public void GetValue(string key, ref int val, int def)
		{
			val = GetInt(key, def);
		}
		/// <summary>
		///	指定されたキーから整数を取得する
		/// </summary>
		public void GetValue(string key, ref int val)
		{
			val = GetInt(key);
		}
		/// <summary>
		///	指定されたキーから実数を取得する
		/// </summary>
		public void GetValue(string key, ref double val, double def)
		{
			val = GetDouble(key, def);
		}
		/// <summary>
		///	指定されたキーから実数を取得する
		/// </summary>
		public void GetValue(string key, ref double val)
		{
			val = GetDouble(key);
		}
		#endregion


		#region SetArray Methods
		/// <summary>
		///	指定されたキーへ文字列配列を設定する
		/// </summary>
		public bool SetArray(string key, params string[] array)
		{
			StringBuilder sb = new StringBuilder();
			bool first = true;
			foreach (string val in array)
			{
				if (first)
				{
					sb.Append(val);
					first = false;
				}
				else
				{
					sb.AppendFormat(",{0}", val);
				}
			}
			return WritePrivateProfileStringByStringBuilder(m_Section, key, sb) != 0;
		}
		/// <summary>
		///	指定されたキーへbool配列を設定する
		/// </summary>
		public bool SetArray(string key, params bool[] array)
		{
			StringBuilder sb = new StringBuilder();
			bool first = true;
			foreach (bool val in array)
			{
				if (first)
				{
					sb.Append(val);
					first = false;
				}
				else
				{
					sb.AppendFormat(",{0}", val);
				}
			}
			return WritePrivateProfileStringByStringBuilder(m_Section, key, sb) != 0;
		}
		/// <summary>
		///	指定されたキーへ整数配列を設定する
		/// </summary>
		public bool SetArray(string key, params int[] array)
		{
			StringBuilder sb = new StringBuilder();
			bool first = true;
			foreach (int val in array)
			{
				if (first)
				{
					sb.Append(val);
					first = false;
				}
				else
				{
					sb.AppendFormat(",{0}", val);
				}
			}
			return WritePrivateProfileStringByStringBuilder(m_Section, key, sb) != 0;
		}
		/// <summary>
		///	指定されたキーへ実数配列を設定する
		/// </summary>
		public bool SetArray(string key, params double[] array)
		{
			StringBuilder sb = new StringBuilder();
			bool first = true;
			foreach (double val in array)
			{
				if (first)
				{
					sb.Append(val);
					first = false;
				}
				else
				{
					sb.AppendFormat(",{0}", val);
				}
			}
			return WritePrivateProfileStringByStringBuilder(m_Section, key, sb) != 0;
		}
		/// <summary>
		///	指定されたキーへオブジェクト配列を設定する
		/// </summary>
		public bool SetArray(string key, params object[] array)
		{
			StringBuilder sb = new StringBuilder();
			bool first = true;
			foreach (object val in array)
			{
				if (first)
				{
					sb.Append(val);
					first = false;
				}
				else
				{
					sb.AppendFormat(",{0}", val);
				}
			}
			return WritePrivateProfileStringByStringBuilder(m_Section, key, sb) != 0;
		}
		#endregion

		#region GetArray Methods
		/// <summary>
		///	指定されたキーから文字列配列を取得する
		/// </summary>
		public string[] GetStringArray(string key, params string[] def)
		{
			string[] array = GetStringArray(key);
			if (array.Length < def.Length)
			{
				int n1 = array.Length;
				int n2 = def.Length;
				System.Array.Resize(ref array, n2);
				for (int i = n1; i < n2; i++)
				{
					array[i] = def[i];
				}
			}
			return array;
		}
		/// <summary>
		///	指定されたキーから文字列配列を取得する
		/// </summary>
		public string[] GetStringArray(string key)
		{
			string data = GetString(key);
			if (data.Length == 0)
			{
				return new string[0];
			}
			else
			{
				return CsvSplit(data);
			}
		}
		/// <summary>
		///	指定されたキーからbool配列を取得する
		/// </summary>
		public bool[] GetBoolArray(string key, params bool[] def)
		{
			bool[] array = GetBoolArray(key);
			if (array.Length < def.Length)
			{
				int n1 = array.Length;
				int n2 = def.Length;
				System.Array.Resize(ref array, n2);
				for (int i = n1; i < n2; i++)
				{
					array[i] = def[i];
				}
			}
			return array;
		}
		/// <summary>
		///	指定されたキーから整数配列を取得する
		/// </summary>
		public int[] GetIntArray(string key, params int[] def)
		{
			int[] array = GetIntArray(key);
			if (array.Length < def.Length)
			{
				int n1 = array.Length;
				int n2 = def.Length;
				System.Array.Resize(ref array, n2);
				for (int i = n1; i < n2; i++)
				{
					array[i] = def[i];
				}
			}
			return array;
		}
		/// <summary>
		///	指定されたキーからbool配列を取得する
		/// </summary>
		public bool[] GetBoolArray(string key)
		{
			string[] strarray = GetStringArray(key);
			bool[] array = new bool[strarray.Length];
			for (int i = 0; i < array.Length; i++)
			{
				double val;
				bool result = double.TryParse(strarray[i], out val);
				if (result)
				{
					if (val == 1) array[i] = true;
					else array[i] = false;
				}
				else
					array[i] = false;
			}
			return array;
		}
		/// <summary>
		///	指定されたキーから整数配列を取得する
		/// </summary>
		public int[] GetIntArray(string key)
		{
			string[] strarray = GetStringArray(key);
			int[] array = new int[strarray.Length];
			for (int i = 0; i < array.Length; i++)
			{
				double val;
				bool result = double.TryParse(strarray[i], out val);
				if (result)
					array[i] = (int)val;
				else
					array[i] = 0;
			}
			return array;
		}
		/// <summary>
		///	指定されたキーから実数配列を取得する
		/// </summary>
		public double[] GetDoubleArray(string key, params double[] def)
		{
			double[] array = GetDoubleArray(key);
			if (array.Length < def.Length)
			{
				int n1 = array.Length;
				int n2 = def.Length;
				System.Array.Resize(ref array, n2);
				for (int i = n1; i < n2; i++)
				{
					array[i] = def[i];
				}
			}
			return array;
		}
		/// <summary>
		///	指定されたキーから実数配列を取得する
		/// </summary>
		public double[] GetDoubleArray(string key)
		{
			string[] strarray = GetStringArray(key);
			double[] array = new double[strarray.Length];
			for (int i = 0; i < array.Length; i++)
			{
				bool result = double.TryParse(strarray[i], out array[i]);
				if (!result)
					array[i] = 0.0;
			}
			return array;
		}
		#endregion

		#region SetArray Each Key Methods
		/// <summary>
		///	指定された書式で生成された各キーへ文字列を設定する
		/// </summary>
		public bool SetArrayEachKey(int baseIndex, string format, params string[] array)
		{
			if (array == null)
				return true;
			int n = array.Length;
			for (int i = 0; i < n; i++)
			{
				string key = string.Format(format, baseIndex + i);
				SetValue(key, array[i]);
			}
			return true;
		}
		/// <summary>
		///	指定された書式で生成された各キーへ整数を設定する
		/// </summary>
		public bool SetArrayEachKey(int baseIndex, string format, params int[] array)
		{
			if (array == null)
				return true;
			int n = array.Length;
			for (int i = 0; i < n; i++)
			{
				string key = string.Format(format, baseIndex + i);
				SetValue(key, array[i]);
			}
			return true;
		}
		/// <summary>
		///	指定された書式で生成された各キーへ実数を設定する
		/// </summary>
		public bool SetArrayEachKey(int baseIndex, string format, params double[] array)
		{
			if (array == null)
				return true;
			int n = array.Length;
			for (int i = 0; i < n; i++)
			{
				string key = string.Format(format, baseIndex + i);
				SetValue(key, array[i]);
			}
			return true;
		}
		#endregion

		#region GetArray Each Key Methods
		/// <summary>
		///	指定された書式で生成された各キーから文字列を取得する
		/// </summary>
		public string[] GetStringArrayEachKey(int baseIndex, string format, params string[] def)
		{
			int i = 0;
			List<string> array = new List<string>();
			while (true)
			{
				string key = string.Format(format, baseIndex + i);
				if ((def == null || def.Length <= i) && !ValueExists(key))
					break; // デフォルト値配列が指定されていない場合はキーが見つからなくなった時点で終了
				string val;
				if (def != null && i < def.Length)
					val = GetString(key, def[i]);
				else
					val = GetString(key);
				array.Add(val);
				i++;
			}
			return array.ToArray();
		}
		/// <summary>
		///	指定された書式で生成された各キーから文字列を取得する
		/// </summary>
		public string[] GetStringArrayEachKey(int baseIndex, string format)
		{
			return GetStringArrayEachKey(baseIndex, format, null);
		}
		/// <summary>
		///	指定された書式で生成された各キーから正数を取得する
		/// </summary>
		public bool[] GetBoolArrayEachKey(int baseIndex, string format, params bool[] def)
		{
			int i = 0;
			List<bool> array = new List<bool>();
			while (true)
			{
				string key = string.Format(format, baseIndex + i);
				if ((def == null || def.Length <= i) && !ValueExists(key))
					break; // デフォルト値配列が指定されていない場合はキーが見つからなくなった時点で終了
				bool val;
				if (def != null && i < def.Length)
					val = GetBool(key, def[i]);
				else
					val = GetBool(key);
				array.Add(val);
				i++;
			}
			return array.ToArray();
		}
		/// <summary>
		///	指定された書式で生成された各キーからboolを取得する
		/// </summary>
		public bool[] GetBoolArrayEachKey(int baseIndex, string format)
		{
			return GetBoolArrayEachKey(baseIndex, format, null);
		}
		/// <summary>
		///	指定された書式で生成された各キーから正数を取得する
		/// </summary>
		public int[] GetIntArrayEachKey(int baseIndex, string format, params int[] def)
		{
			int i = 0;
			List<int> array = new List<int>();
			while (true)
			{
				string key = string.Format(format, baseIndex + i);
				if ((def == null || def.Length <= i) && !ValueExists(key))
					break; // デフォルト値配列が指定されていない場合はキーが見つからなくなった時点で終了
				int val;
				if (def != null && i < def.Length)
					val = GetInt(key, def[i]);
				else
					val = GetInt(key);
				array.Add(val);
				i++;
			}
			return array.ToArray();
		}
		/// <summary>
		///	指定された書式で生成された各キーから整数を取得する
		/// </summary>
		public int[] GetIntArrayEachKey(int baseIndex, string format)
		{
			return GetIntArrayEachKey(baseIndex, format, null);
		}
		/// <summary>
		///	指定された書式で生成された各キーから実数を取得する
		/// </summary>
		public double[] GetDoubleArrayEachKey(int baseIndex, string format, params double[] def)
		{
			int i = 0;
			List<double> array = new List<double>();
			while (true)
			{
				string key = string.Format(format, baseIndex + i);
				if ((def == null || def.Length <= i) && !ValueExists(key))
					break; // デフォルト値配列が指定されていない場合はキーが見つからなくなった時点で終了
				double val;
				if (def != null && i < def.Length)
					val = GetDouble(key, def[i]);
				else
					val = GetDouble(key);
				array.Add(val);
				i++;
			}
			return array.ToArray();
		}
		/// <summary>
		///	指定された書式で生成された各キーから実数を取得する
		/// </summary>
		public double[] GetDoubleArrayEachKey(int baseIndex, string format)
		{
			return GetDoubleArrayEachKey(baseIndex, format, null);
		}
		#endregion

		#region GetArrayValue Methods
		/// <summary>
		///	指定されたキーの指定したインデックスのデータから文字列を取得する
		/// </summary>
		public string GetArrayValueByString(string key, int index, string def)
		{
			if (m_GetArrayValueCache == null || key != m_LastGetArrayValueKey)
			{
				m_GetArrayValueCache = GetStringArray(key);
				m_LastGetArrayValueKey = key;
			}

			if (m_GetArrayValueCache.Length <= index)
				return def;

			return m_GetArrayValueCache[index];
		}
		/// <summary>
		///	指定されたキーの指定したインデックスのデータから文字列を取得する
		/// </summary>
		public string GetArrayValueByString(string key, int index)
		{
			return GetArrayValueByString(key, index, "");
		}
		/// <summary>
		///	指定されたキーの指定したインデックスのデータからboolを取得する
		/// </summary>
		public bool GetArrayValueByBool(string key, int index, bool def)
		{
			if (m_GetArrayValueCache == null || key != m_LastGetArrayValueKey)
			{
				m_GetArrayValueCache = GetStringArray(key);
				m_LastGetArrayValueKey = key;
			}

			if (m_GetArrayValueCache.Length <= index)
				return def;

			double val;
			bool result = double.TryParse(m_GetArrayValueCache[index], out val);
			if (!result)
				return false;
			if (val == 1) return true;
			else return false;
			//            return (bool)val;
		}
		/// <summary>
		///	指定されたキーの指定したインデックスのデータからboolを取得する
		/// </summary>
		public bool GetArrayValueByBool(string key, int index)
		{
			return GetArrayValueByBool(key, index, false);
		}
		/// <summary>
		///	指定されたキーの指定したインデックスのデータから整数を取得する
		/// </summary>
		public int GetArrayValueByInt(string key, int index, int def)
		{
			if (m_GetArrayValueCache == null || key != m_LastGetArrayValueKey)
			{
				m_GetArrayValueCache = GetStringArray(key);
				m_LastGetArrayValueKey = key;
			}

			if (m_GetArrayValueCache.Length <= index)
				return def;

			double val;
			bool result = double.TryParse(m_GetArrayValueCache[index], out val);
			if (!result)
				return 0;

			return (int)val;
		}
		/// <summary>
		///	指定されたキーの指定したインデックスのデータから整数を取得する
		/// </summary>
		public int GetArrayValueByInt(string key, int index)
		{
			return GetArrayValueByInt(key, index, 0);
		}
		/// <summary>
		///	指定されたキーの指定したインデックスのデータから実数を取得する
		/// </summary>
		public double GetArrayValueByDouble(string key, int index, double def)
		{
			if (m_GetArrayValueCache == null || key != m_LastGetArrayValueKey)
			{
				m_GetArrayValueCache = GetStringArray(key);
				m_LastGetArrayValueKey = key;
			}

			if (m_GetArrayValueCache.Length <= index)
				return def;

			double val;
			bool result = double.TryParse(m_GetArrayValueCache[index], out val);
			if (!result)
				return 0.0;

			return val;
		}
		/// <summary>
		///	指定されたキーの指定したインデックスのデータから実数を取得する
		/// </summary>
		public double GetArrayValueByDouble(string key, int index)
		{
			return GetArrayValueByDouble(key, index, 0.0);
		}
		#endregion

		#region Get Methods
		/// <summary>
		///	指定されたキーから文字列を取得する
		/// </summary>
		public string GetString(string key, string def)
		{
			uint result;
			byte[] buf = new byte[256];
			while (true)
			{
				result = GetPrivateProfileStringByByteArray(m_Section, key, def, buf, (uint)buf.Length);
				if (result != buf.Length - 1)
					break;
				buf = new byte[buf.Length * 2];
			}
			return Encoding.Default.GetString(buf, 0, (int)result);
		}
		/// <summary>
		///	指定されたキーから文字列を取得する
		/// </summary>
		public string GetString(string key)
		{
			return GetString(key, "");
		}
		/// <summary>
		///	指定されたキーからboolを取得する
		/// </summary>
		public bool GetBool(string key, bool def)
		{
			int ndef;
			if (def) ndef = 1;
			else ndef = 0;
			int iw = (int)GetPrivateProfileInt(m_Section, key, ndef);
			if (iw == 1) return true;
			else return false;
			//            return (bool)GetPrivateProfileInt(m_Section, key, def);
		}
		/// <summary>
		///	指定されたキーからboolを取得する
		/// </summary>
		public bool GetBool(string key)
		{
			return GetBool(key, false);
			//            return (bool)GetPrivateProfileInt(m_Section, key, 0);
		}
		/// <summary>
		///	指定されたキーから整数を取得する
		/// </summary>
		public int GetInt(string key, int def)
		{
			return (int)GetPrivateProfileInt(m_Section, key, def);
		}
		/// <summary>
		///	指定されたキーから整数を取得する
		/// </summary>
		public int GetInt(string key)
		{
			return (int)GetPrivateProfileInt(m_Section, key, 0);
		}
		/// <summary>
		///	指定されたキーから実数を取得する
		/// </summary>
		public double GetDouble(string key, double def)
		{
			double val;
			bool result = double.TryParse(GetString(key, def.ToString()), out val);
			if (!result)
				val = 0.0;
			return val;
		}
		/// <summary>
		///	指定されたキーから実数を取得する
		/// </summary>
		public double GetDouble(string key)
		{
			return GetDouble(key, 0.0);
		}
		#endregion

		#region ExchangeValue Methods
		/// <summary>
		///	指定されたキーへ文字列の設定／取得を行う
		/// </summary>
		public bool ExchangeValue(bool bWrite, string key, ref string val, string def)
		{
			if (bWrite)
			{
				return SetValue(key, val);
			}
			else
			{
				val = GetString(key, def);
				return true;
			}
		}
		/// <summary>
		///	指定されたキーへ文字列の設定／取得を行う
		/// </summary>
		public bool ExchangeValue(bool bWrite, string key, ref string val)
		{
			return ExchangeValue(bWrite, key, ref val, "");
		}
		/// <summary>
		///	指定されたキーへboolの設定／取得を行う
		/// </summary>
		public bool ExchangeValue(bool bWrite, string key, ref bool val, bool def)
		{
			if (bWrite)
			{
				return SetValue(key, val);
			}
			else
			{
				val = GetBool(key, def);
				return true;
			}
		}
		/// <summary>
		///	指定されたキーへ整数の設定／取得を行う
		/// </summary>
		public bool ExchangeValue(bool bWrite, string key, ref int val, int def)
		{
			if (bWrite)
			{
				return SetValue(key, val);
			}
			else
			{
				val = GetInt(key, def);
				return true;
			}
		}
		/// <summary>
		///	指定されたキーへboolの設定／取得を行う
		/// </summary>
		public bool ExchangeValue(bool bWrite, string key, ref bool val)
		{
			return ExchangeValue(bWrite, key, ref val, false);
		}
		/// <summary>
		///	指定されたキーへ整数の設定／取得を行う
		/// </summary>
		public bool ExchangeValue(bool bWrite, string key, ref int val)
		{
			return ExchangeValue(bWrite, key, ref val, 0);
		}
		/// <summary>
		///	指定されたキーへ実数の設定／取得を行う
		/// </summary>
		public bool ExchangeValue(bool bWrite, string key, ref double val, double def)
		{
			if (bWrite)
			{
				return SetValue(key, val);
			}
			else
			{
				val = GetDouble(key, def);
				return true;
			}
		}
		/// <summary>
		///	指定されたキーへ実数の設定／取得を行う
		/// </summary>
		public bool ExchangeValue(bool bWrite, string key, ref double val)
		{
			return ExchangeValue(bWrite, key, ref val, 0.0);
		}
		#endregion

		#region Delete Methods
		/// <summary>
		///	指定されたキーを削除する
		/// </summary>
		public void DeleteKey(string section, string key)
		{
			WritePrivateProfileString(section, key, null);
		}
		/// <summary>
		///	指定されたキーを削除する
		/// </summary>
		public void DeleteKey(string key)
		{
			WritePrivateProfileString(m_Section, key, null);
		}
		/// <summary>
		///	指定されたセクションを削除する
		/// </summary>
		public void DeleteSection(string section)
		{
			WritePrivateProfileString(section, null, null);
		}
		#endregion

		#region List get Methods
		/// <summary>
		///	指定されたセクション内のキー一覧を取得する
		/// </summary>
		public string[] GetKeys(string section)
		{
			byte[] buf = new byte[2048];
			uint result;
			while (true)
			{
				result = GetPrivateProfileStringByByteArray(section, null, "", buf, (uint)buf.Length);
				if (result != buf.Length - 2)
					break;
				buf = new byte[buf.Length * 2];
			}
			return Encoding.Default.GetString(buf, 0, (int)result - 1).Split('\0');
		}
		/// <summary>
		///	全セクションの一覧を取得する
		/// </summary>
		public string[] GetSections()
		{
			byte[] buf = new byte[2048];
			uint result;
			while (true)
			{
				result = GetPrivateProfileStringByByteArray(null, null, "", buf, (uint)buf.Length);
				if (result != buf.Length - 2)
					break;
				buf = new byte[buf.Length * 2];
			}
			return Encoding.Default.GetString(buf, 0, (int)result - 1).Split('\0');
		}

		/// <summary>
		///	指定されたセクション内の全てのキーと値のペアを取得する
		/// </summary>
		public byte[] GetSectionData(string section)
		{
			byte[] buf = new byte[2048];
			uint result;
			while (true)
			{
				result = GetPrivateProfileSection(section, buf, (uint)buf.Length);
				if (result != buf.Length - 2)
					break;
				buf = new byte[buf.Length * 2];
			}
			return buf;
		}
		/// <summary>
		///	指定されたセクション内の全てのキーと値のペアを設定する
		/// </summary>
		public bool SetSectionData(string section, byte[] data)
		{
			return WritePrivateProfileSection(section, data) != 0;
		}
		#endregion

		#region Check Methods
		/// <summary>
		///	指定されたキーが存在しているか調べる
		/// </summary>
		public bool ValueExists(string section, string key)
		{
			byte[] buf = new byte[2];
			return GetPrivateProfileStringByByteArray(section, key, "", buf, 2) != 0;
		}
		/// <summary>
		///	指定されたキーが存在しているか調べる
		/// </summary>
		public bool ValueExists(string key)
		{
			return ValueExists(m_Section, key);
		}
		/// <summary>
		///	指定されたセクションが存在しているか調べる
		/// </summary>
		public bool SectionExists(string section)
		{
			byte[] buf = new byte[3];
			return GetPrivateProfileStringByByteArray(section, null, "", buf, 3) != 0;
		}
		#endregion

		#region API wrapper Methods
		private uint GetPrivateProfileStringByByteArray(string lpAppName, string lpKeyName, string lpDefault, byte[] lpReturnedString, uint nSize)
		{
			m_GetArrayValueCache = null;
			m_LastGetArrayValueKey = null;
			return GetPrivateProfileStringByByteArray(lpAppName, lpKeyName, lpDefault, lpReturnedString, nSize, m_FileName);
		}

		private uint GetPrivateProfileInt(string lpAppName, string lpKeyName, int nDefault)
		{
			m_GetArrayValueCache = null;
			m_LastGetArrayValueKey = null;
			return GetPrivateProfileInt(lpAppName, lpKeyName, nDefault, m_FileName);
		}

		private uint WritePrivateProfileString(string lpAppName, string lpKeyName, string lpString)
		{
			m_GetArrayValueCache = null;
			m_LastGetArrayValueKey = null;
			return WritePrivateProfileString(lpAppName, lpKeyName, lpString, m_FileName);
		}

		private uint WritePrivateProfileStringByStringBuilder(string lpAppName, string lpKeyName, StringBuilder lpString)
		{
			m_GetArrayValueCache = null;
			m_LastGetArrayValueKey = null;
			return WritePrivateProfileStringByStringBuilder(lpAppName, lpKeyName, lpString, m_FileName);
		}

		private uint GetPrivateProfileSection(string lpAppName, byte[] lpReturnedString, uint nSize)
		{
			m_GetArrayValueCache = null;
			m_LastGetArrayValueKey = null;
			return GetPrivateProfileSection(lpAppName, lpReturnedString, nSize, m_FileName);
		}

		private int WritePrivateProfileSection(string lpAppName, byte[] lpString)
		{
			m_GetArrayValueCache = null;
			m_LastGetArrayValueKey = null;
			return WritePrivateProfileSection(lpAppName, lpString, m_FileName);
		}
		#endregion
	};
}
