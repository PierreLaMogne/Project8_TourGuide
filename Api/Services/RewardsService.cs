using GpsUtil.Location;
using System.Collections.Concurrent;
using TourGuide.LibrairiesWrappers.Interfaces;
using TourGuide.Services.Interfaces;
using TourGuide.Users;

namespace TourGuide.Services;

public class RewardsService : IRewardsService
{
    private const double StatuteMilesPerNauticalMile = 1.15077945;
    private readonly int _defaultProximityBuffer = 10;
    private int _proximityBuffer;
    private readonly int _attractionProximityRange = 200;
    private readonly IGpsUtil _gpsUtil;
    private readonly IRewardCentral _rewardsCentral;
    private static int count = 0;

    public RewardsService(IGpsUtil gpsUtil, IRewardCentral rewardCentral)
    {
        _gpsUtil = gpsUtil;
        _rewardsCentral =rewardCentral;
        _proximityBuffer = _defaultProximityBuffer;
    }

    public void SetProximityBuffer(int proximityBuffer)
    {
        _proximityBuffer = proximityBuffer;
    }

    public void SetDefaultProximityBuffer()
    {
        _proximityBuffer = _defaultProximityBuffer;
    }

    /// <summary>
    /// Method changed to go asynchronous to improve performance when calculating rewards for multiple users
    /// Adding a rewardsToAdd ConcurrentBag to get all the Reward to add without locking the userLocation and userReward Lists, avoiding potential concurrent modification exceptions
    /// Tasking the reward calculation by matching directly attractions that are not rewarded and that the user visited
    /// This beneficiates from the fact that the GetAttractions method goes asynchronous and that the reward calculation is not CPU intensive, so it goes parallel without blocking the main thread
    /// At the end, rewards are added to the user in a single loop, avoiding potential concurrent modification exceptions and improving performance when calculating rewards for multiple users
    /// </summary>

    public async Task CalculateRewards(User user)
    {
        count++;
        var rewardsToAdd = new ConcurrentBag<UserReward>();
        List<VisitedLocation> userLocations = user.VisitedLocations.ToList();
        List<Attraction> attractions = await _gpsUtil.GetAttractions();

        var tasks = userLocations.SelectMany(visitedLocation =>
            attractions.Where(attraction => !user.UserRewards.Any(r => r.Attraction.AttractionName == attraction.AttractionName) && NearAttraction(visitedLocation, attraction))
                       .Select(attraction => Task.Run(() =>
                       {
                            int rewardPoints = GetRewardPoints(attraction, user);
                            rewardsToAdd.Add(new UserReward(visitedLocation, attraction, rewardPoints));
                       })));
        await Task.WhenAll(tasks);

        foreach (var reward in rewardsToAdd)
        {
            user.AddUserReward(reward);
        }
    }

    public bool IsWithinAttractionProximity(Attraction attraction, Locations location)
    {
        Console.WriteLine(GetDistance(attraction, location));
        return GetDistance(attraction, location) <= _attractionProximityRange;
    }

    private bool NearAttraction(VisitedLocation visitedLocation, Attraction attraction)
    {
        return GetDistance(attraction, visitedLocation.Location) <= _proximityBuffer;
    }

    private int GetRewardPoints(Attraction attraction, User user)
    {
        return _rewardsCentral.GetAttractionRewardPoints(attraction.AttractionId, user.UserId);
    }

    public double GetDistance(Locations loc1, Locations loc2)
    {
        double lat1 = Math.PI * loc1.Latitude / 180.0;
        double lon1 = Math.PI * loc1.Longitude / 180.0;
        double lat2 = Math.PI * loc2.Latitude / 180.0;
        double lon2 = Math.PI * loc2.Longitude / 180.0;

        double angle = Math.Acos(Math.Sin(lat1) * Math.Sin(lat2)
                                + Math.Cos(lat1) * Math.Cos(lat2) * Math.Cos(lon1 - lon2));

        double nauticalMiles = 60.0 * angle * 180.0 / Math.PI;
        return StatuteMilesPerNauticalMile * nauticalMiles;
    }
}
