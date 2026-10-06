using Cysharp.Threading.Tasks;
using System.Net.Http;
using Placeframe.Auth;

namespace Outernet.LBEToolkit.Authorization
{
    public class TokenServerAuthorizationProvider : AuthorizationProvider
    {
        public override bool authorized => _authorized;
        public override HttpMessageHandler httpMessageHandler => _httpMessageHandler;

        public string tokenUrl;
        public string clientId;
        public string username;
        public string password;

        private bool _authorized;
        private HttpMessageHandler _httpMessageHandler;

        public async override UniTask Authorize()
        {
            if (_httpMessageHandler != null)
                return;

            var httpHandler = new TokenServerHttpHandler();
            await httpHandler.Login(tokenUrl, clientId, username, password);
            _httpMessageHandler = httpHandler;
            _authorized = true;
        }
    }
}