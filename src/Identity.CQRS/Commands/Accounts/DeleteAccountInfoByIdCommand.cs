﻿namespace Identity.CQRS.Commands.Accounts
{
    using Identity.Ids;

    using MedEasy.CQRS.Core.Commands;
    using MedEasy.CQRS.Core.Commands.Results;

    using System;

    /// <summary>
    /// Command to delete an <see cref="Accountinfo"/> by its <see cref="AccountInfo.Id"/>
    /// </summary>
    public class DeleteAccountInfoByIdCommand : CommandBase<Guid, AccountId, DeleteCommandResult>
    {
        /// <summary>
        /// Builds a new <see cref="DeleteAccountInfoByIdCommand"/> instance
        /// </summary>
        /// <param name="id">id of the <see cref="AccountInfo"/> to delete.</param>
        public DeleteAccountInfoByIdCommand(AccountId id) : base(Guid.NewGuid(), id)
        {
        }
    }
}
