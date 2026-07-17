namespace CleanArchitecture.WebApi.Routing;

/// <summary>Превращает [controller]/[action] в нижний регистр: AuthController → /auth.</summary>
public class LowercaseParameterTransformer : IOutboundParameterTransformer
{
    public string? TransformOutbound(object? value) => value?.ToString()?.ToLowerInvariant();
}
