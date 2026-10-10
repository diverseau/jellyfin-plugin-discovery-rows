using DiscoveryRows.Core;
using Xunit;

namespace DiscoveryRows.Tests;

public sealed class AllocationTests
{
    private static RankedCandidate[] Candidates(int start, int count, string? franchise = null) => Enumerable.Range(start, count).Select(n => new RankedCandidate(new() { Id = Guid.NewGuid(), WorkKey = $"movie:tmdb:{n}", FranchiseKey = franchise }, new(true, "passed", .8))).ToArray();
    [Fact] public void UnderfilledRowDoesNotConsumeOtherRowsCandidates()
    {
        var a = new RowSettings { Key = "a", Minimum = 3, Limit = 3 };
        var b = new RowSettings { Key = "b", Minimum = 3, Limit = 3 };
        var result = RowAllocator.Allocate([(a, Candidates(1, 3)), (b, Candidates(1, 4))], true);
        Assert.Equal(3, result["a"].Length); Assert.Empty(result["b"]);
    }
    [Fact] public void AllocationIsIndependentOfCallbackOrder()
    {
        var a = new RowSettings { Key = "a", Minimum = 2, Limit = 3 };
        var b = new RowSettings { Key = "b", Minimum = 2, Limit = 3 };
        var ac = Candidates(1, 3); var bc = Candidates(1, 7);
        var one = RowAllocator.Allocate([(a, ac), (b, bc)], true);
        var two = RowAllocator.Allocate([(b, bc), (a, ac)], true);
        Assert.Equal(one["a"], two["a"]); Assert.Equal(one["b"], two["b"]);
        Assert.Empty(one["a"].Select(r => r.Item.WorkKey).Intersect(one["b"].Select(r => r.Item.WorkKey)));
    }
    [Fact] public void FranchiseCapIsAppliedBeforeMinimumTest()
    {
        var s = new RowSettings { Key = "a", Minimum = 3, Limit = 6, FranchiseCap = 2 };
        Assert.Empty(RowAllocator.Allocate([(s, Candidates(1, 6, "collection:1"))], false)["a"]);
    }
    [Fact] public void IndependentModeDoesNotSuppressOverlappingShelves()
    {
        var a = new RowSettings { Key = "a", Minimum = 2, Limit = 3 }; var b = a with { Key = "b" };
        var result = RowAllocator.Allocate([(a, Candidates(1, 3)), (b, Candidates(1, 3))], false);
        Assert.Equal(3, result["a"].Length); Assert.Equal(3, result["b"].Length);
    }
    [Fact] public void ReleasingFailedRowReservationsRefillsUsefulRow()
    {
        var a = new RowSettings { Key = "a", Minimum = 4, Limit = 4, FranchiseCap = 1 };
        var b = a with { Key = "b", FranchiseCap = 10 };
        var result = RowAllocator.Allocate([(a, Candidates(1, 4, "same")), (b, Candidates(1, 6))], true);
        Assert.Empty(result["a"]); Assert.Equal(4, result["b"].Length);
    }
    [Fact] public void FailedScarceShelfDoesNotCascadeIntoHealthyShelf()
    {
        var a = new RowSettings { Key = "a", Minimum = 3, Limit = 4, FranchiseCap = 2 };
        var b = a with { Key = "b" };
        var result = RowAllocator.Allocate([(a, Candidates(1, 3, "same")), (b, Candidates(1, 4))], true);
        Assert.Empty(result["a"]); Assert.Equal(4, result["b"].Length);
    }
}
