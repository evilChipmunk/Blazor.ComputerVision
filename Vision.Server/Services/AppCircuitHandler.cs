using Microsoft.AspNetCore.Components.Server.Circuits;

namespace Vision.Server.Services
{
    public class AppCircuitHandler : CircuitHandler
    {
        public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
        {
            // Log or handle opening of circuit
            return base.OnCircuitOpenedAsync(circuit, cancellationToken);
        }

        public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
        {
            // Log or handle closing of circuit
            return base.OnCircuitClosedAsync(circuit, cancellationToken);
        }

        public override Task OnConnectionDownAsync(Circuit circuit, CancellationToken cancellationToken)
        {
            // Log or handle connection down
            return base.OnConnectionDownAsync(circuit, cancellationToken);
        }

        public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
        {
            // Log or handle connection up
            return base.OnConnectionUpAsync(circuit, cancellationToken);
        }
    }

}