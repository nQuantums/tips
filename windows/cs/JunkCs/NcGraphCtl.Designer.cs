namespace GCodeViewer {
	partial class NcGraphCtl {
		/// <summary> 
		/// 必要なデザイナー変数です。
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary> 
		/// 使用中のリソースをすべてクリーンアップします。
		/// </summary>
		/// <param name="disposing">マネージ リソースが破棄される場合 true、破棄されない場合は false です。</param>
		protected override void Dispose(bool disposing) {
			if (disposing && (components != null)) {
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region コンポーネント デザイナーで生成されたコード

		/// <summary> 
		/// デザイナー サポートに必要なメソッドです。このメソッドの内容を 
		/// コード エディターで変更しないでください。
		/// </summary>
		private void InitializeComponent() {
			this.graphCtl1 = new Lt.Graph.GraphCtl();
			this.scaleCtl1 = new Lt.Graph.ScaleCtl();
			this.scaleCtl2 = new Lt.Graph.ScaleCtl();
			this.SuspendLayout();
			// 
			// graphCtl1
			// 
			this.graphCtl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.graphCtl1.BackColor = System.Drawing.Color.White;
			this.graphCtl1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.graphCtl1.BottomLeftColor = System.Drawing.Color.White;
			this.graphCtl1.BottomRightColor = System.Drawing.Color.White;
			this.graphCtl1.Location = new System.Drawing.Point(68, 3);
			this.graphCtl1.Name = "graphCtl1";
			this.graphCtl1.Size = new System.Drawing.Size(549, 527);
			this.graphCtl1.TabIndex = 0;
			this.graphCtl1.Text = "graphCtl1";
			this.graphCtl1.TopLeftColor = System.Drawing.Color.White;
			this.graphCtl1.TopRightColor = System.Drawing.Color.White;
			// 
			// scaleCtl1
			// 
			this.scaleCtl1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.scaleCtl1.CodSysId = 1;
			this.scaleCtl1.FontScale = new System.Drawing.Font("MS UI Gothic", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
			this.scaleCtl1.FontScaleMaxMin = new System.Drawing.Font("MS UI Gothic", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
			this.scaleCtl1.FontUnit = new System.Drawing.Font("MS UI Gothic", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
			this.scaleCtl1.GraphCtl = this.graphCtl1;
			this.scaleCtl1.Location = new System.Drawing.Point(68, 536);
			this.scaleCtl1.Name = "scaleCtl1";
			this.scaleCtl1.ScaleAlign = Lt.Graph.ScaleAlign.Top;
			this.scaleCtl1.Size = new System.Drawing.Size(549, 25);
			this.scaleCtl1.TabIndex = 1;
			this.scaleCtl1.Text = "scaleCtl1";
			this.scaleCtl1.UnitPos = new System.Drawing.Point(50, 100);
			this.scaleCtl1.UnitSize = new System.Drawing.Size(50, 50);
			// 
			// scaleCtl2
			// 
			this.scaleCtl2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
			this.scaleCtl2.CodSysId = 2;
			this.scaleCtl2.FontScale = new System.Drawing.Font("MS UI Gothic", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
			this.scaleCtl2.FontScaleMaxMin = new System.Drawing.Font("MS UI Gothic", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
			this.scaleCtl2.FontUnit = new System.Drawing.Font("MS UI Gothic", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
			this.scaleCtl2.GraphCtl = this.graphCtl1;
			this.scaleCtl2.Location = new System.Drawing.Point(3, 3);
			this.scaleCtl2.Name = "scaleCtl2";
			this.scaleCtl2.Size = new System.Drawing.Size(59, 527);
			this.scaleCtl2.TabIndex = 2;
			this.scaleCtl2.Text = "scaleCtl2";
			this.scaleCtl2.UnitPos = new System.Drawing.Point(0, 50);
			this.scaleCtl2.UnitSize = new System.Drawing.Size(50, 50);
			// 
			// NcGraphCtl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.scaleCtl2);
			this.Controls.Add(this.scaleCtl1);
			this.Controls.Add(this.graphCtl1);
			this.Name = "NcGraphCtl";
			this.Size = new System.Drawing.Size(620, 564);
			this.ResumeLayout(false);

		}

		#endregion

		private Lt.Graph.GraphCtl graphCtl1;
		private Lt.Graph.ScaleCtl scaleCtl1;
		private Lt.Graph.ScaleCtl scaleCtl2;
	}
}
