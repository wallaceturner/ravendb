using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Corax.Querying.Matches.Meta;
using Sparrow.Server;

namespace Corax.Querying.Matches
{
    [DebuggerDisplay("{DebugView,nq}")]
    public unsafe partial struct BinaryMatch<TInner, TOuter> : IQueryMatch
        where TInner : IQueryMatch
        where TOuter : IQueryMatch
    {
        private readonly delegate*<ref BinaryMatch<TInner, TOuter>, Span<long>, int>  _fillFunc;
        private readonly delegate*<ref BinaryMatch<TInner, TOuter>, Span<long>, int, int> _andWithFunc;
        private readonly delegate*<ref BinaryMatch<TInner, TOuter>, QueryInspectionNode> _inspectFunc;

        private TInner _inner;
        private TOuter _outer;
        private MemoizationMatchProvider<TOuter> _memoizedOuter;
        private readonly ByteStringContext _ctx;
        private readonly Querying.IndexSearcher _indexSearcher;
        private readonly long _totalResults;
        private readonly QueryCountConfidence _confidence;
        private readonly SkipSortingResult _skipSortingResult;
        private readonly CancellationToken _token;
        private readonly SkipSortingResult _innerSkipSorting;
        private readonly SkipSortingResult _outerSkipSorting;

        public SkipSortingResult AttemptToSkipSorting() => _skipSortingResult;

        public bool IsBoosting => _inner.IsBoosting || _outer.IsBoosting;

        public long Count => _totalResults;

        public QueryCountConfidence Confidence => _confidence;

        private BinaryMatch(
            Querying.IndexSearcher indexSearcher,
            in TInner inner, in TOuter outer,
            delegate*<ref BinaryMatch<TInner, TOuter>, Span<long>, int> fillFunc,
            delegate*<ref BinaryMatch<TInner, TOuter>, Span<long>, int, int> andWithFunc,
            delegate*<ref BinaryMatch<TInner, TOuter>, QueryInspectionNode> inspectionFunc,
            long totalResults,
            QueryCountConfidence confidence,
            SkipSortingResult skipSortingResult,
            in CancellationToken token)
        {
            _indexSearcher = indexSearcher;
            _totalResults = totalResults;

            _fillFunc = fillFunc;
            _andWithFunc = andWithFunc;
            _inspectFunc = inspectionFunc;

            _inner = inner;
            _outer = outer;
            _confidence = confidence;
            _skipSortingResult = skipSortingResult;
            _token = token;
            _ctx = indexSearcher.Allocator;

            _innerSkipSorting = _inner.AttemptToSkipSorting();
            _outerSkipSorting = _outer.AttemptToSkipSorting();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Fill(Span<long> buffer)
        {
            return _fillFunc(ref this, buffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int AndWith(Span<long> buffer, int matches)
        {
            return _andWithFunc(ref this, buffer, matches);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Score(Span<long> matches, Span<float> scores, float boostFactor)
        {
            // Nothing to do if there is no boosting happening at this level.
            // Remember: When you're sorting by score and primitives can be boosted, those should be true!
            bool innerBoosting = _inner.IsBoosting;
            bool outerBoosting = _outer.IsBoosting;
            if (innerBoosting == false && outerBoosting == false)
                return;
            
            // From now on we have boosting happening somewhere in this chain. 

            // If there are two chains we need to combine them.
            if (innerBoosting == true && outerBoosting == true)
            {
                _inner.Score(matches, scores, boostFactor);
                _outer.Score(matches, scores, boostFactor);
                return;
            }

            // From now on, only a single requires score calculations. 

            if (innerBoosting == true)
            {
                // Inner can still be not boosting. In this case it is, so we delegate
                // the call into the boosting layer that provides us the information.
                _inner.Score(matches, scores, boostFactor);
                return;
            }

            if (outerBoosting == true)
            {
                // Outer can still be not boosting. In this case it is, so we delegate
                // the call into the boosting layer that provides us the information.
                _outer.Score(matches, scores, boostFactor);
            }
        }

        public QueryInspectionNode Inspect()
        {
            return _inspectFunc is null ? QueryInspectionNode.NotInitializedInspectionNode(nameof(BinaryMatch)) : _inspectFunc(ref this);
        }

        string DebugView => Inspect().ToString();
    }
}
