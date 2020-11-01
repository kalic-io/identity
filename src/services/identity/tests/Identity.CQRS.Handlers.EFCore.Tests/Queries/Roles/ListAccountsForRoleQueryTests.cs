﻿using FluentAssertions;
using Identity.CQRS.Queries.Roles;
using Identity.DTO;
using MedEasy.CQRS.Core.Queries;
using Optional;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace Identity.CQRS.Handlers.EFCore.Tests.Queries.Roles
{
    public class ListAccountsForRoleQueryTests
    {
        private readonly ITestOutputHelper _outputHelper;

        public ListAccountsForRoleQueryTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        [Fact]
        public void IsValid() => typeof(ListAccountsForRoleQuery).Should()
                                                                .NotHaveDefaultConstructor().And
                                                                .NotBeStatic().And
                                                                .HaveConstructor(new[] { typeof(Guid) }).And
                                                                .BeDerivedFrom<QueryBase<Guid, Guid, Option<IEnumerable<AccountInfo>>>>();
    }
}