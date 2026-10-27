using WinTotp.Shared;
using WinTotp.WindowsClient.Services;

namespace WinTotp.WindowsClient.Gui
{
    public partial class AuthenticatorPanel : UserControl
    {
        private readonly System.Windows.Forms.Timer _timer = new() { Interval = 500 };
        private string _secret = "";
        private int _stepSeconds = 30;
        private string _account = Environment.UserName;
        private Image? _qrImage;

        public event EventHandler? SecretChanged;

        public AuthenticatorPanel()
        {
            InitializeComponent();
            _timer.Tick += (_, _) => RefreshLocalCode();
            _timer.Start();
            Disposed += (_, _) =>
            {
                _timer.Stop();
                _timer.Dispose();
                _qrImage?.Dispose();
            };
        }

        public string Secret => _secret;

        public void Bind(string? secret, int stepSeconds, string? accountLabel)
        {
            _stepSeconds = stepSeconds <= 0 ? 30 : stepSeconds;
            _account = string.IsNullOrWhiteSpace(accountLabel) ? Environment.UserName : accountLabel.Trim();
            _secret = AuthenticatorBind.EnsureSecret(secret);
            txtBindSecret.Text = _secret;
            UpdateFormatHint();
            RebuildQr();
            RefreshLocalCode();
        }

        public void SyncFromConfigSecret(string? secret, int stepSeconds)
        {
            _stepSeconds = stepSeconds <= 0 ? 30 : stepSeconds;
            UpdateFormatHint();

            var next = AuthenticatorBind.NormalizeSecret(secret);
            if (string.IsNullOrWhiteSpace(next))
            {
                RefreshLocalCode();
                return;
            }
            if (string.Equals(next, _secret, StringComparison.Ordinal))
            {
                RefreshLocalCode();
                return;
            }

            _secret = next;
            txtBindSecret.Text = _secret;
            RebuildQr();
            RefreshLocalCode();
        }

        private void UpdateFormatHint()
        {
            lblBindHint.Text = "扫码绑定。登录：登录选项 → TR → 6 位码。";
        }

        private void RebuildQr()
        {
            try
            {
                var url = AuthenticatorBind.BuildOtpAuthUrl(_secret, _account);
                var bmp = AuthenticatorBind.CreateQrBitmap(url);
                var old = _qrImage;
                _qrImage = bmp;
                picQr.Image = bmp;
                old?.Dispose();
            }
            catch (Exception ex)
            {
                picQr.Image = null;
                lblRemain.Text = "二维码生成失败: " + ex.Message;
            }
        }

        private void RefreshLocalCode()
        {
            if (string.IsNullOrWhiteSpace(_secret))
            {
                lblLocalPassword.Text = "(未配置密钥)";
                lblRemain.Text = "";
                return;
            }

            try
            {
                // Empty affixes: preview is the raw 6-digit code used at logon.
                var totp = new TotpService(_secret, _stepSeconds, passwordPrefix: "", passwordSuffix: "");
                var snap = totp.CreateAuthoritySnapshot();
                lblLocalPassword.Text = snap.Code;
                lblRemain.Text = $"剩余 {snap.SecondsRemaining} 秒";
            }
            catch (Exception ex)
            {
                lblLocalPassword.Text = "(密钥无效)";
                lblRemain.Text = ex.Message;
            }
        }

        private void btnCopySecret_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_secret)) return;
            Clipboard.SetText(_secret);
        }

        private void btnCopyPassword_Click(object sender, EventArgs e)
        {
            var pwd = lblLocalPassword.Text;
            if (string.IsNullOrWhiteSpace(pwd) || pwd.StartsWith('(')) return;
            Clipboard.SetText(pwd);
        }

        private void btnRefreshQr_Click(object sender, EventArgs e)
            => RebuildQr();

        private void btnRegenSecret_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(this,
                    "重新生成密钥后，旧验证器绑定会失效，必须重新扫码。\n\n确定继续？",
                    "WinTotp", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            _secret = TotpService.GenerateBase32Secret();
            txtBindSecret.Text = _secret;
            RebuildQr();
            RefreshLocalCode();
            SecretChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
