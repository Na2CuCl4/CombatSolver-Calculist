using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using CombatSolver.Engine.InCombat.Simulation;

namespace CombatSolver;

internal sealed partial class SimulatedCombatState
{
    private ForkableDictionary<(Creature Possessor, Creature Target), decimal>? _stolenStrength;
    private ForkableDictionary<(Creature Possessor, Creature Target), decimal>? _stolenDexterity;

    private void CapturePossessRootState(PowerModel livePower)
    {
        (string field, bool strength) = livePower switch
        {
            PossessStrengthPower => ("stolenStrength", true),
            PossessSpeedPower => ("stolenDexterity", false),
            _ => throw new InvalidOperationException("Possess root capture requires a possession power."),
        };
        object data = PowerInternalDataField.GetValue(livePower)
            ?? throw new InvalidOperationException($"{livePower.Id.Entry} has no internal possession state.");
        var captured = (Dictionary<Creature, decimal>)(data.GetType().GetField(field)?.GetValue(data)
            ?? throw new MissingFieldException(data.GetType().FullName, field));
        var ledger = strength ? (_stolenStrength ??= []) : (_stolenDexterity ??= []);
        foreach ((Creature target, decimal stolen) in captured)
            ledger.Add((livePower.Owner, target), stolen);
    }

    private void RecordPossessedStatChange(PowerModel power, int applied, Creature? applier)
    {
        if (applied >= 0 || applier == null || power.Owner.Player == null)
            return;
        ForkableDictionary<(Creature, Creature), decimal>? ledger = power switch
        {
            StrengthPower when GetAmount<PossessStrengthPower>(applier) > 0
                => _stolenStrength ??= [],
            DexterityPower when GetAmount<PossessSpeedPower>(applier) > 0
                => _stolenDexterity ??= [],
            _ => null,
        };
        if (ledger == null)
            return;
        (Creature, Creature) key = (applier, power.Owner);
        ledger[key] = ledger.GetValueOrDefault(key) + applied;
    }

    public void RefundPossessedStats(CombatPredictionSimulator simulator, Creature possessor)
    {
        Refund<StrengthPower>(_stolenStrength, possessor, !simulator.IsEnding);
        Refund<DexterityPower>(_stolenDexterity, possessor, !simulator.IsEnding);
    }

    private void Refund<T>(
        ForkableDictionary<(Creature Possessor, Creature Target), decimal>? ledger,
        Creature possessor,
        bool canApplyRefund)
        where T : PowerModel
    {
        if (ledger == null)
            return;
        foreach (((Creature owner, Creature target), decimal stolen) in ledger
                     .Where(entry => entry.Key.Possessor == possessor)
                     .ToArray())
        {
            // Native stores the decimal hook delta and truncates only when the
            // refund is applied to the integer Power amount.
            if (canApplyRefund)
                Apply<T>(target, checked((int)-stolen), null);
            ledger.Remove((owner, target));
        }
    }

    private void AppendPossessFingerprint(ref StateFingerprintBuilder fingerprint)
    {
        AppendPossessLedger(ref fingerprint, 's', _stolenStrength);
        AppendPossessLedger(ref fingerprint, 'y', _stolenDexterity);
    }

    private static void AppendPossessLedger(
        ref StateFingerprintBuilder fingerprint,
        char marker,
        IReadOnlyDictionary<(Creature Possessor, Creature Target), decimal>? ledger)
    {
        if (ledger == null)
            return;
        foreach (((Creature possessor, Creature target), decimal amount) in ledger
                     .OrderBy(entry => entry.Key.Possessor.CombatId)
                     .ThenBy(entry => entry.Key.Target.CombatId))
        {
            fingerprint.Add(marker);
            fingerprint.Add(possessor.CombatId ?? uint.MaxValue);
            fingerprint.Add(target.CombatId ?? uint.MaxValue);
            fingerprint.Add(amount);
        }
    }
}
