using Easymakemoney.Services.Device;

namespace Easymakemoney.Tests.Fakes;

public class FakeDeviceInfoService : IDeviceInfoService
{
    public string DeviceName { get; set; } = "Appareil Test";

    public string GetDeviceName() => DeviceName;
}

