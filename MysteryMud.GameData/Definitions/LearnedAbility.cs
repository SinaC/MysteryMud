using System;
using System.Collections.Generic;
using System.Text;

namespace MysteryMud.GameData.Definitions;

public struct LearnedAbility
{
    public int AbilityId;
    public int ClassId;
    public int LearnedPercent;
    public int LearnedLevel;
    public int MasterTier;
}
