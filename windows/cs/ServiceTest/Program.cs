using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace ServiceTest {
	public class Program : ServiceBase {
		public const string ConstServiceName = "AwesomeService";
		public const string ServiceDisplayName = "Awesome Service";
		public const string ServiceDescription = "This is awesome Service";

		public const string CommandAsService = "as_service";

		public static void Main(string[] args) {
			if (1 <= args.Length && args[0] == CommandAsService) {
				// Run as Windows Service
				ServiceBase.Run(new Program());
				return;
			}

			// Run as console mode when an argument is provided.
			if (1 <= args.Length) {
				RunAsConsoleMode(args[0]);
				return;
			}
		}

		public Program() {
			//自動ログ採取を有効
			this.AutoLog = true;

			// サービスの終了、停止可能
			this.CanShutdown = true;
			this.CanStop = true;

			// このサービスの名前
			this.ServiceName = ServiceName;

			this.EventLog.WriteEntry("コンストラクタが呼び出されました。");
		}

		// サービスを開始するときに呼び出されるメソッド
		protected override void OnStart(string[] args) {
			Console.WriteLine("テストサービスを開始します。");
			this.EventLog.WriteEntry("テストサービスを開始します。");
		}

		// サービスを停止するときに呼び出されるメソッド
		protected override void OnStop() {
			// お作法らしい
			this.RequestAdditionalTime(2000);
			this.EventLog.WriteEntry("テストサービスを停止します。");
			Console.WriteLine("テストサービスを停止します。");
			// 正常終了を通知
			this.ExitCode = 0;
		}


		// システムシャットダウン時に呼び出される
		protected override void OnShutdown() {
			this.EventLog.WriteEntry("テストサービスがシステムの終了を検知しました。");
			Console.WriteLine("テストサービスがシステムの終了を検知しました。");
		}

		/// <summary>
		/// Run as console mode when an argument is provided.
		/// </summary>
		static void RunAsConsoleMode(string arg) {
			string mode = arg.ToLower();
			var myAssembly = System.Reflection.Assembly.GetEntryAssembly();
			string path = myAssembly.Location;

			if (mode == "/i" || mode == "/u") {
				bool isInstallMode = (mode == "/i");
				var mes = (isInstallMode) ? "installed" : "uninstalled";
				if (IsServiceExists() == isInstallMode) {
					Console.WriteLine("{0} has been already {1}.", ServiceDisplayName, mes);
				} else {
					if (!isInstallMode) {
						StopService();
						Console.WriteLine("Service dtopped");
					}
					var param = isInstallMode ? new[] { path } : new[] { "/u", path };
					ManagedInstallerClass.InstallHelper(param);
					Console.WriteLine("{0} has been successfully {1}.", ServiceDisplayName, mes);
					if (isInstallMode) {
						StartService();
					}
				}
			} else if (mode == "/start") {
				StartService();
			} else if (mode == "/stop") {
				StopService();
			} else {
				Console.WriteLine("Provided arguments are unrecognized.");
			}
		}

		/// <summary>
		/// Whether the service already exists in the computer or not.
		/// </summary>
		static bool IsServiceExists(string name = ConstServiceName) {
			ServiceController[] services = ServiceController.GetServices();
			return services.Any(s => s.ServiceName == name);
		}

		/// <summary>
		/// Start the service.
		/// </summary>
		static void StartService() {
			if (IsServiceExists()) {
				Console.WriteLine("Starting {0}...", ConstServiceName);
				ServiceController sc = new ServiceController(ConstServiceName);
				if (sc.Status == ServiceControllerStatus.Running) {
					Console.WriteLine("{0} has been already started.", ServiceDisplayName);
				} else {
					try {
						sc.Start();
						Console.WriteLine("{0} has been started.", ServiceDisplayName);
					} catch (Exception) {
						Console.WriteLine("{0} could not be started.", ServiceDisplayName);
					}
				}
			}
		}

		/// <summary>
		/// Stop the service.
		/// </summary>
		static void StopService() {
			if (IsServiceExists()) {
				Console.WriteLine("Stopping {0}...", ConstServiceName);
				ServiceController sc = new ServiceController(ConstServiceName);
				if (sc.Status == ServiceControllerStatus.Stopped) {
					Console.WriteLine("{0} has been already stopped.", ServiceDisplayName);
				} else {
					try {
						sc.Stop();
						Console.WriteLine("{0} has been stopped.", ServiceDisplayName);
					} catch (Exception) {
						Console.WriteLine("{0} could not be stopped.", ServiceDisplayName);
					}
				}
			}
		}
	}

	[RunInstaller(true)]
	public class ProjectInstaller : Installer {
		public ProjectInstaller() {
			var spi = new ServiceProcessInstaller {
				Account = ServiceAccount.LocalSystem
			};
			var si = new ServiceInstaller {
				ServiceName = Program.ConstServiceName,
				DisplayName = Program.ServiceDisplayName,
				Description = Program.ServiceDescription,
				StartType = ServiceStartMode.Automatic,
			};
			this.Installers.Add(spi);
			this.Installers.Add(si);
		}

		protected virtual string AppendPathParameter(string path, string parameter) {
			if (path.Length > 0 && path[0] != '"') {
				path = "\"" + path + "\"";
			}
			path += " " + parameter;
			return path;
		}

		protected override void OnBeforeInstall(System.Collections.IDictionary savedState) {
			// コマンドライン引数にサービスとして起動された事を示す値を渡す様にオーバーライド
			Context.Parameters["assemblypath"] = AppendPathParameter(Context.Parameters["assemblypath"], Program.CommandAsService);
			base.OnBeforeInstall(savedState);
		}

		protected override void OnBeforeUninstall(System.Collections.IDictionary savedState) {
			// コマンドライン引数にサービスとして起動された事を示す値を渡す様にオーバーライド
			Context.Parameters["assemblypath"] = AppendPathParameter(Context.Parameters["assemblypath"], Program.CommandAsService);
			base.OnBeforeUninstall(savedState);
		}
	}
}
