using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PatchStalker {
	/// <summary>
	/// 埋め込みJavaScriptとの仲介オブジェクト
	/// </summary>
	public class Bridge {
		public event Action AddLinkStart;
		public event Action AddLinkEnd;

		public int Count;

		/// <summary>
		/// スタートページから現在のページまでの距離
		/// </summary>
		public int CurrentDistance = 0;

		/// <summary>
		/// 巡回済みアドレス
		/// </summary>
		public HashSet<string> KnownLinks { get; private set; } = new HashSet<string>();

		/// <summary>
		/// これから巡回するリンク、最後尾側が優先度高
		/// </summary>
		public List<Link> Links { get; private set; } = new List<Link>();


		public void testFunc(object[] values) {
		}

		public void start() {
			var d = this.AddLinkStart;
			if (d != null) {
				d();
			}
		}
		public void addLink(int id, string address, string keyword) {
			lock (this) {
				// 既に巡回したアドレスは無視する
				if (this.KnownLinks.Contains(address)) {
					return;
				}
				this.KnownLinks.Add(address);

				// 巡回するアドレスとして登録
				this.Links.Add(new Link(id, address, keyword, this.CurrentDistance, Link.CalcPriority(address, keyword, this.CurrentDistance)));
			}
		}
		public void setTable(int id, object[] tableData) {
			var table = new Table(tableData);
			// TODO: テーブルを構築し、リンクが関連する行や列から情報を取得しタグ付けしながら Link オブジェクトに情報を追加していく
		}
		public void setInnerText(string htmlText) {
			lock (this) {
				File.WriteAllText(this.Count + ".txt", htmlText, Encoding.UTF8);
				this.Count++;
			}
		}
		public void end() {
			lock (this) {
				this.Links.Sort((l, r) => l.Priority - r.Priority);
			}

			var d = this.AddLinkEnd;
			if (d != null) {
				d();
			}
		}
	}
}
