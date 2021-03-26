using System;
using System.Threading.Tasks;

namespace Shared.Library
{
    public class Work
    {
        public static async Task DoWorkAsync()
        {
            await Task.Delay(1000);
        }
    }
}
