using System.Net;

namespace DoorCEServer.Application.DataContentsManager.Domain.Services;

public static class UrlSafety
{
    public static async Task<bool> ValidateFileUrlAsync(string? url)
    {
        // 1. Check if URL is correctly formed
        if (string.IsNullOrWhiteSpace(url))
            return false;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        // 2. Allow only HTTP and HTTPS schemes
        if (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)
            return false;

        // 3. Check if the address is public
        if (!await IsPublicAddressAsync(uri))
            return false;

        return true;
    }

    private static async Task<bool> IsPublicAddressAsync(Uri uri)
    {
        try
        {
            var host = uri.Host;

            // DNS resolve
            var addresses = await Dns.GetHostAddressesAsync(host);
            foreach (var ip in addresses)
            {
                if (!IsPublicIp(ip))
                    return false;
            }
        }
        catch
        {
            return false;
        }

        return true;
    }

    private static bool IsPublicIp(IPAddress ip)
    {
        var bytes = ip.GetAddressBytes();

        return !(IPAddress.IsLoopback(ip)
                 || IsInRange(bytes, "10.0.0.0", "10.255.255.255")
                 || IsInRange(bytes, "172.16.0.0", "172.31.255.255")
                 || IsInRange(bytes, "192.168.0.0", "192.168.255.255")
                 || ip.IsIPv6LinkLocal
                 || ip.IsIPv6SiteLocal
                 || ip.IsIPv6Multicast);
    }

    private static bool IsInRange(byte[] ipBytes, string start, string end)
    {
        var startBytes = IPAddress.Parse(start).GetAddressBytes();
        var endBytes = IPAddress.Parse(end).GetAddressBytes();

        for (int i = 0; i < ipBytes.Length; i++)
        {
            if (ipBytes[i] < startBytes[i] || ipBytes[i] > endBytes[i])
                return false;
        }
        return true;
    }
}
