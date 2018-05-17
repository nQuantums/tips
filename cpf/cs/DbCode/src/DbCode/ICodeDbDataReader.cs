using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace DbCode {
	public interface IDbCodeDataReader : IDisposable {
		object Core { get; }

		bool Read();
		bool NextResult();

		bool GetBoolean(int ordinal);
		char GetChar(int ordinal);
		int GetInt32(int ordinal);
		long GetInt64(int ordinal);
		double GetDouble(int ordinal);
		string GetString(int ordinal);
		Guid GetGuid(int ordinal);
		DateTime GetDateTime(int ordinal);
		object GetValue(int ordinal);
	}
}
