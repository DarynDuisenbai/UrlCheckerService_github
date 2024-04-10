namespace UrlCheckerService
{
    public class BackgroundServiceRunner : BackgroundService
    {
        private readonly LinkChecker _linkChecker;
        private readonly IConfiguration _configuration;
        private readonly TimeSpan _interval;

        public BackgroundServiceRunner(LinkChecker linkChecker, IConfiguration configuration)
        {
            _linkChecker = linkChecker;
            _configuration = configuration;
            _interval = TimeSpan.FromMinutes(_configuration.GetValue<int>("IntervalInMinutes"));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await _linkChecker.CheckLinks();
                await Task.Delay(_interval, stoppingToken);
            }
        }
    }
}
