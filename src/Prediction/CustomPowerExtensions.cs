using CombatSolver.Engine.Common;
using CombatSolver.Engine.Common.Mirrors;
using CombatSolver.Engine.InCombat.Mirrors;
using CombatSolver.Engine.InCombat.Simulation;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace CombatSolver;

// Maintained by the Calculist fork. No native command queue is used.
internal static class CustomPowerMirrors
{
    public static Action<SimulatedCombatState, PowerModel, PowerModel>? BindAlias { get; set; }
    public static Action<SimulatedCombatState, PowerModel>? Created { get; set; }
    public static Action<SimulatedCombatState, Player, int>? PlayerTurnAdvanced { get; set; }
    public static Func<Type, bool>? UsesDurationSkip { get; set; }
    public static Action<CombatPredictionSimulator, PowerModel>? Applied { get; set; }
    public static Action<CombatPredictionSimulator, PowerModel>? Removed { get; set; }
    public static Func<SimulatedCombatState, PowerModel, Creature, decimal, Creature?, decimal>? ModifyReceived { get; set; }
    public static Func<SimulatedCombatState, AbstractModel, Creature, bool?>? ShouldClearBlock { get; set; }
    private static readonly MethodMirrorRegistry<PowerModel, CustomAfterSideTurnEndMirrorContext> EndRegistry = new(
        MirrorMethodSpec.Hook(nameof(AbstractModel.AfterSideTurnEnd),
            [typeof(PlayerChoiceContext), typeof(CombatSide), typeof(IEnumerable<Creature>)]));
    private static readonly MethodMirrorRegistry<PowerModel, CustomAfterEnergySpentMirrorContext> EnergyRegistry = new(
        MirrorMethodSpec.Hook(nameof(AbstractModel.AfterEnergySpent), [typeof(CardModel), typeof(int)]));
    private static readonly MethodMirrorRegistry<RelicModel, CustomCreatureAddedMirrorContext> CreatureAddedRegistry = new(
        MirrorMethodSpec.Hook(nameof(AbstractModel.AfterCreatureAddedToCombat), [typeof(Creature)]));

    public static void RegisterAfterSideTurnEnd<T>(Action<CombatPredictionSimulator, T, CombatSide, IReadOnlyList<Creature>> handler) where T : PowerModel
        => EndRegistry.Register<T>((power, context) => handler(context.Simulator, power, context.Side, context.Participants));
    public static void RegisterAfterEnergySpent<T>(Action<CombatPredictionSimulator, T, CardModel, int> handler) where T : PowerModel
        => EnergyRegistry.Register<T>((power, context) => handler(context.Simulator, power, context.Card, context.Amount));
    public static void RegisterAfterCreatureAdded<T>(Action<CombatPredictionSimulator, T, Creature> handler) where T : RelicModel
        => CreatureAddedRegistry.Register<T>((relic, context) => handler(context.Simulator, relic, context.Creature));
    internal static bool TryCreatureAdded(CombatPredictionSimulator simulator, RelicModel relic, Creature creature)
    {
        if (!CreatureAddedRegistry.HasRegisteredHandler(relic)) return false;
        using var opaque = simulator.BeginExecutionDispatch();
        CreatureAddedRegistry.Invoke(relic, new CustomCreatureAddedMirrorContext { Simulator = simulator, Creature = creature });
        return true;
    }
    public static bool TryEnd(CombatPredictionSimulator simulator, PowerModel power, CombatSide side, IReadOnlyList<Creature> participants)
    {
        if (!EndRegistry.HasRegisteredHandler(power)) return false;
        using var opaque = simulator.BeginExecutionDispatch();
        EndRegistry.Invoke(power, new CustomAfterSideTurnEndMirrorContext
        { Simulator = simulator, Side = side, Participants = participants });
        return true;
    }
    public static void AfterEnergySpent(CombatPredictionSimulator simulator, SimulatedCombatState combat, PredictedCard card, int amount)
    {
        using var opaque = simulator.BeginExecutionDispatch();
        foreach (var power in combat.EffectivePowers().ToArray())
        {
            if (EnergyRegistry.HasRegisteredHandler(power))
                EnergyRegistry.Invoke(power, new CustomAfterEnergySpentMirrorContext
                { Simulator = simulator, Card = card.Preview, Amount = amount });
            if (simulator.HasPendingChoice) return;
        }
    }
}

internal sealed class CustomAfterSideTurnEndMirrorContext : CombatMirrorContext<PowerModel>
{
    public required CombatSide Side { get; init; }
    public required IReadOnlyList<Creature> Participants { get; init; }
}
internal sealed class CustomAfterEnergySpentMirrorContext : CombatMirrorContext<PowerModel>
{
    public required CardModel Card { get; init; }
    public required int Amount { get; init; }
}
internal sealed class CustomCreatureAddedMirrorContext : CombatMirrorContext<RelicModel>
{
    public required Creature Creature { get; init; }
}

internal sealed partial class SimulatedCombatState
{
    private CombatPredictionSimulator? _customPowerSimulator;
    internal void AttachCustomPowerSimulator(CombatPredictionSimulator simulator) => _customPowerSimulator = simulator;

    internal int ModifyCustomPowerAmount(CombatPredictionSimulator simulator, PowerModel source, int offset, Creature? applier, CardModel? cardSource)
    {
        if (simulator.IsEnding) return 0;
        PowerModel power = GetMutablePowerInstance(source);
        BeginCardPowerApplication(cardSource);
        try
        {
            int modified = ModifyPowerAmountForRelics(power, power.Owner, offset, applier);
            modified = (int)(CustomPowerMirrors.ModifyReceived?.Invoke(this, power, power.Owner, modified, applier) ?? modified);
            int previous = power._amount;
            power._amount = Math.Clamp(previous + modified, -999_999_999, 999_999_999);
            UpdatePowerListenerOrder(power, previous, power._amount);
            InvalidateHookListenersForAmountTransition(previous, power._amount);
            RecordPowerAmountChange(power, modified, applier);
            RecordPossessedStatChange(power, modified, applier);
            PowerLifecycleSupport.ResolvePowerAmountChanges(simulator, this);
            if (power.ShouldRemoveDueToAmount()) RemoveCustomPower(simulator, power);
            return power._amount;
        }
        finally { CompleteCardPowerApplication(cardSource); }
    }

    internal void RemoveCustomPower(CombatPredictionSimulator simulator, PowerModel source)
    {
        PowerModel power = GetMutablePowerInstance(source);
        SetPowerAmount(power, 0);
        CustomPowerMirrors.Removed?.Invoke(simulator, power);
    }

    private void NotifyCustomPowerApplied(PowerModel power, int previousAmount)
    {
        if (previousAmount != 0 || power.Amount == 0 || CustomPowerMirrors.Applied == null) return;
        CustomPowerMirrors.Applied(_customPowerSimulator
            ?? throw new InvalidOperationException("Custom power lifecycle has no branch simulator."), power);
    }

    private bool ShouldClearCustomBlock(Creature creature, out AbstractModel? preventer)
    {
        foreach (var listener in IterateHookListeners())
        {
            bool clear = CustomPowerMirrors.ShouldClearBlock?.Invoke(this, listener, creature)
                ?? listener.ShouldClearBlock(creature);
            if (!clear) { preventer = listener; return false; }
        }
        preventer = null; return true;
    }
}
