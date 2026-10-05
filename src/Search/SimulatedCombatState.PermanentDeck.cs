using System.Reflection;
using System.Text;
using CombatSolver.Engine.Common;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace CombatSolver;

internal sealed partial class SimulatedCombatState
{
    private static readonly FieldInfo RunCardsField = typeof(RunState).GetField("_allCards", BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new MissingFieldException(typeof(RunState).FullName, "_allCards");
    // The original models are immutable identities, not branch-mutable previews.
    private IReadOnlyDictionary<Player, CardModel[]> _rootPermanentDeckMembers = null!;
    private IReadOnlyDictionary<CardModel, int> _rootPermanentCardIndices = null!;
    private IReadOnlyDictionary<AbstractModel, CardModel> _rootPermanentDeckListenerSources = null!;
    private ForkableSet<CardModel>? _removedPermanentDeckCards;

    private static Dictionary<CardModel, int> CaptureRunCardIndices(IRunState run)
    {
        if (run is not RunState native)
            throw new NotSupportedException("Permanent card identity requires a native RunState.");
        var cards = (List<CardModel>)(RunCardsField.GetValue(native)
            ?? throw new InvalidOperationException("Native run card identity list is absent."));
        Dictionary<CardModel, int> indices = new(ReferenceEqualityComparer.Instance);
        for (int index = 0; index < cards.Count; index++) indices.Add(cards[index], index);
        return indices;
    }

    private void CapturePermanentDeckRoot(ICombatState state, IReadOnlyDictionary<AbstractModel, AbstractModel> clones)
    {
        _rootPermanentDeckMembers = state.Players.ToDictionary(player => player, player => player.Deck.Cards.ToArray());
        _rootPermanentCardIndices = CaptureRunCardIndices(state.RunState);
        Dictionary<AbstractModel, CardModel> listeners = new(ReferenceEqualityComparer.Instance);
        foreach (CardModel card in _rootPermanentDeckMembers.Values.SelectMany(cards => cards))
        {
            if (!_rootPermanentCardIndices.ContainsKey(card)) throw new InvalidOperationException("Deck card is outside its native run scope.");
            if (clones.TryGetValue(card, out AbstractModel? clone)) listeners.Add(clone, card);
            if (card.Enchantment is { } enchantment && clones.TryGetValue(enchantment, out clone)) listeners.Add(clone, card);
        }
        _rootPermanentDeckListenerSources = listeners;
    }

    internal IReadOnlyList<CardModel> PermanentDeckMembers(Player player)
    {
        if (!_rootPermanentDeckMembers.TryGetValue(player, out CardModel[]? members))
            throw new InvalidOperationException("Permanent deck was not captured for this player.");
        return _removedPermanentDeckCards is null ? members : members.Where(card => !_removedPermanentDeckCards.Contains(card)).ToArray();
    }

    internal void RemovePermanentDeckCard(Player player, CardModel card)
    {
        if (!PermanentDeckMembers(player).Any(current => ReferenceEquals(current, card)))
            throw new InvalidOperationException("The stolen DeckVersion is not a current permanent deck member.");
        (_removedPermanentDeckCards ??= new(ReferenceEqualityComparer.Instance)).Add(card);
        InvalidateHookListeners();
    }

    private bool IsRemovedPermanentDeckListener(AbstractModel listener)
        => _removedPermanentDeckCards is not null
            && _rootPermanentDeckListenerSources.TryGetValue(listener, out CardModel? source)
            && _removedPermanentDeckCards.Contains(source);

    private IReadOnlyDictionary<AbstractModel, CardModel> ForkPermanentDeckListenerSources(PredictionForkContext context)
    {
        Dictionary<AbstractModel, CardModel>? remapped = null;
        foreach (var pair in _rootPermanentDeckListenerSources)
        {
            AbstractModel mapped = context.RemapOrSelf(pair.Key);
            if (ReferenceEquals(mapped, pair.Key)) continue;
            remapped ??= new(_rootPermanentDeckListenerSources, ReferenceEqualityComparer.Instance);
            if (!remapped.TryAdd(mapped, pair.Value) && !ReferenceEquals(remapped[mapped], pair.Value))
                throw new InvalidOperationException("Permanent listener remapping collided across card identities.");
        }
        return remapped ?? _rootPermanentDeckListenerSources;
    }

    private void AppendPermanentDeckFingerprint(ref StateFingerprintBuilder fingerprint)
    {
        fingerprint.Add('d');
        foreach (Player player in Players)
        {
            var members = PermanentDeckMembers(player);
            fingerprint.Add(player.NetId); fingerprint.Add(members.Count);
            foreach (CardModel card in members) fingerprint.Add(_rootPermanentCardIndices[card]);
        }
    }

    internal static void AppendLivePermanentDeckContinuation(StringBuilder text, CombatState state)
    {
        var indices = CaptureRunCardIndices(state.RunState);
        text.Append(";permanent_deck=");
        foreach (Player player in state.Players)
        {
            text.Append(player.NetId).Append(':');
            foreach (CardModel card in player.Deck.Cards) text.Append(indices[card]).Append(',');
            text.Append('/');
        }
    }

    internal void AppendPredictedPermanentDeckContinuation(StringBuilder text)
    {
        text.Append(";permanent_deck=");
        foreach (Player player in Players)
        {
            text.Append(player.NetId).Append(':');
            foreach (CardModel card in PermanentDeckMembers(player)) text.Append(_rootPermanentCardIndices[card]).Append(',');
            text.Append('/');
        }
    }
}
