﻿namespace Identity.CQRS.Handlers.Queries
{
    using Identity.CQRS.Queries;

    using MedEasy.ValueObjects;

    using MediatR;

    using Microsoft.AspNetCore.Cryptography.KeyDerivation;

    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public class HandleHashPasswordWithPredefinedSaltAndIterationQuery : IRequestHandler<HashPasswordWithPredefinedSaltAndIterationQuery, Password>
    {
        ///<inheritdoc/>
        public Task<Password> Handle(HashPasswordWithPredefinedSaltAndIterationQuery request, CancellationToken cancellationToken) =>
            // generate a 128-bit salt using a secure PRNG

            // derive a 256-bit subkey (use HMACSHA1 with 10,000 iterations)
            Task.FromResult(Password.From(Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: request.Data.password.Value,
                salt: Convert.FromBase64String(request.Data.salt),
                prf: KeyDerivationPrf.HMACSHA512,
                iterationCount: request.Data.iteration,
                numBytesRequested: 256 / 8))));
    }
}
