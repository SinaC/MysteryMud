using TinyECS;
using TinyECS.DemoApp;

// =============================================================================
// BOOTSTRAP
// =============================================================================

Random _rng = new();

var world = new World();

// Create a player
EntityId player = world.CreateEntity();
world.Set<PlayerControlled>(player);
world.Set(player, new Health { Current = 100, Max = 100 });
world.Set(player, new Mana { Current = 80, Max = 80 });
world.Set(player, new Level { Value = 10 });
world.Set(player, new Position { RoomVnum = 3001 });

// Create a mob
EntityId mob = world.CreateEntity();
world.Set<Mobile>(mob);
world.Set(mob, new Health { Current = 3, Max = 40 });
world.Set(mob, new Level { Value = 1 });
world.Set(mob, new Position { RoomVnum = 3001 });

// =============================================================================
// COMBAT SYSTEM  (called every pulse_combat tick)
// =============================================================================

static void CombatSystem(World world, Random rng)
{
    // Iterate every EntityId that is fighting AND alive (no Dead tag).
    var query = new Query(world)
        .With<Fighting>()
        .Without<Stunned>()
        .Without<Dead>();

    foreach (EntityId attacker in query)
    {
        ref Fighting rel = ref world.Get<Fighting>(attacker);

        // Validate the relation target each tick — targets can die mid-tick.
        if (!world.IsAlive(rel.Target) || world.Has<Dead>(rel.Target))
        {
            world.Remove<Fighting>(attacker);
            world.Remove<CombatState>(attacker);
            continue;
        }

        // Simple damage roll.
        int damage = Roll(rng, 1, 8) + world.Get<Level>(attacker).Value / 2;
        ref Health targetHp = ref world.Get<Health>(rel.Target);
        targetHp.Current -= damage;

        Console.WriteLine($"{attacker} hits {rel.Target} for {damage} — target HP: {targetHp.Current}/{targetHp.Max}");

        if (targetHp.Current <= 0)
        {
            world.Set<Dead>(rel.Target);           // tag: will be extracted later
            world.Remove<Fighting>(attacker);
            world.Remove<CombatState>(attacker);
        }
    }
}

// =============================================================================
// REGEN SYSTEM  (called every pulse_regen tick)
// =============================================================================

static void RegenSystem(World world)
{
    // All entities with Health that are NOT dead and NOT in combat regen HP.
    var query = new Query(world)
        .With<Health>()
        .Without<Dead>()
        .Without<CombatState>();

    foreach (EntityId e in query)
    {
        ref Health hp = ref world.Get<Health>(e);
        hp.Current = Math.Min(hp.Current + 5, hp.Max);
    }

    // Mana regen (everyone not dead).
    var manaQuery = new Query(world)
        .With<Mana>()
        .Without<Dead>();

    foreach (EntityId e in manaQuery)
    {
        ref Mana mp = ref world.Get<Mana>(e);
        mp.Current = Math.Min(mp.Current + 3, mp.Max);
    }
}

// =============================================================================
// TICK SYSTEM  (decrement timed tags and remove them when expired)
// =============================================================================

static void TickSystem(World world)
{
    // Stun ticker
    foreach (EntityId e in new Query(world).With<Stunned>())
    {
        ref Stunned s = ref world.Get<Stunned>(e);
        if (--s.TicksRemaining <= 0)
            world.Remove<Stunned>(e);
    }

    // Poison ticker
    foreach (EntityId e in new Query(world).With<Poisoned>().Without<Dead>())
    {
        ref Poisoned p = ref world.Get<Poisoned>(e);
        ref Health h = ref world.Get<Health>(e);
        h.Current -= p.DamagePerTick;

        if (--p.TicksRemaining <= 0)
            world.Remove<Poisoned>(e);

        if (h.Current <= 0)
            world.Set<Dead>(e);
    }

    // Casting ticker
    foreach (EntityId e in new Query(world).With<Casting>())
    {
        ref Casting c = ref world.Get<Casting>(e);
        if (--c.TicksRemaining <= 0)
        {
            // Spell completes — fire it (omitted for brevity) then clear tag.
            Console.WriteLine($"{e} finishes casting spell {c.SpellId}");
            world.Remove<Casting>(e);
        }
    }
}

// =============================================================================
// EXTRACTION SYSTEM  (clean up Dead entities at end of tick)
// =============================================================================

static void ExtractionSystem(World world)
{
    // Collect first — never remove while iterating a store.
    var dead = new List<EntityId>();
    foreach (EntityId e in new Query(world).With<Dead>())
        dead.Add(e);

    foreach (EntityId e in dead)
    {
        Console.WriteLine($"{e} extracted.");
        world.DestroyEntity(e);
    }
}

// =============================================================================
// RELATION EXAMPLE — starting a fight
// =============================================================================

static void StartFight(World world, EntityId aggressor, EntityId defender)
{
    world.Set(aggressor, new Fighting { Target = defender });
    world.Set<CombatState>(aggressor);

    // Defender fights back if they aren't already fighting.
    if (!world.Has<Fighting>(defender))
    {
        world.Set(defender, new Fighting { Target = aggressor });
        world.Set<CombatState>(defender);
    }
}

// =============================================================================
// RELATION EXAMPLE — group follow
// =============================================================================

static void JoinGroup(World world, EntityId follower, EntityId leader)
{
    world.Set(follower, new Following { Leader = leader });
    Console.WriteLine($"{follower} now follows {leader}");
}

// =============================================================================
// SIMULATE ONE TICK
// =============================================================================

StartFight(world, player, mob);

Console.WriteLine("--- Tick 1 ---");
CombatSystem(world, _rng);
TickSystem(world);
RegenSystem(world);
ExtractionSystem(world);

// =============================================================================
// HELPERS
// =============================================================================

static int Roll(Random rng, int count, int sides)
{
    int total = 0;
    for (int i = 0; i < count; i++) total += rng.Next(1, sides + 1);
    return total;
}
