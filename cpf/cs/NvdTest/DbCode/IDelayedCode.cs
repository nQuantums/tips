using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DbCode.Internal;

namespace DbCode {
	/// <summary>
	/// <see cref="Commandable.CommandTextAndParameters"/>取得時に評価されるコード
	/// </summary>
	public interface IDelayedCode {
		/// <summary>
		/// 指定のバッファへコマンドテキストを追加する
		/// </summary>
		/// <param name="workingBuffer">追加先バッファ</param>
		void Build(WorkingBuffer workingBuffer);
	}

	public class DelayedCodeGenerator : IDelayedCode {
		public event Action<WorkingBuffer> Generate;

		public DelayedCodeGenerator() {
		}

		public DelayedCodeGenerator(Action<WorkingBuffer> generate) {
			this.Generate += generate;
		}

		public void Build(WorkingBuffer workingBuffer) {
			var d = this.Generate;
			if (d != null) {
				d(workingBuffer);
			}
		}
	}
}
