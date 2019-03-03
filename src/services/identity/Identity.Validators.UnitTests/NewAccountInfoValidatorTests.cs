﻿using Bogus;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Identity.DataStores.SqlServer;
using Identity.DTO;
using Identity.Objects;
using MedEasy.DAL.EFStore;
using MedEasy.DAL.Interfaces;
using MedEasy.IntegrationTests.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Xunit.Categories;
using ValidationResult = FluentValidation.Results.ValidationResult;

namespace Identity.Validators.UnitTests
{
    [UnitTest]
    [Feature("Accounts")]
    public class NewAccountInfoValidatorTests : IDisposable, IClassFixture<SqliteDatabaseFixture>
    {
        private ITestOutputHelper _outputHelper;
        private IUnitOfWorkFactory _uowFactory;
        private Mock<ILogger<NewAccountInfoValidator>> _loggerMock;
        private NewAccountInfoValidator _sut;

        public NewAccountInfoValidatorTests(ITestOutputHelper outputHelper, SqliteDatabaseFixture databaseFixture)
        {
            _outputHelper = outputHelper;
            DbContextOptionsBuilder<IdentityContext> dbContextOptionsBuilder = new DbContextOptionsBuilder<IdentityContext>();
            dbContextOptionsBuilder.UseSqlite(databaseFixture.Connection);
            _uowFactory = new EFUnitOfWorkFactory<IdentityContext>(dbContextOptionsBuilder.Options, (options) =>
            {
                IdentityContext context = new IdentityContext(options);
                context.Database.EnsureCreated();
                return context;
            });
            _loggerMock = new Mock<ILogger<NewAccountInfoValidator>>();
            _sut = new NewAccountInfoValidator(_uowFactory, _loggerMock.Object);
        }

        public async void Dispose()
        {
            using (IUnitOfWork uow =_uowFactory.NewUnitOfWork())
            {
                uow.Repository<Account>().Clear();
                await uow.SaveChangesAsync()
                    .ConfigureAwait(false);
            }
            _uowFactory = null;
            _outputHelper = null;
            _loggerMock = null;
        }



        [Fact]
        public void IsNewAccountValidator() => typeof(NewAccountInfoValidator).Should()
            .Implement<IValidator<NewAccountInfo>>();

        public static IEnumerable<object[]> ValidationCases
        {
            get
            {
                
                yield return new object[]
                {
                    Enumerable.Empty<Account>(),
                    new NewAccountInfo(),
                    (Expression<Func<ValidationResult, bool>>)(vr =>
                        vr.Errors.Count == 4
                        && vr.Errors.Once(err => err.PropertyName == nameof(NewAccountInfo.Email))
                        && vr.Errors.Once(err => err.PropertyName == nameof(NewAccountInfo.Password))
                        && vr.Errors.Once(err => err.PropertyName == nameof(NewAccountInfo.ConfirmPassword))
                        && vr.Errors.Once(err => err.PropertyName == nameof(NewAccountInfo.Username))

                    ),
                    "No property set"
                };

                {
                    Faker faker = new Faker();
                    Account account = new Account
                    {
                        UserName = faker.Person.UserName,
                        PasswordHash = faker.Lorem.Word(),
                        Salt = faker.Lorem.Word(),
                        Email = faker.Internet.Email("joker"),
                        UUID = Guid.NewGuid()
                    };
                    yield return new object[]
                    {
                        new[] {account},
                        new NewAccountInfo
                        {
                            Name = faker.Person.Company.Name,
                            Email = "joker@card-city.com",
                            Password = "smile",
                            ConfirmPassword = "smile",
                            Username = account.UserName
                        },
                        (Expression<Func<ValidationResult, bool>>)(vr =>
                            vr.Errors.Count == 1
                            && vr.Errors.Once(err => err.PropertyName == nameof(NewAccountInfo.Username))

                        ),
                        "An account with the same username already exists"
                    };
                }
                {
                    Faker faker = new Faker();
                    Account account = new Account
                    {
                        UserName = "joker",
                        PasswordHash = faker.Lorem.Word(),
                        Salt = faker.Lorem.Word(),
                        Email = faker.Internet.Email("joker"),
                        UUID = Guid.NewGuid()
                    };
                    yield return new object[]
                    {
                        new[] {account},
                        new NewAccountInfo
                        {
                            Name = faker.Person.Company.Name,
                            Email = "joker@card-city.com",
                            Password = "smile",
                            ConfirmPassword = "smiles",
                            Username = faker.Person.UserName
                        },
                        (Expression<Func<ValidationResult, bool>>)(vr =>
                            vr.Errors.Count == 1
                            && vr.Errors.Once(err => err.PropertyName == nameof(NewAccountInfo.ConfirmPassword))

                        ),
                        $"{nameof(NewAccountInfo.Password)} && {nameof(NewAccountInfo.ConfirmPassword)} don't match"
                    };
                }

                {
                    Faker faker = new Faker();
                    Account account = new Account
                    {
                        UserName = "joker",
                        PasswordHash = faker.Lorem.Word(),
                        Salt = faker.Lorem.Word(),
                        Email = "joker@card-city.com",
                        UUID = Guid.NewGuid()
                    };
                    yield return new object[]
                    {
                        new[] {account},
                        new NewAccountInfo
                        {
                            Name = faker.Person.Company.Name,
                            Email = account.Email,
                            Password = "smile",
                            ConfirmPassword = "smile",
                            Username = faker.Person.UserName
                        },
                        (Expression<Func<ValidationResult, bool>>)(vr =>
                            vr.Errors.Count == 1
                            && vr.Errors.Once(err => err.PropertyName == nameof(NewAccountInfo.Email))

                        ),
                        $"An account with the same {nameof(NewAccountInfo.Email)} already exists"
                    };


                    yield return new object[]
                    {
                        Enumerable.Empty<Account>(),
                        new NewAccountInfo
                        {
                            Name = "The dark knight",
                            Email = "batman@gotham.fr",
                            Password = "smile",
                            ConfirmPassword = "smile",
                            Username = "capedcrusader"
                        },
                        (Expression<Func<ValidationResult, bool>>)(vr => !vr.Errors.Any()),
                        $"Informations are ok"
                    };
                }

            }
        }

        [Theory]
        [MemberData(nameof(ValidationCases))]
        public async Task ValidateTests(IEnumerable<Account> accounts, NewAccountInfo newAccountInfo, Expression<Func<ValidationResult, bool>> validationResultExpectation, string reason)
        {
            // Arrange
            using (IUnitOfWork uow = _uowFactory.NewUnitOfWork())
            {
                uow.Repository<Account>().Create(accounts);
                await uow.SaveChangesAsync()
                    .ConfigureAwait(false);
            }

            _outputHelper.WriteLine($"Accounts in datastore : {accounts.Stringify()}");
            _outputHelper.WriteLine($"NewAccount : {newAccountInfo.Stringify()}");

            // Act
            ValidationResult vr = await _sut.ValidateAsync(newAccountInfo, default)
                .ConfigureAwait(false);

            // Assert
            vr.Should()
                .Match(validationResultExpectation, reason);

        }
    }
}
