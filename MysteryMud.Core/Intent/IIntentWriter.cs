namespace MysteryMud.Core.Intent;

public interface IIntentWriter<TIntent> where TIntent : struct
{
    ref TIntent Add();
}
