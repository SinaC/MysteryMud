using DefaultEcs;
using MysteryMud.Core.Bus;
using MysteryMud.Core.Persistence;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Players;
using MysteryMud.Domain.Components.Characters.Resources;
using MysteryMud.GameData.Events;

namespace MysteryMud.Domain.Services;

public class ExperienceService : IExperienceService
{
    private readonly IGameMessageService _msg;
    private readonly IDirtyTracker _dirtyTracker;
    private readonly IEventBuffer<ExperienceGrantedEvent> _experiences;
    private readonly IEventBuffer<LevelIncreasedEvent> _levels;

    public ExperienceService(IGameMessageService msg, IDirtyTracker dirtyTracker, IEventBuffer<ExperienceGrantedEvent> experiences, IEventBuffer<LevelIncreasedEvent> levels)
    {
        _msg = msg;
        _dirtyTracker = dirtyTracker;
        _experiences = experiences;
        _levels = levels;
    }

    public long CalculateCombatXp(Entity player, Entity victim)
    {
        if (!player.IsAlive)
            return 0;

        if (!player.Has<Level>() || !victim.Has<Level>())
            return 0;
        ref var playerLevel = ref player.Get<Level>();
        ref var victimLevel = ref victim.Get<Level>();

        // base XP formula
        long baseXp = 50 + victimLevel.Value * 10;

        // level difference modifier
        int levelDiff = victimLevel.Value - playerLevel.Value;
        decimal modifier = 1.0m;

        if (levelDiff >= 5) modifier = 2.0m;        // harder targets give more XP
        else if (levelDiff <= -5) modifier = 0.5m;  // weaker targets give less XP

        // TODO: add buffs, XP bonuses, event modifiers, etc.

        return (long)(baseXp * modifier);
    }

    public void GrantExperience(Entity player, long xpGained)
    {
        if (!player.IsAlive)
            return;

        if (xpGained == 0)
            return;

        if (!player.Has<Progression>() || !player.Has<Level>())
            return;
        ref var progression = ref player.Get<Progression>();
        ref var level = ref player.Get<Level>();

        var currentLevelExperienceFloor = progression.ExperienceByLevel * (level.Value - 1);
        progression.Experience = Math.Max(currentLevelExperienceFloor, progression.Experience + xpGained); // don't go below current level

        if (xpGained > 0)
            _msg.To(player).Act("%gYou gain {0} XP.%x").With(xpGained);
        else
            _msg.To(player).Act("%gYou lose {0} XP.%x").With(-xpGained);

        if (player.Has<PlayerTag>()) // to be sure
            _dirtyTracker.MarkDirty(player, DirtyReason.Experience);

        var levelIncreased = false;
        if (progression.ExperienceToNextLevel > 0) // ExperienceToNextLevel = 0 means max level reached
        {
            while (progression.Experience >= progression.ExperienceToNextLevel)
            {
                level.Value++;
                progression.ExperienceToNextLevel = progression.ExperienceByLevel * level.Value;

                // TODO: calculate stats/resources increase
                //_dirtyTracker.MarkDirty(player, DirtyReason.Stats);
                //_dirtyTracker.MarkDirty(player, DirtyReason.Resources);

                levelIncreased = true;

                _msg.To(player).Act("%gYou leveled up to {0}! Your health and mana are fully restored.%x").With(level.Value);
            }
        }

        if (levelIncreased)
        {
            // Full restore
            SetResourceToMax<Health>(player, x => x.Max, (ref x, value) => x.Current = value);
            SetResourceToMax<Move>(player, x => x.Max, (ref x, value) => x.Current = value);
            SetResourceToMax<Mana>(player, x => x.Max, (ref x, value) => x.Current = value);
            SetResourceToMax<Energy>(player, x => x.Max, (ref x, value) => x.Current = value);
            SetResourceToMax<Rage>(player, x => x.Max, (ref x, value) => x.Current = value);

            // Recalculate derived stats
            if (!player.Has<DirtyStats>())
                player.Set<DirtyStats>();

            // TODO: set dirty health, dirty mana, ... ?

            if (player.Has<PlayerTag>())
                _dirtyTracker.MarkDirty(player, DirtyReason.CoreData);
        }

        // add experience awarded event
        ref var experienceAwardedEvt = ref _experiences.Add();
        experienceAwardedEvt.Target = player;
        experienceAwardedEvt.Gain = xpGained;

        // add level increase level event (if relevant)
        if (levelIncreased)
        {
            ref var levelIncreasedEvt = ref _levels.Add();
            levelIncreasedEvt.Target = player;
            levelIncreasedEvt.Level = level.Value;
        }
    }

    private delegate void ResourceValueSetter<TResource>(ref TResource resource, int value);

    private void SetResourceToMax<TResource>(Entity player, Func<TResource, int> getMaxValueFunc, ResourceValueSetter<TResource> setResourceValueAction)
        where TResource : struct
    {
        if (!player.Has<TResource>())
            return;
        ref var resource = ref player.Get<TResource>();

        var maxResource = getMaxValueFunc(resource);
        setResourceValueAction(ref resource, maxResource);
    }
}
