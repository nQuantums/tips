using System;
using System.Collections.Generic;
using System.Text;

namespace DbCode {
	public interface IDbCodeCommand : IDisposable {
		object Core { get; }
		IDbCodeConnection Connection { get; }
		string CommandText { get; set; }
		int CommandTimeout { get; set; }

		void Cancel();
		int ExecuteNonQuery();
		int ExecuteNonQuery(Commandable program);
		IDbCodeDataReader ExecuteReader();
		IDbCodeDataReader ExecuteReader(Commandable program);
	}
}
