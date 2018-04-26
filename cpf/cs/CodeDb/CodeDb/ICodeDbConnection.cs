using System;
using System.Collections.Generic;
using System.Text;

namespace CodeDb {
	public interface ICodeDbConnection : IDisposable {
		object Core { get; }

		void Open();
		ICodeDbCommand CreateCommand();
	}
}
