using System.Net.Http;
using Cysharp.Threading.Tasks;

namespace Outernet.LBEToolkit.Authorization
{
    public class AnonymousIdentityAuthorizationProvider : AuthorizationProvider
    {
        public override bool authorized => true;
        public override HttpMessageHandler httpMessageHandler => _httpMessageHandler;

        public string identity;

        private HttpMessageHandler _httpMessageHandler;

        private void Awake()
        {
            _httpMessageHandler = new AnonymousIdentityHttpHandler(identity);
        }

        public override UniTask<HttpMessageHandler> Authorize()
            => new UniTask<HttpMessageHandler>(_httpMessageHandler);
    }
}
