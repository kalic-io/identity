﻿namespace Identity.CQRS.UnitTests.Handlers.Queries.Accounts
{
    using Bogus;

    using FluentAssertions;

    using Identity.CQRS.Handlers.Queries.Accounts;
    using Identity.CQRS.Queries.Accounts;
    using Identity.DataStores;
    using Identity.DTO;
    using Identity.Ids;
    using Identity.Mapping;
    using Identity.Objects;

    using MedEasy.DAL.EFStore;
    using MedEasy.DAL.Interfaces;
    using MedEasy.DAL.Repositories;
    using MedEasy.Ids;
    using MedEasy.IntegrationTests.Core;
    using MedEasy.RestObjects;
    using MedEasy.ValueObjects;

    using NodaTime;
    using NodaTime.Testing;

    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Xunit;
    using Xunit.Abstractions;
    using Xunit.Categories;

    [UnitTest]
    [Feature("Identity")]
    public class HandleGetPageOfAccountsQueryTests : IClassFixture<SqliteEfCoreDatabaseFixture<IdentityDataStore>>
    {
        private readonly ITestOutputHelper _outputHelper;
        private readonly EFUnitOfWorkFactory<IdentityDataStore> _uowFactory;
        private readonly HandleGetPageOfAccountsQuery _sut;
        private readonly FakeClock _clock;

        public HandleGetPageOfAccountsQueryTests(ITestOutputHelper outputHelper, SqliteEfCoreDatabaseFixture<IdentityDataStore> databaseFixture)
        {
            _outputHelper = outputHelper;

            _clock = new FakeClock(new Instant());
            _uowFactory = new EFUnitOfWorkFactory<IdentityDataStore>(databaseFixture.OptionsBuilder.Options, (options) =>
            {
                IdentityDataStore context = new(options, _clock);
                context.Database.EnsureCreated();
                return context;
            });

            _sut = new HandleGetPageOfAccountsQuery(_uowFactory, expressionBuilder: AutoMapperConfig.Build().ExpressionBuilder);
        }

        [Fact]
        public async Task GivenNoUser_Handler_Returns_None()
        {
            // Arrange
            PaginationConfiguration data = new()
            {
                PageSize = 10,
                Page = 1,
            };
            GetPageOfAccountsQuery query = new(data);

            // Act
            Page<AccountInfo> pageOfAccounts = await _sut.Handle(query, default)
                .ConfigureAwait(false);

            // Assert
            pageOfAccounts.Should()
                .NotBeNull();
            pageOfAccounts.Total.Should()
                .Be(0);
            pageOfAccounts.Count.Should().Be(1);
            pageOfAccounts.Entries.Should()
                .NotBeNull().And
                .BeEmpty("Account store is empty element");
        }

        [Fact]
        public async Task GivenAccounts_Handler_Returns_AccountsThatBelongToTenant()
        {
            // Arrange
            TenantId tenantId = TenantId.New();
            IEnumerable<Account> accounts = new Faker<Account>()
                .CustomInstantiator(faker =>
                {
                    Account account = new(id: AccountId.New(),
                                          tenantId: tenantId,
                                          email: Email.From(faker.Internet.Email()),
                                          username: UserName.From(faker.Person.UserName),
                                          passwordHash: Password.From(faker.Internet.Password()),
                                          salt: string.Empty)
                    {
                        CreatedDate = faker.Noda().Instant.Recent()
                    };

                    return account;
                })
                .Generate(10);

            using (IUnitOfWork uow = _uowFactory.NewUnitOfWork())
            {
                uow.Repository<Account>().Create(accounts);
                await uow.SaveChangesAsync()
                    .ConfigureAwait(false);
            }

            PaginationConfiguration data = new()
            {
                PageSize = 10,
                Page = 1
            };
            GetPageOfAccountsQuery query = new(data);

            // Act
            Page<AccountInfo> pageOfAccounts = await _sut.Handle(query, default)
                                                         .ConfigureAwait(false);

            // Assert
            pageOfAccounts.Should().NotBeNull();
            pageOfAccounts.Count.Should().Be(1);
            pageOfAccounts.Entries.Should()
                                  .NotBeNull().And
                                  .NotContainNulls().And
                                  .HaveCount(10);
        }
    }
}
