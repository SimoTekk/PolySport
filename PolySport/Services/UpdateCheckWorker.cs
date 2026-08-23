namespace PolySport.Services
{
    /// <summary>
    /// Prüft im Hintergrund regelmässig auf neue Versionen. Dadurch kann die
    /// Anzeige im Layout den Zwischenspeicher lesen, ohne dass ein Seitenaufruf
    /// auf eine Netzwerkantwort warten muss.
    /// </summary>
    public class UpdateCheckWorker : BackgroundService
    {
        private readonly IUpdateService _updateService;
        private readonly ILogger<UpdateCheckWorker> _logger;
        private readonly TimeSpan _interval;

        public UpdateCheckWorker(
            IUpdateService updateService,
            IConfiguration configuration,
            ILogger<UpdateCheckWorker> logger)
        {
            _updateService = updateService;
            _logger = logger;

            var hours = configuration.GetValue<double?>("Update:CheckIntervalHours") ?? 6;
            _interval = TimeSpan.FromHours(Math.Max(1, hours));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Kurz warten, damit der Start der Anwendung nicht blockiert wird
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                var info = await _updateService.CheckAsync(stoppingToken);

                if (info.IsUpdateAvailable)
                    _logger.LogInformation("Neue Version verfügbar: {Version} (installiert: {Current})",
                        info.LatestVersion, info.CurrentVersion);

                try
                {
                    await Task.Delay(_interval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }
    }
}
