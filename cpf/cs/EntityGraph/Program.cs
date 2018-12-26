using System;
using System.IO;
using System.Text;

#pragma warning disable CS0649

namespace EntityGraph {
	class Program {
		public class HSC : PC {
			public hvn_db hvn_db;
			public App JVNGetter;
			public App NVDGetter;
			public App WSUSGetter;
			public App VendorPatchGet;
			public App VendorPatchReport;
			public App VendorPatchApply;
			public App OfficePatchUpdater;
			public App WinPatchUpdater;
			public App WinVerMaintenance;
			public App WinUpdPatchEditor;
			public App CpeEdit;
			public App PatchMapGetter;
			public App HvnDbFinalize;
			public App HvnDbCheck;
			public App HvnDbBuild;
			public Csv VendorPatches;
			public Csv VendorPatchesModified;
			public Csv VendorPatchesNew;
			public Zip hvn_db_zip;
		}

		public class hvn_db : Db {
			public Tbl test1;
			public Tbl test2;
		}

		public class PCloud : PC {
			public Zip hvn_db_zip;
		}

		public class Web : Entity {
			public WebSite iPedia = new WebSite("https://jvndb.jvn.jp/");
			public WebSite NvdDataFeeds = new WebSite("https://jvndb.jvn.jp/");
			public WebSite Adobe = new WebSite("https://helpx.adobe.com/jp/acrobat/release-note/release-notes-acrobat-reader.html");
			public WebSite Autodesk = new WebSite("https://knowledge.autodesk.com/ja/download");
			public WebSite Justsystems = new WebSite("http://support.justsystems.com/jp/");
			public WebSite Firefox = new WebSite("https://ftp.mozilla.org/pub/firefox/");
			public WebSite Thunderbird = new WebSite("https://ftp.mozilla.org/pub/thunderbird/");
			public WebSite Microsoft = new WebSite("");
			public DataFile wsusscn2 = new DataFile { Name = "wsusscn2.cab" };

			public Web() : base("Web") {
			}
		}

		public class UserPServer : PC {
			public App PServer;
			public App CheckVersion;
			public App Tonton;
			public App DbReplicator;
			public Db hvn_db;
			public Db asset_db;
			public Zip hvn_db_zip;

			public UserPServer() : base() {
			}
		}

		public class World : Entity {
			public Web Web;
			public HSC HSC;
			public PCloud PCloud;
			public UserPServer UserPServer;

			public World() : base(null) {
			}
		}

		private static void Main(string[] args) {
			var w = new World();
			var g = new Graph();
			var man = new Man { Name = "作業者" };
			var adm = new Man { Name = "管理者" };


			var hsc = w.HSC;
			var web = w.Web;
			var hvn_db = hsc.hvn_db;

			var hscTask = g.Task("HSC作業");

			hscTask.Flow(web.Microsoft, web.wsusscn2, hsc.JVNGetter);
			hscTask.Flow(web.iPedia, hsc.JVNGetter, hvn_db);
			hscTask.Flow(web.NvdDataFeeds, hsc.NVDGetter, hvn_db);
			hscTask.Flow(hsc.WSUSGetter, hvn_db);
			hscTask.Flow(web.wsusscn2, hsc.OfficePatchUpdater, hvn_db);
			hscTask.Flow(web.wsusscn2, hsc.WinPatchUpdater, hvn_db);

			hscTask.Flow(hvn_db.test1, hsc.HvnDbCheck);

			hscTask.Flow(hvn_db, hsc.VendorPatchGet, hsc.VendorPatches, hsc.VendorPatchReport);
			hscTask.Flow(hsc.VendorPatchReport, hsc.VendorPatchesNew);
			hscTask.Flow(hsc.VendorPatchReport, hsc.VendorPatchesModified);
			hscTask.Flow(hsc.VendorPatchesNew, hsc.VendorPatchApply);
			hscTask.Flow(hsc.VendorPatchesModified, hsc.VendorPatchApply);

			hscTask.Flow(web.Adobe, hsc.VendorPatchReport);
			hscTask.Flow(web.Autodesk, hsc.VendorPatchReport);
			hscTask.Flow(web.Justsystems, hsc.VendorPatchReport);
			hscTask.Flow(web.Firefox, hsc.VendorPatchReport);
			hscTask.Flow(web.Thunderbird, hsc.VendorPatchReport);

			hscTask.Flow(hsc.VendorPatchApply, hvn_db);

			hscTask.Flow(hvn_db, hsc.CpeEdit, hvn_db);
			hscTask.Flow(man, hsc.CpeEdit);

			hscTask.Flow(web.Microsoft, hsc.PatchMapGetter, hvn_db);

			hscTask.Flow(web.Microsoft, hsc.WinVerMaintenance, hvn_db);

			hscTask.Flow(hvn_db, hsc.WinUpdPatchEditor, hvn_db);
			hscTask.Flow(man, hsc.WinUpdPatchEditor);

			hscTask.Flow(hvn_db, hsc.HvnDbFinalize, hvn_db);
			hscTask.Flow(hvn_db, hsc.HvnDbBuild, hsc.hvn_db_zip);

			hscTask.Flow(hsc.hvn_db_zip, w.PCloud.hvn_db_zip);

			hscTask.Flow(w.PCloud.hvn_db_zip, w.UserPServer.hvn_db_zip, w.UserPServer.DbReplicator, w.UserPServer.hvn_db);
			hscTask.Flow(w.UserPServer.hvn_db, w.UserPServer.Tonton, w.UserPServer.asset_db);

			File.WriteAllText("out.dot", g.GetDotCode(), Encoding.UTF8);
		}
	}
}
