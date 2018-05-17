using System;
using System.Collections.Generic;
using System.Text;

namespace DbCode {
	public interface IDbCodeConnection : IDisposable {
		object Core { get; }

		void Open();
		IDbCodeCommand CreateCommand();
	}
}
