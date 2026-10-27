namespace WinTotp.WindowsClient.Gui
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            tabs = new TabControl();
            tabBind = new TabPage();
            panelBindHost = new Panel();
            tabLogin = new TabPage();
            loginLayout = new TableLayoutPanel();
            configGrid = new TableLayoutPanel();
            lblTargetUser = new Label();
            txtTargetUser = new TextBox();
            btnSaveConfig = new Button();
            lblStep = new Label();
            numStep = new NumericUpDown();
            lblServiceStatus = new Label();
            bootButtons = new FlowLayoutPanel();
            btnInstallCp = new Button();
            btnUninstallCp = new Button();
            btnDiagnoseCp = new Button();
            btnStartService = new Button();
            btnStopService = new Button();
            btnUninstallService = new Button();
            sessionPanel = new FlowLayoutPanel();
            btnOpenConfigDir = new Button();
            btnRefreshService = new Button();
            tabLog = new TabPage();
            txtLog = new TextBox();
            errorProvider = new ErrorProvider(components);
            rootLayout = new TableLayoutPanel();
            contentHost = new Panel();
            actionStack = new TableLayoutPanel();
            grpBootService = new GroupBox();
            bootStack = new TableLayoutPanel();
            grpConfig = new GroupBox();
            grpLog = new GroupBox();
            btnInstallService = new Button();
            lblConfigHint = new Label();
            configStack = new TableLayoutPanel();
            tabs.SuspendLayout();
            tabBind.SuspendLayout();
            tabLogin.SuspendLayout();
            loginLayout.SuspendLayout();
            configGrid.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numStep).BeginInit();
            bootButtons.SuspendLayout();
            sessionPanel.SuspendLayout();
            tabLog.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)errorProvider).BeginInit();
            SuspendLayout();
            // 
            // tabs
            // 
            tabs.Controls.Add(tabBind);
            tabs.Controls.Add(tabLogin);
            tabs.Controls.Add(tabLog);
            tabs.Dock = DockStyle.Fill;
            tabs.Font = new Font("Microsoft YaHei UI", 9F);
            tabs.Location = new Point(12, 12);
            tabs.Name = "tabs";
            tabs.Padding = new Point(12, 6);
            tabs.SelectedIndex = 0;
            tabs.Size = new Size(500, 386);
            tabs.TabIndex = 0;
            // 
            // tabBind
            // 
            tabBind.Controls.Add(panelBindHost);
            tabBind.Location = new Point(4, 32);
            tabBind.Name = "tabBind";
            tabBind.Padding = new Padding(6);
            tabBind.Size = new Size(492, 350);
            tabBind.TabIndex = 0;
            tabBind.Text = "验证器";
            tabBind.UseVisualStyleBackColor = true;
            // 
            // panelBindHost
            // 
            panelBindHost.Dock = DockStyle.Fill;
            panelBindHost.Location = new Point(8, 8);
            panelBindHost.Name = "panelBindHost";
            panelBindHost.Size = new Size(476, 334);
            panelBindHost.TabIndex = 0;
            // 
            // tabLogin
            // 
            tabLogin.Controls.Add(loginLayout);
            tabLogin.Location = new Point(4, 32);
            tabLogin.Name = "tabLogin";
            tabLogin.Padding = new Padding(6);
            tabLogin.Size = new Size(492, 350);
            tabLogin.TabIndex = 1;
            tabLogin.Text = "登录组件";
            tabLogin.UseVisualStyleBackColor = true;
            // 
            // loginLayout
            // 
            loginLayout.ColumnCount = 1;
            loginLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            loginLayout.Controls.Add(configGrid, 0, 0);
            loginLayout.Controls.Add(lblServiceStatus, 0, 1);
            loginLayout.Controls.Add(bootButtons, 0, 2);
            loginLayout.AutoSize = true;
            loginLayout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            loginLayout.Dock = DockStyle.Fill;
            loginLayout.Name = "loginLayout";
            loginLayout.Padding = new Padding(2, 0, 2, 0);
            loginLayout.RowCount = 3;
            loginLayout.RowStyles.Add(new RowStyle());
            loginLayout.RowStyles.Add(new RowStyle());
            loginLayout.RowStyles.Add(new RowStyle());
            loginLayout.Size = new Size(472, 300);
            loginLayout.TabIndex = 0;
            // 
            // configGrid
            // 
            configGrid.AutoSize = true;
            configGrid.ColumnCount = 3;
            configGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72F));
            configGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            configGrid.ColumnStyles.Add(new ColumnStyle());
            configGrid.Controls.Add(lblTargetUser, 0, 0);
            configGrid.Controls.Add(txtTargetUser, 1, 0);
            configGrid.Controls.Add(btnSaveConfig, 2, 0);
            configGrid.Controls.Add(lblStep, 0, 1);
            configGrid.Controls.Add(numStep, 1, 1);
            configGrid.Dock = DockStyle.Top;
            configGrid.Location = new Point(3, 3);
            configGrid.Name = "configGrid";
            configGrid.RowCount = 2;
            configGrid.RowStyles.Add(new RowStyle());
            configGrid.RowStyles.Add(new RowStyle());
            configGrid.Size = new Size(466, 68);
            configGrid.TabIndex = 0;
            // 
            // lblTargetUser
            // 
            lblTargetUser.Anchor = AnchorStyles.Left;
            lblTargetUser.AutoSize = true;
            lblTargetUser.Location = new Point(3, 10);
            lblTargetUser.Name = "lblTargetUser";
            lblTargetUser.Size = new Size(32, 17);
            lblTargetUser.TabIndex = 0;
            lblTargetUser.Text = "用户";
            // 
            // txtTargetUser
            // 
            txtTargetUser.Dock = DockStyle.Fill;
            txtTargetUser.Location = new Point(72, 4);
            txtTargetUser.Margin = new Padding(0, 4, 8, 4);
            txtTargetUser.Name = "txtTargetUser";
            txtTargetUser.PlaceholderText = "默认当前用户";
            txtTargetUser.Size = new Size(311, 23);
            txtTargetUser.TabIndex = 1;
            // 
            // btnSaveConfig
            // 
            btnSaveConfig.Anchor = AnchorStyles.Left;
            btnSaveConfig.AutoSize = true;
            btnSaveConfig.Location = new Point(391, 2);
            btnSaveConfig.Margin = new Padding(0, 2, 0, 2);
            btnSaveConfig.Name = "btnSaveConfig";
            btnSaveConfig.Padding = new Padding(10, 3, 10, 3);
            btnSaveConfig.Size = new Size(75, 33);
            btnSaveConfig.TabIndex = 2;
            btnSaveConfig.Text = "保存";
            btnSaveConfig.UseVisualStyleBackColor = true;
            btnSaveConfig.Click += btnSaveConfig_Click;
            // 
            // lblStep
            // 
            lblStep.Anchor = AnchorStyles.Left;
            lblStep.AutoSize = true;
            lblStep.Location = new Point(3, 44);
            lblStep.Name = "lblStep";
            lblStep.Size = new Size(32, 17);
            lblStep.TabIndex = 3;
            lblStep.Text = "周期";
            // 
            // numStep
            // 
            numStep.Location = new Point(72, 41);
            numStep.Margin = new Padding(0, 4, 0, 4);
            numStep.Maximum = new decimal(new int[] { 300, 0, 0, 0 });
            numStep.Minimum = new decimal(new int[] { 15, 0, 0, 0 });
            numStep.Name = "numStep";
            numStep.Size = new Size(72, 23);
            numStep.TabIndex = 4;
            numStep.Value = new decimal(new int[] { 30, 0, 0, 0 });
            // 
            // lblServiceStatus
            // 
            lblServiceStatus.AutoSize = true;
            lblServiceStatus.ForeColor = Color.DimGray;
            lblServiceStatus.Location = new Point(0, 86);
            lblServiceStatus.Margin = new Padding(0, 6, 0, 6);
            lblServiceStatus.MaximumSize = new Size(448, 0);
            lblServiceStatus.Name = "lblServiceStatus";
            lblServiceStatus.Size = new Size(56, 17);
            lblServiceStatus.TabIndex = 1;
            lblServiceStatus.Text = "尚未启用";
            // 
            // bootButtons
            // 
            bootButtons.AutoSize = true;
            bootButtons.Controls.Add(btnInstallCp);
            bootButtons.Controls.Add(btnUninstallCp);
            bootButtons.Controls.Add(btnDiagnoseCp);
            bootButtons.Controls.Add(btnStartService);
            bootButtons.Controls.Add(btnStopService);
            bootButtons.Controls.Add(btnUninstallService);
            bootButtons.Controls.Add(btnOpenConfigDir);
            bootButtons.Controls.Add(btnRefreshService);
            bootButtons.Dock = DockStyle.Fill;
            bootButtons.Location = new Point(0, 111);
            bootButtons.Margin = new Padding(0);
            bootButtons.Name = "bootButtons";
            bootButtons.Size = new Size(472, 120);
            bootButtons.TabIndex = 2;
            bootButtons.WrapContents = true;
            // 
            // btnInstallCp
            // 
            btnInstallCp.AutoSize = true;
            btnInstallCp.Location = new Point(0, 0);
            btnInstallCp.Margin = new Padding(0, 0, 6, 6);
            btnInstallCp.Name = "btnInstallCp";
            btnInstallCp.Padding = new Padding(10, 4, 10, 4);
            btnInstallCp.Size = new Size(90, 33);
            btnInstallCp.TabIndex = 0;
            btnInstallCp.Text = "启用登录";
            btnInstallCp.UseVisualStyleBackColor = true;
            btnInstallCp.Click += btnInstallCp_Click;
            // 
            // btnUninstallCp
            // 
            btnUninstallCp.AutoSize = true;
            btnUninstallCp.Location = new Point(98, 0);
            btnUninstallCp.Margin = new Padding(0, 0, 6, 6);
            btnUninstallCp.Name = "btnUninstallCp";
            btnUninstallCp.Padding = new Padding(8, 4, 8, 4);
            btnUninstallCp.Size = new Size(75, 33);
            btnUninstallCp.TabIndex = 1;
            btnUninstallCp.Text = "卸载";
            btnUninstallCp.UseVisualStyleBackColor = true;
            btnUninstallCp.Click += btnUninstallCp_Click;
            // 
            // btnDiagnoseCp
            // 
            btnDiagnoseCp.AutoSize = true;
            btnDiagnoseCp.Location = new Point(181, 0);
            btnDiagnoseCp.Margin = new Padding(0, 0, 12, 6);
            btnDiagnoseCp.Name = "btnDiagnoseCp";
            btnDiagnoseCp.Padding = new Padding(8, 4, 8, 4);
            btnDiagnoseCp.Size = new Size(75, 33);
            btnDiagnoseCp.TabIndex = 2;
            btnDiagnoseCp.Text = "诊断";
            btnDiagnoseCp.UseVisualStyleBackColor = true;
            btnDiagnoseCp.Click += btnDiagnoseCp_Click;
            // 
            // btnStartService
            // 
            btnStartService.AutoSize = true;
            btnStartService.Location = new Point(272, 0);
            btnStartService.Margin = new Padding(0, 0, 6, 6);
            btnStartService.Name = "btnStartService";
            btnStartService.Padding = new Padding(8, 4, 8, 4);
            btnStartService.Size = new Size(82, 33);
            btnStartService.TabIndex = 3;
            btnStartService.Text = "启动服务";
            btnStartService.UseVisualStyleBackColor = true;
            btnStartService.Click += btnStartService_Click;
            // 
            // btnStopService
            // 
            btnStopService.AutoSize = true;
            btnStopService.Location = new Point(362, 0);
            btnStopService.Margin = new Padding(0, 0, 6, 6);
            btnStopService.Name = "btnStopService";
            btnStopService.Padding = new Padding(8, 4, 8, 4);
            btnStopService.Size = new Size(82, 33);
            btnStopService.TabIndex = 4;
            btnStopService.Text = "停止服务";
            btnStopService.UseVisualStyleBackColor = true;
            btnStopService.Click += btnStopService_Click;
            // 
            // btnUninstallService
            // 
            btnUninstallService.AutoSize = true;
            btnUninstallService.Location = new Point(0, 39);
            btnUninstallService.Margin = new Padding(0, 0, 6, 6);
            btnUninstallService.Name = "btnUninstallService";
            btnUninstallService.Padding = new Padding(8, 4, 8, 4);
            btnUninstallService.Size = new Size(82, 33);
            btnUninstallService.TabIndex = 5;
            btnUninstallService.Text = "卸载服务";
            btnUninstallService.UseVisualStyleBackColor = true;
            btnUninstallService.Click += btnUninstallService_Click;
            // 
            // sessionPanel (unused host; buttons live in bootButtons)
            // 
            sessionPanel.AutoSize = true;
            sessionPanel.Location = new Point(0, 0);
            sessionPanel.Name = "sessionPanel";
            sessionPanel.Size = new Size(0, 0);
            sessionPanel.TabIndex = 3;
            sessionPanel.Visible = false;
            // 
            // btnOpenConfigDir
            // 
            btnOpenConfigDir.AutoSize = true;
            btnOpenConfigDir.Location = new Point(88, 39);
            btnOpenConfigDir.Margin = new Padding(0, 0, 6, 6);
            btnOpenConfigDir.Name = "btnOpenConfigDir";
            btnOpenConfigDir.Padding = new Padding(8, 4, 8, 4);
            btnOpenConfigDir.Size = new Size(82, 33);
            btnOpenConfigDir.TabIndex = 6;
            btnOpenConfigDir.Text = "配置目录";
            btnOpenConfigDir.UseVisualStyleBackColor = true;
            btnOpenConfigDir.Click += btnOpenConfigDir_Click;
            // 
            // btnRefreshService
            // 
            btnRefreshService.AutoSize = true;
            btnRefreshService.Location = new Point(176, 39);
            btnRefreshService.Margin = new Padding(0, 0, 0, 6);
            btnRefreshService.Name = "btnRefreshService";
            btnRefreshService.Padding = new Padding(8, 4, 8, 4);
            btnRefreshService.Size = new Size(60, 33);
            btnRefreshService.TabIndex = 7;
            btnRefreshService.Text = "刷新";
            btnRefreshService.UseVisualStyleBackColor = true;
            btnRefreshService.Click += btnRefreshService_Click;
            // 
            // tabLog
            // 
            tabLog.Controls.Add(txtLog);
            tabLog.Location = new Point(4, 32);
            tabLog.Name = "tabLog";
            tabLog.Padding = new Padding(8);
            tabLog.Size = new Size(492, 350);
            tabLog.TabIndex = 2;
            tabLog.Text = "日志";
            tabLog.UseVisualStyleBackColor = true;
            // 
            // txtLog
            // 
            txtLog.BackColor = Color.FromArgb(245, 246, 248);
            txtLog.BorderStyle = BorderStyle.None;
            txtLog.Dock = DockStyle.Fill;
            txtLog.Font = new Font("Consolas", 9F);
            txtLog.ForeColor = Color.FromArgb(40, 44, 52);
            txtLog.Location = new Point(8, 8);
            txtLog.Multiline = true;
            txtLog.Name = "txtLog";
            txtLog.ReadOnly = true;
            txtLog.ScrollBars = ScrollBars.Vertical;
            txtLog.Size = new Size(476, 334);
            txtLog.TabIndex = 0;
            txtLog.WordWrap = false;
            // 
            // errorProvider
            // 
            errorProvider.ContainerControl = this;
            // 
            // rootLayout
            // 
            rootLayout.Location = new Point(0, 0);
            rootLayout.Name = "rootLayout";
            rootLayout.Size = new Size(200, 100);
            rootLayout.TabIndex = 0;
            rootLayout.Visible = false;
            // 
            // contentHost
            // 
            contentHost.Location = new Point(0, 0);
            contentHost.Name = "contentHost";
            contentHost.Size = new Size(200, 100);
            contentHost.TabIndex = 0;
            contentHost.Visible = false;
            // 
            // actionStack
            // 
            actionStack.Location = new Point(0, 0);
            actionStack.Name = "actionStack";
            actionStack.Size = new Size(200, 100);
            actionStack.TabIndex = 0;
            actionStack.Visible = false;
            // 
            // grpBootService
            // 
            grpBootService.Location = new Point(0, 0);
            grpBootService.Name = "grpBootService";
            grpBootService.Size = new Size(200, 100);
            grpBootService.TabIndex = 0;
            grpBootService.TabStop = false;
            grpBootService.Visible = false;
            // 
            // bootStack
            // 
            bootStack.Location = new Point(0, 0);
            bootStack.Name = "bootStack";
            bootStack.Size = new Size(200, 100);
            bootStack.TabIndex = 0;
            bootStack.Visible = false;
            // 
            // grpConfig
            // 
            grpConfig.Location = new Point(0, 0);
            grpConfig.Name = "grpConfig";
            grpConfig.Size = new Size(200, 100);
            grpConfig.TabIndex = 0;
            grpConfig.TabStop = false;
            grpConfig.Visible = false;
            // 
            // grpLog
            // 
            grpLog.Location = new Point(0, 0);
            grpLog.Name = "grpLog";
            grpLog.Size = new Size(200, 100);
            grpLog.TabIndex = 0;
            grpLog.TabStop = false;
            grpLog.Visible = false;
            // 
            // btnInstallService
            // 
            btnInstallService.Location = new Point(0, 0);
            btnInstallService.Name = "btnInstallService";
            btnInstallService.Size = new Size(75, 23);
            btnInstallService.TabIndex = 0;
            btnInstallService.Visible = false;
            btnInstallService.Click += btnInstallService_Click;
            // 
            // lblConfigHint
            // 
            lblConfigHint.Location = new Point(0, 0);
            lblConfigHint.Name = "lblConfigHint";
            lblConfigHint.Size = new Size(100, 23);
            lblConfigHint.TabIndex = 0;
            lblConfigHint.Visible = false;
            // 
            // configStack
            // 
            configStack.Location = new Point(0, 0);
            configStack.Name = "configStack";
            configStack.Size = new Size(200, 100);
            configStack.TabIndex = 0;
            configStack.Visible = false;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.White;
            ClientSize = new Size(460, 318);
            Controls.Add(tabs);
            Font = new Font("Microsoft YaHei UI", 9F);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimumSize = new Size(440, 300);
            Name = "MainForm";
            Padding = new Padding(8);
            StartPosition = FormStartPosition.CenterScreen;
            Text = "WinTotp";
            Load += MainForm_Load;
            tabs.ResumeLayout(false);
            tabBind.ResumeLayout(false);
            tabLogin.ResumeLayout(false);
            loginLayout.ResumeLayout(false);
            loginLayout.PerformLayout();
            configGrid.ResumeLayout(false);
            configGrid.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numStep).EndInit();
            bootButtons.ResumeLayout(false);
            bootButtons.PerformLayout();
            sessionPanel.ResumeLayout(false);
            sessionPanel.PerformLayout();
            tabLog.ResumeLayout(false);
            tabLog.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)errorProvider).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private TabControl tabs;
        private TabPage tabBind;
        private Panel panelBindHost;
        private TabPage tabLogin;
        private TableLayoutPanel loginLayout;
        private TabPage tabLog;
        private TableLayoutPanel rootLayout;
        private Panel contentHost;
        private GroupBox grpConfig;
        private TableLayoutPanel configStack;
        private TableLayoutPanel configGrid;
        private Label lblStep;
        private NumericUpDown numStep;
        private Label lblTargetUser;
        private TextBox txtTargetUser;
        private Button btnSaveConfig;
        private Label lblConfigHint;
        private TableLayoutPanel actionStack;
        private GroupBox grpBootService;
        private TableLayoutPanel bootStack;
        private Label lblServiceStatus;
        private FlowLayoutPanel bootButtons;
        private Button btnInstallCp;
        private Button btnUninstallCp;
        private Button btnDiagnoseCp;
        private Button btnInstallService;
        private Button btnStartService;
        private Button btnStopService;
        private Button btnUninstallService;
        private FlowLayoutPanel sessionPanel;
        private Button btnOpenConfigDir;
        private Button btnRefreshService;
        private GroupBox grpLog;
        private TextBox txtLog;
        private ErrorProvider errorProvider;
    }
}
