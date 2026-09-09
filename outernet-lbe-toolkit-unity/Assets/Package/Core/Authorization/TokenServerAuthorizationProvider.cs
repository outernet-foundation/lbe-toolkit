using Cysharp.Threading.Tasks;
using System.Net.Http;

namespace Outernet.LBEToolkit.Authorization
{
    public class TokenServerAuthorizationProvider : AuthorizationProvider
    {
        public override bool authorized => _authorized;
        public override HttpMessageHandler httpMessageHandler => _httpMessageHandler;

        public bool loginAutomatically;

        [ToggleGroup(nameof(loginAutomatically))]
        public string tokenUrl;

        [ToggleGroup(nameof(loginAutomatically))]
        public string clientId;

        [ToggleGroup(nameof(loginAutomatically))]
        public string username;

        [ToggleGroup(nameof(loginAutomatically))]
        public string password;

        private bool _authorized;
        private HttpMessageHandler _httpMessageHandler;

        private void Awake()
        {
            if (loginAutomatically)
                Login(tokenUrl, clientId, username, password).Forget();
        }

        public async UniTask Login(string tokenUrl, string clientId, string username, string password)
        {
            var httpHandler = new TokenServerHttpHandler();
            await httpHandler.Login(tokenUrl, clientId, username, password);
            _httpMessageHandler = httpHandler;
            _authorized = true;
        }
    }
}