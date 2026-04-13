using GpsUtil.Location;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Globalization;
using TourGuide.LibrairiesWrappers.Interfaces;
using TourGuide.Services.Interfaces;
using TourGuide.Users;
using TourGuide.Utilities;
using TripPricer;

namespace TourGuide.Services;

public class TourGuideService : ITourGuideService, IDisposable
{
    private readonly ILogger _logger;
    private readonly IGpsUtil _gpsUtil;
    private readonly IRewardsService _rewardsService;
    private readonly TripPricer.TripPricer _tripPricer;
    private readonly RewardCentral.RewardCentral _rewardCentral;
    public Tracker Tracker { get; private set; }
    private readonly Dictionary<string, User> _internalUserMap = new();
    private const string TripPricerApiKey = "test-server-api-key";
    private readonly bool _testMode = true;

    public TourGuideService(ILogger<TourGuideService> logger, IGpsUtil gpsUtil, IRewardsService rewardsService, ILoggerFactory loggerFactory)
    {
        _logger = logger;
        _tripPricer = new();
        _rewardCentral = new();
        _gpsUtil = gpsUtil;
        _rewardsService = rewardsService;

        CultureInfo.CurrentCulture = new CultureInfo("en-US");

        if (_testMode)
        {
            _logger.LogInformation("TestMode enabled");
            _logger.LogDebug("Initializing users");
            InitializeInternalUsers();
            _logger.LogDebug("Finished initializing users");
        }

        var trackerLogger = loggerFactory.CreateLogger<Tracker>();

        Tracker = new Tracker(this, trackerLogger);
        AddShutDownHook();
    }

    public List<UserReward> GetUserRewards(User user)
    {
        return user.UserRewards;
    }

    public async Task<VisitedLocation> GetUserLocation(User user) // Method changed to go asynchronous do to TrackUserLocation being asynchronous
    {
        return user.VisitedLocations.Any() ? user.GetLastVisitedLocation() : await TrackUserLocation(user);
    }

    public User GetUser(string userName)
    {
        return _internalUserMap.ContainsKey(userName) ? _internalUserMap[userName] : null;
    }

    public List<User> GetAllUsers()
    {
        return _internalUserMap.Values.ToList();
    }

    public void AddUser(User user)
    {
        if (!_internalUserMap.ContainsKey(user.UserName))
        {
            _internalUserMap.Add(user.UserName, user);
        }
    }

    public List<Provider> GetTripDeals(User user)
    {
        int cumulativeRewardPoints = user.UserRewards.Sum(i => i.RewardPoints);
        List<Provider> providers = _tripPricer.GetPrice(TripPricerApiKey, user.UserId,
            user.UserPreferences.NumberOfAdults, user.UserPreferences.NumberOfChildren,
            user.UserPreferences.TripDuration, cumulativeRewardPoints);
        user.TripDeals = providers;
        return providers;
    }

    public async Task<VisitedLocation> TrackUserLocation(User user) // Method changed to go asynchronous do to GetUserLocation and CalculateRewards being asynchronous
    {
        VisitedLocation visitedLocation = await _gpsUtil.GetUserLocation(user.UserId);
        user.AddToVisitedLocations(visitedLocation);
        await _rewardsService.CalculateRewards(user);
        return visitedLocation;
    }

    /// <summary>
    /// Method changed to go asynchronous do to GetUserLocation and CalculateRewards being asynchronous
    /// Now retunring a List of newly created NearbyAttraction objects that contains informations required by the user
    /// Has User as parameter to get the calculate rewards points for each attraction (meaning GetUserLocation is used into this method and no longer in the Controller)
    /// </summary>
    public async Task<List<NearbyAttraction>> GetNearbyAttractions(User user)
    {
        var visitedLocation = await GetUserLocation(user);
        List<Attraction> allAttractions = await _gpsUtil.GetAttractions();

        List<NearbyAttraction> nearbyAttractions = allAttractions
            .Select(attraction => new NearbyAttraction(
                attraction.AttractionName,
                attraction,
                visitedLocation.Location,
                _rewardsService.GetDistance(visitedLocation.Location, attraction),
                _rewardCentral.GetAttractionRewardPoints(attraction.AttractionId, user.UserId)
            ))
            .OrderBy(nearbyAttraction => nearbyAttraction.Distance)
            .Take(5)
            .ToList();

        return nearbyAttractions;
    }

    private void AddShutDownHook()
    {
        AppDomain.CurrentDomain.ProcessExit += (sender, e) => Tracker.StopTracking();
    }
    public void Dispose()
    {
        Tracker?.StopTracking();
        Tracker?.Dispose();
    }

    /**********************************************************************************
    * 
    * Methods Below: For Internal Testing
    * 
    **********************************************************************************/

    private void InitializeInternalUsers()
    {
        for (int i = 0; i < InternalTestHelper.GetInternalUserNumber(); i++)
        {
            var userName = $"internalUser{i}";
            var user = new User(Guid.NewGuid(), userName, "000", $"{userName}@tourGuide.com");
            GenerateUserLocationHistory(user);
            _internalUserMap.Add(userName, user);
        }

        _logger.LogDebug("Created {InternalUserCount} internal test users.", InternalTestHelper.GetInternalUserNumber());
    }

    private void GenerateUserLocationHistory(User user)
    {
        for (int i = 0; i < 3; i++)
        {
            var visitedLocation = new VisitedLocation(user.UserId, new Locations(GenerateRandomLatitude(), GenerateRandomLongitude()), GetRandomTime());
            user.AddToVisitedLocations(visitedLocation);
        }
    }

    private static readonly Random random = new Random();

    private static double GenerateRandomLongitude()
    {
        return random.NextDouble() * (180 - (-180)) + (-180);
    }

    private static double GenerateRandomLatitude()
    {
        return random.NextDouble() * (90 - (-90)) + (-90);
    }

    private static DateTime GetRandomTime()
    {
        return DateTime.UtcNow.AddDays(-random.Next(30));
    }
}
