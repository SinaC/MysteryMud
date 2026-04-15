using MysteryMud.Core;
using MysteryMud.Core.Bus;
using MysteryMud.Core.Contracts;
using MysteryMud.Core.Services;
using MysteryMud.GameData.Enums;
using MysteryMud.GameData.Events;

namespace MysteryMud.Domain.Systems;

public class LookSystem
{
    private readonly ILookService _lookService;
    private readonly IIntentContainer _intents;
    private readonly IEventBuffer<LookedEvent> _lookedEvents;

    public LookSystem(ILookService lookService, IIntentContainer intents, IEventBuffer<LookedEvent> lookedEvents)
    {
        _lookService = lookService;
        _intents = intents;
        _lookedEvents = lookedEvents;
    }

    public void Tick(GameState state, LookMode lookMode)
    {
        foreach (ref var intent in _intents.LookSpan)
        {
            if (intent.Mode != lookMode)
                continue;
            switch (intent.TargetKind)
            {
                case LookTargetKind.Room:
                    _lookService.DescribeRoom(intent.Viewer, intent.Target);
                    break;
                case LookTargetKind.Character:
                    _lookService.DescribeCharacter(intent.Viewer, intent.Target);
                    break;
                case LookTargetKind.Item:
                    _lookService.DescribeItem(intent.Viewer, intent.Target);
                    break;
            }

            // add looked event
            ref var lookedEvt = ref _lookedEvents.Add();
            lookedEvt.Viewer = intent.Viewer;
            lookedEvt.Entity = intent.Target;
            lookedEvt.TargetKind = intent.TargetKind;
        }
    }
}
