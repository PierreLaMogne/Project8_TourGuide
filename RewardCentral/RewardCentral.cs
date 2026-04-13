using RewardCentral.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RewardCentral;

public class RewardCentral
{
    // Method changed to go asynchronous to improve performance when calculating rewards for multiple users
    public static async Task<int> GetAttractionRewardPoints(Guid attractionId, Guid userId) 
    {
        int randomDelay = new Random().Next(1, 1000);
        await Task.Delay(randomDelay);

        int randomInt = new Random().Next(1, 1000);
        return randomInt;
    }
}
