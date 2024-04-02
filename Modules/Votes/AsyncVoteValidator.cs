using static RetakesAllocator.Modules.Utils;
using static RetakesAllocator.Modules.Votes.Votes;

namespace RetakesAllocator.Modules.Votes;

public class AsyncVoteValidator
{
    private float VotePercentage = 0F;
    public int RequiredVotes { get => (int)Math.Round(ValidPlayerCount() * VotePercentage); }

    public AsyncVoteValidator()
    {
        VotePercentage = RequiredPrecentage / 100F;
    }

    public bool CheckVotes(int numberOfVotes)
    {
        return numberOfVotes > 0 && numberOfVotes >= RequiredVotes;
    }
}