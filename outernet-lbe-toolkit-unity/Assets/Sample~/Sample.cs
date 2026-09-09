using UnityEngine;
using ObserveThing;
using System;

using static Nessle.Props;
using System.Collections.Generic;
using System.Linq;

namespace Outernet.LBEToolkit.Sample
{
    public class Sample : RoomSceneManager<SampleState>
    {
        [SerializeField]
        private string _realtimeChannel = "Sample";

        [SerializeField]
        private SampleManager[] _managers;

        public override string realtimeChannel => _realtimeChannel;
        private IDisposable _subscription;

        protected override void Setup()
        {
            foreach (var manager in _managers)
                manager.Setup(state);

            _subscription = new ComposedDisposable(

                App.State.inRoomAndSynchronized.Subscribe(
                    inRoomAndSynchronized =>
                    {
                        if (inRoomAndSynchronized)
                            Transactions.Execute(new InitializeLocalPlayerAction(state, App.State.playerId.value, App.State.platform.value != Platform.AndroidMobile));
                    }
                ),

                App.State.players.Subscribe(
                    onRemove: player =>
                    {
                        var toRemove = state.objects.values.Where(x => x.ownerId.value == player.Key && x.destroyOnOwnerDisconnect.value).ToArray();
                        foreach (var obj in toRemove)
                            new DestroySceneObjectAction(state, obj.objectId);
                    }
                ),

                state.localHighFrequencyPrimitives
                    .ObservableSelect(x => x.Value.ObservableSelect(y => (path: x.Key, syncRate: y)))
                    .ObservableBatch()
                    .Subscribe(
                        onNext: batch =>
                        {
                            HashSet<string> removed = new HashSet<string>();
                            Dictionary<string, byte> added = new Dictionary<string, byte>();

                            foreach (var op in batch.operations)
                            {
                                if (op.isRemove)
                                {
                                    added.Remove(op.element.path);
                                    removed.Add(op.element.path);
                                }
                                else
                                {
                                    removed.Remove(op.element.path);
                                    added[op.element.path] = op.element.syncRate;
                                }
                            }

                            foreach (var toRemove in removed)
                                _synchronizationManager.RemoveHighFrequencyPrimitive(toRemove);

                            foreach (var toAdd in added)
                                _synchronizationManager.AddHighFrequencyPrimitive(toAdd.Key, toAdd.Value);
                        }
                    )
            );

            AppUI.AddSystemMenu(new()
            {
                menuItemLabel = $"Sample[{_realtimeChannel}] State",
                generateMenu = x => UIElements.StateLog(layout: x, props: Value(state))
            });
        }

        private void OnDestroy()
        {
            _subscription?.Dispose();

            AppUI.RemoveSystemMenu($"Sample[{_realtimeChannel}] State");
        }

        protected override void WriteInitialStateValues(SampleState state)
        {
            foreach (var manager in _managers)
                manager.WriteInitialStateValues(state);
        }
    }
}