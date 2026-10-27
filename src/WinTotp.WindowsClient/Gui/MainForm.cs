using System.Diagnostics;
using WinTotp.WindowsClient.Options;
using WinTotp.WindowsClient.Services;

namespace WinTotp.WindowsClient.Gui
{
    public partial class MainForm : Form
    {
        private readonly AuthenticatorPanel _bindPanel;

        public MainForm()
        {
            InitializeComponent();
            _bindPanel = new AuthenticatorPanel { Dock = DockStyle.Top, AutoSize = true };
            panelBindHost.Controls.Add(_bindPanel);
            _bindPanel.SecretChanged += (_, _) =>
            {
                try
                {
                    SaveFromUi(silent: true);
                    AppendLog("已保存新的验证器密钥，请重新扫码绑定。");
                    TrySyncCpSecret();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message, "WinTotp", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            var opts = AppSettingsFile.Load();
            if (string.IsNullOrWhiteSpace(opts.TotpSecret))
            {
                opts.TotpSecret = AuthenticatorBind.EnsureSecret(null);
                AppSettingsFile.Save(opts);
                AppendLog("已自动生成本地 TOTP 密钥，请用手机验证器扫码绑定。");
            }

            LoadToUi(opts);
            AppendLog("配置文件：" + AppSettingsFile.GetPath());
            AppendLog("登录：验证器 6 位（登录选项 → TR）。不改 Windows 密码。");
            AppendLog(IsAdministrator()
                ? "当前以管理员运行。"
                : "警告：未以管理员运行，安装 CP / 服务可能失败。");
            RefreshServiceStatus();
        }

        private void btnSaveConfig_Click(object sender, EventArgs e)
        {
            try
            {
                SaveFromUi();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "WinTotp", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void btnInstallCp_Click(object sender, EventArgs e)
        {
            try
            {
                if (CredentialProviderInstaller.IsRegistered())
                {
                    MessageBox.Show(this, "登录组件已启用。若要重装，请先卸载。", "WinTotp",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                SaveFromUi(silent: true, restartServiceIfRunning: false);
                if (string.IsNullOrWhiteSpace(txtTargetUser.Text))
                    txtTargetUser.Text = Environment.UserName;
                if (!IsAdministrator())
                    throw new InvalidOperationException("请以管理员身份运行后再安装登录组件。");

                var user = txtTargetUser.Text.Trim();
                var warn = MessageBox.Show(this,
                    "启用后不会改 Windows 密码。\n" +
                    "请准备账户「" + user + "」的当前密码（无密码留空）。\n\n建议先做虚拟机快照。继续？",
                    "WinTotp", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (warn != DialogResult.Yes)
                    return;

                var currentPwd = PromptCurrentWindowsPassword(user);
                if (currentPwd is null)
                    return;

                var timeMsg = WindowsTimeSync.EnsureAutomaticSync();
                AppendLog("时间同步: " + timeMsg);

                var opts = ReadFromUi();
                var msg = CredentialProviderInstaller.Enable(
                    opts.TargetUser,
                    opts.TotpSecret,
                    opts.TotpStepSeconds,
                    currentPwd);
                AppendLog(msg);

                if (!WindowsServiceInstaller.IsInstalled())
                {
                    WindowsServiceInstaller.Install();
                    WindowsServiceInstaller.Start();
                    AppendLog("已安装开机服务（同步密钥）。");
                }
                else if (!WindowsServiceInstaller.IsRunning())
                {
                    WindowsServiceInstaller.Start();
                }
                else
                {
                    WindowsServiceInstaller.Stop();
                    WindowsServiceInstaller.Start();
                    AppendLog("已重启开机服务。");
                }

                MessageBox.Show(this,
                    "已启用。\n\n注销 → 登录选项 → TR → 输入验证器 6 位。\n" +
                    "（Hyper-V 请用基本会话）",
                    "WinTotp", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                AppendLog("安装登录组件失败: " + ex.Message);
                MessageBox.Show(this, ex.Message, "WinTotp", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                RefreshServiceStatus();
            }
        }

        private void btnDiagnoseCp_Click(object sender, EventArgs e)
        {
            try
            {
                var report = CredentialProviderInstaller.Diagnose();
                AppendLog(report);
                MessageBox.Show(this, report, "WinTotp 诊断", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                AppendLog("诊断失败: " + ex.Message);
                MessageBox.Show(this, ex.Message, "WinTotp", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static string? PromptCurrentWindowsPassword(string user)
        {
            using var f = new Form
            {
                Text = "当前 Windows 密码",
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                ClientSize = new Size(440, 150),
                MaximizeBox = false,
                MinimizeBox = false,
                ShowInTaskbar = false
            };
            var lbl = new Label
            {
                Text = "账户「" + user + "」的当前登录密码。\n本来没有密码请留空后点确定（切勿乱填）。",
                Left = 12,
                Top = 12,
                Width = 410,
                Height = 40
            };
            var tb = new TextBox
            {
                Left = 12,
                Top = 58,
                Width = 410,
                UseSystemPasswordChar = true
            };
            var ok = new Button { Text = "确定", DialogResult = DialogResult.OK, Left = 240, Top = 100, Width = 85 };
            var cancel = new Button { Text = "取消", DialogResult = DialogResult.Cancel, Left = 335, Top = 100, Width = 85 };
            f.Controls.AddRange(new Control[] { lbl, tb, ok, cancel });
            f.AcceptButton = ok;
            f.CancelButton = cancel;
            return f.ShowDialog() == DialogResult.OK ? tb.Text : null;
        }

        private void btnUninstallCp_Click(object sender, EventArgs e)
        {
            try
            {
                if (MessageBox.Show(this,
                        "确定卸载 Credential Provider？\n\n卸载后登录界面不再提供验证器入口；" +
                        "请用系统「密码」磁贴 + 你启用时填写的当前 Windows 密码登录（无密码则回车）。",
                        "WinTotp", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                    return;

                CredentialProviderInstaller.Uninstall();
                AppendLog("已卸载 Credential Provider 注册（DLL 可能需重启后才能删除）。");

                if (MessageBox.Show(this,
                        "是否同时删除 ProgramData 中的 CP 配置（含隐藏静态密码）？\n选「是」前请确认你还能用其他方式登录。",
                        "WinTotp", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    CpConfigStore.Delete();
                    AppendLog("已删除 " + CpConfigStore.GetPath());
                }
            }
            catch (Exception ex)
            {
                AppendLog("卸载登录组件失败: " + ex.Message);
                MessageBox.Show(this, ex.Message, "WinTotp", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                RefreshServiceStatus();
            }
        }

        private void btnInstallService_Click(object sender, EventArgs e)
        {
            try
            {
                SaveFromUi(silent: true, restartServiceIfRunning: false);
                if (string.IsNullOrWhiteSpace(txtTargetUser.Text))
                    txtTargetUser.Text = Environment.UserName;

                var timeMsg = WindowsTimeSync.EnsureAutomaticSync();
                AppendLog("时间同步: " + timeMsg);

                WindowsServiceInstaller.Install();
                WindowsServiceInstaller.Start();
                AppendLog("已安装并启动开机服务 WinTotpAgent（同步 CP 密钥）。");
                MessageBox.Show(this,
                    "开机服务已安装。\n\n请再点「安装/启用登录组件」启用 Credential Provider。\n登录时输入验证器 6 位。",
                    "WinTotp", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                AppendLog("安装开机服务失败: " + ex.Message);
                MessageBox.Show(this, ex.Message, "WinTotp", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                RefreshServiceStatus();
            }
        }

        private void btnUninstallService_Click(object sender, EventArgs e)
        {
            try
            {
                if (MessageBox.Show(this,
                        "确定卸载开机服务？\n（不影响已安装的 Credential Provider 登录组件）",
                        "WinTotp", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                    return;

                WindowsServiceInstaller.Uninstall();
                AppendLog("已卸载开机服务。");
            }
            catch (Exception ex)
            {
                AppendLog("卸载失败: " + ex.Message);
                MessageBox.Show(this, ex.Message, "WinTotp", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                RefreshServiceStatus();
            }
        }

        private void btnStartService_Click(object sender, EventArgs e)
        {
            try
            {
                SaveFromUi(silent: true, restartServiceIfRunning: false);
                var timeMsg = WindowsTimeSync.EnsureAutomaticSync();
                AppendLog("时间同步: " + timeMsg);
                WindowsServiceInstaller.Start();
                AppendLog("开机服务已启动。");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "WinTotp", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                RefreshServiceStatus();
            }
        }

        private void btnStopService_Click(object sender, EventArgs e)
        {
            try
            {
                WindowsServiceInstaller.Stop();
                AppendLog("开机服务已停止。");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "WinTotp", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                RefreshServiceStatus();
            }
        }

        private void btnOpenConfigDir_Click(object sender, EventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = AppContext.BaseDirectory,
                UseShellExecute = true
            });
        }

        private void btnRefreshService_Click(object sender, EventArgs e)
        {
            RefreshServiceStatus();
        }

        private void RefreshServiceStatus()
        {
            try
            {
                var cpOn = CredentialProviderInstaller.IsRegistered();
                var cpDll = CredentialProviderInstaller.IsDllPresent();
                var installed = WindowsServiceInstaller.IsInstalled();
                var running = WindowsServiceInstaller.IsRunning();

                // Already enabled → gray out primary install button.
                btnInstallCp.Enabled = !cpOn && cpDll;
                btnInstallCp.Text = cpOn ? "已启用" : "启用登录";
                btnUninstallCp.Enabled = cpOn;
                btnDiagnoseCp.Enabled = true;

                btnInstallService.Enabled = false;
                btnUninstallService.Enabled = installed;
                btnStartService.Enabled = installed && !running;
                btnStopService.Enabled = installed && running;
                btnStartService.Visible = installed;
                btnStopService.Visible = installed;
                btnUninstallService.Visible = installed;

                if (cpOn)
                {
                    lblServiceStatus.Text = running
                        ? "已启用 · 服务运行中 · 登录选项 → TR → 输入 6 位"
                        : "已启用 · 服务未运行（可点「启动服务」同步密钥）";
                    lblServiceStatus.ForeColor = Color.ForestGreen;
                }
                else if (!cpDll)
                {
                    lblServiceStatus.Text = "缺少登录组件 DLL（放到程序目录或 credential-provider\\）";
                    lblServiceStatus.ForeColor = Color.DarkRed;
                }
                else
                {
                    lblServiceStatus.Text = "未启用 · 扫码绑定后点「启用登录」";
                    lblServiceStatus.ForeColor = Color.DimGray;
                }
            }
            catch (Exception ex)
            {
                lblServiceStatus.Text = "状态读取失败：" + ex.Message;
                lblServiceStatus.ForeColor = Color.DarkRed;
            }
        }

        private void LoadToUi(WinTotpOptions o)
        {
            numStep.Value = Math.Clamp(o.TotpStepSeconds <= 0 ? 30 : o.TotpStepSeconds, 15, 300);
            txtTargetUser.Text = string.IsNullOrWhiteSpace(o.TargetUser)
                ? Environment.UserName
                : o.TargetUser.Trim();
            _bindPanel.Bind(
                o.TotpSecret,
                (int)numStep.Value,
                txtTargetUser.Text);
        }

        private WinTotpOptions ReadFromUi()
        {
            var user = txtTargetUser.Text.Trim();
            if (string.IsNullOrWhiteSpace(user))
                user = Environment.UserName;

            return new WinTotpOptions
            {
                TotpSecret = AuthenticatorBind.NormalizeSecret(_bindPanel.Secret),
                TotpStepSeconds = (int)numStep.Value,
                TargetUser = user
            };
        }

        private void SaveFromUi(bool silent = false, bool restartServiceIfRunning = true)
        {
            var opts = ReadFromUi();
            if (string.IsNullOrWhiteSpace(opts.TotpSecret))
                throw new InvalidOperationException("请先绑定验证器（本地 TOTP 密钥不能为空）。");

            AppSettingsFile.Save(opts);
            txtTargetUser.Text = opts.TargetUser;
            _bindPanel.SyncFromConfigSecret(opts.TotpSecret, opts.TotpStepSeconds);
            TrySyncCpSecret();
            AppendLog("已保存 appsettings.json · 用户 " + opts.TargetUser);

            var restarted = false;
            if (restartServiceIfRunning && WindowsServiceInstaller.IsInstalled() && WindowsServiceInstaller.IsRunning())
            {
                WindowsServiceInstaller.Stop();
                WindowsServiceInstaller.Start();
                restarted = true;
                AppendLog("已重启开机服务以加载最新配置。");
            }

            if (!silent)
            {
                MessageBox.Show(this,
                    restarted ? "配置已保存，并已重启开机服务。" : "配置已保存。",
                    "WinTotp", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            RefreshServiceStatus();
        }

        private void TrySyncCpSecret()
        {
            try
            {
                if (!CpConfigStore.Exists())
                    return;
                var opts = ReadFromUi();
                CredentialProviderInstaller.SyncSecretFromAppSettings(
                    opts.TotpSecret, opts.TotpStepSeconds, opts.TargetUser);
            }
            catch (Exception ex)
            {
                AppendLog("同步 CP 配置失败: " + ex.Message);
            }
        }

        private void AppendLog(string line)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => AppendLog(line)));
                return;
            }

            var stamp = DateTime.Now.ToString("HH:mm:ss");
            txtLog.AppendText("[" + stamp + "] " + line + Environment.NewLine);
            if (txtLog.TextLength > 120_000)
                txtLog.Text = txtLog.Text[^80_000..];
            txtLog.SelectionStart = txtLog.TextLength;
            txtLog.ScrollToCaret();
        }

        private static bool IsAdministrator()
        {
            try
            {
                using var id = System.Security.Principal.WindowsIdentity.GetCurrent();
                return new System.Security.Principal.WindowsPrincipal(id)
                    .IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }
    }
}
