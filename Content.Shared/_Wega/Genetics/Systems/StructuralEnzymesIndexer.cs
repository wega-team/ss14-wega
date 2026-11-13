using System.Linq;
using Content.Shared.GameTicking;
using Robust.Shared.Random;

namespace Content.Shared.Genetics.Systems
{
    public sealed partial class StructuralEnzymesIndexerSystem : EntitySystem
    {
        [Dependency] private IRobustRandom _random = default!;

        private List<EnzymesPrototypeInfo> _enzymes = new();

        [SubscribeLocalEvent]
        private void OnRoundRestart(RoundRestartCleanupEvent args)
            => _enzymes.Clear();

        public List<EnzymesPrototypeInfo> GetAllEnzymesPrototypes()
        {
            if (_enzymes.Count == 0)
                InitializeEnzymesPrototypes();

            return _enzymes;
        }

        private void InitializeEnzymesPrototypes()
        {
            _enzymes.Clear();

            var allEnzymesPrototypes = ProtoMan.EnumeratePrototypes<StructuralEnzymesPrototype>().ToList();
            _random.Shuffle(allEnzymesPrototypes);

            const int maxBlocks = 54;
            var blocksToAdd = Math.Min(allEnzymesPrototypes.Count, maxBlocks);

            for (var i = 0; i < blocksToAdd; i++)
            {
                _enzymes.Add(new EnzymesPrototypeInfo
                {
                    EnzymesPrototypeId = allEnzymesPrototypes[i].ID
                });
            }

            for (var i = blocksToAdd; i < maxBlocks; i++)
                _enzymes.Add(new EnzymesPrototypeInfo());

            _random.Shuffle(_enzymes);

            for (var i = 0; i < _enzymes.Count; i++)
                _enzymes[i] = _enzymes[i] with { Order = i + 1 };

            _enzymes.Add(new EnzymesPrototypeInfo { Order = 55 });
        }
    }
}
