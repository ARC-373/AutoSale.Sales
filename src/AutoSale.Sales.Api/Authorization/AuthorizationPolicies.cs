namespace AutoSale.Api.Authorization;

public static class AuthorizationPolicies
{
    public const string AdminOnly = "AdminOnly";
    public const string VehiclesIntegration = "VehiclesIntegration";
    public const string PaymentWebhook = "PaymentWebhook";
    public const string CognitoGroupsClaimType = "cognito:groups";
    public const string AdministratorsGroup = "admins";
    public const string ServiceKeyHeader = "X-Service-Key";
    public const string PaymentWebhookKeyHeader = "X-Payment-Webhook-Key";
}
