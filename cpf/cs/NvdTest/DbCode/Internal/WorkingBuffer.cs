using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using DbCode;

namespace DbCode.Internal {

	/// <summary>
	/// ビルド作業用のバッファ
	/// </summary>
	public class WorkingBuffer {
		/// <summary>
		/// 記号一覧
		/// </summary>
		public const string Symbols = "(),.+-*/%=<>#;";

		/// <summary>
		/// <see cref="System.Text.StringBuilder"/>または<see cref="ElementCode"/>のリスト、最終的に<see cref="IDbCodeCommand.CommandText"/>にセットされるテキストに変換される
		/// </summary>
		List<object> Buffer = new List<object>();

		/// <summary>
		/// 最終的に<see cref="IDbCodeCommand"/>で使用されるパラメータ
		/// </summary>
		public List<Parameter> Parameters { get; private set; } = new List<Parameter>();

		/// <summary>
		/// テーブル一覧、テーブルにエイリアス名を付けるために使用する
		/// </summary>
		public List<object> Tables { get; private set; } = new List<object>();

		/// <summary>
		/// <see cref="IDelayedCode"/>を含んでいるかどうか
		/// </summary>
		public bool HasDelayedCode {
			get {
				var buffer = this.Buffer;
				for (int i = 0, n = buffer.Count; i < n; i++) {
					if (buffer[i] is IDelayedCode) {
						return true;
					}
				}
				return false;
			}
		}

		/// <summary>
		/// 指定オブジェクトのパラメータ名を取得する
		/// </summary>
		/// <param name="value">オブジェクト</param>
		/// <returns>パラメータ名</returns>
		public string GetParameterName(object value) {
			// IndexOf などでは Variable のオーバーロードのせいで正しく判定できないので自前で object.ReferenceEquals 呼び出して判定する
			var parameters = this.Parameters;
			Parameter p;
			for (int i = 0, n = parameters.Count; i < n; i++) {
				// リファレンス先として同じのが登録されてたら名前返す
				p = parameters[i];
				if (value == p.Value) {
					return p.Name;
				}
			}

			// インデックス番号を名前としてパラメータ作成する
			var index = parameters.Count;
			p = new Parameter("@p" + index, value, value is Argument);
			parameters.Add(p);
			return p.Name;
		}

		/// <summary>
		/// テーブルのエイリアス名を取得する
		/// </summary>
		/// <param name="table">テーブル</param>
		/// <returns>エイリアス名</returns>
		public string GetTableAlias(object table) {
			var tables = this.Tables;
			var index = tables.IndexOf(table);
			if (0 <= index) {
				return "t" + index;
			}
			index = tables.Count;
			tables.Add(table);
			return "t" + index;
		}

		/// <summary>
		/// バッファの終端に文字列を連結する
		/// </summary>
		/// <param name="value">文字列</param>
		public void Concat(string value) {
			// 連結するものが無ければ何もしない
			if (value is null || value.Length == 0) {
				return;
			}

			// 連結先 StringBuffer の取得
			StringBuilder sb;
			var items = this.Buffer;
			if (items.Count == 0) {
				items.Add(sb = new StringBuilder());
			} else {
				var sb2 = items[items.Count - 1] as StringBuilder;
				if (sb2 is null) {
					items.Add(sb = new StringBuilder());
				} else {
					sb = sb2;
				}
			}

			// 記号系以外が連続してしまうならスペースを挟む
			if (sb.Length != 0 && Symbols.IndexOf(sb[sb.Length - 1]) < 0 && Symbols.IndexOf(value[0]) < 0) {
				sb.Append(' ');
			}

			// 連結
			sb.Append(value);
		}

		/// <summary>
		/// 遅延評価されるコードを追加する
		/// </summary>
		/// <param name="code">コード</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Add(IDelayedCode code) {
			this.Buffer.Add(code);
		}

		/// <summary>
		/// 指定のバッファへコマンドテキストを追加する
		/// </summary>
		/// <param name="destWorkingBuffer">追加先バッファ</param>
		public void Build(WorkingBuffer destWorkingBuffer) {
			destWorkingBuffer.Parameters.AddRange(this.Parameters);
			destWorkingBuffer.Tables.AddRange(this.Tables);

			var buffer = this.Buffer;
			for (int i = 0, n = buffer.Count; i < n; i++) {
				var obj = buffer[i];
				var sb = obj as StringBuilder;
				if (sb != null) {
					destWorkingBuffer.Concat(sb.ToString());
				} else {
					var dc = obj as IDelayedCode;
					if (dc != null) {
						dc.Build(destWorkingBuffer);
					}
				}
			}
		}

		/// <summary>
		/// 指定のバッファへコマンドテキストを追加する
		/// </summary>
		/// <returns>コマンドテキスト</returns>
		public string Build() {
			var buffer = this.Buffer;
			if (buffer.Count == 0) {
				return "";
			} else if (buffer.Count == 1) {
				return buffer[0].ToString();
			} else {
				var sb = new StringBuilder();
				for (int i = 0, n = buffer.Count; i < n; i++) {
					sb.Append(buffer[i]);
				}
				return sb.ToString();
			}
		}
	}
}
