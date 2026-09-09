using UnityEngine;

namespace Outernet.LBEToolkit.Sample
{
    public class SetObjectOwnerAction : StateTransaction
    {
        private SampleState _state;
        private int _objectId;
        private int _ownerId;

        public SetObjectOwnerAction(SampleState state, int objectId, int ownerId)
        {
            _state = state;
            _objectId = objectId;
            _ownerId = ownerId;
        }

        public override void Execute()
        {
            var objState = _state.objects[_objectId];

            if (!objState.allowOwnershipTransfer.value)
                throw new System.Exception($"Ownership transfer is disallowed on object {_objectId}.");

            objState.ownerId.value = _ownerId;
        }
    }

    public class AddHighFrequencyPrimitiveAction : StateTransaction
    {
        private SampleState _state;
        private string _primitivePath;
        private byte _syncRate;

        public AddHighFrequencyPrimitiveAction(SampleState state, string primitivePath, byte syncRate)
        {
            _state = state;
            _primitivePath = primitivePath;
            _syncRate = syncRate;
        }

        public override void Execute()
        {
            _state.localHighFrequencyPrimitives.Add(_primitivePath).value = _syncRate;
        }
    }

    public class RemoveHighFrequencyPrimitiveAction : StateTransaction
    {
        private SampleState _state;
        private string _primitivePath;

        public RemoveHighFrequencyPrimitiveAction(SampleState state, string primitivePath)
        {
            _state = state;
            _primitivePath = primitivePath;
        }

        public override void Execute()
        {
            _state.localHighFrequencyPrimitives.Remove(_primitivePath);
        }
    }

    public class InitializeLocalPlayerAction : StateTransaction
    {
        private SampleState _state;
        private int _playerId;
        private bool _generateAvatar;

        public InitializeLocalPlayerAction(SampleState state, int playerId, bool generateAvatar)
        {
            _state = state;
            _playerId = playerId;
            _generateAvatar = generateAvatar;
        }

        public override void Execute()
        {
            var player = _state.players.Add(_playerId);

            if (_generateAvatar)
            {
                var id = _state.objectIdHelper.AllocateID();
                var obj = _state.objects.Add(id);
                var transform = _state.transforms.Add(id);
                var avatar = _state.avatars.Add(id);

                player.avatarId.value = id;
                obj.ownerId.value = App.State.playerId.value;
                obj.viewPrefab.value = "AvatarSceneView";

                new AddHighFrequencyPrimitiveAction(_state, transform.localPosition.nodePath, 16).Execute();
                new AddHighFrequencyPrimitiveAction(_state, transform.localRotation.nodePath, 16).Execute();
                new AddHighFrequencyPrimitiveAction(_state, avatar.headLocalRotation.nodePath, 16).Execute();
                new AddHighFrequencyPrimitiveAction(_state, avatar.leftHandLocalPosition.nodePath, 16).Execute();
                new AddHighFrequencyPrimitiveAction(_state, avatar.leftHandLocalRotation.nodePath, 16).Execute();
                new AddHighFrequencyPrimitiveAction(_state, avatar.rightHandLocalPosition.nodePath, 16).Execute();
                new AddHighFrequencyPrimitiveAction(_state, avatar.rightHandLocalRotation.nodePath, 16).Execute();
            }
        }
    }

    public class DestroySceneObjectAction : StateTransaction
    {
        private SampleState _state;
        private int _objectId;

        public DestroySceneObjectAction(SampleState state, int objectId)
        {
            _state = state;
            _objectId = objectId;
        }

        public override void Execute()
        {
            foreach (var component in _state.sceneObjectComponents)
                component.Remove(_objectId);
        }
    }
}
