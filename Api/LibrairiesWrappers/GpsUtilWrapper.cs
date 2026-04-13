using GpsUtil.Location;
using TourGuide.LibrairiesWrappers.Interfaces;

namespace TourGuide.LibrairiesWrappers;

public class GpsUtilWrapper : IGpsUtil
{
    private readonly GpsUtil.GpsUtil _gpsUtil;

    public GpsUtilWrapper()
    {
        _gpsUtil = new();
    }

    public async Task<VisitedLocation> GetUserLocation(Guid userId)
    {
        return await _gpsUtil.GetUserLocation(userId);
    }

    public async Task<List<Attraction>> GetAttractions()
    {
        return await _gpsUtil.GetAttractions();
    }
}
