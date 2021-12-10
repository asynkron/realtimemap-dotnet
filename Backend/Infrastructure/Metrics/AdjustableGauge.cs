using System.Diagnostics.Metrics;

namespace Backend.Infrastructure.Metrics;

public class AdjustableGauge
{
    private long _currentValue = 0;

    public AdjustableGauge(Meter meter, string name, string description = null)
    {
        meter.CreateObservableGauge(name, () => _currentValue, description: description);
    }

    public void ChangeBy(long delta) => Interlocked.Add(ref _currentValue, delta);
}