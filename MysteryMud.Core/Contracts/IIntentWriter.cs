namespace MysteryMud.Core.Contracts;

public interface IIntentWriter<TIntent> where TIntent : struct
{
    ref TIntent Add();
}
