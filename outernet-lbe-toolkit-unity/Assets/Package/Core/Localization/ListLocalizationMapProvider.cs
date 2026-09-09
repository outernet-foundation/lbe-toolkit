using System;
using UnityEngine;

namespace Outernet.LBEToolkit.Localization
{
    public class ListLocalizationMapProvider : LocalizationMapProviderComponent
    {
        [SerializeField]
        private string[] _maps;

        private void Awake()
        {
            foreach (var map in _maps)
                AddMap(new Guid(map));
        }
    }
}
