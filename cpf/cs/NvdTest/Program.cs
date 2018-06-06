using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using DbCode;

namespace NvdTest
{
	class Program {
		static void Main(string[] args) {
			// データベース関係初期化
			Db.Initialize();

			// データベースへCPEを登録
			using (var con = Db.CreateConnection()) {
				con.Open();
				var cmd = con.CreateCommand();

				using (var fs = new FileStream(@"D:\work\cve\nvdcve-1.0-2017.json", FileMode.Open, FileAccess.Read, FileShare.Read)) {
					dynamic root = DeserializeFromStream(fs);
					foreach (var i in root.CVE_Items) {
						var cve = i.cve;
						var configurations = i.configurations;

						var cveID = Db.AddCve(cmd, (string)cve.CVE_data_meta.ID);

						foreach (var vd in cve.affects.vendor.vendor_data) {
							Db.AddCveVendor(cmd, cveID, Db.AddVendor(cmd, (string)vd.vendor_name));
						}

						foreach (var rd in cve.references.reference_data) {
							Db.AddCveUrl(cmd, cveID, (string)rd.url);
						}

						foreach (var node in configurations.nodes) {
							OperatorTest(cmd, cveID, node);
						}
					}
				}
			}
		}

		public static void OperatorTest(IDbCodeCommand cmd, int cveID, dynamic node) {
			switch ((string)node["operator"]) {
			case "AND":
				OperatorAnd(cmd, cveID, node);
				break;
			case "OR":
				OperatorOr(cmd, cveID, node);
				break;
			default:
				throw new ApplicationException();
			}
		}

		public static void OperatorAnd(IDbCodeCommand cmd, int cveID, dynamic node) {
			var cpe = node.cpe;
			if (cpe != null) {
				Cpe(cmd, cveID, cpe);
			}
			var children = node.children;
			if (children != null) {
				Children(cmd, cveID, children);
			}
		}

		public static void OperatorOr(IDbCodeCommand cmd, int cveID, dynamic node) {
			Cpe(cmd, cveID, node.cpe);
		}

		public static void Children(IDbCodeCommand cmd, int cveID, dynamic children) {
			foreach (var child in children) {
				OperatorTest(cmd, cveID, child);
			}
		}

		public static void Cpe(IDbCodeCommand cmd, int cveID, dynamic cpes) {
			foreach (var cpe in cpes) {
				var c22 = cpe.cpe22Uri;
				var c23 = cpe.cpe23Uri;
				if (c22 != null) {
					Db.AddCveCpe22(cmd, cveID, Db.AddCpe22(cmd, (string)c22));
				}
				if (c23 != null) {
					Db.AddCveCpe23(cmd, cveID, Db.AddCpe23(cmd, (string)c23));
				} else {
					throw new ApplicationException();
				}
			}
		}

		public static object DeserializeFromStream(Stream stream) {
			var serializer = new Newtonsoft.Json.JsonSerializer();
			using (var sr = new StreamReader(stream))
			using (var jsonTextReader = new JsonTextReader(sr)) {
				return serializer.Deserialize(jsonTextReader);
			}
		}
	}
}
