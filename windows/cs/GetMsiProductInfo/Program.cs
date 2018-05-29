using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace GetMsiProductInfo {
	class Program {
		const int MSIDBOPEN_READONLY = 0;  // database open read-only, no persistent changes
		const int MSIDBOPEN_TRANSACT = 1;  // database read/write in transaction mode
		const int MSIDBOPEN_DIRECT = 2;  // database direct read/write without transaction
		const int MSIDBOPEN_CREATE = 3;  // create new database, transact mode read/write
		const int MSIDBOPEN_CREATEDIRECT = 4;  // create new database, direct mode read/write
		const int MSIDBOPEN_PATCHFILE = 16; // add flag to indicate patch file

		[DllImport("msi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		static extern uint MsiOpenDatabaseW(string szDatabasePath, IntPtr phPersist, out IntPtr phDatabase);
		[DllImport("msi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		static extern uint MsiOpenPackageW(string szPackagePath, out IntPtr hProduct);
		[DllImport("msi.dll", ExactSpelling = true)]
		static extern uint MsiCloseHandle(IntPtr hAny);
		[DllImport("msi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		static extern uint MsiDatabaseExportW(IntPtr hDatabase, string szTableName, string szFolderPath, string szFileName);
		[DllImport("msi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		static extern uint MsiGetPropertyW(IntPtr hInstall, string szName, StringBuilder szValueBuf, ref uint pchValueBuf);

		static string MsiGetProperty(IntPtr hInstall, string szName) {
			var sb = new StringBuilder(4096);
			var length = (uint)sb.Capacity;
			var r = MsiGetPropertyW(hInstall, szName, sb, ref length);
			if (r != 0) {
				throw new Win32Exception((int)r);
			}
			return sb.ToString();
		}

		static int Main(string[] args) {
			if (args.Length < 1) {
				Console.WriteLine("usage : GetMsiProductInfo <.msi file>");
				return 0;
			}

			IntPtr hMsi;
			uint r;

			r = MsiOpenPackageW(args[0], out hMsi);
			if (r != 0) {
				throw new Win32Exception((int)r);
			}
			try {
				Console.WriteLine($"ProductName: {MsiGetProperty(hMsi, "ProductName")}");
				Console.WriteLine($"ProductCode: {MsiGetProperty(hMsi, "ProductCode")}");
				Console.WriteLine($"ProductVersion: {MsiGetProperty(hMsi, "ProductVersion")}");
			} finally {
				MsiCloseHandle(hMsi);
			}

			//IntPtr hMsiDb;
			//r = MsiOpenDatabaseW(args[0], new IntPtr(MSIDBOPEN_READONLY), out hMsiDb);
			//if (r != 0) {
			//	throw new Win32Exception((int)r);
			//}
			//try {
			//	r = MsiDatabaseExportW(hMsiDb, "Property", @"c:\work\msi", "afe.txt");
			//	if (r != 0) {
			//		throw new Win32Exception((int)r);
			//	}
			//} finally {
			//	MsiCloseHandle(hMsiDb);
			//}

			return 0;
		}
	}
}
