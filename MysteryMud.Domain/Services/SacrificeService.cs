using MysteryMud.Domain.Helpers;
using TinyECS;

namespace MysteryMud.Domain.Services;

public class SacrificeService : ISacrificeService
{
    private readonly World _world;
    private readonly IGameMessageService _msg;

    public SacrificeService(World world, IGameMessageService msg)
    {
        _world = world;
        _msg = msg;
    }

    public void Sacrifice(EntityId actor, EntityId item)
    {
        // TODO
        //var gold = CalculateReward(item);
        //GiveGold(actor, gold);
        //_msg.To(actor).Send($"You sacrifice {item.DisplayName} and receive {gold} gold.");
        //_msg.ToRoom(actor).Send($"{actor.DisplayName} sacrifices {item.DisplayName}.");

        _msg.To(actor).Act("You sacrifice {0}.").With(item);
        _msg.ToRoom(actor).Act("{0} sacrifices {1}.").With(actor, item);

        ItemHelpers.DestroyItem(_world, item);
    }

    //private static int CalculateReward(Entity item)
    //{
    //    // reward logic here
    //}
}
