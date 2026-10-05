using CombatSolver.Engine.Common;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Creatures;
using System.Text;
using CombatSolver.Engine.InCombat.Simulation;

namespace CombatSolver;

// Maintained by the Calculist fork. These are deliberately
// internal: only the isolated friend host can register a semantic adapter.
internal interface ICustomCombatPredictionState : IPredictionStateForkable
{
    void BindObservation(CombatPredictionSimulator simulator) { }
    void AppendFingerprint(ref StateFingerprintBuilder fingerprint);
    string ContinuationText { get; }
}

internal static class CustomCombatStateMirrors
{
    private static Func<CombatState, ICustomCombatPredictionState>? _capture;
    private static Func<CombatState, string>? _live;
    private static Func<Type, bool>? _subscriber;
    private static bool _sealed;
    public static bool Strict { get; set; }
    public static Action<CombatPredictionSimulator, CardPlay>? AfterCardResolved { get; set; }
    public static Action<CombatPredictionSimulator, CardModel, bool>? SetPlayMode { get; set; }
    public static Func<CombatPredictionSimulator, CardModel, bool>? IsRecurrence { get; set; }
    public static Func<CombatPredictionSimulator, CardModel, bool>? ShouldPlay { get; set; }
    public static Action<CombatPredictionSimulator, CardPlay>? CardPlayStarted { get; set; }
    public static Action<CombatPredictionSimulator, PredictedCard>? CardExhausted { get; set; }
    public static Func<CardModel, string>? ChoiceSemanticKey { get; set; }

    public static void Register(Func<CombatState, ICustomCombatPredictionState> capture,
        Func<CombatState, string> liveContinuation, Func<Type, bool> supportedSubscriber)
    {
        if (_sealed || _capture != null)
            throw new InvalidOperationException("Custom combat adapter registration is closed or duplicate.");
        _capture = capture ?? throw new ArgumentNullException(nameof(capture));
        _live = liveContinuation ?? throw new ArgumentNullException(nameof(liveContinuation));
        _subscriber = supportedSubscriber ?? throw new ArgumentNullException(nameof(supportedSubscriber));
    }

    public static ICustomCombatPredictionState? Capture(CombatState live)
    {
        _sealed = true;
        if (Strict && _capture == null) throw new NotSupportedException("Strict custom combat adapter is missing.");
        var state = _capture?.Invoke(live);
        if (Strict && state == null) throw new NotSupportedException("Strict custom combat adapter returned no captured state.");
        return state;
    }
    public static string LiveContinuation(CombatState live)
    {
        _sealed = true;
        return _live?.Invoke(live) ?? "";
    }
    public static bool SupportsSubscriber(Type type) => _subscriber?.Invoke(type) == true;
    public static ICustomCombatPredictionState? Fork(ICustomCombatPredictionState? state,
        PredictionForkContext context)
    {
        if (state == null) return null;
        if (state is IPredictionForkBoundary boundary) boundary.AssertForkable();
        var copy = state.Fork(context) as ICustomCombatPredictionState;
        if (copy == null || ReferenceEquals(copy, state) || copy.GetType() != state.GetType())
            throw new InvalidOperationException("Custom combat state must fork to a detached same-type state.");
        context.Register(state, copy);
        return copy;
    }
}

internal sealed partial class SimulatedCombatState
{
    public ICustomCombatPredictionState? CustomState { get; private set; }
    // Continuation describes native HappenedThisTurn's current-side window.
    // Gameplay counters remain intact for effects that explicitly use the
    // player's turn window. The tuple map is replaced, never mutated/shared.
    private IReadOnlyDictionary<Player,(int Status,int Zero,int Starts,int AttackSkill,int Attacks)>? _continuationEnemyHistoryBaseline;
    private void CaptureContinuationSideWindow(CombatSide next)
    {
        if(next==_currentSide)return;
        _continuationEnemyHistoryBaseline=next==CombatSide.Enemy?_players.ToDictionary(player=>player,player=>
            (GetStatusCardsDrawnThisTurn(player),GetZeroCostAttackStartsThisTurn(player.Creature),
             GetCardPlayStartsThisTurn(player.Creature),GetAttackSkillStartsThisTurn(player.Creature),GetAttackPlayStartsThisTurn(player.Creature))):null;
    }
    private void AppendContinuationSideCardHistory(StringBuilder text,Player player)
    {
        var baseline=_currentSide==CombatSide.Enemy&&_continuationEnemyHistoryBaseline?.TryGetValue(player,out var found)==true?found:default;
        AppendTurnCardHistory(text,GetStatusCardsDrawnThisTurn(player)-baseline.Status,
            GetZeroCostAttackStartsThisTurn(player.Creature)-baseline.Zero,GetCardPlayStartsThisTurn(player.Creature)-baseline.Starts,
            GetAttackSkillStartsThisTurn(player.Creature)-baseline.AttackSkill);
        text.Append(";FlameHp=").Append(_brightestFlameMaxHpSpent);
        text.Append(";AttackStarts=").Append(GetAttackPlayStartsThisTurn(player.Creature)-baseline.Attacks);
    }
}

internal sealed partial class CombatBeamSolver
{
    internal SimulationSnapshot ReplayIsolatedPrefix(IReadOnlyList<PlanAction> actions) => Replay(actions);
}
