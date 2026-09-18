using FofX.Stateful;
using UnityEngine;
using System;
using System.Collections.Generic;
using ObserveThing;
using Outernet.LBEToolkit.StateSynchronization;

namespace Outernet.LBEToolkit.Sample
{
    public class SampleState : StateObject
    {
        public static readonly int MAX_IDS_PER_PLAYER = 10000;

        [DoNotSync]
        public StateValue<int> playerId { get; private set; }

        public StateDictionary<int, PlayerData> players { get; private set; }
        public StateDictionary<int, SceneObjectState> objects { get; private set; }
        public StateDictionary<int, SceneTransformState> transforms { get; private set; }
        public StateDictionary<int, SceneAvatarState> avatars { get; private set; }
        public StateDictionary<int, StateDictionary<string, SceneToggleState>> toggles { get; private set; }
        public StateDictionary<int, StateDictionary<string, ScenePlayableState>> playables { get; private set; }

        [DoNotSync]
        public StateDictionary<string, StateValue<byte>> localHighFrequencyPrimitives { get; private set; }

        public IEnumerable<IStateDictionary> sceneObjectComponents
        {
            get
            {
                yield return objects;
                yield return transforms;
                yield return avatars;
                yield return toggles;
                yield return playables;
            }
        }

        public int GetNextObjectId()
        {
            var offset = ((SampleState)root).playerId.value * MAX_IDS_PER_PLAYER;
            for (int i = 0; i < MAX_IDS_PER_PLAYER; i++)
            {
                if (objects.ContainsKey(offset + i))
                    continue;

                return offset + i;
            }

            throw new Exception("All available IDs have been used");
        }
    }

    public class SceneToggleState : StateObject, IKeyedStateNode<string>
    {
        public string id { get; private set; }
        public StateValue<bool> isOn { get; private set; }

        void IKeyedStateNode<string>.AssignKey(string key)
            => id = key;
    }

    public class ScenePlayableState : StateObject, IKeyedStateNode<string>
    {
        public string id { get; private set; }
        public StateValue<bool> playing { get; private set; }
        public StateValue<float> startTime { get; private set; }
        public StateValue<float> offset { get; private set; }

        void IKeyedStateNode<string>.AssignKey(string key)
            => id = key;
    }

    public class PlayerData : StateObject, IKeyedStateNode<int>
    {
        public int playerId { get; private set; }
        public StateValue<int> avatarId { get; private set; }

        public void AssignKey(int key)
            => playerId = key;
    }

    public class SceneObjectComponentState : StateObject, IKeyedStateNode<int>
    {
        public int objectId { get; private set; }

        public void AssignKey(int key)
            => objectId = key;
    }

    public class SceneObjectState : SceneObjectComponentState
    {
        public StateValue<string> viewPrefab { get; private set; }
        public StateValue<int> ownerId { get; private set; }
        public StateValue<bool> allowOwnershipTransfer { get; private set; } = new StateValue<bool>(x => x.value = true);
        public StateValue<bool> destroyOnOwnerDisconnect { get; private set; }
        public StateValueView<bool> isMine { get; private set; }

        protected override void PostInitializeInternal()
        {
            isMine.InitializeView(mutator => Observables.ObservableCombineValues(
                ((SampleState)root).playerId,
                ownerId,
                (playerId, ownerId) => playerId == ownerId
            ).Subscribe(x => mutator.value = x));
        }
    }

    public class SceneTransformState : SceneObjectComponentState
    {
        public StateValue<Vector3> localPosition { get; private set; }
        public StateValue<Quaternion> localRotation { get; private set; } = new StateValue<Quaternion>(x => x.value = Quaternion.identity);
        public StateValue<Vector3> localScale { get; private set; } = new StateValue<Vector3>(x => x.value = Vector3.one);
    }

    public class SceneAvatarState : SceneObjectComponentState
    {
        public StateValue<Quaternion> headLocalRotation { get; private set; } = new StateValue<Quaternion>(x => x.value = Quaternion.identity);

        public StateValue<bool> leftHandTracked { get; private set; }
        public StateValue<Vector3> leftHandLocalPosition { get; private set; }
        public StateValue<Quaternion> leftHandLocalRotation { get; private set; } = new StateValue<Quaternion>(x => x.value = Quaternion.identity);

        public StateValue<bool> rightHandTracked { get; private set; }
        public StateValue<Vector3> rightHandLocalPosition { get; private set; }
        public StateValue<Quaternion> rightHandLocalRotation { get; private set; } = new StateValue<Quaternion>(x => x.value = Quaternion.identity);
    }
}