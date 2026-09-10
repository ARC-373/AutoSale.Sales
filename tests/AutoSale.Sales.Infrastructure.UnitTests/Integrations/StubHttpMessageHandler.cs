namespace AutoSale.Sales.Infrastructure.UnitTests.Integrations;

internal sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _response;

    public StubHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> response)
    {
        _response = response;
    }

    public string? RequestUri { get; private set; }
    public string? ServiceKey { get; private set; }
    public string? RequestBody { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        RequestUri = request.RequestUri?.ToString();
        ServiceKey = request.Headers.GetValues("X-Service-Key").Single();
        RequestBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        return await _response(request, cancellationToken);
    }
}
