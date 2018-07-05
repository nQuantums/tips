using System;
using System.Collections.Generic;
using System.Text;

namespace Jk {
	/// <summary>
	/// CSV関係の処理をまとめたクラス。
	/// </summary>
	public static class Csv {
		/// <summary>
		/// CSV形式の文字列をフィールドに分ける。
		/// </summary>
		/// <param name="text">フィールドに分割したい文字列。</param>
		/// <param name="sp">セパレータ文字列</param>
		/// <returns>フィールドの配列。</returns>
		public static string[] TextSplit(string text, char sp) {
			if (text == null)
				return new string[0];

			int i; // 作業用ポインタ
			int start; // フィールドの開始位置
			int end; // フィールドの終端位置+1(区切り文字または終端の位置)
			int len = text.Length; // 文字列長
			List<string> fields = new List<string>(); // フィールド配列

			// 文字列からフィールドを取り出していく
			i = 0;
			while (i <= len) {
				// フィールドの先頭を探す
				while (i < len && (text[i] == ' ' || text[i] == '\t')) {
					i++;
				}

				// ダブルクォーテーションで括られているか調べる
				char sp2 = sp;
				if (i < len && text[i] == '"') {
					sp2 = '"';
					i++;
				}

				// この時点でフィールドの先頭が確定
				start = i;

				// 区切り文字を探しフィールドの終端を確定する
				end = text.IndexOf(sp2, start);
				if (end < 0)
					end = len;

				// フィールド配列にフィールドを追加
				fields.Add(text.Substring(start, end - start));

				// 区切り文字がオーバーロードされていた場合には元の区切り文字まで進める
				if (sp != sp2) {
					end = text.IndexOf(sp, end);
					if (end < 0)
						end = len;
				}

				// フィールド終端が文字列の終端に一致しているなら全て切り出し完了
				if (end == len)
					break;

				// 次フィールドの検索開始位置へ移動
				i = end + 1;
			}

			return fields.ToArray();
		}

		/// <summary>
		/// 指定区切り文字でフィールドを切り分けて取得する
		/// </summary>
		/// <param name="text">[in] 切り出したい文字列</param>
		/// <param name="separator">[in] 区切り文字</param>
		/// <param name="bundler">[in] 括り文字</param>
		/// <returns>分割後のフィールド配列が返る</returns>
		public static List<string> Split(string text, char separator, char bundler) {
			if (string.IsNullOrEmpty(text))
				return new List<string>();

			int p = 0; // 作業用ポインタ
			int pFieldStart; // フィールドの開始位置
			int pFieldEnd; // フィールドの終端位置+1(区切り文字または終端の位置)
			int pTextEnd = text.Length; // 文字列長
			StringBuilder field = new StringBuilder(); // フィールド文字配列
			List<string> fields = new List<string>(); // 分割後のフィールド配列

			// 文字列からフィールドを取り出していく
			while (p <= pTextEnd) {
				// フィールドの先頭を探す
				while (p < pTextEnd && (text[p] == ' ')) {
					p++;
				}

				// ダブルクォーテーションで括られているか調べる
				char sp2 = separator;
				if (bundler != 0 && p < pTextEnd && text[p] == bundler) {
					sp2 = bundler;
					p++;
				}

				// この時点でフィールドの先頭が確定
				pFieldStart = p;

				// 区切り文字を探しフィールドの終端を確定する
				field.Length = 0;
				pFieldEnd = pFieldStart;
				while (pFieldEnd < pTextEnd) {
					int move = 1;
					if (text[pFieldEnd] == sp2) {
						if (separator != sp2 && pFieldEnd + 1 < text.Length && text[pFieldEnd + 1] == sp2)
							move = 2; // ダブルクォーテーションが２つ並んでいるならそれは終端ではない
						else
							break;
					}
					field.Append(text[pFieldEnd]);
					pFieldEnd += move;
				}

				// フィールド配列にフィールドを追加
				if (field.Length == 0)
					fields.Add("");
				else
					fields.Add(field.ToString());

				// 区切り文字がオーバーロードされていた場合には元の区切り文字まで進める
				if (separator != sp2) {
					while (pFieldEnd < pTextEnd) {
						if (text[pFieldEnd] == separator)
							break;
						pFieldEnd++;
					}
				}

				// フィールド終端が文字列の終端に一致しているなら全て切り出し完了
				if (pFieldEnd == pTextEnd)
					break;

				// 次フィールドの検索開始位置へ移動
				p = pFieldEnd + 1;
			}

			return fields;
		}

		/// <summary>
		/// 指定区切り文字でフィールドを連結して取得する
		/// </summary>
		/// <param name="fields">[in] 結果の分割された文字列が追加される配列</param>
		/// <param name="separator">[in] 区切り文字</param>
		/// <param name="bundler">[in] 括り文字</param>
		/// <returns>連結後の文字列</returns>
		public static string Combine(IEnumerable<string> fields, char separator, char bundler) {
			char[] spchars;
			string rep1 = null, rep2 = null;
			StringBuilder text = new StringBuilder();

			if (bundler == 0) {
				spchars = new char[1];
				spchars[0] = separator;
			} else {
				spchars = new char[2];
				spchars[0] = separator;
				spchars[1] = bundler;

				rep1 = new string(new char[] { bundler });
				rep2 = new string(new char[] { bundler, bundler });
			}

			bool firstField = true;
			foreach (var s in fields) {
				if (firstField) {
					firstField = false;
				} else {
					text.Append(separator);
				}

				if (!string.IsNullOrEmpty(s)) {
					if (bundler == 0 || s.IndexOfAny(spchars) < 0) {
						// 括り文字指定が無いまたはフィールド内に区切り文字が含まれていない場合には括らない
						text.Append(s);
					} else {
						// 括り文字指定があり且つフィールド内に区切り文字が含まれている場合には括る
						// その際括り文字を２連に置き換える
						text.Append(bundler);
						text.Append(s.Replace(rep1, rep2));
						text.Append(bundler);
					}
				}
			}

			return text.ToString();
		}

		/// <summary>
		/// 配列から指定フィールドを取得する、配列サイズが足りなかったらデフォルト値が返る
		/// </summary>
		/// <param name="fields">[in] 配列</param>
		/// <param name="index">[in] 配列内のインデックス</param>
		/// <param name="def">[in] デフォルト値</param>
		/// <returns>指定位置のフィールド値</returns>
		public static string GetField(List<string> fields, int index, string def = null) {
			if (index < 0 || fields.Count <= index) {
				if (def != null)
					return def;
				else
					return "";
			}
			return fields[index];
		}

		/// <summary>
		/// 配列から指定フィールドを取得する、配列サイズが足りなかったらデフォルト値が返る
		/// </summary>
		/// <param name="fields">[in] 配列</param>
		/// <param name="index">[in] 配列内のインデックス</param>
		/// <param name="def">[in] デフォルト値</param>
		/// <returns>指定位置のフィールド値</returns>
		public static int GetField(List<string> fields, int index, int def) {
			int value;
			if (index < 0 || fields.Count <= index || !int.TryParse(fields[index].Trim(), out value)) {
				return def;
			}
			return value;
		}

		/// <summary>
		/// 配列から指定フィールドを取得する、配列サイズが足りなかったらデフォルト値が返る
		/// </summary>
		/// <param name="fields">[in] 配列</param>
		/// <param name="index">[in] 配列内のインデックス</param>
		/// <param name="def">[in] デフォルト値</param>
		/// <returns>指定位置のフィールド値</returns>
		public static long GetField(List<string> fields, int index, long def) {
			long value;
			if (index < 0 || fields.Count <= index || !long.TryParse(fields[index].Trim(), out value)) {
				return def;
			}
			return value;
		}

		/// <summary>
		/// 配列から指定フィールドを取得する、配列サイズが足りなかったらデフォルト値が返る
		/// </summary>
		/// <param name="fields">[in] 配列</param>
		/// <param name="index">[in] 配列内のインデックス</param>
		/// <param name="def">[in] デフォルト値</param>
		/// <returns>指定位置のフィールド値</returns>
		public static double GetField(List<string> fields, int index, double def) {
			double value;
			if (index < 0 || fields.Count <= index || !double.TryParse(fields[index].Trim(), out value)) {
				return def;
			}
			return value;
		}
	}
}
