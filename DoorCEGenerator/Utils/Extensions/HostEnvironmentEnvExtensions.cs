namespace DoorCEGenerator.Utils.Extensions;

    public static class HostEnvironmentEnvExtensions
    {
        public static void SetEnvironment(
            this IHostEnvironment hostEnvironment,
            string? environmentName)
        {
            if (environmentName != null) hostEnvironment.EnvironmentName = environmentName;
        }
    }
