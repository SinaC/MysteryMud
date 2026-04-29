using DefaultEcs;
using MysteryMud.Domain.Helpers;

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

        //_msg.To(actor).Act("You sacrifice {0}.").With(item);
        //_msg.ToRoom(actor).Act("{0} sacrifices {1}.").With(actor, item);
        _msg.ToAll(actor).Act("{0} sacrifice{0:v} {1}.").With(actor, item);

        ItemHelpers.DestroyItem(item);
    }

    //private static int CalculateReward(Entity item)
    //{
    //    // reward logic here
    //}
}
