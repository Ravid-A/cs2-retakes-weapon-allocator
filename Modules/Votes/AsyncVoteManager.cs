using RetakesAllocator.Modules.Models;
using static RetakesAllocator.Modules.Core;

namespace RetakesAllocator.Modules.Votes;

public class AsyncVoteManager
{
    private List<int> _votes = new();
    public int VoteCount => _votes.Count;
    public int RequiredVotes => _voteValidator.RequiredVotes;
    private readonly AsyncVoteValidator _voteValidator;
    public string Command => Vote.Command;
    public Vote Vote { get; set; } = null!;

    public AsyncVoteManager(Vote vote)
    {
        _voteValidator = new AsyncVoteValidator();
        Vote = vote;
    }

    public void OnMapStart()
    {
        _votes.Clear();
    }

    public VoteResultEnum AddVote(int userId)
    {
        VoteResultEnum? result = null;
        if (_votes.IndexOf(userId) != -1)
            result = VoteResultEnum.AlreadyAddedBefore;
        else
        {
            _votes.Add(userId);
            result = VoteResultEnum.Added;
        }

        return result.Value;
    }

    public bool CheckVotes()
    {
        if (_voteValidator.CheckVotes(_votes.Count))
        {
            _votes.Clear();
            return true;
        }

        return false;
    }

    public bool IsRunningVote()
    {
        return CurrentVote != null! && Command == CurrentVote.Command;
    }

    public void RemoveVote(int userId)
    {
        var index = _votes.IndexOf(userId);
        if (index > -1)
            _votes.RemoveAt(index);
    }

    public void ClearVotes()
    {
        _votes.Clear();
    }
}
