using System;
using System.Collections.Generic;
using System.Text;

namespace CodeDb {
	public interface ICodeDbCommand : IDisposable {
		object Core { get; }
		ICodeDbConnection Connection { get; }
		string CommandText { get; set; }
		int CommandTimeout { get; set; }

		void Cancel();
		int ExecuteNonQuery();
		int ExecuteNonQuery(Commandable program);
		ICodeDbDataReader ExecuteReader();
		ICodeDbDataReader ExecuteReader(Commandable program);
	}
}
