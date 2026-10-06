using System.Net.Http;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Outernet.LBEToolkit.Authorization
{
    public interface IAuthorizationProvider
    {
        bool authorized { get; }
        HttpMessageHandler httpMessageHandler { get; }

        UniTask Authorize();
    }

    public abstract class AuthorizationProvider : MonoBehaviour, IAuthorizationProvider
    {
        public abstract bool authorized { get; }
        public abstract HttpMessageHandler httpMessageHandler { get; }

        public abstract UniTask Authorize();
    }
}