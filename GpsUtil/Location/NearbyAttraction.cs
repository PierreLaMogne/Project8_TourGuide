namespace GpsUtil.Location
{
    public class NearbyAttraction
    {
        public string AttractionName { get; }
        public Locations AttractionLocation { get; }
        public Locations UserLocation { get; }
        public double Distance { get; }
        public int RewardPoint { get; }

        public NearbyAttraction(string attractionName, Locations attractionLocation, Locations userLocation, double distance, int rewardPoint)
        {
            AttractionName = attractionName;
            AttractionLocation = attractionLocation;
            UserLocation = userLocation;
            Distance = distance;
            RewardPoint = rewardPoint;
        }
    }
}
