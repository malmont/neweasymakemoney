namespace Easymakemoney.Services.Device;

public class MauiDeviceInfoService : IDeviceInfoService
{
    public string GetDeviceName() => $"{DeviceInfo.Manufacturer} {DeviceInfo.Model}".Trim();
}

