using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TripPricer.Helpers;

namespace TripPricer;

public class TripPricer
{
    public List<Provider> GetPrice(string apiKey, Guid attractionId, int adults, int children, int nightsStay, int rewardsPoints)
    {
        List<Provider> providers = new List<Provider>();
        HashSet<string> providersUsed = new HashSet<string>();

        // Sleep to simulate some latency
        Thread.Sleep(ThreadLocalRandom.Current.Next(1, 50));

        for (int i = 0; i < 10; i++)
        {
            int multiple = ThreadLocalRandom.Current.Next(100, 700);
            double childrenDiscount = children / 3.0;
            double price = multiple * adults + multiple * childrenDiscount * nightsStay + 0.99 - rewardsPoints;

            if (price < 0.0)
            {
                price = 0.0;
            }
            
            string provider = GetProviderName(providersUsed);

            providersUsed.Add(provider);
            providers.Add(new Provider(attractionId, provider, price));
        }
        return providers;
    }

    public string GetProviderName(HashSet<string> alreadyUsedProviders)
    {
        List<string> providerNames = new List<string>
        {
            "Holiday Travels",
            "Enterprize Ventures Limited",
            "Sunny Days",
            "FlyAway Trips",
            "United Partners Vacations",
            "Dream Trips",
            "Live Free",
            "Dancing Waves Cruselines and Partners",
            "AdventureCo",
            "Cure-Your-Blues"
        };

        providerNames = providerNames.Where(p => !alreadyUsedProviders.Contains(p)).ToList();

        if (providerNames.Count == 0)
        {
            throw new InvalidOperationException("No more unique providers available.");
        }

        return providerNames[ThreadLocalRandom.Current.Next(providerNames.Count)];
    }
}
