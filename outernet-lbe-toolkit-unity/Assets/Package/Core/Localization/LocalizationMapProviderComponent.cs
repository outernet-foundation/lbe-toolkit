using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Outernet.LBEToolkit.Localization
{
    public interface ILocalizationMapProvider
    {
        event Action<Guid> onMapAdded;
        event Action<Guid> onMapRemoved;

        IEnumerable<Guid> maps { get; }
    }

    public abstract class LocalizationMapProviderComponent : MonoBehaviour, ILocalizationMapProvider
    {
        public IEnumerable<Guid> maps => _maps;

        public event Action<Guid> onMapAdded;
        public event Action<Guid> onMapRemoved;

        private HashSet<Guid> _maps = new HashSet<Guid>();

        protected void AddMap(Guid map)
        {
            _maps.Add(map);
            onMapAdded?.Invoke(map);
        }

        protected void RemoveMap(Guid map)
        {
            _maps.Remove(map);
            onMapRemoved?.Invoke(map);
        }

        protected void SetMaps(params Guid[] maps)
        {
            foreach (var toRemove in _maps.Except(maps).ToArray())
                RemoveMap(toRemove);

            foreach (var toAdd in maps.Except(_maps).ToArray())
                AddMap(toAdd);
        }
    }
}