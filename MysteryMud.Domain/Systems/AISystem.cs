using MysteryMud.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace MysteryMud.Domain.Systems;

public class AISystem
{
//    public void Execute(GameState state)
//    {
//        long now = state.CurrentTimeMs;

//        foreach (var entity in state.World.Query<CommandBuffer, AIComponent>())
//        {
//            ref var ai = ref entity.Get<AIComponent>();

//            // Skip NPCs that aren’t due for an AI tick
//            if (now - ai.LastAITick < ai.TickRate)
//                continue;

//            // Generate commands for this NPC
//            GenerateCommands(entity, ref ai, now);

//            // Update last tick
//            ai.LastAITick = now;
//        }
//    }
}
