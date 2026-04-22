namespace MysteryMud.Domain.Services;

public interface ICastMessageService
{
    string CasterInstantMessage { get; }
    string RoomInstantMessage { get; }
    // to be used when start casting (cast time > 0)
    string CasterStartMessage { get; }
    string RoomStartMessage { get; }
    // to be used when casting is ongoing
    string CasterTickMessage { get; }
    string RoomTickMessage { get; }
    // to be used when casting is complete
    string CasterFinishMessage { get; }
    string RoomFinishMessage { get; }
    // to be used for interrupt
    string CasterInterruptMessage { get; }
    string RoomInterruptMessage { get; }
}
