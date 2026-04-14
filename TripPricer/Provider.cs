using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TripPricer;

public class Provider
{
    public string Name { get; init; }
    public double Price { get; init; }
    public Guid TripId { get; init; }

    public Provider(Guid tripId, string name, double price)
    {
        this.TripId = tripId;
        this.Name = name;
        this.Price = price;
    }
}
