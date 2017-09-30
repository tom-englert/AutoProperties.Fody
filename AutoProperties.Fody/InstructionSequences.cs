using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

using JetBrains.Annotations;

using Mono.Cecil.Cil;

namespace AutoProperties.Fody
{
    /// <summary>
    /// Group instructions by sequence points.
    /// </summary>
    internal class InstructionSequences : ReadOnlyCollection<InstructionSequence>
    {
        public InstructionSequences([NotNull, ItemNotNull] IList<Instruction> instructions, [CanBeNull, ItemNotNull] IList<SequencePoint> sequencePoints)
            : base(CreateSequences(instructions, sequencePoints).ToArray())
        {
        }

        [NotNull, ItemNotNull]
        private static IEnumerable<InstructionSequence> CreateSequences([NotNull, ItemNotNull] IList<Instruction> instructions, [CanBeNull, ItemNotNull] IList<SequencePoint> sequencePoints)
        {
            if (sequencePoints == null)
            {
                yield return new InstructionSequence(instructions, null, instructions.Count, null);
                yield break;
            }

            var sequencePointMapper = new SequencePointMapper(sequencePoints);

            var sequences = instructions
                .Select(inst => sequencePointMapper.GetNext(inst.Offset))
                .ToArray()
                .GroupBy(item => item);

            InstructionSequence previous = null;

            foreach (var group in sequences)
            {
                Debug.Assert(group != null, "group != null");
                yield return (previous = new InstructionSequence(instructions, previous, group.Count(), group.Key));
            }
        }

        private class SequencePointMapper
        {
            [NotNull, ItemNotNull]
            private readonly IList<SequencePoint> _sequencePoints;
            private int _index = 1;

            public SequencePointMapper([NotNull, ItemNotNull] IList<SequencePoint> sequencePoints)
            {
                _sequencePoints = sequencePoints;
            }

            [NotNull]
            public SequencePoint GetNext(int offset)
            {
                while (true)
                {
                    if (_index >= _sequencePoints.Count)
                        // ReSharper disable once AssignNullToNotNullAttribute
                        return _sequencePoints.Last();

                    var nextPoint = _sequencePoints[_index];

                    // ReSharper disable once PossibleNullReferenceException
                    if (nextPoint.Offset > offset)
                    {
                        // ReSharper disable once AssignNullToNotNullAttribute
                        return _sequencePoints[_index - 1];
                    }

                    _index += 1;
                }
            }
        }
    }
}
