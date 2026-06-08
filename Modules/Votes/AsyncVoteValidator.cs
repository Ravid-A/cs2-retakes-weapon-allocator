using static RetakesAllocator.Modules.Utils;
using static RetakesAllocator.Modules.Votes.Votes;

namespace RetakesAllocator.Modules.Votes;

public class AsyncVoteValidator
{
    private float _votePercentage;
    public int RequiredVotes { get => (int)Math.Round(ValidPlayerCount() * _votePercentage); }

    public AsyncVoteValidator()
    {
        _votePercentage = RequiredPercentage / 100F;
    }

    public bool CheckVotes(int numberOfVotes)
    {
        return numberOfVotes > 0 && numberOfVotes >= RequiredVotes;
    }
}
