using System;
using System.Collections.Generic;
using System.Text;

namespace CodeDb {
	public interface ICodeDbCommand : IDisposable {
		object Core { get; }
		ICodeDbConnection Connection { get; }
		string CommandText { get; set; }
		int CommandTimeout { get; set; }

		void Apply(SqlProgram program);

		void Cancel();
		int ExecuteNonQuery();
		ICodeDbDataReader ExecuteReader();
	}
}
