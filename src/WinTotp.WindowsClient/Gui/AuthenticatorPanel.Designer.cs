namespace WinTotp.WindowsClient.Gui
{
    partial class AuthenticatorPanel
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        private void InitializeComponent()
        {
            this.bindLayout = new System.Windows.Forms.TableLayoutPanel();
            this.picQr = new System.Windows.Forms.PictureBox();
            this.rightStack = new System.Windows.Forms.TableLayoutPanel();
            this.lblBindHint = new System.Windows.Forms.Label();
            this.txtBindSecret = new System.Windows.Forms.TextBox();
            this.bindButtons = new System.Windows.Forms.FlowLayoutPanel();
            this.btnCopySecret = new System.Windows.Forms.Button();
            this.btnRegenSecret = new System.Windows.Forms.Button();
            this.btnRefreshQr = new System.Windows.Forms.Button();
            this.codeRow = new System.Windows.Forms.FlowLayoutPanel();
            this.lblLocalPassword = new System.Windows.Forms.Label();
            this.lblRemain = new System.Windows.Forms.Label();
            this.btnCopyPassword = new System.Windows.Forms.Button();
            // Compatibility stubs (not shown)
            this.grpBind = new System.Windows.Forms.GroupBox();
            this.lblSecretCaption = new System.Windows.Forms.Label();
            this.lblPasswordCaption = new System.Windows.Forms.Label();

            this.bindLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picQr)).BeginInit();
            this.rightStack.SuspendLayout();
            this.bindButtons.SuspendLayout();
            this.codeRow.SuspendLayout();
            this.SuspendLayout();
            //
            // bindLayout — fixed QR column + content; height follows content
            //
            this.bindLayout.AutoSize = true;
            this.bindLayout.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.bindLayout.ColumnCount = 2;
            this.bindLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 128F));
            this.bindLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.bindLayout.Controls.Add(this.picQr, 0, 0);
            this.bindLayout.Controls.Add(this.rightStack, 1, 0);
            this.bindLayout.Dock = System.Windows.Forms.DockStyle.Top;
            this.bindLayout.Name = "bindLayout";
            this.bindLayout.RowCount = 1;
            this.bindLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            //
            // picQr — fixed square, top-aligned (not stretched)
            //
            this.picQr.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left;
            this.picQr.BackColor = System.Drawing.Color.White;
            this.picQr.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.picQr.Margin = new System.Windows.Forms.Padding(0, 0, 10, 0);
            this.picQr.Name = "picQr";
            this.picQr.Size = new System.Drawing.Size(118, 118);
            this.picQr.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picQr.TabStop = false;
            //
            // rightStack
            //
            this.rightStack.AutoSize = true;
            this.rightStack.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.rightStack.ColumnCount = 1;
            this.rightStack.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rightStack.Controls.Add(this.lblBindHint, 0, 0);
            this.rightStack.Controls.Add(this.txtBindSecret, 0, 1);
            this.rightStack.Controls.Add(this.bindButtons, 0, 2);
            this.rightStack.Controls.Add(this.codeRow, 0, 3);
            this.rightStack.Dock = System.Windows.Forms.DockStyle.Top;
            this.rightStack.Name = "rightStack";
            this.rightStack.RowCount = 4;
            this.rightStack.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.rightStack.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.rightStack.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.rightStack.RowStyles.Add(new System.Windows.Forms.RowStyle());
            //
            // lblBindHint
            //
            this.lblBindHint.AutoSize = true;
            this.lblBindHint.ForeColor = System.Drawing.Color.DimGray;
            this.lblBindHint.Margin = new System.Windows.Forms.Padding(0, 0, 0, 6);
            this.lblBindHint.MaximumSize = new System.Drawing.Size(300, 0);
            this.lblBindHint.Name = "lblBindHint";
            this.lblBindHint.Text = "扫码绑定。登录：登录选项 → TR → 6 位码。";
            //
            // txtBindSecret
            //
            this.txtBindSecret.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtBindSecret.Font = new System.Drawing.Font("Consolas", 9F);
            this.txtBindSecret.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
            this.txtBindSecret.Name = "txtBindSecret";
            this.txtBindSecret.ReadOnly = true;
            //
            // bindButtons
            //
            this.bindButtons.AutoSize = true;
            this.bindButtons.Controls.Add(this.btnCopySecret);
            this.bindButtons.Controls.Add(this.btnRegenSecret);
            this.bindButtons.Controls.Add(this.btnRefreshQr);
            this.bindButtons.Margin = new System.Windows.Forms.Padding(0, 0, 0, 6);
            this.bindButtons.Name = "bindButtons";
            //
            // btnCopySecret
            //
            this.btnCopySecret.AutoSize = true;
            this.btnCopySecret.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCopySecret.Margin = new System.Windows.Forms.Padding(0, 0, 4, 0);
            this.btnCopySecret.Name = "btnCopySecret";
            this.btnCopySecret.Text = "复制密钥";
            this.btnCopySecret.UseVisualStyleBackColor = true;
            this.btnCopySecret.Click += new System.EventHandler(this.btnCopySecret_Click);
            //
            // btnRegenSecret
            //
            this.btnRegenSecret.AutoSize = true;
            this.btnRegenSecret.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRegenSecret.Margin = new System.Windows.Forms.Padding(0, 0, 4, 0);
            this.btnRegenSecret.Name = "btnRegenSecret";
            this.btnRegenSecret.Text = "重新生成";
            this.btnRegenSecret.UseVisualStyleBackColor = true;
            this.btnRegenSecret.Click += new System.EventHandler(this.btnRegenSecret_Click);
            //
            // btnRefreshQr
            //
            this.btnRefreshQr.AutoSize = true;
            this.btnRefreshQr.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRefreshQr.Margin = new System.Windows.Forms.Padding(0);
            this.btnRefreshQr.Name = "btnRefreshQr";
            this.btnRefreshQr.Text = "刷新";
            this.btnRefreshQr.UseVisualStyleBackColor = true;
            this.btnRefreshQr.Click += new System.EventHandler(this.btnRefreshQr_Click);
            //
            // codeRow
            //
            this.codeRow.AutoSize = true;
            this.codeRow.Controls.Add(this.lblLocalPassword);
            this.codeRow.Controls.Add(this.lblRemain);
            this.codeRow.Controls.Add(this.btnCopyPassword);
            this.codeRow.Name = "codeRow";
            this.codeRow.WrapContents = false;
            //
            // lblLocalPassword
            //
            this.lblLocalPassword.AutoSize = true;
            this.lblLocalPassword.Font = new System.Drawing.Font("Consolas", 16F, System.Drawing.FontStyle.Bold);
            this.lblLocalPassword.ForeColor = System.Drawing.Color.FromArgb(16, 124, 16);
            this.lblLocalPassword.Margin = new System.Windows.Forms.Padding(0, 2, 10, 0);
            this.lblLocalPassword.Name = "lblLocalPassword";
            this.lblLocalPassword.Text = "------";
            //
            // lblRemain
            //
            this.lblRemain.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblRemain.AutoSize = true;
            this.lblRemain.ForeColor = System.Drawing.Color.DimGray;
            this.lblRemain.Margin = new System.Windows.Forms.Padding(0, 6, 8, 0);
            this.lblRemain.Name = "lblRemain";
            this.lblRemain.Text = "剩余 -- 秒";
            //
            // btnCopyPassword
            //
            this.btnCopyPassword.AutoSize = true;
            this.btnCopyPassword.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCopyPassword.Margin = new System.Windows.Forms.Padding(0, 2, 0, 0);
            this.btnCopyPassword.Name = "btnCopyPassword";
            this.btnCopyPassword.Text = "复制";
            this.btnCopyPassword.UseVisualStyleBackColor = true;
            this.btnCopyPassword.Click += new System.EventHandler(this.btnCopyPassword_Click);
            //
            this.grpBind.Visible = false;
            this.lblSecretCaption.Visible = false;
            this.lblPasswordCaption.Visible = false;
            //
            // AuthenticatorPanel
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.Controls.Add(this.bindLayout);
            this.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.Name = "AuthenticatorPanel";
            this.Padding = new System.Windows.Forms.Padding(2);
            this.Size = new System.Drawing.Size(440, 130);
            this.bindLayout.ResumeLayout(false);
            this.bindLayout.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picQr)).EndInit();
            this.rightStack.ResumeLayout(false);
            this.rightStack.PerformLayout();
            this.bindButtons.ResumeLayout(false);
            this.bindButtons.PerformLayout();
            this.codeRow.ResumeLayout(false);
            this.codeRow.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.GroupBox grpBind;
        private System.Windows.Forms.TableLayoutPanel bindLayout;
        private System.Windows.Forms.PictureBox picQr;
        private System.Windows.Forms.TableLayoutPanel rightStack;
        private System.Windows.Forms.Label lblBindHint;
        private System.Windows.Forms.Label lblSecretCaption;
        private System.Windows.Forms.TextBox txtBindSecret;
        private System.Windows.Forms.FlowLayoutPanel bindButtons;
        private System.Windows.Forms.Button btnCopySecret;
        private System.Windows.Forms.Button btnRegenSecret;
        private System.Windows.Forms.Button btnRefreshQr;
        private System.Windows.Forms.Label lblPasswordCaption;
        private System.Windows.Forms.Label lblLocalPassword;
        private System.Windows.Forms.Label lblRemain;
        private System.Windows.Forms.Button btnCopyPassword;
        private System.Windows.Forms.FlowLayoutPanel codeRow;
    }
}
