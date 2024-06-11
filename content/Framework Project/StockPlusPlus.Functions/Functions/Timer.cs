using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace StockPlusPlus.Functions.Functions
{
    public class Timer
    {
        private readonly ILogger _logger;

        public Timer(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<Timer>();
        }

        [Function("Timer")]
        public void Run([TimerTrigger("0 */5 * * * *")] TimerInfo myTimer)
        {
            _logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            
            if (myTimer.ScheduleStatus is not null)
            {
                _logger.LogInformation($"Next timer schedule at: {myTimer.ScheduleStatus.Next}");
            }
        }
    }
}
