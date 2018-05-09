using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace NMeCab.Properties
{
	[GeneratedCode("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "10.0.0.0")]
	[CompilerGenerated]
	internal sealed class Settings : ApplicationSettingsBase
	{
		private static Settings defaultInstance = (Settings)SettingsBase.Synchronized(new Settings());

		public static Settings Default
		{
			get
			{
				return Settings.defaultInstance;
			}
		}

		[DebuggerNonUserCode]
		[ApplicationScopedSetting]
		[DefaultSettingValue("dic\\ipadic")]
		public string DicDir
		{
			get
			{
				return (string)((SettingsBase)this)["DicDir"];
			}
		}

		[DefaultSettingValue("")]
		[DebuggerNonUserCode]
		[ApplicationScopedSetting]
		public string UserDic
		{
			get
			{
				return (string)((SettingsBase)this)["UserDic"];
			}
		}

		[DebuggerNonUserCode]
		[ApplicationScopedSetting]
		[DefaultSettingValue("lattice")]
		public string OutputFormatType
		{
			get
			{
				return (string)((SettingsBase)this)["OutputFormatType"];
			}
		}

		private void SettingChangingEventHandler(object sender, SettingChangingEventArgs e)
		{
		}

		private void SettingsSavingEventHandler(object sender, CancelEventArgs e)
		{
		}
	}
}
