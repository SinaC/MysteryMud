namespace MysteryMud.Domain.Helpers;

public static class CastMessageHelpers // TODO: move to json ?
{
    // to be used for instant cast
    public static string CasterInstantMessage => CasterInstantMessages[Random.Shared.Next(0, CasterInstantMessages.Length)];
    public static string RoomInstantMessage => RoomInstantMessages[Random.Shared.Next(0, RoomInstantMessages.Length)];
    // to be used when start casting (cast time > 0)
    public static string CasterStartMessage => CasterStartMessages[Random.Shared.Next(0, CasterStartMessages.Length)];
    public static string RoomStartMessage => RoomStartMessages[Random.Shared.Next(0, RoomStartMessages.Length)];
    // to be used when casting is ongoing
    public static string CasterTickMessage => CasterTickMessages[Random.Shared.Next(0, CasterTickMessages.Length)];
    public static string RoomTickMessage => RoomTickMessages[Random.Shared.Next(0, RoomTickMessages.Length)];
    // to be used when casting is complete
    public static string CasterFinishMessage => CasterFinishMessages[Random.Shared.Next(0, CasterFinishMessages.Length)];
    public static string RoomFinishMessage => RoomFinishMessages[Random.Shared.Next(0, RoomFinishMessages.Length)];
    // to be used for interrupt
    public static string CasterInterruptMessage => CasterInterruptMessages[Random.Shared.Next(0, CasterInterruptMessages.Length)];
    public static string RoomInterruptMessage => RoomInterruptMessages[Random.Shared.Next(0, RoomInterruptMessages.Length)];

    // to be used for instant cast
    public static readonly string[] CasterInstantMessages =
    [
        // clean/default
        "You cast '{0}'.",
        //"You instantly cast '{0}'.",
        "You invoke '{0}'.",
        // slightly more impactful
        "You unleash '{0}'.",
        //"You release '{0}' instantly.",
        "You call forth '{0}'.",
        // more magical flavor
        "You channel energy and release '{0}'.",
        "You snap your focus into '{0}'.",
        "You weave and release '{0}' in a heartbeat.",
    ];
    public static readonly string[] RoomInstantMessages =
    [
        // clean/default
        //"{0} casts '{1}'.",
        //"{0} instantly casts a spell.",
        "{0} invokes a spell.",
        // slightly more impactful
        //"{0} unleashes '{1}'.",
        //"{0} releases a spell in an instant.",
        "{0} calls forth magical energy.",
        // more magical flavor
        "{0} gestures sharply and releases a spell.",
        //"{0}’s hands flash as {0:e} casts '{1}'.",
        "{0} channels energy and unleashes it immediately.",
    ];

    // to be used when start casting (cast time > 0)
    public static readonly string[] CasterStartMessages =
    [
        // clean/default
        "You begin concentrating to cast '{0}'.",
        "You begin to focus on casting '{0}'.",
        "You start preparing the spell '{0}'.",
        // slightly more immersive
        "You draw in a breath and begin focusing on '{0}'.",
        "You steady your mind and begin casting '{0}'.",
        "You gather your thoughts to invoke '{0}'.",
        // more magical flavor
        "You begin channeling energy for '{0}'.",
        "You start weaving the energies of '{0}'.",
        "You reach out to the arcane forces to cast'{0}'.",
    ];
    public static readonly string[] RoomStartMessages =
    [
        // neutral/classic
        "{0} begins concentrating on a spell.",
        "{0} starts focusing intently.",
        "{0} begins preparing a spell.",
        "{0} gathers {0:s} thoughts to cast something.",
        // slightly flavorful
        "{0}’s eyes narrow as {0:e} begins to concentrate.",
        "{0} mutters softly, beginning a spell.",
        "{0} draws in a deep breath and focuses.",
        "{0} seems to be gathering magical energy.",
        // more immersive/magical
        "{0} begins weaving magical energies.",
        "{0}’s presence sharpens as {0:e} channels power.",
        "{0} starts shaping unseen forces.",
        "{0} draws upon arcane energies.",
    ];

    // to be used when casting is ongoing
    public static readonly string[] CasterTickMessages =
    [
        // minimal
        "You continue concentrating.",
        "You remain focused.",
        "You hold your concentration.",
        // slight variation
        "You maintain your focus.",
        "You stay locked on the spell.",
        "Your concentration holds steady.",
        // more immersive
        "You continue shaping the spell’s energy.",
        "You keep channeling magical power.",
        "Your mind remains fixed on the spell."
    ];
    public static readonly string[] RoomTickMessages =
    [
        // neutral/classic
        "{0} continues concentrating.",
        "{0} remains focused.",
        "{0} holds {0:s} concentration.",
        // slightly flavorful
        "{0} maintains {0:s} focus.",
        "{0} continues {0:s} incantation.",
        "{0} stays locked in concentration.",
        // more immersive/magical
        "{0}’s concentration does not waver.",
        "{0} continues channeling energy.",
        "{0} remains deep in focus, ignoring distractions.",
        "{0} sustains the flow of magic.",
    ];

    // to be used when casting is complete
    public static readonly string[] CasterFinishMessages =
    [
        // clean
        "You complete your concentration and cast '{0}'.",
        "You finish casting '{0}'.",
        // more satisfying
        "Your concentration peaks—you cast '{0}'.",
        "You release your focus and unleash '{0}'.",
        "You complete the spell and cast '{0}'.",
        // more magical
        "You release the gathered energy as '{0}'.",
        "The spell forms—you cast '{0}'.",
    ];

    public static readonly string[] RoomFinishMessages =
    [
        // classic
        "{0} completes {0:s} spell.",
        "{0} finishes casting and releases a spell.",
        // with spell name (if visible)
        //"{0} completes {0:s} spell and casts '{1}'.",
        //"{0} releases '{1}' with a final gesture.",
        // more impactful
        "{0}’s concentration breaks as {0:e} unleashes a spell!",
        "{0} completes {0:s} incantation and releases its power!",
        "{0} finishes weaving magic and releases it."
    ];

    // to be used for interrupt
    public static readonly string[] CasterInterruptMessages =
    [
        // clear
        "Your concentration is broken! You fail to cast '{0}'.",
        "You lose your focus and the spell fizzles.",
        // more immersive
        "Your thoughts scatter—you lose control of the spell!",
        "Your concentration slips, and the magic fades.",
        "The spell collapses before completion!",
    ];

    public static readonly string[] RoomInterruptMessages =
    [
        // basic
        "{0}’s concentration is broken!.",
        "{0} loses {0:s} concentration and stops casting.",
        // more expressive
        "{0} falters, losing control of{0:s} spell!",
        "{0}’s spell fizzles as {0:e} is interrupted!",
        "{0}’s concentration shatters!",
    ];

    /* Interrupt messages
Failure messages
The spell slips from your grasp.
You fail to shape the spell correctly.
The magic dissipates before taking form.
*/
}
