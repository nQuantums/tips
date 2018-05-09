using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PatchStalker {
	/// <summary>
	/// リンク情報
	/// </summary>
	public class Link {
		/// <summary>
		/// リンクのID
		/// </summary>
		public int Id { get; private set; }

		/// <summary>
		/// リンク先アドレス
		/// </summary>
		public string Address { get; set; }

		/// <summary>
		/// リンク時のキーワード
		/// </summary>
		public string Keyword { get; set; }

		/// <summary>
		/// スタートページからリンク先アドレスまでの距離
		/// </summary>
		public int Distance { get; set; }

		/// <summary>
		/// 巡回優先度
		/// </summary>
		public int Priority { get; set; }

		/// <summary>
		/// リンク先アドレスがインストーラーかどうか
		/// </summary>
		public bool IsInstaller { get; private set; }

		/// <summary>
		/// コンストラクタ、全要素を指定して初期化する
		/// </summary>
		public Link(int id, string address, string keyword, int distance, int priority) {
			this.Id = id;
			this.Address = address;
			this.Keyword = keyword;
			this.Distance = distance;
			this.Priority = priority;
			this.IsInstaller = IsAddressInstaller(address);
		}

		public override string ToString() {
			return string.Join(" : ", this.Keyword, this.Address);
		}

		/// <summary>
		/// 指定アドレスがインストーラーかどうか判定する
		/// </summary>
		/// <param name="address">アドレス</param>
		/// <returns>インストーラーなら true</returns>
		public static bool IsAddressInstaller(string address) {
			var index = address.LastIndexOf('.');
			if (index < 0) {
				return false;
			}
			var ext = address.Substring(index).ToLower();
			switch (ext) {
			case ".exe":
			case ".msi":
			case ".msu":
			case ".msp":
				return true;
			default:
				return false;
			}
		}

		/// <summary>
		/// 指定アドレス、キーワード、距離から優先度を計算する
		/// </summary>
		/// <param name="address">リンク先アドレス</param>
		/// <param name="keyword">リンク関連キーワード</param>
		/// <param name="distance">スタートページからリンク先までの距離</param>
		/// <returns>優先度</returns>
		public static int CalcPriority(string address, string keyword, int distance) {
			int priority = 0;

			// リンク先がインストーラーのファイルならかなりポイント高い
			if (IsAddressInstaller(address)) {
				priority += 1000;
			}

			// リンク先関連のキーワードに製品名があればまぁまぁポイント高い
			if (Matching.IsMatched(keyword)) {
				priority += 100;
			}

			// なるべく開始ページから近いものを選びたいので距離が離れたらポイント減
			if (distance != 0) {
				priority /= distance;
			}

			return priority;
		}
	}
}
