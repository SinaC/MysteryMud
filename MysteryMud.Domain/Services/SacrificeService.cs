using Arch.Core;
using MysteryMud.Domain.Extensions;
using MysteryMud.Domain.Helpers;
using System;
using System.Collections.Generic;
using System.Text;

namespace MysteryMud.Domain.Services;

public class SacrificeService : ISacrificeService
{
    private readonly IGameMessageService _msg;

    public SacrificeService(IGameMessageService msg)
    {
        _msg = msg;
    }

    public void Sacrifice(Entity actor, Entity item)
    {
        // TODO
        //var gold = CalculateReward(item);
        //GiveGold(actor, gold);
        //_msg.To(actor).Send($"You sacrifice {item.DisplayName} and receive {gold} gold.");
        //_msg.ToRoom(actor).Send($"{actor.DisplayName} sacrifices {item.DisplayName}.");

        _msg.To(actor).Send($"You sacrifice {item.DisplayName}.");
        _msg.ToRoom(actor).Send($"{actor.DisplayName} sacrifices {item.DisplayName}.");

        ItemHelpers.DestroyItem(item);
    }

    //private static int CalculateReward(Entity item)
    //{
    //    // reward logic here
    //}
}
