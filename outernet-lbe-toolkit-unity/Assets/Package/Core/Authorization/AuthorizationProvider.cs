using System.Net.Http;
using UnityEngine;

namespace Outernet.LBEToolkit.Authorization
{
    public abstract class AuthorizationProvider : MonoBehaviour
    {
        public abstract bool authorized { get; }
        public abstract HttpMessageHandler httpMessageHandler { get; }
    }
}