using static RetakesAllocator.Modules.Core;

namespace RetakesAllocator.Modules.Votes;

public class AsyncVoteManager
{
    private List<int> votes = new();
    public int VoteCount => votes.Count;
    public int RequiredVotes => _voteValidator.RequiredVotes;
    private readonly AsyncVoteValidator _voteValidator;
    public string Command => vote.Command;
    public Vote vote { get; set; } = null!;

    public AsyncVoteManager(Vote vote)
    {
        _voteValidator = new AsyncVoteValidator();
        this.vote = vote;
    }

    public void OnMapStart()
    {
        votes.Clear();
    }

    public VoteResultEnum AddVote(int userId)
    {
        VoteResultEnum? result = null;
        if (votes.IndexOf(userId) != -1)
            result = VoteResultEnum.AlreadyAddedBefore;
        else
        {
            votes.Add(userId);
            result = VoteResultEnum.Added;
        }

        return result.Value;
    }

    public bool CheckVotes()
    {
        if (_voteValidator.CheckVotes(votes.Count))
        {
            votes.Clear();
            return true;
        }

        return false;
    }

    public bool IsRunningVote()
    {
        return currentVote != null! && Command == currentVote.Command;
    }

    public void RemoveVote(int userId)
    {
        var index = votes.IndexOf(userId);
        if (index > -1)
            votes.RemoveAt(index);
    }
}