namespace Jk {
	partial class ExceptionMessageForm {
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing) {
			if (disposing && (components != null)) {
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent() {
			this.lblMessage = new System.Windows.Forms.Label();
			this.btnOK = new System.Windows.Forms.Button();
			this.chkViewDetail = new System.Windows.Forms.CheckBox();
			this.pnlMain = new System.Windows.Forms.Panel();
			this.tbDetail = new System.Windows.Forms.TextBox();
			this.pnlMain.SuspendLayout();
			this.SuspendLayout();
			// 
			// lblMessage
			// 
			this.lblMessage.AutoSize = true;
			this.lblMessage.Location = new System.Drawing.Point(32, 23);
			this.lblMessage.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.lblMessage.MaximumSize = new System.Drawing.Size(640, 1280);
			this.lblMessage.Name = "lblMessage";
			this.lblMessage.Size = new System.Drawing.Size(0, 15);
			this.lblMessage.TabIndex = 0;
			// 
			// btnOK
			// 
			this.btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnOK.Location = new System.Drawing.Point(192, 82);
			this.btnOK.Margin = new System.Windows.Forms.Padding(4);
			this.btnOK.Name = "btnOK";
			this.btnOK.Size = new System.Drawing.Size(100, 29);
			this.btnOK.TabIndex = 2;
			this.btnOK.Text = "OK";
			this.btnOK.UseVisualStyleBackColor = true;
			this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
			// 
			// chkViewDetail
			// 
			this.chkViewDetail.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.chkViewDetail.AutoSize = true;
			this.chkViewDetail.Location = new System.Drawing.Point(12, 92);
			this.chkViewDetail.Name = "chkViewDetail";
			this.chkViewDetail.Size = new System.Drawing.Size(149, 19);
			this.chkViewDetail.TabIndex = 3;
			this.chkViewDetail.Text = "エラー内容詳細表示";
			this.chkViewDetail.UseVisualStyleBackColor = true;
			this.chkViewDetail.CheckedChanged += new System.EventHandler(this.chkViewDetail_CheckedChanged);
			// 
			// pnlMain
			// 
			this.pnlMain.Controls.Add(this.lblMessage);
			this.pnlMain.Controls.Add(this.btnOK);
			this.pnlMain.Controls.Add(this.chkViewDetail);
			this.pnlMain.Dock = System.Windows.Forms.DockStyle.Top;
			this.pnlMain.Location = new System.Drawing.Point(0, 0);
			this.pnlMain.Name = "pnlMain";
			this.pnlMain.Size = new System.Drawing.Size(305, 119);
			this.pnlMain.TabIndex = 4;
			// 
			// tbDetail
			// 
			this.tbDetail.BackColor = System.Drawing.SystemColors.Window;
			this.tbDetail.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tbDetail.Location = new System.Drawing.Point(0, 119);
			this.tbDetail.Margin = new System.Windows.Forms.Padding(4);
			this.tbDetail.MaxLength = 99999999;
			this.tbDetail.Multiline = true;
			this.tbDetail.Name = "tbDetail";
			this.tbDetail.ReadOnly = true;
			this.tbDetail.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.tbDetail.Size = new System.Drawing.Size(305, 0);
			this.tbDetail.TabIndex = 5;
			this.tbDetail.Visible = false;
			this.tbDetail.WordWrap = false;
			// 
			// ExceptionMessageForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(305, 119);
			this.Controls.Add(this.tbDetail);
			this.Controls.Add(this.pnlMain);
			this.Font = new System.Drawing.Font("MS UI Gothic", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
			this.Margin = new System.Windows.Forms.Padding(4);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "ExceptionMessageForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.pnlMain.ResumeLayout(false);
			this.pnlMain.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label lblMessage;
		private System.Windows.Forms.Button btnOK;
		private System.Windows.Forms.CheckBox chkViewDetail;
		private System.Windows.Forms.Panel pnlMain;
		private System.Windows.Forms.TextBox tbDetail;
	}
}